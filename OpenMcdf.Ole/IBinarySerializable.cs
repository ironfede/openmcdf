namespace OpenMcdf.Ole;

public interface IBinarySerializable
{
    void Write(BinaryWriter bw);
    void Read(BinaryReader br);
}
