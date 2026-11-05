namespace ttf;

/// <summary>Defines read-only GDEF inspection.</summary>
public interface IFontGlyphDefinition
{
    /// <summary>Gets the major version.</summary><returns>The major version.</returns>
    int GetMajorVersion();
    /// <summary>Gets the minor version.</summary><returns>The minor version.</returns>
    int GetMinorVersion();
    /// <summary>Gets a glyph class.</summary><param name="glyphId">The glyph ID.</param><returns>The class, or zero.</returns>
    int GetGlyphClass(int glyphId);
    /// <summary>Gets a mark attachment class.</summary><param name="glyphId">The glyph ID.</param><returns>The class, or zero.</returns>
    int GetMarkAttachmentClass(int glyphId);
    /// <summary>Gets the attachment glyph count.</summary><returns>The glyph count.</returns>
    int GetAttachmentGlyphCount();
    /// <summary>Gets an attachment glyph ID.</summary><param name="index">The coverage index.</param><returns>The glyph ID.</returns>
    int GetAttachmentGlyphId(int index);
    /// <summary>Gets an attachment point count.</summary><param name="glyphId">The glyph ID.</param><returns>The point count, or zero.</returns>
    int GetAttachmentPointCount(int glyphId);
    /// <summary>Gets an attachment point.</summary><param name="glyphId">The glyph ID.</param><param name="index">The point index.</param><returns>The contour point.</returns>
    int GetAttachmentPoint(int glyphId, int index);
    /// <summary>Gets the ligature glyph count.</summary><returns>The glyph count.</returns>
    int GetLigatureGlyphCount();
    /// <summary>Gets a ligature glyph ID.</summary><param name="index">The coverage index.</param><returns>The glyph ID.</returns>
    int GetLigatureGlyphId(int index);
    /// <summary>Gets a ligature caret count.</summary><param name="glyphId">The glyph ID.</param><returns>The caret count, or zero.</returns>
    int GetLigatureCaretCount(int glyphId);
    /// <summary>Gets a ligature caret.</summary><param name="glyphId">The glyph ID.</param><param name="index">The caret index.</param><returns>The caret model.</returns>
    FontGlyphCaret GetLigatureCaret(int glyphId, int index);
    /// <summary>Gets the mark glyph-set count.</summary><returns>The set count.</returns>
    int GetMarkGlyphSetCount();
    /// <summary>Reports whether a mark set contains a glyph.</summary><param name="setIndex">The set index.</param><param name="glyphId">The glyph ID.</param><returns>True when covered.</returns>
    bool MarkGlyphSetContains(int setIndex, int glyphId);
    /// <summary>Reports whether an item variation store exists.</summary><returns>True when present.</returns>
    bool HasItemVariationStore();
}
