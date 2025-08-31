using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com.StructuredStorage;

namespace StructuredStorage;

/// <summary>
/// Wraps <c>IPropertyStorage</c>.
/// </summary>
public sealed class PropertyStorage : IDisposable
{
    private readonly IPropertyStorage propertyStorage;
    private bool disposed;

    internal unsafe PropertyStorage(IPropertyStorage propertyStorage)
    {
        this.propertyStorage = propertyStorage;
        StatPropStgCollection = new(propertyStorage);

        STATPROPSETSTG prop;
        this.propertyStorage.Stat(&prop);
    }

    #region IDisposable Members

    public void Dispose()
    {
        if (disposed)
            return;

        int count = Marshal.ReleaseComObject(propertyStorage);
        Debug.Assert(count == 0);

        disposed = true;
    }

    #endregion

    internal StatPropStgCollection StatPropStgCollection { get; }

    public void Flush(CommitFlags flags = CommitFlags.Default) => propertyStorage.Commit((uint)flags);

    public unsafe void Remove(int propertyID)
    {
        PROPSPEC propspec = new()
        {
            ulKind = PROPSPEC_KIND.PRSPEC_PROPID,
            Anonymous = new PROPSPEC._Anonymous_e__Union()
            {
                propid = (uint)propertyID,
            },
        };
        propertyStorage.DeleteMultiple(1, &propspec);
    }

    public void Revert() => propertyStorage.Revert();

    public unsafe object? this[int propertyID]
    {
        get
        {
            PROPSPEC spec = PropVariantExtensions.CreatePropSpec(PROPSPEC_KIND.PRSPEC_PROPID, propertyID);

            var variants = new PROPVARIANT[1];
            propertyStorage.ReadMultiple(1, &spec, variants);
            HRESULT hr = PInvoke.PropVariantToVariant(variants[0], out object variant);
            hr.ThrowOnFailure();
            return variant;
        }

        set
        {
            PROPSPEC spec = PropVariantExtensions.CreatePropSpec(PROPSPEC_KIND.PRSPEC_PROPID, propertyID);

            HRESULT hr = PInvoke.VariantToPropVariant(value, out PROPVARIANT pv);
            hr.ThrowOnFailure();

            PROPVARIANT[] pvs = [pv];
            propertyStorage.WriteMultiple(1, &spec, pvs, 2);
        }
    }
}
