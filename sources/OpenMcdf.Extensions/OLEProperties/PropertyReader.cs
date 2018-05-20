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
        private PropertyFactory factory = null;

        public PropertyReader()
        {
            factory = new PropertyFactory(ctx);
        }

        public List<ITypedPropertyValue> ReadProperty(PropertyIdentifiersSummaryInfo propertyIdentifier, BinaryReader br)
        {
            List<ITypedPropertyValue> res = new List<ITypedPropertyValue>();
            bool isVariant = false;
            PropertyDimensions dim = PropertyDimensions.IsScalar;

            UInt16 pVal = br.ReadUInt16();

            VTPropertyType vType = (VTPropertyType)(pVal & 0x00FF);

            if ((pVal & 0x1000) != 0)
                dim = PropertyDimensions.IsVector;
            else if ((pVal & 0x2000) != 0)
                dim = PropertyDimensions.IsArray;

            isVariant = ((pVal & 0x00FF) == 0x000C);

            br.ReadUInt16(); // Ushort Padding

            switch (dim)
            {
                case PropertyDimensions.IsVector:

                    ITypedPropertyValue vectorHeader = factory.NewProperty(VTPropertyType.VT_VECTOR_HEADER, ctx);
                    vectorHeader.Read(br);

                    uint nItems = (uint)vectorHeader.PropertyValue;

                    for (int i = 0; i < nItems; i++)
                    {
                        VTPropertyType vTypeItem = VTPropertyType.VT_EMPTY;

                        if (isVariant)
                        {
                            UInt16 pValItem = br.ReadUInt16();
                            vTypeItem = (VTPropertyType)(pValItem & 0x00FF);
                            br.ReadUInt16(); // Ushort Padding
                        }
                        else
                        {
                            vTypeItem = vType;
                        }

                        var p = factory.NewProperty(vTypeItem, ctx);

                        p.Read(br);
                        res.Add(p);
                    }

                    break;
                default:

                    //Scalar property
                    ITypedPropertyValue pr = factory.NewProperty(vType, ctx);

                    pr.Read(br);

                    if (propertyIdentifier == PropertyIdentifiersSummaryInfo.CodePageString)
                    {
                        this.ctx.CodePage = (short)pr.PropertyValue;
                    }

                    res.Add(pr);
                    break;
            }

            return res;
        }
    }
}
