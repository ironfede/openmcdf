namespace System.Numerics;

internal static class BitOperations
{
    public static bool IsPow2(uint value) => (value & (value - 1)) == 0 && value != 0;
}
