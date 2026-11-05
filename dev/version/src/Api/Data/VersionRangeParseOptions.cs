namespace version;

/// <summary>
/// Configures version range parse behavior.
/// </summary>
/// <remarks>
/// Configure behavior through the public fields before passing the value to an operation.
/// </remarks>
public class VersionRangeParseOptions
{
    /// <summary>
    /// Stores the <c>rangeSyntax</c> value.
    /// </summary>
    public RangeSyntax rangeSyntax;
    /// <summary>
    /// Stores the <c>prereleasePolicy</c> value.
    /// </summary>
    public PrereleasePolicy prereleasePolicy;
    /// <summary>
    /// Stores the <c>maxInputLength</c> value.
    /// </summary>
    public int maxInputLength;
    /// <summary>
    /// Stores the <c>allowWildcardVersions</c> value.
    /// </summary>
    public bool allowWildcardVersions;
    /// <summary>
    /// Stores the <c>allowHyphenRanges</c> value.
    /// </summary>
    public bool allowHyphenRanges;
    /// <summary>
    /// Stores the <c>allowFloatingVersions</c> value.
    /// </summary>
    public bool allowFloatingVersions;
    /// <summary>
    /// Stores the <c>allowMultipleSets</c> value.
    /// </summary>
    public bool allowMultipleSets;
    /// <summary>
    /// Stores the <c>allowUnboundedRanges</c> value.
    /// </summary>
    public bool allowUnboundedRanges;

    /// <summary>
    /// Initializes a new Version Range Parse Options instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="rangeSyntaxValue">The range syntax value used by the operation.</param>
    /// <param name="prereleasePolicyValue">The prerelease policy value used by the operation.</param>
    /// <param name="maxInputLengthValue">The max input length value used by the operation.</param>
    /// <param name="allowWildcardVersionsValue">Whether to enable allow wildcard versions value.</param>
    /// <param name="allowHyphenRangesValue">Whether to enable allow hyphen ranges value.</param>
    /// <param name="allowFloatingVersionsValue">Whether to enable allow floating versions value.</param>
    /// <param name="allowMultipleSetsValue">Whether to enable allow multiple sets value.</param>
    /// <param name="allowUnboundedRangesValue">Whether to enable allow unbounded ranges value.</param>
    public VersionRangeParseOptions(
        RangeSyntax rangeSyntaxValue,
        PrereleasePolicy prereleasePolicyValue,
        int maxInputLengthValue,
        bool allowWildcardVersionsValue,
        bool allowHyphenRangesValue,
        bool allowFloatingVersionsValue,
        bool allowMultipleSetsValue,
        bool allowUnboundedRangesValue)
    {
        ValidateRangeSyntax(rangeSyntaxValue);
        ValidatePrereleasePolicy(prereleasePolicyValue);
        ValidateMaxInputLength(maxInputLengthValue);

        rangeSyntax = rangeSyntaxValue;
        prereleasePolicy = prereleasePolicyValue;
        maxInputLength = maxInputLengthValue;
        allowWildcardVersions = allowWildcardVersionsValue;
        allowHyphenRanges = allowHyphenRangesValue;
        allowFloatingVersions = allowFloatingVersionsValue;
        allowMultipleSets = allowMultipleSetsValue;
        allowUnboundedRanges = allowUnboundedRangesValue;
    }

    /// <summary>
    /// Creates a default.
    /// </summary>
    /// <returns>The resulting version range parse options.</returns>
    internal static VersionRangeParseOptions CreateDefault()
    {
        return new VersionRangeParseOptions(
            RangeSyntax.Unknown,
            PrereleasePolicy.Default,
            4096,
            true,
            true,
            true,
            true,
            true);
    }

    /// <summary>
    /// Creates a for syntax.
    /// </summary>
    /// <param name="rangeSyntax">The range syntax used by the operation.</param>
    /// <returns>The resulting version range parse options.</returns>
    internal static VersionRangeParseOptions CreateForSyntax(RangeSyntax rangeSyntax)
    {
        return CreateForSyntaxAndPrereleasePolicy(rangeSyntax, PrereleasePolicy.Default);
    }

    /// <summary>
    /// Creates a node semver.
    /// </summary>
    /// <returns>The resulting version range parse options.</returns>
    internal static VersionRangeParseOptions CreateNodeSemVer()
    {
        return CreateForSyntax(RangeSyntax.NodeSemVer);
    }

    /// <summary>
    /// Creates a cargo.
    /// </summary>
    /// <returns>The resulting version range parse options.</returns>
    internal static VersionRangeParseOptions CreateCargo()
    {
        return CreateForSyntax(RangeSyntax.Cargo);
    }

    /// <summary>
    /// Creates a nuget.
    /// </summary>
    /// <returns>The resulting version range parse options.</returns>
    internal static VersionRangeParseOptions CreateNuGet()
    {
        return CreateForSyntax(RangeSyntax.NuGet);
    }

    /// <summary>
    /// Creates a maven.
    /// </summary>
    /// <returns>The resulting version range parse options.</returns>
    internal static VersionRangeParseOptions CreateMaven()
    {
        return CreateForSyntax(RangeSyntax.Maven);
    }

    /// <summary>
    /// Creates a rubygems.
    /// </summary>
    /// <returns>The resulting version range parse options.</returns>
    internal static VersionRangeParseOptions CreateRubyGems()
    {
        return CreateForSyntax(RangeSyntax.RubyGems);
    }

    /// <summary>
    /// Creates a python.
    /// </summary>
    /// <returns>The resulting version range parse options.</returns>
    internal static VersionRangeParseOptions CreatePython()
    {
        return CreateForSyntax(RangeSyntax.Python);
    }

    /// <summary>
    /// Creates a with prerelease policy.
    /// </summary>
    /// <param name="rangeSyntax">The range syntax used by the operation.</param>
    /// <param name="prereleasePolicy">The prerelease policy used by the operation.</param>
    /// <returns>The resulting version range parse options.</returns>
    internal static VersionRangeParseOptions CreateWithPrereleasePolicy(
        RangeSyntax rangeSyntax,
        PrereleasePolicy prereleasePolicy)
    {
        return CreateForSyntaxAndPrereleasePolicy(rangeSyntax, prereleasePolicy);
    }

    /// <summary>
    /// Creates options for a syntax and prerelease policy using the remaining defaults.
    /// </summary>
    /// <param name="rangeSyntax">The range syntax used by the operation.</param>
    /// <param name="prereleasePolicy">The prerelease policy used by the operation.</param>
    /// <returns>The resulting version range parse options.</returns>
    internal static VersionRangeParseOptions CreateForSyntaxAndPrereleasePolicy(
        RangeSyntax rangeSyntax,
        PrereleasePolicy prereleasePolicy)
    {
        return new VersionRangeParseOptions(
            rangeSyntax,
            prereleasePolicy,
            4096,
            true,
            true,
            true,
            true,
            true);
    }

    /// <summary>
    /// Creates a with max input length.
    /// </summary>
    /// <param name="rangeSyntax">The range syntax used by the operation.</param>
    /// <param name="maxInputLength">The requested max input length.</param>
    /// <returns>The resulting version range parse options.</returns>
    internal static VersionRangeParseOptions CreateWithMaxInputLength(RangeSyntax rangeSyntax, int maxInputLength)
    {
        VersionRangeParseOptions options = CreateForSyntax(rangeSyntax);
        options.SetMaxInputLength(maxInputLength);
        return options;
    }

    /// <summary>
    /// Gets a range syntax.
    /// </summary>
    /// <returns>The resulting range syntax.</returns>
    internal RangeSyntax GetRangeSyntax()
    {
        return rangeSyntax;
    }

    /// <summary>
    /// Updates the range syntax setting.
    /// </summary>
    /// <param name="rangeSyntaxValue">The range syntax value used by the operation.</param>
    internal void SetRangeSyntax(RangeSyntax rangeSyntaxValue)
    {
        ValidateRangeSyntax(rangeSyntaxValue);
        rangeSyntax = rangeSyntaxValue;
    }

    /// <summary>
    /// Gets a prerelease policy.
    /// </summary>
    /// <returns>The resulting prerelease policy.</returns>
    internal PrereleasePolicy GetPrereleasePolicy()
    {
        return prereleasePolicy;
    }

    /// <summary>
    /// Updates the prerelease policy setting.
    /// </summary>
    /// <param name="prereleasePolicyValue">The prerelease policy value used by the operation.</param>
    internal void SetPrereleasePolicy(PrereleasePolicy prereleasePolicyValue)
    {
        ValidatePrereleasePolicy(prereleasePolicyValue);
        prereleasePolicy = prereleasePolicyValue;
    }

    /// <summary>
    /// Gets a max input length.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetMaxInputLength()
    {
        return maxInputLength;
    }

    /// <summary>
    /// Updates the max input length setting.
    /// </summary>
    /// <param name="maxInputLengthValue">The max input length value used by the operation.</param>
    internal void SetMaxInputLength(int maxInputLengthValue)
    {
        ValidateMaxInputLength(maxInputLengthValue);
        maxInputLength = maxInputLengthValue;
    }

    /// <summary>
    /// Reports whether wildcard versions.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool AllowsWildcardVersions()
    {
        return allowWildcardVersions;
    }

    /// <summary>
    /// Updates the allow wildcard versions setting.
    /// </summary>
    /// <param name="allowWildcardVersionsValue">Whether to enable allow wildcard versions value.</param>
    internal void SetAllowWildcardVersions(bool allowWildcardVersionsValue)
    {
        allowWildcardVersions = allowWildcardVersionsValue;
    }

    /// <summary>
    /// Reports whether hyphen ranges.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool AllowsHyphenRanges()
    {
        return allowHyphenRanges;
    }

    /// <summary>
    /// Updates the allow hyphen ranges setting.
    /// </summary>
    /// <param name="allowHyphenRangesValue">Whether to enable allow hyphen ranges value.</param>
    internal void SetAllowHyphenRanges(bool allowHyphenRangesValue)
    {
        allowHyphenRanges = allowHyphenRangesValue;
    }

    /// <summary>
    /// Reports whether floating versions.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool AllowsFloatingVersions()
    {
        return allowFloatingVersions;
    }

    /// <summary>
    /// Updates the allow floating versions setting.
    /// </summary>
    /// <param name="allowFloatingVersionsValue">Whether to enable allow floating versions value.</param>
    internal void SetAllowFloatingVersions(bool allowFloatingVersionsValue)
    {
        allowFloatingVersions = allowFloatingVersionsValue;
    }

    /// <summary>
    /// Reports whether multiple sets.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool AllowsMultipleSets()
    {
        return allowMultipleSets;
    }

    /// <summary>
    /// Updates the allow multiple sets setting.
    /// </summary>
    /// <param name="allowMultipleSetsValue">Whether to enable allow multiple sets value.</param>
    internal void SetAllowMultipleSets(bool allowMultipleSetsValue)
    {
        allowMultipleSets = allowMultipleSetsValue;
    }

    /// <summary>
    /// Reports whether unbounded ranges.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool AllowsUnboundedRanges()
    {
        return allowUnboundedRanges;
    }

    /// <summary>
    /// Updates the allow unbounded ranges setting.
    /// </summary>
    /// <param name="allowUnboundedRangesValue">Whether to enable allow unbounded ranges value.</param>
    internal void SetAllowUnboundedRanges(bool allowUnboundedRangesValue)
    {
        allowUnboundedRanges = allowUnboundedRangesValue;
    }

    /// <summary>
    /// Validates a range syntax.
    /// </summary>
    /// <param name="rangeSyntaxValue">The range syntax value used by the operation.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal static void ValidateRangeSyntax(RangeSyntax rangeSyntaxValue)
    {
        if (rangeSyntaxValue == RangeSyntax.Unknown)
        {
            return;
        }

        if (rangeSyntaxValue == RangeSyntax.NodeSemVer)
        {
            return;
        }

        if (rangeSyntaxValue == RangeSyntax.Cargo)
        {
            return;
        }

        if (rangeSyntaxValue == RangeSyntax.NuGet)
        {
            return;
        }

        if (rangeSyntaxValue == RangeSyntax.Maven)
        {
            return;
        }

        if (rangeSyntaxValue == RangeSyntax.RubyGems)
        {
            return;
        }

        if (rangeSyntaxValue == RangeSyntax.Python)
        {
            return;
        }

        throw new System.ArgumentOutOfRangeException("rangeSyntaxValue");
    }

    /// <summary>
    /// Validates a prerelease policy.
    /// </summary>
    /// <param name="prereleasePolicyValue">The prerelease policy value used by the operation.</param>
    internal static void ValidatePrereleasePolicy(PrereleasePolicy prereleasePolicyValue)
    {
        VersionParseOptions.ValidatePrereleasePolicy(prereleasePolicyValue);
    }

    /// <summary>
    /// Validates a max input length.
    /// </summary>
    /// <param name="maxInputLengthValue">The max input length value used by the operation.</param>
    internal static void ValidateMaxInputLength(int maxInputLengthValue)
    {
        VersionParseOptions.ValidateMaxInputLength(maxInputLengthValue);
    }
}
