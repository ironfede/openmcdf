namespace OpenMcdf.Ole;

public enum PropertyType
{
    TypedPropertyValue = 0,
    DictionaryProperty = 1
}

public interface IProperty : IBinarySerializable
{
    object Value
    {
        get;
        set;
    }

    PropertyType PropertyType
    {
        get;
    }
}
