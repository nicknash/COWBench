namespace COWBench
{
    class CyclicArray
    {
        private readonly int _capacity;
        private int _nextIndex;
        private int[] _data;

        public CyclicArray(int capacity)
        {
            _capacity = capacity;
            _data = new int[capacity];
        }

        public CyclicArray(CyclicArray other)
        {
            _nextIndex = other._nextIndex;
            _capacity = other._capacity;
            _data = new int[_capacity];
            for(int i = 0; i < _capacity; ++i)
            {
                _data[i] = other._data[i];
            }
        }

        public void Add(int v)
        {
            _data[_nextIndex] = v;
            _nextIndex = (_nextIndex + 1) % _capacity;
        }

        public int this[int idx] => _data[idx % _capacity];
    }
}
