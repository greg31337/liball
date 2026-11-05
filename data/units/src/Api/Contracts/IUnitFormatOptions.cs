namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="UnitFormatOptions"/>.
/// </summary>
public interface IUnitFormatOptions
{
    /// <summary>
    /// Gets the style.
    /// </summary>
    /// <returns>The resulting unit format style.</returns>
    public UnitFormatStyle GetStyle();
    /// <summary>
    /// Reports whether the current formatting style is symbol.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsSymbol();
    /// <summary>
    /// Reports whether the current formatting style is name.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsName();
    /// <summary>
    /// Reports whether the current formatting style is canonical.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsCanonical();
    /// <summary>
    /// Reports whether the current formatting style is decomposed.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsDecomposed();
    /// <summary>
    /// Creates a new options object with style replaced.
    /// </summary>
    /// <param name="style">The unit formatting style.</param>
    /// <returns>The resulting unit format options.</returns>
    public UnitFormatOptions WithStyle(UnitFormatStyle style);
}
