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

        public Worker(uint threadID)
        {
            ThreadID = threadID;
            Signal = new ManualResetEventSlim(false);
            JobQueue = new ConcurrentQueue<Job>();
            Thread = new Thread(() =>
            {
                while (true)
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
                Priority = ThreadPriority.Normal
            };

            Thread.Start();
        }

        public void Dispose()
        {
            Signal.Set();
            Thread.Join();
            Signal.Dispose();
        }
    }
}
