using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using NativeArrayTest;  // Aquí vive UnmanagedBatch<T>

namespace JobSystemTest
{
    [MemoryDiagnoser]
    public class TwoWayBenchmark
    {
        [Params(100_000, 1_000_000)]
        public int TotalItems;

        private int taskCount = Environment.ProcessorCount;

        private TemporalArray<int> unmanagedBatch;
        private ConcurrentBag<int> concurrentBag;

        //——— INSERTION SETUPS ———

        [IterationSetup(Target = nameof(Insert_UnmanagedBatch))]
        public void SetupInsertUnmanaged()
        {
            // pre‑allocate all chunks para evitar resize-on-the-fly
            unmanagedBatch = new TemporalArray<int>(TotalItems);
        }

        [IterationSetup(Target = nameof(Insert_ConcurrentBag))]
        public void SetupInsertBag()
        {
            concurrentBag = new ConcurrentBag<int>();
        }

        //——— INSERTION BENCHMARKS ———

        [Benchmark(Description = "Insert into UnmanagedBatch")]
        public void Insert_UnmanagedBatch()
        {
            int chunk = TotalItems / taskCount;
            Parallel.For(0, taskCount, t =>
            {
                int start = t * chunk;
                int end = start + chunk;
                for (int i = start; i < end; i++)
                    unmanagedBatch.Add(i);
            });
        }

        [Benchmark(Description = "Insert into ConcurrentBag")]
        public void Insert_ConcurrentBag()
        {
            int chunk = TotalItems / taskCount;
            Parallel.For(0, taskCount, t =>
            {
                int start = t * chunk;
                int end = start + chunk;
                for (int i = start; i < end; i++)
                    concurrentBag.Add(i);
            });
        }

        //——— ACCESS SETUPS ———

        [GlobalSetup(Target = nameof(Access_UnmanagedBatch))]
        public void SetupAccessUnmanaged()
        {
            unmanagedBatch = new TemporalArray<int>(TotalItems);
            for (int i = 0; i < TotalItems; i++)
                unmanagedBatch.Add(i);
        }

        [GlobalSetup(Target = nameof(Access_ConcurrentBag))]
        public void SetupAccessBag()
        {
            concurrentBag = new ConcurrentBag<int>();
            for (int i = 0; i < TotalItems; i++)
                concurrentBag.Add(i);
        }

        //——— ACCESS BENCHMARKS ———

        [Benchmark(Description = "Access all elements in UnmanagedBatch")]
        public int Access_UnmanagedBatch()
        {
            int sum = 0;
            long count = unmanagedBatch.Count;
            for (long i = 0; i < count; i++)
                sum += unmanagedBatch[i];
            return sum;
        }

        [Benchmark(Description = "Access all elements in ConcurrentBag")]
        public int Access_ConcurrentBag()
        {
            int sum = 0;
            foreach (var x in concurrentBag)
                sum += x;
            return sum;
        }
    }
}
