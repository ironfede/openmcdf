using System.IO;

namespace OpenMcdf.Extensions.OLEProperties.Interfaces
{
    public interface IDictionaryProperty : IProperty
    {
        void Read(BinaryReader br);
        void Write(BinaryWriter bw);
    }
}