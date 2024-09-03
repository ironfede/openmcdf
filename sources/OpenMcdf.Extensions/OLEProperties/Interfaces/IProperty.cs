namespace OpenMcdf.Extensions.OLEProperties.Interfaces
{
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
}
