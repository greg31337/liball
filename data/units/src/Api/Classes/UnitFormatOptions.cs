namespace units;

/// <summary>
/// Specifies how a unit definition is rendered as text.
/// </summary>
public class UnitFormatOptions : IUnitFormatOptions
{
    internal UnitFormatStyle _style;

    /// <summary>
    /// Initializes a new unit format options from the supplied metadata.
    /// </summary>
    /// <param name="style">The unit formatting style.</param>
    public UnitFormatOptions(UnitFormatStyle style)
    {
        if (!IsValidStyle(style))
        {
            throw new System.ArgumentException("Unit format style is not supported.");
        }

        _style = style;
    }

    internal static UnitFormatOptions CreateDefault()
    {
        return CreateSymbol();
    }

    internal static UnitFormatOptions CreateSymbol()
    {
        return new UnitFormatOptions(UnitFormatStyle.Symbol);
    }

    internal static UnitFormatOptions CreateName()
    {
        return new UnitFormatOptions(UnitFormatStyle.Name);
    }

    internal static UnitFormatOptions CreateCanonical()
    {
        return new UnitFormatOptions(UnitFormatStyle.Canonical);
    }

    internal static UnitFormatOptions CreateDecomposed()
    {
        return new UnitFormatOptions(UnitFormatStyle.Decomposed);
    }

    /// <summary>
    /// Gets the style.
    /// </summary>
    /// <returns>The resulting unit format style.</returns>
    public UnitFormatStyle GetStyle()
    {
        return _style;
    }

    /// <summary>
    /// Reports whether the current formatting style is symbol.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsSymbol()
    {
        return _style == UnitFormatStyle.Symbol;
    }

    /// <summary>
    /// Reports whether the current formatting style is name.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsName()
    {
        return _style == UnitFormatStyle.Name;
    }

    /// <summary>
    /// Reports whether the current formatting style is canonical.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsCanonical()
    {
        return _style == UnitFormatStyle.Canonical;
    }

    /// <summary>
    /// Reports whether the current formatting style is decomposed.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsDecomposed()
    {
        return _style == UnitFormatStyle.Decomposed;
    }

    /// <summary>
    /// Creates a new options object with style replaced.
    /// </summary>
    /// <param name="style">The unit formatting style.</param>
    /// <returns>The resulting unit format options.</returns>
    public UnitFormatOptions WithStyle(UnitFormatStyle style)
    {
        return new UnitFormatOptions(style);
    }

    internal static bool IsValidStyle(UnitFormatStyle style)
    {
        return style == UnitFormatStyle.Symbol
            || style == UnitFormatStyle.Name
            || style == UnitFormatStyle.Canonical
            || style == UnitFormatStyle.Decomposed;
    }
}
