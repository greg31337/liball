namespace json;

/// <summary>Contains a JSON value operation outcome.</summary>
public class JsonValueResult
{
    /// <summary>Indicates whether the requested value operation succeeded.</summary>
    public bool Success;

    /// <summary>The current read state for streaming value operations.</summary>
    public JsonReadStatus Status;

    /// <summary>The resulting JSON value.</summary>
    public JsonValue? Value;

    /// <summary>The diagnostic when the operation failed.</summary>
    public JsonDiagnostic? Diagnostic;
}
