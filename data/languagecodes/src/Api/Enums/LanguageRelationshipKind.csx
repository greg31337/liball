namespace languagecodes;

/// <summary>Identifies a relationship between language entities.</summary>
public enum LanguageRelationshipKind
{
    /// <summary>A macrolanguage-to-member relationship.</summary>
    MacrolanguageMember = 0,
    /// <summary>An ISO 639 Set 5 parent-to-child relationship.</summary>
    Set5ParentChild = 1
}
