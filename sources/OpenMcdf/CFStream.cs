/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. 
 * 
 * The Original Code is OpenMCDF - Compound Document Format library.
 * 
 * The Initial Developer of the Original Code is Federico Blaseotto.*/

using System;
using System.IO;



namespace OpenMcdf
{

    /// <summary>
    /// OLE structured storage <see cref="T:OpenMcdf.CFStream">stream</see> Object
    /// It is contained inside a Storage object in a file-directory
    /// relationship and indexed by its name.
    /// </summary>
    public class CFStream : CFItem
    {
        internal CFStream(CompoundFile compoundFile, IDirectoryEntry dirEntry)
            : base(compoundFile)
        {
            if (dirEntry == null || dirEntry.SID < 0)
                throw new CFException("Attempting to add a CFStream using an unitialized directory");
            
            this.DirEntry = dirEntry;
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
        /// <remarks>Existing associated data will be lost after method invocation</remarks>
        public void SetData(Byte[] data)
        {
            CheckDisposed();

            this.CompoundFile.FreeData(this);
            this.CompoundFile.WriteData(this, data);
        }


        /// <summary>
        /// Write a data buffer to a specific position into current CFStream object
        /// </summary>
        /// <param name="data">Data buffer to Write</param>
        /// <param name="position">Position into the stream object to start writing from</param>
        /// <remarks>Current stream will be extended to receive data buffer over 
        /// its current size</remarks>
        public void Write(byte[] data, long position)
        {
            this.Write(data, position, 0, data.Length);
        }

        /// <summary>
        /// Write <paramref name="count">count</paramref> bytes of a data buffer to a specific position into 
        /// the current CFStream object starting from the specified position.
        /// </summary>
        /// <param name="data">Data buffer to copy bytes from</param>
        /// <param name="position">Position into the stream object to start writing from</param>
        /// <param name="offset">The zero-based byte offset in buffer at which to 
        /// begin copying bytes to the current <see cref="T:OpenMcdf.CFStream">CFStream</see>. </param>
        /// <param name="count">The number of bytes to be written to the current <see cref="T:OpenMcdf.CFStream">CFStream</see> </param>
        /// <remarks>Current stream will be extended to receive data buffer over 
        /// its current size.</remarks>
        internal void Write(byte[] data, long position, int offset, int count)
        {
            CheckDisposed();
            this.CompoundFile.WriteData(this, data, position, offset, count);
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
        public void Append(Byte[] data)
        {
            CheckDisposed();
            if (this.Size > 0)
            {
                this.CompoundFile.AppendData(this, data);
            }
            else
            {
                this.CompoundFile.WriteData(this, data);
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

            byte[] buffer = new byte[input.Length];

            if (input.CanSeek)
            {
                input.Seek(0, SeekOrigin.Begin);
            }

            input.Read(buffer, 0, (int)input.Length);
            this.SetData(buffer);
        }


        /// <summary>
        /// Resize stream padding with zero if enlarging, trimming data if reducing size.
        /// </summary>
        /// <param name="length">New length to assign to this stream</param>
        public void Resize(long length)
        {
            this.CompoundFile.SetStreamLength(this, length);
        }
    }
}
