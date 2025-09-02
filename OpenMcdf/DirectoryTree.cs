﻿using System.Diagnostics.CodeAnalysis;

namespace OpenMcdf;

/// <summary>
/// Encapsulates adding and removing <see cref="DirectoryEntry"/> objects to a red-black tree.
/// </summary>
internal sealed class DirectoryTree
{
    internal enum RelationType
    {
        LeftSibling,
        RightSibling,
        Root,
    }

    private readonly DirectoryEntries directories;
    private readonly DirectoryEntry root;

    public DirectoryTree(DirectoryEntries directories, DirectoryEntry root)
    {
        this.directories = directories;
        this.root = root;
    }

    public bool TryGetDirectoryEntry(string name, [MaybeNullWhen(false)] out DirectoryEntry entry, bool validateColor = false)
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
            if (compare == 0)
            {
                entry = child;
                return true;
            }

            SiblingType siblingType = compare < 0 ? SiblingType.Left : SiblingType.Right;
            child = directories.TryGetSibling(child, siblingType, validateColor);
        }

        entry = null;
        return false;
    }

    public DirectoryEntry GetParent(DirectoryEntry entry, out RelationType relation)
    {
        if (!TryGetParent(entry, out DirectoryEntry? parent, out relation))
            throw new FileFormatException($"DirectoryEntry {entry} has no parent.");
        return parent;
    }

    public bool TryGetParent(DirectoryEntry entry, [MaybeNullWhen(false)] out DirectoryEntry parent, out RelationType relation)
    {
        if (!directories.TryGetDictionaryEntry(root.ChildId, out DirectoryEntry? child))
        {
            parent = null;
            relation = RelationType.Root;
            return false;
        }

        parent = root;
        relation = RelationType.Root;
        while (child is not null)
        {
            int compare = DirectoryEntryComparer.Compare(entry.NameCharSpan, child.NameCharSpan);
            if (compare < 0)
            {
                parent = child;
                relation = RelationType.LeftSibling;
                directories.TryGetDictionaryEntry(child.LeftSiblingId, out child);
            }
            else if (compare > 0)
            {
                parent = child;
                relation = RelationType.RightSibling;
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

        while (true)
        {
            uint leftId = currentEntry.LeftSiblingId;
            uint rightId = currentEntry.RightSiblingId;

            int compare = DirectoryEntryComparer.Compare(entry.NameCharSpan, currentEntry.NameCharSpan);
            if (compare < 0)
            {
                if (leftId == StreamId.NoStream)
                {
                    currentEntry.LeftSiblingId = entry.Id;
                    directories.Write(currentEntry);
                    directories.Write(entry);
                    return;
                }

                currentEntry = directories.GetDictionaryEntry(leftId);
            }
            else if (compare > 0)
            {
                if (rightId == StreamId.NoStream)
                {
                    currentEntry.RightSiblingId = entry.Id;
                    directories.Write(currentEntry);
                    directories.Write(entry);
                    return;
                }

                currentEntry = directories.GetDictionaryEntry(rightId);
            }
            else
            {
                throw new IOException($"{entry.Type} \"{entry.NameString}\" already exists.");
            }
        }
    }

    void SetRelation(DirectoryEntry entry, RelationType relation, uint value)
    {
        switch (relation)
        {
            case RelationType.LeftSibling:
                entry.LeftSiblingId = value;
                break;
            case RelationType.RightSibling:
                entry.RightSiblingId = value;
                break;
            case RelationType.Root:
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
                uint newRightChildParentId = entry.LeftSiblingId;
                DirectoryEntry newRightChildParent;
                for (; ; )
                {
                    newRightChildParent = directories.GetDictionaryEntry(newRightChildParentId);
                    newRightChildParentId = newRightChildParent.RightSiblingId;
                    if (newRightChildParent.RightSiblingId == StreamId.NoStream)
                    {
                        break;
                    }
                }

                newRightChildParent.RightSiblingId = entry.RightSiblingId;
                directories.Write(newRightChildParent);
            }
        }
    }

    [ExcludeFromCodeCoverage]
    internal void Validate()
    {
        if (root.ChildId != StreamId.NoStream)
        {
            DirectoryEntry child = directories.GetDictionaryEntry(root.ChildId);
            if (child.Color is not NodeColor.Black)
                throw new FileFormatException("Root child is not black.");

            Validate(child);
        }
    }

    [ExcludeFromCodeCoverage]
    void Validate(DirectoryEntry entry)
    {
        DirectoryEntry? leftSibling = directories.TryGetSibling(entry, SiblingType.Left, false);
        if (leftSibling is not null)
            Validate(leftSibling);

        DirectoryEntry? rightSibling = directories.TryGetSibling(entry, SiblingType.Right, false);
        if (rightSibling is not null)
            Validate(rightSibling);

        if (entry.ChildId != StreamId.NoStream)
        {
            DirectoryEntry child = directories.GetDictionaryEntry(entry.ChildId);
            Validate(child);
        }
    }

    [ExcludeFromCodeCoverage]
    internal void WriteTrace(TextWriter writer)
    {
        if (directories.TryGetDictionaryEntry(root.ChildId, out DirectoryEntry? child))
            WriteTrace(writer, child, 0);
    }

    [ExcludeFromCodeCoverage]
    void WriteTrace(TextWriter writer, DirectoryEntry entry, int indent)
    {
        DirectoryEntry? rightSibling = directories.TryGetSibling(entry, SiblingType.Right, false);
        if (rightSibling is not null)
            WriteTrace(writer, rightSibling, indent + 1);

        for (int i = 0; i < indent; i++)
            writer.Write("  ");
        writer.WriteLine(entry);

        DirectoryEntry? leftSibling = directories.TryGetSibling(entry, SiblingType.Left, false);
        if (leftSibling is not null)
            WriteTrace(writer, leftSibling, indent + 1);
    }
}
