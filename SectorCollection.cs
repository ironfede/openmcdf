using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.IO;

namespace OleCompoundFileStorage
{
    internal class SectorCollection : IList<Sector>
    {
        ArrayList sectors = null;
        int sectorSize = 0;
        BinaryReader reader;
        internal SectorCollection(int capacity, int sectorSize, BinaryReader reader)
        {
            this.sectorSize = sectorSize;
            this.reader = reader;

            sectors = new ArrayList(capacity);

            for (int i = 0; i < capacity; i++)
            {
                sectors[i] = null;
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

                if (s == null)
                {
                    reader.BaseStream.Seek(sectorSize + index * sectorSize, SeekOrigin.Begin);
                    s = new Sector(sectorSize, reader.ReadBytes(sectorSize));
                    s.Id = index;
                    sectors[index] = s;

                }

                return s;
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public void ReleaseSector(int index)
        {
            sectors[index] = null;
        }

        #endregion

        #region ICollection<Sector> Members

        public void Add(Sector item)
        {
            this.sectors.Add(item);
            item.Id = sectors.Count - 1;
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
