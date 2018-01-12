using System.Collections.Generic;

namespace COWBench
{
    class LockList : ISyncList
    {
        private List<int> _data = new List<int>();
        private object _lockObject = new object();

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

