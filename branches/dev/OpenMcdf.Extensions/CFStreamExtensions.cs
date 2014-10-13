using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace OpenMcdf.Extensions
{
    public static class CFStreamExtension
    {
        private class StreamDecorator : Stream
        {
            private CFStream cfStream;
            private long position = 0;

            public StreamDecorator(CFStream cfstream)
            {
                this.cfStream = cfstream;
            }

            public override bool CanRead
            {
                get { return true; }
            }

            public override bool CanSeek
            {
                get { return true; }
            }

            public override bool CanWrite
            {
                get { return true; }
            }

            public override void Flush()
            {
                // nothing to do;
            }

            public override long Length
            {
                get { return cfStream.Size; }
            }

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

                count = this.cfStream.Read(buffer, position, offset, count);
                position += count;
                return count;
            }

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

            public override void SetLength(long value)
            {
                this.cfStream.Resize(value);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                this.cfStream.Write(buffer, position, offset, count);
                position += count;
            }

            public override void Close()
            {
                // Do nothing
            }
        }

        /// <summary>
        /// Return the current <see cref="T:OpenMcdf.CFStream">CFStream</see> object 
        /// as a <see cref="T:System.IO.Stream">Stream</see> object.
        /// </summary>
        /// <param name="cfStream">Current <see cref="T:OpenMcdf.CFStream">CFStream</see> object</param>
        /// <returns>A <see cref="T:System.IO.Stream">Stream</see> object representing structured stream data</returns>
        public static Stream AsIOStream(this CFStream cfStream)
        {
            return new StreamDecorator(cfStream);
        }
    }
}
