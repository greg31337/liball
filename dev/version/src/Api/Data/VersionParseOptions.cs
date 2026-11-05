namespace version;

/// <summary>
/// Configures version parse behavior.
/// </summary>
/// <remarks>
/// Configure behavior through the public fields before passing the value to an operation.
/// </remarks>
public class VersionParseOptions
{
    /// <summary>
    /// Stores the <c>dialect</c> value.
    /// </summary>
    public VersionDialect dialect;
    /// <summary>
    /// Stores the <c>parseMode</c> value.
    /// </summary>
    public VersionParseMode parseMode;
    /// <summary>
    /// Stores the <c>maxInputLength</c> value.
    /// </summary>
    public int maxInputLength;
    /// <summary>
    /// Stores the <c>allowPrereleaseVersions</c> value.
    /// </summary>
    public bool allowPrereleaseVersions;
    /// <summary>
    /// Stores the <c>allowBuildMetadata</c> value.
    /// </summary>
    public bool allowBuildMetadata;
    /// <summary>
    /// Stores the <c>prereleasePolicy</c> value.
    /// </summary>
    public PrereleasePolicy prereleasePolicy;

    /// <summary>
    /// Initializes a new Version Parse Options instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="dialectValue">The dialect value used by the operation.</param>
    /// <param name="parseModeValue">The parse mode value used by the operation.</param>
    /// <param name="maxInputLengthValue">The max input length value used by the operation.</param>
    /// <param name="allowPrereleaseVersionsValue">Whether to enable allow prerelease versions value.</param>
    /// <param name="allowBuildMetadataValue">Whether to enable allow build metadata value.</param>
    /// <param name="prereleasePolicyValue">The prerelease policy value used by the operation.</param>
    public VersionParseOptions(
        VersionDialect dialectValue,
        VersionParseMode parseModeValue,
        int maxInputLengthValue,
        bool allowPrereleaseVersionsValue,
        bool allowBuildMetadataValue,
        PrereleasePolicy prereleasePolicyValue)
    {
        ValidateDialect(dialectValue);
        ValidateParseMode(parseModeValue);
        ValidateMaxInputLength(maxInputLengthValue);
        ValidatePrereleasePolicy(prereleasePolicyValue);

        dialect = dialectValue;
        parseMode = parseModeValue;
        maxInputLength = maxInputLengthValue;
        allowPrereleaseVersions = allowPrereleaseVersionsValue;
        allowBuildMetadata = allowBuildMetadataValue;
        prereleasePolicy = prereleasePolicyValue;
    }

    /// <summary>
    /// Creates a default.
    /// </summary>
    /// <returns>The resulting version parse options.</returns>
    internal static VersionParseOptions CreateDefault()
    {
        return new VersionParseOptions(
            VersionDialect.Unknown,
            VersionParseMode.DialectSpecific,
            4096,
            true,
            true,
            PrereleasePolicy.Default);
    }

    /// <summary>
    /// Creates a for dialect.
    /// </summary>
    /// <param name="dialect">The dialect used by the operation.</param>
    /// <returns>The resulting version parse options.</returns>
    internal static VersionParseOptions CreateForDialect(VersionDialect dialect)
    {
        return CreateForDialectAndMode(dialect, VersionParseMode.DialectSpecific);
    }

    /// <summary>
    /// Creates a strict.
    /// </summary>
    /// <param name="dialect">The dialect used by the operation.</param>
    /// <returns>The resulting version parse options.</returns>
    internal static VersionParseOptions CreateStrict(VersionDialect dialect)
    {
        return CreateForDialectAndMode(dialect, VersionParseMode.Strict);
    }

    /// <summary>
    /// Creates a loose.
    /// </summary>
    /// <param name="dialect">The dialect used by the operation.</param>
    /// <returns>The resulting version parse options.</returns>
    internal static VersionParseOptions CreateLoose(VersionDialect dialect)
    {
        return CreateForDialectAndMode(dialect, VersionParseMode.Loose);
    }

    /// <summary>
    /// Creates a coerce.
    /// </summary>
    /// <param name="dialect">The dialect used by the operation.</param>
    /// <returns>The resulting version parse options.</returns>
    internal static VersionParseOptions CreateCoerce(VersionDialect dialect)
    {
        return CreateForDialectAndMode(dialect, VersionParseMode.Coerce);
    }

    /// <summary>
    /// Creates options for a dialect and parse mode using the remaining defaults.
    /// </summary>
    /// <param name="dialect">The dialect used by the operation.</param>
    /// <param name="parseMode">The parse mode used by the operation.</param>
    /// <returns>The resulting version parse options.</returns>
    internal static VersionParseOptions CreateForDialectAndMode(VersionDialect dialect, VersionParseMode parseMode)
    {
        return new VersionParseOptions(dialect, parseMode, 4096, true, true, PrereleasePolicy.Default);
    }

    /// <summary>
    /// Creates a with max input length.
    /// </summary>
    /// <param name="dialect">The dialect used by the operation.</param>
    /// <param name="parseMode">The parse mode used by the operation.</param>
    /// <param name="maxInputLength">The requested max input length.</param>
    /// <returns>The resulting version parse options.</returns>
    internal static VersionParseOptions CreateWithMaxInputLength(
        VersionDialect dialect,
        VersionParseMode parseMode,
        int maxInputLength)
    {
        VersionParseOptions options = CreateForDialectAndMode(dialect, parseMode);
        options.SetMaxInputLength(maxInputLength);
        return options;
    }

    /// <summary>
    /// Creates a with prerelease policy.
    /// </summary>
    /// <param name="dialect">The dialect used by the operation.</param>
    /// <param name="parseMode">The parse mode used by the operation.</param>
    /// <param name="prereleasePolicy">The prerelease policy used by the operation.</param>
    /// <returns>The resulting version parse options.</returns>
    internal static VersionParseOptions CreateWithPrereleasePolicy(
        VersionDialect dialect,
        VersionParseMode parseMode,
        PrereleasePolicy prereleasePolicy)
    {
        VersionParseOptions options = CreateForDialectAndMode(dialect, parseMode);
        options.SetPrereleasePolicy(prereleasePolicy);
        return options;
    }

    /// <summary>
    /// Gets a dialect.
    /// </summary>
    /// <returns>The resulting version dialect.</returns>
    internal VersionDialect GetDialect()
    {
        return dialect;
    }

    /// <summary>
    /// Updates the dialect setting.
    /// </summary>
    /// <param name="dialectValue">The dialect value used by the operation.</param>
    internal void SetDialect(VersionDialect dialectValue)
    {
        ValidateDialect(dialectValue);
        dialect = dialectValue;
    }

    /// <summary>
    /// Gets a parse mode.
    /// </summary>
    /// <returns>The resulting version parse mode.</returns>
    internal VersionParseMode GetParseMode()
    {
        return parseMode;
    }

    /// <summary>
    /// Updates the parse mode setting.
    /// </summary>
    /// <param name="parseModeValue">The parse mode value used by the operation.</param>
    internal void SetParseMode(VersionParseMode parseModeValue)
    {
        ValidateParseMode(parseModeValue);
        parseMode = parseModeValue;
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
    /// Reports whether prerelease versions.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool AllowsPrereleaseVersions()
    {
        return allowPrereleaseVersions;
    }

    /// <summary>
    /// Updates the allow prerelease versions setting.
    /// </summary>
    /// <param name="allowPrereleaseVersionsValue">Whether to enable allow prerelease versions value.</param>
    internal void SetAllowPrereleaseVersions(bool allowPrereleaseVersionsValue)
    {
        allowPrereleaseVersions = allowPrereleaseVersionsValue;
    }

    /// <summary>
    /// Reports whether build metadata.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool AllowsBuildMetadata()
    {
        return allowBuildMetadata;
    }

    /// <summary>
    /// Updates the allow build metadata setting.
    /// </summary>
    /// <param name="allowBuildMetadataValue">Whether to enable allow build metadata value.</param>
    internal void SetAllowBuildMetadata(bool allowBuildMetadataValue)
    {
        allowBuildMetadata = allowBuildMetadataValue;
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
    /// Validates a dialect.
    /// </summary>
    /// <param name="dialectValue">The dialect value used by the operation.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal static void ValidateDialect(VersionDialect dialectValue)
    {
        if (dialectValue == VersionDialect.Unknown)
        {
            return;
        }

        if (dialectValue == VersionDialect.SemVer)
        {
            return;
        }

        if (dialectValue == VersionDialect.NodeSemVer)
        {
            return;
        }

        if (dialectValue == VersionDialect.NuGet)
        {
            return;
        }

        if (dialectValue == VersionDialect.Maven)
        {
            return;
        }

        if (dialectValue == VersionDialect.Go)
        {
            return;
        }

        if (dialectValue == VersionDialect.Cargo)
        {
            return;
        }

        if (dialectValue == VersionDialect.RubyGems)
        {
            return;
        }

        if (dialectValue == VersionDialect.Python)
        {
            return;
        }

        throw new System.ArgumentOutOfRangeException("dialectValue");
    }

    /// <summary>
    /// Validates a parse mode.
    /// </summary>
    /// <param name="parseModeValue">The parse mode value used by the operation.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal static void ValidateParseMode(VersionParseMode parseModeValue)
    {
        if (parseModeValue == VersionParseMode.Strict)
        {
            return;
        }

        if (parseModeValue == VersionParseMode.Loose)
        {
            return;
        }

        if (parseModeValue == VersionParseMode.Coerce)
        {
            return;
        }

        if (parseModeValue == VersionParseMode.Partial)
        {
            return;
        }

        if (parseModeValue == VersionParseMode.DialectSpecific)
        {
            return;
        }

        throw new System.ArgumentOutOfRangeException("parseModeValue");
    }

    /// <summary>
    /// Validates a max input length.
    /// </summary>
    /// <param name="maxInputLengthValue">The max input length value used by the operation.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal static void ValidateMaxInputLength(int maxInputLengthValue)
    {
        if (maxInputLengthValue <= 0)
        {
            throw new System.ArgumentOutOfRangeException("maxInputLengthValue");
        }
    }

    /// <summary>
    /// Validates a prerelease policy.
    /// </summary>
    /// <param name="prereleasePolicyValue">The prerelease policy value used by the operation.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal static void ValidatePrereleasePolicy(PrereleasePolicy prereleasePolicyValue)
    {
        if (prereleasePolicyValue == PrereleasePolicy.Default)
        {
            return;
        }

        if (prereleasePolicyValue == PrereleasePolicy.Include)
        {
            return;
        }

        if (prereleasePolicyValue == PrereleasePolicy.Exclude)
        {
            return;
        }

        if (prereleasePolicyValue == PrereleasePolicy.ExplicitOnly)
        {
            return;
        }

        if (prereleasePolicyValue == PrereleasePolicy.AutoIfNoStableCandidate)
        {
            return;
        }

        throw new System.ArgumentOutOfRangeException("prereleasePolicyValue");
    }
}
