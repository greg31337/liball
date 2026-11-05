namespace ttf;

internal static class SfntColrGraphNormalizer
{
    internal static SfntColorGlyphGraph? Normalize(
        SfntColrTable table,
        int glyphId,
        FontLimits limits)
    {
        return Normalize(table, glyphId, null, limits);
    }

    internal static SfntColorGlyphGraph? Normalize(
        SfntColrTable table,
        int glyphId,
        SfntVariationLocation? location,
        FontLimits limits)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }

        if (limits == null)
        {
            throw new System.ArgumentNullException("limits");
        }

        SfntColrNormalizationContext context =
            new SfntColrNormalizationContext(table, location, limits);
        SfntColorPaintNode? root = null;

        if (table.GetVersion() == 1)
        {
            SfntColrBaseGlyphPaintRecord? paintRecord =
                table.FindBaseGlyphPaintRecord(glyphId);

            if (paintRecord != null)
            {
                root = context.NormalizeBaseGlyph(paintRecord, 1);
            }
        }

        if (root == null)
        {
            SfntColrBaseGlyphRecord? layerRecord =
                table.FindBaseGlyphRecord(glyphId);

            if (layerRecord != null)
            {
                root = context.NormalizeVersionZero(layerRecord, 1);
            }
        }

        if (root == null)
        {
            return null;
        }

        return new SfntColorGlyphGraph(
            glyphId,
            table.GetVersion(),
            root,
            context.GetNodeCount(),
            context.CopyReferencedGlyphIds());
    }
}

internal class SfntColrNormalizationContext
{
    SfntColrTable table;
    SfntVariationLocation? location;
    int maximumDepth;
    int maximumNodeCount;
    int nodeCount;
    System.Collections.Generic.Dictionary<int, SfntColorPaintNode> normalizedPaints;
    System.Collections.Generic.HashSet<int> activePaintOffsets;
    System.Collections.Generic.HashSet<int> activeBaseGlyphs;
    System.Collections.Generic.HashSet<int> referencedGlyphs;
    System.Collections.Generic.List<int> referencedGlyphIds;

    internal SfntColrNormalizationContext(
        SfntColrTable table,
        SfntVariationLocation? location,
        FontLimits limits)
    {
        this.table = table;
        this.location = location;
        maximumDepth = limits.GetMaxPaintDepth();
        maximumNodeCount = limits.GetMaxCoverageEntries();
        normalizedPaints =
            new System.Collections.Generic.Dictionary<int, SfntColorPaintNode>();
        activePaintOffsets = new System.Collections.Generic.HashSet<int>();
        activeBaseGlyphs = new System.Collections.Generic.HashSet<int>();
        referencedGlyphs = new System.Collections.Generic.HashSet<int>();
        referencedGlyphIds = new System.Collections.Generic.List<int>();
    }

    internal int GetNodeCount()
    {
        return nodeCount;
    }

    internal int[] CopyReferencedGlyphIds()
    {
        return referencedGlyphIds.ToArray();
    }

    internal SfntColorPaintNode NormalizeVersionZero(
        SfntColrBaseGlyphRecord record,
        int depth)
    {
        EnsureDepth(depth);
        SfntColorPaintNode[] layers =
            new SfntColorPaintNode[record.GetLayerCount()];
        int index = 0;

        while (index < layers.Length)
        {
            SfntColrLayerRecord layer = record.GetLayer(index);
            SfntColorPaintNode color = CreateColorNode(
                layer.GetPaletteEntryIndex(),
                1,
                0,
                depth + 2);
            AddReferencedGlyph(layer.GetGlyphId());
            layers[index] = CreateNode(
                SfntColorPaintKind.Glyph,
                0,
                new SfntColorPaintNode[] { color },
                layer.GetGlyphId(),
                0,
                0,
                new double[0],
                null,
                SfntColrCompositeMode.Clear,
                depth + 1);
            index += 1;
        }

        return CreateNode(
            SfntColorPaintKind.LayerList,
            0,
            layers,
            0,
            0,
            0,
            new double[0],
            null,
            SfntColrCompositeMode.Clear,
            depth);
    }

    internal SfntColorPaintNode NormalizeBaseGlyph(
        SfntColrBaseGlyphPaintRecord record,
        int depth)
    {
        int glyphId = record.GetGlyphId();

        if (activeBaseGlyphs.Contains(glyphId))
        {
            throw FontFormatException.CreateDiagnostic(
                "The COLR paint graph contains a PaintColrGlyph cycle.",
                FontDiagnosticCode.ColorPaintCycle);
        }

        activeBaseGlyphs.Add(glyphId);
        SfntColorPaintNode node = NormalizePaint(record.GetPaint(), depth);
        activeBaseGlyphs.Remove(glyphId);
        return node;
    }

    SfntColorPaintNode NormalizePaint(SfntColrPaint paint, int depth)
    {
        EnsureDepth(depth);
        int offset = paint.GetOffset();

        if (activePaintOffsets.Contains(offset))
        {
            throw FontFormatException.CreateDiagnostic(
                "The COLR paint graph contains a paint-offset cycle.",
                FontDiagnosticCode.ColorPaintCycle);
        }

        if (normalizedPaints.ContainsKey(offset))
        {
            SfntColorPaintNode existing = normalizedPaints[offset];
            EnsureSubtreeDepth(depth, existing);
            return existing;
        }

        activePaintOffsets.Add(offset);
        SfntColorPaintNode node = NormalizeNewPaint(paint, depth);
        activePaintOffsets.Remove(offset);
        normalizedPaints.Add(offset, node);
        return node;
    }

    SfntColorPaintNode NormalizeNewPaint(SfntColrPaint paint, int depth)
    {
        if (!paint.IsSupported())
        {
            return CreateLeafNode(
                SfntColorPaintKind.Unsupported,
                paint.GetFormat(),
                depth);
        }

        int format = paint.GetFormat();

        if (format == 1)
        {
            return NormalizeLayerList(paint, depth);
        }

        if (format == 2 || format == 3)
        {
            return CreateColorNode(
                paint.GetPaletteEntryIndex(),
                GetPaintValue(paint, 0),
                format,
                depth);
        }

        if (format >= 4 && format <= 9)
        {
            return NormalizeGradient(paint, depth);
        }

        if (format == 10)
        {
            SfntColorPaintNode child = NormalizeChild(paint, 0, depth + 1);
            AddReferencedGlyph(paint.GetGlyphId());
            return CreateNode(
                SfntColorPaintKind.Glyph,
                format,
                new SfntColorPaintNode[] { child },
                paint.GetGlyphId(),
                0,
                0,
                new double[0],
                null,
                SfntColrCompositeMode.Clear,
                depth);
        }

        if (format == 11)
        {
            return NormalizeColrGlyph(paint, depth);
        }

        if (format >= 12 && format <= 31)
        {
            return NormalizeTransform(paint, depth);
        }

        return NormalizeComposite(paint, depth);
    }

    SfntColorPaintNode NormalizeLayerList(SfntColrPaint paint, int depth)
    {
        SfntColrLayerList? layerList = table.GetLayerList();
        SfntColorPaintNode[] children =
            new SfntColorPaintNode[paint.GetLayerCount()];
        int index = 0;

        while (index < children.Length)
        {
            System.ArgumentNullException.ThrowIfNull(layerList);
            SfntColrPaint childPaint = layerList.GetPaint(
                paint.GetFirstLayerIndex() + index);
            children[index] = NormalizePaint(childPaint, depth + 1);
            index += 1;
        }

        return CreateNode(
            SfntColorPaintKind.LayerList,
            paint.GetFormat(),
            children,
            0,
            0,
            0,
            new double[0],
            null,
            SfntColrCompositeMode.Clear,
            depth);
    }

    SfntColorPaintNode NormalizeGradient(SfntColrPaint paint, int depth)
    {
        int format = paint.GetFormat();
        SfntColorPaintKind kind = SfntColorPaintKind.LinearGradient;

        if (format == 6 || format == 7)
        {
            kind = SfntColorPaintKind.RadialGradient;
        }
        else if (format == 8 || format == 9)
        {
            kind = SfntColorPaintKind.SweepGradient;
        }

        double[] values = new double[paint.GetValueCount()];
        int index = 0;

        while (index < values.Length)
        {
            values[index] = GetPaintValue(paint, index);
            index += 1;
        }

        SfntColorPaintLine colorLine = NormalizeColorLine(
            paint.GetColorLine(),
            format,
            depth + 1);
        return CreateNode(
            kind,
            format,
            new SfntColorPaintNode[0],
            0,
            0,
            0,
            values,
            colorLine,
            SfntColrCompositeMode.Clear,
            depth);
    }

    SfntColorPaintLine NormalizeColorLine(
        SfntColrColorLine? line,
        int sourceFormat,
        int colorDepth)
    {
        System.ArgumentNullException.ThrowIfNull(line);
        SfntColorPaintStop[] stops =
            new SfntColorPaintStop[line.GetStopCount()];
        int index = 0;

        while (index < stops.Length)
        {
            SfntColrColorStop stop = line.GetStop(index);
            double stopOffset = location == null ?
                stop.GetStopOffset() :
                stop.GetStopOffsetAtLocation(location);
            double alpha = location == null ?
                stop.GetAlpha() : stop.GetAlphaAtLocation(location);
            SfntColorPaintNode color = CreateColorNode(
                stop.GetPaletteEntryIndex(),
                alpha,
                sourceFormat,
                colorDepth);
            stops[index] = new SfntColorPaintStop(stopOffset, color);
            index += 1;
        }

        return new SfntColorPaintLine(line.GetExtendMode(), stops);
    }

    SfntColorPaintNode NormalizeColrGlyph(
        SfntColrPaint paint,
        int depth)
    {
        int glyphId = paint.GetGlyphId();
        SfntColrBaseGlyphPaintRecord? record =
            table.FindBaseGlyphPaintRecord(glyphId);

        if (record == null)
        {
            throw FontFormatException.Create(
                "A PaintColrGlyph references a missing color glyph.");
        }

        AddReferencedGlyph(glyphId);
        SfntColorPaintNode child = NormalizeBaseGlyph(record, depth + 1);
        return CreateNode(
            SfntColorPaintKind.ColrGlyph,
            paint.GetFormat(),
            new SfntColorPaintNode[] { child },
            glyphId,
            0,
            0,
            new double[0],
            null,
            SfntColrCompositeMode.Clear,
            depth);
    }

    SfntColorPaintNode NormalizeTransform(SfntColrPaint paint, int depth)
    {
        SfntColorPaintNode child = NormalizeChild(paint, 0, depth + 1);
        double[] matrix = CreateTransformMatrix(paint);
        return CreateNode(
            SfntColorPaintKind.Transform,
            paint.GetFormat(),
            new SfntColorPaintNode[] { child },
            0,
            0,
            0,
            matrix,
            null,
            SfntColrCompositeMode.Clear,
            depth);
    }

    SfntColorPaintNode NormalizeComposite(SfntColrPaint paint, int depth)
    {
        SfntColorPaintNode source = NormalizeChild(paint, 0, depth + 1);
        SfntColorPaintNode backdrop = NormalizeChild(paint, 1, depth + 1);
        return CreateNode(
            SfntColorPaintKind.Composite,
            paint.GetFormat(),
            new SfntColorPaintNode[] { source, backdrop },
            0,
            0,
            0,
            new double[0],
            null,
            paint.GetCompositeMode(),
            depth);
    }

    SfntColorPaintNode NormalizeChild(
        SfntColrPaint paint,
        int childIndex,
        int depth)
    {
        int childOffset = paint.GetChildPaintOffset(childIndex);
        SfntColrPaint? childPaint = table.FindPaintByOffset(childOffset);

        if (childPaint == null)
        {
            throw FontFormatException.Create(
                "A COLR paint references a missing child paint.");
        }

        return NormalizePaint(childPaint, depth);
    }

    double[] CreateTransformMatrix(SfntColrPaint paint)
    {
        int format = paint.GetFormat();

        if (format == 12 || format == 13)
        {
            SfntColrAffineTransform? transform = paint.GetAffineTransform();
            System.ArgumentNullException.ThrowIfNull(transform);
            double[] direct = new double[6];
            int index = 0;

            while (index < direct.Length)
            {
                direct[index] = location == null ?
                    transform.GetValue(index) :
                    transform.GetValueAtLocation(index, location);
                index += 1;
            }

            return direct;
        }

        if (format == 14 || format == 15)
        {
            return new double[] {
                1, 0, 0, 1,
                GetPaintValue(paint, 0), GetPaintValue(paint, 1)
            };
        }

        if (format >= 16 && format <= 23)
        {
            return CreateScaleMatrix(paint);
        }

        if (format >= 24 && format <= 27)
        {
            return CreateRotationMatrix(paint);
        }

        return CreateSkewMatrix(paint);
    }

    double[] CreateScaleMatrix(SfntColrPaint paint)
    {
        int format = paint.GetFormat();
        bool uniform = format >= 20;
        bool centered = format == 18 || format == 19 ||
            format == 22 || format == 23;
        double scaleX = GetPaintValue(paint, 0);
        double scaleY = uniform ? scaleX : GetPaintValue(paint, 1);
        int centerIndex = uniform ? 1 : 2;
        double centerX = centered ? GetPaintValue(paint, centerIndex) : 0;
        double centerY = centered ? GetPaintValue(paint, centerIndex + 1) : 0;
        return new double[] {
            scaleX,
            0,
            0,
            scaleY,
            centerX * (1 - scaleX),
            centerY * (1 - scaleY)
        };
    }

    double[] CreateRotationMatrix(SfntColrPaint paint)
    {
        int format = paint.GetFormat();
        bool centered = format == 26 || format == 27;
        double radians = GetPaintValue(paint, 0) * System.Math.PI;
        double cosine = System.Math.Cos(radians);
        double sine = System.Math.Sin(radians);
        double centerX = centered ? GetPaintValue(paint, 1) : 0;
        double centerY = centered ? GetPaintValue(paint, 2) : 0;
        return new double[] {
            cosine,
            sine,
            -sine,
            cosine,
            centerX - cosine * centerX + sine * centerY,
            centerY - sine * centerX - cosine * centerY
        };
    }

    double[] CreateSkewMatrix(SfntColrPaint paint)
    {
        int format = paint.GetFormat();
        bool centered = format == 30 || format == 31;
        double xSkew = -System.Math.Tan(
            GetPaintValue(paint, 0) * System.Math.PI);
        double ySkew = System.Math.Tan(
            GetPaintValue(paint, 1) * System.Math.PI);
        double centerX = centered ? GetPaintValue(paint, 2) : 0;
        double centerY = centered ? GetPaintValue(paint, 3) : 0;
        return new double[] {
            1,
            ySkew,
            xSkew,
            1,
            -xSkew * centerY,
            -ySkew * centerX
        };
    }

    double GetPaintValue(SfntColrPaint paint, int index)
    {
        if (location == null)
        {
            return paint.GetValue(index);
        }

        return paint.GetValueAtLocation(index, location);
    }

    SfntColorPaintNode CreateColorNode(
        int paletteEntryIndex,
        double alpha,
        int sourceFormat,
        int depth)
    {
        SfntColorPaintKind kind = paletteEntryIndex == 0xFFFF ?
            SfntColorPaintKind.ForegroundColor :
            SfntColorPaintKind.PaletteColor;
        return CreateNode(
            kind,
            sourceFormat,
            new SfntColorPaintNode[0],
            0,
            paletteEntryIndex,
            alpha,
            new double[0],
            null,
            SfntColrCompositeMode.Clear,
            depth);
    }

    SfntColorPaintNode CreateLeafNode(
        SfntColorPaintKind kind,
        int sourceFormat,
        int depth)
    {
        return CreateNode(
            kind,
            sourceFormat,
            new SfntColorPaintNode[0],
            0,
            0,
            0,
            new double[0],
            null,
            SfntColrCompositeMode.Clear,
            depth);
    }

    SfntColorPaintNode CreateNode(
        SfntColorPaintKind kind,
        int sourceFormat,
        SfntColorPaintNode[] children,
        int glyphId,
        int paletteEntryIndex,
        double alpha,
        double[] values,
        SfntColorPaintLine? colorLine,
        SfntColrCompositeMode compositeMode,
        int depth)
    {
        EnsureDepth(depth);

        if (nodeCount >= maximumNodeCount)
        {
            throw FontLimitException.CreateDiagnostic(
                "A normalized COLR paint graph exceeds the configured coverage-entry limit.",
                FontDiagnosticCode.ColorPaintNodeLimitExceeded);
        }

        nodeCount += 1;
        SfntColorPaintNode node = new SfntColorPaintNode(
            kind,
            sourceFormat,
            children,
            glyphId,
            paletteEntryIndex,
            alpha,
            values,
            colorLine,
            compositeMode);
        EnsureSubtreeDepth(depth, node);
        return node;
    }

    void AddReferencedGlyph(int glyphId)
    {
        if (referencedGlyphs.Add(glyphId))
        {
            referencedGlyphIds.Add(glyphId);
        }
    }

    void EnsureDepth(int depth)
    {
        if (depth > maximumDepth)
        {
            throw FontLimitException.CreateDiagnostic(
                "A COLR paint graph exceeds the configured paint-depth limit.",
                FontDiagnosticCode.ColorPaintDepthExceeded);
        }
    }

    void EnsureSubtreeDepth(int depth, SfntColorPaintNode node)
    {
        long deepestDepth = (long)depth +
            node.GetMaximumSubtreeDepth() - 1;

        if (deepestDepth > maximumDepth)
        {
            throw FontLimitException.CreateDiagnostic(
                "A COLR paint graph exceeds the configured paint-depth limit.",
                FontDiagnosticCode.ColorPaintDepthExceeded);
        }
    }
}
