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

    // Brent's cycle detection algorithm
    uint cycleLength = 1;
    uint power = 1;
    uint slowId = StreamId.NoStream;

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

        if (current.Id == slowId && slowId != StreamId.NoStream)
            throw new FileFormatException("Directory tree contains a loop.");

        if (cycleLength == power)
        {
            cycleLength = 0;
            power *= 2;
            slowId = current.Id;
        }

        cycleLength++;

        DirectoryEntry? rightSibling = directories.TryGetSibling(current, SiblingType.Right, false);
        if (rightSibling is not null)
            PushLeft(rightSibling);

        return true;
    }

    /// <inheritdoc/>
    public void Reset()
    {
        current = null;
        stack.Clear();
        cycleLength = 1;
        power = 1;
        slowId = StreamId.NoStream;
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
            node = directories.TryGetSibling(node, SiblingType.Left, false);
        }
    }
}
