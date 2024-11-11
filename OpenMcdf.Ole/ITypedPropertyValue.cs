namespace OpenMcdf.Ole
{
    public interface ITypedPropertyValue : IProperty
    {
        VTPropertyType VTType
        {
            get;
            //set;
        }

        PropertyDimensions PropertyDimensions
        {
            get;
        }

        bool IsVariant
        {
            get;
        }
    }
}
