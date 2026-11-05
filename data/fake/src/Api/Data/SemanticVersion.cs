namespace fake;

/// <summary>Represents the parsed components of a valid Semantic Versioning 2.0.0 value.</summary>
public class SemanticVersion
{
    /// <summary>The complete version text.</summary>
    public string Value = string.Empty;

    /// <summary>The nonnegative major-version numeric identifier.</summary>
    public string Major = string.Empty;

    /// <summary>The nonnegative minor-version numeric identifier.</summary>
    public string Minor = string.Empty;

    /// <summary>The nonnegative patch-version numeric identifier.</summary>
    public string Patch = string.Empty;

    /// <summary>The dot-separated pre-release identifiers, or an empty string.</summary>
    public string PreRelease = string.Empty;

    /// <summary>The dot-separated build identifiers, or an empty string.</summary>
    public string BuildMetadata = string.Empty;

    /// <summary>Whether the value contains pre-release identifiers.</summary>
    public bool HasPreRelease;

    /// <summary>Whether the value contains build metadata.</summary>
    public bool HasBuildMetadata;

    /// <summary>The Semantic Versioning specification version, <c>2.0.0</c>.</summary>
    public string SpecificationVersion = string.Empty;

    internal SemanticVersion()
    {
        Value = string.Empty;
        Major = string.Empty;
        Minor = string.Empty;
        Patch = string.Empty;
        PreRelease = string.Empty;
        BuildMetadata = string.Empty;
        SpecificationVersion = string.Empty;
    }
}
