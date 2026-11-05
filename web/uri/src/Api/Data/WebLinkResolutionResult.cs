namespace uri;

/// <summary>
/// Describes target and context resolution for one Web Link.
/// </summary>
public class WebLinkResolutionResult
{
    /// <summary>Whether target and required context resolution succeeded.</summary>
    public bool Success;

    /// <summary>The source Web Link.</summary>
    public WebLink Link;

    /// <summary>The resolved absolute target URI.</summary>
    public string Target;

    /// <summary>Whether a representation or honored anchor context is available.</summary>
    public bool HasContext;

    /// <summary>The resolved absolute context URI, or an empty string.</summary>
    public string Context;

    /// <summary>Whether an explicit anchor was absent or successfully honored.</summary>
    public bool AnchorHonored;

    /// <summary>The failure diagnostic, or <c>null</c> after success.</summary>
    public UriDiagnostic? Diagnostic;

    /// <summary>Initializes a Web Link resolution result.</summary>
    /// <param name="success">Whether resolution succeeded.</param>
    /// <param name="link">The source Web Link.</param>
    /// <param name="target">The resolved target URI.</param>
    /// <param name="hasContext">Whether a resolved context is available.</param>
    /// <param name="context">The resolved context URI.</param>
    /// <param name="anchorHonored">Whether an explicit anchor was honored.</param>
    /// <param name="diagnostic">The failure diagnostic.</param>
    public WebLinkResolutionResult(
        bool success,
        WebLink link,
        string target,
        bool hasContext,
        string context,
        bool anchorHonored,
        UriDiagnostic? diagnostic)
    {
        Success = success;
        Link = link;
        Target = target;
        HasContext = hasContext;
        Context = context;
        AnchorHonored = anchorHonored;
        Diagnostic = diagnostic;
    }
}
