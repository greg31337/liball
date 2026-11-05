namespace languagecodes;

/// <summary>Contains one directed relationship between language entities.</summary>
public class LanguageRelationship
{
    /// <summary>The parent macrolanguage or ISO 639 Set 5 group code.</summary>
    public string? ParentCode;

    /// <summary>The member language or child ISO 639 Set 5 group code.</summary>
    public string? ChildCode;

    /// <summary>The meaning of the directed relationship.</summary>
    public LanguageRelationshipKind Kind;

    /// <summary>The ISO 639 set supplying the relationship.</summary>
    public Iso639Set SourceSet;

    /// <summary>Whether the relationship has been retired by its authoritative source.</summary>
    public bool IsRetired;
}
