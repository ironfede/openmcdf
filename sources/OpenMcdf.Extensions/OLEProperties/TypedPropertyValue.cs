using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using OpenMcdf.Extensions.OLEProperties.Interfaces;
using System.Linq;

namespace OpenMcdf.Extensions.OLEProperties
{
    internal abstract class TypedPropertyValue<T> : ITypedPropertyValue
    {
        private bool isVariant = false;
        private PropertyDimensions dim = PropertyDimensions.IsScalar;

        private VTPropertyType _VTType;

        public PropertyType PropertyType
        {
            get
            {
                return PropertyType.TypedPropertyValue;
            }
        }

        public VTPropertyType VTType
        {
            get { return _VTType; }
        }

        protected object propertyValue = null;

        public TypedPropertyValue(VTPropertyType vtType, bool isVariant = false)
        {
            this._VTType = vtType;
            dim = CheckPropertyDimensions(vtType);
            this.isVariant = isVariant;
        }

        public PropertyDimensions PropertyDimensions { get { return dim; } }

        public bool IsVariant
        {
            get { return isVariant; }
        }

        private PropertyDimensions CheckPropertyDimensions(VTPropertyType vtType)
        {
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

        public abstract T ReadScalarValue(System.IO.BinaryReader br);


        public void Read(System.IO.BinaryReader br)
        {
            long currentPos = br.BaseStream.Position;
            int size = 0;
            int m = 0;

            switch (this.PropertyDimensions)
            {
                case PropertyDimensions.IsScalar:
                    this.propertyValue = ReadScalarValue(br);
                    size = (int)(br.BaseStream.Position - currentPos);

                    m = (int)size % 4;

                    if (m > 0 && !IsVariant)
                        br.ReadBytes(4 - m); // padding
                    break;

                case PropertyDimensions.IsVector:
                    uint nItems = br.ReadUInt32();

                    List<T> res = new List<T>();


                    for (int i = 0; i < nItems; i++)
                    {
                        T s = ReadScalarValue(br);

                        res.Add(s);
                    }

                    this.propertyValue = res;
                    size = (int)(br.BaseStream.Position - currentPos);

                    //m = (int)size % 4;
                    //if (m > 0 && !IsVariant)
                    //    br.ReadBytes(4 - m); // padding
                    break;
                default:
                    break;
            }
        }

        public abstract void WriteScalarValue(System.IO.BinaryWriter bw, T pValue);

        public void Write(BinaryWriter bw)
        {
            long currentPos = bw.BaseStream.Position;
            int size = 0;
            int m = 0;
            bool needsPadding = HasPadding();

            switch (this.PropertyDimensions)
            {
                case PropertyDimensions.IsScalar:

                    bw.Write((ushort)_VTType);
                    bw.Write((ushort)0);

                    WriteScalarValue(bw, (T)this.propertyValue);
                    size = (int)(bw.BaseStream.Position - currentPos);
                    m = (int)size % 4;

                    if (m > 0 && needsPadding)
                        for (int i = 0; i < 4 - m; i++)  // padding
                            bw.Write((byte)0);
                    break;

                case PropertyDimensions.IsVector:

                    bw.Write((ushort)_VTType);
                    bw.Write((ushort)0);
                    bw.Write((uint)((List<T>)this.propertyValue).Count);

                    for (int i = 0; i < ((List<T>)this.propertyValue).Count; i++)
                    {
                        WriteScalarValue(bw, ((List<T>)this.propertyValue)[i]);
                    }

                    size = (int)(bw.BaseStream.Position - currentPos);
                    m = (int)size % 4;

                    if (m > 0 && needsPadding)
                        for (int i = 0; i < m; i++)  // padding
                            bw.Write((byte)0);
                    break;
            }
        }

        private bool HasPadding()
        {

            VTPropertyType vt = (VTPropertyType)((ushort)this.VTType & 0x00FF);

            switch (vt)
            {
                case VTPropertyType.VT_LPSTR:
                    if (this.IsVariant) return false;
                    if (dim == PropertyDimensions.IsVector) return false;
                    break;
                case VTPropertyType.VT_VARIANT_VECTOR:
                    if (dim == PropertyDimensions.IsVector) return false;
                    break;
                default:
                    return true;
            }

            return true;
        }
    }
}
