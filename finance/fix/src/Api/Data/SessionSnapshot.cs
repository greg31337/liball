using System;

namespace fix;

/// <summary>Captures immutable-by-convention operational state for one session.</summary>
public class SessionSnapshot
{
    /// <summary>The session identity.</summary>
    public SessionId Id = new SessionId();
    /// <summary>The session state.</summary>
    public SessionState State;
    /// <summary>The transport state.</summary>
    public TransportState TransportState;
    /// <summary>Whether logon negotiation completed.</summary>
    public bool IsLoggedOn;
    /// <summary>The next sender sequence number.</summary>
    public int NextSenderSeqNum = FixDefaults.InitialSequenceNumber;
    /// <summary>The next target sequence number.</summary>
    public int NextTargetSeqNum = FixDefaults.InitialSequenceNumber;
    /// <summary>The pending inbound message count.</summary>
    public int InboundQueueCount;
    /// <summary>The pending outbound message count.</summary>
    public int OutboundQueueCount;
    /// <summary>Whether an inbound timestamp is available.</summary>
    public bool HasLastInboundTime;
    /// <summary>The last inbound activity time in UTC.</summary>
    public DateTime LastInboundTimeUtc;
    /// <summary>Whether an outbound timestamp is available.</summary>
    public bool HasLastOutboundTime;
    /// <summary>The last outbound activity time in UTC.</summary>
    public DateTime LastOutboundTimeUtc;
}
