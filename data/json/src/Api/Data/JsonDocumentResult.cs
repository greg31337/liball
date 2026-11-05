namespace json;

/// <summary>Contains a document parse outcome.</summary>
public class JsonDocumentResult
{
    /// <summary>Indicates whether parsing succeeded.</summary>
    public bool Success;

    /// <summary>The terminal read state.</summary>
    public JsonReadStatus Status;

    /// <summary>The parsed document.</summary>
    public JsonDocument? Document;

    /// <summary>The diagnostic when parsing failed.</summary>
    public JsonDiagnostic? Diagnostic;
}
