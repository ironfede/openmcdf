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
            get => position;
            set => Seek(value, SeekOrigin.Begin);
        }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), "Offset must be a non-negative number");

            if ((uint)count > buffer.Length - offset)
                throw new ArgumentException("Offset and length were out of bounds for the array or count is greater than the number of elements from index to the end of the source collection");

            if (position >= cfStream.Size)
                return 0;

            int maxReadableLength = (int)Math.Min(int.MaxValue, Length - Position);
            count = Math.Max(0, Math.Min(maxReadableLength, count));
            if (count == 0)
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
                    if (offset < 0)
                        throw new IOException("Seek before origin");
                    position = offset;
                    break;

                case SeekOrigin.Current:
                    if (position + offset < 0)
                        throw new IOException("Seek before origin");
                    position += offset;
                    break;

                case SeekOrigin.End:
                    if (Length - offset < 0)
                        throw new IOException("Seek before origin");
                    position = Length - offset;
                    break;

                default:
                    throw new ArgumentException("Invalid seek origin", nameof(origin));
            }

            if (position > Length)
                SetLength(position);

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
