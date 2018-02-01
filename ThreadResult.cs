using HdrHistogram;

namespace COWBench
{
    class ThreadResult
    {
        public int ThreadId { get; private set; }
        public string ListType { get; private set; }
        public int Capacity { get; private set; }
        public double LatencyNanoseconds { get; private set; }
        public int NumOperations { get; private set;}
        public double ReadProportion { get; private set; }
        public bool IsRead { get; private set; }

        public void Update(int threadId, string listType, int capacity, double latencyNanoseconds, int numOperations, double readProportion, bool isRead)
        {
            ThreadId = threadId;
            ListType = listType;
            Capacity = capacity;
            LatencyNanoseconds = latencyNanoseconds;
            NumOperations = numOperations;
            ReadProportion = readProportion;
            IsRead = isRead;
        }
    }
}