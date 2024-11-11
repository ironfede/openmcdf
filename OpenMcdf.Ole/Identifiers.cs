namespace OpenMcdf.Ole;

public static class Identifiers
{
    public static string GetDescription(uint identifier, ContainerType map, IDictionary<uint, string> customDict = null)
    {
        IDictionary<uint, string> nameDictionary = customDict;

        if (nameDictionary is null)
        {
            switch (map)
            {
                case ContainerType.SummaryInfo:
                    nameDictionary = PropertyIdentifiers.SummaryInfo;
                    break;
                case ContainerType.DocumentSummaryInfo:
                    nameDictionary = PropertyIdentifiers.DocumentSummaryInfo;
                    break;
            }
        }

        if (nameDictionary?.TryGetValue(identifier, out string? value) == true)
            return value;

        return $"0x{identifier:x8}";
    }
}
