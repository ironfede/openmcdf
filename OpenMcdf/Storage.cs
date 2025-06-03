using System.Diagnostics.CodeAnalysis;

namespace OpenMcdf;

/// <summary>
/// An object in a compound file that is analogous to a file system directory.
/// </summary>
public class Storage : ContextBase
{
    readonly DirectoryTree directoryTree;
    readonly DirectoryEntry directoryEntry;
    readonly string path;

    public Storage? Parent { get; }

    internal Storage(RootContextSite rootContextSite, DirectoryEntry directoryEntry, Storage? parent)
        : base(rootContextSite)
    {
        if (directoryEntry.Type is not StorageType.Storage and not StorageType.Root)
            throw new ArgumentException("DirectoryEntry must be a Storage or Root.", nameof(directoryEntry));

        directoryTree = new(Context.DirectoryEntries, directoryEntry);
        this.directoryEntry = directoryEntry;
        Parent = parent;
        path = parent is null ? $"/" : $"{parent.path}{parent.EntryInfo.Name}/";
    }

    public EntryInfo EntryInfo => directoryEntry.ToEntryInfo(path);

    [Obsolete("Use CLSID instead.")]
    public Guid CLISD
    {
        get => CLSID;
        set => CLSID = value;
    }

    public Guid CLSID
    {
        get => directoryEntry.CLSID;
        set
        {
            Context.ThrowIfNotWritable();

            directoryEntry.CLSID = value;
            Context.DirectoryEntries.Write(directoryEntry);
        }
    }

    public DateTime CreationTime
    {
        get => directoryEntry.CreationTime;
        set
        {
            Context.ThrowIfNotWritable();

            directoryEntry.CreationTime = value;
            Context.DirectoryEntries.Write(directoryEntry);
        }
    }

    public DateTime ModifiedTime
    {
        get => directoryEntry.ModifiedTime;
        set
        {
            Context.ThrowIfNotWritable();

            directoryEntry.ModifiedTime = value;
            Context.DirectoryEntries.Write(directoryEntry);
        }
    }

    public uint StateBits
    {
        get => directoryEntry.StateBits;
        set
        {
            Context.ThrowIfNotWritable();

            directoryEntry.StateBits = value;
            Context.DirectoryEntries.Write(directoryEntry);
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
        using DirectoryTreeEnumerator treeEnumerator = new(Context.DirectoryEntries, directoryEntry);
        while (treeEnumerator.MoveNext())
        {
            yield return treeEnumerator.Current;
        }
    }

    public bool ContainsEntry(string name)
    {
        if (name is null)
            throw new ArgumentNullException(nameof(name));

        ThrowHelper.ThrowIfNameIsInvalid(name);

        this.ThrowIfDisposed(Context.IsDisposed);

        return directoryTree.TryGetDirectoryEntry(name, out DirectoryEntry? _);
    }

    public bool TryGetEntryInfo(string name, out EntryInfo entryInfo)
    {
        if (name is null)
            throw new ArgumentNullException(nameof(name));

        ThrowHelper.ThrowIfNameIsInvalid(name);

        this.ThrowIfDisposed(Context.IsDisposed);

        if (!directoryTree.TryGetDirectoryEntry(name, out DirectoryEntry? entry))
        {
            entryInfo = default;
            return false;
        }

        string path = $"{EntryInfo.Path}{EntryInfo.Name}";
        entryInfo = entry.ToEntryInfo(path);
        return true;
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
        if (name is null)
            throw new ArgumentNullException(nameof(name));

        ThrowHelper.ThrowIfNameIsInvalid(name);

        this.ThrowIfDisposed(Context.IsDisposed);
        Context.ThrowIfNotWritable();

        DirectoryEntry entry = AddDirectoryEntry(StorageType.Storage, name);
        return new Storage(ContextSite, entry, this);
    }

    public CfbStream CreateStream(string name)
    {
        if (name is null)
            throw new ArgumentNullException(nameof(name));

        ThrowHelper.ThrowIfNameIsInvalid(name);

        this.ThrowIfDisposed(Context.IsDisposed);
        Context.ThrowIfNotWritable();

        DirectoryEntry entry = AddDirectoryEntry(StorageType.Stream, name);
        return new CfbStream(ContextSite, entry, this);
    }

    public Storage OpenStorage(string name)
        => TryOpenStorage(name, out Storage? storage)
            ? storage!
            : throw new DirectoryNotFoundException($"Storage not found: {name}.");

    public bool TryOpenStorage(string name, [MaybeNullWhen(false)] out Storage storage)
    {
        if (name is null)
            throw new ArgumentNullException(nameof(name));

        ThrowHelper.ThrowIfNameIsInvalid(name);

        this.ThrowIfDisposed(Context.IsDisposed);

        directoryTree.TryGetDirectoryEntry(name, out DirectoryEntry? entry);
        if (entry is null || entry.Type is not StorageType.Storage)
        {
            storage = null;
            return false;
        }

        storage = new Storage(ContextSite, entry, this);
        return true;
    }

    public CfbStream OpenStream(string name)
         => TryOpenStream(name, out CfbStream? stream)
            ? stream!
            : throw new FileNotFoundException($"Stream not found: {name}.", name);

    public bool TryOpenStream(string name, [MaybeNullWhen(false)] out CfbStream stream)
    {
        if (name is null)
            throw new ArgumentNullException(nameof(name));

        ThrowHelper.ThrowIfNameIsInvalid(name);

        this.ThrowIfDisposed(Context.IsDisposed);

        directoryTree.TryGetDirectoryEntry(name, out DirectoryEntry? entry);
        if (entry is null || entry.Type is not StorageType.Stream)
        {
            stream = null;
            return false;
        }

        stream = new CfbStream(ContextSite, entry, this);
        return true;
    }

    public void CopyTo(Storage destination)
    {
        if (destination is null)
            throw new ArgumentNullException(nameof(destination));

        if (destination == this)
            throw new ArgumentException("A storage cannot be copied to itself.", nameof(destination));

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
                using CfbStream stream = new(ContextSite, entry, this);
                using CfbStream destinationStream = destination.CreateStream(entry.NameString);
                stream.CopyTo(destinationStream);
            }
        }
    }

    public void Delete(string name)
    {
        if (name is null)
            throw new ArgumentNullException(nameof(name));

        ThrowHelper.ThrowIfNameIsInvalid(name);

        this.ThrowIfDisposed(Context.IsDisposed);
        Context.ThrowIfNotWritable();

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
