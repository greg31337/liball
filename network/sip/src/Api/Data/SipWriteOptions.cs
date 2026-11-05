namespace sip;

/// <summary>
/// Holds formatting choices and limits for codec write operations.
/// </summary>
public class SipWriteOptions
{
    /// <summary>Writes canonical protocol text when true.</summary>
    public bool Canonical = true;

    /// <summary>Reuses preserved original forms when possible.</summary>
    public bool PreserveOriginal;

    /// <summary>Uses registered compact header names when true.</summary>
    public bool CompactHeaderNames;

    /// <summary>Permits controlled header folding when true.</summary>
    public bool AllowHeaderFolding;

    /// <summary>Reconciles Content-Length with the binary body when true.</summary>
    public bool ReconcileContentLength = true;

    /// <summary>The maximum output header line size in bytes.</summary>
    public int MaxHeaderLineBytes = SipDefaults.MaxHeaderLineBytes;
}
