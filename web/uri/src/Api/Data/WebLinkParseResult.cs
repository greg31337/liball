namespace uri;

/// <summary>
/// Describes the exception-less outcome of parsing Web Link field values.
/// </summary>
public class WebLinkParseResult
{
    /// <summary>Whether parsing produced the requested complete outcome.</summary>
    public bool Success;

    /// <summary>The ordered parsed links, or an empty set after strict failure.</summary>
    public WebLinkSet LinkSet;

    /// <summary>Whether advisory recovery discarded or repaired input.</summary>
    public bool Recovered;

    /// <summary>The first fatal diagnostic, or <c>null</c> after success.</summary>
    public UriDiagnostic? Diagnostic;

    /// <summary>All fatal and non-fatal diagnostics in source order.</summary>
    public UriDiagnostic[] Diagnostics;

    /// <summary>Initializes a Web Link parsing result.</summary>
    /// <param name="success">Whether the requested parsing operation succeeded.</param>
    /// <param name="linkSet">The parsed Web Link set.</param>
    /// <param name="recovered">Whether recovery discarded or repaired input.</param>
    /// <param name="diagnostic">The first fatal diagnostic.</param>
    /// <param name="diagnostics">All diagnostics in source order.</param>
    public WebLinkParseResult(
        bool success,
        WebLinkSet linkSet,
        bool recovered,
        UriDiagnostic? diagnostic,
        UriDiagnostic[] diagnostics)
    {
        Success = success;
        LinkSet = linkSet;
        Recovered = recovered;
        Diagnostic = diagnostic;
        Diagnostics = diagnostics;
    }
}
