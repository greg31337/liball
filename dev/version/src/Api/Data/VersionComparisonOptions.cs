namespace version;

/// <summary>
/// Configures version comparison behavior.
/// </summary>
/// <remarks>
/// Configure behavior through the public fields before passing the value to an operation.
/// </remarks>
public class VersionComparisonOptions
{
    /// <summary>
    /// Stores the <c>dialect</c> value.
    /// </summary>
    public VersionDialect dialect;
    /// <summary>
    /// Stores the <c>comparisonMode</c> value.
    /// </summary>
    public VersionComparisonMode comparisonMode;
    /// <summary>
    /// Stores the <c>caseSensitive</c> value.
    /// </summary>
    public bool caseSensitive;
    /// <summary>
    /// Stores the <c>includeBuildMetadata</c> value.
    /// </summary>
    public bool includeBuildMetadata;
    /// <summary>
    /// Stores the <c>invalidVersionOrdering</c> value.
    /// </summary>
    public InvalidVersionOrdering invalidVersionOrdering;

    /// <summary>
    /// Initializes a new Version Comparison Options instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="dialectValue">The dialect value used by the operation.</param>
    /// <param name="comparisonModeValue">The comparison mode value used by the operation.</param>
    /// <param name="caseSensitiveValue">Whether to enable case sensitive value.</param>
    /// <param name="includeBuildMetadataValue">Whether to enable include build metadata value.</param>
    /// <param name="invalidVersionOrderingValue">The invalid version ordering value used by the operation.</param>
    public VersionComparisonOptions(
        VersionDialect dialectValue,
        VersionComparisonMode comparisonModeValue,
        bool caseSensitiveValue,
        bool includeBuildMetadataValue,
        InvalidVersionOrdering invalidVersionOrderingValue)
    {
        ValidateDialect(dialectValue);
        ValidateComparisonMode(comparisonModeValue);
        ValidateInvalidVersionOrdering(invalidVersionOrderingValue);

        dialect = dialectValue;
        comparisonMode = comparisonModeValue;
        caseSensitive = caseSensitiveValue;
        includeBuildMetadata = includeBuildMetadataValue;
        invalidVersionOrdering = invalidVersionOrderingValue;
    }

    /// <summary>
    /// Creates a default.
    /// </summary>
    /// <returns>The resulting version comparison options.</returns>
    internal static VersionComparisonOptions CreateDefault()
    {
        return new VersionComparisonOptions(
            VersionDialect.Unknown,
            VersionComparisonMode.Dialect,
            true,
            false,
            InvalidVersionOrdering.Reject);
    }

    /// <summary>
    /// Creates a for dialect.
    /// </summary>
    /// <param name="dialect">The dialect used by the operation.</param>
    /// <returns>The resulting version comparison options.</returns>
    internal static VersionComparisonOptions CreateForDialect(VersionDialect dialect)
    {
        return CreateWithMode(dialect, VersionComparisonMode.Dialect);
    }

    /// <summary>
    /// Creates a with mode.
    /// </summary>
    /// <param name="dialect">The dialect used by the operation.</param>
    /// <param name="comparisonMode">The comparison mode used by the operation.</param>
    /// <returns>The resulting version comparison options.</returns>
    internal static VersionComparisonOptions CreateWithMode(VersionDialect dialect, VersionComparisonMode comparisonMode)
    {
        return new VersionComparisonOptions(dialect, comparisonMode, true, false, InvalidVersionOrdering.Reject);
    }

    /// <summary>
    /// Creates a precedence.
    /// </summary>
    /// <param name="dialect">The dialect used by the operation.</param>
    /// <returns>The resulting version comparison options.</returns>
    internal static VersionComparisonOptions CreatePrecedence(VersionDialect dialect)
    {
        return CreateWithMode(dialect, VersionComparisonMode.Precedence);
    }

    /// <summary>
    /// Creates an identity.
    /// </summary>
    /// <param name="dialect">The dialect used by the operation.</param>
    /// <returns>The resulting version comparison options.</returns>
    internal static VersionComparisonOptions CreateIdentity(VersionDialect dialect)
    {
        return CreateWithMode(dialect, VersionComparisonMode.Identity);
    }

    /// <summary>
    /// Creates a normalized identity.
    /// </summary>
    /// <param name="dialect">The dialect used by the operation.</param>
    /// <returns>The resulting version comparison options.</returns>
    internal static VersionComparisonOptions CreateNormalizedIdentity(VersionDialect dialect)
    {
        return CreateWithMode(dialect, VersionComparisonMode.NormalizedIdentity);
    }

    /// <summary>
    /// Creates a dialect.
    /// </summary>
    /// <param name="dialect">The dialect used by the operation.</param>
    /// <returns>The resulting version comparison options.</returns>
    internal static VersionComparisonOptions CreateDialect(VersionDialect dialect)
    {
        return CreateWithMode(dialect, VersionComparisonMode.Dialect);
    }

    /// <summary>
    /// Creates a stability aware.
    /// </summary>
    /// <param name="dialect">The dialect used by the operation.</param>
    /// <returns>The resulting version comparison options.</returns>
    internal static VersionComparisonOptions CreateStabilityAware(VersionDialect dialect)
    {
        return CreateWithMode(dialect, VersionComparisonMode.StabilityAware);
    }

    /// <summary>
    /// Creates a case insensitive.
    /// </summary>
    /// <param name="dialect">The dialect used by the operation.</param>
    /// <param name="comparisonMode">The comparison mode used by the operation.</param>
    /// <returns>The resulting version comparison options.</returns>
    internal static VersionComparisonOptions CreateCaseInsensitive(VersionDialect dialect, VersionComparisonMode comparisonMode)
    {
        VersionComparisonOptions options = CreateWithMode(dialect, comparisonMode);
        options.SetCaseSensitive(false);
        return options;
    }

    /// <summary>
    /// Creates an including build metadata.
    /// </summary>
    /// <param name="dialect">The dialect used by the operation.</param>
    /// <param name="comparisonMode">The comparison mode used by the operation.</param>
    /// <returns>The resulting version comparison options.</returns>
    internal static VersionComparisonOptions CreateIncludingBuildMetadata(
        VersionDialect dialect,
        VersionComparisonMode comparisonMode)
    {
        VersionComparisonOptions options = CreateWithMode(dialect, comparisonMode);
        options.SetIncludeBuildMetadata(true);
        return options;
    }

    /// <summary>
    /// Creates a with invalid version ordering.
    /// </summary>
    /// <param name="dialect">The dialect used by the operation.</param>
    /// <param name="comparisonMode">The comparison mode used by the operation.</param>
    /// <param name="invalidVersionOrdering">The invalid version ordering used by the operation.</param>
    /// <returns>The resulting version comparison options.</returns>
    internal static VersionComparisonOptions CreateWithInvalidVersionOrdering(
        VersionDialect dialect,
        VersionComparisonMode comparisonMode,
        InvalidVersionOrdering invalidVersionOrdering)
    {
        VersionComparisonOptions options = CreateWithMode(dialect, comparisonMode);
        options.SetInvalidVersionOrdering(invalidVersionOrdering);
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
    /// Gets a comparison mode.
    /// </summary>
    /// <returns>The resulting version comparison mode.</returns>
    internal VersionComparisonMode GetComparisonMode()
    {
        return comparisonMode;
    }

    /// <summary>
    /// Updates the comparison mode setting.
    /// </summary>
    /// <param name="comparisonModeValue">The comparison mode value used by the operation.</param>
    internal void SetComparisonMode(VersionComparisonMode comparisonModeValue)
    {
        ValidateComparisonMode(comparisonModeValue);
        comparisonMode = comparisonModeValue;
    }

    /// <summary>
    /// Reports whether case sensitive.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsCaseSensitive()
    {
        return caseSensitive;
    }

    /// <summary>
    /// Updates the case sensitive setting.
    /// </summary>
    /// <param name="caseSensitiveValue">Whether to enable case sensitive value.</param>
    internal void SetCaseSensitive(bool caseSensitiveValue)
    {
        caseSensitive = caseSensitiveValue;
    }

    /// <summary>
    /// Reports whether build metadata.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IncludesBuildMetadata()
    {
        return includeBuildMetadata;
    }

    /// <summary>
    /// Updates the include build metadata setting.
    /// </summary>
    /// <param name="includeBuildMetadataValue">Whether to enable include build metadata value.</param>
    internal void SetIncludeBuildMetadata(bool includeBuildMetadataValue)
    {
        includeBuildMetadata = includeBuildMetadataValue;
    }

    /// <summary>
    /// Gets an invalid version ordering.
    /// </summary>
    /// <returns>The resulting invalid version ordering.</returns>
    internal InvalidVersionOrdering GetInvalidVersionOrdering()
    {
        return invalidVersionOrdering;
    }

    /// <summary>
    /// Updates the invalid version ordering setting.
    /// </summary>
    /// <param name="invalidVersionOrderingValue">The invalid version ordering value used by the operation.</param>
    internal void SetInvalidVersionOrdering(InvalidVersionOrdering invalidVersionOrderingValue)
    {
        ValidateInvalidVersionOrdering(invalidVersionOrderingValue);
        invalidVersionOrdering = invalidVersionOrderingValue;
    }

    /// <summary>
    /// Validates a dialect.
    /// </summary>
    /// <param name="dialectValue">The dialect value used by the operation.</param>
    internal static void ValidateDialect(VersionDialect dialectValue)
    {
        VersionParseOptions.ValidateDialect(dialectValue);
    }

    /// <summary>
    /// Validates a comparison mode.
    /// </summary>
    /// <param name="comparisonModeValue">The comparison mode value used by the operation.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal static void ValidateComparisonMode(VersionComparisonMode comparisonModeValue)
    {
        if (comparisonModeValue == VersionComparisonMode.Precedence)
        {
            return;
        }

        if (comparisonModeValue == VersionComparisonMode.Identity)
        {
            return;
        }

        if (comparisonModeValue == VersionComparisonMode.NormalizedIdentity)
        {
            return;
        }

        if (comparisonModeValue == VersionComparisonMode.Dialect)
        {
            return;
        }

        if (comparisonModeValue == VersionComparisonMode.StabilityAware)
        {
            return;
        }

        throw new System.ArgumentOutOfRangeException("comparisonModeValue");
    }

    /// <summary>
    /// Validates an invalid version ordering.
    /// </summary>
    /// <param name="invalidVersionOrderingValue">The invalid version ordering value used by the operation.</param>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal static void ValidateInvalidVersionOrdering(InvalidVersionOrdering invalidVersionOrderingValue)
    {
        if (invalidVersionOrderingValue == InvalidVersionOrdering.Reject)
        {
            return;
        }

        if (invalidVersionOrderingValue == InvalidVersionOrdering.SortBeforeValid)
        {
            return;
        }

        if (invalidVersionOrderingValue == InvalidVersionOrdering.SortAfterValid)
        {
            return;
        }

        if (invalidVersionOrderingValue == InvalidVersionOrdering.TreatAsEqual)
        {
            return;
        }

        throw new System.ArgumentOutOfRangeException("invalidVersionOrderingValue");
    }
}
