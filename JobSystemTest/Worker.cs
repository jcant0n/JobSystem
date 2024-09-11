using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobSystemTest
{
    public class Worker : IDisposable
    {
        public uint ThreadID;
        public Thread Thread;
        public ConcurrentQueue<Job> JobQueue;
        public ManualResetEventSlim Signal;
        public bool IsRunning;

        public Worker(uint threadID)
        {
            ThreadID = threadID;
            Signal = new ManualResetEventSlim(true);
            JobQueue = new ConcurrentQueue<Job>();
            Thread = new Thread(() =>
            {
                while (IsRunning)
                {
                    Signal.Wait();

                    while (JobQueue.TryDequeue(out var job))
                    {
                        job?.Execute();
                    }

                    Signal.Reset();
                }
            })
            {
                IsBackground = true,
                Priority = ThreadPriority.AboveNormal,
            };

            IsRunning = true;
            Thread.Start();
        }

        public void Dispose()
        {
            IsRunning = false;
            Signal.Set();
            Thread.Join();
            Signal.Dispose();
        }
    }
}
