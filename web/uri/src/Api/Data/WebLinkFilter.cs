namespace uri;

/// <summary>
/// Defines optional criteria combined when filtering Web Links.
/// </summary>
public class WebLinkFilter
{
    /// <summary>Whether a relation criterion is active.</summary>
    public bool HasRelation;

    /// <summary>The relation type to match.</summary>
    public string Relation;

    /// <summary>Whether a target criterion is active.</summary>
    public bool HasTarget;

    /// <summary>The target URI-reference or resolved URI to match.</summary>
    public string Target;

    /// <summary>Whether a context criterion is active.</summary>
    public bool HasContext;

    /// <summary>The effective context URI to match.</summary>
    public string Context;

    /// <summary>Whether a media type criterion is active.</summary>
    public bool HasMediaType;

    /// <summary>The media type hint to match.</summary>
    public string MediaType;

    /// <summary>Whether a language criterion is active.</summary>
    public bool HasLanguage;

    /// <summary>The language hint to match.</summary>
    public string Language;

    /// <summary>Whether an extension attribute criterion is active.</summary>
    public bool HasExtensionAttribute;

    /// <summary>The extension attribute name to match.</summary>
    public string ExtensionAttributeName;

    /// <summary>Whether the extension attribute must have a value.</summary>
    public bool ExtensionAttributeHasValue;

    /// <summary>The extension attribute value to match when required.</summary>
    public string ExtensionAttributeValue;

    /// <summary>Initializes a filter with no active criteria.</summary>
    public WebLinkFilter()
    {
        HasRelation = false;
        Relation = "";
        HasTarget = false;
        Target = "";
        HasContext = false;
        Context = "";
        HasMediaType = false;
        MediaType = "";
        HasLanguage = false;
        Language = "";
        HasExtensionAttribute = false;
        ExtensionAttributeName = "";
        ExtensionAttributeHasValue = false;
        ExtensionAttributeValue = "";
    }
}
