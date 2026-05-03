namespace OpenMcdf.Ole;

internal class PropertySet
{
    public PropertyContext PropertyContext { get; set; } = new();

    public uint Size { get; set; }

    public List<PropertyIdentifierAndOffset> PropertyIdentifierAndOffsets { get; }

    public List<IProperty> Properties { get; }

    public virtual PropertyFactory PropertyFactory { get; } = DefaultPropertyFactory.Default;

    public DictionaryProperty? DictionaryProperty { get; private set; }

    public PropertySet(BinaryReader br, uint propertySetOffset)
    {
        this.Size = br.ReadUInt32();

        uint propertyCount = br.ReadUInt32();

        // Read property offsets
        // @@TODO@@ Clamp propertyCount when reservice space in the collection in case it's a bad value? (e.g. corrupt so it's a massive number)
        this.PropertyIdentifierAndOffsets = new((int)propertyCount);
        for (int i = 0; i < propertyCount; i++)
        {
            PropertyIdentifierAndOffset pio = PropertyIdentifierAndOffset.Read(br);
            PropertyIdentifierAndOffsets.Add(pio);
        }

        // Treat the code page property specially - we need it to read all the code page specific properties.
        // @@TODO@@ It would be nice to not read the property twice (the general property loop does it again)
        this.PropertyContext.CodePage = ReadCodePage(br, propertySetOffset);

        // Read properties
        this.Properties = new(this.PropertyIdentifierAndOffsets.Count);
        for (int i = 0; i < propertyCount; i++)
        {
            PropertyIdentifierAndOffset propertyIdentifierAndOffset = this.PropertyIdentifierAndOffsets[i];
            br.BaseStream.Seek(propertySetOffset + propertyIdentifierAndOffset.Offset, SeekOrigin.Begin);
            IProperty property = ReadProperty(propertyIdentifierAndOffset.PropertyIdentifier, this.PropertyContext.CodePage, br, this.PropertyFactory);
            this.Properties.Add(property);
        }

        // Load additional context properties
        LoadContext();
    }

    public PropertySet(PropertyContext propertyContext, int initialPropertyCount)
    {
        this.PropertyContext = propertyContext;
        this.PropertyIdentifierAndOffsets = new(initialPropertyCount);
        this.Properties = new(initialPropertyCount);
    }

    protected virtual int ReadCodePage(BinaryReader br, uint propertySetOffset)
    {
        int codePagePropertyIndex = PropertyIdentifierAndOffsets.FindIndex(static pio => pio.PropertyIdentifier == SpecialPropertyIdentifiers.CodePage);
        if (codePagePropertyIndex == -1)
        {
            throw new FileFormatException("Required CodePage property not present");
        }

        long currPos = br.BaseStream.Position;
        int codePageOffset = (int)(propertySetOffset + PropertyIdentifierAndOffsets[codePagePropertyIndex].Offset);

        br.BaseStream.Seek(codePageOffset, SeekOrigin.Begin);

        var vType = (VTPropertyType)br.ReadUInt16();
        br.ReadUInt16(); // Ushort Padding

        var codePage = (ushort)br.ReadInt16();

        br.BaseStream.Position = currPos;

        return codePage;
    }

    // Populate additional context properties, if present
    private void LoadContext()
    {
        // Read the Locale, if present
        int localePropertyIndex = PropertyIdentifierAndOffsets.FindIndex(static pio => pio.PropertyIdentifier == SpecialPropertyIdentifiers.Locale);
        if (localePropertyIndex != -1)
        {
            IProperty localeProperty = Properties[localePropertyIndex];
            if (localeProperty is ITypedPropertyValue { VTType: VTPropertyType.VT_UI4, Value: not null } typedValue)
            {
                this.PropertyContext.Locale = (uint)typedValue.Value!;
            }
        }
    }

    public void Add(Dictionary<uint, string> propertyNames)
    {
        this.DictionaryProperty = new(PropertyContext.CodePage, propertyNames);
        Properties.Add(this.DictionaryProperty);
        PropertyIdentifierAndOffsets.Add(new PropertyIdentifierAndOffset(SpecialPropertyIdentifiers.Dictionary, 0));
    }

    public void AddProperty(VTPropertyType vType, uint propertyIdentifier, object? value)
    {
        ITypedPropertyValue p = this.PropertyFactory.CreateProperty(vType, PropertyContext.CodePage, propertyIdentifier);
        p.Value = value;
        this.Properties.Add(p);
        this.PropertyIdentifierAndOffsets.Add(new PropertyIdentifierAndOffset(propertyIdentifier, 0));
    }

    private IProperty ReadProperty(uint propertyIdentifier, int codePage, BinaryReader br, PropertyFactory factory)
    {
        if (propertyIdentifier == SpecialPropertyIdentifiers.Dictionary)
        {
            this.DictionaryProperty = new(codePage);
            this.DictionaryProperty.Read(br);
            return this.DictionaryProperty;
        }

        var vType = (VTPropertyType)br.ReadUInt16();
        br.ReadUInt16(); // Ushort Padding

        ITypedPropertyValue pr = factory.CreateProperty(vType, codePage, propertyIdentifier);
        pr.Read(br);

        return pr;
    }
}

internal sealed class DocumentSummaryInformationPropertySet : PropertySet
{
    public DocumentSummaryInformationPropertySet(BinaryReader br, uint propertySetOffset)
        : base(br, propertySetOffset)
    {
    }

    public DocumentSummaryInformationPropertySet(PropertyContext propertyContext, int initialPropertyCount)
        : base(propertyContext, initialPropertyCount)
    {
    }

    public override PropertyFactory PropertyFactory { get; } = DocumentSummaryInfoPropertyFactory.Default;
}
