/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 *
 * The Original Code is OpenMCDF - Compound Document Format library.
 *
 * The Initial Developer of the Original Code is Federico Blaseotto.*/

using System.IO;
using System.Reflection.Metadata.Ecma335;

namespace OpenMcdf3
{
    /// <summary>
    /// OLE structured storage <see cref="T:OpenMcdf.CFStream">stream</see> Object
    /// It is contained inside a Storage object in a file-directory
    /// relationship and indexed by its name.
    /// </summary>
    public class CFStream : Stream, IHasDirectoryEntry
    {
        private long position = 0;
        private DirectoryEntry entry;
        private CompoundFile compoundFile;

        DirectoryEntry IHasDirectoryEntry.DirectoryEntry => entry;

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public override long Length => entry.Size;

        public override long Position { get => position; set { position = value; Seek(value, SeekOrigin.Begin); } }

        //DirectoryEntry IHasDirectoryEntry.DirectoryEntry => throw new NotImplementedException();

        internal CFStream(CompoundFile compoundFile, DirectoryEntry dirEntry) : base()

        {
            if (dirEntry == null || dirEntry.SID < 0)
                throw new CFException("Attempting to add a CFStream using an uninitialized directory");

            this.entry = dirEntry;
            this.compoundFile = compoundFile;

        }


        /// <summary>
        /// Append the provided data to stream data.
        /// </summary>
        /// <example>
        /// <code>
        ///    byte[] b = new byte[]{0x0,0x1,0x2,0x3};
        ///    byte[] b2 = new byte[]{0x4,0x5,0x6,0x7};
        ///    CompoundFile cf = new CompoundFile();
        ///    CFStream myStream = cf.RootStorage.AddStream("MyStream");
        ///    myStream.SetData(b); // here we could also have invoked .AppendData
        ///    myStream.AppendData(b2);
        ///    cf.Save("MyLargeStreamsFile.cfs);
        ///    cf.Close();
        /// </code>
        /// </example>
        /// <param name="data">Data bytes to append to this stream</param>
        /// <remarks>
        /// This method allows user to create stream with more than 2GB of data,
        /// appending data to the end of existing ones.
        /// Large streams (>2GB) are only supported by CFS version 4.
        /// Append data can also be invoked on streams with no data in order
        /// to simplify its use inside loops.
        /// </remarks>
        public void Append(byte[] data)
        {
            CheckDisposed();
            if (Size > 0)
            {
                CompoundFile.AppendData(this, data);
            }
            else
            {
                CompoundFile.WriteData(this, data);
            }
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
        public byte[] GetData()
        {
            CheckDisposed();

            return CompoundFile.GetData(DirEntry);
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
        ///  CollectionAssert.AreEqual(b, buffer));
        /// </code>
        /// </example>
        /// <exception cref="T:OpenMcdf.CFDisposedException">
        /// Raised when the owner compound file has been closed.
        /// 
        /// </exception>
        public int Read(byte[] buffer, long position, int count)
        {
            CheckDisposed();
            return CompoundFile.ReadData(DirEntry, position, buffer, 0, count);
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
        ///  CollectionAssert.AreEqual(b, buffer);
        /// </code>
        /// </example>
        /// <exception cref="T:OpenMcdf.CFDisposedException">
        /// Raised when the owner compound file has been closed.
        /// </exception>
        internal int Read(byte[] buffer, long position, int offset, int count)
        {
            CheckDisposed();
            return CompoundFile.ReadData(DirEntry, position, buffer, offset, count);
        }

        /// <summary>
        /// Copy data from an existing stream.
        /// </summary>
        /// <param name="input">A stream to read from</param>
        /// <remarks>
        /// Input stream will NOT be closed after method invocation.
        /// Existing associated data will be deleted.
        /// </remarks>
        public void CopyFrom(Stream input)
        {
            CheckDisposed();

            if (input.CanSeek)
            {
                input.Seek(0, SeekOrigin.Begin);
            }

            byte[] buffer = new byte[input.Length];
            input.ReadExactly(buffer, 0, buffer.Length);
            SetData(buffer);
        }

        /// <summary>
        /// Resize stream padding with zero if enlarging, trimming data if reducing size.
        /// </summary>
        /// <param name="length">New length to assign to this stream</param>
        public void Resize(long length)
        {
            CompoundFile.SetStreamLength(this, length);
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            compoundFile.WriteToChain(entry.SID, buffer, offset, count);
        }
    }
}
