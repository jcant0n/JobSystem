using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using static JobSystemTest.JobSystem;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace JobSystemTest
{
    public class Tests
    {
        public static Stopwatch sw = new Stopwatch();

        public static void BasicSecuential()
        {
            sw.Reset();

            JobSystem jobSystem = new JobSystem(2);
            var context = new Context();

            Console.WriteLine("[BasicSecuential test]");

            sw.Start();

            jobSystem.Execute(context, (args) =>
            {
                var counter = context.PendingJobs;
                Console.WriteLine($"Pending Jobs {counter}, Thread {Thread.CurrentThread.ManagedThreadId}");
                Thread.Sleep(1); // Simulate workload
            });

            jobSystem.Execute(context, (args) =>
            {
                var counter = context.PendingJobs;
                Console.WriteLine($"Pending Jobs {counter}, Thread {Thread.CurrentThread.ManagedThreadId}");
                Thread.Sleep(1); // Simulate workload
            });

            jobSystem.Execute(context, (args) =>
            {
                var counter = context.PendingJobs;
                Console.WriteLine($"Pending Jobs {counter}, Thread {Thread.CurrentThread.ManagedThreadId}");
                Thread.Sleep(1); // Simulate workload
            });

            jobSystem.Execute(context, (args) =>
            {
                var counter = context.PendingJobs;
                Console.WriteLine($"Pending Jobs {counter}, Thread {Thread.CurrentThread.ManagedThreadId}");
                Thread.Sleep(1); // Simulate workload
            });

            jobSystem.Wait(context);
            sw.Stop();
            Console.WriteLine($"[BasicSecuential test] - Time: {sw.ElapsedMilliseconds}");
            jobSystem.Dispose();
        }

        public static void StealingJobs()
        {
            sw.Reset();

            // Initialize data
            int[,] matrix1 = InitializeMatrix(1000, 1000);
            int[,] matrix2 = InitializeMatrix(1000, 1000);
            int[,] result = new int[1000, 1000];

            int count = 15;
            int[] numbers = new int[count];
            long[] results = new long[count];
            Random random = new Random();

            for (int i = 0; i < count; i++)
            {
                numbers[i] = random.Next(35, 40);
            }

            JobSystem jobSystem = new JobSystem((uint)Environment.ProcessorCount);
            var context = new Context();

            Console.WriteLine("[StealingJobs test]");

            sw.Start();

            jobSystem.Execute(context, (args) =>
            {
                MultiplyMatrix(matrix1, matrix2, result, 0, 1000);
                Console.WriteLine($"Pending Jobs {context.PendingJobs}, Thread {Thread.CurrentThread.ManagedThreadId}");
            });

            for (int i = 0; i < 48; i++)
            {
                jobSystem.Execute(context, (args) =>
                {
                    int size = random.Next(8, 15);
                    for (int index = 0; index < size; index++)
                    {
                        int number = numbers[index];
                        results[index] = Fibonacci(number);

                    }
                    Console.WriteLine($"Pending Jobs {context.PendingJobs}, Thread {Thread.CurrentThread.ManagedThreadId}");
                });
            }

            jobSystem.Wait(context);
            sw.Stop();
            Console.WriteLine($"[StealingJobs test] - Time: {sw.ElapsedMilliseconds}");
            jobSystem.Dispose();
        }

        public static void MultiContextSecuential()
        {
            sw.Reset();

            JobSystem jobSystem = new JobSystem(2);
            var ctx1 = new Context();

            Console.WriteLine("[MultiContextSecuential] test]");

            sw.Start();

            jobSystem.Execute(ctx1, (args) =>
            {
                var counter = ctx1.PendingJobs;
                Console.WriteLine($"Pending Jobs {counter}, Thread {Thread.CurrentThread.ManagedThreadId}");
                Thread.Sleep(1); // Simulate workload
            });

            jobSystem.Execute(ctx1, (args) =>
            {
                var counter = ctx1.PendingJobs;
                Console.WriteLine($"Pending Jobs {counter}, Thread {Thread.CurrentThread.ManagedThreadId}");
                Thread.Sleep(1); // Simulate workload
            });

            var ctx2 = new Context();
            jobSystem.Execute(ctx2, (args) =>
            {
                var counter = ctx2.PendingJobs;
                Console.WriteLine($"Pending Jobs {counter}, Thread {Thread.CurrentThread.ManagedThreadId}");
                Thread.Sleep(1); // Simulate workload
            });

            jobSystem.Execute(ctx2, (args) =>
            {
                var counter = ctx2.PendingJobs;
                Console.WriteLine($"Pending Jobs {counter}, Thread {Thread.CurrentThread.ManagedThreadId}");
                Thread.Sleep(1); // Simulate workload
            });

            jobSystem.Wait(ctx1);
            Console.WriteLine($"Context 1 has finished - Time: {sw.ElapsedMilliseconds}");

            jobSystem.Wait(ctx2);
            Console.WriteLine($"Context 2 has finished - Time: {sw.ElapsedMilliseconds}");
            sw.Stop();
            Console.WriteLine($"[MultiContextSecuential test] - Time: {sw.ElapsedMilliseconds}");
            jobSystem.Dispose();
        }

        public static void BasicDispatch()
        {
            sw.Reset();

            JobSystem jobSystem = new JobSystem(3);
            var context = new Context();

            Console.WriteLine("[BasicDispatch] test.");

            sw.Start();
            uint count = 1000;
            uint groupSize = 100;
            jobSystem.Dispatch(context, count, groupSize, (args) =>
            {
                if (args.JobIndex % 100 == 0)
                {
                    var counter = context.PendingJobs;
                    Console.WriteLine($"Pending Jobs {counter}, Thread {Thread.CurrentThread.ManagedThreadId}");
                }
                Thread.Sleep(1); // Simulate workload
            });

            jobSystem.Wait(context);
            sw.Stop();

            Console.WriteLine($"[BasicDispatch] test - Time: {sw.ElapsedMilliseconds}");
            jobSystem.Dispose();
        }

        public static void MatrixInversionDispatchTest()
        {
            sw.Reset();
            JobSystem jobSystem = new JobSystem((uint)Environment.ProcessorCount);
            var ctx = new Context();

            Console.WriteLine("[MatrixInversionDispatchTest] test.");

            Random random = new Random();
            int matrixCount = 100000000;
            var matrices = new Matrix4x4[matrixCount];

            for (int i = 0; i < matrixCount; i++)
            {
                matrices[i] = new Matrix4x4(
                    (float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble(),
                    (float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble(),
                    (float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble(),
                    (float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble()
                );
            }

            sw.Start();
            jobSystem.Dispatch(ctx, (uint)matrices.Length, 100000, (args) =>
            {
                var matrix = matrices[args.JobIndex];
                Matrix4x4.Invert(matrix, out var invertedMatrix);
                var result = matrix * (matrix * invertedMatrix) * (matrix * matrix) * invertedMatrix;
                matrices[args.JobIndex] = result;
            });

            jobSystem.Wait(ctx);
            sw.Stop();
            Console.WriteLine($"[MatrixInversionDispatchTest] test - Time: {sw.ElapsedMilliseconds}");
            jobSystem.Dispose();
        }

        public static void FibonacciDispatchTest()
        {
            sw.Reset();

            JobSystem jobSystem = new JobSystem(4);
            var ctx = new Context();

            Console.WriteLine("[FibonacciDispatchTest] test.");

            int count = 100;
            int[] numbers = new int[count];
            long[] results = new long[count];
            Random random = new Random();

            for (int i = 0; i < count; i++)
            {
                numbers[i] = random.Next(35, 40);
            }

            sw.Start();
            uint groupSize = 5;
            jobSystem.Dispatch(ctx, (uint)count, groupSize, (args) =>
            {
                int index = (int)args.JobIndex;
                int number = numbers[index];

                long result = Fibonacci(number);
                results[index] = result;
            });

            jobSystem.Wait(ctx);
            sw.Stop();
            Console.WriteLine($"[MatrixInversionDispatchTest] test - Time: {sw.ElapsedMilliseconds}");
            jobSystem.Dispose();
        }

        static int[,] InitializeMatrix(int rows, int cols)
        {
            Random random = new Random();
            int[,] matrix = new int[rows, cols];
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    matrix[i, j] = random.Next(1, 10);
                }
            }
            return matrix;
        }
        static void MultiplyMatrix(int[,] matrix1, int[,] matrix2, int[,] resultMatrix, int startRow, int endRow)
        {
            int rows = matrix1.GetLength(0);
            int cols = matrix2.GetLength(1);

            for (int i = startRow; i < endRow; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    for (int k = 0; k < rows; k++)
                    {
                        resultMatrix[i, j] += matrix1[i, k] * matrix2[k, j];
                    }
                }
            }
        }

        static long Fibonacci(int n)
        {
            if (n <= 1) return n;
            return Fibonacci(n - 1) + Fibonacci(n - 2);
        }
    }
}
