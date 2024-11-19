namespace OpenMcdf.Ole;

internal enum PropertyDimensions
{
    IsScalar,
    IsVector,
    IsArray
}

internal interface ITypedPropertyValue : IProperty
{
    VTPropertyType VTType { get; }

    PropertyDimensions PropertyDimensions { get; }

    bool IsVariant { get; }
}
