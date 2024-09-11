using System;

namespace JobSystemTest
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            //Tests.BasicSecuential();
            //Tests.BasicDispatch();
            //Tests.MultiContextSecuential();
            Tests.MatrixInversionDispatchTest();

            Console.WriteLine("All JobSystem tests are finished, Press any key to continue.");
            //Console.ReadKey();
        }
    }
}
