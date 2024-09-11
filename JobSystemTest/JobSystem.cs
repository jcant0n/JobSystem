using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
        private bool isRunning;
        private bool disposed;

        public class Context
        {
            public volatile uint PendingJobs;
        }

        public JobSystem(uint maxThreadCount = 0)
        {
            isRunning = false;

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
            if (isRunning)
            {
                isRunning = false;

                for (int i = 0; i < Workers.Length; i++)
                {
                    Workers[i].Dispose();
                }

                Array.Clear(Workers);
            }

            GC.SuppressFinalize(this);
        }

        public bool IsBusy(Context context)
        {
            return context.PendingJobs > 0;
        }

        public void Wait(Context context)
        {
            while (IsBusy(context))
            {
                Thread.Yield();
            }
        }

        public void Execute(Context context, Action<JobArgs> function)
        {
            Interlocked.Increment(ref context.PendingJobs);

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

        public void Dispatch(Context content, uint jobCount, uint groupSize, Action<JobArgs> function)
        {
            uint groupCount = (jobCount + groupSize - 1) / groupSize;
            Interlocked.Add(ref content.PendingJobs, groupCount);

            Job job = new Job
            {
                Function = function,
                Context = content,
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
