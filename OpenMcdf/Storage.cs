namespace OpenMcdf;

/// <summary>
/// An object in a compound file that is analogous to a file system directory.
/// </summary>
public class Storage : ContextBase
{
    readonly DirectoryTree directoryTree;
    readonly string path;

    internal DirectoryEntry DirectoryEntry { get; }

    public Storage? Parent { get; }

    internal Storage(RootContextSite rootContextSite, DirectoryEntry directoryEntry, Storage? parent)
        : base(rootContextSite)
    {
        if (directoryEntry.Type is not StorageType.Storage and not StorageType.Root)
            throw new ArgumentException("DirectoryEntry must be a Storage or Root.", nameof(directoryEntry));

        directoryTree = new(Context.DirectoryEntries, directoryEntry);
        DirectoryEntry = directoryEntry;
        Parent = parent;
        path = parent is null ? $"/" : $"{parent.path}{parent.EntryInfo.Name}/";
    }

    public EntryInfo EntryInfo => DirectoryEntry.ToEntryInfo(path);

    public Guid CLISD
    {
        get => DirectoryEntry.CLSID;
        set
        {
            DirectoryEntry.CLSID = value;
            Context.DirectoryEntries.Write(DirectoryEntry);
        }
    }

    public DateTime CreationTime
    {
        get => DirectoryEntry.CreationTime;
        set
        {
            DirectoryEntry.CreationTime = value;
            Context.DirectoryEntries.Write(DirectoryEntry);
        }
    }

    public DateTime ModifiedTime
    {
        get => DirectoryEntry.ModifiedTime;
        set
        {
            DirectoryEntry.ModifiedTime = value;
            Context.DirectoryEntries.Write(DirectoryEntry);
        }
    }

    public uint StateBits
    {
        get => DirectoryEntry.StateBits;
        set
        {
            DirectoryEntry.StateBits = value;
            Context.DirectoryEntries.Write(DirectoryEntry);
        }
    }

    public IEnumerable<EntryInfo> EnumerateEntries()
    {
        this.ThrowIfDisposed(Context.IsDisposed);

        EntryInfo entryInfo = EntryInfo;
        string path = $"{entryInfo.Path}{entryInfo.Name}";
        return EnumerateDirectoryEntries()
            .Select(e => e.ToEntryInfo(path));
    }

    IEnumerable<DirectoryEntry> EnumerateDirectoryEntries()
    {
        using DirectoryTreeEnumerator treeEnumerator = new(Context.DirectoryEntries, DirectoryEntry);
        while (treeEnumerator.MoveNext())
        {
            yield return treeEnumerator.Current;
        }
    }

    DirectoryEntry AddDirectoryEntry(StorageType storageType, string name)
    {
        DirectoryEntry entry = Context.DirectoryEntries.CreateOrRecycleDirectoryEntry();
        entry.Recycle(storageType, name);
        directoryTree.Add(entry);
        return entry;
    }

    public Storage CreateStorage(string name)
    {
        ThrowHelper.ThrowIfNameIsInvalid(name);

        this.ThrowIfDisposed(Context.IsDisposed);

        DirectoryEntry entry = AddDirectoryEntry(StorageType.Storage, name);
        return new Storage(ContextSite, entry, this);
    }

    public CfbStream CreateStream(string name)
    {
        ThrowHelper.ThrowIfNameIsInvalid(name);

        this.ThrowIfDisposed(Context.IsDisposed);

        DirectoryEntry entry = AddDirectoryEntry(StorageType.Stream, name);
        return new CfbStream(ContextSite, entry, this);
    }

    public Storage OpenStorage(string name)
    {
        ThrowHelper.ThrowIfNameIsInvalid(name);

        this.ThrowIfDisposed(Context.IsDisposed);

        directoryTree.TryGetDirectoryEntry(name, out DirectoryEntry? entry);
        if (entry is null || entry.Type is not StorageType.Storage)
            throw new DirectoryNotFoundException($"Storage not found: {name}.");
        return new Storage(ContextSite, entry, this);
    }

    public CfbStream OpenStream(string name)
    {
        ThrowHelper.ThrowIfNameIsInvalid(name);

        this.ThrowIfDisposed(Context.IsDisposed);

        directoryTree.TryGetDirectoryEntry(name, out DirectoryEntry? entry);
        if (entry is null || entry.Type is not StorageType.Stream)
            throw new FileNotFoundException($"Stream not found: {name}.", name);

        return new CfbStream(ContextSite, entry, this);
    }

    public void CopyTo(Storage destination)
    {
        foreach (DirectoryEntry entry in EnumerateDirectoryEntries())
        {
            if (entry.Type is StorageType.Storage)
            {
                Storage subSource = new(ContextSite, entry, this);
                Storage subDestination = destination.CreateStorage(entry.NameString);
                subSource.CopyTo(subDestination);
            }
            else if (entry.Type is StorageType.Stream)
            {
                CfbStream stream = new(ContextSite, entry, this);
                CfbStream destinationStream = destination.CreateStream(entry.NameString);
                stream.CopyTo(destinationStream);
            }
        }
    }

    public void Delete(string name)
    {
        ThrowHelper.ThrowIfNameIsInvalid(name);

        this.ThrowIfDisposed(Context.IsDisposed);

        directoryTree.TryGetDirectoryEntry(name, out DirectoryEntry? entry);
        if (entry is null)
            return;

        if (entry.Type is StorageType.Storage && entry.ChildId is not StreamId.NoStream)
        {
            Storage storage = new(ContextSite, entry, this);
            foreach (EntryInfo childEntry in storage.EnumerateEntries())
            {
                storage.Delete(childEntry.Name);
            };
        }

        if (entry.Type is StorageType.Stream && entry.StartSectorId is not StreamId.NoStream)
        {
            if (entry.StreamLength < Header.MiniStreamCutoffSize)
            {
                using MiniFatChainEnumerator miniFatChainEnumerator = new(ContextSite, entry.StartSectorId);
                miniFatChainEnumerator.Shrink(0);
            }
            else
            {
                using FatChainEnumerator fatChainEnumerator = new(Context.Fat, entry.StartSectorId);
                fatChainEnumerator.Shrink(0);
            }
        }

        directoryTree.Remove(entry);
    }

    internal void TraceDirectoryEntries(TextWriter writer)
    {
        directoryTree.WriteTrace(writer);
    }
}
