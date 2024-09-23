using Be.Windows.Forms;
using OpenMcdf;
using System;

namespace StructuredStorageExplorer
{
    public class StreamDataProvider : IByteProvider
    {
        /// <summary>
        /// Modifying stream
        /// </summary>
        readonly CFStream _modifiedStream;

        /// <summary>
        /// Contains information about changes.
        /// </summary>
        bool _hasChanges;

        /// <summary>
        /// Initializes a new instance of the StreamDataProvider class.
        /// </summary>
        /// <param name="modifiedStream"></param>
        public StreamDataProvider(CFStream modifiedStream)
        {
            Bytes = new ByteCollection(modifiedStream.GetData());
            _modifiedStream = modifiedStream;
        }

        /// <summary>
        /// Raises the Changed event.
        /// </summary>
        void OnChanged(EventArgs e)
        {
            _hasChanges = true;

            Changed?.Invoke(this, e);
        }

        /// <summary>
        /// Raises the LengthChanged event.
        /// </summary>
        void OnLengthChanged(EventArgs e)
        {
            LengthChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Gets the byte collection.
        /// </summary>
        public ByteCollection Bytes { get; }

        #region IByteProvider Members
        /// <summary>
        /// True, when changes are done.
        /// </summary>
        public bool HasChanges()
        {
            return _hasChanges;
        }

        /// <summary>
        /// Applies changes.
        /// </summary>
        public void ApplyChanges()
        {
            _hasChanges = false;

            _modifiedStream.SetData(Bytes.ToArray());
        }

        /// <summary>
        /// Occurs, when the write buffer contains new changes.
        /// </summary>
        public event EventHandler Changed;

        /// <summary>
        /// Occurs, when InsertBytes or DeleteBytes method is called.
        /// </summary>
        public event EventHandler LengthChanged;

        /// <summary>
        /// Reads a byte from the byte collection.
        /// </summary>
        /// <param name="index">the index of the byte to read</param>
        /// <returns>the byte</returns>
        public byte ReadByte(long index)
        { return Bytes[(int)index]; }

        /// <summary>
        /// Write a byte into the byte collection.
        /// </summary>
        /// <param name="index">the index of the byte to write.</param>
        /// <param name="value">the byte</param>
        public void WriteByte(long index, byte value)
        {
            Bytes[(int)index] = value;
            OnChanged(EventArgs.Empty);
        }

        /// <summary>
        /// Deletes bytes from the byte collection.
        /// </summary>
        /// <param name="index">the start index of the bytes to delete.</param>
        /// <param name="length">the length of bytes to delete.</param>
        public void DeleteBytes(long index, long length)
        {
            int internal_index = (int)Math.Max(0, index);
            int internal_length = (int)Math.Min((int)Length, length);
            Bytes.RemoveRange(internal_index, internal_length);

            OnLengthChanged(EventArgs.Empty);
            OnChanged(EventArgs.Empty);
        }

        /// <summary>
        /// Inserts byte into the byte collection.
        /// </summary>
        /// <param name="index">the start index of the bytes in the byte collection</param>
        /// <param name="bs">the byte array to insert</param>
        public void InsertBytes(long index, byte[] bs)
        {
            Bytes.InsertRange((int)index, bs);

            OnLengthChanged(EventArgs.Empty);
            OnChanged(EventArgs.Empty);
        }

        /// <summary>
        /// Gets the length of the bytes in the byte collection.
        /// </summary>
        public long Length => Bytes.Count;

        /// <summary>
        /// Returns true
        /// </summary>
        public bool SupportsWriteByte()
        {
            return true;
        }

        /// <summary>
        /// Returns true
        /// </summary>
        public bool SupportsInsertBytes()
        {
            return true;
        }

        /// <summary>
        /// Returns true
        /// </summary>
        public bool SupportsDeleteBytes()
        {
            return true;
        }
        #endregion
    }
}
