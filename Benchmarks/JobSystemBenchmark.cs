using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;

namespace JobSystemTest
{
    [MemoryDiagnoser]
    public class JobSystemBenchmark
    {
        private JobSystem jobSystem;
        private JobsContext context;

        [Params(1000, 10000, 100000)]
        public int JobCount;

        [GlobalSetup]
        public void Setup()
        {
            jobSystem = new JobSystem();
            context = new JobsContext();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            jobSystem.Dispose();
            context.Signal.Dispose();
        }

        [Benchmark]
        public void JobSystemDispatch()
        {
            jobSystem.Dispatch(context, (uint)JobCount, 1, WorkFunction);
            jobSystem.Wait(context);
        }

        [Benchmark]
        public void ParallelFor()
        {
            Parallel.For(0, JobCount, i =>
            {
                WorkFunction(new JobArgs((uint)i, 0, 0));
            });
        }

        private void WorkFunction(JobArgs args)
        {
            double result = Math.Sqrt(args.JobIndex);
        }
    }
}
