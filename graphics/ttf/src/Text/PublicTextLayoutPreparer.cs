namespace ttf;

internal static class PublicTextLayoutPreparer
{
    internal static PreparedTextResult Prepare(
        FontSet fontSet,
        string text,
        TextLayoutStyle[] styles,
        TextLayoutOptions options)
    {
        ValidateArguments(fontSet, text, styles, options);
        options = options.Copy();
        FontOperationOptions operation = options.GetOperationSettings();
        FontOperationContext.Checkpoint(operation);
        FontLimits retainedLimits = fontSet.GetFaceCount() == 0 ?
            FontLimits.CreateCompatible() :
            fontSet.GetFace(0).GetTableCache().GetLimits();
        FontLimits limits = FontOperationContext.CreateEffectiveLimits(
            retainedLimits,
            operation);
        RuneBuffer input = Utf16Decoder.Decode(
            text,
            Utf16DecodingMode.Strict,
            limits.GetMaxGlyphsPerFace());
        UnicodeData unicodeData = UnicodeData.LoadDefault();
        GraphemeClusterMap clusters = GraphemeSegmenter.Segment(
            input,
            unicodeData,
            input.GetCount(),
            input.GetCount());
        ValidateStyles(text, styles, clusters);
        TextLayoutStyle[] styleSnapshot = new TextLayoutStyle[styles.Length];
        int snapshotIndex = 0;
        while (snapshotIndex < styles.Length)
        {
            styleSnapshot[snapshotIndex] = styles[snapshotIndex].Copy();
            snapshotIndex += 1;
        }
        styles = styleSnapshot;
        LineBreakMap lineBreaks = LineBreakAlgorithm.Analyze(
            input,
            unicodeData,
            input.GetCount());
        TextBreakMap breaks = TextBreakPlanner.Plan(
            input,
            clusters,
            lineBreaks,
            options.AllowsEmergencyBreaks(),
            clusters.GetClusterCount());
        double[] clusterAdvances = new double[clusters.GetClusterCount()];
        int[] sourceRunIndexes = CreateRunIndexes(clusters.GetClusterCount());
        int[] bidiLevels = new int[clusters.GetClusterCount()];
        System.Collections.Generic.List<PreparedRunMetrics> runMetrics =
            new System.Collections.Generic.List<PreparedRunMetrics>();
        System.Collections.Generic.List<FontDiagnostic> diagnostics =
            new System.Collections.Generic.List<FontDiagnostic>();
        int sourceRunIndex = 0;
        double lineStep = 0;
        int styleIndex = 0;
        while (styleIndex < styles.Length)
        {
            FontOperationContext.Checkpoint(operation);
            TextLayoutStyle layoutStyle = styles[styleIndex];
            string rangeText = text.Substring(
                layoutStyle.GetUtf16Start(),
                layoutStyle.GetUtf16Length());
            TextShapingResult shaping = fontSet.ShapeTextWithOptions(
                rangeText,
                layoutStyle.GetFontRequest(),
                CreateShapingOptions(options.GetWritingMode(), operation));
            AddDiagnostics(shaping, diagnostics, limits);
            int runIndex = 0;
            while (runIndex < shaping.GetRunCount())
            {
                TextShapingRun run = shaping.GetRun(runIndex);
                AssignRunClusters(
                    breaks,
                    run,
                    layoutStyle.GetUtf16Start(),
                    sourceRunIndex,
                    sourceRunIndexes,
                    bidiLevels);
                PreparedRunMetrics metrics = BuildRunMetrics(
                    breaks,
                    run,
                    layoutStyle.GetUtf16Start(),
                    layoutStyle.GetTextStyle(),
                    options,
                    sourceRunIndex,
                    clusterAdvances);
                runMetrics.Add(metrics);
                double runLineStep = metrics.GetAscent() +
                    metrics.GetDescent() + metrics.GetLineGap();
                if (runLineStep > lineStep)
                {
                    lineStep = runLineStep;
                }
                MarkUnsafeBoundaries(
                    breaks,
                    run.GetShapedRun(),
                    layoutStyle.GetUtf16Start());
                sourceRunIndex += 1;
                runIndex += 1;
            }
            styleIndex += 1;
        }

        ApplySpecialClusterAdvances(breaks, clusterAdvances, options);
        ApplyHyphenation(text, input, breaks, options);

        double hyphenAdvance = styles.Length == 0 ? 0 : MeasureAuxiliary(
            fontSet,
            "-",
            styles[0],
            options.GetWritingMode(),
            operation);
        double ellipsisAdvance = styles.Length == 0 ? 0 : MeasureAuxiliary(
            fontSet,
            options.GetEllipsisText(),
            styles[styles.Length - 1],
            options.GetWritingMode(),
            operation);
        PreparedRunMetrics[] metricArray =
            new PreparedRunMetrics[runMetrics.Count];
        runMetrics.CopyTo(metricArray, 0);
        PreparedText prepared = PreparedText.Create(
            text,
            breaks,
            clusterAdvances,
            sourceRunIndexes,
            bidiLevels,
            options,
            options.CopyJustificationOpportunities(),
            metricArray,
            hyphenAdvance,
            ellipsisAdvance,
            lineStep);
        return new PreparedTextResult(
            prepared,
            text,
            styles.Length,
            clusters.GetClusterCount(),
            sourceRunIndex,
            options,
            operation,
            diagnostics.ToArray());
    }

    static PreparedRunMetrics BuildRunMetrics(
        TextBreakMap breaks,
        TextShapingRun run,
        int styleStart,
        TextStyle style,
        TextLayoutOptions options,
        int sourceRunIndex,
        double[] clusterAdvances)
    {
        FontInstance instance = run.GetFontInstance();
        FontDesignMetrics designMetrics = instance.GetGlobalMetrics();
        int unitsPerEm = designMetrics.GetUnitsPerEm();
        double pointSize = style.GetPointSize();
        TextWritingMode writingMode = options.GetWritingMode();
        double ascent = Nonnegative(Scale(
            designMetrics.GetAscender(),
            pointSize,
            unitsPerEm));
        double descent = Nonnegative(-Scale(
            designMetrics.GetDescender(),
            pointSize,
            unitsPerEm));
        double lineGap = Nonnegative(Scale(
            designMetrics.GetLineGap(),
            pointSize,
            unitsPerEm));
        ShapedRun shaped = run.GetShapedRun();
        PreparedGlyphMetrics[] glyphs =
            new PreparedGlyphMetrics[shaped.GetGlyphCount()];
        double penX = 0;
        double penY = 0;
        int nextCluster = glyphs.Length == 0 ?
            0 : FindCluster(breaks, styleStart + run.GetUtf16Start());
        int glyphIndex = 0;
        while (glyphIndex < glyphs.Length)
        {
            ShapedGlyph glyph = shaped.GetGlyph(glyphIndex);
            int utf16Start = styleStart + glyph.GetUtf16Start();
            int utf16End = utf16Start + glyph.GetUtf16Length();
            int clusterStart = FindCluster(breaks, utf16Start);
            int clusterEnd = FindClusterEnd(breaks, utf16End, clusterStart);
            while (nextCluster < clusterStart)
            {
                double skippedAdvance = breaks.IsTab(nextCluster) ?
                    options.GetTabWidth() : 0;
                if (writingMode == TextWritingMode.Horizontal)
                {
                    penX += skippedAdvance;
                }
                else
                {
                    penY += skippedAdvance;
                }
                nextCluster += 1;
            }
            double xAdvance = Scale(
                glyph.GetXAdvance(),
                pointSize,
                unitsPerEm) * style.GetHorizontalScale();
            double yAdvance = Scale(
                glyph.GetYAdvance(),
                pointSize,
                unitsPerEm);
            bool special = breaks.IsTab(clusterStart) ||
                breaks.IsHardBreak(clusterStart) ||
                breaks.IsSoftHyphen(clusterStart);
            if (special)
            {
                bool clusterEndGlyph = IsClusterEnd(shaped, glyphIndex);
                double specialAdvance = breaks.IsTab(clusterStart) &&
                    clusterEndGlyph ? options.GetTabWidth() : 0;
                if (writingMode == TextWritingMode.Horizontal)
                {
                    xAdvance = specialAdvance;
                    yAdvance = 0;
                }
                else
                {
                    xAdvance = 0;
                    yAdvance = specialAdvance;
                }
            }
            double x = penX + Scale(
                glyph.GetXOffset(),
                pointSize,
                unitsPerEm) * style.GetHorizontalScale();
            double y = penY + Scale(
                glyph.GetYOffset(),
                pointSize,
                unitsPerEm);
            TextInkBounds ink = TextMeasurer.MeasureGlyph(
                instance,
                glyph.GetGlyphId(),
                style).GetInkBounds();
            if (special)
            {
                ink = TextInkBounds.Create();
            }
            glyphs[glyphIndex] = PreparedGlyphMetrics.Create(
                glyph.GetGlyphId(),
                clusterStart,
                clusterEnd,
                glyph.GetLogicalIndex(),
                glyphIndex,
                x,
                y,
                xAdvance,
                yAdvance,
                ink);
            double inlineAdvance = GetInlineAdvance(
                xAdvance,
                yAdvance,
                writingMode);
            clusterAdvances[clusterStart] += inlineAdvance;
            penX += xAdvance;
            penY += yAdvance;
            if (IsClusterEnd(shaped, glyphIndex) && !special)
            {
                ShapedCluster cluster = shaped.GetCluster(
                    glyph.GetClusterIndex());
                double spacing = style.GetCharacterSpacing();
                if (IsWordSpace(cluster))
                {
                    spacing += style.GetWordSpacing();
                }
                if (writingMode == TextWritingMode.Horizontal)
                {
                    spacing *= style.GetHorizontalScale();
                }
                clusterAdvances[clusterStart] += Nonnegative(spacing);
                if (writingMode == TextWritingMode.Horizontal)
                {
                    penX += spacing;
                }
                else if (writingMode == TextWritingMode.VerticalTopToBottom)
                {
                    penY -= spacing;
                }
                else
                {
                    penY += spacing;
                }
            }
            if (clusterEnd > nextCluster)
            {
                nextCluster = clusterEnd;
            }
            glyphIndex += 1;
        }
        return new PreparedRunMetrics(
            sourceRunIndex,
            instance,
            ascent,
            descent,
            lineGap,
            glyphs);
    }

    static void ApplyHyphenation(
        string text,
        RuneBuffer input,
        TextBreakMap breaks,
        TextLayoutOptions options)
    {
        if (!options.HasHyphenation())
        {
            return;
        }

        int clusterIndex = 0;
        while (clusterIndex < breaks.GetClusterCount())
        {
            if (!IsWordCluster(input, breaks, clusterIndex))
            {
                clusterIndex += 1;
                continue;
            }

            int wordStart = clusterIndex;
            while (clusterIndex < breaks.GetClusterCount() &&
                IsWordCluster(input, breaks, clusterIndex))
            {
                clusterIndex += 1;
            }

            int wordEnd = clusterIndex;
            int utf16Start = breaks.GetUtf16Start(wordStart);
            int finalCluster = wordEnd - 1;
            int utf16End = breaks.GetUtf16Start(finalCluster) +
                breaks.GetUtf16Length(finalCluster);
            string word = text.Substring(utf16Start, utf16End - utf16Start);
            HyphenationMatch match = options.GetHyphenation().Match(word);
            int pointIndex = 0;
            while (pointIndex < match.GetPointCount())
            {
                int position = utf16Start + match.GetUtf16Index(pointIndex);
                MarkHyphenationAtPosition(
                    breaks,
                    wordStart,
                    wordEnd,
                    position);
                pointIndex += 1;
            }
        }
    }

    static void ApplySpecialClusterAdvances(
        TextBreakMap breaks,
        double[] clusterAdvances,
        TextLayoutOptions options)
    {
        int clusterIndex = 0;
        while (clusterIndex < breaks.GetClusterCount())
        {
            if (breaks.IsTab(clusterIndex))
            {
                clusterAdvances[clusterIndex] = options.GetTabWidth();
            }
            else if (breaks.IsHardBreak(clusterIndex) ||
                breaks.IsSoftHyphen(clusterIndex))
            {
                clusterAdvances[clusterIndex] = 0;
            }
            clusterIndex += 1;
        }
    }

    static void MarkHyphenationAtPosition(
        TextBreakMap breaks,
        int wordStart,
        int wordEnd,
        int utf16Position)
    {
        int boundary = wordStart + 1;
        while (boundary < wordEnd)
        {
            if (breaks.GetUtf16Start(boundary) == utf16Position)
            {
                breaks.MarkHyphenationBoundary(boundary);
                return;
            }
            boundary += 1;
        }
    }

    static bool IsWordCluster(
        RuneBuffer input,
        TextBreakMap breaks,
        int clusterIndex)
    {
        int runeIndex = breaks.GetRuneStart(clusterIndex);
        int runeEnd = runeIndex + breaks.GetRuneLength(clusterIndex);
        bool containsLetter = false;
        while (runeIndex < runeEnd)
        {
            System.Globalization.UnicodeCategory category =
                System.Text.Rune.GetUnicodeCategory(input.GetRune(runeIndex));
            if (IsLetterCategory(category))
            {
                containsLetter = true;
            }
            else if (!IsMarkCategory(category))
            {
                return false;
            }
            runeIndex += 1;
        }
        return containsLetter;
    }

    static bool IsLetterCategory(System.Globalization.UnicodeCategory category)
    {
        return category == System.Globalization.UnicodeCategory.UppercaseLetter ||
            category == System.Globalization.UnicodeCategory.LowercaseLetter ||
            category == System.Globalization.UnicodeCategory.TitlecaseLetter ||
            category == System.Globalization.UnicodeCategory.ModifierLetter ||
            category == System.Globalization.UnicodeCategory.OtherLetter;
    }

    static bool IsMarkCategory(System.Globalization.UnicodeCategory category)
    {
        return category == System.Globalization.UnicodeCategory.NonSpacingMark ||
            category == System.Globalization.UnicodeCategory.SpacingCombiningMark ||
            category == System.Globalization.UnicodeCategory.EnclosingMark;
    }

    static void AssignRunClusters(
        TextBreakMap breaks,
        TextShapingRun run,
        int styleStart,
        int sourceRunIndex,
        int[] sourceRunIndexes,
        int[] bidiLevels)
    {
        int runStart = styleStart + run.GetUtf16Start();
        int runEnd = runStart + run.GetUtf16Length();
        int clusterIndex = 0;
        while (clusterIndex < breaks.GetClusterCount())
        {
            int clusterStart = breaks.GetUtf16Start(clusterIndex);
            int clusterEnd = clusterStart + breaks.GetUtf16Length(clusterIndex);
            if (clusterStart >= runStart && clusterEnd <= runEnd)
            {
                sourceRunIndexes[clusterIndex] = sourceRunIndex;
                bidiLevels[clusterIndex] = run.GetBidiLevel();
            }
            clusterIndex += 1;
        }
    }

    static void MarkUnsafeBoundaries(
        TextBreakMap breaks,
        ShapedRun run,
        int styleStart)
    {
        int boundary = 1;
        while (boundary < breaks.GetClusterCount())
        {
            int position = breaks.GetUtf16Start(boundary);
            int clusterIndex = 0;
            while (clusterIndex < run.GetClusterCount())
            {
                ShapedCluster cluster = run.GetCluster(clusterIndex);
                int start = styleStart + cluster.GetUtf16Start();
                int end = start + cluster.GetUtf16Length();
                if ((position > start && position < end) ||
                    (position == start && cluster.IsUnsafeToBreak()))
                {
                    breaks.MarkUnsafeBoundary(boundary);
                }
                clusterIndex += 1;
            }
            boundary += 1;
        }
    }

    static double MeasureAuxiliary(
        FontSet fontSet,
        string text,
        TextLayoutStyle style,
        TextWritingMode writingMode,
        FontOperationOptions operation)
    {
        TextMeasurement measurement = fontSet.ShapeTextWithOptions(
            text,
            style.GetFontRequest(),
            CreateShapingOptions(writingMode, operation)).MeasureWithStyle(
                style.GetTextStyle());
        return GetInlineAdvance(
            measurement.GetXAdvance(),
            measurement.GetYAdvance(),
            writingMode);
    }

    static TextShapingOptions CreateShapingOptions(
        TextWritingMode writingMode,
        FontOperationOptions operation)
    {
        TextShapingOptions options = new TextShapingOptions();
        options.SetOperationOptions(operation);
        if (writingMode == TextWritingMode.VerticalTopToBottom)
        {
            options.SetDirection(TextShapingDirection.TopToBottom);
        }
        else if (writingMode == TextWritingMode.VerticalBottomToTop)
        {
            options.SetDirection(TextShapingDirection.BottomToTop);
        }
        return options;
    }

    static void AddDiagnostics(
        TextShapingResult shaping,
        System.Collections.Generic.List<FontDiagnostic> diagnostics,
        FontLimits limits)
    {
        int index = 0;

        while (index < shaping.GetDiagnosticCount())
        {
            if (diagnostics.Count == limits.GetMaxDiagnostics())
            {
                throw FontLimitException.CreateDiagnostic(
                    "Text layout exceeds the configured diagnostic limit.",
                    FontDiagnosticCode.LimitExceeded);
            }

            diagnostics.Add(shaping.GetDiagnostic(index));
            index += 1;
        }
    }

    static double GetInlineAdvance(
        double xAdvance,
        double yAdvance,
        TextWritingMode writingMode)
    {
        double value = writingMode == TextWritingMode.Horizontal ?
            xAdvance : yAdvance;
        if (value == 0)
        {
            value = xAdvance;
        }
        return System.Math.Abs(value);
    }

    static int FindCluster(TextBreakMap breaks, int utf16Index)
    {
        int clusterIndex = 0;
        while (clusterIndex < breaks.GetClusterCount())
        {
            int start = breaks.GetUtf16Start(clusterIndex);
            int end = start + breaks.GetUtf16Length(clusterIndex);
            if (utf16Index >= start && utf16Index < end)
            {
                return clusterIndex;
            }
            clusterIndex += 1;
        }
        throw FontFormatException.Create(
            "A shaped glyph is outside the prepared source clusters.");
    }

    static int FindClusterEnd(
        TextBreakMap breaks,
        int utf16End,
        int clusterStart)
    {
        int clusterIndex = clusterStart;
        while (clusterIndex < breaks.GetClusterCount())
        {
            int end = breaks.GetUtf16Start(clusterIndex) +
                breaks.GetUtf16Length(clusterIndex);
            if (end >= utf16End)
            {
                return clusterIndex + 1;
            }
            clusterIndex += 1;
        }
        return breaks.GetClusterCount();
    }

    static bool IsClusterEnd(ShapedRun run, int glyphIndex)
    {
        if (glyphIndex + 1 == run.GetGlyphCount())
        {
            return true;
        }
        return run.GetGlyph(glyphIndex).GetClusterIndex() !=
            run.GetGlyph(glyphIndex + 1).GetClusterIndex();
    }

    static bool IsWordSpace(ShapedCluster cluster)
    {
        int index = 0;
        while (index < cluster.GetUnicodeScalarCount())
        {
            int scalar = cluster.GetUnicodeScalar(index);
            if (scalar == 0x0020 || scalar == 0x00A0 ||
                scalar == 0x1680 ||
                (scalar >= 0x2000 && scalar <= 0x200A) ||
                scalar == 0x202F || scalar == 0x205F || scalar == 0x3000)
            {
                return true;
            }
            index += 1;
        }
        return false;
    }

    static double Scale(double value, double pointSize, int unitsPerEm)
    {
        if (unitsPerEm <= 0)
        {
            throw FontFormatException.Create(
                "Prepared text requires a positive units-per-em value.");
        }
        double scaled = value * pointSize / unitsPerEm;
        if (!double.IsFinite(scaled))
        {
            throw FontFormatException.Create(
                "Prepared text metrics exceed finite layout coordinates.");
        }
        return scaled;
    }

    static double Nonnegative(double value)
    {
        return value < 0 ? 0 : value;
    }

    static int[] CreateRunIndexes(int count)
    {
        int[] values = new int[count];
        int index = 0;
        while (index < values.Length)
        {
            values[index] = -1;
            index += 1;
        }
        return values;
    }

    static void ValidateArguments(
        FontSet fontSet,
        string text,
        TextLayoutStyle[] styles,
        TextLayoutOptions options)
    {
        if (fontSet == null)
        {
            throw new System.ArgumentNullException("fontSet");
        }
        if (text == null)
        {
            throw new System.ArgumentNullException("text");
        }
        if (styles == null)
        {
            throw new System.ArgumentNullException("styles");
        }
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }
    }

    static void ValidateStyles(
        string text,
        TextLayoutStyle[] styles,
        GraphemeClusterMap clusters)
    {
        if (text.Length == 0)
        {
            if (styles.Length != 0)
            {
                throw new System.ArgumentException(
                    "Empty text cannot contain style ranges.",
                    "styles");
            }
            return;
        }

        bool[] boundaries = new bool[text.Length + 1];
        boundaries[0] = true;
        boundaries[text.Length] = true;
        int clusterIndex = 0;
        while (clusterIndex < clusters.GetClusterCount())
        {
            int start = clusters.GetUtf16Start(clusterIndex);
            boundaries[start] = true;
            boundaries[start + clusters.GetUtf16Length(clusterIndex)] = true;
            clusterIndex += 1;
        }

        int expectedStart = 0;
        int styleIndex = 0;
        while (styleIndex < styles.Length)
        {
            TextLayoutStyle style = styles[styleIndex];
            if (style == null)
            {
                throw new System.ArgumentException(
                    "A text layout style cannot be null.",
                    "styles");
            }
            int start = style.GetUtf16Start();
            int end = start + style.GetUtf16Length();
            if (start != expectedStart || end > text.Length ||
                !boundaries[start] || !boundaries[end])
            {
                throw new System.ArgumentException(
                    "Styles must cover the source exactly at grapheme boundaries.",
                    "styles");
            }
            expectedStart = end;
            styleIndex += 1;
        }
        if (expectedStart != text.Length)
        {
            throw new System.ArgumentException(
                "Styles must cover the source exactly at grapheme boundaries.",
                "styles");
        }
    }
}
