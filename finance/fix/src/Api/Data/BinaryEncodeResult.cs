using System.Collections.Generic;

namespace fix;

/// <summary>Contains the explicit outcome of external binary encoding.</summary>
public class BinaryEncodeResult
{
    /// <summary>The encode status.</summary>
    public EncodeStatus Status = EncodeStatus.Invalid;
    /// <summary>The effective encoding, schema, template, version, and dictionary metadata used by the codec.</summary>
    public BinaryMessageContext Context = new BinaryMessageContext();
    /// <summary>The number of destination bytes written.</summary>
    public int BytesWritten;
    /// <summary>The required destination size.</summary>
    public int RequiredSize;
    /// <summary>The ordered codec diagnostics.</summary>
    public List<ValidationIssue> Diagnostics = [];
    /// <summary>A stable machine-readable error code.</summary>
    public string ErrorCode = "";
    /// <summary>A human-readable error description.</summary>
    public string ErrorMessage = "";
}
