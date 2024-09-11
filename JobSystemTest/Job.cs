using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobSystemTest
{
    public struct JobArgs
    {
        public uint JobIndex;
        public uint GroupID;
        public uint GroupIndex;
    }

    public class Job
    {
        public required Action<JobArgs> Function;
        public required JobSystem.Context Context;
        public uint GroupID;
        public uint GroupJobOffset;
        public uint GroupJobEnd;

        public void Execute()
        {
            JobArgs args = default;
            args.GroupID = GroupID;

            for (uint i = GroupJobOffset; i < GroupJobEnd; i++)
            {
                args.JobIndex = i;
                args.GroupIndex = i - GroupJobOffset;
                Function.Invoke(args);
            }

            Context.Decrement();
        }
    }

}
