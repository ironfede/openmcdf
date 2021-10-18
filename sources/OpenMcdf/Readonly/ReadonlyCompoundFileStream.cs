/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. 
 * 
 * The Original Code is OpenMCDF - Compound Document Format library.
 * 
 * The Initial Developer of the Original Code is Federico Blaseotto.*/

using System;


namespace OpenMcdf
{
    /// <summary>
    /// OLE structured storage <see cref="T:OpenMcdf.CFStream">stream</see> Object
    /// It is contained inside a Storage object in a file-directory
    /// relationship and indexed by its name.
    /// </summary>
    public class ReadonlyCompoundFileStream : ReadonlyCompoundFileItem
    {
        internal ReadonlyCompoundFileStream(ReadonlyCompoundFile compoundFile, IDirectoryEntry dirEntry)
            : base(compoundFile)
        {
            if (dirEntry == null || dirEntry.SID < 0)
            {
                throw new CFException("Attempting to add a CFStream using an unitialized directory");
            }

            this.DirEntry = dirEntry;
        }

        /// <summary>
        /// Get all the data associated with the stream object.
        /// </summary>
        /// <example>
        /// <code>
        ///     CompoundFile cf2 = new CompoundFile("AFileName.cfs");
        ///     CFStream st = cf2.RootStorage.GetStream("MyStream");
        ///     byte[] buffer = st.ReadAll();
        /// </code>
        /// </example>
        /// <returns>Array of byte containing stream data</returns>
        /// <exception cref="T:OpenMcdf.CFDisposedException">
        /// Raised when the owner compound file has been closed.
        /// </exception>
        public Byte[] GetData()
        {
            CheckDisposed();

            return this.CompoundFile.GetData(this);
        }


        /// <summary>
        /// Read <paramref name="count"/> bytes associated with the stream object, starting from
        /// the provided <paramref name="position"/>. Method returns the effective count of bytes 
        /// read.
        /// </summary>
        /// <param name="buffer">Array of bytes that will contain stream data</param>
        /// <param name="position">The zero-based byte position in the stream at which to begin reading
        /// the data from.</param>
        /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
        /// <returns>The count of bytes effectively read</returns>
        /// <remarks>Method may read a number of bytes lesser then the requested one.</remarks>
        /// <code>
        ///  CompoundFile cf = null;
        ///  byte[] b = Helpers.GetBuffer(1024 * 2, 0xAA); //2MB buffer
        ///  CFStream item = cf.RootStorage.GetStream("AStream");
        ///
        ///  cf = new CompoundFile("$AFILENAME.cfs", CFSUpdateMode.ReadOnly, CFSConfiguration.Default);
        ///  item = cf.RootStorage.GetStream("AStream");
        ///
        ///  byte[] buffer = new byte[2048];
        ///  item.Read(buffer, 0, 2048);
        ///  Assert.IsTrue(Helpers.CompareBuffer(b, buffer));
        /// </code>
        /// </example>
        /// <exception cref="T:OpenMcdf.CFDisposedException">
        /// Raised when the owner compound file has been closed.
        /// </exception>
        public int Read(byte[] buffer, long position, int count)
        {
            CheckDisposed();
            return this.CompoundFile.ReadData(this, position, buffer, 0, count);
        }


        /// <summary>
        /// Read <paramref name="count"/> bytes associated with the stream object, starting from
        /// a provided <paramref name="position"/>. Method returns the effective count of bytes 
        /// read.
        /// </summary>
        /// <param name="buffer">Array of bytes that will contain stream data</param>
        /// <param name="position">The zero-based byte position in the stream at which to begin reading
        /// the data from.</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream. </param>
        /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
        /// <returns>The count of bytes effectively read</returns>
        /// <remarks>Method may read a number of bytes lesser then the requested one.</remarks>
        /// <code>
        ///  CompoundFile cf = null;
        ///  byte[] b = Helpers.GetBuffer(1024 * 2, 0xAA); //2MB buffer
        ///  CFStream item = cf.RootStorage.GetStream("AStream");
        ///
        ///  cf = new CompoundFile("$AFILENAME.cfs", CFSUpdateMode.ReadOnly, CFSConfiguration.Default);
        ///  item = cf.RootStorage.GetStream("AStream");
        ///
        ///  byte[] buffer = new byte[2048];
        ///  item.Read(buffer, 0, 2048);
        ///  Assert.IsTrue(Helpers.CompareBuffer(b, buffer));
        /// </code>
        /// </example>
        /// <exception cref="T:OpenMcdf.CFDisposedException">
        /// Raised when the owner compound file has been closed.
        /// </exception>
        internal int Read(byte[] buffer, long position, int offset, int count)
        {
            CheckDisposed();
            return this.CompoundFile.ReadData(this, position, buffer, offset, count);
        }
    }
}