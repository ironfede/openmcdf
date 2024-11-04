using System.Collections;
using System.Diagnostics;

namespace OpenMcdf3;

/// <summary>
/// Enumerates the children of a <see cref="DirectoryEntry"/>.
/// </summary>
internal sealed class DirectoryTreeEnumerator : IEnumerator<DirectoryEntry>
{
    private readonly IOContext ioContext;
    private readonly DirectoryEntry root;
    private DirectoryEntry? child;
    private readonly Stack<DirectoryEntry> stack = new();
    private readonly DirectoryEntryEnumerator directoryEntryEnumerator;
    DirectoryEntry? current;

    internal DirectoryTreeEnumerator(IOContext ioContext, DirectoryEntry root)
    {
        directoryEntryEnumerator = new(ioContext);
        this.ioContext = ioContext;
        this.root = root;
        if (root.ChildId != StreamId.NoStream)
            child = directoryEntryEnumerator.GetDictionaryEntry(root.ChildId);
        PushLeft(child);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        directoryEntryEnumerator.Dispose();
    }

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
        if (stack.Count == 0)
        {
            current = null;
            return false;
        }

        current = stack.Pop();
        if (current.RightSiblingId != StreamId.NoStream)
        {
            DirectoryEntry rightSibling = directoryEntryEnumerator.GetDictionaryEntry(current.RightSiblingId);
            PushLeft(rightSibling);
        }

        return true;
    }

    /// <inheritdoc/>
    public void Reset()
    {
        current = null;
        stack.Clear();
        PushLeft(child);
    }

    private void PushLeft(DirectoryEntry? node)
    {
        while (node is not null)
        {
            stack.Push(node);
            node = node.LeftSiblingId == StreamId.NoStream ? null : directoryEntryEnumerator.GetDictionaryEntry(node.LeftSiblingId);
        }
    }

    public bool MoveTo(StorageType type, string name)
    {
        Reset();

        while (MoveNext())
        {
            if (Current.Type == type && Current.Name == name)
                return true;
        }

        return false;
    }

    public DirectoryEntry? TryGetDirectoryEntry(StorageType type, string name)
    {
        if (MoveTo(type, name))
            return Current;
        return null;
    }

    public DirectoryEntry Add(StorageType storageType, string name)
    {
        if (MoveTo(storageType, name))
            throw new IOException($"{storageType} \"{name}\" already exists.");

        DirectoryEntry entry = directoryEntryEnumerator.CreateOrRecycleDirectoryEntry();
        entry.Recycle(storageType, name);

        Add(entry);

        return entry;
    }

    void Add(DirectoryEntry entry)
    {
        Reset();

        entry.Color = NodeColor.Black;
        directoryEntryEnumerator.Write(entry);

        if (root.ChildId == StreamId.NoStream)
        {
            Debug.Assert(child is null);
            root.ChildId = entry.Id;
            directoryEntryEnumerator.Write(root);
            child = entry;
        }
        else
        {
            Debug.Assert(child is not null);
            DirectoryEntry node = child!;
            while (node.LeftSiblingId != StreamId.NoStream)
                node = directoryEntryEnumerator.GetDictionaryEntry(node.LeftSiblingId);
            node.LeftSiblingId = entry.Id;
            directoryEntryEnumerator.Write(node);
        }
    }
}
