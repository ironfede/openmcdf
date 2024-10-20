namespace OpenMcdf3.Tests;

internal static class StreamAssert
{
    public static void AreEqual(Stream expected, Stream actual, int bufferLength = 4096)
    {
        Assert.AreEqual(expected.Length, actual.Length);

        expected.Position = 0;
        actual.Position = 0;

        byte[] expectedBuffer = new byte[bufferLength];
        byte[] actualBuffer = new byte[bufferLength];
        while (expected.Position < expected.Length)
        {
            int expectedRead = expected.Read(expectedBuffer, 0, expectedBuffer.Length);
            int actualRead = actual.Read(actualBuffer, 0, actualBuffer.Length);

            if (expectedRead == bufferLength && actualRead == bufferLength)
                CollectionAssert.AreEqual(expectedBuffer, actualBuffer);
            else
                CollectionAssert.AreEqual(expectedBuffer.Take(expectedRead).ToList(), actualBuffer.Take(actualRead).ToList());
        }

        Assert.AreEqual(expected.Position, actual.Position);
    }
}
