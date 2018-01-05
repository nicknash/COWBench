using System.Collections.Generic;
using System.Threading;

class MultipleWriterCOWList
{
    private List<int> _data = new List<int>();

    public void Add(int v)
    {
        while(true)
        {
            var local = Volatile.Read(ref _data);
            var copy = local == null ? new List<int>() : new List<int>(local);
            copy.Add(v);
            if(Interlocked.CompareExchange(ref _data, copy, local) == local)
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