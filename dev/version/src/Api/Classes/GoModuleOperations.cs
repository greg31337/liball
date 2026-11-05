namespace version;

/// <summary>
/// Provides public Go version, module path, and semantic import version operations.
/// </summary>
/// <remarks>
/// The service evaluates only caller-supplied text and finite candidate arrays.
/// </remarks>
public class GoModuleOperations : IGoModuleVersioning
{
    /// <summary>
    /// Initializes a stateless Go module operations service.
    /// </summary>
    public GoModuleOperations()
    {
    }

    /// <summary>
    /// Reports whether text is a valid Go module version.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns><c>true</c> when the text is valid; otherwise, <c>false</c>.</returns>
    public bool IsValidVersion(string text)
    {
        return GoVersionParser.Parse(text).IsSuccess();
    }

    /// <summary>
    /// Parses a Go module version without throwing for ordinary invalid input.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>A result containing the parsed version or diagnostics.</returns>
    public VersionParseResult ParseVersion(string text)
    {
        return GoVersionParser.Parse(text);
    }

    /// <summary>
    /// Canonicalizes a Go module version and removes build metadata.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The canonical version, or an empty string when the input is invalid.</returns>
    public string CanonicalizeVersion(string text)
    {
        return GoVersionComparer.Canonicalize(text);
    }

    /// <summary>
    /// Gets the normalized major version with its leading <c>v</c>.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The major version, or an empty string when the input is invalid.</returns>
    public string GetMajorVersion(string text)
    {
        return GoVersionComparer.GetMajor(text);
    }

    /// <summary>
    /// Gets the normalized major and minor version with its leading <c>v</c>.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The major and minor version, or an empty string when the input is invalid.</returns>
    public string GetMajorMinorVersion(string text)
    {
        return GoVersionComparer.GetMajorMinor(text);
    }

    /// <summary>
    /// Gets the prerelease suffix including its leading hyphen.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The prerelease suffix, or an empty string when absent or invalid.</returns>
    public string GetPrerelease(string text)
    {
        return GoVersionComparer.GetPrerelease(text);
    }

    /// <summary>
    /// Gets the build suffix including its leading plus sign.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The build suffix, or an empty string when absent or invalid.</returns>
    public string GetBuild(string text)
    {
        return GoVersionComparer.GetBuild(text);
    }

    /// <summary>
    /// Compares two Go module versions using Go semantic-version precedence and invalid ordering.
    /// </summary>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <returns>A negative number, zero, or a positive number.</returns>
    public int CompareVersions(string leftText, string rightText)
    {
        return GoVersionComparer.Compare(leftText, rightText);
    }

    /// <summary>
    /// Sorts Go module versions in ascending order without changing the supplied array.
    /// </summary>
    /// <param name="versionTexts">The version texts to process.</param>
    /// <returns>A new array containing the sorted version texts.</returns>
    public string[] SortVersionsAscending(string[] versionTexts)
    {
        return GoVersionComparer.SortAscending(versionTexts);
    }

    /// <summary>
    /// Sorts Go module versions in descending order without changing the supplied array.
    /// </summary>
    /// <param name="versionTexts">The version texts to process.</param>
    /// <returns>A new array containing the sorted version texts.</returns>
    public string[] SortVersionsDescending(string[] versionTexts)
    {
        return GoVersionComparer.SortDescending(versionTexts);
    }

    /// <summary>
    /// Parses a Go module path.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>A result containing the parsed module path or diagnostics.</returns>
    public GoModulePathParseResult ParseModulePath(string text)
    {
        return GoModulePathParser.Parse(text);
    }

    /// <summary>
    /// Parses an escaped Go module path.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>A result containing the decoded module path or diagnostics.</returns>
    public GoModulePathParseResult ParseEscapedModulePath(string text)
    {
        return GoModulePathParser.ParseEscaped(text);
    }

    /// <summary>
    /// Reports whether text is a valid Go module path.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns><c>true</c> when the text is valid; otherwise, <c>false</c>.</returns>
    public bool IsValidModulePath(string text)
    {
        return GoModulePathParser.Parse(text).IsSuccess();
    }

    /// <summary>
    /// Reports whether text is a valid escaped Go module path.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns><c>true</c> when the text is valid; otherwise, <c>false</c>.</returns>
    public bool IsValidEscapedModulePath(string text)
    {
        return GoModulePathParser.ParseEscaped(text).IsSuccess();
    }

    /// <summary>
    /// Validates a module version against the major-version suffix in its module path.
    /// </summary>
    /// <param name="modulePathText">The module path text to process.</param>
    /// <param name="versionText">The version text to process.</param>
    /// <returns>A result containing the parsed values or diagnostics.</returns>
    public GoSemanticImportVersionResult ValidateSemanticImportVersion(
        string modulePathText,
        string versionText)
    {
        return GoSemanticImportVersionValidator.Validate(modulePathText, versionText);
    }

    /// <summary>
    /// Reports whether a version is valid for a module path under semantic import versioning.
    /// </summary>
    /// <param name="modulePathText">The module path text to process.</param>
    /// <param name="versionText">The version text to process.</param>
    /// <returns><c>true</c> when the path and version are compatible; otherwise, <c>false</c>.</returns>
    public bool IsValidSemanticImportVersion(string modulePathText, string versionText)
    {
        return GoSemanticImportVersionValidator.Validate(modulePathText, versionText).IsSuccess();
    }

    /// <summary>
    /// Reports whether a version has the exact <c>+incompatible</c> build suffix.
    /// </summary>
    /// <param name="versionText">The version text to process.</param>
    /// <returns><c>true</c> when the version is incompatible; otherwise, <c>false</c>.</returns>
    public bool IsIncompatibleVersion(string versionText)
    {
        return GoSemanticImportVersionValidator.IsIncompatibleVersion(versionText);
    }

    /// <summary>
    /// Canonicalizes a version after validating it against a module path.
    /// </summary>
    /// <param name="modulePathText">The module path text to process.</param>
    /// <param name="versionText">The version text to process.</param>
    /// <returns>The canonical module version, or an empty string when validation fails.</returns>
    public string CanonicalizeModuleVersion(string modulePathText, string versionText)
    {
        return GoSemanticImportVersionValidator.CanonicalizeModuleVersion(modulePathText, versionText);
    }

    /// <summary>
    /// Filters finite version candidates using semantic import versioning rules.
    /// </summary>
    /// <param name="modulePathText">The module path text to process.</param>
    /// <param name="versionTexts">The version texts to process.</param>
    /// <returns>A new array containing compatible candidates in input order.</returns>
    public string[] FilterSemanticImportVersions(string modulePathText, string[] versionTexts)
    {
        return GoSemanticImportVersionValidator.FilterSemanticImportVersions(
            modulePathText,
            versionTexts);
    }

    /// <summary>
    /// Selects the highest compatible version from finite candidates.
    /// </summary>
    /// <param name="modulePathText">The module path text to process.</param>
    /// <param name="versionTexts">The version texts to process.</param>
    /// <returns>The highest compatible version, or null when no candidate is compatible.</returns>
    public string? SelectHighestSemanticImportVersion(
        string modulePathText,
        string[] versionTexts)
    {
        return GoSemanticImportVersionValidator.SelectHighestSemanticImportVersion(
            modulePathText,
            versionTexts);
    }
}
