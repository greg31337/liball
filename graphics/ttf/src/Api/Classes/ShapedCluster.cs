namespace ttf;

/// <summary>Represents one immutable logical source cluster.</summary>
public class ShapedCluster : IShapedCluster
{
    int utf16Start;
    int utf16Length;
    int[] unicodeScalars;
    int[] glyphVisualIndexes;
    int logicalIndex;
    int visualIndex;
    bool unsafeToBreak;

    internal ShapedCluster(
        int utf16Start,
        int utf16Length,
        int[] unicodeScalars,
        int[] glyphVisualIndexes,
        int logicalIndex,
        int visualIndex,
        bool unsafeToBreak)
    {
        this.utf16Start = utf16Start;
        this.utf16Length = utf16Length;
        this.unicodeScalars = CopyIntegers(unicodeScalars);
        this.glyphVisualIndexes = CopyIntegers(glyphVisualIndexes);
        this.logicalIndex = logicalIndex;
        this.visualIndex = visualIndex;
        this.unsafeToBreak = unsafeToBreak;
    }

    /// <summary>Gets the source UTF-16 start.</summary><returns>The absolute offset.</returns>
    public int GetUtf16Start() { return utf16Start; }
    /// <summary>Gets the source UTF-16 length.</summary><returns>The code-unit length.</returns>
    public int GetUtf16Length() { return utf16Length; }
    /// <summary>Gets the exclusive source UTF-16 end.</summary><returns>The absolute end offset.</returns>
    public int GetUtf16End() { return utf16Start + utf16Length; }
    /// <summary>Gets the source Unicode scalar count.</summary><returns>The scalar count.</returns>
    public int GetUnicodeScalarCount() { return unicodeScalars.Length; }
    /// <summary>Gets one source Unicode scalar.</summary><param name="index">The scalar index.</param><returns>The scalar value.</returns>
    public int GetUnicodeScalar(int index)
    {
        ValidateIndex(index, unicodeScalars.Length);
        return unicodeScalars[index];
    }
    /// <summary>Copies the source Unicode scalars.</summary><returns>An independent array.</returns>
    public int[] CopyUnicodeScalars() { return CopyIntegers(unicodeScalars); }
    /// <summary>Gets the output glyph count.</summary><returns>The glyph count.</returns>
    public int GetGlyphCount() { return glyphVisualIndexes.Length; }
    /// <summary>Gets one output glyph visual index.</summary><param name="index">The member index.</param><returns>The visual glyph index.</returns>
    public int GetGlyphVisualIndex(int index)
    {
        ValidateIndex(index, glyphVisualIndexes.Length);
        return glyphVisualIndexes[index];
    }
    /// <summary>Gets the logical cluster index.</summary><returns>The logical index.</returns>
    public int GetLogicalIndex() { return logicalIndex; }
    /// <summary>Gets the visual cluster index.</summary><returns>The visual index.</returns>
    public int GetVisualIndex() { return visualIndex; }
    /// <summary>Reports whether breaking before this cluster may change shaping.</summary><returns>True when unsafe.</returns>
    public bool IsUnsafeToBreak() { return unsafeToBreak; }

    static int[] CopyIntegers(int[] values)
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
