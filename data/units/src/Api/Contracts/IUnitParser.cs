namespace units;

/// <summary>
/// Parses unit expressions using a read-only unit catalog.
/// </summary>
public interface IUnitParser
{
    /// <summary>
    /// Parses the text without throwing for malformed, unknown, or ambiguous unit expressions.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="registry">The catalog that supplies definitions and metadata.</param>
    /// <returns>A result containing the parsed unit or structured failure details.</returns>
    public UnitParseResult Parse(string text, IUnitCatalog registry);
    /// <summary>
    /// Parses the text with options without throwing for malformed, unknown, or ambiguous unit expressions.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="registry">The catalog that supplies definitions and metadata.</param>
    /// <param name="options">The options that control whitespace and ambiguity narrowing.</param>
    /// <returns>A result containing the parsed unit or structured failure details.</returns>
    public UnitParseResult ParseWithOptions(string text, IUnitCatalog registry, UnitParseOptions options);
    /// <summary>
    /// Parses the text and throws when it is not a valid, unambiguous unit expression.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="registry">The catalog that supplies definitions and metadata.</param>
    /// <returns>The parsed unit definition.</returns>
    /// <exception cref="System.ArgumentException">The text is empty or malformed.</exception>
    /// <exception cref="System.InvalidOperationException">A unit is unknown or the lookup is ambiguous.</exception>
    public UnitDefinition ParseOrThrow(string text, IUnitCatalog registry);
    /// <summary>
    /// Parses the text with options and throws when it is not a valid, unambiguous unit expression.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="registry">The catalog that supplies definitions and metadata.</param>
    /// <param name="options">The options that control whitespace and ambiguity narrowing.</param>
    /// <returns>The parsed unit definition.</returns>
    /// <exception cref="System.ArgumentException">The text is empty or malformed.</exception>
    /// <exception cref="System.InvalidOperationException">A unit is unknown or the lookup is ambiguous.</exception>
    public UnitDefinition ParseOrThrowWithOptions(string text, IUnitCatalog registry, UnitParseOptions options);
}
