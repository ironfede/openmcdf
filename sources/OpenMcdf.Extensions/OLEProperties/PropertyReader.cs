using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using OpenMcdf.Extensions.OLEProperties.Interfaces;
using System.Collections;

namespace OpenMcdf.Extensions.OLEProperties
{
   

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
                this.ctx.CodePage = (int)(ushort)(short)pr.Value;
            }

            return pr;
        }
    }
}
