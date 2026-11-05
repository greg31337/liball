namespace ttf;

internal class PdfResolvedDescriptorMetric
{
    bool available;
    int integerValue;
    double numberValue;
    PdfFontDescriptorMetricSource source;

    internal PdfResolvedDescriptorMetric(
        bool available,
        int integerValue,
        double numberValue,
        PdfFontDescriptorMetricSource source)
    {
        this.available = available;
        this.integerValue = integerValue;
        this.numberValue = numberValue;
        this.source = source;
    }

    internal bool IsAvailable() { return available; }

    internal int GetIntegerValue() { return integerValue; }

    internal double GetNumberValue() { return numberValue; }

    internal PdfFontDescriptorMetricSource GetSource() { return source; }
}

internal class PdfResolvedDescriptorBounds
{
    int xMinimum;
    int yMinimum;
    int xMaximum;
    int yMaximum;
    PdfFontDescriptorMetricSource source;

    internal PdfResolvedDescriptorBounds(
        int xMinimum,
        int yMinimum,
        int xMaximum,
        int yMaximum,
        PdfFontDescriptorMetricSource source)
    {
        this.xMinimum = xMinimum;
        this.yMinimum = yMinimum;
        this.xMaximum = xMaximum;
        this.yMaximum = yMaximum;
        this.source = source;
    }

    internal int GetXMinimum() { return xMinimum; }

    internal int GetYMinimum() { return yMinimum; }

    internal int GetXMaximum() { return xMaximum; }

    internal int GetYMaximum() { return yMaximum; }

    internal PdfFontDescriptorMetricSource GetSource() { return source; }
}

internal static class PdfFontDescriptorMetricsBuilder
{
    const int SlantTag = 0x736C6E74;
    const int WeightTag = 0x77676874;

    internal static PdfFontDescriptorMetrics Build(
        FontInstance instance,
        PdfEmbeddedFontFileKind fontFileKind,
        string? fontFileSubtype,
        int fontFileLength)
    {
        if (instance == null)
        {
            throw new System.ArgumentNullException("instance");
        }

        ValidateFontFileMetadata(
            fontFileKind,
            fontFileSubtype,
            fontFileLength);
        FontDesignMetrics metrics = instance.GetGlobalMetrics();
        int unitsPerEm = metrics.GetUnitsPerEm();
        PdfResolvedDescriptorBounds bounds = ResolveBoundingBox(
            instance,
            metrics,
            unitsPerEm);
        PdfResolvedDescriptorMetric italic = ResolveItalicAngle(instance);
        int ascent = ConvertMetric(metrics.GetAscender(), unitsPerEm);
        int descent = ConvertMetric(metrics.GetDescender(), unitsPerEm);
        PdfResolvedDescriptorMetric cap = ResolveCapHeight(
            instance,
            metrics,
            ascent,
            unitsPerEm);
        PdfResolvedDescriptorMetric x = ResolveXHeight(
            instance,
            metrics,
            unitsPerEm);
        int missingWidth = PdfCidWidthsBuilder.ConvertAdvance(
            instance.GetGlyphMetrics(0).GetAdvanceWidth(),
            unitsPerEm);
        PdfResolvedDescriptorMetric stem = ResolveStemVertical(
            instance,
            unitsPerEm);
        return new PdfFontDescriptorMetrics(
            bounds.GetXMinimum(),
            bounds.GetYMinimum(),
            bounds.GetXMaximum(),
            bounds.GetYMaximum(),
            italic.GetNumberValue(),
            ascent,
            descent,
            cap.GetIntegerValue(),
            x.IsAvailable(),
            x.GetIntegerValue(),
            missingWidth,
            stem.GetIntegerValue(),
            metrics.GetLineMetricSource(),
            bounds.GetSource(),
            italic.GetSource(),
            cap.GetSource(),
            x.GetSource(),
            stem.GetSource(),
            fontFileKind,
            fontFileSubtype,
            fontFileLength);
    }

    internal static int ConvertMetric(int value, int unitsPerEm)
    {
        ValidateUnitsPerEm(unitsPerEm);
        long numerator = (long)value * 1000;

        if (numerator >= 0)
        {
            return CheckedInteger(
                (numerator + unitsPerEm / 2) / unitsPerEm);
        }

        return CheckedInteger(
            -((-numerator + unitsPerEm / 2) / unitsPerEm));
    }

    static PdfResolvedDescriptorMetric ResolveItalicAngle(
        FontInstance instance)
    {
        NormalizedFontCoordinates coordinates = instance.GetCoordinates();
        int coordinateIndex = FindAxis(coordinates, SlantTag);

        if (coordinateIndex >= 0)
        {
            return new PdfResolvedDescriptorMetric(
                true,
                0,
                coordinates.GetUserValue(coordinateIndex),
                PdfFontDescriptorMetricSource.VariationSlantAxis);
        }

        FontFace face = instance.GetFace();
        SfntPostTable? post = face.GetPostTable();

        if (post != null)
        {
            return new PdfResolvedDescriptorMetric(
                true,
                0,
                post.GetItalicAngle(),
                PdfFontDescriptorMetricSource.Post);
        }

        SfntCffTable? cff = face.GetCffTable();

        if (cff != null)
        {
            return new PdfResolvedDescriptorMetric(
                true,
                0,
                cff.GetTopDictionary().GetItalicAngle(),
                PdfFontDescriptorMetricSource.CffTopDictionary);
        }

        return new PdfResolvedDescriptorMetric(
            false,
            0,
            0,
            PdfFontDescriptorMetricSource.Unavailable);
    }

    static PdfResolvedDescriptorMetric ResolveCapHeight(
        FontInstance instance,
        FontDesignMetrics metrics,
        int ascent,
        int unitsPerEm)
    {
        if (metrics.HasCapHeightAndXHeight() &&
            metrics.GetCapHeight() != 0)
        {
            return new PdfResolvedDescriptorMetric(
                true,
                ConvertMetric(metrics.GetCapHeight(), unitsPerEm),
                0,
                PdfFontDescriptorMetricSource.Os2);
        }

        double glyphHeight = GetMappedGlyphHeight(instance, 0x48);

        if (double.IsFinite(glyphHeight))
        {
            return new PdfResolvedDescriptorMetric(
                true,
                ConvertDoubleMetric(glyphHeight, unitsPerEm),
                0,
                PdfFontDescriptorMetricSource.CmapGlyphBounds);
        }

        return new PdfResolvedDescriptorMetric(
            true,
            ascent,
            0,
            PdfFontDescriptorMetricSource.AscentFallback);
    }

    static PdfResolvedDescriptorMetric ResolveXHeight(
        FontInstance instance,
        FontDesignMetrics metrics,
        int unitsPerEm)
    {
        if (metrics.HasCapHeightAndXHeight() &&
            metrics.GetXHeight() != 0)
        {
            return new PdfResolvedDescriptorMetric(
                true,
                ConvertMetric(metrics.GetXHeight(), unitsPerEm),
                0,
                PdfFontDescriptorMetricSource.Os2);
        }

        double glyphHeight = GetMappedGlyphHeight(instance, 0x78);

        if (double.IsFinite(glyphHeight))
        {
            return new PdfResolvedDescriptorMetric(
                true,
                ConvertDoubleMetric(glyphHeight, unitsPerEm),
                0,
                PdfFontDescriptorMetricSource.CmapGlyphBounds);
        }

        return new PdfResolvedDescriptorMetric(
            false,
            0,
            0,
            PdfFontDescriptorMetricSource.Unavailable);
    }

    static double GetMappedGlyphHeight(
        FontInstance instance,
        int unicodeValue)
    {
        SfntCmapMapping? cmap = instance.GetFace().GetCmapMapping();

        if (cmap == null)
        {
            return double.NaN;
        }

        int glyphId = cmap.MapUnicode(unicodeValue);

        if (glyphId == 0)
        {
            return double.NaN;
        }

        GlyphBounds? bounds = instance.GetGlyphBounds(glyphId);
        System.ArgumentNullException.ThrowIfNull(bounds);
        return bounds.IsEmpty() ? double.NaN : bounds.GetYMaximum();
    }

    static PdfResolvedDescriptorMetric ResolveStemVertical(
        FontInstance instance,
        int unitsPerEm)
    {
        SfntCffTable? cff = instance.GetFace().GetCffTable();

        if (cff != null)
        {
            CffPrivateDictionary? privateValues = cff.GetPrivateDictionary();

            if (privateValues != null &&
                privateValues.HasStandardVerticalWidth())
            {
                double width = privateValues.GetStandardVerticalWidth();

                if (double.IsFinite(width) && width > 0)
                {
                    return new PdfResolvedDescriptorMetric(
                        true,
                        ConvertDoubleMetric(width, unitsPerEm),
                        0,
                        PdfFontDescriptorMetricSource
                            .CffStandardVerticalWidth);
                }
            }
        }

        SfntOs2Table? os2 = instance.GetFace().GetOs2Table();

        NormalizedFontCoordinates coordinates = instance.GetCoordinates();
        int coordinateIndex = FindAxis(coordinates, WeightTag);

        if (coordinateIndex >= 0)
        {
            int stemVertical = EstimateStemFromWeight(
                RoundWeight(coordinates.GetUserValue(coordinateIndex)));
            return new PdfResolvedDescriptorMetric(
                true,
                stemVertical,
                0,
                PdfFontDescriptorMetricSource.VariationWeightAxis);
        }

        if (os2 != null)
        {
            return new PdfResolvedDescriptorMetric(
                true,
                EstimateStemFromWeight(os2.GetWeightClass()),
                0,
                PdfFontDescriptorMetricSource.Os2WeightClass);
        }

        return new PdfResolvedDescriptorMetric(
            true,
            80,
            0,
            PdfFontDescriptorMetricSource.DefaultStemEstimate);
    }

    static int EstimateStemFromWeight(int weightClass)
    {
        if (weightClass < 200) { return 50; }
        if (weightClass < 300) { return 60; }
        if (weightClass < 400) { return 70; }
        if (weightClass < 500) { return 80; }
        if (weightClass < 600) { return 90; }
        if (weightClass < 700) { return 105; }
        if (weightClass < 800) { return 120; }
        if (weightClass < 900) { return 140; }
        return 160;
    }

    static PdfResolvedDescriptorBounds ResolveBoundingBox(
        FontInstance instance,
        FontDesignMetrics metrics,
        int unitsPerEm)
    {
        FontBounds global = metrics.GetGlobalBounds();

        if (instance.GetCoordinates().IsDefaultUserLocation())
        {
            return new PdfResolvedDescriptorBounds(
                ConvertMinimum(global.GetXMin(), unitsPerEm),
                ConvertMinimum(global.GetYMin(), unitsPerEm),
                ConvertMaximum(global.GetXMax(), unitsPerEm),
                ConvertMaximum(global.GetYMax(), unitsPerEm),
                PdfFontDescriptorMetricSource.Head);
        }

        bool empty = true;
        double xMinimum = 0;
        double yMinimum = 0;
        double xMaximum = 0;
        double yMaximum = 0;
        int glyphId = 0;

        while (glyphId < instance.GetGlyphCount())
        {
            GlyphBounds? glyph = instance.GetGlyphBounds(glyphId);

            System.ArgumentNullException.ThrowIfNull(glyph);
            if (!glyph.IsEmpty())
            {
                if (empty)
                {
                    xMinimum = glyph.GetXMinimum();
                    yMinimum = glyph.GetYMinimum();
                    xMaximum = glyph.GetXMaximum();
                    yMaximum = glyph.GetYMaximum();
                    empty = false;
                }
                else
                {
                    xMinimum = System.Math.Min(
                        xMinimum,
                        glyph.GetXMinimum());
                    yMinimum = System.Math.Min(
                        yMinimum,
                        glyph.GetYMinimum());
                    xMaximum = System.Math.Max(
                        xMaximum,
                        glyph.GetXMaximum());
                    yMaximum = System.Math.Max(
                        yMaximum,
                        glyph.GetYMaximum());
                }
            }

            glyphId += 1;
        }

        if (empty)
        {
            return new PdfResolvedDescriptorBounds(
                ConvertMinimum(global.GetXMin(), unitsPerEm),
                ConvertMinimum(global.GetYMin(), unitsPerEm),
                ConvertMaximum(global.GetXMax(), unitsPerEm),
                ConvertMaximum(global.GetYMax(), unitsPerEm),
                PdfFontDescriptorMetricSource.Head);
        }

        return new PdfResolvedDescriptorBounds(
            ConvertDoubleMinimum(xMinimum, unitsPerEm),
            ConvertDoubleMinimum(yMinimum, unitsPerEm),
            ConvertDoubleMaximum(xMaximum, unitsPerEm),
            ConvertDoubleMaximum(yMaximum, unitsPerEm),
            PdfFontDescriptorMetricSource.ResolvedGlyphBounds);
    }

    static int FindAxis(
        NormalizedFontCoordinates coordinates,
        int tag)
    {
        int index = 0;

        while (index < coordinates.GetAxisCount())
        {
            if (coordinates.GetAxisTag(index) == tag)
            {
                return index;
            }

            index += 1;
        }

        return -1;
    }

    static int RoundWeight(double value)
    {
        if (!double.IsFinite(value))
        {
            throw FontFormatException.Create(
                "A variation weight is outside the supported range.");
        }

        double rounded = System.Math.Round(
            value,
            System.MidpointRounding.AwayFromZero);

        if (rounded < 1) { return 1; }
        if (rounded > 1000) { return 1000; }
        return (int)rounded;
    }

    static int ConvertDoubleMetric(double value, int unitsPerEm)
    {
        ValidateUnitsPerEm(unitsPerEm);
        double converted = value * 1000 / unitsPerEm;

        if (!double.IsFinite(converted) ||
            converted < int.MinValue || converted > int.MaxValue)
        {
            throw FontFormatException.Create(
                "A PDF descriptor metric exceeds the supported range.");
        }

        return (int)System.Math.Round(
            converted,
            System.MidpointRounding.AwayFromZero);
    }

    static int ConvertMinimum(int value, int unitsPerEm)
    {
        ValidateUnitsPerEm(unitsPerEm);
        long numerator = (long)value * 1000;
        long converted = numerator / unitsPerEm;

        if (numerator < 0 && numerator % unitsPerEm != 0)
        {
            converted -= 1;
        }

        return CheckedInteger(converted);
    }

    static int ConvertDoubleMinimum(double value, int unitsPerEm)
    {
        return ConvertDoubleBound(value, unitsPerEm, false);
    }

    static int ConvertDoubleMaximum(double value, int unitsPerEm)
    {
        return ConvertDoubleBound(value, unitsPerEm, true);
    }

    static int ConvertDoubleBound(
        double value,
        int unitsPerEm,
        bool maximum)
    {
        ValidateUnitsPerEm(unitsPerEm);
        double converted = value * 1000 / unitsPerEm;

        if (!double.IsFinite(converted) ||
            converted < int.MinValue || converted > int.MaxValue)
        {
            throw FontFormatException.Create(
                "A PDF descriptor bound exceeds the supported range.");
        }

        return maximum ?
            (int)System.Math.Ceiling(converted) :
            (int)System.Math.Floor(converted);
    }

    static int ConvertMaximum(int value, int unitsPerEm)
    {
        ValidateUnitsPerEm(unitsPerEm);
        long numerator = (long)value * 1000;
        long converted = numerator / unitsPerEm;

        if (numerator > 0 && numerator % unitsPerEm != 0)
        {
            converted += 1;
        }

        return CheckedInteger(converted);
    }

    static int CheckedInteger(long value)
    {
        if (value < int.MinValue || value > int.MaxValue)
        {
            throw FontLimitException.Create(
                "A PDF descriptor metric exceeds the supported range.");
        }

        return (int)value;
    }

    static void ValidateUnitsPerEm(int unitsPerEm)
    {
        if (unitsPerEm <= 0)
        {
            throw FontFormatException.Create(
                "PDF descriptor metrics require positive units per em.");
        }
    }

    static void ValidateFontFileMetadata(
        PdfEmbeddedFontFileKind fontFileKind,
        string? fontFileSubtype,
        int fontFileLength)
    {
        if (fontFileKind < PdfEmbeddedFontFileKind.FontFile2 ||
            fontFileKind > PdfEmbeddedFontFileKind.FontFile3)
        {
            throw new System.ArgumentOutOfRangeException("fontFileKind");
        }

        if (fontFileLength < 0)
        {
            throw new System.ArgumentOutOfRangeException("fontFileLength");
        }

        if (fontFileKind == PdfEmbeddedFontFileKind.FontFile2 &&
            fontFileSubtype != null)
        {
            throw new System.ArgumentException(
                "A FontFile2 stream does not have a subtype.",
                "fontFileSubtype");
        }

        if (fontFileKind == PdfEmbeddedFontFileKind.FontFile3 &&
            string.IsNullOrEmpty(fontFileSubtype))
        {
            throw new System.ArgumentException(
                "A FontFile3 stream requires a subtype.",
                "fontFileSubtype");
        }
    }
}
