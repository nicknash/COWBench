namespace COWBench
{
    class LatencyResult
    {
        public string ListType;
        public int Capacity;
        public long CountAtThisValue;
        public double Percentile;
        public double PercentileLevel;
        public long TotalCountToThisValue;
        public long TotalValueToThisValue;
        public double ValueIteratedFromNanos;
        public double ValueIteratedToNanos;
        public double ReadProportion;
        
        public void UpdateFrom(string listType, int capacity, long countAtThisValue, double percentile, double percentileLevel, long totalCountToThisValue, 
                               long totalValueToThisValue, double valueIteratedFromNanos, double valueIteratedToNanos, double readProportion)
        {
            ListType = listType;
            Capacity = capacity;
            CountAtThisValue = countAtThisValue;
            Percentile = percentile;
            PercentileLevel = percentileLevel;
            TotalCountToThisValue = totalCountToThisValue;
            TotalValueToThisValue = totalValueToThisValue;
            ValueIteratedFromNanos = valueIteratedFromNanos;
            ValueIteratedToNanos = valueIteratedToNanos;
            ReadProportion = readProportion;
        }
    }
}