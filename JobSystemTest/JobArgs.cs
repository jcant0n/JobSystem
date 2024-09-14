namespace JobSystemTest
{
    public struct JobArgs
    {
        public readonly uint JobIndex;
        public readonly uint GroupID;
        public readonly uint GroupIndex;

        public JobArgs(uint jobIndex, uint groupID, uint groupIndex)
        {
            JobIndex = jobIndex;
            GroupID = groupID;
            GroupIndex = groupIndex;
        }
    }
}
