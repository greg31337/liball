namespace json;

/// <summary>Represents a JSON input parsing failure.</summary>
public class JsonParseException : global::System.Exception
{
    /// <summary>The structured diagnostic associated with the failure.</summary>
    public JsonDiagnostic Diagnostic;

    /// <summary>Initializes an exception from a structured JSON diagnostic.</summary>
    /// <param name="diagnostic">The diagnostic to preserve.</param>
    public JsonParseException(JsonDiagnostic diagnostic) : base(diagnostic.Message)
    {
        Diagnostic = diagnostic;
    }
}
