namespace json;

/// <summary>Represents a configured JSON resource limit failure.</summary>
public class JsonLimitException : global::System.Exception
{
    /// <summary>The structured diagnostic associated with the failure.</summary>
    public JsonDiagnostic Diagnostic;

    /// <summary>Initializes an exception from a structured JSON diagnostic.</summary>
    /// <param name="diagnostic">The diagnostic to preserve.</param>
    public JsonLimitException(JsonDiagnostic diagnostic) : base(diagnostic.Message)
    {
        Diagnostic = diagnostic;
    }
}
