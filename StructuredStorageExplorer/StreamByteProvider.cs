using Be.Windows.Forms;
using OpenMcdf;

namespace StructuredStorageExplorer;

internal sealed class StreamByteProvider : IByteProvider, IDisposable
{
    CfbStream _stream;

    bool _hasChanges;

    public ByteCollection Bytes { get; private set; }

    public StreamByteProvider(CfbStream stream)
    {
        byte[] data = new byte[stream.Length];
        stream.Position = 0;
        stream.ReadExactly(data, 0, data.Length);
        Bytes = new ByteCollection(data);
        _stream = stream;
    }

    public void Dispose()
    {
        _stream.Dispose();
    }

    public void CopyFrom(Stream stream)
    {
        byte[] data = new byte[stream.Length];
        stream.Position = 0;
        stream.ReadExactly(data, 0, data.Length);
        Bytes = new ByteCollection(data);
        OnLengthChanged(EventArgs.Empty);
        OnChanged(EventArgs.Empty);
        _hasChanges = false;
    }

    public void CopyTo(Stream stream)
    {
        _stream.Position = 0;
        _stream.CopyTo(stream);
    }

    void OnChanged(EventArgs e)
    {
        _hasChanges = true;

        Changed?.Invoke(this, e);
    }

    void OnLengthChanged(EventArgs e)
    {
        LengthChanged?.Invoke(this, e);
    }

    #region IByteProvider Members
    public bool HasChanges() => _hasChanges;

    public void ApplyChanges()
    {
        _stream.Position = 0;
        _stream.Write(Bytes.ToArray());

        _hasChanges = false;
    }

    public event EventHandler? Changed;

    public event EventHandler? LengthChanged;

    public byte ReadByte(long index) => Bytes[(int)index];

    public void WriteByte(long index, byte value)
    {
        Bytes[(int)index] = value;
        OnChanged(EventArgs.Empty);
    }

    public void DeleteBytes(long index, long length)
    {
        int internal_index = (int)Math.Max(0, index);
        int internal_length = (int)Math.Min((int)Length, length);
        Bytes.RemoveRange(internal_index, internal_length);

        OnLengthChanged(EventArgs.Empty);
        OnChanged(EventArgs.Empty);
    }

    public void InsertBytes(long index, byte[] bs)
    {
        Bytes.InsertRange((int)index, bs);

        OnLengthChanged(EventArgs.Empty);
        OnChanged(EventArgs.Empty);
    }

    public long Length => Bytes.Count;

    public bool SupportsWriteByte() => true;

    public bool SupportsInsertBytes() => true;

    public bool SupportsDeleteBytes() => true;
    #endregion
}
