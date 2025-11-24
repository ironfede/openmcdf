using System.Collections;

namespace OpenMcdf;

/// <summary>
/// Enumerates the children of a <see cref="DirectoryEntry"/>.
/// </summary>
internal sealed class DirectoryTreeEnumerator : IEnumerator<DirectoryEntry>
{
    private readonly DirectoryEntries directories;
    private readonly DirectoryEntry root;
    private readonly Stack<DirectoryEntry> stack = new();
    DirectoryEntry? current;

    internal DirectoryTreeEnumerator(DirectoryEntries directories, DirectoryEntry root)
    {
        this.directories = directories;
        this.root = root;
        Reset();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
    }

    /// <inheritdoc/>
    public DirectoryEntry Current => current switch
    {
        null => throw new InvalidOperationException("Enumeration has not started. Call MoveNext."),
        _ => current,
    };

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
        if (directories.TryGetDictionaryEntry(current.RightSiblingId, out DirectoryEntry? rightSibling))
            PushLeft(rightSibling);

        return true;
    }

    /// <inheritdoc/>
    public void Reset()
    {
        current = null;
        stack.Clear();
        if (root.ChildId != StreamId.NoStream)
        {
            DirectoryEntry child = directories.GetDictionaryEntry(root.ChildId);
            PushLeft(child);
        }
    }

    private void PushLeft(DirectoryEntry? node)
    {
        while (node is not null)
        {
            stack.Push(node);
            directories.TryGetDictionaryEntry(node.LeftSiblingId, out node);
        }
    }
}
