using System.Diagnostics.CodeAnalysis;

namespace OpenMcdf;

internal enum SiblingType
{
    Left,
    Right,
}

/// <summary>
/// Encapsulates getting and adding <see cref="DirectoryEntry"/> objects."
/// </summary>
internal sealed class DirectoryEntries : ContextBase, IDisposable
{
    private readonly FatChainEnumerator fatChainEnumerator;
    private readonly DirectoryEntryEnumerator directoryEntryEnumerator;

    public DirectoryEntry RootEntry { get; }

    public DirectoryEntries(RootContextSite rootContextSite, bool create)
        : base(rootContextSite)
    {
        fatChainEnumerator = new FatChainEnumerator(Context.Fat, Context.Header.FirstDirectorySectorId);
        directoryEntryEnumerator = new DirectoryEntryEnumerator(this);

        if (create)
        {
            RootEntry = CreateOrRecycleDirectoryEntry();
            RootEntry.RecycleRoot();
        }
        else
        {
            RootEntry = GetDictionaryEntry(0);
        }
    }

    public void Dispose()
    {
        directoryEntryEnumerator.Dispose();
        fatChainEnumerator.Dispose();
    }

    /// <summary>
    /// Gets the <see cref="DirectoryEntry"/> for the specified stream ID.
    /// </summary>
    public DirectoryEntry GetDictionaryEntry(uint streamId)
    {
        if (!TryGetDictionaryEntry(streamId, out DirectoryEntry? entry))
            throw new FileFormatException($"Directory entry {streamId} was not found.");
        return entry!;
    }

    public bool TryGetDictionaryEntry(uint streamId, [MaybeNullWhen(false)] out DirectoryEntry entry)
    {
        if (streamId == StreamId.NoStream)
        {
            entry = null;
            return false;
        }

        if (streamId > StreamId.Maximum)
            throw new FileFormatException($"Invalid directory entry stream ID: ${streamId:X8}.");

        uint chainIndex = GetChainIndexAndEntryIndex(streamId, out long entryIndex);
        if (!fatChainEnumerator.MoveTo(chainIndex))
        {
            entry = null;
            return false;
        }

        Context.Reader.Position = fatChainEnumerator.CurrentSector.Position + (entryIndex * DirectoryEntry.Length);
        entry = Context.Reader.ReadDirectoryEntry(Context.Version, streamId);
        return true;
    }

    public DirectoryEntry? TryGetSibling(DirectoryEntry entry, SiblingType siblingType, bool validateColor)
    {
        uint siblingId = siblingType == SiblingType.Left ? entry.LeftSiblingId : entry.RightSiblingId;
        if (!TryGetDictionaryEntry(siblingId, out DirectoryEntry? sibling))
            return null;

        int compare = DirectoryEntryComparer.Compare(sibling.NameCharSpan, entry.NameCharSpan);
        if ((siblingType is SiblingType.Left && compare >= 0) || (siblingType is SiblingType.Right && compare <= 0))
            throw new FileFormatException("Directory tree is not sorted.");
        if (validateColor && entry.Color is NodeColor.Red && sibling.Color is NodeColor.Red)
            throw new FileFormatException("Red-black tree red-violation.");
        return sibling;
    }

    private uint GetChainIndexAndEntryIndex(uint streamId, out long entryIndex) => (uint)Math.DivRem(streamId, Context.DirectoryEntriesPerSector, out entryIndex);

    public DirectoryEntry CreateOrRecycleDirectoryEntry()
    {
        DirectoryEntry? entry = TryRecycleDirectoryEntry();
        if (entry is not null)
            return entry;

        CfbBinaryWriter writer = Context.Writer;
        uint id = fatChainEnumerator.Extend();
        Header header = Context.Header;
        if (header.FirstDirectorySectorId == SectorType.EndOfChain)
            header.FirstDirectorySectorId = id;
        if (Context.Version == Version.V4)
            header.DirectorySectorCount++;

        Sector sector = new(id, Context.SectorSize);
        writer.Position = sector.Position;
        for (int i = 0; i < Context.DirectoryEntriesPerSector; i++)
            writer.Write(DirectoryEntry.Unallocated);

        entry = TryRecycleDirectoryEntry()
            ?? throw new InvalidOperationException("Failed to add or recycle directory entry.");
        return entry;
    }

    private DirectoryEntry? TryRecycleDirectoryEntry()
    {
        directoryEntryEnumerator.Reset();

        while (directoryEntryEnumerator.MoveNext())
        {
            DirectoryEntry current = directoryEntryEnumerator.Current;
            if (directoryEntryEnumerator.Current.Type is StorageType.Unallocated)
                return current;
        }

        return null;
    }

    public void Write(DirectoryEntry entry)
    {
        uint chainIndex = GetChainIndexAndEntryIndex(entry.Id, out long entryIndex);
        if (!fatChainEnumerator.MoveTo(chainIndex))
            throw new FileFormatException($"Directory entry {entry.Id} was not found.");

        CfbBinaryWriter writer = Context.Writer;
        writer.Position = fatChainEnumerator.CurrentSector.Position + (entryIndex * DirectoryEntry.Length);
        writer.Write(entry);
    }

    [ExcludeFromCodeCoverage]
    public void Validate()
    {
        DirectoryTree tree = new(this, RootEntry);
        tree.Validate();
    }

    public void WriteTrace(TextWriter writer)
    {
        DirectoryTree tree = new(this, RootEntry);
        tree.WriteTrace(writer);
    }
}
