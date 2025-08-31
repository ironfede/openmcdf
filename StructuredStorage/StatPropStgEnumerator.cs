using System.Collections;
using System.Runtime.InteropServices;
using Windows.Win32.System.Com.StructuredStorage;

namespace StructuredStorage;

/// <summary>
/// Enumerates <c>STATPROPSTG</c> elements from a <c>PropertyStorage</c>.
/// </summary>
internal sealed class StatPropStgEnumerator : IEnumerator<STATPROPSTG>
{
    readonly IEnumSTATPROPSTG enumerator;
    STATPROPSTG propStat;

    public STATPROPSTG Current => propStat;

    object IEnumerator.Current => propStat;

    public unsafe StatPropStgEnumerator(IPropertyStorage propertyStorage)
    {
        propertyStorage.Enum(out enumerator);
    }

    public unsafe void Dispose()
    {
        FreeName();

        Marshal.ReleaseComObject(enumerator);
    }

    private unsafe void FreeName()
    {
        Marshal.FreeCoTaskMem((nint)propStat.lpwstrName.Value);
        propStat.lpwstrName = null;
    }

    public unsafe bool MoveNext()
    {
        FreeName();

        fixed (STATPROPSTG* statPtr = &propStat)
        {
            uint fetched;
            enumerator.Next(1, statPtr, &fetched);
            return fetched > 0;
        }
    }

    public void Reset()
    {
        FreeName();

        enumerator.Reset();
    }
}
