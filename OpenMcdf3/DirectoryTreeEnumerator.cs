using System.Collections;

namespace OpenMcdf3;

internal sealed class DirectoryTreeEnumerator : IEnumerator<DirectoryEntry>
{
    private readonly DirectoryEntry? child;
    private readonly Stack<DirectoryEntry> stack = new();
    private readonly DirectoryEntryEnumerator directoryEntryEnumerator;
    DirectoryEntry current;

    internal DirectoryTreeEnumerator(IOContext ioContext, DirectoryEntry root)
    {
        directoryEntryEnumerator = new(ioContext);
        this.child = directoryEntryEnumerator.Get(root.ChildID);
        PushLeft(child);
        current = default!;
    }

    public void Dispose()
    {
        directoryEntryEnumerator.Dispose();
    }

    public DirectoryEntry Current => current;

    object IEnumerator.Current => Current;

    public bool MoveNext()
    {
        if (stack.Count == 0)
            return false;

        current = stack.Pop();
        DirectoryEntry? rightSibling = directoryEntryEnumerator.Get(Current.RightSiblingID);
        PushLeft(rightSibling);
        return true;
    }

    public void Reset()
    {
        current = default!;
        stack.Clear();
        PushLeft(child);
    }

    private void PushLeft(DirectoryEntry? node)
    {
        while (node is not null)
        {
            stack.Push(node);
            node = directoryEntryEnumerator.Get(node.LeftSiblingID);
        }
    }
}
