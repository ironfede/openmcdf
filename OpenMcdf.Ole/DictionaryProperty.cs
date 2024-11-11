using System.Text;

namespace OpenMcdf.Ole;

public class DictionaryProperty : IProperty
{
    private readonly int codePage;
    private Dictionary<uint, string>? entries = new();

    public DictionaryProperty(int codePage)
    {
        this.codePage = codePage;
    }

    public PropertyType PropertyType => PropertyType.DictionaryProperty;

    public object? Value
    {
        get => entries;
        set => entries = (Dictionary<uint, string>?)value;
    }

    public void Read(BinaryReader br)
    {
        long curPos = br.BaseStream.Position;

        uint numEntries = br.ReadUInt32();

        for (uint i = 0; i < numEntries; i++)
        {
            DictionaryEntry de = new(codePage);

            de.Read(br);
            entries!.Add(de.PropertyIdentifier, de.Name);
        }

        int m = (int)(br.BaseStream.Position - curPos) % 4;

        if (m > 0)
        {
            for (int i = 0; i < m; i++)
            {
                br.ReadByte();
            }
        }
    }

    /// <summary>
    /// Write the dictionary and all its values into the specified <see cref="BinaryWriter"/>.
    /// </summary>
    /// <remarks>
    /// Based on the Microsoft specifications at https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-oleps/99127b7f-c440-4697-91a4-c853086d6b33
    /// </remarks>
    /// <param name="bw">A writer to write the dictionary into.</param>
    public void Write(BinaryWriter bw)
    {
        long curPos = bw.BaseStream.Position;

        bw.Write(entries!.Count);

        foreach (KeyValuePair<uint, string> kv in entries)
        {
            WriteEntry(bw, kv.Key, kv.Value);
        }

        int size = (int)(bw.BaseStream.Position - curPos);
        WritePaddingIfNeeded(bw, size);
    }

    // Write a single entry to the dictionary, and handle and required null termination and padding.
    private void WriteEntry(BinaryWriter bw, uint propertyIdentifier, string name)
    {
        // Write the PropertyIdentifier
        bw.Write(propertyIdentifier);

        // Encode string data with the current code page
        byte[] nameBytes = Encoding.GetEncoding(codePage).GetBytes(name);
        uint byteLength = (uint)nameBytes.Length;

        // If the code page is WINUNICODE, write the length as the number of characters and pad the length to a multiple of 4 bytes
        // Otherwise, write the length as the number of bytes and don't pad.
        // In either case, the string must be null terminated
        if (codePage == CodePages.WinUnicode)
        {
            bool addNullTerminator =
                byteLength == 0 || nameBytes[byteLength - 1] != '\0' || nameBytes[byteLength - 2] != '\0';

            if (addNullTerminator)
                byteLength += 2;

            bw.Write(byteLength / 2);
            bw.Write(nameBytes);

            if (addNullTerminator)
            {
                bw.Write((byte)0);
                bw.Write((byte)0);
            }

            WritePaddingIfNeeded(bw, (int)byteLength);
        }
        else
        {
            bool addNullTerminator =
                byteLength == 0 || nameBytes[byteLength - 1] != '\0';

            if (addNullTerminator)
                byteLength += 1;

            bw.Write(byteLength);
            bw.Write(nameBytes);

            if (addNullTerminator)
                bw.Write((byte)0);
        }
    }

    // Write as much padding as needed to pad fieldLength to a multiple of 4 bytes
    private static void WritePaddingIfNeeded(BinaryWriter bw, int fieldLength)
    {
        int m = fieldLength % 4;

        if (m > 0)
        {
            for (int i = 0; i < 4 - m; i++) // padding
                bw.Write((byte)0);
        }
    }
}

