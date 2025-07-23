using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace NativeArrayTest
{
    /// <summary>
    /// High-performance, thread-safe collection of unmanaged items that minimizes GC pressure
    /// and supports automatic resizing.
    /// </summary>
    public unsafe class NativeArray<T> : IDisposable where T : unmanaged
    {
        // Pointer to unmanaged memory
        private T* buffer;
        // Current capacity of the buffer
        private int capacity;
        // Current count of items (atomic for thread safety)
        private int count;

        /// <summary>
        /// Number of items currently stored.
        /// </summary>
        public int Count => Volatile.Read(ref count);

        /// <summary>
        /// Creates a new NativeArray with the given initial capacity.
        /// </summary>
        /// <param name="initialCapacity">Initial capacity of the collection</param>
        public NativeArray(int initialCapacity = 1024)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Capacity must be greater than zero.");

            capacity = initialCapacity;
            count = 0;

            // Allocate unmanaged memory for T elements
            buffer = (T*)NativeMemory.Alloc((nuint)(capacity * sizeof(T)));
        }

        /// <summary>
        /// Adds an item in a thread-safe manner, resizing if needed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Add(T item)
        {
            int index = Interlocked.Increment(ref count) - 1;
            if ((uint)index >= (uint)capacity)
            {
                lock (this)
                {
                    if ((uint)index >= (uint)capacity)
                    {
                        Resize(capacity * 2);
                    }
                }
            }

            buffer[index] = item;
            return index;
        }

        /// <summary>
        /// Provides direct indexed access to the stored item.
        /// </summary>
        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if ((uint)index >= (uint)Volatile.Read(ref count))
                    throw new IndexOutOfRangeException();
                return ref buffer[index];
            }
        }

        /// <summary>
        /// Clears the collection by resetting the count to zero (no memory deallocation).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            Volatile.Write(ref count, 0);
        }

        /// <summary>
        /// Resizes the underlying unmanaged memory buffer.
        /// </summary>
        private void Resize(int newCapacity)
        {
            if (newCapacity <= capacity)
                return;

            T* oldBuffer = buffer;
            int oldCapacity = capacity;

            // Allocate new block
            T* newBuffer = (T*)NativeMemory.Alloc((nuint)(newCapacity * sizeof(T)));
            // Copy existing items
            Unsafe.CopyBlock(newBuffer, oldBuffer, (uint)(oldCapacity * sizeof(T)));
            // Free old block
            NativeMemory.Free(oldBuffer);

            buffer = newBuffer;
            capacity = newCapacity;
        }

        /// <summary>
        /// Frees the unmanaged memory.
        /// </summary>
        public void Dispose()
        {
            if (buffer != null)
            {
                NativeMemory.Free(buffer);
                buffer = null;
            }
            capacity = 0;
            count = 0;
        }
    }
}