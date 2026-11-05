namespace json;

/// <summary>Contains a pull-read status, token, or diagnostic.</summary>
public class JsonTokenResult
{
    /// <summary>The current read state.</summary>
    public JsonReadStatus Status;

    /// <summary>The available token.</summary>
    public JsonToken? Token;

    /// <summary>The diagnostic when reading failed.</summary>
    public JsonDiagnostic? Diagnostic;
}
