using System;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Collections.Generic;

using HdrHistogram;

namespace COWBench
{
    partial class EntryPoint
    {
        public static void Main(string[] args)
        {
            int numTestThreads = Int32.Parse(args[0]);
            var barrier = new Barrier(numTestThreads + 1);
            int numOperations = 10000;
            int numReadProportions = 20;
            int maxPercentiles = 256;
            var listCapacities = new int[]{10, 100, 1000, 10000};

            var readProportions = Enumerable.Range(0, numReadProportions).Select(i => i / (double) numReadProportions).ToArray();
            var allLists = new List<ISyncList[]>();            
            for(int i = 0; i < numReadProportions; ++i)
            {
                foreach (var c in listCapacities)
                {
                    allLists.Add(new ISyncList[]{new LockList(c), new RWLockList(c), new MultipleWriterCOWList(c)});
                }
            }
            var latencyResults = new LatencyResult[listCapacities.Length * numReadProportions * allLists[0].Length * maxPercentiles];
            for(int i = 0; i < latencyResults.Length; ++i)
            {
                latencyResults[i] = new LatencyResult();
            }
            var throughputResults = new ThroughputResult[listCapacities.Length * numReadProportions * allLists[0].Length]; 
            for(int i = 0; i < throughputResults.Length; ++i)
            {
                throughputResults[i] = new ThroughputResult();
            }
            int latencyResultIdx = 0;
            int throughputResultIdx = 0;
            for(int capacityIdx = 0; capacityIdx < listCapacities.Length; ++capacityIdx)
            {
                var capacity = listCapacities[capacityIdx];
                for(int propIdx = 0; propIdx < numReadProportions; ++propIdx)
                {
                    var lists = allLists[propIdx * listCapacities.Length];
                    var readProportion = readProportions[propIdx];
                    for (int i = 0; i < lists.Length; ++i)
                    {
                        var list = lists[i];
                        list.Add(0);
                        var threadContexts = StartThreads(list, numTestThreads, numOperations, barrier, readProportion);

                        barrier.SignalAndWait();
                        var start = Stopwatch.GetTimestamp();
                        barrier.SignalAndWait();
                        var end = Stopwatch.GetTimestamp();
                        var listType = list.GetType().Name.ToString();
                        throughputResults[throughputResultIdx].Update(listType, capacity, ToNanos(end - start), readProportion);
                        ++throughputResultIdx;
                        latencyResultIdx = RecordResults(latencyResults, capacity, listType, latencyResultIdx, 0, readProportion, threadContexts);
                    }
                }
            }
            var today = DateTime.Today.ToString("yyyy-MM-dd");
            var suffix = $"{today}-{Process.GetCurrentProcess().Id}";
            WriteResults($"COWBench-Latency-{suffix}.csv", 
                        "ListType,Capacity,ReadProportion,CountAtThisValue,Percentile,PercentileLevel,TotalCountToThisValue,TotalValueToThisValue",
                         latencyResults, latencyResultIdx,
                         r => $"{r.ListType},{r.Capacity},{r.ReadProportion},{r.CountAtThisValue},{r.Percentile},{r.PercentileLevel},{r.TotalCountToThisValue},{r.TotalValueToThisValue}");
            
            WriteResults($"COWBench-Throughput-{suffix}.csv", "ListType,Capacity,LatencyNanoseconds,ReadProportion", 
                         throughputResults, throughputResultIdx,
                         r => $"{r.ListType},{r.Capacity},{r.LatencyNanoseconds},{r.ReadProportion}");
        }

        private static void WriteResults<T>(string fileName, string header, T[] results, int numResults, Func<T, string> format)
        {
            using (var writer = File.CreateText(fileName))
            {
                writer.WriteLine(header);
                for (int i = 0; i < numResults; ++i)
                {
                    writer.WriteLine(format(results[i]));
                }
            }
        }
        private static double ToNanos(long ticks) => 1e9*ticks/Stopwatch.Frequency;
        private static int RecordResults(LatencyResult[] target, int capacity, string listType, int startResultIdx, int threadIdOffset, double readProportion, ThreadContext[] contexts)
        {
            int resultIdx = startResultIdx;
            foreach(var v in contexts[0].Latencies.Percentiles(3))
            {
                var t = target[resultIdx];
                t.UpdateFrom(listType, capacity, v.CountAtValueIteratedTo, v.Percentile, v.PercentileLevelIteratedTo, v.TotalCountToThisValue, v.TotalValueToThisValue, readProportion);
                ++resultIdx;
            }
            return resultIdx;
        }

        private static ThreadContext[] StartThreads(ISyncList list, int numThreads, int numOperations, Barrier barrier, double readProportion)
        {
            var threads = new Thread[numThreads];
            var contexts = new ThreadContext[numThreads];
            for(int i = 0; i < numThreads; ++i)
            {
                threads[i] = new Thread(new ParameterizedThreadStart(Thread)){IsBackground = true};
                var latencies = i == 0 ? new LongHistogram(TimeStamp.Seconds(1), 3) : null;
                var context = new ThreadContext(list, numOperations, barrier, readProportion, latencies);
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
                var isRead = context.isRead[i];
                var before = Stopwatch.GetTimestamp();
                if(isRead)
                {
                    results[i % results.Length] = context.SyncList[0];
                }
                else
                {
                    context.SyncList.Add(i);
                }
                var after = Stopwatch.GetTimestamp();
                context.Latencies?.RecordValue(after - before);
            }
            context.Barrier.SignalAndWait();
        }
    }
}