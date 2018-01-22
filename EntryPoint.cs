using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;

namespace COWBench
{
    class EntryPoint
    {
        public static void Main(string[] args)
        {
            int numTestThreads = Int32.Parse(args[0]);
            var barrier = new Barrier(numTestThreads + 1);
            int numOperations = 10;
            var listCapacities = new int[]{10, 100, 1000, 10000};

            var numThreadCombinations = 1 + numTestThreads;
            var allLists = new List<ISyncList[]>();            
            for(int i = 0; i < numThreadCombinations; ++i)
            {
                foreach (var c in listCapacities)
                {
                    allLists.Add(new ISyncList[]{new LockList(c), new RWLockList(c), new MultipleWriterCOWList(c)});
                }
            }
            var latencyResults = new ThreadResult[listCapacities.Length * numThreadCombinations * allLists[0].Length * numTestThreads * numOperations];
            for(int i = 0; i < latencyResults.Length; ++i)
            {
                latencyResults[i] = new ThreadResult();
            }
            var throughputResults = new ThreadResult[listCapacities.Length * numThreadCombinations * allLists[0].Length]; 
            for(int i = 0; i < throughputResults.Length; ++i)
            {
                throughputResults[i] = new ThreadResult();
            }
            int latencyResultIdx = 0;
            int throughputResultIdx = 0;
            for(int capacityIdx = 0; capacityIdx < listCapacities.Length; ++capacityIdx)
            {
                var capacity = listCapacities[capacityIdx];
                for (int numReaders = 0; numReaders <= numTestThreads; ++numReaders)
                {
                    int numWriters = numTestThreads - numReaders;
                    var lists = allLists[numReaders * listCapacities.Length];
                    for (int i = 0; i < lists.Length; ++i)
                    {
                        var list = lists[i];
                        list.Add(0);
                        var readContexts = StartThreads(idx => list[0], numReaders, numOperations, barrier);
                        var writeContexts = StartThreads(value => { list.Add(value); return value; }, numWriters, numOperations, barrier);

                        barrier.SignalAndWait();
                        var start = Stopwatch.GetTimestamp();
                        barrier.SignalAndWait();
                        var end = Stopwatch.GetTimestamp();
                        var listType = list.GetType().Name.ToString();
                        throughputResults[throughputResultIdx].Update(-1, listType, capacity, ToNanos(end - start), numOperations, "ALL", numReaders, numWriters);
                        ++throughputResultIdx;
                        latencyResultIdx = RecordResults(latencyResults, capacity, listType, latencyResultIdx, 0, "Reader", numReaders, numWriters, readContexts);
                        latencyResultIdx = RecordResults(latencyResults, capacity, listType, latencyResultIdx, 0, "Writer", numReaders, numWriters, writeContexts);
                    }
                }
            }
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            var suffix = $"{today}-{Process.GetCurrentProcess().Id}";
            WriteResults($"COWBench-Latency-{suffix}.csv", latencyResults);
            WriteResults($"COWBench-Throughput-{suffix}.csv", throughputResults);
        }

        private static void WriteResults(string fileName, ThreadResult[] results)
        {
            using (var writer = File.CreateText(fileName))
            {
                writer.WriteLine($"ThreadId,ThreadType,NumReaders,NumWriters,NumOperations,ListType,Capacity,LatencyNanoseconds");
                foreach (var r in results)
                {
                    writer.WriteLine($"{r.ThreadId},{r.ThreadType},{r.NumReaders},{r.NumWriters},{r.NumOperations},{r.ListType},{r.Capacity},{r.LatencyNanoseconds}");
                }
            }
        }
        private static double ToNanos(long ticks) => 1e9*ticks/Stopwatch.Frequency;
        private static int RecordResults(ThreadResult[] target, int capacity, string listType, int startResultIdx, int threadIdOffset, string threadTag, int numReaders, int numWriters, ThreadContext[] contexts)
        {
            int resultIdx = startResultIdx;
            for (int k = 0; k < contexts.Length; ++k)
            {
                var context = contexts[k];
                for (int j = 0; j < context.Latencies.Length; ++j)
                {
                    var latencyNanos = ToNanos(context.Latencies[j]);
                    target[resultIdx].Update(k + threadIdOffset, listType, capacity, latencyNanos, context.Latencies.Length, threadTag, numReaders, numWriters);
                    ++resultIdx;
                }
            }
            return resultIdx;
        }

        private static ThreadContext[] StartThreads(Func<int, int> operation, int numThreads, int numOperations, Barrier barrier)
        {
            var threads = new Thread[numThreads];
            var contexts = new ThreadContext[numThreads];
            for(int i = 0; i < numThreads; ++i)
            {
                threads[i] = new Thread(new ParameterizedThreadStart(Thread)){IsBackground = true};
                var context = new ThreadContext(numOperations, operation, barrier);
                contexts[i] = context;
                threads[i].Start(context);
            }
            return contexts;
        }

        public static void Thread(object p)
        {
            var context = (ThreadContext) p;
            context.Barrier.SignalAndWait();
            var results = context.OperationResults;
            for (int i = 0; i < context.NumOperations; ++i)
            {
                var before = Stopwatch.GetTimestamp();
                var r = context.Operation(i);
                var after = Stopwatch.GetTimestamp();
                results[i % results.Length] = r;
                context.Latencies[i] = after - before;
            }
            context.Barrier.SignalAndWait();
        }
    }
}