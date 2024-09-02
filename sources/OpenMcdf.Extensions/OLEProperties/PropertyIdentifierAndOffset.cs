using OpenMcdf.Extensions.OLEProperties.Interfaces;
using System.IO;

namespace OpenMcdf.Extensions.OLEProperties
{
    public class PropertyIdentifierAndOffset : IBinarySerializable
    {
        public uint PropertyIdentifier { get; set; }
        public uint Offset { get; set; }

        public void Read(BinaryReader br)
        {
            PropertyIdentifier = br.ReadUInt32();
            Offset = br.ReadUInt32();
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write(PropertyIdentifier);
            bw.Write(Offset);
        }
    }
}