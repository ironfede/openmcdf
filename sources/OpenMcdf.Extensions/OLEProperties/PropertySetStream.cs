using OpenMcdf.Extensions.OLEProperties.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenMcdf.Extensions.OLEProperties
{
    public class PropertySetStream
    {
        public ushort ByteOrder { get; set; }
        public ushort Version { get; set; }
        public uint SystemIdentifier { get; set; }
        public Guid CLSID { get; set; }
        public uint NumPropertySets { get; set; }
        public Guid FMTID0 { get; set; }
        public uint Offset0 { get; set; }
        public Guid FMTID1 { get; set; }
        public uint Offset1 { get; set; }
        public PropertySet PropertySet0 { get; set; }
        public PropertySet PropertySet1 { get; set; }

        //private SummaryInfoMap map;

        public PropertySetStream()
        {

        }

        public void Read(System.IO.BinaryReader br)
        {
            ByteOrder = br.ReadUInt16();
            Version = br.ReadUInt16();
            SystemIdentifier = br.ReadUInt32();
            CLSID = new Guid(br.ReadBytes(16));
            NumPropertySets = br.ReadUInt32();
            FMTID0 = new Guid(br.ReadBytes(16));
            Offset0 = br.ReadUInt32();

            if (NumPropertySets == 2)
            {
                FMTID1 = new Guid(br.ReadBytes(16));
                Offset1 = br.ReadUInt32();
            }

            PropertySet0 = new PropertySet();
            PropertySet0.Size = br.ReadUInt32();
            PropertySet0.NumProperties = br.ReadUInt32();


            // Read property offsets
            for (int i = 0; i < PropertySet0.NumProperties; i++)
            {
                PropertyIdentifierAndOffset pio = new PropertyIdentifierAndOffset();
                pio.PropertyIdentifier = br.ReadUInt32();
                pio.Offset = br.ReadUInt32();
                PropertySet0.PropertyIdentifierAndOffsets.Add(pio);
            }

         

            // Read properties
            PropertyReader pr = new PropertyReader();
            for (int i = 0; i < PropertySet0.NumProperties; i++)
            {
                br.BaseStream.Seek(Offset0 + PropertySet0.PropertyIdentifierAndOffsets[i].Offset, System.IO.SeekOrigin.Begin);
                PropertySet0.Properties.Add(pr.ReadProperty(PropertySet0.PropertyIdentifierAndOffsets[i].PropertyIdentifier, br));
            }

            if (NumPropertySets == 2)
            {
                br.BaseStream.Seek(Offset1, System.IO.SeekOrigin.Begin);
                PropertySet1 = new PropertySet();
                PropertySet1.Size = br.ReadUInt32();
                PropertySet1.NumProperties = br.ReadUInt32();

                // Read property offsets
                for (int i = 0; i < PropertySet1.NumProperties; i++)
                {
                    PropertyIdentifierAndOffset pio = new PropertyIdentifierAndOffset();
                    pio.PropertyIdentifier = br.ReadUInt32();
                    pio.Offset = br.ReadUInt32();
                    PropertySet1.PropertyIdentifierAndOffsets.Add(pio);
                }

                // Read properties

                for (int i = 0; i < PropertySet1.NumProperties; i++)
                {
                    br.BaseStream.Seek(Offset1 + PropertySet1.PropertyIdentifierAndOffsets[i].Offset, System.IO.SeekOrigin.Begin);
                    PropertySet1.Properties.Add(pr.ReadProperty(PropertySet0.PropertyIdentifierAndOffsets[i].PropertyIdentifier, br));
                }
            }
        }

        private class OffsetContainer
        {
            public long OffsetP0Size { get; set; }
            public List<long> PropertyIdentifierOffsets { get; set; }
            public List<long> PropertyOffsets { get; set; }

            public OffsetContainer()
            {
                this.PropertyOffsets = new List<long>();
                this.PropertyIdentifierOffsets = new List<long>();
            }
        }

        public void Write(System.IO.BinaryWriter bw)
        {
            var oc = new OffsetContainer();

            bw.Write(ByteOrder); //   ByteOrder = br.ReadUInt16();
            bw.Write(Version);// = br.ReadUInt16();
            bw.Write(SystemIdentifier); // = br.ReadUInt32();
            bw.Write(CLSID.ToByteArray()); // = new Guid(br.ReadBytes(16));
            bw.Write(NumPropertySets);// = br.ReadUInt32();
            bw.Write(FMTID0.ToByteArray());// = new Guid(br.ReadBytes(16));
            bw.Write(Offset0); // = br.ReadUInt32();

            if (NumPropertySets == 2)
            {
                bw.Write(FMTID1.ToByteArray());// = new Guid(br.ReadBytes(16));
                bw.Write(Offset1);// = br.ReadUInt32();
            }

            PropertySet0 = new PropertySet();

            oc.OffsetP0Size = bw.BaseStream.Position;

            bw.Write(PropertySet0.Size);
            bw.Write(PropertySet0.NumProperties);

            // w property offsets
            for (int i = 0; i < PropertySet0.PropertyIdentifierAndOffsets.Count; i++)
            {
                oc.PropertyOffsets.Add(bw.BaseStream.Position + 4); //Offset of 4 to Offset value
                PropertySet0.PropertyIdentifierAndOffsets[i].Write(bw);
            }

            for (int i = 0; i < PropertySet0.NumProperties; i++)
            {
                oc.PropertyOffsets.Add(bw.BaseStream.Position);
                PropertySet0.Properties[i].Write(bw);
            }

            for (int i = 0; i < PropertySet0.PropertyIdentifierAndOffsets.Count; i++)
            {
                bw.Seek((int)oc.PropertyOffsets[i], System.IO.SeekOrigin.Begin); //Offset of 4 to Offset value
                bw.Write(oc.PropertyOffsets[i] - oc.OffsetP0Size);
            }

            bw.Seek((int)oc.OffsetP0Size, System.IO.SeekOrigin.Begin);

            foreach (var op in PropertySet0.Properties)
            {
                op.Write(bw);
            }

        }

    }
}