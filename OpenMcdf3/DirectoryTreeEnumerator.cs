using System.Collections;

namespace OpenMcdf3;

internal sealed class DirectoryTreeEnumerator : IEnumerator<DirectoryEntry>
{
    private readonly DirectoryEntry? child;
    private readonly Stack<DirectoryEntry> stack = new();
    private readonly DirectoryEntryEnumerator directoryEntryEnumerator;
    DirectoryEntry? current;

    internal DirectoryTreeEnumerator(IOContext ioContext, DirectoryEntry root)
    {
        directoryEntryEnumerator = new(ioContext);
        child = directoryEntryEnumerator.Get(root.ChildId);
        PushLeft(child);
    }

    public void Dispose()
    {
        directoryEntryEnumerator.Dispose();
    }

    public DirectoryEntry Current
    {
        get
        {
            if (current is null)
                throw new InvalidOperationException("Enumeration has not started. Call MoveNext.");
            return current;
        }
    }

    object IEnumerator.Current => Current;

    public bool MoveNext()
    {
        if (stack.Count == 0)
        {
            current = null;
            return false;
        }

        current = stack.Pop();
        DirectoryEntry? rightSibling = directoryEntryEnumerator.Get(Current.RightSiblingId);
        PushLeft(rightSibling);
        return true;
    }

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
            node = directoryEntryEnumerator.Get(node.LeftSiblingId);
        }
    }
}
