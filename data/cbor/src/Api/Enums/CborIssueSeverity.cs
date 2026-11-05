namespace cbor;

/// <summary>
/// Identifies the severity of a CBOR validation issue.
/// </summary>
public enum CborIssueSeverity
{
    /// <summary>The issue is informational.</summary>
    Information = 0,
    /// <summary>The issue is noteworthy but does not invalidate the input.</summary>
    Warning = 1,
    /// <summary>The issue invalidates the input at the selected validation level.</summary>
    Error = 2
}
