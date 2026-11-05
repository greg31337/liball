namespace ttf;

/// <summary>Represents one positioned output glyph and its source attribution.</summary>
public class ShapedGlyph : IShapedGlyph
{
    int glyphId;
    int clusterIndex;
    int utf16Start;
    int utf16Length;
    int[] unicodeScalars;
    int ligatureComponentCount;
    int ligatureComponentIndex;
    int logicalIndex;
    int visualIndex;
    ShapedGlyphFlags flags;
    double xAdvance;
    double yAdvance;
    double xOffset;
    double yOffset;

    internal ShapedGlyph(
        int glyphId,
        int clusterIndex,
        int utf16Start,
        int utf16Length,
        int[] unicodeScalars,
        int ligatureComponentCount,
        int ligatureComponentIndex,
        int logicalIndex,
        int visualIndex,
        ShapedGlyphFlags flags,
        double xAdvance,
        double yAdvance,
        double xOffset,
        double yOffset)
    {
        this.glyphId = glyphId;
        this.clusterIndex = clusterIndex;
        this.utf16Start = utf16Start;
        this.utf16Length = utf16Length;
        this.unicodeScalars = CopyScalars(unicodeScalars);
        this.ligatureComponentCount = ligatureComponentCount;
        this.ligatureComponentIndex = ligatureComponentIndex;
        this.logicalIndex = logicalIndex;
        this.visualIndex = visualIndex;
        this.flags = flags;
        this.xAdvance = xAdvance;
        this.yAdvance = yAdvance;
        this.xOffset = xOffset;
        this.yOffset = yOffset;
    }

    /// <summary>Gets the glyph identifier.</summary><returns>The zero-based glyph ID.</returns>
    public int GetGlyphId() { return glyphId; }
    /// <summary>Gets the logical source cluster index.</summary><returns>The cluster index.</returns>
    public int GetClusterIndex() { return clusterIndex; }
    /// <summary>Gets the source UTF-16 start.</summary><returns>The absolute offset.</returns>
    public int GetUtf16Start() { return utf16Start; }
    /// <summary>Gets the source UTF-16 length.</summary><returns>The code-unit length.</returns>
    public int GetUtf16Length() { return utf16Length; }
    /// <summary>Gets the exclusive source UTF-16 end.</summary><returns>The absolute end offset.</returns>
    public int GetUtf16End() { return utf16Start + utf16Length; }
    /// <summary>Gets the attributed Unicode scalar count.</summary><returns>The scalar count.</returns>
    public int GetUnicodeScalarCount() { return unicodeScalars.Length; }
    /// <summary>Gets one attributed Unicode scalar.</summary><param name="index">The scalar index.</param><returns>The scalar value.</returns>
    public int GetUnicodeScalar(int index)
    {
        ValidateIndex(index, unicodeScalars.Length);
        return unicodeScalars[index];
    }
    /// <summary>Copies the attributed Unicode scalars.</summary><returns>An independent array.</returns>
    public int[] CopyUnicodeScalars() { return CopyScalars(unicodeScalars); }
    /// <summary>Gets the ligature component count.</summary><returns>The positive component count.</returns>
    public int GetLigatureComponentCount() { return ligatureComponentCount; }
    /// <summary>Gets this glyph's ligature component index.</summary><returns>The zero-based component index.</returns>
    public int GetLigatureComponentIndex() { return ligatureComponentIndex; }
    /// <summary>Gets the logical glyph index.</summary><returns>The logical index.</returns>
    public int GetLogicalIndex() { return logicalIndex; }
    /// <summary>Gets the visual glyph index.</summary><returns>The visual index.</returns>
    public int GetVisualIndex() { return visualIndex; }
    /// <summary>Gets retained shaping flags.</summary><returns>The flags.</returns>
    public ShapedGlyphFlags GetFlags() { return flags; }
    /// <summary>Reports whether a break before the glyph is unsafe.</summary><returns>True when unsafe.</returns>
    public bool IsUnsafeToBreak() { return HasFlag(ShapedGlyphFlags.UnsafeToBreak); }
    /// <summary>Reports whether the source was default ignorable.</summary><returns>True when default ignorable.</returns>
    public bool IsDefaultIgnorable() { return HasFlag(ShapedGlyphFlags.DefaultIgnorable); }
    /// <summary>Reports whether this is a ligature glyph.</summary><returns>True for a ligature.</returns>
    public bool IsLigature() { return HasFlag(ShapedGlyphFlags.Ligature); }
    /// <summary>Reports whether mark attachment positioned the glyph.</summary><returns>True for mark attachment.</returns>
    public bool IsMarkAttachment() { return HasFlag(ShapedGlyphFlags.MarkAttachment); }
    /// <summary>Reports whether cursive attachment positioned the glyph.</summary><returns>True for cursive attachment.</returns>
    public bool IsCursiveAttachment() { return HasFlag(ShapedGlyphFlags.CursiveAttachment); }
    /// <summary>Gets horizontal advance.</summary><returns>The design-unit advance.</returns>
    public double GetXAdvance() { return xAdvance; }
    /// <summary>Gets vertical advance.</summary><returns>The design-unit advance.</returns>
    public double GetYAdvance() { return yAdvance; }
    /// <summary>Gets horizontal placement offset.</summary><returns>The design-unit offset.</returns>
    public double GetXOffset() { return xOffset; }
    /// <summary>Gets vertical placement offset.</summary><returns>The design-unit offset.</returns>
    public double GetYOffset() { return yOffset; }

    bool HasFlag(ShapedGlyphFlags value)
    {
        return ((int)flags & (int)value) != 0;
    }

    static int[] CopyScalars(int[] values)
    {
        if (values == null)
        {
            throw new System.ArgumentNullException("values");
        }
        int[] copy = new int[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }

    static void ValidateIndex(int index, int count)
    {
        if (index < 0 || index >= count)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
    }
}
