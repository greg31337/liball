namespace units;

/// <summary>
/// Builds the complete read-only registry of definitions supplied by the library.
/// </summary>
public class StandardRegistry : IStandardRegistry
{
    /// <summary>
    /// Initializes a standard-registry builder.
    /// </summary>
    public StandardRegistry()
    {
    }

    /// <summary>
    /// Creates the complete standard registry and freezes it before returning.
    /// </summary>
    /// <returns>The resulting unit registry.</returns>
    public UnitRegistry Create()
    {
        UnitRegistry registry = new UnitRegistry();

        RequireSuccess(new StandardUnits().RegisterSiBaseUnits(registry));
        RequireSuccess(new StandardUnits().RegisterSiDerivedUnits(registry));
        RequireSuccess(new StandardUnits().RegisterMetricUnits(registry));
        RequireSuccess(new StandardUnits().RegisterTemperatureUnits(registry));
        RequireSuccess(new StandardUnits().RegisterDimensionlessUnits(registry));
        RequireSuccess(new StandardUnits().RegisterCgsUnits(registry));
        RequireSuccess(new StandardUnits().RegisterImperialAndUsCustomaryUnits(registry));
        RequireSuccess(new StandardUnits().RegisterDataUnits(registry));
        RequireSuccess(new StandardUnits().RegisterCompoundUnits(registry));
        RequireSuccess(new StandardPrefixes().Register(registry));
        RequireSuccess(new StandardQuantityKinds().RegisterCoreQuantityKinds(registry));
        RequireSuccess(new StandardUnitSystems().RegisterStandardUnitSystems(registry));
        RequireSuccess(new StandardEquivalencies().RegisterStandardEquivalencies(registry));
        RequireSuccess(new PhysicalConstants().RegisterFundamentalConstants(registry));

        registry.Freeze();
        return registry;
    }

    internal static void RequireSuccess(OperationResult result)
    {
        if (result == null)
        {
            throw new System.ArgumentNullException("result");
        }

        if (result.IsFailure())
        {
            throw new System.InvalidOperationException(result.GetMessage());
        }
    }
}
