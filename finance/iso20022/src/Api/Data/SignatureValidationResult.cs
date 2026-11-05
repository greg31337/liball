using System.Collections.Generic;

namespace iso20022;

/// <summary>Contains the exception-less XML signature discovery and verification outcome.</summary>
public class SignatureValidationResult
{
    /// <summary>Whether signature processing completed under the selected configuration and limits.</summary>
    public bool Success;
    /// <summary>Whether the selected signature requirement and every evaluated signature passed.</summary>
    public bool Valid;
    /// <summary>The aggregate signature validation outcome.</summary>
    public SignatureValidationStatus Status;
    /// <summary>The discovered signatures in document order.</summary>
    public List<XmlSignatureResult> Signatures = new List<XmlSignatureResult>();
    /// <summary>The ordered located signature diagnostics.</summary>
    public List<ValidationDiagnostic> Diagnostics = new List<ValidationDiagnostic>();
    /// <summary>A stable operation error code when signature processing could not complete.</summary>
    public string ErrorCode = string.Empty;
    /// <summary>A human-readable operation error message when signature processing could not complete.</summary>
    public string ErrorMessage = string.Empty;
}
