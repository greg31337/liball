namespace uri;

/// <summary>
/// Stores a reusable parsed URI Template and its ordered parts.
/// </summary>
public class UriTemplate
{
    /// <summary>The ordered literal and expression parts.</summary>
    public UriTemplatePart[] Parts;

    /// <summary>The minimum RFC 6570 feature level required by the template.</summary>
    public UriTemplateLevel Level;

    /// <summary>The complete original template text.</summary>
    public string OriginalText;

    /// <summary>Initializes an empty Level 1 URI Template.</summary>
    public UriTemplate()
    {
        Parts = new UriTemplatePart[0];
        Level = UriTemplateLevel.Level1;
        OriginalText = "";
    }
}
