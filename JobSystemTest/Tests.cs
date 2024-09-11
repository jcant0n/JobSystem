using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using static JobSystemTest.JobSystem;

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
            uint count = 1000;

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

        public static void MultiContextSecuential()
        {
            sw.Reset();

            JobSystem jobSystem = new JobSystem(2);
            var ctx1 = new Context();

            Console.WriteLine("[MultiContextSecuential] test]");

            sw.Start();
            uint count = 1000;

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
    }
}
