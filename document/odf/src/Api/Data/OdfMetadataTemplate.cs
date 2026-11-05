namespace odf;

/// <summary>
/// Describes the template from which a document was created.
/// </summary>
public class OdfMetadataTemplate
{
    /// <summary>Whether template metadata is present.</summary>
    public bool IsPresent;
    /// <summary>The template IRI.</summary>
    public string Href = "";
    /// <summary>The XLink type token.</summary>
    public string XLinkType = "";
    /// <summary>The XLink actuate token.</summary>
    public string Actuate = "";
    /// <summary>The template title.</summary>
    public string Title = "";
    /// <summary>The preserved template date or date-time.</summary>
    public string Date = "";
    /// <summary>Whether the template date has a valid XML Schema lexical form.</summary>
    public bool IsDateValid;
    internal OdfNode? Node;
}
