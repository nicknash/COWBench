using System;
using System.Threading;
using HdrHistogram;

namespace COWBench
{
    class ThreadContext
    {
        public readonly ISyncList SyncList;
        public readonly int NumOperations;
        public readonly int[] OperationResults;
        public readonly Barrier Barrier; 
        public readonly LongHistogram Latencies;
        public readonly bool[] isRead;

        public ThreadContext(ISyncList list, int numOperations, Barrier barrier, double readProportion)
        {
            SyncList = list;
            NumOperations = numOperations;
            Latencies = new LongHistogram(TimeStamp.Seconds(1), 3);
            Barrier = barrier;
            OperationResults = new int[10];
            isRead = new bool[numOperations];
            var r = new Random();
            for(int i = 0; i < numOperations; ++i)
            {
                isRead[i] = r.NextDouble() < readProportion;
            }
        }
    }
}