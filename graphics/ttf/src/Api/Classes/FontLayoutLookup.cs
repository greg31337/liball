namespace ttf;

/// <summary>Represents one available OpenType layout lookup.</summary>
public class FontLayoutLookup : IFontLayoutLookup
{
    int index;
    SfntLayoutLookup lookup;
    int effectiveType;

    internal FontLayoutLookup(
        int index,
        SfntLayoutLookup lookup,
        int effectiveType)
    {
        this.index = index;
        this.lookup = lookup;
        this.effectiveType = effectiveType;
    }

    /// <summary>Gets the lookup-list index.</summary>
    /// <returns>The zero-based index.</returns>
    public int GetIndex() { return index; }

    /// <summary>Gets the declared lookup type.</summary>
    /// <returns>The encoded lookup type.</returns>
    public int GetLookupType() { return lookup.GetLookupType(); }

    /// <summary>Gets the effective lookup type after extension resolution.</summary>
    /// <returns>The effective lookup type.</returns>
    public int GetEffectiveType() { return effectiveType; }

    /// <summary>Gets the raw lookup flags.</summary>
    /// <returns>The lookup flags.</returns>
    public int GetFlags() { return lookup.GetLookupFlag(); }

    /// <summary>Reports the right-to-left flag.</summary>
    /// <returns>True when set; otherwise false.</returns>
    public bool IsRightToLeft() { return lookup.IsRightToLeft(); }

    /// <summary>Reports whether base glyphs are ignored.</summary>
    /// <returns>True when ignored; otherwise false.</returns>
    public bool IgnoresBaseGlyphs() { return lookup.IgnoresBaseGlyphs(); }

    /// <summary>Reports whether ligatures are ignored.</summary>
    /// <returns>True when ignored; otherwise false.</returns>
    public bool IgnoresLigatures() { return lookup.IgnoresLigatures(); }

    /// <summary>Reports whether marks are ignored.</summary>
    /// <returns>True when ignored; otherwise false.</returns>
    public bool IgnoresMarks() { return lookup.IgnoresMarks(); }

    /// <summary>Gets the mark attachment type.</summary>
    /// <returns>The attachment type.</returns>
    public int GetMarkAttachmentType() { return lookup.GetMarkAttachmentType(); }

    /// <summary>Reports whether a mark filtering set is used.</summary>
    /// <returns>True when used; otherwise false.</returns>
    public bool UsesMarkFilteringSet() { return lookup.UsesMarkFilteringSet(); }

    /// <summary>Gets the mark filtering set index.</summary>
    /// <returns>The set index.</returns>
    public int GetMarkFilteringSet() { return lookup.GetMarkFilteringSet(); }

    /// <summary>Gets the subtable count.</summary>
    /// <returns>The subtable count.</returns>
    public int GetSubtableCount() { return lookup.GetSubtableCount(); }
}
