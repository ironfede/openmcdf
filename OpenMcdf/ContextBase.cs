namespace OpenMcdf;

/// <summary>
/// Supports switching the <see cref="RootContext"/> object.
/// </summary>
public abstract class ContextBase
{
    internal RootContextSite ContextSite { get; }

    internal RootContext Context => ContextSite.Context;

    internal ContextBase(RootContextSite site)
    {
        ContextSite = site;
    }
}
