using System;
using static JobSystemTest.Tests;

namespace JobSystemTest
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            //BasicSecuential();
            //BasicDispatch();
            //MultiContextSecuential();
            //MatrixInversionDispatchTest();
            StealingJobs();
            //FibonacciDispatchTest();

            Console.WriteLine("All JobSystem tests are finished, Press any key to continue.");
            //Console.ReadKey();
        }
    }
}
