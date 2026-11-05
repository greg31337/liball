namespace uri;

/// <summary>
/// Stores one ordered Web Link parameter and its parsed value metadata.
/// </summary>
public class WebLinkParameter
{
    /// <summary>The parameter's recognized role.</summary>
    public WebLinkParameterKind Kind;

    /// <summary>The parameter name with its original spelling.</summary>
    public string Name;

    /// <summary>Whether an equals sign and value were present.</summary>
    public bool HasValue;

    /// <summary>The unquoted value, or decoded Unicode extended value.</summary>
    public string Value;

    /// <summary>The original value text before unquoting or extended decoding.</summary>
    public string RawValue;

    /// <summary>Whether the original value used quoted-string syntax.</summary>
    public bool WasQuoted;

    /// <summary>Whether the parameter uses RFC 8187 extended-value syntax.</summary>
    public bool Extended;

    /// <summary>The declared RFC 8187 character set, or an empty string.</summary>
    public string CharacterSet;

    /// <summary>The declared RFC 8187 language tag, or an empty string.</summary>
    public string Language;

    /// <summary>Whether RFC first-occurrence processing ignored this parameter.</summary>
    public bool Ignored;

    /// <summary>The complete original parameter text.</summary>
    public string OriginalText;

    /// <summary>The parameter's range in its original field value.</summary>
    public TextRange Range;

    /// <summary>Initializes an empty extension parameter.</summary>
    public WebLinkParameter()
    {
        Kind = WebLinkParameterKind.Extension;
        Name = "";
        HasValue = false;
        Value = "";
        RawValue = "";
        WasQuoted = false;
        Extended = false;
        CharacterSet = "";
        Language = "";
        Ignored = false;
        OriginalText = "";
        Range = new TextRange(0, 0);
    }
}
