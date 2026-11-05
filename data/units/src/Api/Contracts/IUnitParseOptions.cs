namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="UnitParseOptions"/>.
/// </summary>
public interface IUnitParseOptions
{
    /// <summary>
    /// Reports whether strict unit-expression whitespace rules are enabled.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsStrict();
    /// <summary>
    /// Reports whether quantity-kind context is present.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool HasQuantityKind();
    /// <summary>
    /// Gets the quantity-kind context.
    /// </summary>
    /// <returns>The resulting quantity-kind definition.</returns>
    public QuantityKindDefinition GetQuantityKind();
    /// <summary>
    /// Reports whether unit-system context is present.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool HasUnitSystem();
    /// <summary>
    /// Gets the unit-system context.
    /// </summary>
    /// <returns>The resulting unit-system definition.</returns>
    public UnitSystemDefinition GetUnitSystem();
    /// <summary>
    /// Creates a new options object with strictness replaced.
    /// </summary>
    /// <param name="strict">Whether strict unit-expression whitespace rules are enabled.</param>
    /// <returns>The resulting unit parse options.</returns>
    public UnitParseOptions WithStrict(bool strict);
    /// <summary>
    /// Creates a new options object with quantity-kind context replaced.
    /// </summary>
    /// <param name="quantityKind">The quantity kind used to narrow ambiguous units.</param>
    /// <returns>The resulting unit parse options.</returns>
    public UnitParseOptions WithQuantityKind(QuantityKindDefinition quantityKind);
    /// <summary>
    /// Creates a new options object without quantity-kind context.
    /// </summary>
    /// <returns>The resulting unit parse options.</returns>
    public UnitParseOptions WithoutQuantityKind();
    /// <summary>
    /// Creates a new options object with unit-system context replaced.
    /// </summary>
    /// <param name="unitSystem">The unit system used to narrow ambiguous units.</param>
    /// <returns>The resulting unit parse options.</returns>
    public UnitParseOptions WithUnitSystem(UnitSystemDefinition unitSystem);
    /// <summary>
    /// Creates a new options object without unit-system context.
    /// </summary>
    /// <returns>The resulting unit parse options.</returns>
    public UnitParseOptions WithoutUnitSystem();
}
