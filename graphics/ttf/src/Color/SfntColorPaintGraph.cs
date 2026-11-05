namespace ttf;

internal enum SfntColorPaintKind
{
    LayerList = 0,
    Glyph = 1,
    ColrGlyph = 2,
    LinearGradient = 3,
    RadialGradient = 4,
    SweepGradient = 5,
    Transform = 6,
    Composite = 7,
    PaletteColor = 8,
    ForegroundColor = 9,
    Unsupported = 10
}

internal class SfntColorPaintStop
{
    double offset;
    SfntColorPaintNode color;

    internal SfntColorPaintStop(double offset, SfntColorPaintNode color)
    {
        this.offset = offset;
        this.color = color;
    }

    internal double GetOffset()
    {
        return offset;
    }

    internal SfntColorPaintNode GetColor()
    {
        return color;
    }
}

internal class SfntColorPaintLine
{
    SfntColrExtendMode extendMode;
    SfntColorPaintStop[] stops;

    internal SfntColorPaintLine(
        SfntColrExtendMode extendMode,
        SfntColorPaintStop[] stops)
    {
        this.extendMode = extendMode;
        this.stops = stops;
    }

    internal SfntColrExtendMode GetExtendMode()
    {
        return extendMode;
    }

    internal int GetStopCount()
    {
        return stops.Length;
    }

    internal SfntColorPaintStop GetStop(int index)
    {
        if (index < 0 || index >= stops.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return stops[index];
    }
}

internal class SfntColorPaintNode
{
    SfntColorPaintKind kind;
    int sourceFormat;
    SfntColorPaintNode[] children;
    int glyphId;
    int paletteEntryIndex;
    double alpha;
    double[] values;
    SfntColorPaintLine? colorLine;
    SfntColrCompositeMode compositeMode;
    int maximumSubtreeDepth;

    internal SfntColorPaintNode(
        SfntColorPaintKind kind,
        int sourceFormat,
        SfntColorPaintNode[] children,
        int glyphId,
        int paletteEntryIndex,
        double alpha,
        double[] values,
        SfntColorPaintLine? colorLine,
        SfntColrCompositeMode compositeMode)
    {
        this.kind = kind;
        this.sourceFormat = sourceFormat;
        this.children = children;
        this.glyphId = glyphId;
        this.paletteEntryIndex = paletteEntryIndex;
        this.alpha = alpha;
        this.values = values;
        this.colorLine = colorLine;
        this.compositeMode = compositeMode;
        maximumSubtreeDepth = CalculateMaximumSubtreeDepth();
    }

    internal SfntColorPaintKind GetKind()
    {
        return kind;
    }

    internal int GetSourceFormat()
    {
        return sourceFormat;
    }

    internal int GetChildCount()
    {
        return children.Length;
    }

    internal SfntColorPaintNode GetChild(int index)
    {
        if (index < 0 || index >= children.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return children[index];
    }

    internal int GetGlyphId()
    {
        if (kind != SfntColorPaintKind.Glyph &&
            kind != SfntColorPaintKind.ColrGlyph)
        {
            throw new System.InvalidOperationException(
                "This color paint node does not reference a glyph.");
        }

        return glyphId;
    }

    internal int GetPaletteEntryIndex()
    {
        if (kind != SfntColorPaintKind.PaletteColor)
        {
            throw new System.InvalidOperationException(
                "This color paint node is not a palette color.");
        }

        return paletteEntryIndex;
    }

    internal double GetAlpha()
    {
        if (kind != SfntColorPaintKind.PaletteColor &&
            kind != SfntColorPaintKind.ForegroundColor)
        {
            throw new System.InvalidOperationException(
                "This color paint node is not a color reference.");
        }

        return alpha;
    }

    internal int GetValueCount()
    {
        return values.Length;
    }

    internal double GetValue(int index)
    {
        if (index < 0 || index >= values.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return values[index];
    }

    internal SfntColorPaintLine GetColorLine()
    {
        if (colorLine == null)
        {
            throw new System.InvalidOperationException(
                "This color paint node has no color line.");
        }

        return colorLine;
    }

    internal SfntColrCompositeMode GetCompositeMode()
    {
        if (kind != SfntColorPaintKind.Composite)
        {
            throw new System.InvalidOperationException(
                "This color paint node is not a composite.");
        }

        return compositeMode;
    }

    internal int GetMaximumSubtreeDepth()
    {
        return maximumSubtreeDepth;
    }

    int CalculateMaximumSubtreeDepth()
    {
        int maximumChildDepth = 0;
        int index = 0;

        while (index < children.Length)
        {
            int childDepth = children[index].GetMaximumSubtreeDepth();

            if (childDepth > maximumChildDepth)
            {
                maximumChildDepth = childDepth;
            }

            index += 1;
        }

        if (colorLine != null)
        {
            index = 0;

            while (index < colorLine.GetStopCount())
            {
                int stopDepth = colorLine.GetStop(index).GetColor()
                    .GetMaximumSubtreeDepth();

                if (stopDepth > maximumChildDepth)
                {
                    maximumChildDepth = stopDepth;
                }

                index += 1;
            }
        }

        return maximumChildDepth + 1;
    }
}

internal class SfntColorGlyphGraph
{
    int glyphId;
    int colrVersion;
    SfntColorPaintNode root;
    int nodeCount;
    int[] referencedGlyphIds;

    internal SfntColorGlyphGraph(
        int glyphId,
        int colrVersion,
        SfntColorPaintNode root,
        int nodeCount,
        int[] referencedGlyphIds)
    {
        this.glyphId = glyphId;
        this.colrVersion = colrVersion;
        this.root = root;
        this.nodeCount = nodeCount;
        this.referencedGlyphIds = referencedGlyphIds;
    }

    internal int GetGlyphId()
    {
        return glyphId;
    }

    internal int GetColrVersion()
    {
        return colrVersion;
    }

    internal SfntColorPaintNode GetRoot()
    {
        return root;
    }

    internal int GetNodeCount()
    {
        return nodeCount;
    }

    internal int GetMaximumDepth()
    {
        return root.GetMaximumSubtreeDepth();
    }

    internal int GetReferencedGlyphCount()
    {
        return referencedGlyphIds.Length;
    }

    internal int GetReferencedGlyphId(int index)
    {
        if (index < 0 || index >= referencedGlyphIds.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return referencedGlyphIds[index];
    }
}
