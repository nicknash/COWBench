using System;
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

            var numThreadCombinations = numTestThreads * (numTestThreads + 1) / 2;
            var allLists = new List<ISyncList[]>();            
            for(int i = 0; i < numThreadCombinations; ++i)
            {
                allLists[i] = new ISyncList[]{new LockList(), new RWLockList(), new MultipleWriterCOWList()};
            }
            int listsIdx = 0;
            for (int numReaders = 0; numReaders < numTestThreads; ++numReaders)
            {
                for (int numWriters = 0; numWriters < numTestThreads - numReaders; ++numWriters)
                {
                    var lists = allLists[listsIdx];
                    for(int i = 0; i < lists.Length; ++i)
                    {
                        var list = lists[i];
                        StartThreads(idx => list[idx], numReaders, numOperations, barrier);
                        StartThreads(value => { list.Add(value); return value; }, numWriters, numOperations, barrier);

                        barrier.SignalAndWait();
                        var start = Stopwatch.GetTimestamp();
                        barrier.SignalAndWait();
                        var end = Stopwatch.GetTimestamp();
                    }
                    ++listsIdx;
                }
            }
        }

        private static void StartThreads(Func<int, int> operation, int numThreads, int numOperations, Barrier barrier)
        {
            var threads = new Thread[numThreads];
            for(int i = 0; i < numThreads; ++i)
            {
                var context = new ThreadContext(numOperations, operation, barrier);
                threads[i] = new Thread(new ParameterizedThreadStart(Thread)){IsBackground = true};
                threads[i].Start();
            }
        }

        public static void Thread(object p)
        {
            var context = (ThreadContext) p;
            context.StartBarrier.SignalAndWait();
            var results = context.OperationResults;
            for (int i = 0; i < context.NumOperations; ++i)
            {
                var before = Stopwatch.GetTimestamp();
                var r = context.Operation(i);
                var after = Stopwatch.GetTimestamp();
                results[i % results.Length] = r;
                context.Latencies[i] = after - before;
            }
        }
    }
}