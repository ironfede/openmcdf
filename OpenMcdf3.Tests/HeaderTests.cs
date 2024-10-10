namespace OpenMcdf3.Tests;

[TestClass]
public sealed class HeaderTests
{
    [TestMethod]
    public void Header()
    {
        using FileStream stream = File.OpenRead("_Test.ppt");
        using McdfBinaryReader reader = new(stream);
        Header header = reader.ReadHeader();
    }
}
