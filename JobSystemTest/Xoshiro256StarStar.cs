using System.Runtime.CompilerServices;

namespace JobSystemTest
{
    /// <summary>
    /// Implements the Xoshiro256** pseudo-random number generator.
    /// </summary>
    public class Xoshiro256StarStar
    {
        private ulong state0;
        private ulong state1;
        private ulong state2;
        private ulong state3;

        /// <summary>
        /// Initializes a new instance of the Xoshiro256StarStar class with the specified seed.
        /// </summary>
        /// <param name="seed">The seed for the random number generator.</param>
        public Xoshiro256StarStar(ulong seed)
        {
            if (seed == 0)
                seed = 0xdeadbeef;

            state0 = seed;
            state1 = SplitMix64(seed + 1);
            state2 = SplitMix64(seed + 2);
            state3 = SplitMix64(seed + 3);
        }

        /// <summary>
        /// Implements the SplitMix64 algorithm, used for initializing the generator's state.
        /// </summary>
        /// <param name="x">The input value to be transformed.</param>
        /// <returns>A 64-bit unsigned integer derived from the input.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong SplitMix64(ulong x)
        {
            x += 0x9e3779b97f4a7c15;
            x = (x ^ (x >> 30)) * 0xbf58476d1ce4e5b9;
            x = (x ^ (x >> 27)) * 0x94d049bb133111eb;
            return x ^ (x >> 31);
        }

        /// <summary>
        /// Performs a left rotation on a 64-bit unsigned integer.
        /// </summary>
        /// <param name="x">The value to rotate.</param>
        /// <param name="k">The number of bits to rotate by.</param>
        /// <returns>The rotated value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong RotateLeft(ulong x, int k)
        {
            return (x << k) | (x >> (64 - k));
        }

        /// <summary>
        /// Generates the next 64-bit unsigned integer.
        /// </summary>
        /// <returns>A random 64-bit unsigned integer.</returns>
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

        /// <summary>
        /// Generates the next 32-bit unsigned integer.
        /// </summary>
        /// <returns>A random 32-bit unsigned integer.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint Next()
        {
            return (uint)(NextUInt64() >> 32);
        }

        /// <summary>
        /// Generates a random number between 0 (inclusive) and maxValue (exclusive).
        /// </summary>
        /// <param name="maxValue">The exclusive upper bound of the random number.</param>
        /// <returns>A random number between 0 (inclusive) and maxValue (exclusive).</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint Next(uint maxValue)
        {
            return (uint)(NextUInt64() % (ulong)maxValue);
        }

        /// <summary>
        /// Generates a random number between minValue (inclusive) and maxValue (exclusive).
        /// </summary>
        /// <param name="minValue">The inclusive lower bound of the random number.</param>
        /// <param name="maxValue">The exclusive upper bound of the random number.</param>
        /// <returns>A random number between minValue (inclusive) and maxValue (exclusive).</returns>
        public uint Next(uint minValue, uint maxValue)
        {
            return minValue + Next(maxValue - minValue);
        }
    }
}