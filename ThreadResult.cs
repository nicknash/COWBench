namespace COWBench
{
    class ThreadResult
    {
        public int ThreadId { get; private set; }
        public string ListType { get; private set; }
        public double LatencyNanoseconds { get; private set; }
        public string ThreadType { get; private set; }

        public void Update(int threadId, string listType, double latencyNanoseconds, string threadType)
        {
            ThreadId = threadId;
            ListType = listType;
            LatencyNanoseconds = latencyNanoseconds;
            ThreadType = threadType;
        }
    }
}