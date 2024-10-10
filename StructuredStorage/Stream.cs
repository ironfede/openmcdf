using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;

namespace StructuredStorage;

/// <summary>
/// Implements <c>Stream</c> on an COM <c>IStream</c>.
/// </summary>
public sealed class Stream : System.IO.Stream
{
    public Storage Parent { get; }

    readonly IStream stream;
    bool disposed;

    internal Stream(IStream stream, Storage parent)
    {
        this.stream = stream;
        Parent = parent;

        STGM mode = Stat.grfMode;
        CanRead = mode.HasFlag(STGM.STGM_READWRITE) || !mode.HasFlag(STGM.STGM_WRITE);
        CanWrite = mode.HasFlag(STGM.STGM_READWRITE) || mode.HasFlag(STGM.STGM_WRITE);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposed)
            return;

        if (disposing)
        {
            Flush();

            int count = Marshal.ReleaseComObject(stream);
            Debug.Assert(count == 0);
        }

        disposed = true;

        base.Dispose(disposing);
    }

    public override void Flush() => Flush(CommitFlags.Default);

    public void Flush(CommitFlags flags)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        stream.Commit((STGC)flags);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        Span<byte> slice = buffer.AsSpan(offset, count);
        return Read(slice);
    }

    public override unsafe int Read(Span<byte> buffer)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        fixed (byte* ptr = buffer)
        {
            uint read;
            HRESULT hr = stream.Read(ptr, (uint)buffer.Length, &read);
            hr.ThrowOnFailure();
            return (int)read;
        }
    }

    public void Revert()
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        stream.Revert();
    }

    public override unsafe long Seek(long offset, SeekOrigin origin)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        ulong pos;
        stream.Seek(offset, origin, &pos);
        return (long)pos;
    }

    public override void SetLength(long value)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        stream.SetSize((ulong)value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        ReadOnlySpan<byte> slice = buffer.AsSpan(offset, count);
        Write(slice);
    }

    public override unsafe void Write(ReadOnlySpan<byte> buffer)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        fixed (byte* ptr = buffer)
        {
            uint written;
            HRESULT result = stream.Write(ptr, (uint)buffer.Length, &written);
            result.ThrowOnFailure();
        }
    }

    // Properties
    public override bool CanRead { get; }

    public override bool CanSeek => true;

    public override bool CanWrite { get; }

    public override long Length
    {
        get
        {
            ObjectDisposedException.ThrowIf(disposed, this);

            return (long)Stat.cbSize;
        }
    }

    public override unsafe long Position
    {
        get
        {
            ObjectDisposedException.ThrowIf(disposed, this);

            ulong pos;
            stream.Seek(0L, SeekOrigin.Current, &pos);
            return (long)pos;
        }

        set
        {
            ObjectDisposedException.ThrowIf(disposed, this);

            stream.Seek(value, SeekOrigin.Begin, null);
        }
    }

    public Guid Id
    {
        get
        {
            ObjectDisposedException.ThrowIf(disposed, this);

            HRESULT hr = PInvoke.ReadClassStm(stream, out Guid guid);
            hr.ThrowOnFailure();
            return guid;
        }

        set
        {
            ObjectDisposedException.ThrowIf(disposed, this);

            HRESULT hr = PInvoke.WriteClassStm(stream, value);
            hr.ThrowOnFailure();
        }
    }

    internal unsafe STATSTG Stat
    {
        get
        {
            STATSTG stat;
            stream.Stat(&stat, STATFLAG.STATFLAG_NONAME);
            return stat;
        }
    }
}
