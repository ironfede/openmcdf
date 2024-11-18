namespace OpenMcdf.Tests;

[TestClass]
public sealed class DirectoryEntryTests
{
    [TestMethod]
    [DataRow("", "", 0)]
    [DataRow("", "longer", -1)]
    [DataRow("longer", "", 1)]
    [DataRow("a", "a", 0)]
    [DataRow("a", "b", -1)]
    [DataRow("b", "a", 1)]
    public void EnumerateEntryInfos(string nameX, string nameY, int expectedCompare)
    {
        DirectoryEntry entryX = new()
        {
            NameString = nameX,
        };

        DirectoryEntry entryY = new()
        {
            NameString = nameY,
        };

        int actualCompare = DirectoryEntryComparer.Default.Compare(entryX, entryY);
        Assert.AreEqual(expectedCompare, actualCompare);
    }
}
