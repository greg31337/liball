namespace json;

/// <summary>Represents a JSON output or writer-state failure.</summary>
public class JsonWriteException : global::System.Exception
{
    /// <summary>The structured diagnostic associated with the failure.</summary>
    public JsonDiagnostic Diagnostic;

    /// <summary>Initializes an exception from a structured JSON diagnostic.</summary>
    /// <param name="diagnostic">The diagnostic to preserve.</param>
    public JsonWriteException(JsonDiagnostic diagnostic) : base(diagnostic.Message)
    {
        Diagnostic = diagnostic;
    }
}
