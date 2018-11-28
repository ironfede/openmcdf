using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OpenMcdf.Extensions.OLEProperties.Interfaces;

namespace OpenMcdf.Extensions.OLEProperties
{
    public class OLEPropertiesContainer
    {
        public OLEPropertiesContainer UserDefinedProperties { get; private set; }

        public bool HasUserDefinedProperties { get; private set; }

        public ContainerType ContainerType { get; internal set; }

        private PropertyContext ctx = new PropertyContext
        {
            Behavior = Behavior.CaseSensitive
        };


        private List<OLEProperty> properties = new List<OLEProperty>();
        internal CFStream cfStream;

        internal OLEPropertiesContainer()
        {

        }

        internal OLEPropertiesContainer(CFStream cfStream)
        {
            PropertySetStream pStream = new PropertySetStream();

            this.cfStream = cfStream;
            pStream = new OLEProperties.PropertySetStream();
            pStream.Read(new BinaryReader(new StreamDecorator(cfStream)));

            switch (pStream.FMTID0.ToString("B").ToUpperInvariant())
            {
                case "{F29F85E0-4FF9-1068-AB91-08002B27B3D9}":
                    this.ContainerType = ContainerType.SummaryInfo;
                    break;
                case "{D5CDD502-2E9C-101B-9397-08002B2CF9AE}":
                    this.ContainerType = ContainerType.DocumentSummaryInfo;
                    break;
                default:
                    this.ContainerType = ContainerType.AppSpecific;
                    break;
            }

            for (int i = 0; i < pStream.PropertySet0.Properties.Count; i++)
            {
                var p = pStream.PropertySet0.Properties[i];
                var poi = pStream.PropertySet0.PropertyIdentifierAndOffsets[i];

                var op = new OLEProperty(this);

                op.VTType = p.VTType;
                op.PropertyIdentifier = (uint)pStream.PropertySet0.PropertyIdentifierAndOffsets[i].PropertyIdentifier;
                op.Value = p.Value;

                properties.Add(op);
            }

            if (pStream.NumPropertySets == 2)
            {
                UserDefinedProperties = new OLEPropertiesContainer();
                this.HasUserDefinedProperties = true;
                UserDefinedProperties.ContainerType = ContainerType.UserDefinedProperties;

                for (int i = 0; i < pStream.PropertySet1.Properties.Count; i++)
                {
                    var p = pStream.PropertySet1.Properties[i];
                    var poi = pStream.PropertySet1.PropertyIdentifierAndOffsets[i];

                    var op = new OLEProperty(this);

                    op.VTType = p.VTType;
                    op.PropertyIdentifier = (uint)pStream.PropertySet1.PropertyIdentifierAndOffsets[i].PropertyIdentifier;
                    op.Value = p.Value;

                    UserDefinedProperties.properties.Add(op);
                }
               
            }
        }

        public IEnumerable<OLEProperty> Properties
        {
            get { return properties; }
        }

        public OLEProperty NewProperty(VTPropertyType vtPropertyType, uint propertyIdentifier, string propertyName = null)
        {
            throw new NotImplementedException("API Unstable - Work in progress - Milestone 2.3.0.0");
            var op = new OLEProperty(this);
            op.VTType = vtPropertyType;
            op.PropertyIdentifier = propertyIdentifier;

            return op;
        }

        public void AddProperty(OLEProperty property)
        {
            throw new NotImplementedException("API Unstable - Work in progress - Milestone 2.3.0.0");
            properties.Add(property);
        }

        public void RemoveProperty(uint propertyIdentifier)
        {
            throw new NotImplementedException("API Unstable - Work in progress - Milestone 2.3.0.0");
            var toRemove = properties.Where(o => o.PropertyIdentifier == propertyIdentifier).FirstOrDefault();

            if (toRemove != null)
                properties.Remove(toRemove);
        }


        public void Save(CFStream cfStream)
        {
            throw new NotImplementedException("API Unstable - Work in progress - Milestone 2.3.0.0");

            properties.Sort((a, b) => a.PropertyIdentifier.CompareTo(b.PropertyIdentifier));

            Stream s = new StreamDecorator(cfStream);
            BinaryWriter bw = new BinaryWriter(s);

            PropertySetStream ps = new PropertySetStream();

            ps.ByteOrder = 0xFFFE;
            ps.Version = 0;
            ps.SystemIdentifier = 0x00020006;
            ps.CLSID = Guid.Empty;
            ps.NumPropertySets = 1;
            ps.FMTID0 = this.ContainerType == ContainerType.SummaryInfo ? new Guid("{F29F85E0-4FF9-1068-AB91-08002B27B3D9}") : new Guid("{D5CDD502-2E9C-101B-9397-08002B2CF9AE}");
            ps.Offset0 = 0x30;
            ps.FMTID1 = Guid.Empty;
            ps.Offset1 = 0;
            ps.PropertySet0 = new PropertySet();
            ps.PropertySet0.NumProperties = (uint)this.Properties.Count() + 1;
            ps.PropertySet0.PropertyIdentifierAndOffsets = new List<PropertyIdentifierAndOffset>();
            ps.PropertySet0.Properties = new List<Interfaces.ITypedPropertyValue>();


            //if (NumPropertySets == 2)
            //{
            //    bw.Write(FMTID1.ToByteArray());// = new Guid(br.ReadBytes(16));
            //    bw.Write(Offset1);// = br.ReadUInt32();
            //}

            ps.PropertySet0 = new PropertySet();

            foreach (var op in this.Properties)
            {
                ITypedPropertyValue p = PropertyFactory.Instance.NewProperty(op.VTType, null);
                p.Value = op.Value;
                ps.PropertySet0.Properties.Add(p);
            }

            ps.Write(bw);
        }
    }
}
