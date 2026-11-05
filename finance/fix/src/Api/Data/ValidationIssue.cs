namespace fix;

/// <summary>Describes one stable, actionable FIX validation issue.</summary>
public class ValidationIssue
{
    /// <summary>The issue severity.</summary>
    public ValidationSeverity Severity;
    /// <summary>The validation layer that produced the issue.</summary>
    public ValidationLayer Layer;
    /// <summary>A stable machine-readable issue code.</summary>
    public string Code = "";
    /// <summary>A human-readable issue description.</summary>
    public string Message = "";
    /// <summary>Whether the issue identifies a field tag.</summary>
    public bool HasTag;
    /// <summary>The affected field tag.</summary>
    public int Tag;
    /// <summary>The nested repeating-group path.</summary>
    public string GroupPath = "";
    /// <summary>The affected MsgType.</summary>
    public string MsgType = "";
    /// <summary>Whether a FIX RefTagID mapping is available.</summary>
    public bool HasRefTagId;
    /// <summary>The mapped FIX RefTagID value.</summary>
    public int RefTagId;
    /// <summary>The mapped FIX RefMsgType value, or an empty string when unavailable.</summary>
    public string RefMsgType = "";
    /// <summary>Whether a FIX RefSeqNum mapping is available.</summary>
    public bool HasRefSeqNum;
    /// <summary>The mapped FIX RefSeqNum value.</summary>
    public int RefSeqNum;
    /// <summary>Whether the issue identifies a source offset.</summary>
    public bool HasSourceOffset;
    /// <summary>The affected byte offset.</summary>
    public int SourceOffset;
    /// <summary>Whether a FIX SessionRejectReason mapping is available.</summary>
    public bool HasSessionRejectReason;
    /// <summary>The mapped FIX SessionRejectReason value.</summary>
    public int SessionRejectReason;
    /// <summary>Whether a FIX BusinessRejectReason mapping is available.</summary>
    public bool HasBusinessRejectReason;
    /// <summary>The mapped FIX BusinessRejectReason value.</summary>
    public int BusinessRejectReason;
}
