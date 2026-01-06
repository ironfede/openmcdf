namespace OpenMcdf;

/// <summary>
/// A site for the <see cref="RootContext"/> object, to allow switching streams.
/// </summary>
internal sealed class RootContextSite
{
    RootContext? context;

    internal RootContext Context => context!;

    internal void Switch(RootContext context)
    {
        this.context = context;
    }
}
