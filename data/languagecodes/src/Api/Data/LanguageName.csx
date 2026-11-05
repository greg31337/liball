namespace languagecodes;

/// <summary>Contains one authoritative or registry-supplied name for a language entity.</summary>
public class LanguageName
{
    /// <summary>The language name exactly as supplied by its source.</summary>
    public string? Text;

    /// <summary>The lowercase language code describing the language of <see cref="Text"/>.</summary>
    public string? NameLanguage;

    /// <summary>The role and source-language form of the name.</summary>
    public LanguageNameKind Kind;

    /// <summary>The ISO 639 set supplying the name, or <see cref="Iso639Set.None"/> for a non-ISO source.</summary>
    public Iso639Set SourceSet;

    /// <summary>Whether this is the authoritative reference name for its entity.</summary>
    public bool IsReferenceName;
}
