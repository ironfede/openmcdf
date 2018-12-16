using System.Collections.Generic;
using System.IO;
using OpenMcdf.Extensions.OLEProperties.Interfaces;

namespace OpenMcdf.Extensions.OLEProperties
{
    internal abstract class TypedPropertyValue<T> : ITypedPropertyValue
    {
        protected object propertyValue;

        public TypedPropertyValue(VTPropertyType vtType, bool isVariant = false)
        {
            VTType = vtType;
            PropertyDimensions = CheckPropertyDimensions(vtType);
            IsVariant = isVariant;
        }

        public PropertyType PropertyType => PropertyType.TypedPropertyValue;

        public VTPropertyType VTType { get; }

        public PropertyDimensions PropertyDimensions { get; } = PropertyDimensions.IsScalar;

        public bool IsVariant { get; }

        public virtual object Value
        {
            get => propertyValue;

            set => propertyValue = value;
        }


        public void Read(BinaryReader br)
        {
            var currentPos = br.BaseStream.Position;
            var size = 0;
            var m = 0;

            switch (PropertyDimensions)
            {
                case PropertyDimensions.IsScalar:
                    propertyValue = ReadScalarValue(br);
                    size = (int) (br.BaseStream.Position - currentPos);

                    m = size % 4;

                    if (m > 0 && !IsVariant)
                        br.ReadBytes(m); // padding
                    break;

                case PropertyDimensions.IsVector:
                    var nItems = br.ReadUInt32();

                    var res = new List<T>();


                    for (var i = 0; i < nItems; i++)
                    {
                        var s = ReadScalarValue(br);

                        res.Add(s);
                    }

                    propertyValue = res;
                    size = (int) (br.BaseStream.Position - currentPos);

                    m = size % 4;
                    if (m > 0 && !IsVariant)
                        br.ReadBytes(m); // padding
                    break;
            }
        }

        public void Write(BinaryWriter bw)
        {
            var currentPos = bw.BaseStream.Position;
            var size = 0;
            var m = 0;
            var needsPadding = HasPadding();

            switch (PropertyDimensions)
            {
                case PropertyDimensions.IsScalar:

                    bw.Write((ushort) VTType);
                    bw.Write((ushort) 0);

                    WriteScalarValue(bw, (T) propertyValue);
                    size = (int) (bw.BaseStream.Position - currentPos);
                    m = size % 4;

                    if (m > 0 && needsPadding)
                        for (var i = 0; i < m; i++) // padding
                            bw.Write((byte) 0);
                    break;

                case PropertyDimensions.IsVector:

                    bw.Write((ushort) VTType);
                    bw.Write((ushort) 0);
                    bw.Write((uint) ((List<T>) propertyValue).Count);

                    for (var i = 0; i < ((List<T>) propertyValue).Count; i++)
                        WriteScalarValue(bw, ((List<T>) propertyValue)[i]);

                    size = (int) (bw.BaseStream.Position - currentPos);
                    m = size % 4;

                    if (m > 0 && needsPadding)
                        for (var i = 0; i < m; i++) // padding
                            bw.Write((byte) 0);
                    break;
            }
        }

        private PropertyDimensions CheckPropertyDimensions(VTPropertyType vtType)
        {
            if (((ushort) vtType & 0x1000) != 0)
                return PropertyDimensions.IsVector;
            if (((ushort) vtType & 0x2000) != 0)
                return PropertyDimensions.IsArray;
            return PropertyDimensions.IsScalar;
        }

        public abstract T ReadScalarValue(BinaryReader br);

        public abstract void WriteScalarValue(BinaryWriter bw, T pValue);

        private bool HasPadding()
        {
            var vt = (VTPropertyType) ((ushort) VTType & 0x00FF);

            switch (vt)
            {
                case VTPropertyType.VT_LPSTR:
                    if (IsVariant) return false;
                    if (PropertyDimensions == PropertyDimensions.IsVector) return false;
                    break;
                case VTPropertyType.VT_VARIANT_VECTOR:
                    if (PropertyDimensions == PropertyDimensions.IsVector) return false;
                    break;
                default:
                    return true;
            }

            return true;
        }
    }
}