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
    readonly StorageModeFlags storageModeFlags;

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
        return new RootStorage(ioContext, flags);
    }

    public static RootStorage Open(string fileName, FileMode mode, StorageModeFlags flags = StorageModeFlags.None)
    {
        FileStream stream = File.Open(fileName, mode);
        return Open(stream);
    }

    public static RootStorage OpenRead(string fileName, StorageModeFlags flags = StorageModeFlags.None)
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
        return new RootStorage(ioContext, flags);
    }

    RootStorage(IOContext ioContext, StorageModeFlags storageModeFlags)
        : base(ioContext, ioContext.RootEntry)
    {
        this.storageModeFlags = storageModeFlags;
    }

    public void Dispose() => ioContext?.Dispose();

    public void Flush(bool consolidate = false)
    {
        this.ThrowIfDisposed(ioContext.IsDisposed);

        ioContext.Flush();

        if (consolidate)
            Consolidate();
    }

    void Consolidate()
    {
        // TODO: Consolidate by defragmentation instead of copy

        Stream? destinationStream = null;

        try
        {
            if (ioContext.BaseStream is MemoryStream)
                destinationStream = new MemoryStream((int)ioContext.BaseStream.Length);
            else if (ioContext.BaseStream is FileStream)
                destinationStream = File.Create(Path.GetTempFileName());
            else
                throw new NotSupportedException("Unsupported stream type for consolidation.");

            using (RootStorage destinationStorage = Create(destinationStream, ioContext.Version, storageModeFlags))
                CopyTo(destinationStorage);

            ioContext.BaseStream.Position = 0;
            destinationStream.Position = 0;

            destinationStream.CopyTo(ioContext.BaseStream);
            ioContext.BaseStream.SetLength(destinationStream.Length);
        }
        catch
        {
            if (destinationStream is FileStream fs)
            {
                string fileName = fs.Name;
                fs.Dispose();
                File.Delete(fileName);
            }
        }
    }

    public void Commit()
    {
        this.ThrowIfDisposed(ioContext.IsDisposed);

        ioContext.Commit();
    }

    public void Revert()
    {
        this.ThrowIfDisposed(ioContext.IsDisposed);

        ioContext.Revert();
    }

    internal void Trace(TextWriter writer)
    {
        writer.WriteLine(ioContext.Header);
        ioContext.Fat.WriteTrace(writer);
        ioContext.MiniFat.Trace(writer);
    }

    internal void Validate()
    {
        ioContext.Fat.Validate();
        ioContext.MiniFat.Validate();
    }
}
