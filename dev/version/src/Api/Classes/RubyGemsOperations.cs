namespace version;

/// <summary>
/// Provides public RubyGems version, requirement, platform, and finite dependency operations.
/// </summary>
/// <remarks>
/// The service evaluates only caller-supplied specifications and does not contact gem sources.
/// </remarks>
public class RubyGemsOperations : IRubyGemsResolution
{
    /// <summary>
    /// Initializes a stateless RubyGems operations service.
    /// </summary>
    public RubyGemsOperations()
    {
    }

    /// <summary>
    /// Parses a RubyGems version without throwing for ordinary invalid input.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>A result containing the parsed version or diagnostics.</returns>
    public VersionParseResult ParseVersion(string text)
    {
        return RubyGemsVersionParser.ParseUniversal(text);
    }

    /// <summary>
    /// Normalizes RubyGems version text while retaining its represented segments.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>The normalized RubyGems version text.</returns>
    public string NormalizeVersion(string text)
    {
        return RubyGemsVersionFormatter.FormatNormalized(text);
    }

    /// <summary>
    /// Reports whether valid RubyGems version text contains a prerelease segment.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns><c>true</c> when the version is a prerelease; otherwise, <c>false</c>.</returns>
    public bool IsPrereleaseVersion(string text)
    {
        return RubyGemsVersionComparer.IsPrerelease(text);
    }

    /// <summary>
    /// Compares two RubyGems versions using segment ordering and trailing-zero equivalence.
    /// </summary>
    /// <param name="leftText">The left text to process.</param>
    /// <param name="rightText">The right text to process.</param>
    /// <returns>A negative number, zero, or a positive number.</returns>
    public int CompareVersions(string leftText, string rightText)
    {
        return RubyGemsVersionComparer.Compare(leftText, rightText);
    }

    /// <summary>
    /// Parses a RubyGems requirement without throwing for ordinary invalid input.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <returns>A result containing the parsed requirement or diagnostics.</returns>
    public VersionRangeParseResult ParseRequirement(string text)
    {
        return RubyGemsRangeParser.Parse(text);
    }

    /// <summary>
    /// Creates the default stable RubyGems requirement.
    /// </summary>
    /// <returns>The default requirement list.</returns>
    public RubyGemsRequirementList CreateDefaultRequirement()
    {
        return RubyGemsRequirementList.CreateDefault();
    }

    /// <summary>
    /// Creates the default RubyGems requirement that admits prereleases.
    /// </summary>
    /// <returns>The default prerelease requirement list.</returns>
    public RubyGemsRequirementList CreateDefaultPrereleaseRequirement()
    {
        return RubyGemsRequirementList.CreateDefaultPrerelease();
    }

    /// <summary>
    /// Creates a requirement list from nullable and comma-separated requirement text.
    /// </summary>
    /// <param name="requirementTexts">The requirement text to flatten and normalize.</param>
    /// <returns>The resulting requirement list.</returns>
    public RubyGemsRequirementList CreateRequirementList(string?[] requirementTexts)
    {
        return new RubyGemsRequirementList(requirementTexts);
    }

    /// <summary>
    /// Creates a requirement list from nested nullable requirement arrays.
    /// </summary>
    /// <param name="requirementTextArrays">The nested requirement text to flatten and normalize.</param>
    /// <returns>The resulting requirement list.</returns>
    public RubyGemsRequirementList CreateNestedRequirementList(
        string?[]?[] requirementTextArrays)
    {
        return RubyGemsRequirementList.CreateFromArrays(requirementTextArrays);
    }

    /// <summary>
    /// Reports whether a requirement list admits RubyGems version text.
    /// </summary>
    /// <param name="requirements">The requirements to apply.</param>
    /// <param name="versionText">The version text to process.</param>
    /// <returns><c>true</c> when every requirement admits the version; otherwise, <c>false</c>.</returns>
    public bool RequirementAllowsVersion(
        RubyGemsRequirementList requirements,
        string versionText)
    {
        System.ArgumentNullException.ThrowIfNull(requirements);

        return requirements.AllowsVersionText(versionText);
    }

    /// <summary>
    /// Reports whether a gem platform is compatible with a local platform.
    /// </summary>
    /// <param name="gemPlatform">The gem platform to examine.</param>
    /// <param name="localPlatform">The local platform to examine.</param>
    /// <returns><c>true</c> when the platforms are compatible; otherwise, <c>false</c>.</returns>
    public bool PlatformMatches(
        RubyGemsPlatform gemPlatform,
        RubyGemsPlatform localPlatform)
    {
        return RubyGemsPlatformSelector.PlatformMatches(gemPlatform, localPlatform);
    }

    /// <summary>
    /// Filters specifications to those compatible with at least one local platform.
    /// </summary>
    /// <param name="specifications">The specifications to examine.</param>
    /// <param name="localPlatforms">The local platforms to examine.</param>
    /// <returns>A new array containing compatible specifications in input order.</returns>
    public RubyGemsSpecification[] FilterCompatibleSpecifications(
        RubyGemsSpecification[] specifications,
        RubyGemsPlatform[] localPlatforms)
    {
        return RubyGemsPlatformSelector.FilterCompatibleSpecifications(
            specifications,
            localPlatforms);
    }

    /// <summary>
    /// Selects the best platform specification, preferring exact native matches.
    /// </summary>
    /// <param name="specifications">The specifications to examine.</param>
    /// <param name="localPlatforms">The local platforms to examine.</param>
    /// <returns>The best compatible specification, or null when none is compatible.</returns>
    public RubyGemsSpecification? SelectBestPlatformSpecification(
        RubyGemsSpecification[] specifications,
        RubyGemsPlatform[] localPlatforms)
    {
        return RubyGemsPlatformSelector.SelectBestSpecification(
            specifications,
            localPlatforms);
    }

    /// <summary>
    /// Resolves RubyGems dependencies from the supplied specifications.
    /// </summary>
    /// <param name="rootDependencies">The root dependencies to examine.</param>
    /// <param name="availableSpecifications">The available specifications to examine.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A result containing the resolved dependency graph and conflicts.</returns>
    public RubyGemsDependencyResolutionResult ResolveDependencies(
        RubyGemsDependency[] rootDependencies,
        RubyGemsSpecification[] availableSpecifications,
        RubyGemsResolverOptions options)
    {
        return RubyGemsDependencyResolver.Resolve(
            rootDependencies,
            availableSpecifications,
            options);
    }
}
