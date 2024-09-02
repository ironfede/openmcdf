using System.IO;

namespace OpenMcdf.Extensions.OLEProperties.Interfaces
{
    public interface IBinarySerializable
    {
        void Write(BinaryWriter bw);
        void Read(BinaryReader br);
    }
}
