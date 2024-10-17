using System.Collections;

namespace OpenMcdf3;

/// <summary>
/// Enumerates the children of a <see cref="DirectoryEntry"/>.
/// </summary>
internal sealed class DirectoryTreeEnumerator : IEnumerator<DirectoryEntry>
{
    private readonly DirectoryEntry? child;
    private readonly Stack<DirectoryEntry> stack = new();
    private readonly DirectoryEntryEnumerator directoryEntryEnumerator;
    DirectoryEntry? current;

    internal DirectoryTreeEnumerator(IOContext ioContext, DirectoryEntry root)
    {
        directoryEntryEnumerator = new(ioContext);
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
}
