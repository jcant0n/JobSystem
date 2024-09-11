using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JobSystemTest
{
    public class JobSystem : IDisposable
    {
        public Worker[] Workers;
        public readonly uint NumThreads;
        private uint nextQueueIndex;
        private bool disposed;

        public struct Context
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

            Workers = new Worker[NumThreads];

            for (int i = 0; i < NumThreads; i++)
            {
                Workers[i] = new Worker((uint)i);
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < Workers.Length; i++)
            {
                Workers[i].Dispose();
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
            context.Increment(1);

            Job job = new Job
            {
                Function = function,
                Context = context,
                GroupID = 0,
                GroupJobOffset = 0,
                GroupJobEnd = 1,
            };

            uint queueIndex = Interlocked.Increment(ref nextQueueIndex) % NumThreads;
            Worker w = Workers[queueIndex];
            w.JobQueue.Enqueue(job);
            w.Signal.Set();
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
                Worker w = Workers[queueIndex];
                w.JobQueue.Enqueue(job);
            }

            for (int i = 0; i < NumThreads; i++)
            {
                Workers[i].Signal.Set();
            }
        }
    }
}
