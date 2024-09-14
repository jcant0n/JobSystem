using System.Runtime.CompilerServices;

namespace JobSystemTest
{

    public struct Job
    {
        public readonly Action<JobArgs> Function;
        public readonly JobsContext Context;
        public readonly uint GroupID;
        public readonly uint GroupJobOffset;
        public readonly uint GroupJobEnd;

        public Job(Action<JobArgs> function, JobsContext context, uint groupID, uint groupJobOffset, uint groupJobEnd)
        {
            Function = function;
            Context = context;
            GroupID = groupID;
            GroupJobOffset = groupJobOffset;
            GroupJobEnd = groupJobEnd;
        }

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
