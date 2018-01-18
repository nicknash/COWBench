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
            int numOperations = 10000;
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

            var allThreadResults = new ThreadResult[listCapacities.Length * numThreadCombinations * numOperations * allLists.Count];
            for(int i = 0; i < allThreadResults.Length; ++i)
            {
                allThreadResults[i] = new ThreadResult();
            }

            int resultIdx = 0;
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
                        // TODO: Record throughput results here as well as the latency results below.
                        var listType = list.GetType().Name.ToString();
                        resultIdx = RecordResults(allThreadResults, listType, resultIdx, 0, "Reader", readContexts);
                        resultIdx = RecordResults(allThreadResults, listType, resultIdx + readContexts.Length * numOperations, 0, "Writer", writeContexts);
                    }
                }
            }
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            using(var writer = File.CreateText($"COWBench-Latency-{today}-{Process.GetCurrentProcess().Id}.csv"))
            {
                foreach(var r in allThreadResults)
                {
                    writer.WriteLine($"{r.ThreadId},{r.ThreadType},{r.ListType},{r.LatencyNanoseconds}");
                }
                // TODO: Throughput results.
            }
        }

        private static int RecordResults(ThreadResult[] target, string listType, int startResultIdx, int threadIdOffset, string threadTag, ThreadContext[] contexts)
        {
            int resultIdx = startResultIdx;
            for (int k = 0; k < contexts.Length; ++k)
            {
                var context = contexts[k];
                for (int j = 0; j < context.Latencies.Length; ++j)
                {
                    var latencyNanos = 1e9 * context.Latencies[j] / (double)Stopwatch.Frequency;
                    target[resultIdx].Update(k + threadIdOffset, listType, latencyNanos, threadTag);
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