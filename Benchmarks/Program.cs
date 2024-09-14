using BenchmarkDotNet.Running;

namespace JobSystemTest
{
    class Program
    {
        static void Main(string[] args)
        {
            //var summary = BenchmarkRunner.Run<RandomBenchmark>();
            var summary = BenchmarkRunner.Run<JobSystemBenchmark>();
        }
    }
}
