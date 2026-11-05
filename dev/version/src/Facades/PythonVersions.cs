namespace version;

/// <summary>
/// Provides the public entry points for Python and PEP 440 version operations.
/// </summary>
/// <remarks>
/// Parsing methods return result objects for ordinary invalid input. Comparison, formatting, and resolver helpers apply the rules of this facade's ecosystem.
/// </remarks>
internal static class PythonVersions
{
    /// <summary>
    /// Parses version text using this API's ecosystem rules.
    /// </summary>
    /// <remarks>
    /// Invalid input is represented by a failure result with diagnostics rather than by a parsing exception.
    /// </remarks>
    /// <param name="text">The text to process.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public static VersionParseResult Parse(string text)
    {
        return PythonVersionParser.Parse(text);
    }

    /// <summary>
    /// Parses version text using this API's ecosystem rules.
    /// </summary>
    /// <remarks>
    /// Invalid input is represented by a failure result with diagnostics rather than by a parsing exception.
    /// </remarks>
    /// <param name="text">The text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public static VersionParseResult Parse(string text, VersionParseOptions options)
    {
        return VersionOptionDispatcher.Parse(
            text,
            VersionOptionDispatcher.CreateParseOptionsForDialect(options, VersionDialect.Python));
    }

    /// <summary>
    /// Parses a Python version specifier set from text.
    /// </summary>
    /// <remarks>
    /// Invalid input is represented by a failure result with diagnostics rather than by a parsing exception.
    /// </remarks>
    /// <param name="text">The text to process.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public static VersionRangeParseResult ParseSpecifier(string text)
    {
        return PythonSpecifierParser.Parse(text);
    }

    /// <summary>
    /// Parses a Python version specifier set from text.
    /// </summary>
    /// <remarks>
    /// Invalid input is represented by a failure result with diagnostics rather than by a parsing exception.
    /// </remarks>
    /// <param name="text">The text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public static VersionRangeParseResult ParseSpecifier(string text, VersionRangeParseOptions options)
    {
        return VersionOptionDispatcher.ParseRange(
            text,
            VersionOptionDispatcher.CreateRangeOptionsForSyntax(options, RangeSyntax.Python));
    }

    /// <summary>
    /// Parses a version range from text.
    /// </summary>
    /// <remarks>
    /// Invalid input is represented by a failure result with diagnostics rather than by a parsing exception.
    /// </remarks>
    /// <param name="text">The text to process.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public static VersionRangeParseResult ParseRange(string text)
    {
        return ParseSpecifier(text);
    }

    /// <summary>
    /// Parses a version range from text.
    /// </summary>
    /// <remarks>
    /// Invalid input is represented by a failure result with diagnostics rather than by a parsing exception.
    /// </remarks>
    /// <param name="text">The text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public static VersionRangeParseResult ParseRange(string text, VersionRangeParseOptions options)
    {
        return ParseSpecifier(text, options);
    }

    /// <summary>
    /// Compares two versions using the selected ecosystem precedence rules.
    /// </summary>
    /// <remarks>
    /// A negative value orders the left version first, zero means equal under the selected comparison mode, and a positive value orders the right version first.
    /// </remarks>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <returns>A negative number, zero, or a positive number when the left version is less than, equal to, or greater than the right version.</returns>
    public static int Compare(string leftText, string rightText)
    {
        return PythonVersionComparer.Compare(leftText, rightText);
    }

    /// <summary>
    /// Compares two versions using the selected ecosystem precedence rules.
    /// </summary>
    /// <remarks>
    /// A negative value orders the left version first, zero means equal under the selected comparison mode, and a positive value orders the right version first.
    /// </remarks>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A negative number, zero, or a positive number when the left version is less than, equal to, or greater than the right version.</returns>
    public static int Compare(string leftText, string rightText, VersionComparisonOptions options)
    {
        return VersionOptionDispatcher.Compare(
            leftText,
            rightText,
            VersionOptionDispatcher.CreateComparisonOptionsForDialect(options, VersionDialect.Python));
    }

    /// <summary>
    /// Formats the supplied value as normalized text.
    /// </summary>
    /// <remarks>
    /// Formatting does not change the represented version; the selected form controls which original or normalized details are emitted.
    /// </remarks>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>The resulting text.</returns>
    public static string FormatNormalized(PythonVersion version)
    {
        return PythonVersionFormatter.FormatNormalized(version);
    }

    /// <summary>
    /// Formats the supplied value as canonical text.
    /// </summary>
    /// <remarks>
    /// Formatting does not change the represented version; the selected form controls which original or normalized details are emitted.
    /// </remarks>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>The resulting text.</returns>
    public static string FormatCanonical(PythonVersion version)
    {
        return PythonVersionFormatter.FormatCanonical(version);
    }

    /// <summary>
    /// Formats the supplied value as normalized text.
    /// </summary>
    /// <remarks>
    /// Formatting does not change the represented version; the selected form controls which original or normalized details are emitted.
    /// </remarks>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>The resulting text.</returns>
    public static string FormatNormalized(UniversalVersion version)
    {
        return VersionFormatter.FormatNormalized(version);
    }

    /// <summary>
    /// Formats the supplied value as canonical text.
    /// </summary>
    /// <remarks>
    /// Formatting does not change the represented version; the selected form controls which original or normalized details are emitted.
    /// </remarks>
    /// <param name="version">The version used by the operation.</param>
    /// <returns>The resulting text.</returns>
    public static string FormatCanonical(UniversalVersion version)
    {
        return VersionFormatter.FormatCanonical(version);
    }
}
