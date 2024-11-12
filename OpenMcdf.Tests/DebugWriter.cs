using System.Diagnostics;
using System.Text;

namespace OpenMcdf.Tests;

internal sealed class DebugWriter : TextWriter
{
    public static DebugWriter Default { get; } = new();

    public override Encoding Encoding => Encoding.Unicode;

    public override void Write(char value) => Debug.Write(value);

    public override void Write(string? value) => Debug.Write(value);

    public override void WriteLine(string? value) => Debug.WriteLine(value);
}
