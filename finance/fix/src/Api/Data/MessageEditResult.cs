namespace fix;

/// <summary>Contains the explicit outcome of a mutable message operation.</summary>
public class MessageEditResult
{
    /// <summary>Whether the requested edit completed successfully.</summary>
    public bool Success;
    /// <summary>The edited message.</summary>
    public FixMessage? Message;
    /// <summary>The tag directly affected by the edit, or zero when no tag applies.</summary>
    public int AffectedTag;
    /// <summary>A stable machine-readable error code.</summary>
    public string ErrorCode = "";
    /// <summary>A human-readable error description.</summary>
    public string ErrorMessage = "";
}
