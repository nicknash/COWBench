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

            var numThreadCombinations = 1 + numTestThreads;
            var allLists = new List<ISyncList[]>();            
            for(int i = 0; i < numThreadCombinations; ++i)
            {
                // TODO: For simpler experimental results, consider making these cyclic N element arrays
                // And try N = 10, 100, 1000 say. (GC + list growth)
                allLists.Add(new ISyncList[]{new LockList(), new RWLockList(), new MultipleWriterCOWList()});
            }

            for (int numReaders = 0; numReaders <= numTestThreads; ++numReaders)
            {
                int numWriters = numTestThreads - numReaders;
                var lists = allLists[numReaders];
                for (int i = 0; i < lists.Length; ++i)
                {
                    var list = lists[i];
                    list.Add(0);
                    StartThreads(idx => list[0], numReaders, numOperations, barrier);
                    StartThreads(value => { list.Add(value); return value; }, numWriters, numOperations, barrier);

                    barrier.SignalAndWait();
                    var start = Stopwatch.GetTimestamp();
                    barrier.SignalAndWait();
                    var end = Stopwatch.GetTimestamp();
                    // TODO: Accumulate results here.
                }
            }
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            using(var writer = File.CreateText($"COWBench-{today}-{Process.GetCurrentProcess().Id}"))
            {
                // TODO: Write thread latency distributions and all thread throughput.
            }
        }

        private static void StartThreads(Func<int, int> operation, int numThreads, int numOperations, Barrier barrier)
        {
            var threads = new Thread[numThreads];
            for(int i = 0; i < numThreads; ++i)
            {
                threads[i] = new Thread(new ParameterizedThreadStart(Thread)){IsBackground = true};
                var context = new ThreadContext(numOperations, operation, barrier);
                threads[i].Start(context);
            }
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