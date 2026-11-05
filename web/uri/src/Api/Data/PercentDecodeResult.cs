namespace uri;

/// <summary>
/// Describes the exception-less outcome of decoding percent-encoded data.
/// </summary>
public class PercentDecodeResult
{
    /// <summary>Whether the requested decoding operation succeeded.</summary>
    public bool Success;

    /// <summary>The decoded bytes, or an empty array when bytes were not requested or decoding failed.</summary>
    public byte[] Bytes;

    /// <summary>The decoded text, or an empty string when text was not requested or decoding failed.</summary>
    public string Text;

    /// <summary>Whether invalid UTF-8 was replaced while producing text.</summary>
    public bool UsedReplacement;

    /// <summary>The failure diagnostic, or <c>null</c> after successful decoding.</summary>
    public UriDiagnostic? Diagnostic;

    /// <summary>
    /// Initializes a percent-decoding result.
    /// </summary>
    /// <param name="success">Whether decoding succeeded.</param>
    /// <param name="bytes">The decoded byte outcome.</param>
    /// <param name="text">The decoded text outcome.</param>
    /// <param name="usedReplacement">Whether invalid UTF-8 was replaced.</param>
    /// <param name="diagnostic">The failure diagnostic.</param>
    public PercentDecodeResult(
        bool success,
        byte[] bytes,
        string text,
        bool usedReplacement,
        UriDiagnostic? diagnostic)
    {
        Success = success;
        Bytes = bytes;
        Text = text;
        UsedReplacement = usedReplacement;
        Diagnostic = diagnostic;
    }
}
