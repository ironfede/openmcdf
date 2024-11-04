using System.Collections;
using System.Diagnostics;

namespace OpenMcdf3;

/// <summary>
/// Enumerates <see cref="DirectoryEntry"/> instances from a <see cref="FatChainEnumerator"/>.
/// </summary>
internal sealed class DirectoryEntryEnumerator : IEnumerator<DirectoryEntry>
{
    private readonly IOContext ioContext;
    private readonly FatChainEnumerator fatChainEnumerator;
    private bool start = true;
    private uint index = uint.MaxValue;
    private DirectoryEntry? current;

    public DirectoryEntryEnumerator(IOContext ioContext)
    {
        this.ioContext = ioContext;
        fatChainEnumerator = new FatChainEnumerator(ioContext, ioContext.Header.FirstDirectorySectorId);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        fatChainEnumerator.Dispose();
    }

    private int EntriesPerSector => ioContext.SectorSize / DirectoryEntry.Length;

    /// <inheritdoc/>
    public DirectoryEntry Current
    {
        get
        {
            if (current is null)
                throw new InvalidOperationException("Enumeration has not started. Call MoveNext.");
            return current;
        }
    }

    /// <inheritdoc/>
    object IEnumerator.Current => Current;

    /// <inheritdoc/>
    public bool MoveNext()
    {
        if (start)
        {
            start = false;
            index = 0;
        }

        uint chainIndex = (uint)Math.DivRem(index, EntriesPerSector, out long entryIndex);
        if (!fatChainEnumerator.MoveTo(chainIndex))
        {
            current = null;
            index = uint.MaxValue;
            return false;
        }

        ioContext.Reader.Position = fatChainEnumerator.CurrentSector.Position + (entryIndex * DirectoryEntry.Length);
        current = ioContext.Reader.ReadDirectoryEntry(ioContext.Version, index);
        index++;
        return true;
    }

    public DirectoryEntry CreateOrRecycleDirectoryEntry()
    {
        DirectoryEntry? entry = TryRecycleDirectoryEntry();
        if (entry is not null)
            return entry;

        CfbBinaryWriter writer = ioContext.Writer;
        uint id = fatChainEnumerator.Extend();
        if (ioContext.Header.FirstDirectorySectorId == SectorType.EndOfChain)
            ioContext.Header.FirstDirectorySectorId = id;

        Sector sector = new(id, ioContext.SectorSize);
        writer.Position = sector.Position;
        int directoryEntriesPerSector = EntriesPerSector;
        for (int i = 0; i < directoryEntriesPerSector; i++)
            writer.Write(DirectoryEntry.Unallocated);

        entry = TryRecycleDirectoryEntry()
            ?? throw new InvalidOperationException("Failed to add or recycle directory entry.");
        return entry;
    }

    private DirectoryEntry? TryRecycleDirectoryEntry()
    {
        Reset();

        while (MoveNext())
        {
            if (current!.Type == StorageType.Unallocated)
            {
                return current;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the <see cref="DirectoryEntry"/> for the specified stream ID.
    /// </summary>
    public DirectoryEntry GetDictionaryEntry(uint streamId)
    {
        if (streamId > StreamId.Maximum)
            throw new ArgumentException($"Invalid directory entry stream ID: ${streamId:X8}.", nameof(streamId));

        uint chainIndex = (uint)Math.DivRem(streamId, EntriesPerSector, out long entryIndex);
        if (!fatChainEnumerator.MoveTo(chainIndex))
            throw new KeyNotFoundException($"Directory entry {streamId} was not found.");

        ioContext.Reader.Position = fatChainEnumerator.CurrentSector.Position + (entryIndex * DirectoryEntry.Length);
        current = ioContext.Reader.ReadDirectoryEntry(ioContext.Version, streamId);
        return current;
    }

    public void Write(DirectoryEntry entry)
    {
        uint chainIndex = (uint)Math.DivRem(entry.Id, EntriesPerSector, out long entryIndex);
        if (!fatChainEnumerator.MoveTo(chainIndex))
            throw new KeyNotFoundException($"Directory entry {entry.Id} was not found.");

        CfbBinaryWriter writer = ioContext.Writer;
        writer.Position = fatChainEnumerator.CurrentSector.Position + (entryIndex * DirectoryEntry.Length);
        writer.Write(entry);
    }

    /// <inheritdoc/>
    public void Reset()
    {
        fatChainEnumerator.Reset();
        start = true;
        current = null;
        index = uint.MaxValue;
    }
}
