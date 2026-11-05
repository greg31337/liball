namespace rtp;

/// <summary>
/// Defines lookup of signaling-supplied RTP header-extension mappings.
/// </summary>
public interface IRtpHeaderExtensionRegistry
{
    /// <summary>Adds or replaces a header-extension mapping.</summary>
    /// <param name="mapping">The mapping to validate and store independently.</param>
    void SetHeaderExtensionMapping(RtpHeaderExtensionMapping mapping);

    /// <summary>Finds a header-extension mapping by local identifier.</summary>
    /// <param name="identifier">The locally negotiated identifier.</param>
    /// <returns>The mapping copy, or <c>null</c> when no mapping exists.</returns>
    RtpHeaderExtensionMapping? FindHeaderExtensionMapping(int identifier);

    /// <summary>Finds a header-extension mapping by URI.</summary>
    /// <param name="uri">The registered header-extension URI.</param>
    /// <returns>The mapping copy, or <c>null</c> when no mapping exists.</returns>
    RtpHeaderExtensionMapping? FindHeaderExtensionMappingByUri(string uri);

    /// <summary>Removes a header-extension mapping.</summary>
    /// <param name="identifier">The locally negotiated identifier to remove.</param>
    /// <returns><c>true</c> when a mapping was removed.</returns>
    bool RemoveHeaderExtensionMapping(int identifier);

    /// <summary>Returns independent copies of all registered mappings.</summary>
    /// <returns>The registered mappings in identifier order.</returns>
    RtpHeaderExtensionMapping[] GetHeaderExtensionMappings();
}
