namespace rtp;

/// <summary>
/// Describes the lifecycle state of an RTP source.
/// </summary>
public enum RtpSourceStatus
{
    /// <summary>The source is awaiting sequence validation.</summary>
    Probationary = 0,

    /// <summary>The source is an active session member.</summary>
    ActiveMember = 1,

    /// <summary>The source is an active RTP sender.</summary>
    ActiveSender = 2,

    /// <summary>The source is inactive.</summary>
    Inactive = 3,

    /// <summary>The source has sent or received a goodbye indication.</summary>
    Goodbye = 4,

    /// <summary>The source is involved in an SSRC collision.</summary>
    Collision = 5
}
