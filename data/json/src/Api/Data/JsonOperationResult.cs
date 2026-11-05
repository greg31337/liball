namespace json;

/// <summary>Contains the outcome of a JSON operation.</summary>
public class JsonOperationResult
{
    /// <summary>Indicates whether the operation succeeded.</summary>
    public bool Success;

    /// <summary>The number of input bytes consumed by the operation.</summary>
    public long BytesConsumed;

    /// <summary>The number of output bytes written by the operation.</summary>
    public long BytesWritten;

    /// <summary>The diagnostic when the operation failed.</summary>
    public JsonDiagnostic? Diagnostic;
}
