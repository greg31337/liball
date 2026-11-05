namespace ttf;

/// <summary>Defines one available OpenType layout lookup.</summary>
public interface IFontLayoutLookup
{
    /// <summary>Gets the lookup-list index.</summary>
    /// <returns>The zero-based index.</returns>
    int GetIndex();

    /// <summary>Gets the declared lookup type.</summary>
    /// <returns>The encoded lookup type.</returns>
    int GetLookupType();

    /// <summary>Gets the effective lookup type after extension resolution.</summary>
    /// <returns>The effective lookup type.</returns>
    int GetEffectiveType();

    /// <summary>Gets the raw lookup flags.</summary>
    /// <returns>The lookup flags.</returns>
    int GetFlags();

    /// <summary>Reports the right-to-left flag.</summary>
    /// <returns>True when set; otherwise false.</returns>
    bool IsRightToLeft();

    /// <summary>Reports whether base glyphs are ignored.</summary>
    /// <returns>True when ignored; otherwise false.</returns>
    bool IgnoresBaseGlyphs();

    /// <summary>Reports whether ligatures are ignored.</summary>
    /// <returns>True when ignored; otherwise false.</returns>
    bool IgnoresLigatures();

    /// <summary>Reports whether marks are ignored.</summary>
    /// <returns>True when ignored; otherwise false.</returns>
    bool IgnoresMarks();

    /// <summary>Gets the mark attachment type.</summary>
    /// <returns>The attachment type.</returns>
    int GetMarkAttachmentType();

    /// <summary>Reports whether a mark filtering set is used.</summary>
    /// <returns>True when used; otherwise false.</returns>
    bool UsesMarkFilteringSet();

    /// <summary>Gets the mark filtering set index.</summary>
    /// <returns>The set index.</returns>
    int GetMarkFilteringSet();

    /// <summary>Gets the subtable count.</summary>
    /// <returns>The subtable count.</returns>
    int GetSubtableCount();
}
