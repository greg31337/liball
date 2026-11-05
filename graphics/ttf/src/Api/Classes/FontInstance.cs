namespace ttf;

/// <summary>Represents the default resolved outline and metric state of one font face.</summary>
public class FontInstance : IFontInstance
{
    FontFace face;
    int glyphCount;
    GlyphOutline[] outlines;
    GlyphMetrics[] glyphMetrics;
    SfntVariedGlyph[] trueTypeVariedGlyphs;
    SfntMetricVariationsLocation? horizontalMetricVariations;
    SfntMetricVariationsLocation? verticalMetricVariations;
    bool horizontalMetricVariationsResolved;
    bool verticalMetricVariationsResolved;
    string[] glyphNames;
    FontDesignMetrics? globalMetrics;
    NormalizedFontCoordinates coordinates;
    TrueTypeHintingData? trueTypeHintingData;
    byte[]? staticSfntBytes;
    object instanceSync;

    internal FontInstance(
        FontFace face,
        NormalizedFontCoordinates coordinates)
    {
        if (face == null)
        {
            throw new System.ArgumentNullException("face");
        }

        if (coordinates == null)
        {
            throw new System.ArgumentNullException("coordinates");
        }

        SfntMaxpTable? maxp = face.GetMaxpTable();

        if (maxp == null)
        {
            throw FontFormatException.Create(
                "A font instance requires a supported maxp table.");
        }

        this.face = face;
        this.coordinates = coordinates;
        glyphCount = maxp.GetGlyphCount();
        outlines = new GlyphOutline[glyphCount];
        glyphMetrics = new GlyphMetrics[glyphCount];
        trueTypeVariedGlyphs = new SfntVariedGlyph[glyphCount];
        glyphNames = new string[glyphCount];
        instanceSync = new object();
    }

    /// <summary>Gets the source face.</summary>
    /// <returns>The immutable source face.</returns>
    public FontFace GetFace() { return face; }

    /// <summary>Gets the immutable resolved variation coordinates.</summary>
    /// <returns>The clamped and normalized coordinate set.</returns>
    public NormalizedFontCoordinates GetCoordinates()
    {
        return coordinates;
    }

    /// <summary>Shapes a UTF-16 string with default options.</summary>
    /// <param name="text">The source text.</param>
    /// <returns>The immutable visual shaping runs and diagnostics.</returns>
    /// <exception cref="System.ArgumentNullException"><c>text</c> is null.</exception>
    /// <exception cref="System.Text.DecoderFallbackException"><c>text</c> contains malformed UTF-16.</exception>
    /// <exception cref="FontFormatException">Required font data is unavailable or malformed.</exception>
    /// <exception cref="FontLimitException">A shaping resource limit is exceeded.</exception>
    public TextShapingResult ShapeText(string text)
    {
        return TextShaper.Shape(text, this);
    }

    /// <summary>Shapes a UTF-16 string with explicit options.</summary>
    /// <param name="text">The source text.</param>
    /// <param name="options">The Unicode and OpenType shaping controls.</param>
    /// <returns>The immutable visual shaping runs and diagnostics.</returns>
    /// <exception cref="System.ArgumentNullException"><c>text</c> or <c>options</c> is null.</exception>
    /// <exception cref="System.Text.DecoderFallbackException"><c>text</c> contains malformed UTF-16.</exception>
    /// <exception cref="FontFormatException">Required font data is unavailable or malformed.</exception>
    /// <exception cref="FontLimitException">A shaping resource limit is exceeded.</exception>
    public TextShapingResult ShapeTextWithOptions(
        string text,
        TextShapingOptions options)
    {
        return TextShaper.Shape(text, this, options);
    }

    /// <summary>Shapes a UTF-16 character span with default options.</summary>
    /// <remarks>The span is copied before shaping and is never retained.</remarks>
    /// <param name="text">The source text span.</param>
    /// <returns>The immutable visual shaping runs and diagnostics.</returns>
    /// <exception cref="System.Text.DecoderFallbackException"><c>text</c> contains malformed UTF-16.</exception>
    /// <exception cref="FontFormatException">Required font data is unavailable or malformed.</exception>
    /// <exception cref="FontLimitException">A shaping resource limit is exceeded.</exception>
    public TextShapingResult ShapeSpan(ReadOnlySpan<char> text)
    {
        return TextShaper.Shape(text.ToString(), this);
    }

    /// <summary>Shapes a UTF-16 character span with explicit options.</summary>
    /// <remarks>The span is copied before shaping and is never retained.</remarks>
    /// <param name="text">The source text span.</param>
    /// <param name="options">The Unicode and OpenType shaping controls.</param>
    /// <returns>The immutable visual shaping runs and diagnostics.</returns>
    /// <exception cref="System.ArgumentNullException"><c>options</c> is null.</exception>
    /// <exception cref="System.Text.DecoderFallbackException"><c>text</c> contains malformed UTF-16.</exception>
    /// <exception cref="FontFormatException">Required font data is unavailable or malformed.</exception>
    /// <exception cref="FontLimitException">A shaping resource limit is exceeded.</exception>
    public TextShapingResult ShapeSpanWithOptions(
        ReadOnlySpan<char> text,
        TextShapingOptions options)
    {
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }
        return TextShaper.Shape(text.ToString(), this, options);
    }

    /// <summary>Gets the glyph count.</summary>
    /// <returns>The number of glyph identifiers in the instance.</returns>
    public int GetGlyphCount() { return glyphCount; }

    /// <summary>Gets preserved global TrueType hint data.</summary>
    /// <remarks>
    /// Programs and control values are returned for preservation and inspection.
    /// This library does not execute hints, grid-fit outlines, or rasterize glyphs.
    /// </remarks>
    /// <returns>The stable data view, or null for non-TrueType outlines.</returns>
    public TrueTypeHintingData? GetTrueTypeHintingData()
    {
        if (face.GetOutlineKind() != FontOutlineKind.TrueType)
        {
            return null;
        }

        lock (instanceSync)
        {
            if (trueTypeHintingData == null)
            {
                trueTypeHintingData = coordinates.IsDefaultUserLocation() ?
                    TrueTypeHintingData.Create(face) :
                    TrueTypeHintingData.Create(
                        face,
                        coordinates.GetNormalizedValues());
            }

            return trueTypeHintingData;
        }
    }

    /// <summary>Gets one resolved normalized glyph outline.</summary>
    /// <param name="glyphId">The zero-based glyph identifier.</param>
    /// <returns>The stable immutable glyph outline.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>glyphId</c> is outside the font.</exception>
    /// <exception cref="FontFormatException">The face does not contain supported outlines.</exception>
    public GlyphOutline GetGlyphOutline(int glyphId)
    {
        ValidateGlyphId(glyphId);

        lock (instanceSync)
        {
            if (outlines[glyphId] == null)
            {
                outlines[glyphId] = CreateGlyphOutline(glyphId);
            }

            return outlines[glyphId];
        }
    }

    /// <summary>Copies this instance as a standalone static SFNT font.</summary>
    /// <remarks>
    /// TrueType or CFF2 outlines and associated metrics are materialized.
    /// Variation-only tables and invalidated digital signatures are omitted
    /// from the returned font. CFF2 outlines are emitted as static CFF1.
    /// Repeated calls are byte-for-byte deterministic and return independent
    /// arrays.
    /// </remarks>
    /// <returns>A new array containing the static font bytes.</returns>
    /// <exception cref="FontFormatException">
    /// The face does not contain supported outlines or required variation data
    /// is unavailable or malformed.
    /// </exception>
    /// <exception cref="FontLimitException">
    /// The static font exceeds a configured resource limit.
    /// </exception>
    public byte[] CopyStaticSfntBytes()
    {
        lock (instanceSync)
        {
            if (staticSfntBytes == null)
            {
                staticSfntBytes = CopyStaticSfntBytesWithOptions(
                    new FontOperationOptions());
            }

            return staticSfntBytes.AsSpan().ToArray();
        }
    }

    /// <summary>Copies this instance as static SFNT using explicit operation controls.</summary>
    /// <param name="options">Policy, limits, and cancellation controls.</param>
    /// <returns>A new array containing the static font bytes.</returns>
    /// <exception cref="System.ArgumentNullException"><c>options</c> is null.</exception>
    /// <exception cref="System.OperationCanceledException">Cancellation was requested.</exception>
    /// <exception cref="FontLimitException">The output exceeds a configured limit.</exception>
    public byte[] CopyStaticSfntBytesWithOptions(
        FontOperationOptions options)
    {
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        FontOperationOptions snapshot = options.Copy();
        FontOperationContext.Checkpoint(snapshot);
        byte[] output;

        lock (instanceSync)
        {
            FontOutlineKind kind = face.GetOutlineKind();

            if (kind == FontOutlineKind.Cff2)
            {
                output = SfntStaticCff2Instancer.Create(this);
            }
            else if (kind == FontOutlineKind.Cff1)
            {
                output = SfntFaceWriter.Write(
                    face,
                    face.GetTableCache().GetLimits());
            }
            else
            {
                output = SfntStaticTrueTypeInstancer.Create(this);
            }
        }

        FontOperationContext.Checkpoint(snapshot);
        FontLimits effective = FontOperationContext.CreateEffectiveLimits(
            face.GetTableCache().GetLimits(),
            snapshot);
        FontOperationContext.ValidateOutputLength(output.Length, effective);
        return output;
    }

    /// <summary>Gets one glyph's exact normalized path bounds.</summary>
    /// <param name="glyphId">The zero-based glyph identifier.</param>
    /// <returns>The stable immutable exact bounds.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>glyphId</c> is outside the font.</exception>
    /// <exception cref="FontFormatException">The face does not contain supported outlines.</exception>
    public GlyphBounds? GetGlyphBounds(int glyphId)
    {
        return GetGlyphOutline(glyphId).GetBounds();
    }

    /// <summary>Measures one horizontal glyph at a point size.</summary>
    /// <param name="glyphId">The zero-based glyph identifier.</param>
    /// <param name="pointSize">The nonnegative finite point size.</param>
    /// <returns>The scaled advance and exact outline bounds.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>glyphId</c> is outside the font or <c>pointSize</c> is invalid.
    /// </exception>
    /// <exception cref="FontFormatException">
    /// Required outline, metric, or units-per-em data is unavailable.
    /// </exception>
    public TextMeasurement MeasureGlyph(int glyphId, double pointSize)
    {
        return TextMeasurer.MeasureGlyph(this, glyphId, pointSize);
    }

    /// <summary>Measures one horizontal glyph with point-based style adjustments.</summary>
    /// <param name="glyphId">The zero-based glyph identifier.</param>
    /// <param name="style">The point size and geometric style adjustments.</param>
    /// <returns>The adjusted advance and exact transformed outline bounds.</returns>
    /// <remarks>
    /// Character and word spacing require shaped source clusters and therefore
    /// do not affect this single-glyph measurement.
    /// </remarks>
    /// <exception cref="System.ArgumentNullException"><c>style</c> is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>glyphId</c> is outside the font.
    /// </exception>
    /// <exception cref="FontFormatException">
    /// Required outline, metric, or units-per-em data is unavailable.
    /// </exception>
    public TextMeasurement MeasureStyledGlyph(int glyphId, TextStyle style)
    {
        return TextMeasurer.MeasureGlyph(this, glyphId, style);
    }

    /// <summary>Measures a shaped run at a point size with exact positioned ink bounds.</summary>
    /// <param name="run">The immutable shaped run.</param>
    /// <param name="pointSize">The nonnegative finite point size.</param>
    /// <returns>The scaled advance vector and exact run ink bounds.</returns>
    /// <exception cref="System.ArgumentNullException"><c>run</c> is null.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>pointSize</c> is invalid.</exception>
    /// <exception cref="FontFormatException">Required outline or metric data is unavailable.</exception>
    public TextMeasurement MeasureRun(ShapedRun run, double pointSize)
    {
        return TextMeasurer.MeasureRun(run, this, pointSize);
    }

    /// <summary>Measures a shaped run with point-based style adjustments.</summary>
    /// <param name="run">The immutable shaped run.</param>
    /// <param name="style">The point size and geometric style adjustments.</param>
    /// <returns>The adjusted advance vector and exact transformed run ink bounds.</returns>
    /// <exception cref="System.ArgumentNullException"><c>run</c> or <c>style</c> is null.</exception>
    /// <exception cref="FontFormatException">Required outline or metric data is unavailable.</exception>
    public TextMeasurement MeasureStyledRun(ShapedRun run, TextStyle style)
    {
        return TextMeasurer.MeasureRun(run, this, style);
    }

    /// <summary>Gets one glyph's PostScript name or a stable synthetic name.</summary>
    /// <param name="glyphId">The zero-based glyph identifier.</param>
    /// <returns>The parsed or synthetic glyph name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>glyphId</c> is outside the font.</exception>
    public string GetGlyphName(int glyphId)
    {
        ValidateGlyphId(glyphId);

        lock (instanceSync)
        {
            if (glyphNames[glyphId] == null)
            {
                SfntPostTable? post = face.GetPostTable();
                glyphNames[glyphId] = post == null ?
                    CreateSyntheticGlyphName(glyphId) :
                    post.GetGlyphName(glyphId);
            }

            return glyphNames[glyphId];
        }
    }

    /// <summary>Gets one glyph's resolved metrics in design units.</summary>
    /// <param name="glyphId">The zero-based glyph identifier.</param>
    /// <returns>The stable design-unit glyph metrics.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><c>glyphId</c> is outside the font.</exception>
    /// <exception cref="FontFormatException">Required outline or metric tables are unavailable.</exception>
    public GlyphMetrics GetGlyphMetrics(int glyphId)
    {
        ValidateGlyphId(glyphId);

        lock (instanceSync)
        {
            if (glyphMetrics[glyphId] == null)
            {
                GlyphOutline outline = GetGlyphOutline(glyphId);
                SfntVariedGlyph varied = trueTypeVariedGlyphs[glyphId];
                glyphMetrics[glyphId] = CreateGlyphMetrics(
                    glyphId,
                    outline,
                    varied);
            }

            return glyphMetrics[glyphId];
        }
    }

    GlyphMetrics CreateGlyphMetrics(
        int glyphId,
        GlyphOutline outline,
        SfntVariedGlyph varied)
    {
        GlyphBounds? bounds = outline.GetBounds();
        System.ArgumentNullException.ThrowIfNull(bounds);
        int glyphYMaximum = bounds.IsEmpty() ? 0 :
            RoundMaximum(bounds.GetYMaximum());
        GlyphMetrics metrics = varied == null ?
            FontMetricResolver.CreateGlyph(
                face,
                glyphId,
                outline.GetMetricsGlyphId(),
                glyphYMaximum) :
            new GlyphMetrics(
                glyphId,
                RoundMetric(varied.GetAdvanceWidth()),
                RoundMetric(varied.GetLeftSideBearing()),
                RoundMetric(varied.GetAdvanceHeight()),
                RoundMetric(varied.GetTopSideBearing()),
                RoundMetric(varied.GetVerticalOriginY()),
                varied.UsesSynthesizedVerticalAdvance(),
                varied.UsesVorgOrigin());

        if (coordinates.IsDefaultUserLocation())
        {
            return metrics;
        }

        SfntMetricVariationsLocation? horizontal =
            GetHorizontalMetricVariations();
        SfntMetricVariationsLocation? vertical =
            GetVerticalMetricVariations();

        if (horizontal == null && vertical == null)
        {
            return metrics;
        }

        int metricsGlyphId = outline.GetMetricsGlyphId();
        int advanceWidth = metrics.GetAdvanceWidth();
        int leftSideBearing = metrics.GetLeftSideBearing();
        int advanceHeight = metrics.GetAdvanceHeight();
        int topSideBearing = metrics.GetTopSideBearing();
        int verticalOriginY = metrics.GetVerticalOriginY();
        bool synthesizedAdvance = metrics.UsesSynthesizedVerticalAdvance();
        bool usesVorg = metrics.UsesVorgOrigin();

        if (horizontal != null)
        {
            SfntHmtxTable? hmtx = face.GetHmtxTable();
            System.ArgumentNullException.ThrowIfNull(hmtx);
            advanceWidth = RoundMetric(
                hmtx.GetAdvanceWidth(metricsGlyphId) +
                horizontal.CalculateAdvanceDelta(glyphId));

            if (horizontal.HasLeadingBearingVariations())
            {
                leftSideBearing = RoundMetric(
                    hmtx.GetLeftSideBearing(metricsGlyphId) +
                    horizontal.CalculateLeadingBearingDelta(glyphId));
            }
        }

        if (vertical != null)
        {
            SfntVmtxTable? vmtx = face.GetVmtxTable();
            System.ArgumentNullException.ThrowIfNull(vmtx);
            advanceHeight = RoundMetric(
                vmtx.GetAdvanceHeight(metricsGlyphId) +
                vertical.CalculateAdvanceDelta(glyphId));
            synthesizedAdvance = false;

            if (vertical.HasLeadingBearingVariations())
            {
                topSideBearing = RoundMetric(
                    vmtx.GetTopSideBearing(metricsGlyphId) +
                    vertical.CalculateLeadingBearingDelta(glyphId));

                if (face.GetOutlineKind() != FontOutlineKind.Cff2 ||
                    face.GetVorgTable() == null)
                {
                    verticalOriginY = RoundMetric(
                        (double)glyphYMaximum + topSideBearing);
                    usesVorg = false;
                }
            }

            if (face.GetOutlineKind() == FontOutlineKind.Cff2)
            {
                SfntVorgTable? vorg = face.GetVorgTable();

                if (vorg != null &&
                    vertical.HasVerticalOriginVariations())
                {
                    verticalOriginY = RoundMetric(
                        vorg.GetVerticalOriginY(metricsGlyphId) +
                        vertical.CalculateVerticalOriginDelta(glyphId));
                    usesVorg = true;
                }
            }
        }

        return new GlyphMetrics(
            glyphId,
            advanceWidth,
            leftSideBearing,
            advanceHeight,
            topSideBearing,
            verticalOriginY,
            synthesizedAdvance,
            usesVorg);
    }

    SfntMetricVariationsLocation? GetHorizontalMetricVariations()
    {
        if (!horizontalMetricVariationsResolved)
        {
            SfntMetricVariationsTable? table = face.GetHvarTable();

            if (table != null)
            {
                horizontalMetricVariations = table.CreateLocation(
                    coordinates.GetNormalizedValues());
            }
            else if (face.HasTable(FontTags.Hvar))
            {
                throw FontFormatException.Create(
                    "Varied horizontal metrics require a supported HVAR table.");
            }

            horizontalMetricVariationsResolved = true;
        }

        return horizontalMetricVariations;
    }

    SfntMetricVariationsLocation? GetVerticalMetricVariations()
    {
        if (!verticalMetricVariationsResolved)
        {
            SfntMetricVariationsTable? table = face.GetVvarTable();

            if (table != null)
            {
                verticalMetricVariations = table.CreateLocation(
                    coordinates.GetNormalizedValues());
            }
            else if (face.HasTable(FontTags.Vvar))
            {
                throw FontFormatException.Create(
                    "Varied vertical metrics require a supported VVAR table.");
            }

            verticalMetricVariationsResolved = true;
        }

        return verticalMetricVariations;
    }

    GlyphOutline CreateGlyphOutline(int glyphId)
    {
        FontOutlineKind kind = face.GetOutlineKind();

        if (kind == FontOutlineKind.TrueType)
        {
            SfntSimpleGlyph? simple = face.GetSimpleGlyph(glyphId);
            SfntCompositeGlyph? composite = simple == null
                ? face.GetCompositeGlyph(glyphId)
                : null;

            if (!coordinates.IsDefaultUserLocation() &&
                face.HasTable(FontTags.Gvar))
            {
                SfntVariedGlyph varied = SfntGvarGlyphResolver.Resolve(
                    face,
                    glyphId,
                    coordinates.GetNormalizedValues(),
                    trueTypeVariedGlyphs);
                trueTypeVariedGlyphs[glyphId] = varied;
                return GlyphOutline.Create(varied, simple, composite);
            }

            SfntResolvedGlyph resolved = face.GetResolvedGlyph(glyphId);
            return GlyphOutline.Create(resolved, simple, composite);
        }

        if (kind == FontOutlineKind.Cff1)
        {
            SfntCffTable? table = face.GetCffTable();

            if (table == null || table.GetCharStringsIndex().GetCount() != glyphCount)
            {
                throw FontFormatException.Create(
                    "CFF glyph outlines do not match the maxp glyph count.");
            }

            return GlyphOutline.Create(table.GetNormalizedGlyph(glyphId));
        }

        if (kind == FontOutlineKind.Cff2)
        {
            SfntCff2Table? table = face.GetCff2Table();

            if (table == null || table.GetCharStringsIndex().GetCount() != glyphCount)
            {
                throw FontFormatException.Create(
                    "CFF2 glyph outlines do not match the maxp glyph count.");
            }

            return GlyphOutline.Create(table.GetNormalizedGlyph(
                glyphId,
                GetNormalizedCoordinates(table)));
        }

        throw FontFormatException.Create(
            "The font instance does not contain a supported outline table.");
    }

    double[] GetNormalizedCoordinates(SfntCff2Table table)
    {
        Cff2VariationStore? store = table.GetVariationData();
        int expectedCount = store == null ? 0 : store.GetAxisCount();

        if (coordinates.GetAxisCount() != expectedCount)
        {
            throw FontFormatException.Create(
                "CFF2 variation axes do not match the resolved coordinates.");
        }

        return coordinates.GetNormalizedValues();
    }

    int RoundMaximum(double value)
    {
        if (!double.IsFinite(value) ||
            value < int.MinValue || value > int.MaxValue)
        {
            throw FontFormatException.Create(
                "A glyph bound is outside the supported metric range.");
        }

        return (int)System.Math.Ceiling(value);
    }

    int RoundMetric(double value)
    {
        if (!double.IsFinite(value) ||
            value < int.MinValue || value > int.MaxValue)
        {
            throw FontFormatException.Create(
                "A varied glyph metric is outside the supported range.");
        }

        return (int)System.Math.Round(
            value,
            System.MidpointRounding.AwayFromZero);
    }

    string CreateSyntheticGlyphName(int glyphId)
    {
        return "gid" + glyphId.ToString(
            "D4",
            System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>Gets recommended global metrics in design units.</summary>
    /// <returns>The stable recommended global metric result.</returns>
    /// <exception cref="FontFormatException">Required global metric tables are unavailable.</exception>
    public FontDesignMetrics GetGlobalMetrics()
    {
        lock (instanceSync)
        {
            if (globalMetrics == null)
            {
                FontDesignMetrics resolved = face.GetDesignMetrics(
                    FontMetricSelectionPolicy.OpenTypeRecommended);

                if (!coordinates.IsDefaultUserLocation())
                {
                    SfntMvarTable? mvar = face.GetMvarTable();

                    if (mvar != null)
                    {
                        resolved = resolved.ApplyVariations(
                            mvar.CreateLocation(
                                coordinates.GetNormalizedValues()));
                    }
                    else if (face.HasTable(FontTags.Mvar))
                    {
                        throw FontFormatException.Create(
                            "Varied global metrics require a supported MVAR table.");
                    }
                }

                globalMetrics = resolved;
            }

            return globalMetrics;
        }
    }

    void ValidateGlyphId(int glyphId)
    {
        if (glyphId < 0 || glyphId >= glyphCount)
        {
            throw new System.ArgumentOutOfRangeException("glyphId");
        }
    }

    internal SfntVariedGlyph GetTrueTypeVariedGlyph(int glyphId)
    {
        ValidateGlyphId(glyphId);

        if (face.GetOutlineKind() != FontOutlineKind.TrueType)
        {
            throw FontFormatException.Create(
                "A varied TrueType glyph requires TrueType outlines.");
        }

        lock (instanceSync)
        {
            if (trueTypeVariedGlyphs[glyphId] == null)
            {
                trueTypeVariedGlyphs[glyphId] = SfntGvarGlyphResolver.Resolve(
                    face,
                    glyphId,
                    coordinates.GetNormalizedValues(),
                    trueTypeVariedGlyphs);
            }

            return trueTypeVariedGlyphs[glyphId];
        }
    }
}
