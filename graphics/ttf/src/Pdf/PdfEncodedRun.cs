namespace ttf;

internal class PdfPositionedRunData
{
    PdfPositionedGlyph[] glyphs;
    PdfTjAdjustment[] adjustments;
    double xAdvance;
    double yAdvance;

    internal PdfPositionedRunData(
        PdfPositionedGlyph[] glyphs,
        PdfTjAdjustment[] adjustments,
        double xAdvance,
        double yAdvance)
    {
        this.glyphs = glyphs;
        this.adjustments = adjustments;
        this.xAdvance = xAdvance;
        this.yAdvance = yAdvance;
    }

    internal PdfPositionedGlyph[] GetGlyphs() { return glyphs; }
    internal PdfTjAdjustment[] GetAdjustments() { return adjustments; }
    internal double GetXAdvance() { return xAdvance; }
    internal double GetYAdvance() { return yAdvance; }
}

internal static class PdfFontRunEncoder
{
    internal static PdfEncodedRun Encode(
        PdfFontUsage usage,
        PdfCidAllocator allocator,
        ShapedRun run)
    {
        return Encode(usage, allocator, run, 0);
    }

    internal static PdfEncodedRun Encode(
        PdfFontUsage usage,
        PdfCidAllocator allocator,
        ShapedRun run,
        double baselineShift)
    {
        if (usage == null || allocator == null || run == null)
        {
            throw new System.ArgumentNullException("values");
        }

        if (!double.IsFinite(baselineShift))
        {
            throw new System.ArgumentOutOfRangeException(
                "baselineShift");
        }

        ValidatePositionValues(run);

        FontInstance instance = usage.GetInstance();

        if (allocator.GetSourceGlyphCount() != instance.GetGlyphCount())
        {
            throw new System.ArgumentException(
                "The CID allocator does not belong to the usage font.",
                "allocator");
        }

        PdfFontOptions options = usage.CopyOptions();
        ValidateWritingMode(options.GetWritingMode(), run.GetDirection());
        bool simple = options.GetFontKind() ==
            PdfFontKind.SimpleTrueType;

        if (simple)
        {
            ValidateSimpleFont(instance, options);
        }

        int[] glyphIds = CopyGlyphIds(run);
        int maximumCid = simple ? 255 : ushort.MaxValue;
        int[] cids = allocator.GetOrAddCids(glyphIds, maximumCid);
        byte[] codes = simple ? CreateOneByteCodes(cids) :
            CreateTwoByteCodes(cids);
        PdfPositionedRunData positioning = CreatePositioning(
            instance,
            run,
            cids,
            options.GetWritingMode(),
            baselineShift);
        usage.AddShapedRun(run);
        return new PdfEncodedRun(
            codes,
            simple ? 1 : 2,
            cids,
            glyphIds,
            options.GetWritingMode(),
            positioning.GetGlyphs(),
            positioning.GetAdjustments(),
            positioning.GetXAdvance(),
            positioning.GetYAdvance(),
            baselineShift,
            run.GetSourceLength());
    }

    static PdfPositionedRunData CreatePositioning(
        FontInstance instance,
        ShapedRun run,
        int[] cids,
        PdfWritingMode writingMode,
        double baselineShift)
    {
        SfntHeadTable? head = instance.GetFace().GetHeadTable();
        int unitsPerEm = head == null ? 1000 : head.GetUnitsPerEm();
        bool hasNominalMetrics = head != null &&
            instance.GetFace().GetHmtxTable() != null;
        PdfPositionedGlyph[] glyphs =
            new PdfPositionedGlyph[run.GetGlyphCount()];
        System.Collections.Generic.List<PdfTjAdjustment> adjustments =
            new System.Collections.Generic.List<PdfTjAdjustment>();
        double xAdvance = 0;
        double yAdvance = 0;
        int index = 0;

        while (index < glyphs.Length)
        {
            ShapedGlyph shapedGlyph = run.GetGlyph(index);
            double glyphXAdvance = ConvertDesignValue(
                shapedGlyph.GetXAdvance(),
                unitsPerEm,
                "xAdvance");
            double glyphYAdvance = ConvertDesignValue(
                shapedGlyph.GetYAdvance(),
                unitsPerEm,
                "yAdvance");
            double glyphXOffset = ConvertDesignValue(
                shapedGlyph.GetXOffset(),
                unitsPerEm,
                "xOffset");
            double glyphYOffset = ConvertDesignValue(
                shapedGlyph.GetYOffset(),
                unitsPerEm,
                "yOffset");
            glyphs[index] = new PdfPositionedGlyph(
                index,
                cids[index],
                cids[index],
                shapedGlyph.GetGlyphId(),
                shapedGlyph.GetUtf16Start(),
                shapedGlyph.GetUtf16Length(),
                glyphXAdvance,
                glyphYAdvance,
                glyphXOffset,
                glyphYOffset,
                baselineShift);
            xAdvance += glyphXAdvance;
            yAdvance += glyphYAdvance;
            EnsureFiniteTotal(xAdvance);
            EnsureFiniteTotal(yAdvance);

            if (hasNominalMetrics)
            {
                double nominalAdvance = GetNominalAdvance(
                    instance,
                    shapedGlyph.GetGlyphId(),
                    run.GetDirection(),
                    writingMode,
                    unitsPerEm);
                double shapedAdvance =
                    writingMode == PdfWritingMode.Horizontal ?
                    glyphXAdvance : glyphYAdvance;
                double adjustment = nominalAdvance - shapedAdvance;
                EnsureFiniteTotal(adjustment);

                if (adjustment != 0)
                {
                    adjustments.Add(new PdfTjAdjustment(
                        index,
                        adjustment));
                }
            }

            index += 1;
        }

        return new PdfPositionedRunData(
            glyphs,
            adjustments.ToArray(),
            xAdvance,
            yAdvance);
    }

    static void ValidatePositionValues(ShapedRun run)
    {
        int index = 0;

        while (index < run.GetGlyphCount())
        {
            ShapedGlyph glyph = run.GetGlyph(index);

            if (!double.IsFinite(glyph.GetXAdvance()) ||
                !double.IsFinite(glyph.GetYAdvance()) ||
                !double.IsFinite(glyph.GetXOffset()) ||
                !double.IsFinite(glyph.GetYOffset()))
            {
                throw new System.ArgumentException(
                    "A shaped glyph contains a non-finite position.",
                    "run");
            }

            index += 1;
        }
    }

    static double GetNominalAdvance(
        FontInstance instance,
        int glyphId,
        ShapingDirection direction,
        PdfWritingMode writingMode,
        int unitsPerEm)
    {
        GlyphMetrics metrics = instance.GetGlyphMetrics(glyphId);

        if (writingMode == PdfWritingMode.Horizontal)
        {
            return PdfCidWidthsBuilder.ConvertAdvance(
                metrics.GetAdvanceWidth(),
                unitsPerEm);
        }

        int displacement = PdfCidVerticalWidthsBuilder.ConvertDisplacement(
            metrics.GetAdvanceHeight(),
            unitsPerEm);
        return direction == ShapingDirection.BottomToTop ?
            -displacement : displacement;
    }

    static double ConvertDesignValue(
        double value,
        int unitsPerEm,
        string valueName)
    {
        if (!double.IsFinite(value))
        {
            throw new System.ArgumentException(
                "A shaped glyph contains a non-finite position.",
                valueName);
        }

        if (unitsPerEm <= 0)
        {
            throw FontFormatException.Create(
                "PDF run positioning requires positive units per em.");
        }

        double converted = value * 1000.0 / unitsPerEm;

        if (!double.IsFinite(converted))
        {
            throw FontLimitException.Create(
                "A PDF run position exceeds the supported range.");
        }

        return converted;
    }

    static void EnsureFiniteTotal(double value)
    {
        if (!double.IsFinite(value))
        {
            throw FontLimitException.Create(
                "A PDF run position exceeds the supported range.");
        }
    }

    static int[] CopyGlyphIds(ShapedRun run)
    {
        int[] glyphIds = new int[run.GetGlyphCount()];
        int index = 0;

        while (index < glyphIds.Length)
        {
            glyphIds[index] = run.GetGlyph(index).GetGlyphId();
            index += 1;
        }

        return glyphIds;
    }

    static byte[] CreateOneByteCodes(int[] cids)
    {
        byte[] codes = new byte[cids.Length];
        int index = 0;

        while (index < cids.Length)
        {
            codes[index] = (byte)cids[index];
            index += 1;
        }

        return codes;
    }

    static byte[] CreateTwoByteCodes(int[] cids)
    {
        byte[] codes = new byte[cids.Length * 2];
        int index = 0;

        while (index < cids.Length)
        {
            codes[index * 2] = (byte)((cids[index] >> 8) & 0xFF);
            codes[index * 2 + 1] = (byte)(cids[index] & 0xFF);
            index += 1;
        }

        return codes;
    }

    static void ValidateSimpleFont(
        FontInstance instance,
        PdfFontOptions options)
    {
        if (instance.GetFace().GetOutlineKind() !=
            FontOutlineKind.TrueType)
        {
            throw new System.InvalidOperationException(
                "Simple PDF font codes require TrueType outlines.");
        }

        if (options.GetWritingMode() != PdfWritingMode.Horizontal)
        {
            throw new System.InvalidOperationException(
                "Simple PDF font codes do not support vertical writing.");
        }

        if (options.GetVersion() < PdfVersion.Pdf1_1)
        {
            throw new System.InvalidOperationException(
                "Simple TrueType font embedding requires PDF 1.1 or later.");
        }
    }

    static void ValidateWritingMode(
        PdfWritingMode writingMode,
        ShapingDirection direction)
    {
        bool vertical = direction == ShapingDirection.TopToBottom ||
            direction == ShapingDirection.BottomToTop;

        if (writingMode == PdfWritingMode.Vertical != vertical)
        {
            throw new System.ArgumentException(
                "The shaped-run direction does not match the PDF writing mode.",
                "run");
        }
    }
}
