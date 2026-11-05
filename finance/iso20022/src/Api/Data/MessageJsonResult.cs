using System.Collections.Generic;

namespace iso20022;

/// <summary>Contains the outcome of converting an ISO 20022 generic or generated typed model to or from JSON.</summary>
public class MessageJsonResult
{
    /// <summary>Whether conversion completed successfully.</summary>
    public bool Success;
    /// <summary>The JSON text produced by a write operation.</summary>
    public string Json = string.Empty;
    /// <summary>The reconstructed XML text produced by a read operation.</summary>
    public string Xml = string.Empty;
    /// <summary>The generic message document produced by a read operation.</summary>
    public MessageDocument? Document;
    /// <summary>The declared ISO 20022 JSON schema identifier.</summary>
    public string SchemaIdentifier = string.Empty;
    /// <summary>The exact payload namespace identity.</summary>
    public string NamespaceUri = string.Empty;
    /// <summary>Whether no XML information was knowingly lost during conversion.</summary>
    public bool IsLossless = true;
    /// <summary>The ordered non-fatal conversion diagnostics.</summary>
    public List<MessageJsonDiagnostic> Diagnostics = new List<MessageJsonDiagnostic>();
    /// <summary>The stable error code when conversion failed.</summary>
    public string ErrorCode = string.Empty;
    /// <summary>The human-readable error message when conversion failed.</summary>
    public string ErrorMessage = string.Empty;
}
