namespace uri;

/// <summary>
/// Describes the exception-less outcome of parsing a URI Template.
/// </summary>
public class UriTemplateParseResult
{
    /// <summary>Whether the complete input is a valid URI Template.</summary>
    public bool Success;

    /// <summary>The parsed template, or an empty template after failure.</summary>
    public UriTemplate Template;

    /// <summary>The first fatal diagnostic, or <c>null</c> after success.</summary>
    public UriDiagnostic? Diagnostic;

    /// <summary>Initializes a URI Template parsing result.</summary>
    /// <param name="success">Whether parsing succeeded.</param>
    /// <param name="template">The parsed template outcome.</param>
    /// <param name="diagnostic">The first fatal diagnostic.</param>
    public UriTemplateParseResult(
        bool success,
        UriTemplate template,
        UriDiagnostic? diagnostic)
    {
        Success = success;
        Template = template;
        Diagnostic = diagnostic;
    }
}
