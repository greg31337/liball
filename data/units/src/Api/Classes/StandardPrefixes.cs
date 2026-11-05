namespace units;

/// <summary>
/// Provides and registers the standard SI prefix catalog.
/// </summary>
public class StandardPrefixes : IStandardPrefixes
{
    /// <summary>
    /// Initializes a standard-prefix catalog.
    /// </summary>
    public StandardPrefixes()
    {
    }

    /// <summary>
    /// Creates the catalog of standard SI prefixes.
    /// </summary>
    /// <returns>The resulting prefix catalog.</returns>
    public PrefixCatalog CreateCatalog()
    {
        return PrefixCatalog.CreateSi();
    }

    /// <summary>
    /// Registers  in the supplied registry.
    /// </summary>
    /// <param name="registry">The registry that supplies definitions and metadata.</param>
    /// <returns>A result that describes success or failure.</returns>
    public OperationResult Register(UnitRegistry registry)
    {
        if (registry == null)
        {
            throw new System.ArgumentNullException("registry");
        }

        PrefixDefinition[] prefixes = CreateCatalog().GetPrefixes();
        int index = 0;

        while (index < prefixes.Length)
        {
            OperationResult result = registry.AddPrefix(prefixes[index]);
            if (result.IsFailure())
            {
                return result;
            }

            index = index + 1;
        }

        return OperationResult.CreateSuccess();
    }
}
