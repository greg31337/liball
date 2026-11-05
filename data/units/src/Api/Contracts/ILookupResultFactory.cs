namespace units;

/// <summary>
/// Creates validated lookup results for custom catalog implementations.
/// </summary>
public interface ILookupResultFactory
{
    /// <summary>
    /// Creates a unit lookup result from all definitions matching the supplied text.
    /// </summary>
    /// <param name="units">The matching unit definitions.</param>
    /// <param name="text">The lookup text used in failure messages.</param>
    /// <returns>A result that reports a unique match, ambiguity, or failure.</returns>
    public UnitLookupResult CreateUnitResult(UnitDefinition[] units, string text);

    /// <summary>
    /// Creates a quantity-kind lookup result from all definitions matching the supplied text.
    /// </summary>
    /// <param name="quantityKinds">The matching quantity-kind definitions.</param>
    /// <param name="text">The lookup text used in failure messages.</param>
    /// <returns>A result that reports a unique match, ambiguity, or failure.</returns>
    public QuantityKindLookupResult CreateQuantityKindResult(QuantityKindDefinition[] quantityKinds, string text);

    /// <summary>
    /// Creates a unit-system lookup result from all definitions matching the supplied text.
    /// </summary>
    /// <param name="unitSystems">The matching unit-system definitions.</param>
    /// <param name="text">The lookup text used in failure messages.</param>
    /// <returns>A result that reports a unique match, ambiguity, or failure.</returns>
    public UnitSystemLookupResult CreateUnitSystemResult(UnitSystemDefinition[] unitSystems, string text);

    /// <summary>
    /// Creates a physical-constant lookup result from all definitions matching the supplied text.
    /// </summary>
    /// <param name="physicalConstants">The matching physical-constant definitions.</param>
    /// <param name="text">The lookup text used in failure messages.</param>
    /// <returns>A result that reports a unique match, ambiguity, or failure.</returns>
    public PhysicalConstantLookupResult CreatePhysicalConstantResult(PhysicalConstantDefinition[] physicalConstants, string text);
}
