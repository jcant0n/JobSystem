using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobSystemTest
{
    public class JobsContext
    {
        public uint PendingJobs = 0;
        public AutoResetEvent Signal;

        public JobsContext()
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
}
