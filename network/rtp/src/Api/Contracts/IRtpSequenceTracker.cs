namespace rtp;

/// <summary>
/// Defines RTP sequence validation, extension, and sender advancement operations.
/// </summary>
public interface IRtpSequenceTracker
{
    /// <summary>Processes one received RTP sequence number.</summary>
    /// <param name="sequenceNumber">The 16-bit sequence number to process.</param>
    /// <returns>The resulting sequence state update.</returns>
    RtpSequenceUpdate ProcessRtpSequenceNumber(ushort sequenceNumber);

    /// <summary>Advances and returns the next outbound RTP sequence number.</summary>
    /// <returns>The next 16-bit sequence number.</returns>
    ushort AdvanceRtpSequenceNumber();

    /// <summary>Returns an independent snapshot of the current sequence state.</summary>
    /// <returns>The current sequence state.</returns>
    RtpSequenceUpdate GetRtpSequenceState();

    /// <summary>Restarts receive and outbound sequence tracking at a supplied value.</summary>
    /// <param name="sequenceNumber">The receive candidate and next outbound value.</param>
    /// <returns>The reset sequence state.</returns>
    RtpSequenceUpdate ResetRtpSequenceTracker(ushort sequenceNumber);
}
