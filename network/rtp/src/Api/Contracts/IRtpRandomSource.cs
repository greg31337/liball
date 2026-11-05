namespace rtp;

/// <summary>
/// Defines random values used for RTP identifiers and RTCP interval dithering.
/// </summary>
public interface IRtpRandomSource
{
    /// <summary>Creates a randomized RTP synchronization source identifier.</summary>
    /// <returns>The new SSRC value.</returns>
    uint CreateSynchronizationSource();

    /// <summary>Creates a randomized initial RTP sequence number.</summary>
    /// <returns>The new sequence number.</returns>
    ushort CreateSequenceNumber();

    /// <summary>Creates a randomized initial RTP timestamp.</summary>
    /// <returns>The new timestamp.</returns>
    uint CreateTimestamp();

    /// <summary>Creates a uniformly distributed value from zero through one.</summary>
    /// <returns>The unit-interval random value.</returns>
    double CreateUnitIntervalValue();
}
