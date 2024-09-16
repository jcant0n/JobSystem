using System.Runtime.InteropServices;

namespace JobSystemTest
{
    /// <summary>
    /// Represents the arguments passed to a job function.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public struct JobArgs
    {
        /// <summary>
        /// The index of the current job.
        /// </summary>
        [FieldOffset(0)]
        public readonly uint JobIndex;

        /// <summary>
        /// The ID of the group this job belongs to.
        /// </summary>
        [FieldOffset(4)]
        public readonly uint GroupID;

        /// <summary>
        /// The index of this job within its group.
        /// </summary>
        [FieldOffset(8)]
        public readonly uint GroupIndex;

        /// <summary>
        /// Padding to ensure 16-byte alignment.
        /// </summary>
        [FieldOffset(12)]
        private readonly uint padding;

        /// <summary>
        /// Initializes a new instance of the JobArgs struct.
        /// </summary>
        /// <param name="jobIndex">The index of the current job.</param>
        /// <param name="groupID">The ID of the group this job belongs to.</param>
        /// <param name="groupIndex">The index of this job within its group.</param>
        public JobArgs(uint jobIndex, uint groupID, uint groupIndex)
        {
            JobIndex = jobIndex;
            GroupID = groupID;
            GroupIndex = groupIndex;
            padding = 0;
        }
    }
}
