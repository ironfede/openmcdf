using System;

namespace OpenMcdf
{
    /// <summary>
    /// net45 compatible version of BinaryPrimitives
    /// </summary>
    internal static class BinaryPrimitives
    {
        public static int ReverseEndianness(int value)
        {
            return (int)ReverseEndianness((uint)value);
        }

        public static uint ReverseEndianness(uint value)
        {
            uint num = value & 0xFF00FFu;
            uint num2 = value & 0xFF00FF00u;
            return ((num >> 8) | (num << 24)) + ((num2 << 8) | (num2 >> 24));
        }

        public static void WriteInt32LittleEndian(byte[] destination, int offset, int value)
        {
            // TODO: Use System.Memory for BinaryPrimitives in v3
            if (destination is null)
                throw new ArgumentNullException(nameof(destination));
            if (offset + sizeof(int) > destination.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));

            if (!BitConverter.IsLittleEndian)
                value = ReverseEndianness(value);

            destination[offset + 0] = (byte)value;
            destination[offset + 1] = (byte)(value >> 8);
            destination[offset + 2] = (byte)(value >> 16);
            destination[offset + 3] = (byte)(value >> 24);
        }
    }
}
