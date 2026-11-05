namespace uri;

/// <summary>
/// Describes the exception-less outcome of expanding a URI Template.
/// </summary>
public class UriTemplateExpansionResult
{
    /// <summary>Whether every expression expanded successfully.</summary>
    public bool Success;

    /// <summary>The complete or RFC-defined partial expansion output.</summary>
    public string Text;

    /// <summary>The number of UTF-16 code units in <see cref="Text"/>.</summary>
    public int Length;

    /// <summary>The first fatal diagnostic, or <c>null</c> after success.</summary>
    public UriDiagnostic? Diagnostic;

    /// <summary>Initializes a URI Template expansion result.</summary>
    /// <param name="success">Whether expansion succeeded.</param>
    /// <param name="text">The complete or partial output.</param>
    /// <param name="length">The output length.</param>
    /// <param name="diagnostic">The first fatal diagnostic.</param>
    public UriTemplateExpansionResult(
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
