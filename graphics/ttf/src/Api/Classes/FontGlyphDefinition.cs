namespace ttf;

/// <summary>Provides read-only GDEF inspection.</summary>
public class FontGlyphDefinition : IFontGlyphDefinition
{
    SfntGdefTable table;

    internal FontGlyphDefinition(SfntGdefTable table) { this.table = table; }

    /// <summary>Gets the major version.</summary><returns>The major version.</returns>
    public int GetMajorVersion() { return table.GetMajorVersion(); }
    /// <summary>Gets the minor version.</summary><returns>The minor version.</returns>
    public int GetMinorVersion() { return table.GetMinorVersion(); }
    /// <summary>Gets a glyph class.</summary><param name="glyphId">The glyph ID.</param><returns>The class, or zero.</returns>
    public int GetGlyphClass(int glyphId) { return table.GetGlyphClass(glyphId); }
    /// <summary>Gets a mark attachment class.</summary><param name="glyphId">The glyph ID.</param><returns>The class, or zero.</returns>
    public int GetMarkAttachmentClass(int glyphId)
    {
        return table.GetMarkAttachmentClass(glyphId);
    }
    /// <summary>Gets the attachment glyph count.</summary><returns>The glyph count.</returns>
    public int GetAttachmentGlyphCount()
    {
        SfntGdefAttachmentList? list = table.GetAttachmentList();
        return list == null ? 0 : list.GetGlyphCount();
    }
    /// <summary>Gets an attachment glyph ID.</summary><param name="index">The coverage index.</param><returns>The glyph ID.</returns>
    public int GetAttachmentGlyphId(int index)
    {
        SfntGdefAttachmentList? list = table.GetAttachmentList();
        if (list == null || index < 0 || index >= list.GetGlyphCount())
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
        return list.GetCoverage().GetGlyphId(index);
    }
    /// <summary>Gets an attachment point count.</summary><param name="glyphId">The glyph ID.</param><returns>The point count, or zero.</returns>
    public int GetAttachmentPointCount(int glyphId)
    {
        SfntGdefAttachmentList? list = table.GetAttachmentList();
        SfntGdefAttachmentPoint? point = list == null ? null :
            list.FindAttachmentPoint(glyphId);
        return point == null ? 0 : point.GetPointCount();
    }
    /// <summary>Gets an attachment point.</summary><param name="glyphId">The glyph ID.</param><param name="index">The point index.</param><returns>The contour point.</returns>
    public int GetAttachmentPoint(int glyphId, int index)
    {
        SfntGdefAttachmentList? list = table.GetAttachmentList();
        SfntGdefAttachmentPoint? point = list == null ? null :
            list.FindAttachmentPoint(glyphId);
        if (point == null)
        {
            throw new System.ArgumentException(
                "The glyph has no GDEF attachment points.",
                "glyphId");
        }
        return point.GetPointIndex(index);
    }
    /// <summary>Gets the ligature glyph count.</summary><returns>The glyph count.</returns>
    public int GetLigatureGlyphCount()
    {
        SfntGdefLigatureCaretList? list = table.GetLigatureCaretList();
        return list == null ? 0 : list.GetGlyphCount();
    }
    /// <summary>Gets a ligature glyph ID.</summary><param name="index">The coverage index.</param><returns>The glyph ID.</returns>
    public int GetLigatureGlyphId(int index)
    {
        SfntGdefLigatureCaretList? list = table.GetLigatureCaretList();
        if (list == null || index < 0 || index >= list.GetGlyphCount())
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
        return list.GetCoverage().GetGlyphId(index);
    }
    /// <summary>Gets a ligature caret count.</summary><param name="glyphId">The glyph ID.</param><returns>The caret count, or zero.</returns>
    public int GetLigatureCaretCount(int glyphId)
    {
        SfntGdefLigatureCaretList? list = table.GetLigatureCaretList();
        SfntGdefLigatureGlyph? glyph = list == null ? null :
            list.FindLigatureGlyph(glyphId);
        return glyph == null ? 0 : glyph.GetCaretCount();
    }
    /// <summary>Gets a ligature caret.</summary><param name="glyphId">The glyph ID.</param><param name="index">The caret index.</param><returns>The caret model.</returns>
    public FontGlyphCaret GetLigatureCaret(int glyphId, int index)
    {
        SfntGdefLigatureCaretList? list = table.GetLigatureCaretList();
        SfntGdefLigatureGlyph? glyph = list == null ? null :
            list.FindLigatureGlyph(glyphId);
        if (glyph == null)
        {
            throw new System.ArgumentException(
                "The glyph has no GDEF ligature carets.",
                "glyphId");
        }
        return new FontGlyphCaret(glyph.GetCaretValue(index));
    }
    /// <summary>Gets the mark glyph-set count.</summary><returns>The set count.</returns>
    public int GetMarkGlyphSetCount()
    {
        SfntGdefMarkGlyphSets? sets = table.GetMarkGlyphSets();
        return sets == null ? 0 : sets.GetSetCount();
    }
    /// <summary>Reports whether a mark set contains a glyph.</summary><param name="setIndex">The set index.</param><param name="glyphId">The glyph ID.</param><returns>True when covered.</returns>
    public bool MarkGlyphSetContains(int setIndex, int glyphId)
    {
        SfntGdefMarkGlyphSets? sets = table.GetMarkGlyphSets();
        if (sets == null)
        {
            throw new System.ArgumentOutOfRangeException("setIndex");
        }
        return sets.Covers(setIndex, glyphId);
    }
    /// <summary>Reports whether an item variation store exists.</summary><returns>True when present.</returns>
    public bool HasItemVariationStore()
    {
        return table.GetItemVariationStore() != null;
    }
}
