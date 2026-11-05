namespace units;

/// <summary>
/// Defines the public operations provided by <see cref="QuantityParser"/>.
/// </summary>
public interface IQuantityParser
{
    /// <summary>
    /// Parses the text without throwing for malformed, unknown, ambiguous, or incompatible quantities.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="registry">The catalog that supplies definitions and metadata.</param>
    /// <returns>A result containing the parsed quantity or structured failure details.</returns>
    public QuantityParseResult Parse(string text, IUnitCatalog registry);
    /// <summary>
    /// Parses the text with options without throwing for malformed, unknown, ambiguous, or incompatible quantities.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="registry">The catalog that supplies definitions and metadata.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A result containing the parsed quantity or structured failure details.</returns>
    public QuantityParseResult ParseWithOptions(string text, IUnitCatalog registry, QuantityParseOptions options);
    /// <summary>
    /// Parses the text and throws when it is not a valid, unambiguous quantity.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="registry">The catalog that supplies definitions and metadata.</param>
    /// <returns>The parsed quantity.</returns>
    /// <exception cref="System.ArgumentException">The text is empty or malformed.</exception>
    /// <exception cref="System.InvalidOperationException">A unit is unknown, ambiguous, or incompatible with the requested quantity kind.</exception>
    public QuantityValue ParseOrThrow(string text, IUnitCatalog registry);
    /// <summary>
    /// Parses the text and throws when it is not a valid, unambiguous quantity.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="registry">The catalog that supplies definitions and metadata.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The parsed quantity.</returns>
    /// <exception cref="System.ArgumentException">The text is empty or malformed.</exception>
    /// <exception cref="System.InvalidOperationException">A unit is unknown, ambiguous, or incompatible with the requested quantity kind.</exception>
    public QuantityValue ParseOrThrowWithOptions(string text, IUnitCatalog registry, QuantityParseOptions options);
}
