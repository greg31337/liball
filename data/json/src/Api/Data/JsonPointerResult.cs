namespace json;

/// <summary>Contains a JSON Pointer evaluation outcome.</summary>
public class JsonPointerResult
{
    /// <summary>Indicates whether evaluation succeeded.</summary>
    public bool Success;

    /// <summary>The first matching value.</summary>
    public JsonValue? Value;

    /// <summary>All matching values when all-duplicate resolution was requested.</summary>
    public global::System.Collections.Generic.List<JsonValue>? Values;

    /// <summary>The zero-based failed reference-token index, or -1 when no segment failed.</summary>
    public int FailedSegmentIndex = -1;

    /// <summary>The diagnostic when evaluation failed.</summary>
    public JsonDiagnostic? Diagnostic;
}
