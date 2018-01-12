using System;
using System.Threading;

namespace COWBench
{
    class ThreadContext
    {
        public readonly int NumOperations;
        public readonly Func<int, int> Operation;
        public readonly int[] OperationResults;
        public readonly Barrier StartBarrier; 
        public readonly long[] Latencies;

        public ThreadContext(int numOperations, Func<int, int> operation, Barrier barrier)
        {
            NumOperations = numOperations;
            Operation = operation;
            Latencies = new long[numOperations];
            StartBarrier = barrier;
            OperationResults = new int[10];
        }
    }
}