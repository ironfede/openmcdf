﻿using System.Collections;

namespace OpenMcdf;

/// <summary>
/// Enumerates <see cref="DirectoryEntry"/> instances from a <see cref="DirectoryEntries"/>.
/// </summary>
internal sealed class DirectoryEntryEnumerator : IEnumerator<DirectoryEntry>
{
    private readonly DirectoryEntries directories;
    private bool start = true;
    private uint index = uint.MaxValue;
    private DirectoryEntry? current;

    public DirectoryEntryEnumerator(DirectoryEntries directories)
    {
        this.directories = directories;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
    }

    /// <inheritdoc/>
    public DirectoryEntry Current => current switch
    {
        null => throw new InvalidOperationException("Enumeration has not started. Call MoveNext."),
        _ => current
    };

    /// <inheritdoc/>
    object IEnumerator.Current => Current;

    /// <inheritdoc/>
    public bool MoveNext()
    {
        if (start)
        {
            start = false;
            index = uint.MaxValue;
        }

        uint nextIndex = index + 1;
        if (!directories.TryGetDictionaryEntry(nextIndex, out current))
        {
            index = uint.MaxValue;
            return false;
        }

        index = nextIndex;
        return true;
    }

    /// <inheritdoc/>
    public void Reset()
    {
        start = true;
        current = null;
        index = uint.MaxValue;
    }
}
