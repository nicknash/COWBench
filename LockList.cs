using System.Collections.Generic;

namespace COWBench
{
    class LockList : ISyncList
    {
        private readonly CyclicArray _data;
        private readonly object _lockObject = new object();

        public LockList(int capacity)
        {
            _data = new CyclicArray(capacity);
        }

        public void Add(int v)
        {
            lock (_lockObject)
            {
                _data.Add(v);
            }
        }

        public int this[int idx]
        {
            get
            {
                lock (_lockObject)
                {
                    return _data[idx];
                }
            }
        }
    }
}

