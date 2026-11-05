namespace ttf;

internal class ColorPaintGraphBuilder
{
    SfntCpalTable cpal;
    int paletteIndex;
    ColorGlyphColor foregroundColor;
    System.Collections.Generic.Dictionary<SfntColorPaintNode, ColorPaint> paints;

    internal ColorPaintGraphBuilder(
        SfntCpalTable cpal,
        int paletteIndex,
        ColorGlyphColor foregroundColor)
    {
        this.cpal = cpal;
        this.paletteIndex = paletteIndex;
        this.foregroundColor = foregroundColor;
        paints = new System.Collections.Generic.Dictionary<SfntColorPaintNode, ColorPaint>();
    }

    internal ColorPaintGraph Build(
        SfntColorGlyphGraph source,
        ColorGlyphClipBox? clipBox,
        NormalizedFontCoordinates variationCoordinates)
    {
        int[] references = new int[source.GetReferencedGlyphCount()];
        int index = 0;

        while (index < references.Length)
        {
            references[index] = source.GetReferencedGlyphId(index);
            index += 1;
        }

        return new ColorPaintGraph(
            source.GetGlyphId(),
            source.GetColrVersion(),
            ConvertPaint(source.GetRoot()),
            source.GetNodeCount(),
            source.GetMaximumDepth(),
            references,
            clipBox,
            variationCoordinates);
    }

    ColorPaint ConvertPaint(SfntColorPaintNode source)
    {
        if (paints.ContainsKey(source))
        {
            return paints[source];
        }

        ColorPaint[] children = new ColorPaint[source.GetChildCount()];
        int index = 0;

        while (index < children.Length)
        {
            children[index] = ConvertPaint(source.GetChild(index));
            index += 1;
        }

        double[] values = new double[source.GetValueCount()];
        index = 0;

        while (index < values.Length)
        {
            values[index] = source.GetValue(index);
            index += 1;
        }

        ColorPaintStop[]? stops = null;
        int extendMode = 0;
        SfntColorPaintKind sourceKind = source.GetKind();

        if (sourceKind == SfntColorPaintKind.LinearGradient ||
            sourceKind == SfntColorPaintKind.RadialGradient ||
            sourceKind == SfntColorPaintKind.SweepGradient)
        {
            SfntColorPaintLine line = source.GetColorLine();
            extendMode = (int)line.GetExtendMode();
            stops = new ColorPaintStop[line.GetStopCount()];
            index = 0;

            while (index < stops.Length)
            {
                SfntColorPaintStop stop = line.GetStop(index);
                stops[index] = new ColorPaintStop(
                    stop.GetOffset(),
                    ConvertPaint(stop.GetColor()));
                index += 1;
            }
        }

        int glyphId = 0;
        int paletteEntryIndex = 0;
        double alpha = 0;
        int compositeMode = 0;
        ColorGlyphColor? color = null;

        if (sourceKind == SfntColorPaintKind.Glyph ||
            sourceKind == SfntColorPaintKind.ColrGlyph)
        {
            glyphId = source.GetGlyphId();
        }
        if (sourceKind == SfntColorPaintKind.PaletteColor)
        {
            paletteEntryIndex = source.GetPaletteEntryIndex();
            alpha = source.GetAlpha();
            color = ConvertColor(cpal.ResolveColor(
                paletteIndex,
                paletteEntryIndex,
                foregroundColor.ToCpalColor()));
        }
        if (sourceKind == SfntColorPaintKind.ForegroundColor)
        {
            alpha = source.GetAlpha();
            color = foregroundColor;
        }
        if (sourceKind == SfntColorPaintKind.Composite)
        {
            compositeMode = (int)source.GetCompositeMode();
        }

        ColorPaint result = new ColorPaint(
            (ColorPaintKind)(int)sourceKind,
            source.GetSourceFormat(),
            children,
            glyphId,
            paletteEntryIndex,
            alpha,
            values,
            extendMode,
            stops,
            compositeMode,
            color);
        paints.Add(source, result);
        return result;
    }

    static ColorGlyphColor ConvertColor(SfntCpalColor color)
    {
        return new ColorGlyphColor(
            color.GetRed(),
            color.GetGreen(),
            color.GetBlue(),
            color.GetAlpha());
    }
}
