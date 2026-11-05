namespace json;

/// <summary>Contains an owned JSON root value and its parse-profile metadata.</summary>
public class JsonDocument
{
    /// <summary>The owned root value.</summary>
    public JsonValue? Root;

    /// <summary>The conformance profile used to parse or create the document.</summary>
    public JsonConformanceProfile ConformanceProfile;

    /// <summary>The duplicate-name policy used to materialize the document.</summary>
    public JsonDuplicateKeyPolicy DuplicateKeyPolicy;

    /// <summary>The Unicode policy used to parse or create the document.</summary>
    public JsonUnicodePolicy UnicodePolicy;

    /// <summary>The framing mode from which the document was read.</summary>
    public JsonInputMode InputMode;

    /// <summary>The byte order mark policy used while reading the document.</summary>
    public JsonBomPolicy BomPolicy;

    /// <summary>The number of source UTF-8 bytes consumed to create the document.</summary>
    public long SourceByteCount;
}
