namespace version;

/// <summary>
/// Provides the public entry points for Semantic Versioning version operations.
/// </summary>
/// <remarks>
/// Parsing methods return result objects for ordinary invalid input. Comparison, formatting, and resolver helpers apply the rules of this facade's ecosystem.
/// </remarks>
internal static class SemVer
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
        return ParseStrict(text);
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
            VersionOptionDispatcher.CreateParseOptionsForDialect(options, VersionDialect.SemVer));
    }

    /// <summary>
    /// Parses version text using strict Semantic Versioning rules.
    /// </summary>
    /// <remarks>
    /// Invalid input is represented by a failure result with diagnostics rather than by a parsing exception.
    /// </remarks>
    /// <param name="text">The text to process.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public static VersionParseResult ParseStrict(string text)
    {
        return SemVerParser.ParseStrict(text);
    }

    /// <summary>
    /// Parses version text using strict Semantic Versioning rules.
    /// </summary>
    /// <remarks>
    /// Invalid input is represented by a failure result with diagnostics rather than by a parsing exception.
    /// </remarks>
    /// <param name="text">The text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public static VersionParseResult ParseStrict(string text, VersionParseOptions options)
    {
        VersionParseOptions resolvedOptions = VersionOptionDispatcher.CreateParseOptionsForDialect(options, VersionDialect.SemVer);
        return VersionOptionDispatcher.Parse(text, CreateParseOptionsWithMode(resolvedOptions, VersionParseMode.Strict));
    }

    /// <summary>
    /// Parses version text using the accepted loose-input rules.
    /// </summary>
    /// <remarks>
    /// Invalid input is represented by a failure result with diagnostics rather than by a parsing exception.
    /// </remarks>
    /// <param name="text">The text to process.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public static VersionParseResult ParseLoose(string text)
    {
        return SemVerParser.ParseLoose(text);
    }

    /// <summary>
    /// Parses version text using the accepted loose-input rules.
    /// </summary>
    /// <remarks>
    /// Invalid input is represented by a failure result with diagnostics rather than by a parsing exception.
    /// </remarks>
    /// <param name="text">The text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public static VersionParseResult ParseLoose(string text, VersionParseOptions options)
    {
        VersionParseOptions resolvedOptions = VersionOptionDispatcher.CreateParseOptionsForDialect(options, VersionDialect.SemVer);
        return VersionOptionDispatcher.Parse(text, CreateParseOptionsWithMode(resolvedOptions, VersionParseMode.Loose));
    }

    /// <summary>
    /// Extracts the first numeric version tuple and fills omitted minor or patch components with zero.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public static VersionParseResult Coerce(string text)
    {
        return SemVerParser.Coerce(text);
    }

    /// <summary>
    /// Extracts the first numeric version tuple and fills omitted minor or patch components with zero.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public static VersionParseResult Coerce(string text, VersionParseOptions options)
    {
        VersionParseOptions resolvedOptions = VersionOptionDispatcher.CreateParseOptionsForDialect(options, VersionDialect.SemVer);
        return VersionOptionDispatcher.Parse(text, CreateParseOptionsWithMode(resolvedOptions, VersionParseMode.Coerce));
    }

    /// <summary>
    /// Compares two versions using precedence rules.
    /// </summary>
    /// <remarks>
    /// A negative value orders the left version first, zero means equal under the selected comparison mode, and a positive value orders the right version first.
    /// </remarks>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <returns>A negative number, zero, or a positive number when the left version is less than, equal to, or greater than the right version.</returns>
    public static int ComparePrecedence(string leftText, string rightText)
    {
        UniversalVersion left = ParseStrictVersion(leftText);
        UniversalVersion right = ParseStrictVersion(rightText);
        return SemVerVersionComparer.ComparePrecedence(left, right);
    }

    /// <summary>
    /// Compares two versions using precedence rules.
    /// </summary>
    /// <remarks>
    /// A negative value orders the left version first, zero means equal under the selected comparison mode, and a positive value orders the right version first.
    /// </remarks>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A negative number, zero, or a positive number when the left version is less than, equal to, or greater than the right version.</returns>
    public static int ComparePrecedence(string leftText, string rightText, VersionComparisonOptions options)
    {
        VersionComparisonOptions resolvedOptions = VersionOptionDispatcher.CreateComparisonOptionsForDialect(options, VersionDialect.SemVer);
        return VersionOptionDispatcher.Compare(leftText, rightText, CreateComparisonOptionsWithMode(resolvedOptions, VersionComparisonMode.Precedence));
    }

    /// <summary>
    /// Compares two versions using identity rules.
    /// </summary>
    /// <remarks>
    /// A negative value orders the left version first, zero means equal under the selected comparison mode, and a positive value orders the right version first.
    /// </remarks>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <returns>A negative number, zero, or a positive number when the left version is less than, equal to, or greater than the right version.</returns>
    public static int CompareIdentity(string leftText, string rightText)
    {
        UniversalVersion left = ParseStrictVersion(leftText);
        UniversalVersion right = ParseStrictVersion(rightText);
        return SemVerVersionComparer.CompareIdentity(left, right);
    }

    /// <summary>
    /// Compares two versions using identity rules.
    /// </summary>
    /// <remarks>
    /// A negative value orders the left version first, zero means equal under the selected comparison mode, and a positive value orders the right version first.
    /// </remarks>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A negative number, zero, or a positive number when the left version is less than, equal to, or greater than the right version.</returns>
    public static int CompareIdentity(string leftText, string rightText, VersionComparisonOptions options)
    {
        VersionComparisonOptions resolvedOptions = VersionOptionDispatcher.CreateComparisonOptionsForDialect(options, VersionDialect.SemVer);
        return VersionOptionDispatcher.Compare(leftText, rightText, CreateComparisonOptionsWithMode(resolvedOptions, VersionComparisonMode.Identity));
    }

    /// <summary>
    /// Compares two versions using normalized identity rules.
    /// </summary>
    /// <remarks>
    /// A negative value orders the left version first, zero means equal under the selected comparison mode, and a positive value orders the right version first.
    /// </remarks>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <returns>A negative number, zero, or a positive number when the left version is less than, equal to, or greater than the right version.</returns>
    public static int CompareNormalizedIdentity(string leftText, string rightText)
    {
        UniversalVersion left = ParseStrictVersion(leftText);
        UniversalVersion right = ParseStrictVersion(rightText);
        return SemVerVersionComparer.CompareNormalizedIdentity(left, right);
    }

    /// <summary>
    /// Compares two versions using normalized identity rules.
    /// </summary>
    /// <remarks>
    /// A negative value orders the left version first, zero means equal under the selected comparison mode, and a positive value orders the right version first.
    /// </remarks>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A negative number, zero, or a positive number when the left version is less than, equal to, or greater than the right version.</returns>
    public static int CompareNormalizedIdentity(string leftText, string rightText, VersionComparisonOptions options)
    {
        VersionComparisonOptions resolvedOptions = VersionOptionDispatcher.CreateComparisonOptionsForDialect(options, VersionDialect.SemVer);
        return VersionOptionDispatcher.Compare(leftText, rightText, CreateComparisonOptionsWithMode(resolvedOptions, VersionComparisonMode.NormalizedIdentity));
    }

    internal static UniversalVersion ParseStrictVersion(string text)
    {
        VersionParseResult result = ParseStrict(text);
        if (result.IsFailure())
        {
            throw new System.ArgumentException("Invalid SemVer version text.", "text");
        }

        return result.GetVersion();
    }

    internal static VersionParseOptions CreateParseOptionsWithMode(VersionParseOptions options, VersionParseMode parseMode)
    {
        return new VersionParseOptions(
            options.GetDialect(),
            parseMode,
            options.GetMaxInputLength(),
            options.AllowsPrereleaseVersions(),
            options.AllowsBuildMetadata(),
            options.GetPrereleasePolicy());
    }

    internal static VersionComparisonOptions CreateComparisonOptionsWithMode(
        VersionComparisonOptions options,
        VersionComparisonMode comparisonMode)
    {
        return new VersionComparisonOptions(
            options.GetDialect(),
            comparisonMode,
            options.IsCaseSensitive(),
            options.IncludesBuildMetadata(),
            options.GetInvalidVersionOrdering());
    }
}
