using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.IO;

namespace OleCompoundFileStorage
{
    internal class SectorCollection : IList<Sector>
    {
        CompoundFile owner;
        ArrayList sectors = null;
        int sectorSize = 0;

        internal SectorCollection(int capacity, int sectorSize, CompoundFile owner)
        {
            this.owner = owner;
            this.sectorSize = sectorSize;

            sectors = new ArrayList(capacity);

            for (int i = 0; i < capacity; i++)
            {
                sectors.Add(null);
            }
        }

        #region IList<Sector> Members

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
                Sector s = sectors[index] as Sector;

                return s;
            }

            set
            {
                sectors[index] = value;
            }
        }

        public void ReleaseStreamedSector(int index)
        {
            Sector s = (Sector)sectors[index];

            if (s != null)
            {
                s.ReleaseData();
                s.DirtyFlag = false;
            }
        }

        #endregion

        #region ICollection<Sector> Members

        public void Add(Sector item)
        {
            CheckTransactionLockSector();

            this.sectors.Add(item);
            item.Id = sectors.Count - 1;

        }

        private const int MAX_SECTOR_V4_COUNT_LOCK_RANGE = 524287;

        private void CheckTransactionLockSector()
        {
            if (!owner._transactionLockAdded && ((sectors.Count - 2) > MAX_SECTOR_V4_COUNT_LOCK_RANGE))
            {
                Sector rangeLockSector = new Sector(sectorSize, owner.sourceStream);
                rangeLockSector.Id = sectors.Count - 1;
                rangeLockSector.Type = SectorType.RangeLockSector;
                sectors.Add(new Sector(sectorSize, owner.sourceStream));
                owner._transactionLockAdded = true;
                owner._lockSectorId = rangeLockSector.Id;
            }
        }

        public void Clear()
        {
            this.sectors.Clear();
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
            get { return sectors.Count; }
        }

        public bool IsReadOnly
        {
            get { return true; }
        }

        public bool Remove(Sector item)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IEnumerable<Sector> Members

        public IEnumerator<Sector> GetEnumerator()
        {
            return sectors.GetEnumerator() as IEnumerator<Sector>;
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return sectors.GetEnumerator();
        }

        #endregion
    }
}
