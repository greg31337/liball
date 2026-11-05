namespace rtp;

/// <summary>
/// Defines lookup of signaling-supplied RTP payload-type definitions.
/// </summary>
public interface IRtpPayloadTypeRegistry
{
    /// <summary>Adds or replaces a payload-type definition.</summary>
    /// <param name="definition">The definition to validate and store independently.</param>
    void SetPayloadTypeDefinition(RtpPayloadTypeDefinition definition);

    /// <summary>Finds a payload-type definition.</summary>
    /// <param name="payloadType">The RTP payload type.</param>
    /// <returns>The definition copy, or <c>null</c> when no definition exists.</returns>
    RtpPayloadTypeDefinition? FindPayloadTypeDefinition(byte payloadType);

    /// <summary>Removes a payload-type definition.</summary>
    /// <param name="payloadType">The RTP payload type to remove.</param>
    /// <returns><c>true</c> when a definition was removed.</returns>
    bool RemovePayloadTypeDefinition(byte payloadType);

    /// <summary>Returns independent copies of all registered payload definitions.</summary>
    /// <returns>The registered definitions in payload-type order.</returns>
    RtpPayloadTypeDefinition[] GetPayloadTypeDefinitions();
}
