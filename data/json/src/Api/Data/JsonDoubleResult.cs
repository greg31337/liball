namespace json;

/// <summary>Contains an exception-less finite double-precision conversion outcome.</summary>
public class JsonDoubleResult
{
    /// <summary>Indicates whether conversion succeeded.</summary>
    public bool Success;

    /// <summary>The converted finite double-precision value.</summary>
    public double Value;

    /// <summary>The diagnostic when conversion failed.</summary>
    public JsonDiagnostic? Diagnostic;
}
