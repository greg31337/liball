namespace uri;

/// <summary>
/// Describes the exception-less outcome of composing URI text.
/// </summary>
public class UriTextResult
{
    /// <summary>Whether the requested text operation succeeded.</summary>
    public bool Success;

    /// <summary>The composed text, or an empty string after failure.</summary>
    public string Text;

    /// <summary>The output length, or zero after failure.</summary>
    public int Length;

    /// <summary>The failure diagnostic, or <c>null</c> after success.</summary>
    public UriDiagnostic? Diagnostic;

    /// <summary>
    /// Initializes a URI text result.
    /// </summary>
    /// <param name="success">Whether the operation succeeded.</param>
    /// <param name="text">The composed text outcome.</param>
    /// <param name="length">The output length.</param>
    /// <param name="diagnostic">The failure diagnostic.</param>
    public UriTextResult(
        bool success,
        string text,
        int length,
        UriDiagnostic? diagnostic)
    {
        Success = success;
        Text = text;
        Length = length;
        Diagnostic = diagnostic;
    }
}
