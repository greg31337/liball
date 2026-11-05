namespace uri;

/// <summary>
/// Stores one Web Link value with ordered relations and target attributes.
/// </summary>
public class WebLink
{
    /// <summary>The target URI-reference as serialized inside angle brackets.</summary>
    public string Target;

    /// <summary>Whether an explicit anchor supplied the link context.</summary>
    public bool HasAnchor;

    /// <summary>The explicit anchor URI-reference, or an empty string.</summary>
    public string Anchor;

    /// <summary>The ordered forward relation types.</summary>
    public string[] Relations;

    /// <summary>The kind of each corresponding forward relation type.</summary>
    public WebLinkRelationKind[] RelationKinds;

    /// <summary>The ordered deprecated reverse relation types.</summary>
    public string[] ReverseRelations;

    /// <summary>The kind of each corresponding reverse relation type.</summary>
    public WebLinkRelationKind[] ReverseRelationKinds;

    /// <summary>All parameters in their original order, including ignored repeats.</summary>
    public WebLinkParameter[] Parameters;

    /// <summary>All accepted language hints in their original order.</summary>
    public string[] HrefLanguages;

    /// <summary>Whether an accepted media attribute is present.</summary>
    public bool HasMedia;

    /// <summary>The accepted media attribute value.</summary>
    public string Media;

    /// <summary>Whether an accepted title attribute is present.</summary>
    public bool HasTitle;

    /// <summary>The accepted title attribute value.</summary>
    public string Title;

    /// <summary>Whether an accepted extended title attribute is present.</summary>
    public bool HasTitleExtended;

    /// <summary>The decoded extended title value.</summary>
    public string TitleExtended;

    /// <summary>The extended title character set with original spelling.</summary>
    public string TitleExtendedCharacterSet;

    /// <summary>The extended title language tag with original spelling.</summary>
    public string TitleExtendedLanguage;

    /// <summary>The raw extended title value before RFC 8187 decoding.</summary>
    public string TitleExtendedRawValue;

    /// <summary>Whether an accepted media type hint is present.</summary>
    public bool HasType;

    /// <summary>The accepted media type hint.</summary>
    public string Type;

    /// <summary>The complete original link-value text.</summary>
    public string OriginalText;

    /// <summary>The link-value's range in its original field value.</summary>
    public TextRange Range;

    /// <summary>Initializes an empty Web Link value.</summary>
    public WebLink()
    {
        Target = "";
        HasAnchor = false;
        Anchor = "";
        Relations = new string[0];
        RelationKinds = new WebLinkRelationKind[0];
        ReverseRelations = new string[0];
        ReverseRelationKinds = new WebLinkRelationKind[0];
        Parameters = new WebLinkParameter[0];
        HrefLanguages = new string[0];
        HasMedia = false;
        Media = "";
        HasTitle = false;
        Title = "";
        HasTitleExtended = false;
        TitleExtended = "";
        TitleExtendedCharacterSet = "";
        TitleExtendedLanguage = "";
        TitleExtendedRawValue = "";
        HasType = false;
        Type = "";
        OriginalText = "";
        Range = new TextRange(0, 0);
    }
}
