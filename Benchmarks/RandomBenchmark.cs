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
    //BenchmarkDotNet v0.14.0, Windows 11 (10.0.22631.4037/23H2/2023Update/SunValley3)
    //13th Gen Intel Core i7-13700KF, 1 CPU, 24 logical and 16 physical cores
    //.NET SDK 8.0.202
    //  [Host]     : .NET 8.0.3 (8.0.324.11423), X64 RyuJIT AVX2
    //  DefaultJob : .NET 8.0.3 (8.0.324.11423), X64 RyuJIT AVX2

    //Job = DefaultJob

    //| Method | Mean | Allocated |
    //| ---------------------------------- | -----------:|----------:|
    //| SystemRandom_Next                 | 4,971.9 us |       3 B |
    //| XorShiftRandom_Next               | 1,174.8 us |       1 B |
    //| Xoshiro256Random_Next             |   853.9 us |         - |
    //| SystemRandom_ThreadLocal_Next     | 4,498.5 us |       2 B |
    //| XorShiftRandom_ThreadLocal_Next   | 1,212.3 us |       1 B |
    //| Xoshiro256Random_ThreadLocal_Next |   947.3 us |         - |

    [MemoryDiagnoser]
    [HideColumns("Job", "Error", "StdDev", "Median", "RatioSD")]
    public class RandomBenchmark
    {
        private Random systemRandom;
        private XorShiftRandom xorShiftRandom;
        private Xoshiro256StarStar Xoshiro256Random;
        private FastRandom fastRandom;

        private const uint N = 1000000;

        private static readonly ThreadLocal<Random> threadLocalRandom = new ThreadLocal<Random>(() =>
        {
            return new Random(Environment.TickCount ^ Thread.CurrentThread.ManagedThreadId);
        });

        private static readonly ThreadLocal<XorShiftRandom> threadLocalXorShiftRandom = new ThreadLocal<XorShiftRandom>(() =>
        {
            return new XorShiftRandom((ulong)(Environment.TickCount ^ Thread.CurrentThread.ManagedThreadId));
        });

        private static readonly ThreadLocal<Xoshiro256StarStar> threadLocalXoshiro256Random = new ThreadLocal<Xoshiro256StarStar>(() =>
        {
            return new Xoshiro256StarStar((ulong)(Environment.TickCount ^ Thread.CurrentThread.ManagedThreadId));
        });

        private static readonly ThreadLocal<FastRandom> threadLocalFastRandom = new ThreadLocal<FastRandom>(() =>
        {
            return new FastRandom(Environment.TickCount ^ Thread.CurrentThread.ManagedThreadId);
        });

        [GlobalSetup]
        public void Setup()
        {
            ulong seed = 42;
            systemRandom = new Random((int)seed);
            xorShiftRandom = new XorShiftRandom(seed);
            Xoshiro256Random = new Xoshiro256StarStar(seed);
            fastRandom = new FastRandom((int)seed);
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
        public uint Xoshiro256Random_Next()
        {
            uint sum = 0;
            for (int i = 0; i < N; i++)
            {
                sum += Xoshiro256Random.Next();
            }
            return sum;
        }

        [Benchmark]
        public uint FastRandom_Next()
        {
            uint sum = 0;
            for (int i = 0; i < N; i++)
            {
                sum += fastRandom.NextUInt();
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

        [Benchmark]
        public uint Xoshiro256Random_ThreadLocal_Next()
        {
            uint sum = 0;
            var random = threadLocalXoshiro256Random.Value;
            for (int i = 0; i < N; i++)
            {
                sum += random.Next();
            }
            return sum;
        }

        [Benchmark]
        public uint FastRandom_ThreadLocal_Next()
        {
            uint sum = 0;
            var random = threadLocalFastRandom.Value;
            for (int i = 0; i < N; i++)
            {
                sum += random.NextUInt();
            }
            return sum;
        }
    }
}
