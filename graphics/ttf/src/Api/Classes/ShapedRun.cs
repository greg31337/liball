namespace ttf;

/// <summary>Represents an immutable shaped glyph run and its source clusters.</summary>
public class ShapedRun : IShapedRun
{
    ShapedGlyph[] glyphs;
    ShapedCluster[] clusters;
    int[] logicalToVisualGlyphIndexes;
    int[] visualToLogicalGlyphIndexes;
    int[] logicalToVisualClusterIndexes;
    int[] visualToLogicalClusterIndexes;
    int sourceLength;
    ShapingDirection direction;

    internal ShapedRun(
        ShapedGlyph[] glyphs,
        ShapedCluster[] clusters,
        int[] logicalToVisualGlyphIndexes,
        int[] visualToLogicalGlyphIndexes,
        int[] logicalToVisualClusterIndexes,
        int[] visualToLogicalClusterIndexes,
        int sourceLength,
        ShapingDirection direction)
    {
        this.glyphs = CopyGlyphs(glyphs);
        this.clusters = CopyClusters(clusters);
        this.logicalToVisualGlyphIndexes =
            CopyIntegers(logicalToVisualGlyphIndexes);
        this.visualToLogicalGlyphIndexes =
            CopyIntegers(visualToLogicalGlyphIndexes);
        this.logicalToVisualClusterIndexes =
            CopyIntegers(logicalToVisualClusterIndexes);
        this.visualToLogicalClusterIndexes =
            CopyIntegers(visualToLogicalClusterIndexes);
        this.sourceLength = sourceLength;
        this.direction = direction;
    }

    /// <summary>Gets the visual glyph count.</summary><returns>The glyph count.</returns>
    public int GetGlyphCount()
    {
        return glyphs.Length;
    }

    /// <summary>Gets one visual-order glyph.</summary><param name="visualIndex">The visual index.</param><returns>The glyph.</returns>
    public ShapedGlyph GetGlyph(int visualIndex)
    {
        ValidateIndex(visualIndex, glyphs.Length);
        return glyphs[visualIndex];
    }

    /// <summary>Gets one logical-order glyph.</summary><param name="logicalIndex">The logical index.</param><returns>The glyph.</returns>
    public ShapedGlyph GetLogicalGlyph(int logicalIndex)
    {
        return GetGlyph(GetVisualGlyphIndex(logicalIndex));
    }

    /// <summary>Maps a logical glyph index to visual order.</summary><param name="logicalIndex">The logical index.</param><returns>The visual index.</returns>
    public int GetVisualGlyphIndex(int logicalIndex)
    {
        ValidateIndex(logicalIndex, logicalToVisualGlyphIndexes.Length);
        return logicalToVisualGlyphIndexes[logicalIndex];
    }

    /// <summary>Maps a visual glyph index to logical order.</summary><param name="visualIndex">The visual index.</param><returns>The logical index.</returns>
    public int GetLogicalGlyphIndex(int visualIndex)
    {
        ValidateIndex(visualIndex, visualToLogicalGlyphIndexes.Length);
        return visualToLogicalGlyphIndexes[visualIndex];
    }

    /// <summary>Gets the logical cluster count.</summary><returns>The cluster count.</returns>
    public int GetClusterCount()
    {
        return clusters.Length;
    }

    /// <summary>Gets one logical-order cluster.</summary><param name="logicalIndex">The logical index.</param><returns>The cluster.</returns>
    public ShapedCluster GetCluster(int logicalIndex)
    {
        ValidateIndex(logicalIndex, clusters.Length);
        return clusters[logicalIndex];
    }

    /// <summary>Gets one visual-order cluster.</summary><param name="visualIndex">The visual index.</param><returns>The cluster.</returns>
    public ShapedCluster GetVisualCluster(int visualIndex)
    {
        return GetCluster(GetLogicalClusterIndex(visualIndex));
    }

    /// <summary>Maps a logical cluster index to visual order.</summary><param name="logicalIndex">The logical index.</param><returns>The visual index.</returns>
    public int GetVisualClusterIndex(int logicalIndex)
    {
        ValidateIndex(logicalIndex, logicalToVisualClusterIndexes.Length);
        return logicalToVisualClusterIndexes[logicalIndex];
    }

    /// <summary>Maps a visual cluster index to logical order.</summary><param name="visualIndex">The visual index.</param><returns>The logical index.</returns>
    public int GetLogicalClusterIndex(int visualIndex)
    {
        ValidateIndex(visualIndex, visualToLogicalClusterIndexes.Length);
        return visualToLogicalClusterIndexes[visualIndex];
    }

    /// <summary>Gets the complete source UTF-16 length.</summary><returns>The code-unit length.</returns>
    public int GetSourceLength()
    {
        return sourceLength;
    }

    /// <summary>Gets the resolved run direction.</summary><returns>The direction.</returns>
    public ShapingDirection GetDirection()
    {
        return direction;
    }

    /// <summary>Gets total horizontal advance.</summary><returns>The design-unit advance.</returns>
    public double GetTotalXAdvance()
    {
        double total = 0;
        int index = 0;
        while (index < glyphs.Length)
        {
            total += glyphs[index].GetXAdvance();
            index += 1;
        }
        return total;
    }

    /// <summary>Gets total vertical advance.</summary><returns>The design-unit advance.</returns>
    public double GetTotalYAdvance()
    {
        double total = 0;
        int index = 0;
        while (index < glyphs.Length)
        {
            total += glyphs[index].GetYAdvance();
            index += 1;
        }
        return total;
    }

    /// <summary>Reports whether breaking at a UTF-16 position may change shaping.</summary>
    /// <param name="utf16Index">The absolute boundary position.</param>
    /// <returns>True when unsafe.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>utf16Index</c> is outside the source.</exception>
    public bool IsUnsafeToBreakAtUtf16Index(int utf16Index)
    {
        if (utf16Index < 0 || utf16Index > sourceLength)
        {
            throw new System.ArgumentOutOfRangeException("utf16Index");
        }
        if (utf16Index == 0 || utf16Index == sourceLength)
        {
            return false;
        }
        int index = 0;
        while (index < clusters.Length)
        {
            ShapedCluster cluster = clusters[index];
            if (utf16Index > cluster.GetUtf16Start() &&
                utf16Index < cluster.GetUtf16End())
            {
                return true;
            }
            if (utf16Index == cluster.GetUtf16Start() &&
                cluster.IsUnsafeToBreak())
            {
                return true;
            }
            index += 1;
        }
        return false;
    }

    internal TextMeasurement Measure(
        FontInstance instance,
        double pointSize)
    {
        return TextMeasurer.MeasureRun(this, instance, pointSize);
    }

    internal TextMeasurement MeasureWithStyle(
        FontInstance instance,
        TextStyle style)
    {
        return TextMeasurer.MeasureRun(this, instance, style);
    }

    static ShapedGlyph[] CopyGlyphs(ShapedGlyph[] values)
    {
        if (values == null)
        {
            throw new System.ArgumentNullException("values");
        }

        ShapedGlyph[] copy = new ShapedGlyph[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }

    static ShapedCluster[] CopyClusters(ShapedCluster[] values)
    {
        if (values == null)
        {
            throw new System.ArgumentNullException("values");
        }

        ShapedCluster[] copy = new ShapedCluster[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }

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
