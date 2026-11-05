namespace units;

/// <summary>
/// Specifies culture, strictness, and optional quantity-kind and unit-system context for parsing.
/// </summary>
public class QuantityParseOptions : IQuantityParseOptions
{
    internal System.Globalization.CultureInfo _culture;
    internal bool _strict;
    internal QuantityKindDefinition? _quantityKind;
    internal UnitSystemDefinition? _unitSystem;

    /// <summary>
    /// Initializes a new quantity parse options from the supplied metadata.
    /// </summary>
    /// <param name="culture">The culture used to read or write numeric text.</param>
    /// <param name="strict">Whether surrounding or separating whitespace must follow strict parsing rules.</param>
    /// <param name="quantityKind">The quantity kind used to validate or narrow the operation.</param>
    /// <param name="unitSystem">The unit system used to narrow ambiguous units.</param>
    public QuantityParseOptions(System.Globalization.CultureInfo culture, bool strict, QuantityKindDefinition? quantityKind, UnitSystemDefinition? unitSystem)
    {
        if (culture == null)
        {
            throw new System.ArgumentNullException("culture");
        }

        _culture = culture;
        _strict = strict;
        _quantityKind = quantityKind;
        _unitSystem = unitSystem;
    }

    internal static QuantityParseOptions CreateDefault()
    {
        return new QuantityParseOptions(System.Globalization.CultureInfo.InvariantCulture, false, null, null);
    }

    /// <summary>
    /// Gets the culture.
    /// </summary>
    /// <returns>The resulting system.globalization.culture info.</returns>
    public System.Globalization.CultureInfo GetCulture()
    {
        return _culture;
    }

    /// <summary>
    /// Reports whether strict parsing rules are enabled.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsStrict()
    {
        return _strict;
    }

    /// <summary>
    /// Reports whether an optional quantity kind is present.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool HasQuantityKind()
    {
        return _quantityKind != null;
    }

    /// <summary>
    /// Gets the quantity kind.
    /// </summary>
    /// <returns>The resulting quantity kind definition.</returns>
    public QuantityKindDefinition GetQuantityKind()
    {
        if (_quantityKind == null)
        {
            throw new System.InvalidOperationException("Quantity parse options do not specify a quantity kind.");
        }

        return _quantityKind;
    }

    /// <summary>
    /// Reports whether an optional unit system is present.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool HasUnitSystem()
    {
        return _unitSystem != null;
    }

    /// <summary>
    /// Gets the unit system.
    /// </summary>
    /// <returns>The resulting unit-system definition.</returns>
    public UnitSystemDefinition GetUnitSystem()
    {
        if (_unitSystem == null)
        {
            throw new System.InvalidOperationException("Quantity parse options do not specify a unit system.");
        }

        return _unitSystem;
    }

    /// <summary>
    /// Creates a new options object with culture replaced.
    /// </summary>
    /// <param name="culture">The culture used to read or write numeric text.</param>
    /// <returns>The resulting quantity parse options.</returns>
    public QuantityParseOptions WithCulture(System.Globalization.CultureInfo culture)
    {
        if (culture == null)
        {
            throw new System.ArgumentNullException("culture");
        }

        return new QuantityParseOptions(culture, _strict, _quantityKind, _unitSystem);
    }

    /// <summary>
    /// Creates a new options object with strict replaced.
    /// </summary>
    /// <param name="strict">Whether surrounding or separating whitespace must follow strict parsing rules.</param>
    /// <returns>The resulting quantity parse options.</returns>
    public QuantityParseOptions WithStrict(bool strict)
    {
        return new QuantityParseOptions(_culture, strict, _quantityKind, _unitSystem);
    }

    /// <summary>
    /// Creates a new options object with quantity kind replaced.
    /// </summary>
    /// <param name="quantityKind">The quantity kind used to validate or narrow the operation.</param>
    /// <returns>The resulting quantity parse options.</returns>
    public QuantityParseOptions WithQuantityKind(QuantityKindDefinition quantityKind)
    {
        if (quantityKind == null)
        {
            throw new System.ArgumentNullException("quantityKind");
        }

        return new QuantityParseOptions(_culture, _strict, quantityKind, _unitSystem);
    }

    /// <summary>
    /// Creates a new options object with out quantity kind replaced.
    /// </summary>
    /// <returns>The resulting quantity parse options.</returns>
    public QuantityParseOptions WithoutQuantityKind()
    {
        return new QuantityParseOptions(_culture, _strict, null, _unitSystem);
    }

    /// <summary>
    /// Creates a new options object with unit system replaced.
    /// </summary>
    /// <param name="unitSystem">The unit system used to narrow ambiguous units.</param>
    /// <returns>The resulting quantity parse options.</returns>
    public QuantityParseOptions WithUnitSystem(UnitSystemDefinition unitSystem)
    {
        if (unitSystem == null)
        {
            throw new System.ArgumentNullException("unitSystem");
        }

        return new QuantityParseOptions(_culture, _strict, _quantityKind, unitSystem);
    }

    /// <summary>
    /// Creates a new options object without unit-system context.
    /// </summary>
    /// <returns>The resulting quantity parse options.</returns>
    public QuantityParseOptions WithoutUnitSystem()
    {
        return new QuantityParseOptions(_culture, _strict, _quantityKind, null);
    }
}
