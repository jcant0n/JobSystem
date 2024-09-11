using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobSystemTest
{
    public class Tests
    {
        public static Stopwatch sw = new Stopwatch();

        public static void BasicSecuential()
        {
            sw.Reset();

            JobSystem jobSystem = new JobSystem(3);
            var context = new JobSystem.Context();

            Console.WriteLine("[BasicSecuential test]");

            sw.Start();
            uint count = 1000;
            jobSystem.Dispatch(context, count, 100, (args) =>
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
            Console.WriteLine($"[BasicSecuential test] - Time: {sw.ElapsedMilliseconds}");
            jobSystem.Dispose();
        }

        public static void BasicDispatch()
        {
            sw.Reset();

            JobSystem jobSystem = new JobSystem(3);
            var context = new JobSystem.Context();

            Console.WriteLine("JBasicDispatch test.");

            sw.Start();
            uint count = 1000;
            jobSystem.Dispatch(context, count, 100, (args) =>
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

            Console.WriteLine($"JBasicDispatch test - Time: {sw.ElapsedMilliseconds}");
            jobSystem.Dispose();
        }
    }
}
