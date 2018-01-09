using System;
using System.Threading;

namespace COWBench
{
    class ThreadContext
    {
        public readonly int NumOperations;
        public readonly Action<int> Operation;
        public readonly Barrier StartBarrier; 
        public readonly long[] Latencies;

        public ThreadContext(int numOperations, Action<int> operation, Barrier barrier)
        {
            NumOperations = numOperations;
            Operation = operation;
            Latencies = new long[numOperations];
            StartBarrier = barrier;
        }
    }
}