using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JobSystemTest
{
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    public struct Job
    {
        [FieldOffset(0)]
        public readonly Action<JobArgs> Function;

        [FieldOffset(8)]
        public readonly JobsContext Context;

        [FieldOffset(16)]
        public readonly uint GroupID;

        [FieldOffset(20)]
        public readonly uint GroupJobOffset;

        [FieldOffset(24)]
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
