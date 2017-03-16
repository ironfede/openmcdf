using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenMcdf.Extensions.OLEProperties.Interfaces
{
    public interface ITypedPropertyValue : IBinarySerializable
    {
        PropertyDimensions Dimensions
        {
            get;
        }


        object PropertyValue
        {
            get;
            set;
        }

        VTPropertyType VTType
        {
            get;
            //set;
        }
    }
}
