using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.ObjectModel;

namespace Common
{
    public class CollectionOfCollections<T>
    {
        private Collection<Collection<T>> collections;

        // totalSize = sum(sizes)
        private int totalSize;

        public CollectionOfCollections()
        {
            this.collections = new Collection<Collection<T>>();
            this.totalSize = 0;
        }

        public void Add(Collection<T> c)
        {
            if (c == null) throw new ArgumentNullException();
            collections.Add(c);
            totalSize += c.Count;
        }

        public T Get(int i)
        {
            if (i < 0 || i >= totalSize) throw new ArgumentException("i");

            int accum = 0;
            for (int ci = 0; ci < collections.Count; ci++)
            {
                Collection<T> currColl = collections[ci];
                if (i < accum + currColl.Count)
                {
                    // Desired element is in currColl.
                    return currColl[i - accum];
                }
                accum += currColl.Count;
            }
            throw new RandoopBug();
        }

        public int Size()
        {
            return this.totalSize;
        }

    }
}
