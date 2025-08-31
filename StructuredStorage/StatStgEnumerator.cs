using System.Collections;
using System.Runtime.InteropServices;
using Windows.Win32.System.Com;
using Windows.Win32.System.Com.StructuredStorage;

namespace StructuredStorage;

/// <summary>
/// Enumerates <c>STATSTG</c> elements from a <c>Storage</c>.
/// </summary>
internal sealed class StatStgEnumerator : IEnumerator<STATSTG>
{
    readonly IEnumSTATSTG enumerator;
    STATSTG stat;

    public STATSTG Current => stat;

    object IEnumerator.Current => stat;

    public unsafe StatStgEnumerator(IStorage storage)
    {
        storage.EnumElements(0, null, 0, out enumerator);
    }

    public unsafe void Dispose()
    {
        FreeName();

        Marshal.ReleaseComObject(enumerator);
    }

    private unsafe void FreeName()
    {
        Marshal.FreeCoTaskMem((nint)stat.pwcsName.Value);
        stat.pwcsName = null;
    }

    public unsafe bool MoveNext()
    {
        FreeName();

        fixed (STATSTG* statPtr = &stat)
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
