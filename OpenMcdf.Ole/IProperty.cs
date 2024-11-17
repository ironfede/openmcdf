namespace OpenMcdf.Ole;

internal enum PropertyType
{
    TypedPropertyValue = 0,
    DictionaryProperty = 1
}

internal interface IProperty : IBinarySerializable
{
    object? Value { get; set; }

    PropertyType PropertyType { get; }
}
