using System.Collections;
using Windows.Win32.System.Com.StructuredStorage;

namespace StructuredStorage;

/// <summary>
/// Creates an enumerator for <c>STATPROPSTG</c> elements from a <c>PropertyStorage</c>.
/// </summary>
internal sealed class StatPropStgCollection : IEnumerable<STATPROPSTG>
{
    readonly IPropertyStorage propertyStorage;

    public StatPropStgCollection(IPropertyStorage propertyStorage)
    {
        this.propertyStorage = propertyStorage;
    }

    public IEnumerator<STATPROPSTG> GetEnumerator() => new StatPropStgEnumerator(propertyStorage);

    IEnumerator IEnumerable.GetEnumerator() => new StatPropStgEnumerator(propertyStorage);
}
