using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JobSystemTest
{
    public unsafe class JobSystem : IDisposable
    {
        public Thread[] Workers;
        public ConcurrentQueue<Job>[] QueuePerWorker;
        public ManualResetEventSlim[] SignalPerWorker;

        public readonly uint NumThreads;
        private uint nextQueueIndex;
        private bool isRunning;
        private bool disposed;

        public class Context
        {
            public volatile uint PendingJobs = 0;
            public AutoResetEvent Signal;

            public Context()
            {
                PendingJobs = 0;
                Signal = new AutoResetEvent(false);
            }

            public void Increment(uint count)
            {
                Interlocked.Add(ref PendingJobs, count);
            }
            public void Decrement()
            {
                uint currentJobs = Interlocked.Decrement(ref PendingJobs);
                if (currentJobs == 0)
                {
                    Signal.Set();
                }
            }
        }

        public JobSystem(uint maxThreadCount = 0)
        {
            if (maxThreadCount == 0)
            {
                NumThreads = (uint)Environment.ProcessorCount;
            }
            else
            {
                NumThreads = maxThreadCount;
            }

            nextQueueIndex = NumThreads - 1;
            Workers = new Thread[NumThreads];
            QueuePerWorker = new ConcurrentQueue<Job>[NumThreads];
            SignalPerWorker = new ManualResetEventSlim[NumThreads];
            isRunning = true;

            for (uint i = 0; i < NumThreads; i++)
            {
                SignalPerWorker[i] = new ManualResetEventSlim(false);
                QueuePerWorker[i] = new ConcurrentQueue<Job>();

                uint threadID = i;
                Workers[i] = new Thread(() =>
                {
                    uint nextThreadID = threadID;

                    while (isRunning)
                    {
                        SignalPerWorker[threadID].Wait();
                        
                        // Own queue
                        while (QueuePerWorker[threadID].TryDequeue(out var job))
                        {
                            job.Execute();
                        }

                        // Steal from other queues
                        for (int n = 0; n < NumThreads; n++)
                        {
                            nextThreadID = (nextThreadID + 1) % NumThreads;

                            while (QueuePerWorker[nextThreadID].TryDequeue(out var job))
                            {
                                Console.WriteLine($"Thread: {threadID} stolen from Thread: {nextThreadID}");
                                job.Execute();
                            }
                        }

                        SignalPerWorker[threadID].Reset();
                    }
                })
                {
                    Name = $"JobSystem Worker {i}",
                    IsBackground = true,
                    Priority = ThreadPriority.Highest,
                };

                Workers[threadID].Start();
            }
        }

        public void Dispose()
        {
            isRunning = false;

            for (int i = 0; i < Workers.Length; i++)
            {
                SignalPerWorker[i].Set();
                Workers[i].Join();
                SignalPerWorker[i].Dispose();
            }

            Array.Clear(Workers);
            GC.SuppressFinalize(this);
        }

        public bool IsBusy(Context context)
        {
            return context.PendingJobs > 0;
        }

        public void Wait(Context context)
        {
            context.Signal.WaitOne();
        }

        public void Execute(Context context, Action<JobArgs> function)
        {
            Job job = new Job
            {
                Function = function,
                Context = context,
                GroupID = 0,
                GroupJobOffset = 0,
                GroupJobEnd = 1,
            };

            uint queueIndex = Interlocked.Increment(ref nextQueueIndex) % NumThreads;
            QueuePerWorker[queueIndex].Enqueue(job);
            context.Increment(1);
            SignalPerWorker[queueIndex].Set();
        }

        public void Dispatch(Context context, uint jobCount, uint groupSize, Action<JobArgs> function)
        {
            uint groupCount = (jobCount + groupSize - 1) / groupSize;
            context.Increment(groupCount);

            Job job = new Job
            {
                Function = function,
                Context = context,
            };

            for (uint groupID = 0; groupID < groupCount; groupID++)
            {
                job.GroupID = groupID;
                job.GroupJobOffset = groupID * groupSize;
                job.GroupJobEnd = Math.Min(jobCount, job.GroupJobOffset + groupSize);

                uint queueIndex = Interlocked.Increment(ref nextQueueIndex) % NumThreads;
                QueuePerWorker[queueIndex].Enqueue(job);
            }

            for (int i = 0; i < NumThreads; i++)
            {
                SignalPerWorker[i].Set();
            }
        }
    }
}
