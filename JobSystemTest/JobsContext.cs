namespace JobSystemTest
{
    /// <summary>
    /// Represents the context for a group of jobs, managing their execution state.
    /// </summary>
    public class JobsContext
    {
        protected uint pendingJobs = 0;
        protected internal AutoResetEvent signal;

        /// <summary>
        /// Gets the number of pending jobs in this context.
        /// </summary>
        public uint PendingJobs => this.pendingJobs;

        /// <summary>
        /// Initializes a new instance of the JobsContext class.
        /// </summary>
        public JobsContext()
        {
            pendingJobs = 0;
            signal = new AutoResetEvent(false);
        }

        /// <summary>
        /// Increments the number of pending jobs by the specified count.
        /// </summary>
        /// <param name="count">The number of jobs to add to the pending count.</param>
        public void Increment(uint count)
        {
            Interlocked.Add(ref pendingJobs, count);
        }

        /// <summary>
        /// Decrements the number of pending jobs and signals completion if all jobs are done.
        /// </summary>
        public void Decrement()
        {
            uint currentJobs = Interlocked.Decrement(ref pendingJobs);
            if (currentJobs == 0)
            {
                signal.Set();
            }
        }
    }
}
