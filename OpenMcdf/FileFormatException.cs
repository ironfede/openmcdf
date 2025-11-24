namespace OpenMcdf;

/// <summary>
/// The exception that is thrown when an compound file data stream contains invalid data.
/// </summary>
public class FileFormatException : FormatException
{
    public FileFormatException()
    {
    }

    public FileFormatException(string message)
        : base(message)
    {
    }

    public FileFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
