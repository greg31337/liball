namespace iso20022;

/// <summary>Identifies the message region included in checksum processing.</summary>
public enum ChecksumScope
{
    /// <summary>The checksum scope is absent or not recognized.</summary>
    Unknown = 0,
    /// <summary>The complete supplied message or envelope is included.</summary>
    WholeMessage = 1,
    /// <summary>Only the ISO 20022 message body is included.</summary>
    MessageBody = 2
}
