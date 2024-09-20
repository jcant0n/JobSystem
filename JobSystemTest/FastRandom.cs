// Copyright © Plain Concepts S.L.U. All rights reserved. Use is subject to license terms.

using System;

namespace JobSystemTest
{
    /// <summary>
    /// Random number generator service (based on XOR shift technique).
    /// </summary>
    /// <remarks>
    /// Based on FastRandom.cs from colgreen:
    /// http://www.codeproject.com/Articles/9187/A-fast-equivalent-for-System-Random.
    /// </remarks>
    public sealed class FastRandom
    {
        /// <summary>
        /// RealUnitInt constant.
        /// </summary>
        private const double RealUnitInt = 1.0 / ((double)int.MaxValue + 1.0);

        /// <summary>
        /// RealUnitUInt constant.
        /// </summary>
        private const double RealUnitUInt = 1.0 / ((double)uint.MaxValue + 1.0);

        /// <summary>
        /// Y constant.
        /// </summary>
        private const uint Y = 842502087;

        /// <summary>
        /// Z constant.
        /// </summary>
        private const uint Z = 3579807591;

        /// <summary>
        /// W constant.
        /// </summary>
        private const uint W = 273326509;

        /// <summary>
        /// x member.
        /// </summary>
        private uint x;

        /// <summary>
        /// y member.
        /// </summary>
        private uint y;

        /// <summary>
        /// z member.
        /// </summary>
        private uint z;

        /// <summary>
        /// w member.
        /// </summary>
        private uint w;

        /// <summary>
        /// Buffer 32 bits in bitBuffer, return 1 at a time, keep track of how many
        /// have been returned with bitMask.
        /// </summary>
        private uint bitBuffer;

        /// <summary>
        /// Number of bits that have been returned.
        /// </summary>
        private uint bitMask = 1;

        /// <summary>
        /// Current random Seed field.
        /// </summary>
        private int seed;

        /// <summary>
        /// Gets or sets a value indicating whether the Seed.
        /// </summary>
        public int Seed
        {
            get
            {
                return this.seed;
            }

            set
            {
                this.seed = value;
                this.Reinitialise(this.seed);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FastRandom"/> class.
        /// </summary>
        /// <remarks>
        /// The initial seed depends on the time.
        /// </remarks>
        public FastRandom()
            : this(Environment.TickCount)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FastRandom"/> class with a given seed.
        /// </summary>
        /// <param name="seed">The initial seed.</param>
        public FastRandom(int seed)
        {
            this.seed = seed;
            this.Reinitialise(this.seed);
        }

        /// <summary>
        /// Reinitializes this instance with the specified seed.
        /// </summary>
        /// <param name="seed">The initial seed.</param>
        internal void Reinitialise(int seed)
        {
            // The only stipulation stated for the xorshift RNG is that at least one of
            // the seeds x,y,z,w is non-zero. We fulfill that requirement by only allowing
            // resetting of the x seed
            this.x = (uint)seed;
            this.y = Y;
            this.z = Z;
            this.w = W;

            this.bitBuffer = 0;
            this.bitMask = 1;
        }

        /// <summary>
        /// Generates a random int over the range 0 to int.MaxValue-1.
        /// </summary>
        /// <remarks>
        /// MaxValue is not generated in order to remain functionally equivalent to System.Random.Next().
        /// This does slightly eat into some of the performance gain over System.Random, but not much.
        /// For better performance see:
        /// Call NextInt() for an int over the range 0 to int.MaxValue.
        /// Call NextUInt() and cast the result to an int to generate an int over the full Int32 value range
        /// including negative values.
        /// </remarks>
        /// <returns>A 32-bit signed integer greater than or equal to zero and less than <see cref="int.MaxValue"/>.</returns>
        public int Next()
        {
            uint t = this.x ^ (this.x << 11);
            this.x = this.y;
            this.y = this.z;
            this.z = this.w;
            this.w = (this.w ^ (this.w >> 19)) ^ (t ^ (t >> 8));

            // Handle the special case where the value int.MaxValue is generated. This is outside of
            // the range of permitted values, so we therefore call Next() to try again.
            uint rtn = this.w & 0x7FFFFFFF;
            if (rtn == 0x7FFFFFFF)
            {
                return this.Next();
            }

            return (int)rtn;
        }

        /// <summary>
        /// Generates a random int over the range 0 to upperBound-1, and not including upperBound.
        /// </summary>
        /// <param name="upperBound">Non inclusive upper bound.</param>
        /// <returns>A 32-bit signed integer greater than or equal to zero and less than <c>upperBound</c>.</returns>
        public int Next(int upperBound)
        {
            if (upperBound < 0)
            {
                throw new ArgumentOutOfRangeException("upperBound", "upperBound must be >=0");
            }

            uint t = this.x ^ (this.x << 11);
            this.x = this.y;
            this.y = this.z;
            this.z = this.w;

            // The explicit int cast before the first multiplication gives better performance.
            // See comments in NextDouble.
            return (int)((RealUnitInt * (int)(0x7FFFFFFF & (this.w = (this.w ^ (this.w >> 19)) ^ (t ^ (t >> 8))))) * upperBound);
        }

        /// <summary>
        /// Generates a random int over the range lowerBound to upperBound-1, and not including upperBound.
        /// upperBound must be >= lowerBound. lowerBound may be negative.
        /// </summary>
        /// <param name="lowerBound">The inclusive lower bound.</param>
        /// <param name="upperBound">The non inclusive upper bound.</param>
        /// <returns>A 32-bit signed integer greater than or equal to <c>lowerBound</c> and less than <c>upperBound</c>.</returns>
        public int Next(int lowerBound, int upperBound)
        {
            if (lowerBound > upperBound)
            {
                throw new ArgumentOutOfRangeException("upperBound", "upperBound must be >=lowerBound");
            }

            uint t = this.x ^ (this.x << 11);
            this.x = this.y;
            this.y = this.z;
            this.z = this.w;

            // The explicit int cast before the first multiplication gives better performance.
            // See comments in NextDouble.
            int range = upperBound - lowerBound;
            if (range < 0)
            {
                // If range is <0 then an overflow has occurred and must resort to using long integer arithmetic instead (slower).
                // We also must use all 32 bits of precision, instead of the normal 31, which again is slower.
                return lowerBound + (int)((RealUnitUInt * (double)(this.w = (this.w ^ (this.w >> 19)) ^ (t ^ (t >> 8)))) * (double)((long)upperBound - (long)lowerBound));
            }

            // 31 bits of precision will suffice if range<=int.MaxValue. This allows us to cast to an int and gain
            // a little more performance.
            return lowerBound + (int)((RealUnitInt * (double)(int)(0x7FFFFFFF & (this.w = (this.w ^ (this.w >> 19)) ^ (t ^ (t >> 8))))) * (double)range);
        }

        /// <summary>
        /// Generates a random double between 0.0 and 1.0, not including 1.0.
        /// </summary>
        /// <returns>A double-precision floating point number greater than or equal to 0.0, and less than 1.0.</returns>
        public double NextDouble()
        {
            uint t = this.x ^ (this.x << 11);
            this.x = this.y;
            this.y = this.z;
            this.z = this.w;

            // Here we can gain a 2x speed improvement by generating a value that can be cast to
            // an int instead of the more easily available uint. If we then explicitly cast to an
            // int the compiler will then cast the int to a double to perform the multiplication,
            // this final cast is a lot faster than casting from a uint to a double. The extra cast
            // to an int is very fast (the allocated bits remain the same) and so the overall effect
            // of the extra cast is a significant performance improvement.
            //
            // Also note that the loss of one bit of precision is equivalent to what occurs within
            // System.Random.
            return RealUnitInt * (int)(0x7FFFFFFF & (this.w = (this.w ^ (this.w >> 19)) ^ (t ^ (t >> 8))));
        }

        /// <summary>
        /// Fills the provided byte array with random bytes.
        /// </summary>
        /// <remarks>
        /// This method is functionally equivalent to System.Random.NextBytes().
        /// </remarks>
        /// <param name="buffer">An array of bytes to contain random numbers. </param>
        public void NextBytes(byte[] buffer)
        {
            // Fill up the bulk of the buffer in chunks of 4 bytes at a time.
            uint x = this.x, y = this.y, z = this.z, w = this.w;
            int i = 0;
            uint t;
            for (int bound = buffer.Length - 3; i < bound;)
            {
                // Generate 4 bytes.
                // Increased performance is achieved by generating 4 random bytes per loop.
                // Also note that no mask needs to be applied to zero out the higher order bytes before
                // casting because the cast ignores this bytes. Thanks to Stefan Troschütz for pointing this out.
                t = x ^ (x << 11);
                x = y;
                y = z;
                z = w;
                w = (w ^ (w >> 19)) ^ (t ^ (t >> 8));

                buffer[i++] = (byte)w;
                buffer[i++] = (byte)(w >> 8);
                buffer[i++] = (byte)(w >> 16);
                buffer[i++] = (byte)(w >> 24);
            }

            // Fill up any remaining bytes in the buffer.
            if (i < buffer.Length)
            {
                // Generate 4 bytes.
                t = x ^ (x << 11);
                x = y;
                y = z;
                z = w;
                w = (w ^ (w >> 19)) ^ (t ^ (t >> 8));

                buffer[i++] = (byte)w;
                if (i < buffer.Length)
                {
                    buffer[i++] = (byte)(w >> 8);
                    if (i < buffer.Length)
                    {
                        buffer[i++] = (byte)(w >> 16);
                        if (i < buffer.Length)
                        {
                            buffer[i] = (byte)(w >> 24);
                        }
                    }
                }
            }

            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        /// <summary>
        /// Generates a uint. Values returned are over the full range of a uint,
        /// uint.MinValue to uint.MaxValue, inclusive.
        /// </summary>
        /// <remarks>
        /// This is the fastest method for generating a single random number because the underlying
        /// random number generator algorithm generates 32 random bits that can be cast directly to
        /// a uint.
        /// </remarks>
        /// <returns>A 32-bit signed integer greater than or equal to zero and less or equal than <see cref="uint.MaxValue"/>.</returns>
        public uint NextUInt()
        {
            uint t = this.x ^ (this.x << 11);
            this.x = this.y;
            this.y = this.z;
            this.z = this.w;
            return this.w = (this.w ^ (this.w >> 19)) ^ (t ^ (t >> 8));
        }

        /// <summary>
        /// Generates a random int over the range 0 to int.MaxValue, both inclusive.
        /// </summary>
        /// <remarks>
        /// This method differs from Next() only in that the range is 0 to int.MaxValue
        /// and not 0 to int.MaxValue-1.
        /// The slight difference in range means this method is slightly faster than Next()
        /// but is not functionally equivalent to System.Random.Next().
        /// </remarks>
        /// <returns>A 32-bit signed integer greater than or equal to zero and less or equal than <see cref="int.MaxValue"/>.</returns>
        public int NextInt()
        {
            uint t = this.x ^ (this.x << 11);
            this.x = this.y;
            this.y = this.z;
            this.z = this.w;
            return (int)(0x7FFFFFFF & (this.w = (this.w ^ (this.w >> 19)) ^ (t ^ (t >> 8))));
        }
    }
}
