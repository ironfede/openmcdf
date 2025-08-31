using Windows.Win32.System.Com.StructuredStorage;

namespace StructuredStorage;

static class PropVariantExtensions
{
    public static PROPSPEC CreatePropSpec(PROPSPEC_KIND kind, int propertyID)
    {
        return new PROPSPEC
        {
            ulKind = kind,
            Anonymous = new PROPSPEC._Anonymous_e__Union
            {
                propid = (uint)propertyID,
            },
        };
    }
}
