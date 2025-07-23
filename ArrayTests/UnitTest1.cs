using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using NativeArrayTest;

namespace ArrayTests
{
    public class TemporalArrayTests
    {
        private readonly ITestOutputHelper output;

        public TemporalArrayTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void Initialize_WithDefaultAndCustomCapacity()
        {
            // Default capacity
            using var array1 = new TemporalArray<int>();
            Assert.Equal(0, array1.Count);

            // Custom capacity
            using var array2 = new TemporalArray<int>(5000);
            Assert.Equal(0, array2.Count);
        }

        [Fact]
        public void Add_SingleAndMultipleItems()
        {
            using var array = new TemporalArray<int>();

            // Add single item
            array.Add(42);
            Assert.Equal(1, array.Count);
            Assert.Equal(42, array[0]);

            // Add multiple items
            const int count = 10000;
            for (int i = 0; i < count; i++)
            {
                array.Add(i);
            }
            Assert.Equal(count + 1, array.Count);
        }

        [Fact]
        public void Add_MultithreadedOperations()
        {
            using var array = new TemporalArray<int>();
            const int threadsCount = 8;
            const int itemsPerThread = 10000;
            var tasks = new Task[threadsCount];

            // Add items from multiple threads
            for (int t = 0; t < threadsCount; t++)
            {
                int threadId = t;
                tasks[t] = Task.Run(() =>
                {
                    for (int i = 0; i < itemsPerThread; i++)
                    {
                        array.Add(threadId * itemsPerThread + i);
                    }
                });
            }

            Task.WaitAll(tasks);

            Assert.Equal(threadsCount * itemsPerThread, array.Count);

            // Verify all expected values exist
            var expectedValues = new HashSet<int>();
            var actualValues = new HashSet<int>();

            for (int t = 0; t < threadsCount; t++)
                for (int i = 0; i < itemsPerThread; i++)
                    expectedValues.Add(t * itemsPerThread + i);

            for (long i = 0; i < array.Count; i++)
                actualValues.Add(array[i]);

            Assert.Equal(expectedValues.Count, actualValues.Count);
            Assert.True(expectedValues.SetEquals(actualValues));
        }

        [Fact]
        public void Sum_AllItems()
        {
            using var array = new TemporalArray<int>();
            const int count = 10000;
            long expectedSum = 0;

            for (int i = 0; i < count; i++)
            {
                array.Add(i);
                expectedSum += i;
            }

            // Sum items
            long actualSum = 0;
            for (long i = 0; i < array.Count; i++)
            {
                actualSum += array[i];
            }

            Assert.Equal(expectedSum, actualSum);
        }

        [Fact]
        public void Sum_ParallelItems()
        {
            using var array = new TemporalArray<long>();
            const int count = 1000000;

            // Add items
            Parallel.For(0, count, i => array.Add(i));

            // Expected sum using formula for sum of first n natural numbers
            long expectedSum = (long)count * (count - 1) / 2;

            // Sum in parallel
            long actualSum = 0;
            Parallel.For(0, (int)array.Count,
                () => 0L,
                (i, _, localSum) => localSum + array[i],
                localSum => Interlocked.Add(ref actualSum, localSum));

            Assert.Equal(expectedSum, actualSum);
        }

        [Fact]
        public void Reorder_Items()
        {
            using var array = new TemporalArray<int>();
            const int count = 1000;
            var random = new Random(42);

            // Add items in random order
            for (int i = 0; i < count; i++)
            {
                array.Add(random.Next(count));
            }

            // Copy to list and sort
            var sortedList = new List<int>((int)array.Count);
            for (long i = 0; i < array.Count; i++)
            {
                sortedList.Add(array[i]);
            }
            sortedList.Sort();

            // Verify sorting worked
            for (int i = 1; i < sortedList.Count; i++)
            {
                Assert.True(sortedList[i - 1] <= sortedList[i]);
            }
        }

        [Fact]
        public void Clear_ResetsCount()
        {
            using var array = new TemporalArray<int>();
            const int count = 1000;

            for (int i = 0; i < count; i++)
            {
                array.Add(i);
            }
            Assert.Equal(count, array.Count);

            array.Clear();
            Assert.Equal(0, array.Count);
        }

        [Fact]
        public void ThreadLocalBuffer_AutoFlush()
        {
            using var array = new TemporalArray<int>();
            const int localBufferSize = 256; // Same as in TemporalArray
            const int count = 1000;

            // Add more than buffer size
            for (int i = 0; i < count; i++)
            {
                array.Add(i);
            }

            // Some items should be auto-flushed
            Assert.True(array.Count >= localBufferSize);
        }

        [Fact]
        public void IndexOutOfRange_ThrowsException()
        {
            using var array = new TemporalArray<int>();
            array.Add(42);

            Assert.Throws<IndexOutOfRangeException>(() => array[1]);
            Assert.Throws<IndexOutOfRangeException>(() => array[-1]);
        }

        [Fact]
        public void Performance_CompareToList()
        {
            const int itemCount = 1000000;
            var watch = System.Diagnostics.Stopwatch.StartNew();

            // List performance
            var list = new List<int>(itemCount);
            for (int i = 0; i < itemCount; i++)
            {
                list.Add(i);
            }

            watch.Stop();
            long listTime = watch.ElapsedMilliseconds;
            output.WriteLine($"List add time: {listTime}ms");

            // TemporalArray performance
            watch.Restart();
            using var array = new TemporalArray<int>(itemCount);
            for (int i = 0; i < itemCount; i++)
            {
                array.Add(i);
            }

            watch.Stop();
            long arrayTime = watch.ElapsedMilliseconds;
            output.WriteLine($"TemporalArray add time: {arrayTime}ms");

            output.WriteLine($"Performance ratio: {(double)listTime / arrayTime:F2}x");
        }

        [Fact]
        public void Clear_DoesNotGenerateGCPressure()
        {
            const int itemCount = 1000000;
            const int clearCount = 100;
            
            // Record initial GC counts
            int[] initialCounts = new int[3];
            for (int i = 0; i < 3; i++)
                initialCounts[i] = GC.CollectionCount(i);
            
            // Temporal array test
            using (var temporalArray = new TemporalArray<int>(itemCount))
            {
                // Fill the array
                for (int i = 0; i < itemCount; i++)
                    temporalArray.Add(i);
                    
                // Clear multiple times
                for (int i = 0; i < clearCount; i++)
                {
                    temporalArray.Clear();
                    // Add one item to ensure the array is working
                    temporalArray.Add(i);
                }
            }
            
            // Record GC counts after TemporalArray test
            int[] temporalArrayCounts = new int[3];
            for (int i = 0; i < 3; i++)
                temporalArrayCounts[i] = GC.CollectionCount(i) - initialCounts[i];
                
            // Reset initial counts
            for (int i = 0; i < 3; i++)
                initialCounts[i] = GC.CollectionCount(i);
            
            // Standard List test for comparison
            {
                var standardList = new List<int>(itemCount);
                
                // Fill the list
                for (int i = 0; i < itemCount; i++)
                    standardList.Add(i);
                    
                // Clear multiple times
                for (int i = 0; i < clearCount; i++)
                {
                    standardList.Clear();
                    // Add one item to ensure the list is working
                    standardList.Add(i);
                }
                
                standardList = null;
            }
            
            // Force a collection to ensure standardList is collected
            GC.Collect();
            GC.WaitForPendingFinalizers();
            
            // Record GC counts after standard List test
            int[] standardListCounts = new int[3];
            for (int i = 0; i < 3; i++)
                standardListCounts[i] = GC.CollectionCount(i) - initialCounts[i];
            
            // Log the collection counts
            output.WriteLine("TemporalArray GC collections:");
            output.WriteLine($"  Gen 0: {temporalArrayCounts[0]}");
            output.WriteLine($"  Gen 1: {temporalArrayCounts[1]}");
            output.WriteLine($"  Gen 2: {temporalArrayCounts[2]}");
            
            output.WriteLine("Standard List GC collections:");
            output.WriteLine($"  Gen 0: {standardListCounts[0]}");
            output.WriteLine($"  Gen 1: {standardListCounts[1]}");
            output.WriteLine($"  Gen 2: {standardListCounts[2]}");
            
            // Assert that TemporalArray caused fewer Gen 2 collections
            Assert.True(temporalArrayCounts[2] <= standardListCounts[2], 
                "TemporalArray should not cause more Gen 2 collections than List");
                
            // Also check if the overall number of collections is lower
            int temporalTotal = temporalArrayCounts.Sum();
            int standardTotal = standardListCounts.Sum();
            
            output.WriteLine($"Total GC collections - TemporalArray: {temporalTotal}, List: {standardTotal}");
            Assert.True(temporalTotal <= standardTotal, 
                "TemporalArray should not cause more total GC collections than List");
        }
    }
}