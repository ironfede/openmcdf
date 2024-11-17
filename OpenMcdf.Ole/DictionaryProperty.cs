﻿using System.Text;

namespace OpenMcdf.Ole;

public sealed class DictionaryProperty : IProperty
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
            ReadEntry(br);
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

    // Read a single dictionary entry
    private void ReadEntry(BinaryReader br)
    {
        uint propertyIdentifier = br.ReadUInt32();
        int length = br.ReadInt32();

        byte[] nameBytes;

        if (codePage == CodePages.WinUnicode)
        {
            nameBytes = br.ReadBytes(length << 1);

            int m = length * 2 % 4;
            if (m > 0)
                br.ReadBytes(m);
        }
        else
        {
            nameBytes = br.ReadBytes(length);
        }

        string entryName = Encoding.GetEncoding(codePage).GetString(nameBytes).Trim('\0');
        entries!.Add(propertyIdentifier, entryName);
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
            // Two bytes padding for Unicode strings
            byteLength += 2;

            bw.Write(byteLength / 2);
            bw.Write(nameBytes);
            bw.Write((byte)0);
            bw.Write((byte)0);

            WritePaddingIfNeeded(bw, (int)byteLength);
        }
        else
        {
            byteLength += 1;

            bw.Write(byteLength);
            bw.Write(nameBytes);
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

