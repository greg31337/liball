namespace rtp;

/// <summary>
/// Describes the result of processing an RTP sequence number.
/// </summary>
public enum RtpSequenceStatus
{
    /// <summary>The source remains in sequence probation.</summary>
    Probation = 0,

    /// <summary>The sequence number was accepted in order.</summary>
    Accepted = 1,

    /// <summary>The sequence number identifies a duplicate packet.</summary>
    Duplicate = 2,

    /// <summary>The sequence number was accepted out of order.</summary>
    Reordered = 3,

    /// <summary>The sequence number represents an unconfirmed large jump.</summary>
    LargeJump = 4,

    /// <summary>The sequence state restarted after jump confirmation.</summary>
    Restarted = 5,

    /// <summary>The sequence number or tracker input was invalid.</summary>
    Invalid = 6
}
