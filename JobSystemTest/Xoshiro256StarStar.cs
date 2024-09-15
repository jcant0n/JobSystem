using System.Runtime.CompilerServices;

namespace JobSystemTest
{
    public class Xoshiro256StarStar
    {
        private ulong state0;
        private ulong state1;
        private ulong state2;
        private ulong state3;

        public Xoshiro256StarStar(ulong seed)
        {
            if (seed == 0)
                seed = 0xdeadbeef;

            state0 = seed;
            state1 = SplitMix64(seed + 1);
            state2 = SplitMix64(seed + 2);
            state3 = SplitMix64(seed + 3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong SplitMix64(ulong x)
        {
            x += 0x9e3779b97f4a7c15;
            x = (x ^ (x >> 30)) * 0xbf58476d1ce4e5b9;
            x = (x ^ (x >> 27)) * 0x94d049bb133111eb;
            return x ^ (x >> 31);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong NextUInt64()
        {
            ulong result = RotateLeft(state1 * 5, 7) * 9;

            ulong t = state1 << 17;

            state2 ^= state0;
            state3 ^= state1;
            state1 ^= state2;
            state0 ^= state3;

            state2 ^= t;
            state3 = RotateLeft(state3, 45);

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint Next()
        {
            return (uint)(NextUInt64() >> 32);
        }

        public uint Next(uint maxValue)
        {
            return (uint)(NextUInt64() % (ulong)maxValue);
        }

        public uint Next(uint minValue, uint maxValue)
        {
            return minValue + Next(maxValue - minValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong RotateLeft(ulong x, int k)
        {
            return (x << k) | (x >> (64 - k));
        }
    }
}