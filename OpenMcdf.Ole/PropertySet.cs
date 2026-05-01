namespace OpenMcdf.Ole;

internal sealed class PropertySet
{
    public PropertyContext PropertyContext { get; set; } = new();

    public uint Size { get; set; }

    public List<PropertyIdentifierAndOffset> PropertyIdentifierAndOffsets { get; } = new();

    public List<IProperty> Properties { get; } = new();

    public void LoadContext(int propertySetOffset, BinaryReader br, Guid fmtID)
    {
        long currPos = br.BaseStream.Position;

        // Read the code page
        // The 'HwpSummaryInformation' stream doesn't contain a codepage, but we treat it as mandatory for all other streams
        PropertyIdentifierAndOffset? codePageProperty = PropertyIdentifierAndOffsets.FirstOrDefault(pio => pio.PropertyIdentifier == SpecialPropertyIdentifiers.CodePage);
        if (codePageProperty is null)
        {
            // For HWP streams, treat the default codpage as UTF-8
            // NOTE: This is what various other HWP readers do, but I don't presently have a test file for that - all the files I#ve seen only use VT_LPWSTR properties which are always CP_WINUNICODE
            if (fmtID == FormatIdentifiers.HwpSummaryInformation)
            {
                PropertyContext.CodePage = 65001;
            }
            else
            {
                throw new FileFormatException("Required CodePage property not present");
            }
        }
        else
        {
            int codePageOffset = (int)(propertySetOffset + codePageProperty.Value.Offset);
            br.BaseStream.Seek(codePageOffset, SeekOrigin.Begin);

            var vType = (VTPropertyType)br.ReadUInt16();
            br.ReadUInt16(); // Ushort Padding
            PropertyContext.CodePage = (ushort)br.ReadInt16();
        }

        // Read the Locale, if present
        PropertyIdentifierAndOffset? localeProperty = PropertyIdentifierAndOffsets.FirstOrDefault(pio => pio.PropertyIdentifier == SpecialPropertyIdentifiers.Locale);
        if (localeProperty is not null)
        {
            long localeOffset = propertySetOffset + localeProperty.Value.Offset;
            br.BaseStream.Seek(localeOffset, SeekOrigin.Begin);

            var vType = (VTPropertyType)br.ReadUInt16();
            br.ReadUInt16(); // Ushort Padding
            PropertyContext.Locale = br.ReadUInt32();
        }

        br.BaseStream.Position = currPos;
    }

    public void Add(IDictionary<uint, string> propertyNames)
    {
        DictionaryProperty dictionaryProperty = new(PropertyContext.CodePage)
        {
            Value = propertyNames,
        };
        Properties.Add(dictionaryProperty);
        PropertyIdentifierAndOffsets.Add(new PropertyIdentifierAndOffset(SpecialPropertyIdentifiers.Dictionary, 0));
    }
}
