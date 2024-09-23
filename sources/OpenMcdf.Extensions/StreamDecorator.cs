using System;
using System.IO;

namespace OpenMcdf.Extensions
{
    /// <summary>
    /// A wrapper class to present a <see cref="CFStream"/> as a <see cref="Stream"/>.
    /// </summary>
    public class StreamDecorator : Stream
    {
        private readonly CFStream cfStream;
        private long position = 0;

        /// <summary>
        /// Create a new <see cref="StreamDecorator"/> for the specified <seealso cref="CFStream"/>.
        /// </summary>
        /// <param name="cfstream">The <see cref="CFStream"/> being wrapped.</param>
        public StreamDecorator(CFStream cfstream)
        {
            cfStream = cfstream;
        }

        /// <inheritdoc/>
        public override bool CanRead => true;

        /// <inheritdoc/>
        public override bool CanSeek => true;

        /// <inheritdoc/>
        public override bool CanWrite => true;

        /// <inheritdoc/>
        public override void Flush()
        {
            // nothing to do;
        }

        /// <inheritdoc/>
        public override long Length => cfStream.Size;

        /// <inheritdoc/>
        public override long Position
        {
            get
            {
                return position;
            }
            set
            {
                position = value;
            }
        }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (count > buffer.Length)
                throw new ArgumentException("Count parameter exceeds buffer size");

            if (buffer == null)
                throw new ArgumentNullException("Buffer cannot be null");

            if (offset < 0 || count < 0)
                throw new ArgumentOutOfRangeException("Offset and Count parameters must be non-negative numbers");

            if (position >= cfStream.Size)
                return 0;

            count = cfStream.Read(buffer, position, offset, count);
            position += count;
            return count;
        }

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    position = offset;
                    break;
                case SeekOrigin.Current:
                    position += offset;
                    break;
                case SeekOrigin.End:
                    position -= offset;
                    break;
                default:
                    throw new Exception("Invalid origin selected");
            }

            return position;
        }

        /// <inheritdoc/>
        public override void SetLength(long value)
        {
            cfStream.Resize(value);
        }

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            cfStream.Write(buffer, position, offset, count);
            position += count;
        }

        /// <inheritdoc/>
        public override void Close()
        {
            // Do nothing
        }
    }
}
