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
            for (int numReaders = 0; numReaders < testThreads; ++numReaders)
            {
                for (int numWriters = 0; numWriters < testThreads - numReaders; ++numWriters)
                {
                    // TODO, simple demonstration of:
                    //  * Time to complete for N readers, M writers (explore N, M)
                    //  * Read/Write latency (repeat, say 10,000 times - full distribution?) for N readers, M writers (explore N, M)
                }
            }
            barrier.SignalAndWait(); // start 
        }

        public void Thread(object p)
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