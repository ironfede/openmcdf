/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. 
 * 
 * The Original Code is OpenMCDF - Compound Document Format library.
 * 
 * The Initial Developer of the Original Code is Federico Blaseotto.*/

using System;
using System.Collections;
using System.Collections.Generic;

namespace OpenMcdf
{
    /// <summary>
    ///     Action to implement when transaction support - sector
    ///     has to be written to the underlying stream (see specs).
    /// </summary>
    public delegate void Ver3SizeLimitReached();

    /// <summary>
    ///     Ad-hoc Heap Friendly sector collection to avoid using
    ///     large array that may create some problem to GC collection
    ///     (see http://www.simple-talk.com/dotnet/.net-framework/the-dangers-of-the-large-object-heap/ )
    /// </summary>
    internal class SectorCollection : IList<Sector>
    {
        private const int MAX_SECTOR_V4_COUNT_LOCK_RANGE = 524287; //0x7FFFFF00 for Version 4
        private const int SLICE_SIZE = 4096;

        private readonly List<List<Sector>> largeArraySlices = new List<List<Sector>>();

        private bool sizeLimitReached;

        public event Ver3SizeLimitReached OnVer3SizeLimitReached;

        private void DoCheckSizeLimitReached()
        {
            if (!sizeLimitReached && Count - 1 > MAX_SECTOR_V4_COUNT_LOCK_RANGE)
            {
                OnVer3SizeLimitReached?.Invoke();

                sizeLimitReached = true;
            }
        }

        #region IEnumerable<T> Members

        public IEnumerator<Sector> GetEnumerator()
        {
            for (var i = 0; i < largeArraySlices.Count; i++)
                for (var j = 0; j < largeArraySlices[i].Count; j++)
                    yield return largeArraySlices[i][j];
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            yield return GetEnumerator();
        }

        #endregion

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
                if (index <= -1 || index >= Count)
                    throw new ArgumentOutOfRangeException(nameof(index), index, "Argument out of range");

                var itemIndex = index / SLICE_SIZE;
                var itemOffset = index % SLICE_SIZE;
                return largeArraySlices[itemIndex][itemOffset];
            }

            set
            {
                if (index <= -1 || index >= Count)
                    throw new ArgumentOutOfRangeException(nameof(index), index, "Argument out of range");

                var itemIndex = index / SLICE_SIZE;
                var itemOffset = index % SLICE_SIZE;
                largeArraySlices[itemIndex][itemOffset] = value;
            }
        }

        #endregion

        #region ICollection<T> Members

        public int Count { get; private set; }

        public bool IsReadOnly => false;

        private int add(Sector item)
        {
            var itemIndex = Count / SLICE_SIZE;

            if (itemIndex < largeArraySlices.Count)
            {
                largeArraySlices[itemIndex].Add(item);
                Count++;
            }
            else
            {
                var ar = new List<Sector>(SLICE_SIZE) {item};
                largeArraySlices.Add(ar);
                Count++;
            }

            return Count - 1;
        }

        public void Add(Sector item)
        {
            DoCheckSizeLimitReached();

            add(item);
        }

        public void Clear()
        {
            foreach (var slice in largeArraySlices) slice.Clear();

            largeArraySlices.Clear();

            Count = 0;
        }

        public bool Contains(Sector item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(Sector[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(Sector item)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}