namespace ttf;

/// <summary>Resolves and selects the supported color representations of a glyph.</summary>
public class ColorGlyphResolver : IColorGlyphResolver
{
    /// <summary>Creates a color glyph resolver.</summary>
    public ColorGlyphResolver()
    {
    }

    /// <summary>Resolves a glyph using palette zero and automatic selection.</summary>
    /// <param name="instance">The resolved font instance.</param>
    /// <param name="glyphId">The zero-based glyph identifier.</param>
    /// <returns>The available representations and deterministic selection.</returns>
    public ColorGlyph ResolveGlyph(FontInstance instance, int glyphId)
    {
        return Resolve(instance, glyphId);
    }

    /// <summary>Resolves a glyph using explicit palette and representation choices.</summary>
    /// <param name="instance">The resolved font instance.</param>
    /// <param name="glyphId">The zero-based glyph identifier.</param>
    /// <param name="options">Palette, foreground, and representation choices.</param>
    /// <returns>The available representations and deterministic selection.</returns>
    public ColorGlyph ResolveGlyphWithOptions(
        FontInstance instance,
        int glyphId,
        ColorGlyphOptions options)
    {
        return Resolve(instance, glyphId, options);
    }

    /// <summary>Resolves a glyph using palette zero and automatic selection.</summary>
    /// <param name="instance">The resolved font instance.</param>
    /// <param name="glyphId">The zero-based glyph identifier.</param>
    /// <returns>The available representations and deterministic selection.</returns>
    /// <exception cref="System.ArgumentNullException"><c>instance</c> is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>glyphId</c> is outside the font.</exception>
    internal static ColorGlyph Resolve(FontInstance instance, int glyphId)
    {
        return Resolve(instance, glyphId, new ColorGlyphOptions());
    }

    /// <summary>Resolves a glyph using explicit palette and representation choices.</summary>
    /// <remarks>
    /// Automatic selection prefers COLR, SVG, CBDT, <c>sbix</c>, EBDT, and then
    /// the monochrome outline. An unavailable explicit preference uses the same
    /// fallback order. All available representations remain exposed on the result.
    /// </remarks>
    /// <param name="instance">The resolved font instance.</param>
    /// <param name="glyphId">The zero-based glyph identifier.</param>
    /// <param name="options">Palette, foreground, and representation choices.</param>
    /// <returns>The available representations and deterministic selection.</returns>
    /// <exception cref="System.ArgumentNullException">
    /// <c>instance</c> or <c>options</c> is null.
    /// </exception>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>glyphId</c> is outside the font or the selected palette does not exist.
    /// </exception>
    internal static ColorGlyph Resolve(
        FontInstance instance,
        int glyphId,
        ColorGlyphOptions options)
    {
        if (instance == null)
        {
            throw new System.ArgumentNullException("instance");
        }

        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        options = options.Copy();
        if (glyphId < 0 || glyphId >= instance.GetGlyphCount())
        {
            throw new System.ArgumentOutOfRangeException("glyphId");
        }

        FontOperationOptions operation = options.GetOperationSettings();
        FontOperationContext.Checkpoint(operation);
        FontFace face = instance.GetFace();
        SfntTableCache cache = face.GetTableCache();
        FontLimits limits = FontOperationContext.CreateEffectiveLimits(
            cache.GetLimits(),
            operation);
        ColorPaintGraph? paintGraph = ResolvePaintGraph(
            instance,
            glyphId,
            options,
            cache,
            limits);
        FontOperationContext.Checkpoint(operation);
        ColorSvgDocument? svgDocument = ResolveSvg(glyphId, cache);
        FontOperationContext.Checkpoint(operation);
        ColorBitmapStrike[] bitmapStrikes = ResolveBitmapStrikes(
            glyphId,
            cache);
        if (bitmapStrikes.Length > limits.GetMaxCoverageEntries())
        {
            throw FontLimitException.CreateDiagnostic(
                "Color bitmap resolution exceeds the configured entry limit.",
                FontDiagnosticCode.LimitExceeded);
        }
        FontOperationContext.Checkpoint(operation);
        GlyphOutline? outline = ResolveOutline(instance, glyphId);
        int selectedBitmapIndex = -1;
        ColorGlyphRepresentation selected = SelectPreferred(
            options.GetPreferredRepresentation(),
            paintGraph,
            svgDocument,
            bitmapStrikes,
            outline);

        if (IsBitmap(selected))
        {
            selectedBitmapIndex = FindBitmap(
                bitmapStrikes,
                selected,
                options.GetTargetPixelsPerEmX(),
                options.GetTargetPixelsPerEmY());
        }

        PdfColorGlyphAppearance guidance = GetPdfGuidance(
            selected,
            bitmapStrikes,
            selectedBitmapIndex);
        FontDiagnostic[] diagnostics = CreateSelectionDiagnostics(
            glyphId,
            options.GetPreferredRepresentation(),
            selected,
            face.GetFaceIndex());
        if (diagnostics.Length > limits.GetMaxDiagnostics())
        {
            throw FontLimitException.CreateDiagnostic(
                "Color resolution exceeds the configured diagnostic limit.",
                FontDiagnosticCode.LimitExceeded);
        }
        FontOperationContext.RejectDiagnosticsInStrictMode(
            diagnostics,
            operation,
            "Strict color resolution rejected an unavailable representation.");
        return new ColorGlyph(
            glyphId,
            options.GetPaletteIndex(),
            selected,
            guidance,
            paintGraph,
            bitmapStrikes,
            svgDocument,
            outline,
            selectedBitmapIndex,
            options,
            diagnostics);
    }

    static ColorPaintGraph? ResolvePaintGraph(
        FontInstance instance,
        int glyphId,
        ColorGlyphOptions options,
        SfntTableCache cache,
        FontLimits limits)
    {
        SfntColrTable? colr = SfntColrTableParser.FindTable(cache);

        if (colr == null)
        {
            return null;
        }

        SfntColorGlyphGraph? source;
        SfntVariationLocation? location = null;

        if (colr.HasVariationData())
        {
            location = colr.CreateVariationLocation(
                instance.GetCoordinates().GetNormalizedValues());
            source = SfntColrGraphNormalizer.Normalize(
                colr,
                glyphId,
                location,
                limits);
        }
        else
        {
            source = SfntColrGraphNormalizer.Normalize(
                colr,
                glyphId,
                limits);
        }

        if (source == null)
        {
            return null;
        }

        SfntCpalTable? cpal = SfntCpalTableParser.FindTable(cache);
        System.ArgumentNullException.ThrowIfNull(cpal);
        cpal.GetPalette(options.GetPaletteIndex());
        ColorPaintGraphBuilder builder = new ColorPaintGraphBuilder(
            cpal,
            options.GetPaletteIndex(),
            options.GetForegroundColor());
        ColorGlyphClipBox? clipBox = ResolveClipBox(
            colr,
            glyphId,
            location);
        return builder.Build(source, clipBox, instance.GetCoordinates());
    }

    static FontDiagnostic[] CreateSelectionDiagnostics(
        int glyphId,
        ColorGlyphRepresentation preferred,
        ColorGlyphRepresentation selected,
        int faceIndex)
    {
        if (preferred == ColorGlyphRepresentation.None ||
            preferred == selected)
        {
            return new FontDiagnostic[0];
        }

        return new FontDiagnostic[]
        {
            new FontDiagnostic(
                FontDiagnosticSeverity.Warning,
                FontDiagnosticCode.InvalidColorGlyph,
                "The preferred color representation is unavailable; deterministic fallback was selected.",
                faceIndex,
                0,
                -1,
                "ColorGlyph[" + glyphId + "].Selection")
        };
    }

    static ColorGlyphClipBox? ResolveClipBox(
        SfntColrTable table,
        int glyphId,
        SfntVariationLocation? location)
    {
        SfntColrClipList? clipList = table.GetClipList();

        if (clipList == null)
        {
            return null;
        }

        SfntColrClipBox? source = clipList.FindClipBox(glyphId);

        if (source == null)
        {
            return null;
        }
        if (!source.IsSupported())
        {
            return new ColorGlyphClipBox(
                source.GetFormat(),
                false,
                0,
                0,
                0,
                0);
        }

        double[] coordinates = new double[4];
        int index = 0;

        while (index < coordinates.Length)
        {
            coordinates[index] = location == null ?
                source.GetCoordinate(index) :
                source.GetCoordinateAtLocation(index, location);
            index += 1;
        }

        return new ColorGlyphClipBox(
            source.GetFormat(),
            true,
            coordinates[0],
            coordinates[1],
            coordinates[2],
            coordinates[3]);
    }

    static ColorSvgDocument? ResolveSvg(int glyphId, SfntTableCache cache)
    {
        SfntSvgTable? table = SfntSvgTableParser.FindTable(cache);

        if (table == null)
        {
            return null;
        }

        SfntSvgDocument? document = table.FindDocument(glyphId);
        return document == null ? null : new ColorSvgDocument(document);
    }

    static ColorBitmapStrike[] ResolveBitmapStrikes(
        int glyphId,
        SfntTableCache cache)
    {
        System.Collections.Generic.List<ColorBitmapStrike> results =
            new System.Collections.Generic.List<ColorBitmapStrike>();
        AddEmbeddedBitmapStrikes(
            results,
            SfntCblcTableParser.FindTable(cache),
            glyphId,
            ColorGlyphRepresentation.Cbdt);
        AddSbixStrikes(
            results,
            SfntSbixTableParser.FindTable(cache),
            glyphId);
        AddEmbeddedBitmapStrikes(
            results,
            SfntEblcTableParser.FindTable(cache),
            glyphId,
            ColorGlyphRepresentation.Ebdt);
        return results.ToArray();
    }

    static void AddSbixStrikes(
        System.Collections.Generic.List<ColorBitmapStrike> results,
        SfntSbixTable? table,
        int glyphId)
    {
        if (table == null)
        {
            return;
        }

        int index = 0;

        while (index < table.GetStrikeCount())
        {
            SfntSbixStrike strike = table.GetStrike(index);
            SfntSbixGlyphImage? image = strike.ResolveGlyphImage(glyphId);

            if (image != null)
            {
                ColorBitmapPayloadKind payloadKind = ConvertSbixPayloadKind(
                    image.GetGraphicKind());
                results.Add(new ColorBitmapStrike(
                    ColorGlyphRepresentation.Sbix,
                    glyphId,
                    0,
                    strike.GetGlyphCount() - 1,
                    strike.GetPixelsPerEm(),
                    strike.GetPixelsPerEm(),
                    strike.GetPixelsPerInch(),
                    0,
                    image.GetSourceGlyphId(),
                    image.GetOriginOffsetX(),
                    image.GetOriginOffsetY(),
                    image.GetGraphicTypeTag(),
                    payloadKind,
                    payloadKind != ColorBitmapPayloadKind.Unsupported,
                    table.DrawOutlines(),
                    null,
                    null,
                    null,
                    new ColorBitmapComponent[0],
                    image.CopyData()));
            }

            index += 1;
        }
    }

    static void AddEmbeddedBitmapStrikes(
        System.Collections.Generic.List<ColorBitmapStrike> results,
        SfntBitmapLocationTable? table,
        int glyphId,
        ColorGlyphRepresentation representation)
    {
        if (table == null)
        {
            return;
        }

        int index = 0;

        while (index < table.GetStrikeCount())
        {
            SfntBitmapStrike strike = table.GetStrike(index);

            if (strike.HasGlyph(glyphId))
            {
                SfntBitmapGlyphData glyph = strike.GetGlyph(glyphId);

                results.Add(new ColorBitmapStrike(
                    representation,
                    glyphId,
                    strike.GetStartGlyphId(),
                    strike.GetEndGlyphId(),
                    strike.GetPixelsPerEmX(),
                    strike.GetPixelsPerEmY(),
                    0,
                    strike.GetBitDepth(),
                    glyphId,
                    0,
                    0,
                    glyph.GetImageFormat(),
                    ConvertEmbeddedPayloadKind(glyph),
                    glyph.IsSupported(),
                    false,
                    ConvertGlyphMetrics(glyph.GetMetrics()),
                    ConvertLineMetrics(strike.GetHorizontalLineMetrics()),
                    ConvertLineMetrics(strike.GetVerticalLineMetrics()),
                    ConvertComponents(glyph),
                    glyph.CopyPayload()));
            }

            index += 1;
        }
    }

    static ColorBitmapPayloadKind ConvertSbixPayloadKind(
        SfntSbixGraphicKind kind)
    {
        if (kind == SfntSbixGraphicKind.Png)
        {
            return ColorBitmapPayloadKind.Png;
        }
        if (kind == SfntSbixGraphicKind.Jpeg)
        {
            return ColorBitmapPayloadKind.Jpeg;
        }
        if (kind == SfntSbixGraphicKind.Tiff)
        {
            return ColorBitmapPayloadKind.Tiff;
        }
        return ColorBitmapPayloadKind.Unsupported;
    }

    static ColorBitmapPayloadKind ConvertEmbeddedPayloadKind(
        SfntBitmapGlyphData glyph)
    {
        if (glyph.IsPng())
        {
            return ColorBitmapPayloadKind.Png;
        }

        int format = glyph.GetImageFormat();

        if (format == 8 || format == 9)
        {
            return ColorBitmapPayloadKind.CompositeBitmap;
        }
        if (format == 1 || format == 2 || format == 5 ||
            format == 6 || format == 7)
        {
            return ColorBitmapPayloadKind.RawBitmap;
        }
        return ColorBitmapPayloadKind.Unsupported;
    }

    static ColorBitmapGlyphMetrics? ConvertGlyphMetrics(
        SfntBitmapGlyphMetrics? source)
    {
        if (source == null)
        {
            return null;
        }

        int verticalBearingX = 0;
        int verticalBearingY = 0;
        int verticalAdvance = 0;

        if (source.IsBig())
        {
            verticalBearingX = source.GetVerticalBearingX();
            verticalBearingY = source.GetVerticalBearingY();
            verticalAdvance = source.GetVerticalAdvance();
        }

        return new ColorBitmapGlyphMetrics(
            source.IsBig(),
            source.IsHorizontal(),
            source.GetHeight(),
            source.GetWidth(),
            source.GetBearingX(),
            source.GetBearingY(),
            source.GetAdvance(),
            verticalBearingX,
            verticalBearingY,
            verticalAdvance);
    }

    static ColorBitmapLineMetrics ConvertLineMetrics(
        SfntBitmapLineMetrics source)
    {
        return new ColorBitmapLineMetrics(
            source.GetAscender(),
            source.GetDescender(),
            source.GetWidthMax(),
            source.GetCaretSlopeNumerator(),
            source.GetCaretSlopeDenominator(),
            source.GetCaretOffset(),
            source.GetMinOriginSideBearing(),
            source.GetMinAdvanceSideBearing(),
            source.GetMaxBeforeBaseline(),
            source.GetMinAfterBaseline());
    }

    static ColorBitmapComponent[] ConvertComponents(
        SfntBitmapGlyphData glyph)
    {
        ColorBitmapComponent[] components =
            new ColorBitmapComponent[glyph.GetComponentCount()];
        int index = 0;

        while (index < components.Length)
        {
            SfntBitmapComponent source = glyph.GetComponent(index);
            components[index] = new ColorBitmapComponent(
                source.GetGlyphId(),
                source.GetXOffset(),
                source.GetYOffset());
            index += 1;
        }
        return components;
    }

    static GlyphOutline? ResolveOutline(FontInstance instance, int glyphId)
    {
        FontOutlineKind kind = instance.GetFace().GetOutlineKind();

        if (kind == FontOutlineKind.Unknown || kind == FontOutlineKind.BitmapOnly)
        {
            return null;
        }

        return instance.GetGlyphOutline(glyphId);
    }

    static ColorGlyphRepresentation SelectPreferred(
        ColorGlyphRepresentation preferred,
        ColorPaintGraph? paintGraph,
        ColorSvgDocument? svgDocument,
        ColorBitmapStrike[] bitmapStrikes,
        GlyphOutline? outline)
    {
        if (IsAvailable(
            preferred,
            paintGraph,
            svgDocument,
            bitmapStrikes,
            outline))
        {
            return preferred;
        }

        if (paintGraph != null) { return ColorGlyphRepresentation.Colr; }
        if (svgDocument != null) { return ColorGlyphRepresentation.Svg; }
        if (FindBitmap(bitmapStrikes, ColorGlyphRepresentation.Cbdt, 0, 0) >= 0)
        {
            return ColorGlyphRepresentation.Cbdt;
        }
        if (FindBitmap(bitmapStrikes, ColorGlyphRepresentation.Sbix, 0, 0) >= 0)
        {
            return ColorGlyphRepresentation.Sbix;
        }
        if (FindBitmap(bitmapStrikes, ColorGlyphRepresentation.Ebdt, 0, 0) >= 0)
        {
            return ColorGlyphRepresentation.Ebdt;
        }
        if (outline != null) { return ColorGlyphRepresentation.MonochromeOutline; }
        return ColorGlyphRepresentation.None;
    }

    static bool IsAvailable(
        ColorGlyphRepresentation representation,
        ColorPaintGraph? paintGraph,
        ColorSvgDocument? svgDocument,
        ColorBitmapStrike[] bitmapStrikes,
        GlyphOutline? outline)
    {
        if (representation == ColorGlyphRepresentation.Colr)
        {
            return paintGraph != null;
        }
        if (representation == ColorGlyphRepresentation.Svg)
        {
            return svgDocument != null;
        }
        if (IsBitmap(representation))
        {
            return FindBitmap(bitmapStrikes, representation, 0, 0) >= 0;
        }
        if (representation == ColorGlyphRepresentation.MonochromeOutline)
        {
            return outline != null;
        }
        return false;
    }

    static bool IsBitmap(ColorGlyphRepresentation representation)
    {
        return representation == ColorGlyphRepresentation.Sbix ||
            representation == ColorGlyphRepresentation.Cbdt ||
            representation == ColorGlyphRepresentation.Ebdt;
    }

    static int FindBitmap(
        ColorBitmapStrike[] strikes,
        ColorGlyphRepresentation representation,
        int targetPixelsPerEmX,
        int targetPixelsPerEmY)
    {
        int bestIndex = -1;
        long bestDistance = 0;
        int index = 0;

        while (index < strikes.Length)
        {
            ColorBitmapStrike strike = strikes[index];

            if (strike.GetRepresentation() == representation &&
                strike.IsSupported())
            {
                long distance = 0;

                if (targetPixelsPerEmX > 0)
                {
                    long xDistance = (long)strike.GetPixelsPerEmX() -
                        targetPixelsPerEmX;
                    long yDistance = (long)strike.GetPixelsPerEmY() -
                        targetPixelsPerEmY;
                    if (xDistance < 0) { xDistance = -xDistance; }
                    if (yDistance < 0) { yDistance = -yDistance; }
                    distance = xDistance + yDistance;
                }

                if (bestIndex < 0 || distance < bestDistance)
                {
                    bestIndex = index;
                    bestDistance = distance;
                }
            }

            index += 1;
        }

        return bestIndex;
    }

    static PdfColorGlyphAppearance GetPdfGuidance(
        ColorGlyphRepresentation representation,
        ColorBitmapStrike[] bitmapStrikes,
        int selectedBitmapIndex)
    {
        if (representation == ColorGlyphRepresentation.Colr)
        {
            return PdfColorGlyphAppearance.DrawVectorAppearance;
        }
        if (representation == ColorGlyphRepresentation.Svg)
        {
            return PdfColorGlyphAppearance.DrawSvgAppearance;
        }
        if (representation == ColorGlyphRepresentation.MonochromeOutline)
        {
            return PdfColorGlyphAppearance.UseTextGlyph;
        }
        if (selectedBitmapIndex >= 0)
        {
            return bitmapStrikes[selectedBitmapIndex].ShouldDrawOutline() ?
                PdfColorGlyphAppearance.DrawImageAndOutlineAppearance :
                PdfColorGlyphAppearance.DrawImageAppearance;
        }
        return PdfColorGlyphAppearance.None;
    }
}
