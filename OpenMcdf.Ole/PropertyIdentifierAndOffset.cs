namespace OpenMcdf.Ole;

internal readonly record struct PropertyIdentifierAndOffset(uint PropertyIdentifier, uint Offset)
{
    public static PropertyIdentifierAndOffset Read(BinaryReader br)
    {
        uint propertyIdentifier = br.ReadUInt32();
        uint offset = br.ReadUInt32();
        return new(propertyIdentifier, offset);
    }

    public void Write(BinaryWriter bw)
    {
        bw.Write(PropertyIdentifier);
        bw.Write(Offset);
    }
}
