namespace languagecodes;

/// <summary>Contains one alternate code or registered tag that resolves to a language entity.</summary>
public class LanguageAlias
{
    /// <summary>The alternate code or tag.</summary>
    public string? Value;

    /// <summary>The origin and meaning of the alias.</summary>
    public LanguageAliasKind Kind;

    /// <summary>The authoritative preferred code when the alias is deprecated or retired.</summary>
    public string? PreferredCode;

    /// <summary>Whether the alias is deprecated by its authoritative source.</summary>
    public bool IsDeprecated;

    /// <summary>The ISO 639 set supplying the alias, or <see cref="Iso639Set.None"/> for a non-ISO source.</summary>
    public Iso639Set SourceSet;
}
