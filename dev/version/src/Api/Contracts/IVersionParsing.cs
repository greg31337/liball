namespace version;

/// <summary>
/// Defines non-throwing and throwing version parsing operations for dependency-injected consumers.
/// </summary>
/// <remarks>
/// Implement this interface in tests when a consumer needs controlled parse results without invoking a concrete dialect parser.
/// </remarks>
public interface IVersionParsing
{
    /// <summary>
    /// Parses version text with the supplied options and throws when the text is invalid.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The resulting universal version.</returns>
    public UniversalVersion Parse(string text, VersionParseOptions options);

    /// <summary>
    /// Attempts to parse version text with the supplied options.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public VersionParseResult TryParse(string text, VersionParseOptions options);
}


