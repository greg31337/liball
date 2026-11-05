namespace version;

/// <summary>
/// Provides the public entry points for Go module version operations.
/// </summary>
/// <remarks>
/// Parsing methods return result objects for ordinary invalid input. Comparison, formatting, and resolver helpers apply the rules of this facade's ecosystem.
/// </remarks>
internal static class GoVersions
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
        return GoVersionParser.Parse(text);
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
            VersionOptionDispatcher.CreateParseOptionsForDialect(options, VersionDialect.Go));
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
        return GoVersionComparer.Compare(leftText, rightText);
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
            VersionOptionDispatcher.CreateComparisonOptionsForDialect(options, VersionDialect.Go));
    }

    /// <summary>
    /// Parses a module path from text.
    /// </summary>
    /// <remarks>
    /// Invalid input is represented by a failure result with diagnostics rather than by a parsing exception.
    /// </remarks>
    /// <param name="text">The text to process.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public static GoModulePathParseResult ParseModulePath(string text)
    {
        return GoModulePathParser.Parse(text);
    }

    /// <summary>
    /// Parses an escaped module path from text.
    /// </summary>
    /// <remarks>
    /// Invalid input is represented by a failure result with diagnostics rather than by a parsing exception.
    /// </remarks>
    /// <param name="text">The text to process.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public static GoModulePathParseResult ParseEscapedModulePath(string text)
    {
        return GoModulePathParser.ParseEscaped(text);
    }

    /// <summary>
    /// Reports whether valid module path.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public static bool IsValidModulePath(string text)
    {
        return GoModulePathParser.Parse(text).IsSuccess();
    }

    /// <summary>
    /// Reports whether valid escaped module path.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public static bool IsValidEscapedModulePath(string text)
    {
        return GoModulePathParser.ParseEscaped(text).IsSuccess();
    }

    /// <summary>
    /// Validates a semantic import version.
    /// </summary>
    /// <param name="modulePathText">The module path text to process.</param>
    /// <param name="versionText">The version text to process.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public static GoSemanticImportVersionResult ValidateSemanticImportVersion(string modulePathText, string versionText)
    {
        return GoSemanticImportVersionValidator.Validate(modulePathText, versionText);
    }

    /// <summary>
    /// Reports whether valid semantic import version.
    /// </summary>
    /// <param name="modulePathText">The module path text to process.</param>
    /// <param name="versionText">The version text to process.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public static bool IsValidSemanticImportVersion(string modulePathText, string versionText)
    {
        return GoSemanticImportVersionValidator.Validate(modulePathText, versionText).IsSuccess();
    }

    /// <summary>
    /// Reports whether incompatible version.
    /// </summary>
    /// <param name="versionText">The version text to process.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public static bool IsIncompatibleVersion(string versionText)
    {
        return GoSemanticImportVersionValidator.IsIncompatibleVersion(versionText);
    }

    /// <summary>
    /// Reports whether onicalize module version.
    /// </summary>
    /// <param name="modulePathText">The module path text to process.</param>
    /// <param name="versionText">The version text to process.</param>
    /// <returns>The resulting text.</returns>
    public static string CanonicalizeModuleVersion(string modulePathText, string versionText)
    {
        return GoSemanticImportVersionValidator.CanonicalizeModuleVersion(modulePathText, versionText);
    }

    /// <summary>
    /// Compares two versions using module versions rules.
    /// </summary>
    /// <remarks>
    /// A negative value orders the left version first, zero means equal under the selected comparison mode, and a positive value orders the right version first.
    /// </remarks>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <returns>A negative number, zero, or a positive number when the left version is less than, equal to, or greater than the right version.</returns>
    public static int CompareModuleVersions(string leftText, string rightText)
    {
        return GoSemanticImportVersionValidator.CompareModuleVersions(leftText, rightText);
    }

    /// <summary>
    /// Filters the supplied values using semantic import versions rules.
    /// </summary>
    /// <remarks>
    /// The operation evaluates the supplied in-memory candidates deterministically; it does not contact package registries or other external services.
    /// </remarks>
    /// <param name="modulePathText">The module path text to process.</param>
    /// <param name="versionTexts">The version texts to process.</param>
    /// <returns>A new array containing the matching or represented values.</returns>
    public static string[] FilterSemanticImportVersions(string modulePathText, string[] versionTexts)
    {
        return GoSemanticImportVersionValidator.FilterSemanticImportVersions(modulePathText, versionTexts);
    }

    /// <summary>
    /// Selects a highest semantic import version from the supplied candidates.
    /// </summary>
    /// <remarks>
    /// The operation evaluates the supplied in-memory candidates deterministically; it does not contact package registries or other external services.
    /// </remarks>
    /// <param name="modulePathText">The module path text to process.</param>
    /// <param name="versionTexts">The version texts to process.</param>
    /// <returns>The resulting text.</returns>
    public static string? SelectHighestSemanticImportVersion(string modulePathText, string[] versionTexts)
    {
        return GoSemanticImportVersionValidator.SelectHighestSemanticImportVersion(modulePathText, versionTexts);
    }
}
