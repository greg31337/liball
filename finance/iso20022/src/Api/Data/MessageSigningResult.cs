using System;
using System.Collections.Generic;

namespace iso20022;

/// <summary>Contains the exception-less outcome and signed XML from one signing operation.</summary>
public class MessageSigningResult
{
    /// <summary>Whether signing completed and produced output.</summary>
    public bool Success;
    /// <summary>The signing operation outcome.</summary>
    public MessageSigningStatus Status;
    /// <summary>The signed XML bytes when signing succeeded.</summary>
    public byte[] Xml = Array.Empty<byte>();
    /// <summary>The generated signature metadata and post-signing verification outcome.</summary>
    public XmlSignatureResult? Signature;
    /// <summary>The ordered located signing diagnostics.</summary>
    public List<ValidationDiagnostic> Diagnostics = new List<ValidationDiagnostic>();
    /// <summary>A stable operation error code when signing failed.</summary>
    public string ErrorCode = string.Empty;
    /// <summary>A human-readable operation error message when signing failed.</summary>
    public string ErrorMessage = string.Empty;
}
