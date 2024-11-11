namespace OpenMcdf.Ole;

internal sealed class PropertySet
{
    public PropertyContext PropertyContext
    {
        get; set;
    }

    public uint Size { get; set; }

    public uint NumProperties { get; set; }

    public List<PropertyIdentifierAndOffset> PropertyIdentifierAndOffsets { get; set; } = new List<PropertyIdentifierAndOffset>();

    public List<IProperty> Properties { get; set; } = new List<IProperty>();

    public void LoadContext(int propertySetOffset, BinaryReader br)
    {
        long currPos = br.BaseStream.Position;

        PropertyContext = new PropertyContext();
        int codePageOffset = (int)(propertySetOffset + PropertyIdentifierAndOffsets.Where(pio => pio.PropertyIdentifier == 1).First().Offset);
        br.BaseStream.Seek(codePageOffset, SeekOrigin.Begin);

        VTPropertyType vType = (VTPropertyType)br.ReadUInt16();
        br.ReadUInt16(); // Ushort Padding
        PropertyContext.CodePage = (ushort)br.ReadInt16();

        br.BaseStream.Position = currPos;
    }
}
