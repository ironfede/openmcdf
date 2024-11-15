namespace OpenMcdf.Ole;

public sealed class OleProperty
{
    private readonly OlePropertiesContainer container;
    object? value;

    internal OleProperty(OlePropertiesContainer container)
    {
        this.container = container;
    }

    public string PropertyName => PropertyIdentifiers.GetDescription(PropertyIdentifier, container.ContainerType, container.PropertyNames);

    public uint PropertyIdentifier { get; internal set; }

    public VTPropertyType VTType { get; internal set; }

    public object? Value
    {
        get
        {
            switch (VTType)
            {
                case VTPropertyType.VT_LPSTR:
                case VTPropertyType.VT_LPWSTR:
                    if (value is string str && !string.IsNullOrEmpty(str))
                        return str.Trim('\0');
                    break;
                default:
                    return value;
            }

            return value;
        }
        set => this.value = value;
    }

    public override bool Equals(object? obj) => obj is OleProperty other && other.PropertyIdentifier == PropertyIdentifier;

    public override int GetHashCode() => (int)PropertyIdentifier;

    public override string ToString() => $"{PropertyName} - {VTType} - {Value}";
}
