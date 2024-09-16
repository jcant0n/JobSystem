using System.Runtime.InteropServices;

namespace JobSystemTest
{
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public struct JobArgs
    {
        [FieldOffset(0)]
        public readonly uint JobIndex;

        [FieldOffset(4)]
        public readonly uint GroupID;
        
        [FieldOffset(8)]
        public readonly uint GroupIndex;
        
        [FieldOffset(12)]
        private readonly uint padding;

        public JobArgs(uint jobIndex, uint groupID, uint groupIndex)
        {
            JobIndex = jobIndex;
            GroupID = groupID;
            GroupIndex = groupIndex;
            padding = 0;
        }
    }
}
