namespace json;

/// <summary>Contains an exception-less decimal number conversion outcome.</summary>
public class JsonDecimalResult
{
    /// <summary>Indicates whether conversion succeeded.</summary>
    public bool Success;

    /// <summary>The converted decimal value.</summary>
    public decimal Value;

    /// <summary>The diagnostic when conversion failed.</summary>
    public JsonDiagnostic? Diagnostic;
}
