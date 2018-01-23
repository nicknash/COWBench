using System.Collections.Generic;
using System.Threading;

namespace COWBench
{
    class RWLockList : ISyncList
    {
        private readonly CyclicArray _data;
        private readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();

        public RWLockList(int capacity)
        {
            _data = new CyclicArray(capacity);
        }

        public void Add(int v)
        {
            _rwLock.EnterWriteLock();
            try
            {
                _data.Add(v);
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }

        public int this[int idx]
        {
            get
            {
                _rwLock.EnterReadLock();
                try
                {
                    return _data[idx];
                }
                finally
                {
                    _rwLock.ExitReadLock();
                }
            }
        }
    }
}