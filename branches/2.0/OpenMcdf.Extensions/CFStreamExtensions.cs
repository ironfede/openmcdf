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
                if (position >= cfStream.Size)
                    return 0;

                count = this.cfStream.GetData(buffer, position, count);
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
                        throw new CFException("Invalid origin selected");
                }

                return position;
            }

            public override void SetLength(long value)
            {
                if (value > this.cfStream.Size)
                {
                    long sizeDelta = cfStream.Size - value;
                    byte[] dataDelta = new byte[sizeDelta];
                    this.cfStream.AppendData(dataDelta);
                }
                else if (value < this.cfStream.Size)
                {
                    byte[] data = this.cfStream.GetData();
                    byte[] newData = new byte[value];

                    Buffer.BlockCopy(data, 0, newData, 0, (int)value);
                    this.cfStream.SetData(newData);
                }
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                byte[] data = new byte[count];

                Buffer.BlockCopy(buffer, offset, data, 0, count);
                this.cfStream.SetData(data, position);
                position += count;
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
