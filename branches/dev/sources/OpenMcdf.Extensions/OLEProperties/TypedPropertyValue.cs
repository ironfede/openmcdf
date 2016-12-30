using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMcdf.Extensions.OLEProperties.Interfaces;

namespace OpenMcdf.Extensions.OLEProperties
{
    public class TypedPropertyValue : ITypedPropertyValue, IBinarySerializable
    {
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
        }
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
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public bool IsVector
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
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
