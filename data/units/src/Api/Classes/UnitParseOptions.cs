namespace units;

/// <summary>
/// Specifies strictness and optional quantity-kind and unit-system context for unit parsing.
/// </summary>
public class UnitParseOptions : IUnitParseOptions
{
    internal bool _strict;
    internal QuantityKindDefinition? _quantityKind;
    internal UnitSystemDefinition? _unitSystem;

    /// <summary>
    /// Initializes unit parse options from the supplied context.
    /// </summary>
    /// <param name="strict">Whether strict unit-expression whitespace rules are enabled.</param>
    /// <param name="quantityKind">The quantity kind used to narrow ambiguous units.</param>
    /// <param name="unitSystem">The unit system used to narrow ambiguous units.</param>
    public UnitParseOptions(bool strict, QuantityKindDefinition? quantityKind, UnitSystemDefinition? unitSystem)
    {
        _strict = strict;
        _quantityKind = quantityKind;
        _unitSystem = unitSystem;
    }

    internal static UnitParseOptions CreateDefault()
    {
        return new UnitParseOptions(false, null, null);
    }

    /// <summary>
    /// Reports whether strict unit-expression whitespace rules are enabled.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsStrict()
    {
        return _strict;
    }

    /// <summary>
    /// Reports whether quantity-kind context is present.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool HasQuantityKind()
    {
        return _quantityKind != null;
    }

    /// <summary>
    /// Gets the quantity-kind context.
    /// </summary>
    /// <returns>The resulting quantity-kind definition.</returns>
    public QuantityKindDefinition GetQuantityKind()
    {
        if (_quantityKind == null)
        {
            throw new System.InvalidOperationException("Unit parse options do not specify a quantity kind.");
        }

        return _quantityKind;
    }

    /// <summary>
    /// Reports whether unit-system context is present.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool HasUnitSystem()
    {
        return _unitSystem != null;
    }

    /// <summary>
    /// Gets the unit-system context.
    /// </summary>
    /// <returns>The resulting unit-system definition.</returns>
    public UnitSystemDefinition GetUnitSystem()
    {
        if (_unitSystem == null)
        {
            throw new System.InvalidOperationException("Unit parse options do not specify a unit system.");
        }

        return _unitSystem;
    }

    /// <summary>
    /// Creates a new options object with strictness replaced.
    /// </summary>
    /// <param name="strict">Whether strict unit-expression whitespace rules are enabled.</param>
    /// <returns>The resulting unit parse options.</returns>
    public UnitParseOptions WithStrict(bool strict)
    {
        return new UnitParseOptions(strict, _quantityKind, _unitSystem);
    }

    /// <summary>
    /// Creates a new options object with quantity-kind context replaced.
    /// </summary>
    /// <param name="quantityKind">The quantity kind used to narrow ambiguous units.</param>
    /// <returns>The resulting unit parse options.</returns>
    public UnitParseOptions WithQuantityKind(QuantityKindDefinition quantityKind)
    {
        if (quantityKind == null)
        {
            throw new System.ArgumentNullException("quantityKind");
        }

        return new UnitParseOptions(_strict, quantityKind, _unitSystem);
    }

    /// <summary>
    /// Creates a new options object without quantity-kind context.
    /// </summary>
    /// <returns>The resulting unit parse options.</returns>
    public UnitParseOptions WithoutQuantityKind()
    {
        return new UnitParseOptions(_strict, null, _unitSystem);
    }

    /// <summary>
    /// Creates a new options object with unit-system context replaced.
    /// </summary>
    /// <param name="unitSystem">The unit system used to narrow ambiguous units.</param>
    /// <returns>The resulting unit parse options.</returns>
    public UnitParseOptions WithUnitSystem(UnitSystemDefinition unitSystem)
    {
        if (unitSystem == null)
        {
            throw new System.ArgumentNullException("unitSystem");
        }

        return new UnitParseOptions(_strict, _quantityKind, unitSystem);
    }

    /// <summary>
    /// Creates a new options object without unit-system context.
    /// </summary>
    /// <returns>The resulting unit parse options.</returns>
    public UnitParseOptions WithoutUnitSystem()
    {
        return new UnitParseOptions(_strict, _quantityKind, null);
    }
}
