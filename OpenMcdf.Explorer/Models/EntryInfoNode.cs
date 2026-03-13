using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System.Collections.ObjectModel;

namespace OpenMcdf.Explorer.Models;

public sealed class EntryInfoNode
{
    private static readonly Bitmap StorageIcon = new(AssetLoader.Open(new Uri("avares://OpenMcdf.Explorer/Assets/storage.png")));
    private static readonly Bitmap StreamIcon = new(AssetLoader.Open(new Uri("avares://OpenMcdf.Explorer/Assets/stream.png")));

    public ObservableCollection<EntryInfoNode> SubNodes { get; } = [];

    public string Name { get; }

    public EntryInfo EntryInfo { get; }

    public Storage? Storage { get; }

    public EntryInfoNode? Parent { get; }

    public bool IsStorage => EntryInfo.Type == EntryType.Storage;

    public Bitmap IconSource => IsStorage ? StorageIcon : StreamIcon;

    public string SanitizedFileName
    {
        get
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            string sanitized = string.Concat(EntryInfo.Name.Select(c => invalid.Contains(c) ? '_' : c));
            return sanitized + ".bin";
        }
    }

    public EntryInfoNode(EntryInfo entryInfo, Storage? storage = null, EntryInfoNode? parent = null)
    {
        if (entryInfo.Type is EntryType.Storage)
            ArgumentNullException.ThrowIfNull(storage, nameof(storage));

        EntryInfo = entryInfo;
        Name = entryInfo.FriendlyName();
        Storage = storage;
        Parent = parent;

        AddChildren();
    }

    public void AddChildren()
    {
        if (Storage is null)
            return;

        IEnumerable<EntryInfo> entries = Storage.EnumerateEntries()
            .OrderBy(e => e.Type)
            .ThenBy(e => e.Name);

        foreach (EntryInfo entry in entries)
        {
            Storage? childStorage = entry.Type is EntryType.Storage
                ? Storage.OpenStorage(entry.Name)
                : null;

            EntryInfoNode childNode = new(entry, childStorage, this);
            SubNodes.Add(childNode);
        }
    }

    public EntryInfoNode AddChildStorage(Storage childStorage)
    {
        EntryInfoNode child = new(childStorage.EntryInfo, childStorage, this);
        InsertSorted(child);
        return child;
    }

    public EntryInfoNode AddChildStream(EntryInfo entryInfo)
    {
        EntryInfoNode child = new(entryInfo, null, this);
        InsertSorted(child);
        return child;
    }

    public void RemoveChild(string name)
    {
        EntryInfoNode? child = SubNodes.FirstOrDefault(n => n.EntryInfo.Name == name);
        if (child is not null)
            SubNodes.Remove(child);
    }

    private void InsertSorted(EntryInfoNode child)
    {
        int index = SubNodes
            .TakeWhile(n => n.EntryInfo.Type < child.EntryInfo.Type ||
                           (n.EntryInfo.Type == child.EntryInfo.Type &&
                            string.Compare(n.EntryInfo.Name, child.EntryInfo.Name, StringComparison.Ordinal) <= 0))
            .Count();
        SubNodes.Insert(index, child);
    }
}
