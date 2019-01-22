using System;
using System.Collections.Generic;
using System.Text;

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
