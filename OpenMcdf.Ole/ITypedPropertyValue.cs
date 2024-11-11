namespace OpenMcdf.Ole;

public enum PropertyDimensions
{
    IsScalar,
    IsVector,
    IsArray
}

public interface ITypedPropertyValue : IProperty
{
    VTPropertyType VTType { get; }

    PropertyDimensions PropertyDimensions { get; }

    bool IsVariant { get; }
}
