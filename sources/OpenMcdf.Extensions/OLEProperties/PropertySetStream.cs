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
                pio.PropertyIdentifier = (PropertyIdentifiersSummaryInfo)br.ReadUInt32();
                pio.Offset = br.ReadUInt32();
                PropertySet0.PropertyIdentifierAndOffsets.Add(pio);
            }

            // Read properties
            PropertyReader pr = new PropertyReader();
            for (int i = 0; i < PropertySet0.NumProperties; i++)
            {
                br.BaseStream.Seek(Offset0 + PropertySet0.PropertyIdentifierAndOffsets[i].Offset, System.IO.SeekOrigin.Begin);
                PropertySet0.Properties.AddRange(pr.ReadProperty(PropertySet0.PropertyIdentifierAndOffsets[i].PropertyIdentifier, br));
            }
        }

        public void Write(System.IO.BinaryWriter bw)
        {
            throw new NotImplementedException();
        }

        //        private void LoadFromStream(Stream inStream)
        //        {
        //            BinaryReader br = new BinaryReader(inStream);
        //            PropertySetStream psStream = new PropertySetStream();
        //            psStream.Read(br);
        //            br.Close();

        //            propertySets.Clear();

        //            if (psStream.NumPropertySets == 1)
        //            {
        //                propertySets.Add(psStream.PropertySet0);
        //            }
        //            else
        //            {
        //                propertySets.Add(psStream.PropertySet0);
        //                propertySets.Add(psStream.PropertySet1);
        //            }

        //            return;
        //        }
    }
}
