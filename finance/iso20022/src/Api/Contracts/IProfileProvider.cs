namespace iso20022;

/// <summary>Finds caller-supplied validation profiles by stable identifier and optional exact version.</summary>
public interface IProfileProvider
{
    /// <summary>Finds one exact profile version or the latest registered version when no version is supplied.</summary>
    /// <param name="identifier">The stable profile identifier.</param>
    /// <param name="version">The exact profile version, or an empty value for the latest registered version.</param>
    /// <returns>The selected profile, or <c>null</c> when no profile matches.</returns>
    ProfileDefinition? FindProfile(string? identifier, string? version);
}
