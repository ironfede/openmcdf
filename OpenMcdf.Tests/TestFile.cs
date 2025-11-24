namespace OpenMcdf.Tests;

internal static class TestFile
{
    public static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch
        {
            // Ignore
        }
    }
}
