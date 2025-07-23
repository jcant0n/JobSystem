using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace NativeArrayTest
{
    public unsafe class TemporalArray<T> : IDisposable where T : unmanaged
    {
        private const int ChunkSize = 1024;
        private const int ChunkBits = 10;              // 2^10 = 1024
        private const long ChunkMask = ChunkSize - 1;   // 0x3FF

        // List of native-memory chunks (as IntPtr)
        private readonly List<IntPtr> chunks;
        private long count;
        private readonly object lockObj = new();

        public TemporalArray(long capacity = ChunkSize)
        {
            int needed = (int)((capacity + ChunkSize - 1) / ChunkSize);
            chunks = new List<IntPtr>(needed);

            for (int i = 0; i < needed; i++)
            {
                void* ptr = NativeMemory.AllocZeroed((UIntPtr)(ChunkSize * (ulong)sizeof(T)));
                chunks.Add((IntPtr)ptr);
            }

            count = 0;
        }

        public long Count => Interlocked.Read(ref count);

        public void Clear()
        {
            Interlocked.Exchange(ref count, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long Add(T item)
        {
            // Atomic increment to get unique index
            long idx = Interlocked.Increment(ref count) - 1;

            // Calculate chunk position
            int chunkIndex = (int)(idx >> ChunkBits);
            int chunkOffset = (int)(idx & ChunkMask);

            // Fast path
            if (chunkIndex < chunks.Count)
            {
                void* basePtr = (void*)chunks[chunkIndex];
                ((T*)basePtr)[chunkOffset] = item;
                return idx;
            }

            // Slow path
            EnsureChunk(chunkIndex);
            void* ptr = (void*)chunks[chunkIndex];
            ((T*)ptr)[chunkOffset] = item;
            return idx;
        }

        public T this[long index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if ((ulong)index >= (ulong)count)
                    throw new IndexOutOfRangeException();

                int chunkIndex = (int)(index >> ChunkBits);
                int chunkOffset = (int)(index & ChunkMask);

                void* basePtr = (void*)chunks[chunkIndex];

                return ((T*)basePtr)[chunkOffset];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureChunk(int ci)
        {
            lock (lockObj)
            {
                if (ci >= chunks.Count)
                {
                    void* ptr = NativeMemory.AllocZeroed((UIntPtr)(ChunkSize * (ulong)sizeof(T)));
                    chunks.Add((IntPtr)ptr);
                }
            }
        }

        public void Dispose()
        {
            foreach (IntPtr ip in chunks)
            {
                if (ip != IntPtr.Zero)
                    NativeMemory.Free((void*)ip);
            }

            chunks.Clear();
            count = 0;
        }
    }
}
