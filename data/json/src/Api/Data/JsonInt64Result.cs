namespace json;

/// <summary>Contains an exception-less signed 64-bit number conversion outcome.</summary>
public class JsonInt64Result
{
    /// <summary>Indicates whether conversion succeeded.</summary>
    public bool Success;

    /// <summary>The converted signed 64-bit value.</summary>
    public long Value;

    /// <summary>The diagnostic when conversion failed.</summary>
    public JsonDiagnostic? Diagnostic;
}
