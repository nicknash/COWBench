using System.Collections.Generic;
using System.Threading;

namespace COWBench
{
    class MultipleWriterCOWList : ISyncList
    {
        private CyclicArray _data;

        public MultipleWriterCOWList(int capacity)
        {
            _data = new CyclicArray(capacity);
        }

        public void Add(int v)
        {
            while (true)
            {
                var local = Volatile.Read(ref _data);
                var copy = new CyclicArray(local); // Assume an initial parent thread creates this instance (so no null check on _data)
                copy.Add(v);
                if (Interlocked.CompareExchange(ref _data, copy, local) == local)
                {
                    break;
                }
            }
        }

        public int this[int idx]
        {
            get
            {
                var local = Volatile.Read(ref _data);
                return local[idx];
            }
        }
    }
}
