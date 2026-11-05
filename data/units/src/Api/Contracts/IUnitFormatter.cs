namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="UnitFormatter"/>.
/// </summary>
public interface IUnitFormatter
{
    /// <summary>
    /// Formats the supplied unit according to the selected options.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>The formatted text.</returns>
    public string Format(UnitDefinition unit);
    /// <summary>
    /// Formats the supplied unit according to the selected options.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The formatted text.</returns>
    public string FormatWithOptions(UnitDefinition unit, UnitFormatOptions options);
}
