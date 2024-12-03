namespace OpenMcdf.Ole;

internal sealed class PropertySet
{
    public PropertyContext PropertyContext { get; set; } = new();

    public uint Size { get; set; }

    public List<PropertyIdentifierAndOffset> PropertyIdentifierAndOffsets { get; } = new();

    public List<IProperty> Properties { get; } = new();

    public void LoadContext(int propertySetOffset, BinaryReader br)
    {
        long currPos = br.BaseStream.Position;
        int codePageOffset = (int)(propertySetOffset + PropertyIdentifierAndOffsets.First(pio => pio.PropertyIdentifier == SpecialPropertyIdentifiers.CodePage).Offset);
        br.BaseStream.Seek(codePageOffset, SeekOrigin.Begin);

        var vType = (VTPropertyType)br.ReadUInt16();
        br.ReadUInt16(); // Ushort Padding
        PropertyContext.CodePage = (ushort)br.ReadInt16();

        br.BaseStream.Position = currPos;
    }

    public void Add(IDictionary<uint, string> propertyNames)
    {
        DictionaryProperty dictionaryProperty = new(PropertyContext.CodePage)
        {
            Value = propertyNames
        };
        Properties.Add(dictionaryProperty);
        PropertyIdentifierAndOffsets.Add(new PropertyIdentifierAndOffset() { PropertyIdentifier = SpecialPropertyIdentifiers.Dictionary, Offset = 0 });
    }
}
