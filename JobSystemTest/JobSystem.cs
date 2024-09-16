using System;
using System.Collections.Concurrent;
using System.Threading;

namespace JobSystemTest
{
    /// <summary>
    /// Manages the execution of jobs across multiple threads.
    /// </summary>
    public class JobSystem : IDisposable
    {
        protected Thread[] Workers;
        protected ConcurrentQueue<Job>[] QueuePerWorker;
        protected ManualResetEventSlim[] SignalPerWorker;
        protected uint nextQueueIndex;
        protected bool isRunning;

        /// <summary>
        /// Gets the number of threads used by this job system.
        /// </summary>
        public readonly uint NumThreads;

        private static readonly ThreadLocal<Xoshiro256StarStar> threadLocalXorShiftRandom = new ThreadLocal<Xoshiro256StarStar>(() =>
        {
            return new Xoshiro256StarStar((ulong)(Environment.TickCount ^ Thread.CurrentThread.ManagedThreadId));
        });

        /// <summary>
        /// Initializes a new instance of the JobSystem class.
        /// </summary>
        /// <param name="maxThreadCount">The maximum number of threads to use. If 0, uses the number of processor cores.</param>
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
                Workers[i] = new Thread(() => WorkerThread(threadID))
                {
                    Name = $"JobSystem Worker {i}",
                    IsBackground = true,
                    Priority = ThreadPriority.Normal,
                };

                Workers[threadID].Start();
            }
        }

        /// <summary>
        /// The main loop for worker threads, processing jobs and stealing work when necessary.
        /// </summary>
        /// <param name="threadID">The ID of the worker thread.</param>
        private void WorkerThread(uint threadID)
        {
            var random = threadLocalXorShiftRandom.Value;

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
                int maxAttempts = (int)NumThreads - 1;

                while (attempts < maxAttempts)
                {
                    uint victimThreadID = random.Next(NumThreads);
                    if (victimThreadID == threadID)
                    {
                        attempts++;
                        continue;
                    }

                    if (QueuePerWorker[victimThreadID].TryDequeue(out var job))
                    {
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
        }

        /// <summary>
        /// Releases all resources used by the JobSystem.
        /// </summary>
        public void Dispose()
        {
            isRunning = false;

            for (int i = 0; i < Workers.Length; i++)
            {
                SignalPerWorker[i].Set();
                Workers[i].Join();
                SignalPerWorker[i].Dispose();
            }
        }

        /// <summary>
        /// Checks if the specified context has any pending jobs.
        /// </summary>
        /// <param name="context">The context to check.</param>
        /// <returns>True if there are pending jobs, false otherwise.</returns>
        public bool IsBusy(JobsContext context)
        {
            return context.PendingJobs > 0;
        }

        /// <summary>
        /// Waits for all jobs in the specified context to complete.
        /// </summary>
        /// <param name="context">The context to wait on.</param>
        public void Wait(JobsContext context)
        {
            context.signal.WaitOne();
        }

        /// <summary>
        /// Executes a single job.
        /// </summary>
        /// <param name="context">The context for the job.</param>
        /// <param name="function">The function to execute.</param>
        public void Execute(JobsContext context, Action<JobArgs> function)
        {
            context.Increment(1);
            Job job = new Job(function, context, 0, 0, 1);
            uint queueIndex = Interlocked.Increment(ref nextQueueIndex) % NumThreads;
            QueuePerWorker[queueIndex].Enqueue(job);
            SignalPerWorker[queueIndex].Set();
        }

        /// <summary>
        /// Dispatches multiple jobs, grouped into batches.
        /// </summary>
        /// <param name="context">The context for the jobs.</param>
        /// <param name="jobCount">The total number of jobs to dispatch.</param>
        /// <param name="groupSize">The size of each job group.</param>
        /// <param name="function">The function to execute for each job.</param>
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
