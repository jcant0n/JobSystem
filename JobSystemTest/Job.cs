using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JobSystemTest
{
    /// <summary>
    /// Represents a job to be executed by the job system.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    public struct Job
    {
        /// <summary>
        /// The function to be executed by this job.
        /// </summary>
        [FieldOffset(0)]
        public readonly Action<JobArgs> Function;

        /// <summary>
        /// The context in which this job is running.
        /// </summary>
        [FieldOffset(8)]
        public readonly JobsContext Context;

        /// <summary>
        /// The ID of the group this job belongs to.
        /// </summary>
        [FieldOffset(16)]
        public readonly uint GroupID;

        /// <summary>
        /// The starting offset of this job within its group.
        /// </summary>
        [FieldOffset(20)]
        public readonly uint GroupJobOffset;

        /// <summary>
        /// The ending offset of this job within its group.
        /// </summary>
        [FieldOffset(24)]
        public readonly uint GroupJobEnd;

        /// <summary>
        /// Initializes a new instance of the Job struct.
        /// </summary>
        /// <param name="function">The function to be executed.</param>
        /// <param name="context">The context in which the job is running.</param>
        /// <param name="groupID">The ID of the group this job belongs to.</param>
        /// <param name="groupJobOffset">The starting offset of this job within its group.</param>
        /// <param name="groupJobEnd">The ending offset of this job within its group.</param>
        public Job(Action<JobArgs> function, JobsContext context, uint groupID, uint groupJobOffset, uint groupJobEnd)
        {
            Function = function;
            Context = context;
            GroupID = groupID;
            GroupJobOffset = groupJobOffset;
            GroupJobEnd = groupJobEnd;
        }

        /// <summary>
        /// Executes the job function for each item in the job's range.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Execute()
        {
            try
            {
                for (uint i = GroupJobOffset; i < GroupJobEnd; i++)
                {
                    JobArgs args = new JobArgs(i, GroupID, i - GroupJobOffset);
                    Function.Invoke(args);
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Error executing job: {e.Message}");
            }
            finally
            {
                Context.Decrement();
            }
        }
    }

}
