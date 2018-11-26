using System;
using System.Collections.Generic;
using System.Text;

namespace OpenMcdf.Extensions.OLEProperties.Interfaces
{
    public interface ITypedPropertyValue : IBinarySerializable
    {

        object Value
        {
            get;
            set;
        }

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
