namespace config;

/// <summary>Defines substitution lookup and local-include resolution policies.</summary>
public class ConfigResolutionOptions
{
    /// <summary>Controls configuration-path substitution lookup.</summary>
    public ConfigSubstitutionLookupMode ConfigPathLookup;
    /// <summary>Controls environment-variable fallback for substitutions.</summary>
    public ConfigSubstitutionLookupMode EnvironmentFallback;
    /// <summary>Controls diagnostics for missing required substitutions.</summary>
    public ConfigSubstitutionMissingMode MissingSubstitution;
    /// <summary>Limits nested substitution references.</summary>
    public int SubstitutionDepthLimit;
    /// <summary>Controls whether missing local include files are required or optional.</summary>
    public ConfigOptional MissingIncludes;
    /// <summary>Limits recursively nested local includes.</summary>
    public int IncludeDepthLimit;

    /// <summary>Creates substitution and include resolution options.</summary>
    /// <param name="configPathLookup">Whether substitutions consult configuration paths.</param>
    /// <param name="environmentFallback">Whether missing configuration paths fall back to environment variables.</param>
    /// <param name="missingSubstitution">How missing required substitutions are diagnosed.</param>
    /// <param name="substitutionDepthLimit">The maximum nested substitution depth.</param>
    /// <param name="missingIncludes">Whether missing local include files are required or optional.</param>
    /// <param name="includeDepthLimit">The maximum nested local-include depth.</param>
    public ConfigResolutionOptions(ConfigSubstitutionLookupMode configPathLookup, ConfigSubstitutionLookupMode environmentFallback, ConfigSubstitutionMissingMode missingSubstitution, int substitutionDepthLimit, ConfigOptional missingIncludes, int includeDepthLimit)
    {
        ConfigPathLookup = configPathLookup;
        EnvironmentFallback = environmentFallback;
        MissingSubstitution = missingSubstitution;
        SubstitutionDepthLimit = substitutionDepthLimit;
        MissingIncludes = missingIncludes;
        IncludeDepthLimit = includeDepthLimit;
    }

    internal ConfigResolutionOptions Copy()
    {
        return new ConfigResolutionOptions(ConfigPathLookup, EnvironmentFallback, MissingSubstitution, SubstitutionDepthLimit, MissingIncludes, IncludeDepthLimit);
    }

    internal static ConfigResolutionOptions DefaultOptions()
    {
        return new ConfigResolutionOptions(ConfigSubstitutionLookupMode.Enabled, ConfigSubstitutionLookupMode.Enabled, ConfigSubstitutionMissingMode.Empty, 32, ConfigOptional.Required, 32);
    }
}
