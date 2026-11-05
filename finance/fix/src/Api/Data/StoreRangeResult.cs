using System.Collections.Generic;

namespace fix;

/// <summary>Contains an ordered durable sequence range and store diagnostics.</summary>
public class StoreRangeResult
{
    /// <summary>Whether the store operation completed successfully.</summary>
    public bool Success;
    /// <summary>The inclusive first requested sequence number.</summary>
    public int BeginSeqNo;
    /// <summary>The inclusive final requested sequence number.</summary>
    public int EndSeqNo;
    /// <summary>The records in ascending sequence order.</summary>
    public List<StoredMessage> Messages = [];
    /// <summary>Whether at least one requested sequence number was unavailable.</summary>
    public bool HasGap;
    /// <summary>Whether corrupt durable data was detected.</summary>
    public bool IsCorrupt;
    /// <summary>Whether the result contains current sequence state.</summary>
    public bool HasSequenceState;
    /// <summary>The next sender sequence number when sequence state is available.</summary>
    public int NextSenderSeqNum = FixDefaults.InitialSequenceNumber;
    /// <summary>The next target sequence number when sequence state is available.</summary>
    public int NextTargetSeqNum = FixDefaults.InitialSequenceNumber;
    /// <summary>A stable machine-readable outcome code.</summary>
    public string Code = "";
    /// <summary>A human-readable outcome description.</summary>
    public string Text = "";
}
