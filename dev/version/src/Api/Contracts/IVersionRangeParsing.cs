namespace version;

/// <summary>
/// Defines non-throwing and throwing range parsing operations for dependency-injected consumers.
/// </summary>
/// <remarks>
/// Implement this interface in tests when a consumer needs controlled range results without invoking a concrete range parser.
/// </remarks>
public interface IVersionRangeParsing
{
    /// <summary>
    /// Parses range text with the supplied options and throws when the text is invalid.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The resulting version range.</returns>
    public VersionRange ParseRange(string text, VersionRangeParseOptions options);

    /// <summary>
    /// Attempts to parse range text with the supplied options.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public VersionRangeParseResult TryParseRange(string text, VersionRangeParseOptions options);
}


