using System;
namespace token;

/// <summary>
/// Describes registry, implementation-scope, and security-policy metadata for an algorithm.
/// </summary>
public class AlgorithmMetadata
{
    /// <summary>The registered algorithm name, or the queried JOSE name when it is unknown.</summary>
    public string? Name;

    /// <summary>The queried COSE integer label.</summary>
    public int Label;

    /// <summary>Indicates whether <see cref="Label"/> identifies this algorithm.</summary>
    public bool UsesIntegerLabel;

    /// <summary>Indicates whether the algorithm occurs in the corresponding registry.</summary>
    public bool Known;

    /// <summary>Indicates whether the algorithm is within the library's built-in-only implementation scope.</summary>
    public bool Supported;

    /// <summary>Indicates whether the registry recommends the algorithm for new uses.</summary>
    public bool Recommended;

    /// <summary>Indicates whether the registry marks the algorithm as deprecated.</summary>
    public bool Deprecated;

    /// <summary>Indicates whether the registry prohibits use of the algorithm.</summary>
    public bool Prohibited;

    /// <summary>Indicates whether library policy permits the algorithm only through explicit compatibility opt-in.</summary>
    public bool CompatibilityOnly;

    /// <summary>Indicates whether the identifier fixes every cryptographic algorithm choice tracked by the registry.</summary>
    public bool FullySpecified;

    internal AlgorithmMetadata(
        string? name,
        int label,
        bool usesIntegerLabel,
        bool known,
        bool supported,
        bool recommended,
        bool deprecated,
        bool prohibited,
        bool compatibilityOnly,
        bool fullySpecified)
    {
        Name = name;
        Label = label;
        UsesIntegerLabel = usesIntegerLabel;
        Known = known;
        Supported = supported;
        Recommended = recommended;
        Deprecated = deprecated;
        Prohibited = prohibited;
        CompatibilityOnly = compatibilityOnly;
        FullySpecified = fullySpecified;
    }
}
