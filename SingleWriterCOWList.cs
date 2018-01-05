using System.Collections.Generic;
using System.Threading;

class SingleWriterCOWList
{
    private List<int> _data = new List<int>();

    public void Add(int v)
    {
        var copy = new List<int>(_data);
        copy.Add(v);
        Volatile.Write(ref _data, copy);
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