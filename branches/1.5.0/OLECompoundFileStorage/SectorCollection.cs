﻿using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;


namespace OleCompoundFileStorage
{
    public delegate void SizeLimitReached();

    /// <summary>
    /// Ad-hoc Heap Friendly sector collection to avoid using 
    /// large array that may create som problem to GC collection 
    /// (see http://www.simple-talk.com/dotnet/.net-framework/the-dangers-of-the-large-object-heap/ )
    /// </summary>
    internal class SectorCollection : IList<Sector>
    {
        private const int MAX_SECTOR_V4_COUNT_LOCK_RANGE = 524287; //0x7FFFFF00 for Version 4
        private const int SLICE_SIZE = 4096;

        private int count = 0;

        public event SizeLimitReached OnSizeLimitReached;

        private List<ArrayList> largeArraySlices = new List<ArrayList>();

        public SectorCollection()
        {

        }

        private bool sizeLimitReached = false;
        private void DoSizeLimitReached()
        {
            if (!sizeLimitReached && (count - 1 > MAX_SECTOR_V4_COUNT_LOCK_RANGE))
            {
                if (OnSizeLimitReached != null)
                    OnSizeLimitReached();

                sizeLimitReached = true;
            }
        }

        #region IList<T> Members

        public int IndexOf(Sector item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, Sector item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public Sector this[int index]
        {
            get
            {
                int itemIndex = index / SLICE_SIZE;
                int itemOffset = index % SLICE_SIZE;

                if ((index > -1) && (index < count))
                {
                    return (Sector)largeArraySlices[itemIndex][itemOffset];
                }
                else
                    throw new ArgumentOutOfRangeException("index", index, "Argument out of range");
            }

            set
            {
                int itemIndex = index / SLICE_SIZE;
                int itemOffset = index % SLICE_SIZE;

                if (index > -1 && index < count)
                {
                    largeArraySlices[itemIndex][itemOffset] = value;
                }
                else
                    throw new ArgumentOutOfRangeException("index", index, "Argument out of range");
            }
        }

        #endregion

        #region ICollection<T> Members

        private int add(Sector item)
        {
            int itemIndex = count / SLICE_SIZE;

            if (itemIndex < largeArraySlices.Count)
            {
                largeArraySlices[itemIndex].Add(item);
                count++;
            }
            else
            {
                ArrayList ar = new ArrayList(SLICE_SIZE);
                ar.Add(item);
                largeArraySlices.Add(ar);
                count++;
            }

            return count - 1;
        }

        public void Add(Sector item)
        {
            DoSizeLimitReached();

            add(item);

        }

        public void Clear()
        {
            foreach (ArrayList slice in largeArraySlices)
            {
                slice.Clear();
            }

            largeArraySlices.Clear();

            count = 0;
        }

        public bool Contains(Sector item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(Sector[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get
            {
                return count;
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(Sector item)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IEnumerable<T> Members

        public IEnumerator<Sector> GetEnumerator()
        {

            for (int i = 0; i < largeArraySlices.Count; i++)
            {
                for (int j = 0; j < largeArraySlices[i].Count; j++)
                {
                    yield return (Sector)largeArraySlices[i][j];

                }
            }
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            for (int i = 0; i < largeArraySlices.Count; i++)
            {
                for (int j = 0; j < largeArraySlices[i].Count; j++)
                {
                    yield return largeArraySlices[i][j];
                }
            }
        }

        #endregion
    }
}
