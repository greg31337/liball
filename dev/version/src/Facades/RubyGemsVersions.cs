namespace version;

/// <summary>
/// Provides the public entry points for RubyGems version operations.
/// </summary>
/// <remarks>
/// Parsing methods return result objects for ordinary invalid input. Comparison, formatting, and resolver helpers apply the rules of this facade's ecosystem.
/// </remarks>
internal static class RubyGemsVersions
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
        return RubyGemsVersionParser.ParseUniversal(text);
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
            VersionOptionDispatcher.CreateParseOptionsForDialect(options, VersionDialect.RubyGems));
    }

    /// <summary>
    /// Parses a dependency version requirement from text.
    /// </summary>
    /// <remarks>
    /// Invalid input is represented by a failure result with diagnostics rather than by a parsing exception.
    /// </remarks>
    /// <param name="text">The text to process.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public static VersionRangeParseResult ParseRequirement(string text)
    {
        return RubyGemsRangeParser.Parse(text);
    }

    /// <summary>
    /// Parses a dependency version requirement from text.
    /// </summary>
    /// <remarks>
    /// Invalid input is represented by a failure result with diagnostics rather than by a parsing exception.
    /// </remarks>
    /// <param name="text">The text to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public static VersionRangeParseResult ParseRequirement(string text, VersionRangeParseOptions options)
    {
        return VersionOptionDispatcher.ParseRange(
            text,
            VersionOptionDispatcher.CreateRangeOptionsForSyntax(options, RangeSyntax.RubyGems));
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
        return ParseRequirement(text);
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
        return ParseRequirement(text, options);
    }

    /// <summary>
    /// Creates a default requirement.
    /// </summary>
    /// <returns>The resulting rubygems requirement list.</returns>
    public static RubyGemsRequirementList CreateDefaultRequirement()
    {
        return RubyGemsRequirementList.CreateDefault();
    }

    /// <summary>
    /// Creates a default prerelease requirement.
    /// </summary>
    /// <returns>The resulting rubygems requirement list.</returns>
    public static RubyGemsRequirementList CreateDefaultPrereleaseRequirement()
    {
        return RubyGemsRequirementList.CreateDefaultPrerelease();
    }

    /// <summary>
    /// Creates a requirement list.
    /// </summary>
    /// <param name="requirementTexts">The requirement texts to process.</param>
    /// <returns>The resulting rubygems requirement list.</returns>
    public static RubyGemsRequirementList CreateRequirementList(string?[] requirementTexts)
    {
        return new RubyGemsRequirementList(requirementTexts);
    }

    /// <summary>
    /// Creates a requirement list.
    /// </summary>
    /// <param name="requirementTextArrays">The requirement text arrays to process.</param>
    /// <returns>The resulting rubygems requirement list.</returns>
    public static RubyGemsRequirementList CreateRequirementList(string?[]?[] requirementTextArrays)
    {
        return RubyGemsRequirementList.CreateFromArrays(requirementTextArrays);
    }

    /// <summary>
    /// Reports whether a gem platform is compatible with a local platform.
    /// </summary>
    /// <param name="gemPlatform">The gem platform used by the operation.</param>
    /// <param name="localPlatform">The local platform used by the operation.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    public static bool PlatformMatches(RubyGemsPlatform gemPlatform, RubyGemsPlatform localPlatform)
    {
        return RubyGemsPlatformSelector.PlatformMatches(gemPlatform, localPlatform);
    }

    /// <summary>
    /// Filters the supplied values using compatible specifications rules.
    /// </summary>
    /// <remarks>
    /// The operation evaluates the supplied in-memory candidates deterministically; it does not contact package registries or other external services.
    /// </remarks>
    /// <param name="specifications">The specifications to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="localPlatforms">The local platforms to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <returns>A new array containing the matching or represented values.</returns>
    public static RubyGemsSpecification[] FilterCompatibleSpecifications(
        RubyGemsSpecification[] specifications,
        RubyGemsPlatform[] localPlatforms)
    {
        return RubyGemsPlatformSelector.FilterCompatibleSpecifications(specifications, localPlatforms);
    }

    /// <summary>
    /// Selects a best platform specification from the supplied candidates.
    /// </summary>
    /// <remarks>
    /// The operation evaluates the supplied in-memory candidates deterministically; it does not contact package registries or other external services.
    /// </remarks>
    /// <param name="specifications">The specifications to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="localPlatforms">The local platforms to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <returns>The resulting rubygems specification.</returns>
    public static RubyGemsSpecification? SelectBestPlatformSpecification(
        RubyGemsSpecification[] specifications,
        RubyGemsPlatform[] localPlatforms)
    {
        return RubyGemsPlatformSelector.SelectBestSpecification(specifications, localPlatforms);
    }

    /// <summary>
    /// Resolves a dependencies from the supplied metadata.
    /// </summary>
    /// <remarks>
    /// The operation evaluates the supplied in-memory candidates deterministically; it does not contact package registries or other external services.
    /// </remarks>
    /// <param name="rootDependencies">The root dependencies to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="availableSpecifications">The available specifications to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public static RubyGemsDependencyResolutionResult ResolveDependencies(
        RubyGemsDependency[] rootDependencies,
        RubyGemsSpecification[] availableSpecifications)
    {
        return RubyGemsDependencyResolver.Resolve(rootDependencies, availableSpecifications);
    }

    /// <summary>
    /// Resolves a dependencies from the supplied metadata.
    /// </summary>
    /// <remarks>
    /// The operation evaluates the supplied in-memory candidates deterministically; it does not contact package registries or other external services.
    /// </remarks>
    /// <param name="rootDependencies">The root dependencies to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="availableSpecifications">The available specifications to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>A result containing the produced value on success or diagnostics on failure.</returns>
    public static RubyGemsDependencyResolutionResult ResolveDependencies(
        RubyGemsDependency[] rootDependencies,
        RubyGemsSpecification[] availableSpecifications,
        RubyGemsResolverOptions options)
    {
        return RubyGemsDependencyResolver.Resolve(rootDependencies, availableSpecifications, options);
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
        return RubyGemsVersionComparer.Compare(leftText, rightText);
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
            VersionOptionDispatcher.CreateComparisonOptionsForDialect(options, VersionDialect.RubyGems));
    }

    /// <summary>
    /// Formats the supplied value as normalized text.
    /// </summary>
    /// <remarks>
    /// Formatting does not change the represented version; the selected form controls which original or normalized details are emitted.
    /// </remarks>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="System.ArgumentException">An argument is malformed or inconsistent with the requested operation.</exception>
    public static string FormatNormalized(string text)
    {
        VersionParseResult result = Parse(text);
        if (result.IsFailure())
        {
            throw new System.ArgumentException("Invalid RubyGems version.");
        }

        return result.GetVersion().ToNormalizedString();
    }

    /// <summary>
    /// Formats the supplied value as canonical text.
    /// </summary>
    /// <remarks>
    /// Formatting does not change the represented version; the selected form controls which original or normalized details are emitted.
    /// </remarks>
    /// <param name="text">The text to process.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="System.ArgumentException">An argument is malformed or inconsistent with the requested operation.</exception>
    public static string FormatCanonical(string text)
    {
        VersionParseResult result = Parse(text);
        if (result.IsFailure())
        {
            throw new System.ArgumentException("Invalid RubyGems version.");
        }

        return result.GetVersion().ToCanonicalString();
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
