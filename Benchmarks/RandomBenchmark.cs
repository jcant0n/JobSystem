using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Iced.Intel;
using JobSystemTest;
using System;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Threading;

namespace JobSystemTest
{
    //BenchmarkDotNet v0.14.0, Windows 11 (10.0.22631.4169/23H2/2023Update/SunValley3)
    //Intel Core i9-10980HK CPU 2.40GHz, 1 CPU, 16 logical and 8 physical cores
    //.NET SDK 8.0.304
    //  [Host]     : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2
    //  DefaultJob : .NET 8.0.8 (8.0.824.36612), X64 RyuJIT AVX2

    //| Method                          | Mean     | Error     | StdDev    | Allocated |
    //|-------------------------------- |---------:|----------:|----------:|----------:|
    //| SystemRandom_Next               | 6.078 ms | 0.0485 ms | 0.0430 ms |       3 B |
    //| XorShiftRandom_Next             | 2.053 ms | 0.0157 ms | 0.0147 ms |       2 B |
    //| SystemRandom_ThreadLocal_Next   | 5.297 ms | 0.0464 ms | 0.0411 ms |       3 B |
    //| XorShiftRandom_ThreadLocal_Next | 1.939 ms | 0.0031 ms | 0.0024 ms |       1 B |

    [MemoryDiagnoser]
    public class RandomBenchmark
    {
        private Random systemRandom;
        private XorShiftRandom xorShiftRandom;

        private const uint N = 1000000;

        private static readonly ThreadLocal<Random> threadLocalRandom = new ThreadLocal<Random>(() =>
        {
            return new Random(Environment.TickCount ^ Thread.CurrentThread.ManagedThreadId);
        });

        private static readonly ThreadLocal<XorShiftRandom> threadLocalXorShiftRandom = new ThreadLocal<XorShiftRandom>(() =>
        {
            return new XorShiftRandom((ulong)(Environment.TickCount ^ Thread.CurrentThread.ManagedThreadId));
        });

        [GlobalSetup]
        public void Setup()
        {
            systemRandom = new Random(42);
            xorShiftRandom = new XorShiftRandom(42);
        }

        [Benchmark]
        public uint SystemRandom_Next()
        {
            uint sum = 0;
            for (int i = 0; i < N; i++)
            {
                sum += (uint)systemRandom.Next();
            }
            return sum;
        }

        [Benchmark]
        public uint XorShiftRandom_Next()
        {
            uint sum = 0;
            for (int i = 0; i < N; i++)
            {
                sum += xorShiftRandom.Next();
            }
            return sum;
        }

        [Benchmark]
        public uint SystemRandom_ThreadLocal_Next()
        {
            uint sum = 0;
            var random = threadLocalRandom.Value;
            for (int i = 0; i < N; i++)
            {
                sum += (uint)random.Next();
            }
            return sum;
        }

        [Benchmark]
        public uint XorShiftRandom_ThreadLocal_Next()
        {
            uint sum = 0;
            var random = threadLocalXorShiftRandom.Value;
            for (int i = 0; i < N; i++)
            {
                sum += random.Next();
            }
            return sum;
        }
    }
}
