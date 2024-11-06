using System.Collections;
using System.Diagnostics;

namespace OpenMcdf3;

/// <summary>
/// Enumerates the children of a <see cref="DirectoryEntry"/>.
/// </summary>
internal sealed class DirectoryTreeEnumerator : IEnumerator<DirectoryEntry>
{
    private readonly Directories directories;
    private readonly DirectoryEntry root;
    private DirectoryEntry? child;
    private readonly Stack<DirectoryEntry> stack = new();
    DirectoryEntry parent;
    DirectoryEntry? current;

    internal DirectoryTreeEnumerator(Directories directories, DirectoryEntry root)
    {
        this.directories = directories;
        this.root = root;
        if (root.ChildId != StreamId.NoStream)
            child = directories.GetDictionaryEntry(root.ChildId);
        parent = root;
        PushLeft(child);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
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
            parent = root;
            return false;
        }

        current = stack.Pop();
        parent = stack.Count == 0 ? root : stack.Peek();
        if (current.RightSiblingId != StreamId.NoStream)
        {
            DirectoryEntry rightSibling = directories.GetDictionaryEntry(current.RightSiblingId);
            PushLeft(rightSibling);
        }

        return true;
    }

    /// <inheritdoc/>
    public void Reset()
    {
        current = null;
        parent = root;
        stack.Clear();
        PushLeft(child);
    }

    private void PushLeft(DirectoryEntry? node)
    {
        while (node is not null)
        {
            stack.Push(node);
            node = node.LeftSiblingId == StreamId.NoStream ? null : directories.GetDictionaryEntry(node.LeftSiblingId);
        }
    }

    public bool MoveTo(string name)
    {
        Reset();

        while (MoveNext())
        {
            if (Current.Name == name)
                return true;
        }

        return false;
    }

    public DirectoryEntry? TryGetDirectoryEntry(string name)
    {
        if (MoveTo(name))
            return Current;
        return null;
    }

    public DirectoryEntry Add(StorageType storageType, string name)
    {
        if (MoveTo(name))
            throw new IOException($"{storageType} \"{name}\" already exists.");

        DirectoryEntry entry = directories.CreateOrRecycleDirectoryEntry();
        entry.Recycle(storageType, name);

        Add(entry);

        return entry;
    }

    void Add(DirectoryEntry entry)
    {
        Reset();

        // TODO: Implement balancing (all-black for now)
        entry.Color = NodeColor.Black;
        directories.Write(entry);

        if (root.ChildId == StreamId.NoStream)
        {
            Debug.Assert(child is null);
            root.ChildId = entry.Id;
            directories.Write(root);
            child = entry;
        }
        else
        {
            Debug.Assert(child is not null);
            DirectoryEntry node = child!;
            while (node.LeftSiblingId != StreamId.NoStream)
                node = directories.GetDictionaryEntry(node.LeftSiblingId);
            node.LeftSiblingId = entry.Id;
            directories.Write(node);
        }
    }

    public void Remove(DirectoryEntry entry)
    {
        if (child is null)
            throw new KeyNotFoundException("DirectoryEntry has no children");

        if (root.ChildId == entry.Id)
        {
            root.ChildId = entry.LeftSiblingId;
            directories.Write(root);
            if (root.ChildId == StreamId.NoStream)
                child = null;
            return;
        }

        Reset();

        while (MoveNext())
        {
            if (current!.Id == entry.Id)
            {
                if (parent.LeftSiblingId == entry.Id)
                    parent.LeftSiblingId = entry.LeftSiblingId;
                directories.Write(parent);

                entry.Recycle();
                directories.Write(entry);
                break;
            }
        }
    }

    internal void PrintTrace(TextWriter writer)
    {
        Reset();

        while (MoveNext())
        {
            for (int i = 0; i < stack.Count; i++)
                writer.Write("  ");
            writer.WriteLine($"{Current.ColorChar} {Current}");
        }
    }
}
