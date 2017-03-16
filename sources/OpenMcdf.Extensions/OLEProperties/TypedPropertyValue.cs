using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMcdf.Extensions.OLEProperties.Interfaces;

namespace OpenMcdf.Extensions.OLEProperties
{
    internal class TypedPropertyValue : ITypedPropertyValue
    {
        private VTPropertyType _VTType;
        private PropertyDimensions dim = PropertyDimensions.IsScalar;
        private PropertyContext ctx;

        protected PropertyContext Ctx
        {
            get { return ctx; }
        }

        public VTPropertyType VTType
        {
            get { return _VTType; }
        }

        public TypedPropertyValue(VTPropertyType vtType, PropertyContext ctx = null, PropertyDimensions dim = PropertyDimensions.IsScalar)
        {
            this._VTType = vtType;
            this.dim = dim;
            this.ctx = ctx;
        }

        protected object propertyValue = null;

        public virtual object PropertyValue
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


        public PropertyDimensions Dimensions
        {
            get
            {
                return dim;
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
