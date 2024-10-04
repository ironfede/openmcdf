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
    /// Action to implement when transaction support - sector
    /// has to be written to the underlying stream (see specs).
    /// </summary>
    public delegate void Ver3SizeLimitReached();

    /// <summary>
    /// Ad-hoc Heap Friendly sector collection to avoid using
    /// large array that may create some problem to GC collection
    /// (see http://www.simple-talk.com/dotnet/.net-framework/the-dangers-of-the-large-object-heap/ )
    /// </summary>
    internal sealed class SectorCollection : IList<Sector>
    {
        private const int MAX_SECTOR_V4_COUNT_LOCK_RANGE = 0x7FFFFF00;
        private const int SLICE_SIZE = 4096;

        public event Ver3SizeLimitReached OnVer3SizeLimitReached;

        private readonly List<List<Sector>> largeArraySlices = new();
        private bool sizeLimitReached = false;

        public SectorCollection()
        {
        }

        private void DoCheckSizeLimitReached()
        {
            if (!sizeLimitReached && (Count - 1 > MAX_SECTOR_V4_COUNT_LOCK_RANGE))
            {
                sizeLimitReached = true;
                OnVer3SizeLimitReached?.Invoke();
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
                if (index <= -1 || index >= Count)
                    throw new CFException("Argument Out of Range, possibly corrupted file", new ArgumentOutOfRangeException(nameof(index), index, "Argument out of range"));

                int itemIndex = Math.DivRem(index, SLICE_SIZE, out int itemOffset);
                return largeArraySlices[itemIndex][itemOffset];
            }

            set
            {
                if (index <= -1 || index >= Count)
                    throw new ArgumentOutOfRangeException(nameof(index), index, "Argument out of range");

                int itemIndex = Math.DivRem(index, SLICE_SIZE, out int itemOffset);
                largeArraySlices[itemIndex][itemOffset] = value;
            }
        }

        #endregion

        #region ICollection<T> Members

        public void Add(Sector item)
        {
            DoCheckSizeLimitReached();

            int itemIndex = Count / SLICE_SIZE;
            if (itemIndex < largeArraySlices.Count)
            {
                largeArraySlices[itemIndex].Add(item);
            }
            else
            {
                List<Sector> ar = new(SLICE_SIZE)
                {
                    item
                };
                largeArraySlices.Add(ar);
            }

            Count++;
        }

        public void Clear()
        {
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

        public int Count { get; private set; } = 0;

        public bool IsReadOnly => false;

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
                List<Sector> slice = largeArraySlices[i];
                for (int j = 0; j < slice.Count; j++)
                {
                    yield return slice[j];
                }
            }
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion
    }
}
