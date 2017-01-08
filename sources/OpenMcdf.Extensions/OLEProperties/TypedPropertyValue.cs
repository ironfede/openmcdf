using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMcdf.Extensions.OLEProperties.Interfaces;

namespace OpenMcdf.Extensions.OLEProperties
{
    public class TypedPropertyValue : ITypedPropertyValue
    {
        private VTPropertyType _VTType;
        private bool isVector = false;
        private bool isArray = false;

        public VTPropertyType VTType
        {
            get { return _VTType; }
        }

        public TypedPropertyValue(VTPropertyType vtType, bool isVector = false, bool isArray = false)
        {
            this._VTType = vtType;
            this.isVector = isVector;
            this.isArray = isArray;
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


        public bool IsArray
        {
            get
            {
                return isArray;
            }
        }

        public bool IsVector
        {
            get
            {
                return isVector;
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
