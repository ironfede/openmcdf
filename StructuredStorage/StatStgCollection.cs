using System.Collections;
using Windows.Win32.System.Com;
using Windows.Win32.System.Com.StructuredStorage;

namespace StructuredStorage;

/// <summary>
/// Creates an enumerator for <c>STATSTG</c> elements from a <c>Storage</c>.
/// </summary>
internal sealed class StatStgCollection : IEnumerable<STATSTG>
{
    readonly IStorage storage;

    public StatStgCollection(IStorage storage)
    {
        this.storage = storage;
    }

    public IEnumerator GetEnumerator() => new StatStgEnumerator(storage);

    IEnumerator<STATSTG> IEnumerable<STATSTG>.GetEnumerator() => new StatStgEnumerator(storage);
}
