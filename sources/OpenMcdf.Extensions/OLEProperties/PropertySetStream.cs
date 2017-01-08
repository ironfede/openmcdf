using OpenMcdf.Extensions.OLEProperties.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public PropertySetStream(Stream s)
        {
            BinaryReader br = new BinaryReader(s);
            Read(br);
        }

        private void Read(BinaryReader br)
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
            PropertySet0.Read(Offset0, br);

            if (NumPropertySets == 2)
            {
                PropertySet1 = new PropertySet();
                PropertySet1.Read(Offset1, br);
            }
        }

        public void Write(Stream outStream)
        {
            BinaryWriter bw = new BinaryWriter(outStream);
            bw.Write(ByteOrder);
            bw.Write(Version);
            bw.Write(SystemIdentifier);
            bw.Write(CLSID.ToByteArray());
            bw.Write(NumPropertySets);
            bw.Write(FMTID0.ToByteArray());
            bw.Write(Offset0);

            if (NumPropertySets == 2)
            {
                bw.Write(FMTID1.ToByteArray());
                bw.Write(Offset1);
            }

            PropertySet0.Write(Offset0, bw);

            if (NumPropertySets == 2)
            {
                PropertySet1.Write(Offset1, bw);
            }
        }

    }
}
