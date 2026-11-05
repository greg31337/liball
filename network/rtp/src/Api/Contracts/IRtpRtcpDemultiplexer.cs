using System;

namespace rtp;

/// <summary>
/// Defines transport-neutral RTP and RTCP datagram classification.
/// </summary>
public interface IRtpRtcpDemultiplexer
{
    /// <summary>Classifies a datagram without guessing when input is ambiguous.</summary>
    /// <param name="datagram">The datagram bytes to classify.</param>
    /// <returns>The classification result.</returns>
    RtpDatagramClassification ClassifyDatagram(ReadOnlySpan<byte> datagram);
}
