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
    public class PropertyReader
    {

        private PropertyContext ctx = new PropertyContext();
        private PropertyFactory factory = null;
        private PropertyIdentifiersBase identifiers;

        public PropertyReader(PropertyIdentifiersBase identifiers)
        {
            factory = new PropertyFactory();
            this.identifiers = identifiers;
        }

        public ITypedPropertyValue ReadProperty(
            uint propertyIdentifier,
            BinaryReader br,
            out Dictionary<uint, string> propertyDictionary)
        {
            propertyDictionary = new Dictionary<uint, string>();
          
            if (propertyIdentifier != 0)
            {
                UInt16 pVal = br.ReadUInt16();
                br.ReadUInt16(); //padding short

                ITypedPropertyValue property = factory.NewProperty((VTPropertyType)pVal, ctx);

                property.Read(br);

                if (propertyIdentifier == PropertyIdentifiersBase.CodePageString)
                {
                    this.ctx.CodePage = (short)property.PropertyValue;
                }

                return property;
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
