using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com.StructuredStorage;

namespace StructuredStorage;

/// <summary>
/// Encapsulates <c>ILockBytes</c> over an HGlobal allocation.
/// </summary>
internal sealed class LockBytes : IDisposable
{
    readonly ILockBytes lockBytes;
    private bool disposedValue;

    public LockBytes(int count)
    {
        IntPtr hGlobal = Marshal.AllocHGlobal(count);
        HRESULT hr = PInvoke.CreateILockBytesOnHGlobal((HGLOBAL)hGlobal, true, out lockBytes);
        hr.ThrowOnFailure();
    }

    public LockBytes(MemoryStream stream)
    {
        var hGlobal = (HGLOBAL)Marshal.AllocHGlobal((int)stream.Length);
        Marshal.Copy(stream.GetBuffer(), 0, hGlobal, (int)stream.Length);
        HRESULT hr = PInvoke.CreateILockBytesOnHGlobal(hGlobal, true, out lockBytes);
        hr.ThrowOnFailure();
    }

    public void Dispose()
    {
        if (disposedValue)
            return;

        int count = Marshal.ReleaseComObject(lockBytes);
        Debug.Assert(count == 0);

        disposedValue = true;
        GC.SuppressFinalize(this);
    }

    ~LockBytes()
    {
        Dispose();
    }

    internal ILockBytes ILockBytes => lockBytes;
}
