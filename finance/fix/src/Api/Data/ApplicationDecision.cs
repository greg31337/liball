namespace fix;

/// <summary>Contains an application's explicit message-processing decision.</summary>
public class ApplicationDecision
{
    /// <summary>The requested disposition.</summary>
    public MessageDisposition Disposition;
    /// <summary>A stable machine-readable decision code.</summary>
    public string Code = "";
    /// <summary>Text suitable for a reject, logout, or audit event.</summary>
    public string Text = "";
}
