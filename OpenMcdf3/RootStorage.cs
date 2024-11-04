namespace OpenMcdf3;

public enum Version : ushort
{
    Unknown = 0,
    V3 = 3,
    V4 = 4
}

[Flags]
public enum StorageModeFlags
{
    None = 0,
    LeaveOpen = 0x01,
    Transacted = 0x02,
}

/// <summary>
/// Encapsulates the root <see cref="Storage"/> of a compound file.
/// </summary>
public sealed class RootStorage : Storage, IDisposable
{
    public static RootStorage Create(string fileName, Version version = Version.V3, StorageModeFlags flags = StorageModeFlags.None)
    {
        FileStream stream = File.Create(fileName);
        return Create(stream, version);
    }

    public static RootStorage Create(Stream stream, Version version = Version.V3, StorageModeFlags flags = StorageModeFlags.None)
    {
        stream.ThrowIfNotSeekable();
        stream.SetLength(0);
        stream.Position = 0;

        IOContextFlags contextFlags = IOContextFlags.Create;
        if (flags.HasFlag(StorageModeFlags.LeaveOpen))
            contextFlags |= IOContextFlags.LeaveOpen;
        if (flags.HasFlag(StorageModeFlags.Transacted))
            contextFlags |= IOContextFlags.Transacted;

        IOContext ioContext = new(stream, version, contextFlags);
        return new RootStorage(ioContext);
    }

    public static RootStorage Open(string fileName, FileMode mode, StorageModeFlags flags = StorageModeFlags.None)
    {
        FileStream stream = File.Open(fileName, mode);
        return Open(stream);
    }

    public static RootStorage OpenRead(string fileName)
    {
        FileStream stream = File.OpenRead(fileName);
        return Open(stream);
    }

    public static RootStorage Open(Stream stream, StorageModeFlags flags = StorageModeFlags.None)
    {
        stream.ThrowIfNotSeekable();
        stream.Position = 0;

        IOContextFlags contextFlags = IOContextFlags.None;
        if (flags.HasFlag(StorageModeFlags.LeaveOpen))
            contextFlags |= IOContextFlags.LeaveOpen;
        if (flags.HasFlag(StorageModeFlags.Transacted))
            contextFlags |= IOContextFlags.Transacted;

        IOContext ioContext = new(stream, Version.Unknown, contextFlags);
        return new RootStorage(ioContext);
    }

    RootStorage(IOContext ioContext)
        : base(ioContext, ioContext.RootEntry)
    {
    }

    public void Dispose() => ioContext?.Dispose();

    public void Commit()
    {
        ioContext.Commit();
    }

    public void Revert()
    {
        ioContext.Revert();
    }

    internal void Trace(TextWriter writer)
    {
        writer.WriteLine(ioContext.Header);
        ioContext.Fat.Trace(writer);
        ioContext.MiniFat.Trace(writer);
    }
}
