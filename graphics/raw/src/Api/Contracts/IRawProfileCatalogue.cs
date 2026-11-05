namespace raw;

/// <summary>Defines the read-only profile lookup and matching operations consumed by decoders and writers.</summary>
/// <remarks>Registration remains a responsibility of <see cref="RawProfileCatalogue"/>. Consumers depend on this narrower contract so profile sources can be substituted without exposing mutation.</remarks>
public interface IRawProfileCatalogue
{
    /// <summary>Finds a unique registered read profile by exact stable identifier.</summary>
    /// <param name="id">The exact profile identifier.</param>
    /// <returns>The registered profile, or <c>null</c>.</returns>
    RawReadProfile? FindReadProfile(string id);

    /// <summary>Finds a unique registered write profile by exact stable identifier.</summary>
    /// <param name="id">The exact profile identifier.</param>
    /// <returns>The registered profile, or <c>null</c>.</returns>
    RawWriteProfile? FindWriteProfile(string id);

    /// <summary>Matches registered read profiles against observed identity and structural evidence.</summary>
    /// <param name="input">Observed identity and structural evidence.</param>
    /// <returns>The deterministic read-profile match result.</returns>
    RawProfileMatchResult MatchReadProfile(RawProfileMatchInput input);

    /// <summary>Matches separately qualified write profiles against requested identity and structural evidence.</summary>
    /// <param name="input">Requested identity and output structural evidence.</param>
    /// <returns>The deterministic write-profile match result.</returns>
    RawProfileMatchResult MatchWriteProfile(RawProfileMatchInput input);

    /// <summary>Builds the application and version compatibility matrix for registered writer profiles of one format.</summary>
    /// <param name="format">The defined output format to include.</param>
    /// <returns>Rows in profile and evidence order.</returns>
    RawWriteCompatibilityMatrix GetWriteCompatibilityMatrix(RawFormat format);

    /// <summary>Returns a stable snapshot of the registered read profiles.</summary>
    /// <returns>An independently replaceable profile array.</returns>
    RawReadProfile[] GetReadProfiles();

    /// <summary>Returns a stable snapshot of the registered write profiles.</summary>
    /// <returns>An independently replaceable profile array.</returns>
    RawWriteProfile[] GetWriteProfiles();
}
