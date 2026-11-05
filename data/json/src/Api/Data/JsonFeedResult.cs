namespace json;

/// <summary>Contains the outcome of an incremental input or output operation.</summary>
public class JsonFeedResult
{
    /// <summary>The number of input bytes accepted from the current call.</summary>
    public int BytesAccepted;

    /// <summary>The number of accepted input bytes consumed by the current call.</summary>
    public int BytesConsumed;

    /// <summary>The number of output bytes written by the current call.</summary>
    public int BytesWritten;

    /// <summary>The current read state.</summary>
    public JsonReadStatus ReadStatus;

    /// <summary>The current write state.</summary>
    public JsonWriteStatus WriteStatus;

    /// <summary>The diagnostic when the operation failed.</summary>
    public JsonDiagnostic? Diagnostic;
}
