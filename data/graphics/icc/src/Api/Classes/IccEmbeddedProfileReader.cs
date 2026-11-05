namespace icc;

/// <summary>Detects carriers and extracts embedded ICC profiles.</summary>
public class IccEmbeddedProfileReader : IIccEmbeddedProfileReader
{
    internal EmbeddedProfileReaderCore Core;

    /// <summary>Initializes an embedded-profile reader.</summary>
    public IccEmbeddedProfileReader()
    {
        Core = new EmbeddedProfileReaderCore();
    }

    /// <summary>Detects a carrier from bytes.</summary>
    /// <param name="carrierBytes">The carrier bytes.</param>
    /// <returns>The detected format.</returns>
    public IccEmbeddedFormat DetectCarrierBytes(System.ReadOnlySpan<byte> carrierBytes)
    {
        return Core.DetectBytes(carrierBytes);
    }

    /// <summary>Detects a carrier from memory.</summary>
    /// <param name="carrierMemory">The carrier memory.</param>
    /// <returns>The detected format.</returns>
    public IccEmbeddedFormat DetectCarrierMemory(System.ReadOnlyMemory<byte> carrierMemory)
    {
        return Core.DetectMemory(carrierMemory);
    }

    /// <summary>Detects a carrier from a stream.</summary>
    /// <param name="carrierStream">The carrier stream.</param>
    /// <returns>The detected format.</returns>
    public IccEmbeddedFormat DetectCarrierStream(System.IO.Stream carrierStream)
    {
        return Core.DetectStream(carrierStream);
    }

    /// <summary>Enumerates profiles and locations in carrier bytes.</summary>
    /// <param name="carrierBytes">The carrier bytes.</param>
    /// <param name="options">The discovery options.</param>
    /// <returns>The bounded discovery result.</returns>
    public IccEmbeddedProfileResult EnumerateProfilesBytes(
        System.ReadOnlySpan<byte> carrierBytes, IccEmbeddingOptions options)
    {
        return Core.EnumerateBytes(carrierBytes, options);
    }

    /// <summary>Enumerates profiles and locations in carrier memory.</summary>
    /// <param name="carrierMemory">The carrier memory.</param>
    /// <param name="options">The discovery options.</param>
    /// <returns>The bounded discovery result.</returns>
    public IccEmbeddedProfileResult EnumerateProfilesMemory(
        System.ReadOnlyMemory<byte> carrierMemory, IccEmbeddingOptions options)
    {
        return Core.EnumerateMemory(carrierMemory, options);
    }

    /// <summary>Enumerates profiles and locations from the current stream position.</summary>
    /// <param name="carrierStream">The readable carrier stream.</param>
    /// <param name="options">The discovery options.</param>
    /// <returns>The bounded discovery result.</returns>
    public IccEmbeddedProfileResult EnumerateProfilesStream(
        System.IO.Stream carrierStream, IccEmbeddingOptions options)
    {
        return Core.EnumerateStream(carrierStream, options);
    }

    /// <summary>Extracts profiles from carrier bytes.</summary>
    /// <param name="carrierBytes">The carrier bytes.</param>
    /// <param name="options">The reading options.</param>
    /// <returns>The extracted profiles.</returns>
    public IccProfile[] ExtractProfilesBytes(System.ReadOnlySpan<byte> carrierBytes, IccReadOptions options)
    {
        return Core.ExtractBytes(carrierBytes, options);
    }

    /// <summary>Extracts profiles from carrier memory.</summary>
    /// <param name="carrierMemory">The carrier memory.</param>
    /// <param name="options">The reading options.</param>
    /// <returns>The extracted profiles.</returns>
    public IccProfile[] ExtractProfilesMemory(System.ReadOnlyMemory<byte> carrierMemory, IccReadOptions options)
    {
        return Core.ExtractMemory(carrierMemory, options);
    }

    /// <summary>Extracts profiles from a carrier stream.</summary>
    /// <param name="carrierStream">The carrier stream.</param>
    /// <param name="options">The reading options.</param>
    /// <returns>The extracted profiles.</returns>
    public IccProfile[] ExtractProfilesStream(System.IO.Stream carrierStream, IccReadOptions options)
    {
        return Core.ExtractStream(carrierStream, options);
    }

    /// <summary>Probes a carrier for profiles.</summary>
    /// <param name="carrierBytes">The carrier bytes.</param>
    /// <param name="limits">The resource limits.</param>
    /// <returns>The probe result.</returns>
    public IccOperationResult ProbeEmbeddedProfiles(System.ReadOnlySpan<byte> carrierBytes, IccResourceLimits limits)
    {
        return Core.ProbeBytes(carrierBytes, limits);
    }
}
