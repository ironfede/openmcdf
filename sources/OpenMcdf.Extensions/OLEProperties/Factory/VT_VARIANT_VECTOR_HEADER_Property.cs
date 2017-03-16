using OpenMcdf.Extensions.OLEProperties.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenMcdf.Extensions.OLEProperties.Factory
{
    internal partial class PropertyFactory
    {
        private class VT_VARIANT_VECTOR_HEADER_Property : TypedPropertyValue
        {
            private uint size = 0;
            private byte[] data;
            private PropertyContext ctx;

            public VT_VARIANT_VECTOR_HEADER_Property(VTPropertyType vType, PropertyContext ctx) : base(vType)
            {
                this.ctx = ctx;
            }

            public override void Read(System.IO.BinaryReader br)
            {
                this.propertyValue = new List<ITypedPropertyValue>();
                size = br.ReadUInt32();

                PropertyFactory f = new PropertyFactory();

                for (int i = 0; i < size; i++)
                {
                    VTPropertyType vt = (VTPropertyType)br.ReadUInt16();
                    br.ReadUInt16();//pad
                    var t = f.NewProperty(vt, ctx);
                    t.Read(br);
                    ((List<ITypedPropertyValue>)this.propertyValue).Add(t);
                }
            }

            public override void Write(System.IO.BinaryWriter bw)
            { 

            }
        }

    }
}
