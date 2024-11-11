namespace OpenMcdf.Ole;

// The default property factory.
internal sealed class DefaultPropertyFactory : PropertyFactory
{
    public static PropertyFactory Instance { get; } = new DefaultPropertyFactory();
}
