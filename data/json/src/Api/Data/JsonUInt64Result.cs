namespace json;

/// <summary>Contains an exception-less unsigned 64-bit number conversion outcome.</summary>
public class JsonUInt64Result
{
    /// <summary>Indicates whether conversion succeeded.</summary>
    public bool Success;

    /// <summary>The converted unsigned 64-bit value.</summary>
    public ulong Value;

    /// <summary>The diagnostic when conversion failed.</summary>
    public JsonDiagnostic? Diagnostic;
}
