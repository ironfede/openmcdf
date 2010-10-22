using System;
using System.Collections.Generic;
using System.Text;

/*
     The contents of this file are subject to the Mozilla Public License
     Version 1.1 (the "License"); you may not use this file except in
     compliance with the License. You may obtain a copy of the License at
     http://www.mozilla.org/MPL/

     Software distributed under the License is distributed on an "AS IS"
     basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
     License for the specific language governing rights and limitations
     under the License.

     The Original Code is OpenMCDF - Compound Document Format library.

     The Initial Developer of the Original Code is Federico Blaseotto.
*/

namespace OLECompoundFileStorage
{
    public class DirectoryEntryCollection:ICollection<IDirectoryEntry>
    {
        private List<IDirectoryEntry> directoryEntries;

        internal DirectoryEntryCollection(List<IDirectoryEntry> directoryEntries)
        {
            this.directoryEntries  = directoryEntries;
        }

        public void Add(IDirectoryEntry item)
        {
            throw new NotSupportedException("Read Only Collection");
        }

        public void Clear()
        {
            throw new NotSupportedException("Read Only Collection");
        }

        public bool Contains(IDirectoryEntry item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(IDirectoryEntry[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsReadOnly
        {
            get { throw new NotImplementedException(); }
        }

        public bool Remove(IDirectoryEntry item)
        {
            throw new NotImplementedException();
        }

        #region IEnumerable<IDirectoryEntry> Members

        public IEnumerator<IDirectoryEntry> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    public class Directory
    {
        private bool isCompoundFileDisposed = false;

        internal bool IsCompoundFileDisposed
        {
            get { return isCompoundFileDisposed; }
            set { isCompoundFileDisposed = value; }
        }

        
    }
}
