using System;
using System.Diagnostics;
using System.Threading;

namespace COWBench
{
    class EntryPoint
    {
        public static void Main(string[] args)
        {
            int testThreads = Int32.Parse(args[0]);
            var barrier = new Barrier(testThreads + 1);
            var list = new MultipleWriterCOWList(); // TODO, pre-alloc this so no growing
            int numOperations = 10000;
            for (int numReaders = 0; numReaders < testThreads; ++numReaders)
            {
                for (int numWriters = 0; numWriters < testThreads - numReaders; ++numWriters)
                {
                    StartThreads(idx => list[idx], numReaders, numOperations, barrier);
                    StartThreads(value => { list.Add(value); return value; }, numWriters, numOperations, barrier);
                    
                    barrier.SignalAndWait();  
                    var start = Stopwatch.GetTimestamp();
                    barrier.SignalAndWait(); 
                    var end = Stopwatch.GetTimestamp();
                    // Record completion time and num ops
                    // Record latency distributions
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
            for (int i = 0; i < context.NumOperations; ++i)
            {
                var before = Stopwatch.GetTimestamp();
                context.Operation(i);
                var after = Stopwatch.GetTimestamp();
                context.Latencies[i] = after - before;
            }
        }
    }
}