namespace json;

/// <summary>Represents a JSON Pointer parsing or evaluation failure.</summary>
public class JsonPointerException : global::System.Exception
{
    /// <summary>The structured diagnostic associated with the failure.</summary>
    public JsonDiagnostic Diagnostic;

    /// <summary>Initializes an exception from a structured JSON diagnostic.</summary>
    /// <param name="diagnostic">The diagnostic to preserve.</param>
    public JsonPointerException(JsonDiagnostic diagnostic) : base(diagnostic.Message)
    {
        Diagnostic = diagnostic;
    }
}
