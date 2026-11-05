namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="QuantityParseOptions"/>.
/// </summary>
public interface IQuantityParseOptions
{
    /// <summary>
    /// Gets the culture.
    /// </summary>
    /// <returns>The resulting system.globalization.culture info.</returns>
    public System.Globalization.CultureInfo GetCulture();
    /// <summary>
    /// Reports whether strict parsing rules are enabled.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsStrict();
    /// <summary>
    /// Reports whether an optional quantity kind is present.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool HasQuantityKind();
    /// <summary>
    /// Gets the quantity kind.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition GetQuantityKind();
    /// <summary>
    /// Reports whether an optional unit system is present.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool HasUnitSystem();
    /// <summary>
    /// Gets the unit system.
    /// </summary>
    /// <returns>The resulting unit-system definition.</returns>
    public UnitSystemDefinition GetUnitSystem();
    /// <summary>
    /// Creates a new options object with culture replaced.
    /// </summary>
    /// <param name="culture">The culture used to read or write numeric text.</param>
    /// <returns>The resulting quantity parse options.</returns>
    public QuantityParseOptions WithCulture(System.Globalization.CultureInfo culture);
    /// <summary>
    /// Creates a new options object with strict replaced.
    /// </summary>
    /// <param name="strict">Whether surrounding or separating whitespace must follow strict parsing rules.</param>
    /// <returns>The resulting quantity parse options.</returns>
    public QuantityParseOptions WithStrict(bool strict);
    /// <summary>
    /// Creates a new options object with quantity kind replaced.
    /// </summary>
    /// <param name="quantityKind">The quantity kind used to validate or narrow the operation.</param>
    /// <returns>The resulting quantity parse options.</returns>
    public QuantityParseOptions WithQuantityKind(QuantityKindDefinition quantityKind);
    /// <summary>
    /// Creates a new options object with out quantity kind replaced.
    /// </summary>
    /// <returns>The resulting quantity parse options.</returns>
    public QuantityParseOptions WithoutQuantityKind();
    /// <summary>
    /// Creates a new options object with unit system replaced.
    /// </summary>
    /// <param name="unitSystem">The unit system used to narrow ambiguous units.</param>
    /// <returns>The resulting quantity parse options.</returns>
    public QuantityParseOptions WithUnitSystem(UnitSystemDefinition unitSystem);
    /// <summary>
    /// Creates a new options object without unit-system context.
    /// </summary>
    /// <returns>The resulting quantity parse options.</returns>
    public QuantityParseOptions WithoutUnitSystem();
}
