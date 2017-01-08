using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OpenMcdf.Extensions.OLEProperties.Interfaces;
using System.Collections;
using OpenMcdf.Extensions.OLEProperties.Factory;

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
        private PropertyFactory factory = null;

        public PropertyReader()
        {
            factory = new PropertyFactory();
        }

        public ITypedPropertyValue ReadProperty(
            uint propertyIdentifier,
            BinaryReader br,
            out Dictionary<uint, string> propertyDictionary)
        {
            propertyDictionary = new Dictionary<uint, string>();
            List<ITypedPropertyValue> res = new List<ITypedPropertyValue>();

            if (propertyIdentifier != 0)
            {

                bool isVariant = false;

                PropertyDimensions dim = PropertyDimensions.IsScalar;

                UInt16 pVal = br.ReadUInt16();

                //VTPropertyType vType = (VTPropertyType)(pVal & 0x00FF);
                ITypedPropertyValue property = factory.NewProperty((VTPropertyType)pVal, ctx);

                //        ITypedPropertyValue pr = factory.NewProperty(vType, ctx);

                property.Read(br);

                if (propertyIdentifier == (uint)PropertyIdentifiersSummaryInfo.CodePageString)
                {
                    this.ctx.CodePage = (short)property.PropertyValue;
                }

                return property;

                //                isVariant = ((pVal & 0x00FF) == 0x000C);

                br.ReadUInt16(); // Ushort Padding

                //switch (dim)
                //{
                //    case PropertyDimensions.IsVector:

                //        ITypedPropertyValue vectorHeader = factory.NewProperty(VTPropertyType.VT_VECTOR_HEADER, ctx);
                //        vectorHeader.Read(br);

                //        uint nItems = (uint)vectorHeader.PropertyValue;

                //        for (int i = 0; i < nItems; i++)
                //        {
                //            VTPropertyType vTypeItem = VTPropertyType.VT_EMPTY;

                //            if (isVariant)
                //            {
                //                UInt16 pValItem = br.ReadUInt16();
                //                vTypeItem = (VTPropertyType)(pValItem & 0x00FF);
                //                br.ReadUInt16(); // Ushort Padding
                //            }
                //            else
                //            {
                //                vTypeItem = vType;
                //            }

                //            var p = factory.NewProperty(vTypeItem, ctx);

                //            p.Read(br);
                //            res.Add(p);
                //        }

                //        break;
                //    default:
                //        // Scalar property, it could be a standard property or a special one as Dictionary or CodePage;

                //        ITypedPropertyValue pr = factory.NewProperty(vType, ctx);

                //        pr.Read(br);

                //        if (propertyIdentifier == (uint)PropertyIdentifiersSummaryInfo.CodePageString)
                //        {
                //            this.ctx.CodePage = (short)pr.PropertyValue;
                //        }

                //        res.Add(pr);
                //        break;
                //}
            }
            else
            {
                var numEntries = br.ReadUInt32();

                int totLength = 0;

                for (int j = 0; j < numEntries; j++)
                {
                    var propertyId = br.ReadUInt32();
                    int length = (int)br.ReadUInt32();  // number of characters including null terminator

                    length--; //remove null;

                    if (ctx.CodePage == 0x04B0)  // if unicode, double length (16 bit characters)
                        length *= 2;

                    totLength += length;

                    var prName = Encoding.GetEncoding(ctx.CodePage).GetString(br.ReadBytes(length));

                    propertyDictionary.Add(propertyId, prName);

                    if (ctx.CodePage == 0x04B0) // if unicode, padding to multiple of 4
                    {
                        int m = (int)length % 4;
                        totLength += m;
                        br.ReadBytes(m);
                    }
                }

                int totPad = (int)totLength % 4;
                br.ReadBytes(totPad);
            }

            return null;
        }
    }
}
