namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="StandardPrefixes"/>.
/// </summary>
public interface IStandardPrefixes
{
    /// <summary>
    /// Creates the catalog of standard SI prefixes.
    /// </summary>
    /// <returns>The resulting prefix catalog.</returns>
    public PrefixCatalog CreateCatalog();
    /// <summary>
    /// Registers  in the supplied registry.
    /// </summary>
    /// <param name="registry">The registry that supplies definitions and metadata.</param>
    /// <returns>A result that describes success or failure.</returns>
    public OperationResult Register(UnitRegistry registry);
}
