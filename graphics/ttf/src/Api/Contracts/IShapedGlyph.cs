namespace ttf;

/// <summary>Defines one positioned output glyph and its source attribution.</summary>
public interface IShapedGlyph
{
    /// <summary>Gets the glyph identifier.</summary><returns>The zero-based glyph ID.</returns>
    int GetGlyphId();
    /// <summary>Gets the logical source cluster index.</summary><returns>The cluster index.</returns>
    int GetClusterIndex();
    /// <summary>Gets the source UTF-16 start.</summary><returns>The absolute offset.</returns>
    int GetUtf16Start();
    /// <summary>Gets the source UTF-16 length.</summary><returns>The code-unit length.</returns>
    int GetUtf16Length();
    /// <summary>Gets the exclusive source UTF-16 end.</summary><returns>The absolute end offset.</returns>
    int GetUtf16End();
    /// <summary>Gets the attributed Unicode scalar count.</summary><returns>The scalar count.</returns>
    int GetUnicodeScalarCount();
    /// <summary>Gets one attributed Unicode scalar.</summary><param name="index">The scalar index.</param><returns>The scalar value.</returns>
    int GetUnicodeScalar(int index);
    /// <summary>Copies the attributed Unicode scalars.</summary><returns>An independent array.</returns>
    int[] CopyUnicodeScalars();
    /// <summary>Gets the ligature component count.</summary><returns>The positive component count.</returns>
    int GetLigatureComponentCount();
    /// <summary>Gets this glyph's ligature component index.</summary><returns>The zero-based component index.</returns>
    int GetLigatureComponentIndex();
    /// <summary>Gets the logical glyph index.</summary><returns>The logical index.</returns>
    int GetLogicalIndex();
    /// <summary>Gets the visual glyph index.</summary><returns>The visual index.</returns>
    int GetVisualIndex();
    /// <summary>Gets retained shaping flags.</summary><returns>The flags.</returns>
    ShapedGlyphFlags GetFlags();
    /// <summary>Reports whether a break before the glyph is unsafe.</summary><returns>True when unsafe.</returns>
    bool IsUnsafeToBreak();
    /// <summary>Reports whether the source was default ignorable.</summary><returns>True when default ignorable.</returns>
    bool IsDefaultIgnorable();
    /// <summary>Reports whether this is a ligature glyph.</summary><returns>True for a ligature.</returns>
    bool IsLigature();
    /// <summary>Reports whether mark attachment positioned the glyph.</summary><returns>True for mark attachment.</returns>
    bool IsMarkAttachment();
    /// <summary>Reports whether cursive attachment positioned the glyph.</summary><returns>True for cursive attachment.</returns>
    bool IsCursiveAttachment();
    /// <summary>Gets horizontal advance.</summary><returns>The design-unit advance.</returns>
    double GetXAdvance();
    /// <summary>Gets vertical advance.</summary><returns>The design-unit advance.</returns>
    double GetYAdvance();
    /// <summary>Gets horizontal placement offset.</summary><returns>The design-unit offset.</returns>
    double GetXOffset();
    /// <summary>Gets vertical placement offset.</summary><returns>The design-unit offset.</returns>
    double GetYOffset();
}
