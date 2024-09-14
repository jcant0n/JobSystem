using System;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace JobSystemTest
{
    // Include necessary namespaces and your JobSystem classes here
    // Ensure that all the classes like JobSystem, JobsContext, Job, JobArgs, and XorShiftRandom are in this namespace or properly referenced

    [MemoryDiagnoser]
    [HideColumns("Job", "Error", "StdDev", "Median", "RatioSD")]
    public class JobSystemBenchmark
    {
        private int[,] matrix1;
        private int[,] matrix2;
        private int[,] result;

        private JobSystem jobSystem;
        private int matrixSize = 1000; // Adjust the size based on your system's capabilities
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

            uint jobCount = (uint)matrixSize; // One job per row
            uint groupSize = 100; // Adjust group size as needed

            // Use JobSystem.Dispatch to distribute the work across jobs
            jobSystem.Dispatch(context, jobCount, groupSize, (args) =>
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

        private void MultiplyMatrix(int[,] mat1, int[,] mat2, int[,] result, int startRow, int endRow)
        {
            for (int i = startRow; i < endRow; i++)
            {
                for (int j = 0; j < mat2.GetLength(1); j++)
                {
                    int sum = 0;
                    for (int k = 0; k < mat1.GetLength(1); k++)
                    {
                        sum += mat1[i, k] * mat2[k, j];
                    }
                    result[i, j] = sum;
                }
            }
        }
    }
}