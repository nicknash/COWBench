namespace COWBench
{
    interface ISyncList
    {
        void Add(int v);
        int this[int idx] { get; }
    }
}