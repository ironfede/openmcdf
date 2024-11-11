using OpenMcdf3;

namespace OpenMcdf.Ole;

public enum ContainerType
{
    AppSpecific = 0,
    SummaryInfo = 1,
    DocumentSummaryInfo = 2,
    UserDefinedProperties = 3,
    GlobalInfo = 4,
    ImageContents = 5,
    ImageInfo = 6
}

public class OlePropertiesContainer
{
    public Dictionary<uint, string>? PropertyNames { get; private set; }

    public OlePropertiesContainer? UserDefinedProperties { get; private set; }

    public ContainerType ContainerType { get; }
    private Guid? FmtID0 { get; }

    public PropertyContext Context { get; }

    private readonly List<OleProperty> properties = new();
    internal Stream? cfStream;

    public OlePropertiesContainer(int codePage, ContainerType containerType)
    {
        Context = new PropertyContext
        {
            CodePage = codePage,
            Behavior = Behavior.CaseInsensitive
        };

        ContainerType = containerType;
    }

    public OlePropertiesContainer(CfbStream cfStream)
    {
        PropertySetStream pStream = new();

        this.cfStream = cfStream;

        cfStream.Position = 0;
        using BinaryReader reader = new(cfStream);
        pStream.Read(reader);

        if (pStream.FMTID0 == FormatIdentifiers.SummaryInformation)
            ContainerType = ContainerType.SummaryInfo;
        else if (pStream.FMTID0 == FormatIdentifiers.DocSummaryInformation)
            ContainerType = ContainerType.DocumentSummaryInfo;
        else
            ContainerType = ContainerType.AppSpecific;
        FmtID0 = pStream.FMTID0;

        PropertyNames = (Dictionary<uint, string>?)pStream.PropertySet0!.Properties
            .FirstOrDefault(p => p.PropertyType == PropertyType.DictionaryProperty)?.Value;

        Context = new PropertyContext()
        {
            CodePage = pStream.PropertySet0.PropertyContext.CodePage
        };

        for (int i = 0; i < pStream.PropertySet0.Properties.Count; i++)
        {
            PropertyIdentifierAndOffset propertyIdentifierAndOffset = pStream.PropertySet0.PropertyIdentifierAndOffsets[i];
            if (propertyIdentifierAndOffset.PropertyIdentifier == 0) continue;
            //if (propertyIdentifierAndOffset.PropertyIdentifier == 1) continue;
            //if (propertyIdentifierAndOffset.PropertyIdentifier == 0x80000000) continue;

            var p = (ITypedPropertyValue)pStream.PropertySet0.Properties[i];

            OleProperty op = new(this)
            {
                VTType = p.VTType,
                PropertyIdentifier = propertyIdentifierAndOffset.PropertyIdentifier,
                Value = p.Value
            };

            properties.Add(op);
        }

        if (pStream.NumPropertySets == 2)
        {
            PropertySet propertySet1 = pStream.PropertySet1!;
            UserDefinedProperties = new OlePropertiesContainer(propertySet1.PropertyContext.CodePage, ContainerType.UserDefinedProperties);

            for (int i = 0; i < propertySet1.Properties.Count; i++)
            {
                PropertyIdentifierAndOffset propertyIdentifierAndOffset = propertySet1.PropertyIdentifierAndOffsets[i];
                if (propertyIdentifierAndOffset.PropertyIdentifier is 0 or 0x80000000)
                    continue;

                var p = (ITypedPropertyValue)propertySet1.Properties[i];

                OleProperty op = new(UserDefinedProperties)
                {
                    VTType = p.VTType,
                    PropertyIdentifier = propertyIdentifierAndOffset.PropertyIdentifier,
                    Value = p.Value
                };

                UserDefinedProperties.properties.Add(op);
            }

            var existingPropertyNames = (Dictionary<uint, string>?)propertySet1.Properties
                .FirstOrDefault(p => p.PropertyType == PropertyType.DictionaryProperty)?.Value;

            UserDefinedProperties.PropertyNames = existingPropertyNames ?? new Dictionary<uint, string>();
        }
    }

    public IList<OleProperty> Properties => properties;

    public OleProperty CreateProperty(VTPropertyType vtPropertyType, uint propertyIdentifier, string? propertyName = null)
    {
        OleProperty op = new(this)
        {
            VTType = vtPropertyType,
            PropertyIdentifier = propertyIdentifier
        };

        return op;
    }

    public void Add(OleProperty property) => properties.Add(property);

    public void RemoveProperty(uint propertyIdentifier)
    {
        OleProperty? toRemove = properties.FirstOrDefault(o => o.PropertyIdentifier == propertyIdentifier);

        if (toRemove is not null)
            properties.Remove(toRemove);
    }

    /// <summary>
    /// Create a new UserDefinedProperties container within this container.
    /// </summary>
    /// <remarks>
    /// Only containers of type DocumentSummaryInfo can contain user defined properties.
    /// </remarks>
    /// <param name="codePage">The code page to use for the user defined properties.</param>
    /// <returns>The UserDefinedProperties container.</returns>
    /// <exception cref="CFInvalidOperation">If this container is a type that doesn't suppose user defined properties.</exception>
    public OlePropertiesContainer CreateUserDefinedProperties(int codePage)
    {
        // Only the DocumentSummaryInfo stream can contain a UserDefinedProperties
        if (ContainerType != ContainerType.DocumentSummaryInfo)
            throw new InvalidOperationException($"Only a DocumentSummaryInfo can contain user defined properties. Current container type is {ContainerType}");

        // Create the container, and add the code page to the initial set of properties
        UserDefinedProperties = new OlePropertiesContainer(codePage, ContainerType.UserDefinedProperties)
        {
            PropertyNames = new Dictionary<uint, string>()
        };

        var op = new OleProperty(UserDefinedProperties)
        {
            VTType = VTPropertyType.VT_I2,
            PropertyIdentifier = 1,
            Value = (short)codePage
        };

        UserDefinedProperties.properties.Add(op);

        return UserDefinedProperties;
    }

    public void Save(Stream cfStream)
    {
        using BinaryWriter bw = new(cfStream);

        Guid fmtId0 = FmtID0 ?? (ContainerType == ContainerType.SummaryInfo ? FormatIdentifiers.SummaryInformation : FormatIdentifiers.DocSummaryInformation);

        PropertySetStream ps = new()
        {
            ByteOrder = 0xFFFE,
            Version = 0,
            SystemIdentifier = 0x00020006,
            CLSID = Guid.Empty,

            NumPropertySets = 1,

            FMTID0 = fmtId0,
            Offset0 = 0,

            FMTID1 = Guid.Empty,
            Offset1 = 0,

            PropertySet0 = new PropertySet
            {
                PropertyContext = Context
            }
        };

        // If we're writing an AppSpecific property set and have property names, then add a dictionary property
        if (ContainerType == ContainerType.AppSpecific && PropertyNames is not null && PropertyNames.Count > 0)
        {
            ps.PropertySet0.Add(PropertyNames);
        }

        PropertyFactory factory =
            ContainerType == ContainerType.DocumentSummaryInfo ? DocumentSummaryInfoPropertyFactory.Default : DefaultPropertyFactory.Default;

        foreach (OleProperty op in Properties)
        {
            ITypedPropertyValue p = factory.CreateProperty(op.VTType, Context.CodePage, op.PropertyIdentifier);
            p.Value = op.Value;
            ps.PropertySet0.Properties.Add(p);
            ps.PropertySet0.PropertyIdentifierAndOffsets.Add(new PropertyIdentifierAndOffset() { PropertyIdentifier = op.PropertyIdentifier, Offset = 0 });
        }

        if (UserDefinedProperties is not null)
        {
            ps.NumPropertySets = 2;

            ps.PropertySet1 = new PropertySet
            {
                PropertyContext = UserDefinedProperties.Context
            };

            ps.FMTID1 = FormatIdentifiers.UserDefinedProperties;
            ps.Offset1 = 0;

            // Add the dictionary containing the property names
            ps.PropertySet1.Add(UserDefinedProperties.PropertyNames!);

            // Add the properties themselves
            foreach (OleProperty op in UserDefinedProperties.Properties)
            {
                ITypedPropertyValue p = DefaultPropertyFactory.Default.CreateProperty(op.VTType, ps.PropertySet1.PropertyContext.CodePage, op.PropertyIdentifier);
                p.Value = op.Value;
                ps.PropertySet1.Properties.Add(p);
                ps.PropertySet1.PropertyIdentifierAndOffsets.Add(new PropertyIdentifierAndOffset() { PropertyIdentifier = op.PropertyIdentifier, Offset = 0 });
            }
        }

        ps.Write(bw);
    }
}
