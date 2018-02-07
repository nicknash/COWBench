using HdrHistogram;

namespace COWBench
{
    class ThroughputResult
    {
        public string ListType { get; private set; }
        public int Capacity { get; private set; }
        public double LatencyNanoseconds { get; private set; }
        public double ReadProportion { get; private set; }

        public void Update(string listType, int capacity, double latencyNanoseconds, double readProportion)
        {
            ListType = listType;
            Capacity = capacity;
            LatencyNanoseconds = latencyNanoseconds;
            ReadProportion = readProportion;
        }
    }
}