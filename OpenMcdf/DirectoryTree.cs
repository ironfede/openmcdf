using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace OpenMcdf;

/// <summary>
/// Encapsulates adding and removing <see cref="DirectoryEntry"/> objects to a tree.
/// </summary>
internal sealed class DirectoryTree
{
    internal enum RelationType
    {
        Previous,
        Next,
        Directory,
    }

    private readonly DirectoryEntries directories;
    private readonly DirectoryEntry root;

    public DirectoryTree(DirectoryEntries directories, DirectoryEntry root)
    {
        this.directories = directories;
        this.root = root;
    }

    public bool TryGetDirectoryEntry(string name, out DirectoryEntry? entry)
    {
        if (!directories.TryGetDictionaryEntry(root.ChildId, out DirectoryEntry? child))
        {
            entry = null;
            return false;
        }

        ReadOnlySpan<char> nameSpan = name.AsSpan();
        while (child is not null)
        {
            int compare = DirectoryEntryComparer.Compare(nameSpan, child.NameCharSpan);
            if (compare < 0)
            {
                directories.TryGetDictionaryEntry(child.LeftSiblingId, out child);
            }
            else if (compare > 0)
            {
                directories.TryGetDictionaryEntry(child.RightSiblingId, out child);
            }
            else
            {
                entry = child;
                return true;
            }
        }

        entry = null;
        return false;
    }

    public DirectoryEntry GetParent(DirectoryEntry entry, out RelationType relation)
    {
        if (!TryGetParent(entry, out DirectoryEntry? parent, out relation))
            throw new KeyNotFoundException($"DirectoryEntry {entry} has no parent.");
        return parent!;
    }

    public bool TryGetParent(DirectoryEntry entry, out DirectoryEntry? parent, out RelationType relation)
    {
        if (!directories.TryGetDictionaryEntry(root.ChildId, out DirectoryEntry? child))
        {
            parent = null;
            relation = RelationType.Directory;
            return false;
        }

        parent = root;
        relation = RelationType.Directory;
        while (child is not null)
        {
            int compare = DirectoryEntryComparer.Compare(entry.NameCharSpan, child.NameCharSpan);
            if (compare < 0)
            {
                parent = child;
                relation = RelationType.Previous;
                directories.TryGetDictionaryEntry(child.LeftSiblingId, out child);
            }
            else if (compare > 0)
            {
                parent = child;
                relation = RelationType.Next;
                directories.TryGetDictionaryEntry(child.RightSiblingId, out child);
            }
            else
            {
                return true;
            }
        }

        return false;
    }

    public void Add(DirectoryEntry entry)
    {
        if (!directories.TryGetDictionaryEntry(root.ChildId, out DirectoryEntry? currentEntry))
        {
            root.ChildId = entry.Id;
            directories.Write(root);
            directories.Write(entry);
            return;
        }

        uint previous = currentEntry!.LeftSiblingId;
        uint next = currentEntry.RightSiblingId;

        while (true)
        {
            int compare = DirectoryEntryComparer.Compare(entry.NameCharSpan, currentEntry!.NameCharSpan);
            if (compare < 0)
            {
                if (previous == StreamId.NoStream)
                {
                    currentEntry.LeftSiblingId = entry.Id;
                    directories.Write(currentEntry);
                    directories.Write(entry);
                    return;
                }

                currentEntry = directories.GetDictionaryEntry(previous);
            }
            else if (compare > 0)
            {
                if (next == StreamId.NoStream)
                {
                    currentEntry.RightSiblingId = entry.Id;
                    directories.Write(currentEntry);
                    directories.Write(entry);
                    return;
                }

                currentEntry = directories.GetDictionaryEntry(next);
            }
            else
            {
                throw new IOException($"{entry.Type} \"{entry.NameString}\" already exists.");
            }

            previous = currentEntry!.LeftSiblingId;
            next = currentEntry!.RightSiblingId;
        }
    }

    void SetRelation(DirectoryEntry entry, RelationType relation, uint value)
    {
        switch (relation)
        {
            case RelationType.Previous:
                entry.LeftSiblingId = value;
                break;
            case RelationType.Next:
                entry.RightSiblingId = value;
                break;
            case RelationType.Directory:
                root.ChildId = value;
                break;
        }
    }

    public void Remove(DirectoryEntry entry)
    {
        DirectoryEntry parent = GetParent(entry, out RelationType relation);

        if (entry.LeftSiblingId == StreamId.NoStream)
        {
            SetRelation(parent, relation, entry.RightSiblingId);
            directories.Write(parent);
        }
        else
        {
            SetRelation(parent, relation, entry.LeftSiblingId);
            directories.Write(parent);

            if (entry.RightSiblingId != StreamId.NoStream)
            {
                uint newRightChildParent = entry.LeftSiblingId;
                DirectoryEntry newRightChildParentEntry;
                for (; ; )
                {
                    newRightChildParentEntry = directories.GetDictionaryEntry(newRightChildParent);
                    if (newRightChildParentEntry.RightSiblingId == StreamId.NoStream)
                    {
                        break;
                    }
                };

                newRightChildParentEntry.RightSiblingId = entry.RightSiblingId;
                directories.Write(newRightChildParentEntry);
            }
        }
    }

    [ExcludeFromCodeCoverage]
    internal void WriteTrace(TextWriter writer)
    {
        if (root.ChildId == StreamId.NoStream)
        {
            Trace.WriteLine("Empty tree");
            return;
        }

        DirectoryEntry current = directories.GetDictionaryEntry(root.ChildId);
        WriteTrace(writer, current, 0);
    }

    void WriteTrace(TextWriter writer, DirectoryEntry entry, int indent)
    {
        directories.TryGetDictionaryEntry(entry.RightSiblingId, out DirectoryEntry? rightSibling);
        if (rightSibling is not null)
            WriteTrace(writer, rightSibling, indent + 1);

        for (int i = 0; i < indent; i++)
            writer.Write("  ");
        writer.WriteLine(entry);

        directories.TryGetDictionaryEntry(entry.LeftSiblingId, out DirectoryEntry? leftSibling);
        if (leftSibling is not null)
            WriteTrace(writer, leftSibling, indent + 1);
    }
}
