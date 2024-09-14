namespace JobSystemTest
{
    public class XorShiftRandom
    {
        private ulong state;

        public XorShiftRandom(ulong seed)
        {
            if (seed == 0)
                seed = 0xdeadbeef;

            state = seed;
        }

        public ulong NextUInt64()
        {
            ulong x = state;
            x ^= x << 13;
            x ^= x >> 7;
            x ^= x << 17;
            state = x;
            return x;
        }

        public uint Next()
        {
            return (uint)(NextUInt64() & 0x7FFFFFFF);
        }

        public uint Next(uint maxValue)
        {
            return (uint)(NextUInt64() % (uint)maxValue);
        }

        public uint Next(uint minValue, uint maxValue)
        {
            return minValue + Next(maxValue - minValue);
        }
    }

}
