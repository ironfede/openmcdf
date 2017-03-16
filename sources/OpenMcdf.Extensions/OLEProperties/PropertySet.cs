using OpenMcdf.Extensions.OLEProperties.Factory;
using OpenMcdf.Extensions.OLEProperties.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace OpenMcdf.Extensions.OLEProperties
{
    public class PropertySet
    {
        public uint Size { get; set; }
        public uint NumProperties { get; set; }
        private bool isPersisted = true;
        private PropertyIdentifiersBase b = null;

        List<PropertyIdentifierAndOffset> propertyIdentifierAndOffsets
            = new List<PropertyIdentifierAndOffset>();

        public List<PropertyIdentifierAndOffset> PropertyIdentifierAndOffsets
        {
            get { return propertyIdentifierAndOffsets; }
            set { propertyIdentifierAndOffsets = value; }
        }

        List<ITypedPropertyValue> properties = new List<ITypedPropertyValue>();

        public List<ITypedPropertyValue> Properties
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

        public PropertySet(Guid fmtd)
        {
            string s = fmtd.ToString().ToUpper();
            switch (s)
            {
                case "F29F85E0-4FF9-1068-AB91-08002B27B3D9":
                    b = new PropertyIdentifiersSummaryInfo();
                    break;
                case "D5CDD502-2E9C-101B-9397-08002B2CF9AE":
                    b = new PropertyIdentifiersDocumentSummaryInfo();
                    break;
                case "D5CDD505-2E9C-101B-9397-08002B2CF9AE":
                    b = new PropertyIdentifiersDocumentSummaryInfo();  // USER DEFINED
                    break;
                default:
                    b = new PropertyIdentifiersSummaryInfo();
                    break;
            }
        }

        internal void AddProperty(uint propertyIdentifier, VTPropertyType vtType, object value)
        {
            PropertyFactory pf = new PropertyFactory();
            ITypedPropertyValue pr = pf.NewProperty(vtType);
            pr.PropertyValue = value;
            properties.Add(pr);
            propertyIdentifierAndOffsets.Add(new PropertyIdentifierAndOffset { Offset = 0, PropertyIdentifier = propertyIdentifier });
            NumProperties++;
            isPersisted = false;
        }

        internal void Read(uint offset, BinaryReader br)
        {
            this.Size = br.ReadUInt32();
            this.NumProperties = br.ReadUInt32();

            // Read property offsets
            for (int i = 0; i < NumProperties; i++)
            {
                PropertyIdentifierAndOffset pio = new PropertyIdentifierAndOffset();
                pio.PropertyIdentifier = br.ReadUInt32();
                pio.Offset = br.ReadUInt32();
                PropertyIdentifierAndOffsets.Add(pio);
            }

            Dictionary<uint, string> r = new Dictionary<uint, string>();
            PropertyReader pr = new PropertyReader(b);
            for (int i = 0; i < NumProperties; i++)
            {
                br.BaseStream.Seek(offset + PropertyIdentifierAndOffsets[i].Offset, SeekOrigin.Begin);
                Properties.Add(pr.ReadProperty(PropertyIdentifierAndOffsets[i].PropertyIdentifier, br, out r));
            }
        }

        public void Write(uint offset, BinaryWriter bw)
        {
            bw.BaseStream.Seek(offset, SeekOrigin.Begin);

            for (int i = 0; i < NumProperties; i++)
            {
                Properties[i].Write(bw);
                PropertyIdentifierAndOffsets[i].Offset = (uint)bw.BaseStream.Position - offset;
            }

            //TODO: Write Dictionary

        }

        private PropertyContext GetPropertySetContext()
        {
            var context = new PropertyContext();

            var b = propertyIdentifierAndOffsets.Select((v, i) => new { p = v, index = i }).FirstOrDefault(z => z.p.PropertyIdentifier == 0x80000003);
            if (b != null)
            {
                context.Behavior = (int)properties[b.index].PropertyValue == 1 ? Behavior.CaseSensitive : Behavior.CaseInsensitive;
            }
            else
                context.Behavior = Behavior.CaseInsensitive;

            var c = propertyIdentifierAndOffsets.Select((v, i) => new { p = v, index = i }).FirstOrDefault(z => z.p.PropertyIdentifier == 1);
            context.CodePage = (int)properties[c.index].PropertyValue;

            var l = propertyIdentifierAndOffsets.Select((v, i) => new { p = v, index = i }).FirstOrDefault(z => z.p.PropertyIdentifier == 0x80000000);
            context.Locale = (uint)properties[l.index].PropertyValue;

            return context;
        }


    }
}
