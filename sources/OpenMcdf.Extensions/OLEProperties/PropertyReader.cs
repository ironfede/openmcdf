using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using OpenMcdf.Extensions.OLEProperties.Interfaces;
using System.Collections;

namespace OpenMcdf.Extensions.OLEProperties
{
    public enum Behavior
    {
        CaseSensitive, CaseInsensitive
    }

    public class PropertyContext
    {

        public Int32 CodePage { get; set; }
        public Behavior Behavior { get; set; }
        public UInt32 Locale { get; set; }
    }

    public enum PropertyDimensions
    {
        IsScalar, IsVector, IsArray
    }

    //public class PropertyResult
    //{
    //    public PropertyDimensions Dimensions { get; set; }
    //    public uint[] DimSizes { get; set; }
    //    public List<ITypedPropertyValue> DimValues { get; set; }
    //}

    public class PropertyReader
    {

        private PropertyContext ctx = new PropertyContext();


        public PropertyReader()
        {

        }

        public ITypedPropertyValue ReadProperty(uint propertyIdentifier, BinaryReader br)
        {
            List<ITypedPropertyValue> res = new List<ITypedPropertyValue>();

            VTPropertyType vType = (VTPropertyType)br.ReadUInt16();
            br.ReadUInt16(); // Ushort Padding

            ITypedPropertyValue pr = PropertyFactory.Instance.NewProperty(vType, ctx);
            pr.Read(br);

            if (propertyIdentifier == 1)
            {
                this.ctx.CodePage = (short)pr.Value;
            }

            return pr;
        }
    }
}
