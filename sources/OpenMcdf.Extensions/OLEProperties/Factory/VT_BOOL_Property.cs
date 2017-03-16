using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace OpenMcdf.Extensions.OLEProperties.Factory
{
    internal partial class PropertyFactory
    {
        private class VT_BOOL_Property : TypedPropertyValue
        {
            private PropertyDimensions d = PropertyDimensions.IsScalar;

            public VT_BOOL_Property(VTPropertyType vType, PropertyContext ctx, PropertyDimensions dim) : base(vType, ctx, dim)
            {
                d = dim;
            }

            public override void Read(BinaryReader br)
            {
                switch (d)
                {

                    case PropertyDimensions.IsScalar:
                        this.propertyValue = br.ReadUInt16() == (ushort)0xFFFF ? true : false;
                        break;
                    case PropertyDimensions.IsVector:
                        var size = br.ReadUInt32();
                        this.propertyValue = new List<bool>();

                        for (int i = 0; i < size; i++)
                        {
                            ((List<bool>)this.propertyValue).Add(br.ReadUInt16() == (ushort)0xFFFF ? true : false);
                        }

                        break;
                    default:
                        throw new NotImplementedException("Dimension " + d.ToString() + " has not been implemented yet");
                }

                //br.ReadUInt16();//padding
            }

            public override void Write(BinaryWriter bw)
            {
                switch (d)
                {
                    case PropertyDimensions.IsScalar:
                        if ((bool)propertyValue)
                            bw.Write((ushort)0xFFFF);
                        else
                            bw.Write((ushort)0x0000);

                        bw.Write((ushort)0x0000);//padding
                        break;

                    case PropertyDimensions.IsVector:
                        bw.Write((uint)((List<bool>)propertyValue).Count);
                        foreach (var b in ((List<bool>)propertyValue))
                        {
                            if (b)
                                bw.Write((ushort)0xFFFF);
                            else
                                bw.Write((ushort)0x0000);
                        }

                        if (((List<bool>)propertyValue).Count > 0 && ((List<bool>)propertyValue).Count % 2 != 0)
                        {
                            bw.Write((ushort)0x0000); // padding
                        }

                        break;
                }
            }
        }
    }
}
