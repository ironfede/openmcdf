using Windows.Win32;
using Windows.Win32.System.Com.StructuredStorage;

namespace StructuredStorage;

/// <summary>
/// Wraps <c>IPropertySetStorage</c>.
/// </summary>
public sealed class PropertySetStorage
{
    /// <summary>
    /// PROPSETFLAG constants.
    /// </summary>
    [Flags]
#pragma warning disable CA1008
    public enum Flags
    {
        Default = (int)PInvoke.PROPSETFLAG_DEFAULT,
        NonSimple = (int)PInvoke.PROPSETFLAG_NONSIMPLE,
        ANSI = (int)PInvoke.PROPSETFLAG_ANSI,
        Unbuffered = (int)PInvoke.PROPSETFLAG_UNBUFFERED,
        CaseSensitive = (int)PInvoke.PROPSETFLAG_CASE_SENSITIVE,
    }
#pragma warning restore CA1008

    private readonly IPropertySetStorage propSet; // Cast of IStorage does not need disposal

    internal PropertySetStorage(IStorage storage)
    {
        propSet = (IPropertySetStorage)storage;
    }

    public PropertyStorage Create(Guid formatID, StorageModes mode) => Create(formatID, Flags.Default, mode, Guid.Empty);

    public PropertyStorage Create(Guid formatID, Flags flags = Flags.Default, StorageModes mode = StorageModes.ShareExclusive | StorageModes.AccessReadWrite) => Create(formatID, flags, mode, Guid.Empty);

    public unsafe PropertyStorage Create(Guid formatID, Flags flags, StorageModes mode, Guid classID)
    {
        propSet.Create(&formatID, &classID, (uint)flags, (uint)mode, out IPropertyStorage stg);
        return new(stg);
    }

    public unsafe PropertyStorage Open(Guid formatID, StorageModes mode = StorageModes.ShareExclusive | StorageModes.AccessReadWrite)
    {
        propSet.Open(&formatID, (uint)mode, out IPropertyStorage propStorage);
        return new(propStorage);
    }

    public unsafe void Remove(Guid formatID) => propSet.Delete(&formatID);
}
