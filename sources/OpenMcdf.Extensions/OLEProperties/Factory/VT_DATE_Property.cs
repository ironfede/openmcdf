using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenMcdf.Extensions.OLEProperties.Factory
{
    internal partial class PropertyFactory
    {
        private class VT_DATE_Property : TypedPropertyValue
        {
            public VT_DATE_Property(VTPropertyType vType, PropertyContext ctx, PropertyDimensions dim) : base(vType, ctx, dim)
            {

            }

            public override void Read(System.IO.BinaryReader br)
            {
                switch (Dimensions)
                {
                    case PropertyDimensions.IsScalar:
                        Double temp = br.ReadDouble();
                        this.propertyValue = DateTime.FromOADate(temp);
                        break;
                    case PropertyDimensions.IsVector:
                        uint size = br.ReadUInt32();
                        var t = new List<DateTime>();
                        for (int i = 0; i < size; i++)
                        {
                            t.Add(DateTime.FromOADate(br.ReadDouble()));
                        }
                        this.propertyValue = t;
                        break;
                }
            }

            public override void Write(System.IO.BinaryWriter bw)
            {
                switch (Dimensions)
                {
                    case PropertyDimensions.IsScalar:
                        bw.Write(((DateTime)propertyValue).ToOADate());

                        break;
                    case PropertyDimensions.IsVector:
                        uint size = (uint)(((List<DateTime>)propertyValue).Count);
                        bw.Write(size);
                        foreach (var d in ((List<DateTime>)propertyValue))
                        {
                            bw.Write(d.ToOADate());
                        }
                        break;
                }

            }
        }
    }
}
