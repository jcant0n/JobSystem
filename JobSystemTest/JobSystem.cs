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
using static JobSystemTest.JobSystem;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace JobSystemTest
{
    public class JobSystem : IDisposable
    {
        public Thread[] Workers;
        public ConcurrentQueue<Job>[] QueuePerWorker;
        public ManualResetEventSlim[] SignalPerWorker;

        public readonly uint NumThreads;
        private uint nextQueueIndex;
        private bool isRunning;
        private bool disposed;

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
                    Random random = new Random(unchecked(Environment.TickCount * (int)threadID));

                    while (isRunning)
                    {
                        SignalPerWorker[threadID].Wait();

                        // Own queue
                        while (QueuePerWorker[threadID].TryDequeue(out var job))
                        {
                            job.Execute();
                        }

                        // Steal from other queues
                        int attempts = 0;
                        int maxAttempts = (int)NumThreads;

                        while (attempts < maxAttempts)
                        {
                            uint victimThreadID = (uint)random.Next((int)NumThreads);
                            if (victimThreadID == threadID)
                            {
                                attempts++;
                                continue;
                            }

                            if (QueuePerWorker[victimThreadID].TryDequeue(out var job))
                            {
                                Console.WriteLine($"Thread: {threadID} stolen from Thread: {victimThreadID}");
                                job.Execute();
                                attempts = 0;
                            }
                            else
                            {
                                attempts++;
                            }
                        }

                        SignalPerWorker[threadID].Reset();
                    }
                })
                {
                    Name = $"JobSystem Worker {i}",
                    IsBackground = true,
                    Priority = ThreadPriority.Normal,
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

            Workers = null;
        }

        public bool IsBusy(JobsContext context)
        {
            return context.PendingJobs > 0;
        }

        public void Wait(JobsContext context)
        {
            context.Signal.WaitOne();
        }

        public void Execute(JobsContext context, Action<JobArgs> function)
        {
            Job job = new Job(function, context, 0, 0, 1);
            uint queueIndex = Interlocked.Increment(ref nextQueueIndex) % NumThreads;
            QueuePerWorker[queueIndex].Enqueue(job);
            context.Increment(1);
            SignalPerWorker[queueIndex].Set();
        }

        public void Dispatch(JobsContext context, uint jobCount, uint groupSize, Action<JobArgs> function)
        {
            uint groupCount = (jobCount + groupSize - 1) / groupSize;
            context.Increment(groupCount);

            for (uint groupID = 0; groupID < groupCount; groupID++)
            {
                uint offset = groupID * groupSize;
                Job job = new Job(function, context, groupID, offset, Math.Min(jobCount, offset + groupSize));

                uint queueIndex = Interlocked.Increment(ref nextQueueIndex) % NumThreads;
                QueuePerWorker[queueIndex].Enqueue(job);
                SignalPerWorker[queueIndex].Set();
            }
        }
    }
}
