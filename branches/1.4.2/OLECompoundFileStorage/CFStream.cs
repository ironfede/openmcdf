using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using BinaryTrees;

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

namespace OleCompoundFileStorage
{

    /// <summary>
    /// OLE structured storage <see cref="T:OLECompoundFileStorage.CFStream">stream</see> Object
    /// It is contained inside a Storage object in a file-directory
    /// relationship and indexed by its name.
    /// </summary>
    public class CFStream : CFItem
    {
        
        internal CFStream(CompoundFile sectorManager)
            : base(sectorManager)
        {
            this.dirEntry = new DirectoryEntry(StgType.StgStream);
            sectorManager.AddDirectoryEntry(this);
            this.dirEntry.StgColor = StgColor.Black;
        }

        internal CFStream(CompoundFile sectorManager, IDirectoryEntry dirEntry)
            : base(sectorManager)
        {
            if (dirEntry == null || dirEntry.SID < 0)
                throw new CFException("Attempting to add a CFStream using an unitialized directory");

            this.dirEntry = dirEntry as DirectoryEntry;
        }

        /// <summary>
        /// Set the data associated with the stream object.
        /// </summary>
        /// <example>
        /// <code>
        ///    byte[] b = new byte[]{0x0,0x1,0x2,0x3};
        ///    CompoundFile cf = new CompoundFile();
        ///    CFStream myStream = cf.RootStorage.AddStream("MyStream");
        ///    myStream.SetData(b);
        /// </code>
        /// </example>
        /// <param name="data">Data bytes to write to this stream</param>
        public void SetData(Byte[] data)
        {
            CheckDisposed();

            this.CompoundFile.SetData(this, data);
        }

        /// <summary>
        /// Get the data associated with the stream object.
        /// </summary>
        /// <example>
        /// <code>
        ///     CompoundFile cf2 = new CompoundFile("AFileName.cfs");
        ///     CFStream st = cf2.RootStorage.GetStream("MyStream");
        ///     byte[] buffer = st.GetData();
        /// </code>
        /// </example>
        /// <returns>Array of byte containing stream data</returns>
        /// <exception cref="T:OLECompoundFileStorage.CFDisposedException">
        /// Raised when the owner compound file has been closed.
        /// </exception>
        public Byte[] GetData()
        {
            CheckDisposed();

            return this.CompoundFile.GetData(this);
        }

        /// <summary>
        /// Copy data from an existing stream.
        /// </summary>
        /// <param name="input">A stream to read from</param>
        public void CopyFrom(Stream input)
        {
            CheckDisposed();

            byte[] buffer = new byte[input.Length];

            if (input.CanSeek)
            {
                input.Seek(0, SeekOrigin.Begin);
            }

            input.Read(buffer,0,(int)input.Length);
            this.SetData(buffer);
        }
    }
}
