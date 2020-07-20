using OpenMcdf.Extensions.OLEProperties.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;

namespace OpenMcdf.Extensions.OLEProperties
{
    internal class PropertySet
    {

        public PropertyContext PropertyContext
        {
            get;  set;
        }

        public uint Size { get; set; }

        public uint NumProperties { get; set; }

        List<PropertyIdentifierAndOffset> propertyIdentifierAndOffsets
            = new List<PropertyIdentifierAndOffset>();

        public List<PropertyIdentifierAndOffset> PropertyIdentifierAndOffsets
        {
            get { return propertyIdentifierAndOffsets; }
            set { propertyIdentifierAndOffsets = value; }
        }

        List<IProperty> properties = new List<IProperty>();
        public List<IProperty> Properties
        {
            get
            {
                return properties;
            }
            set
            {
                properties = value;
            }
        }

        public void LoadContext(int propertySetOffset, BinaryReader br)
        {
            var currPos = br.BaseStream.Position;

            PropertyContext = new PropertyContext();
            var codePageOffset = (int)(propertySetOffset + PropertyIdentifierAndOffsets.Where(pio => pio.PropertyIdentifier == 1).First().Offset);
            br.BaseStream.Seek(codePageOffset, SeekOrigin.Begin);

            VTPropertyType vType = (VTPropertyType)br.ReadUInt16();
            br.ReadUInt16(); // Ushort Padding
            PropertyContext.CodePage = (int)(ushort)br.ReadInt16();

            br.BaseStream.Position = currPos;
        }

    }
}
