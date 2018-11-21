using System;
using System.Collections.Generic;
using System.Text;
using OpenMcdf.Extensions.OLEProperties.Interfaces;

namespace OpenMcdf.Extensions.OLEProperties
{
    internal class TypedPropertyValue : ITypedPropertyValue, IBinarySerializable
    {
        private bool isVariant = false;
        private PropertyDimensions dim = PropertyDimensions.IsScalar;

        private VTPropertyType _VTType;

        public VTPropertyType VTType
        {
            get { return _VTType; }
            //set { _VTType = value; }
        }

        protected object propertyValue = null;

        public TypedPropertyValue(VTPropertyType vtType)
        {
            this._VTType = vtType;
            dim = CheckPropertyDimensions(vtType);
        }

        public PropertyDimensions PropertyDimensions { get { return dim; } }

        public bool IsVariant
        {
            get { return isVariant; }
        }

        private PropertyDimensions CheckPropertyDimensions(VTPropertyType vtType)
        {
            isVariant = ((((ushort)vtType) & 0x00FF) == 0x000C);

            if ((((ushort)vtType) & 0x1000) != 0)
                return PropertyDimensions.IsVector;
            else if ((((ushort)vtType) & 0x2000) != 0)
                return PropertyDimensions.IsArray;
            else
                return PropertyDimensions.IsScalar;
        }

        public virtual object Value
        {
            get
            {
                return propertyValue;
            }

            set
            {
                propertyValue = value;
            }
        }


        public virtual void Read(System.IO.BinaryReader br)
        {


        }

        public virtual void Write(System.IO.BinaryWriter bw)
        {

        }
    }
}
