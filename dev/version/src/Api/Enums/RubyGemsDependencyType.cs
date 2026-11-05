namespace version;

/// <summary>
/// Defines the supported rubygems dependency values.
/// </summary>
/// <remarks>
/// Pass these values to the corresponding option or operation methods instead of relying on ecosystem-specific defaults.
/// </remarks>
public enum RubyGemsDependencyType
{
    /// <summary>
    /// Uses runtime behavior.
    /// </summary>
    Runtime = 0,
    /// <summary>
    /// Uses development behavior.
    /// </summary>
    Development = 1
}


