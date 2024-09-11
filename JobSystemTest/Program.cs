using System;

namespace JobSystemTest
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            JobSystem jobSystem = new JobSystem(3);
            var context = new JobSystem.Context();

            //Console.WriteLine("JobSystem execute test.");

            //for (int i = 0; i < 10; i++)
            //{
            //    jobSystem.Execute(context, (args) =>
            //    {
            //        var counter = context.PendingJobs;
            //        Console.WriteLine($"Pending Jobs {counter}, Thread {Thread.CurrentThread.ManagedThreadId}");
            //        Thread.Sleep(1000); // Simulate workload
            //    });
            //}

            //jobSystem.Wait(context);

            Console.WriteLine("JobSystem dispatch test.");
            uint count = 1000;
            jobSystem.Dispatch(context, count, 100, (args) =>
            {
                if(args.JobIndex % 100 == 0)
                {
                    var counter = context.PendingJobs;
                    Console.WriteLine($"Pending Jobs {counter}, Thread {Thread.CurrentThread.ManagedThreadId}");
                }
                Thread.Sleep(1); // Simulate workload
            });
            jobSystem.Wait(context);

            jobSystem.Dispose();
            
            Console.WriteLine("JobSystem has been stopped, Press any key to continue.");
            Console.ReadKey();
        }
    }
}
