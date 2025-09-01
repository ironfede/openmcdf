using System.Diagnostics;
using System.Text;

namespace OpenMcdf.Tests;

internal sealed class DebugWriter : TextWriter
{
    static Lazy<DebugWriter> lazyDebugWriter = new();

    public static DebugWriter Default => lazyDebugWriter.Value;

    public override Encoding Encoding => Encoding.Unicode;

    public override void Write(char value) => Debug.Write(value);

    public override void Write(string? value) => Debug.Write(value);

    public override void WriteLine(string? value) => Debug.WriteLine(value);
}
