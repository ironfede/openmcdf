using System.Diagnostics.CodeAnalysis;

namespace OpenMcdf;

/// <summary>
/// Represents the major version of the compound file.
/// </summary>
public enum Version : ushort
{
    Unknown = 0,

    /// <summary>
    /// 512 byte sectors.
    /// </summary>
    V3 = 3,

    /// <summary>
    /// 4096 byte sectors.
    /// </summary>
    V4 = 4
}

/// <summary>
/// Specifies options for configuring the behavior of a compound file root storage.
/// </summary>
[Flags]
public enum StorageModeFlags
{
    /// <summary>
    /// Default mode with no special flags set.
    /// </summary>
    None = 0,

    /// <summary>
    /// Leaves the underlying stream open after the <see cref="RootStorage"/> is disposed.
    /// </summary>
    LeaveOpen = 0x01,

    /// <summary>
    /// Allows the compound file to be used in a transacted context, enabling rollback of changes.
    /// </summary>
    Transacted = 0x02,
}

/// <summary>
/// Encapsulates the root <see cref="Storage"/> of a compound file. Provides methods to create, open, commit, revert, and consolidate compound files.
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
            throw new ArgumentException($"{StorageModeFlags.LeaveOpen} is only valid for injected streams.");
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

    /// <summary>
    /// Creates a new compound file at the specified file path.
    /// </summary>
    /// <param name="fileName">The file path to create the compound file.</param>
    /// <param name="version">The compound file version.</param>
    /// <param name="flags">Flags controlling storage behavior.</param>
    /// <returns>A new <see cref="RootStorage"/> instance.</returns>
    public static RootStorage Create(string fileName, Version version = Version.V3, StorageModeFlags flags = StorageModeFlags.None)
    {
        if (fileName is null)
            throw new ArgumentNullException(nameof(fileName));

        ThrowIfLeaveOpen(flags);

        FileStream stream = File.Create(fileName);
        return Create(stream, version, flags);
    }

    /// <summary>
    /// Creates a new compound file in the specified stream.
    /// </summary>
    /// <param name="stream">The stream to use for the compound file.</param>
    /// <param name="version">The compound file version.</param>
    /// <param name="flags">Flags controlling storage behavior.</param>
    /// <returns>A new <see cref="RootStorage"/> instance.</returns>
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

    /// <summary>
    /// Creates a new in-memory compound file.
    /// </summary>
    /// <param name="version">The compound file version.</param>
    /// <param name="flags">Flags controlling storage behavior.</param>
    /// <returns>A new <see cref="RootStorage"/> instance.</returns>
    public static RootStorage CreateInMemory(Version version = Version.V3, StorageModeFlags flags = StorageModeFlags.None)
    {
        ThrowIfLeaveOpen(flags);

        return Create(new MemoryStream(), version, flags);
    }

    /// <summary>
    /// Opens an existing compound file from the specified file path.
    /// </summary>
    /// <param name="fileName">The file path to open.</param>
    /// <param name="mode">The file mode to use.</param>
    /// <param name="flags">Flags controlling storage behavior.</param>
    /// <returns>An opened <see cref="RootStorage"/> instance.</returns>
    public static RootStorage Open(string fileName, FileMode mode, StorageModeFlags flags = StorageModeFlags.None)
    {
        if (fileName is null)
            throw new ArgumentNullException(nameof(fileName));

        ThrowIfInvalid(mode);
        ThrowIfLeaveOpen(flags);

        FileStream stream = File.Open(fileName, mode);
        return Open(stream, flags);
    }

    /// <summary>
    /// Opens an existing compound file from the specified file path with access control.
    /// </summary>
    /// <param name="fileName">The file path to open.</param>
    /// <param name="mode">The file mode to use.</param>
    /// <param name="access">The file access mode.</param>
    /// <param name="flags">Flags controlling storage behavior.</param>
    /// <returns>An opened <see cref="RootStorage"/> instance.</returns>
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

    /// <summary>
    /// Opens an existing compound file from the specified stream.
    /// </summary>
    /// <param name="stream">The stream to open.</param>
    /// <param name="flags">Flags controlling storage behavior.</param>
    /// <returns>An opened <see cref="RootStorage"/> instance.</returns>
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

    /// <summary>
    /// Opens an existing compound file for read-only access.
    /// </summary>
    /// <param name="fileName">The file path to open.</param>
    /// <param name="flags">Flags controlling storage behavior.</param>
    /// <returns>An opened <see cref="RootStorage"/> instance.</returns>
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

    /// <summary>
    /// Disposes the current context of the compound file.
    /// </summary>
    public void Dispose() => Context.Dispose();

    /// <summary>
    /// Gets the underlying stream for this compound file.
    /// </summary>
    public Stream BaseStream => Context.BaseStream;

    /// <summary>
    /// Flushes changes to the underlying stream. Optionally consolidates the file.
    /// </summary>
    /// <param name="consolidate">If true, consolidates the file after flushing.</param>
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

    /// <summary>
    /// Commits all changes to the compound file.
    /// </summary>
    public void Commit()
    {
        this.ThrowIfDisposed(Context.IsDisposed);

        Context.Commit();
    }

    /// <summary>
    /// Reverts all uncommitted changes to the compound file.
    /// </summary>
    public void Revert()
    {
        this.ThrowIfDisposed(Context.IsDisposed);

        Context.Revert();
    }

    private void SwitchToCore(Stream stream, bool allowLeaveOpen)
    {
        Flush();
        Context.Stream.CopyAllTo(stream);
        Context.Dispose();

        IOContextFlags contextFlags = ToIOContextFlags(storageModeFlags);
        if (!allowLeaveOpen)
            contextFlags &= ~IOContextFlags.LeaveOpen;
        _ = new RootContext(ContextSite, stream, Version.Unknown, contextFlags);
    }

    /// <summary>
    /// Switches the underlying storage to a new stream.
    /// </summary>
    /// <param name="stream">The new stream to use.</param>
    public void SwitchTo(Stream stream)
    {
        if (stream is null)
            throw new ArgumentNullException(nameof(stream));

        SwitchToCore(stream, true);
    }

    /// <summary>
    /// Switches the underlying storage to a new file.
    /// </summary>
    /// <param name="fileName">The new file to use.</param>
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

    // TODO: Move checks to Tests project as Asserts
    [ExcludeFromCodeCoverage]
    internal bool Validate()
    {
        return Context.Fat.Validate()
            && Context.MiniFat.Validate();
    }
}
