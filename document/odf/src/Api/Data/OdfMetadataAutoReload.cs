namespace odf;

/// <summary>
/// Describes automatic document reload behavior.
/// </summary>
public class OdfMetadataAutoReload
{
    /// <summary>Whether automatic-reload metadata is present.</summary>
    public bool IsPresent;
    /// <summary>The document IRI to reload.</summary>
    public string Href = "";
    /// <summary>The XLink type token.</summary>
    public string XLinkType = "";
    /// <summary>The XLink show token.</summary>
    public string Show = "";
    /// <summary>The XLink actuate token.</summary>
    public string Actuate = "";
    /// <summary>The preserved reload delay duration.</summary>
    public string Delay = "";
    /// <summary>Whether the delay has a valid XML Schema duration lexical form.</summary>
    public bool IsDelayValid;
    internal OdfNode? Node;
}
