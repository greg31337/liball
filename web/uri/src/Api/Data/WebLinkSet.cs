namespace uri;

/// <summary>
/// Stores ordered links from one or more combined Link field values.
/// </summary>
public class WebLinkSet
{
    /// <summary>The parsed links in field and link-value order.</summary>
    public WebLink[] Links;

    /// <summary>The original field values in input order.</summary>
    public string[] OriginalFieldValues;

    /// <summary>Non-fatal recovery and interpretation diagnostics in source order.</summary>
    public UriDiagnostic[] Diagnostics;

    /// <summary>Initializes an empty Web Link set.</summary>
    public WebLinkSet()
    {
        Links = new WebLink[0];
        OriginalFieldValues = new string[0];
        Diagnostics = new UriDiagnostic[0];
    }
}
