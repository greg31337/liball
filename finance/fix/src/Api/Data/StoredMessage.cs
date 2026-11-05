using System;

namespace fix;

/// <summary>Represents one durable outbound message record.</summary>
public class StoredMessage
{
    /// <summary>The sender sequence number.</summary>
    public int SequenceNumber;
    /// <summary>The original sending time in UTC.</summary>
    public DateTime SendingTimeUtc;
    /// <summary>The exact durable message bytes.</summary>
    public byte[] Bytes = [];
    /// <summary>The decoded MsgType retained for replay policy.</summary>
    public string MsgType = "";
    /// <summary>Whether the record is an administrative message.</summary>
    public bool IsAdministrative;
}
