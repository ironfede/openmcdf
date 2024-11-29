using System.Diagnostics.CodeAnalysis;

namespace OpenMcdf;

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

    private static void ThrowIfInvalid(FileMode mode)
    {
        if (mode is FileMode.Append)
            throw new ArgumentException("Append mode is not valid for compound files.", nameof(mode));
    }

    private static void ThrowIfInvalid(FileAccess access)
    {
        if (access is FileAccess.Write)
            throw new ArgumentException("Write-only access is not valid for compound files.", nameof(access));
    }

    private static void ThrowIfLeaveOpen(StorageModeFlags flags)
    {
        if (flags.HasFlag(StorageModeFlags.LeaveOpen))
            throw new ArgumentException($"{StorageModeFlags.LeaveOpen} is not valid for files");
    }

    private static IOContextFlags ToIOContextFlags(StorageModeFlags flags)
    {
        IOContextFlags contextFlags = IOContextFlags.None;
        if (flags.HasFlag(StorageModeFlags.LeaveOpen))
            contextFlags |= IOContextFlags.LeaveOpen;
        if (flags.HasFlag(StorageModeFlags.Transacted))
            contextFlags |= IOContextFlags.Transacted;
        return contextFlags;
    }

    public static RootStorage Create(string fileName, Version version = Version.V3, StorageModeFlags flags = StorageModeFlags.None)
    {
        if (fileName is null)
            throw new ArgumentNullException(nameof(fileName));

        ThrowIfLeaveOpen(flags);

        FileStream stream = File.Create(fileName);
        return Create(stream, version, flags);
    }

    public static RootStorage Create(Stream stream, Version version = Version.V3, StorageModeFlags flags = StorageModeFlags.None)
    {
        if (stream is null)
            throw new ArgumentNullException(nameof(stream));

        stream.ThrowIfNotSeekable();
        stream.SetLength(0);
        stream.Position = 0;

        IOContextFlags contextFlags = ToIOContextFlags(flags) | IOContextFlags.Create;
        RootContextSite rootContextSite = new();
        _ = new RootContext(rootContextSite, stream, version, contextFlags);
        return new RootStorage(rootContextSite, flags);
    }

    public static RootStorage CreateInMemory(Version version = Version.V3) => Create(new MemoryStream(), version);

    public static RootStorage Open(string fileName, FileMode mode, StorageModeFlags flags = StorageModeFlags.None)
    {
        if (fileName is null)
            throw new ArgumentNullException(nameof(fileName));

        ThrowIfInvalid(mode);
        ThrowIfLeaveOpen(flags);

        FileStream stream = File.Open(fileName, mode);
        return Open(stream, flags);
    }

    public static RootStorage Open(string fileName, FileMode mode, FileAccess access, StorageModeFlags flags = StorageModeFlags.None)
    {
        if (fileName is null)
            throw new ArgumentNullException(nameof(fileName));

        ThrowIfInvalid(mode);
        ThrowIfInvalid(access);
        ThrowIfLeaveOpen(flags);

        FileStream stream = File.Open(fileName, mode, access);
        return Open(stream, flags);
    }

    public static RootStorage Open(Stream stream, StorageModeFlags flags = StorageModeFlags.None)
    {
        if (stream is null)
            throw new ArgumentNullException(nameof(stream));

        stream.ThrowIfNotSeekable();
        stream.Position = 0;

        IOContextFlags contextFlags = ToIOContextFlags(flags);
        RootContextSite rootContextSite = new();
        _ = new RootContext(rootContextSite, stream, Version.Unknown, contextFlags);
        return new RootStorage(rootContextSite, flags);
    }

    public static RootStorage OpenRead(string fileName, StorageModeFlags flags = StorageModeFlags.None)
    {
        if (fileName is null)
            throw new ArgumentNullException(nameof(fileName));

        ThrowIfLeaveOpen(flags);

        FileStream stream = File.OpenRead(fileName);
        return Open(stream, flags);
    }

    RootStorage(RootContextSite rootContextSite, StorageModeFlags storageModeFlags)
        : base(rootContextSite, rootContextSite.Context.RootEntry, null)
    {
        this.storageModeFlags = storageModeFlags;
    }

    public void Dispose() => Context.Dispose();

    public Stream BaseStream => Context.BaseStream;

    public void Flush(bool consolidate = false)
    {
        this.ThrowIfDisposed(Context.IsDisposed);

        Context.Flush();

        if (consolidate)
            Consolidate();
    }

    void Consolidate()
    {
        // TODO: Consolidate by defragmentation instead of copy

        Stream baseStream = Context.BaseStream;
        Stream? destinationStream = null;

        try
        {
            if (baseStream is MemoryStream)
                destinationStream = new MemoryStream((int)baseStream.Length);
            else if (baseStream is FileStream)
                destinationStream = File.Create(Path.GetTempFileName());
            else
                throw new NotSupportedException("Unsupported stream type for consolidation.");

            using (RootStorage destinationStorage = Create(destinationStream, Context.Version, StorageModeFlags.LeaveOpen))
                CopyTo(destinationStorage);

            destinationStream.CopyAllTo(baseStream);

            IOContextFlags contextFlags = ToIOContextFlags(storageModeFlags);
            _ = new RootContext(ContextSite, baseStream, Version.Unknown, contextFlags);
        }
        catch
        {
            destinationStream?.Dispose();

            if (destinationStream is FileStream fs)
            {
                string fileName = fs.Name;
                File.Delete(fileName);
            }

            throw;
        }
    }

    public void Commit()
    {
        this.ThrowIfDisposed(Context.IsDisposed);

        Context.Commit();
    }

    public void Revert()
    {
        this.ThrowIfDisposed(Context.IsDisposed);

        Context.Revert();
    }

    private void SwitchToCore(Stream stream, bool allowLeaveOpen)
    {
        Flush();

        stream.SetLength(Context.BaseStream.Length);

        stream.Position = 0;
        Context.BaseStream.Position = 0;

        Context.BaseStream.CopyTo(stream);
        stream.Position = 0;

        Context.Dispose();

        IOContextFlags contextFlags = ToIOContextFlags(storageModeFlags);
        if (!allowLeaveOpen)
            contextFlags &= ~IOContextFlags.LeaveOpen;
        _ = new RootContext(ContextSite, stream, Version.Unknown, contextFlags);
    }

    public void SwitchTo(Stream stream)
    {
        if (stream is null)
            throw new ArgumentNullException(nameof(stream));

        SwitchToCore(stream, true);
    }

    public void SwitchTo(string fileName)
    {
        if (fileName is null)
            throw new ArgumentNullException(nameof(fileName));

        FileStream stream = File.Create(fileName);
        SwitchToCore(stream, false);
    }

    [ExcludeFromCodeCoverage]
    internal void Trace(TextWriter writer)
    {
        writer.WriteLine(Context.Header);
        Context.Fat.WriteTrace(writer);
        Context.MiniFat.WriteTrace(writer);
    }

    [ExcludeFromCodeCoverage]
    internal void Validate()
    {
        Context.Fat.Validate();
        Context.MiniFat.Validate();
    }
}
