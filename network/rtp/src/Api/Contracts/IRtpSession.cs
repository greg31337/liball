using System;

namespace rtp;

/// <summary>
/// Defines synchronous RTP session processing.
/// </summary>
public interface IRtpSession
{
    /// <summary>Processes one datagram received on a dedicated RTP transport.</summary>
    /// <param name="datagram">The received RTP datagram bytes.</param>
    /// <param name="context">The arrival and transport context.</param>
    /// <returns>The resulting session events and outbound work.</returns>
    RtpSessionUpdate ProcessInboundRtpDatagram(
        ReadOnlySpan<byte> datagram,
        RtpPacketContext context);

    /// <summary>Processes one datagram received on a dedicated RTCP transport.</summary>
    /// <param name="datagram">The received RTCP datagram bytes.</param>
    /// <param name="context">The arrival and transport context.</param>
    /// <returns>The resulting session events and outbound work.</returns>
    RtpSessionUpdate ProcessInboundRtcpDatagram(
        ReadOnlySpan<byte> datagram,
        RtpPacketContext context);

    /// <summary>Classifies and processes one RFC 5761 multiplexed datagram.</summary>
    /// <param name="datagram">The received multiplexed datagram bytes.</param>
    /// <param name="context">The arrival and transport context.</param>
    /// <returns>The resulting session events and outbound work.</returns>
    RtpSessionUpdate ProcessInboundMultiplexedDatagram(
        ReadOnlySpan<byte> datagram,
        RtpPacketContext context);

    /// <summary>Prepares one application RTP packet using current local source state.</summary>
    /// <param name="packet">The packet template and owned media payload.</param>
    /// <param name="context">The send-time and media-clock context.</param>
    /// <returns>The prepared packet and resulting session events.</returns>
    RtpSessionUpdate PrepareOutboundRtpPacket(
        RtpPacket packet,
        RtpPacketContext context);

    /// <summary>Queues one feedback request for deterministic scheduling.</summary>
    /// <param name="request">The feedback request.</param>
    /// <returns>The resulting feedback and session decisions.</returns>
    RtpSessionUpdate RequestSessionFeedback(RtpFeedbackRequest request);

    /// <summary>Applies packets and metadata returned by a repair mechanism.</summary>
    /// <param name="result">The repair result to process.</param>
    /// <param name="context">The repaired-packet timing and transport context.</param>
    /// <returns>The resulting packet releases and session events.</returns>
    RtpSessionUpdate ApplySessionRepairResult(
        RtpRepairResult result,
        RtpPacketContext context);

    /// <summary>Requests local goodbye processing and RTCP scheduling.</summary>
    /// <param name="reason">The optional UTF-8 goodbye reason bytes.</param>
    /// <returns>The resulting goodbye scheduling work.</returns>
    RtpSessionUpdate RequestSessionGoodbye(ReadOnlySpan<byte> reason);

    /// <summary>Flushes currently releasable buffered media and queued work.</summary>
    /// <returns>The released packets and queued session work.</returns>
    RtpSessionUpdate FlushSession();

    /// <summary>Atomically replaces configuration and resets dependent state.</summary>
    /// <param name="configuration">The replacement session configuration.</param>
    /// <returns>The configuration transition event and new snapshot.</returns>
    RtpSessionUpdate UpdateSessionConfiguration(
        RtpSessionConfiguration configuration);

    /// <summary>Polls the session for pending events and control work.</summary>
    /// <returns>The update produced by the poll.</returns>
    RtpSessionUpdate PollSession();

    /// <summary>Returns an independent snapshot of current session state.</summary>
    /// <returns>The current session snapshot.</returns>
    RtpSessionSnapshot GetSessionSnapshot();

    /// <summary>Resets all source, scheduling, buffering, and repair state.</summary>
    void ResetSession();
}
