using System.Text;

namespace OpenMcdf.Ole;

public class DictionaryEntry
{
    readonly int codePage;
    private byte[]? nameBytes;

    public DictionaryEntry(int codePage)
    {
        this.codePage = codePage;
    }

    public uint PropertyIdentifier { get; set; }

    public int Length { get; set; }

    public string Name
    {
        get
        {
            if (nameBytes is null)
                return string.Empty;

            string result = Encoding.GetEncoding(codePage).GetString(nameBytes);
            result = result.Trim('\0');
            return result;
        }
    }

    public void Read(BinaryReader br)
    {
        PropertyIdentifier = br.ReadUInt32();
        Length = br.ReadInt32();

        if (codePage == CodePages.WinUnicode)
        {
            nameBytes = br.ReadBytes(Length << 1);

            int m = Length * 2 % 4;
            if (m > 0)
                br.ReadBytes(m);
        }
        else
        {
            nameBytes = br.ReadBytes(Length);
        }
    }

    public void Write(BinaryWriter bw)
    {
        bw.Write(PropertyIdentifier);
        bw.Write(Length);
        bw.Write(nameBytes!);
    }
}
