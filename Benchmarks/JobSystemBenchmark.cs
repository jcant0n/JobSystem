using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Iced.Intel;

namespace JobSystemTest
{
    //BenchmarkDotNet v0.14.0, Windows 11 (10.0.22631.4037/23H2/2023Update/SunValley3)
    //13th Gen Intel Core i7-13700KF, 1 CPU, 24 logical and 16 physical cores
    //.NET SDK 8.0.202
    //  [Host]     : .NET 8.0.3 (8.0.324.11423), X64 RyuJIT AVX2
    //  Job-HCPCAV : .NET 8.0.3 (8.0.324.11423), X64 RyuJIT AVX2

    //Job = Job - HCPCAV  InvocationCount=1  UnrollFactor=1

    //| Method                              | Mean      | Ratio | Allocated | Alloc Ratio |
    //|------------------------------------ |----------:|------:|----------:|------------:|
    //| MultiplyMatrixSerially              | 122.60 ms |  1.00 |     400 B |        1.00 |
    //| MultiplyMatrixWithParallelFor       |  14.23 ms |  0.12 |    7248 B |       18.12 |
    //| MultiplyMatrixWithJobSystemDispatch |  11.15 ms |  0.09 |     760 B |        1.90 |

    [MemoryDiagnoser]
    [HideColumns("Job", "Error", "StdDev", "Median", "RatioSD")]
    public class JobSystemBenchmark
    {
        private int[,] matrix1;
        private int[,] matrix2;
        private int[,] result;

        private JobSystem jobSystem;
        private int matrixSize = 500;
        private ParallelOptions parallelOptions;

        [GlobalSetup]
        public void Setup()
        {
            // Initialize the matrices
            matrix1 = InitializeMatrix(matrixSize, matrixSize);
            matrix2 = InitializeMatrix(matrixSize, matrixSize);
            result = new int[matrixSize, matrixSize];

            // Initialize your JobSystem
            jobSystem = new JobSystem((uint)Environment.ProcessorCount);

            // ParallelOptions for Parallel.For
            parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            // Dispose of the JobSystem
            jobSystem.Dispose();
        }

        [IterationSetup]
        public void IterationSetup()
        {
            // Clear the result matrix before each iteration
            Array.Clear(result, 0, result.Length);
        }

        [Benchmark(Baseline = true)]
        public void MultiplyMatrixSerially()
        {
            // Multiply matrices on the main thread without parallelism
            for (int i = 0; i < matrixSize; i++)
            {
                for (int j = 0; j < matrixSize; j++)
                {
                    int sum = 0;
                    for (int k = 0; k < matrixSize; k++)
                    {
                        sum += matrix1[i, k] * matrix2[k, j];
                    }
                    result[i, j] = sum;
                }
            }
        }

        [Benchmark]
        public void MultiplyMatrixWithParallelFor()
        {
            // Multiply matrices using Parallel.For
            Parallel.For(0, matrixSize, parallelOptions, i =>
            {
                for (int j = 0; j < matrixSize; j++)
                {
                    int sum = 0;
                    for (int k = 0; k < matrixSize; k++)
                    {
                        sum += matrix1[i, k] * matrix2[k, j];
                    }
                    result[i, j] = sum;
                }
            });
        }

        [Benchmark]
        public void MultiplyMatrixWithJobSystemDispatch()
        {
            // Create a new context for this benchmark
            var context = new JobsContext();

            // Use JobSystem.Dispatch to distribute the work across jobs
            jobSystem.Dispatch(context, (uint)matrixSize, 7, (args) =>
            {
                int i = (int)args.JobIndex;
                for (int j = 0; j < matrixSize; j++)
                {
                    int sum = 0;
                    for (int k = 0; k < matrixSize; k++)
                    {
                        sum += matrix1[i, k] * matrix2[k, j];
                    }
                    result[i, j] = sum;
                }
            });

            // Wait for all jobs to complete
            jobSystem.Wait(context);
        }

        private int[,] InitializeMatrix(int rows, int cols)
        {
            Random random = new Random(42);
            int[,] mat = new int[rows, cols];
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    mat[i, j] = random.Next(0, 10);
                }
            }
            return mat;
        }
    }
}