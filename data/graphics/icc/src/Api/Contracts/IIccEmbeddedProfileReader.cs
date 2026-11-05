namespace icc;

/// <summary>
/// Detects carriers and extracts bounded embedded ICC profiles from byte, memory, and stream inputs.
/// </summary>
public interface IIccEmbeddedProfileReader
{
    /// <summary>
    /// Detects the carrier format of encoded bytes.
    /// </summary>
    /// <param name="carrierBytes">The carrier bytes.</param>
    /// <returns>The detected carrier format.</returns>
    IccEmbeddedFormat DetectCarrierBytes(System.ReadOnlySpan<byte> carrierBytes);

    /// <summary>
    /// Detects the carrier format of encoded memory.
    /// </summary>
    /// <param name="carrierMemory">The carrier memory.</param>
    /// <returns>The detected carrier format.</returns>
    IccEmbeddedFormat DetectCarrierMemory(System.ReadOnlyMemory<byte> carrierMemory);

    /// <summary>
    /// Detects the carrier format from the current stream position.
    /// </summary>
    /// <param name="carrierStream">The readable carrier stream.</param>
    /// <returns>The detected carrier format.</returns>
    IccEmbeddedFormat DetectCarrierStream(System.IO.Stream carrierStream);

    /// <summary>
    /// Enumerates bounded profiles and their carrier locations without throwing for expected discovery failures.
    /// </summary>
    /// <param name="carrierBytes">The carrier bytes.</param>
    /// <param name="options">The carrier discovery options.</param>
    /// <returns>The discovery result and located profiles.</returns>
    IccEmbeddedProfileResult EnumerateProfilesBytes(System.ReadOnlySpan<byte> carrierBytes,
        IccEmbeddingOptions options);

    /// <summary>
    /// Enumerates bounded profiles and their carrier locations from memory.
    /// </summary>
    /// <param name="carrierMemory">The carrier memory.</param>
    /// <param name="options">The carrier discovery options.</param>
    /// <returns>The discovery result and located profiles.</returns>
    IccEmbeddedProfileResult EnumerateProfilesMemory(System.ReadOnlyMemory<byte> carrierMemory,
        IccEmbeddingOptions options);

    /// <summary>
    /// Enumerates bounded profiles and their carrier locations from the current stream position.
    /// </summary>
    /// <param name="carrierStream">The readable carrier stream.</param>
    /// <param name="options">The carrier discovery options.</param>
    /// <returns>The discovery result and located profiles.</returns>
    IccEmbeddedProfileResult EnumerateProfilesStream(System.IO.Stream carrierStream,
        IccEmbeddingOptions options);

    /// <summary>
    /// Extracts every readable embedded profile from carrier bytes.
    /// </summary>
    /// <param name="carrierBytes">The carrier bytes.</param>
    /// <param name="options">The profile reading and resource-limit options.</param>
    /// <returns>The extracted profiles in carrier order.</returns>
    /// <exception cref="IccEmbeddingException">The carrier structure is malformed.</exception>
    IccProfile[] ExtractProfilesBytes(System.ReadOnlySpan<byte> carrierBytes, IccReadOptions options);

    /// <summary>
    /// Extracts every readable embedded profile from carrier memory.
    /// </summary>
    /// <param name="carrierMemory">The carrier memory.</param>
    /// <param name="options">The profile reading and resource-limit options.</param>
    /// <returns>The extracted profiles in carrier order.</returns>
    /// <exception cref="IccEmbeddingException">The carrier structure is malformed.</exception>
    IccProfile[] ExtractProfilesMemory(System.ReadOnlyMemory<byte> carrierMemory, IccReadOptions options);

    /// <summary>
    /// Extracts every readable embedded profile from the current stream position.
    /// </summary>
    /// <param name="carrierStream">The readable carrier stream.</param>
    /// <param name="options">The profile reading and resource-limit options.</param>
    /// <returns>The extracted profiles in carrier order.</returns>
    /// <exception cref="IccEmbeddingException">The carrier structure is malformed.</exception>
    IccProfile[] ExtractProfilesStream(System.IO.Stream carrierStream, IccReadOptions options);

    /// <summary>
    /// Probes carrier bytes for embedded profiles without throwing for expected detection or support failures.
    /// </summary>
    /// <param name="carrierBytes">The carrier bytes to probe.</param>
    /// <param name="limits">The resource limits applied to the probe.</param>
    /// <returns>The probe outcome.</returns>
    IccOperationResult ProbeEmbeddedProfiles(System.ReadOnlySpan<byte> carrierBytes, IccResourceLimits limits);
}
