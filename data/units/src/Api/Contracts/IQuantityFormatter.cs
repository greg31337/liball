namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="QuantityFormatter"/>.
/// </summary>
public interface IQuantityFormatter
{
    /// <summary>
    /// Formats the supplied quantity according to the selected options.
    /// </summary>
    /// <param name="quantity">The quantity to use.</param>
    /// <returns>The formatted text.</returns>
    public string Format(QuantityValue quantity);
    /// <summary>
    /// Formats the supplied quantity according to the selected options.
    /// </summary>
    /// <param name="quantity">The quantity to use.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The formatted text.</returns>
    public string FormatWithOptions(QuantityValue quantity, QuantityFormatOptions options);
}
