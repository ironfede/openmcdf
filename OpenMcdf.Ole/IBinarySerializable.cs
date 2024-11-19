namespace OpenMcdf.Ole;

internal interface IBinarySerializable
{
    void Write(BinaryWriter bw);
    void Read(BinaryReader br);
}
