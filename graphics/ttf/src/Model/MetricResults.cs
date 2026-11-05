namespace ttf;

internal static class FontMetricResolver
{
    internal static FontDesignMetrics CreateGlobal(
        FontFace face,
        FontMetricSelectionPolicy policy)
    {
        ValidatePolicy(policy);
        SfntHeadTable? head = face.GetHeadTable();
        SfntHheaTable? hhea = face.GetHheaTable();
        SfntOs2Table? os2 = face.GetOs2Table();
        SfntPostTable? post = face.GetPostTable();
        SfntVheaTable? vhea = face.GetVheaTable();
        SfntGaspTable? gasp = face.GetGaspTable();
        if (head == null)
        {
            throw FontFormatException.Create("Global metrics require a supported head table.");
        }

        FontLineMetricSource source = SelectSource(policy, os2, hhea);
        int ascender;
        int descender;
        int lineGap;
        if (source == FontLineMetricSource.Typographic)
        {
            System.ArgumentNullException.ThrowIfNull(os2);
            ascender = os2.GetTypographicAscender();
            descender = os2.GetTypographicDescender();
            lineGap = os2.GetTypographicLineGap();
        }
        else if (source == FontLineMetricSource.Windows)
        {
            System.ArgumentNullException.ThrowIfNull(os2);
            ascender = os2.GetWindowsAscent();
            descender = -os2.GetWindowsDescent();
            lineGap = 0;
        }
        else
        {
            System.ArgumentNullException.ThrowIfNull(hhea);
            ascender = hhea.GetAscender();
            descender = hhea.GetDescender();
            lineGap = hhea.GetLineGap();
        }

        bool hasCapAndXHeight = os2 != null && os2.HasVersion2Metrics();
        bool hasUnderline = post != null;
        bool hasStrikeout = os2 != null;
        bool hasScriptMetrics = os2 != null;
        bool hasCaret = hhea != null;
        bool hasVertical = vhea != null;
        int[] gaspMaximums = CreateGaspMaximums(gasp);
        int[] gaspBehaviors = CreateGaspBehaviors(gasp);
        return new FontDesignMetrics(
            policy,
            source,
            head.GetUnitsPerEm(),
            ascender,
            descender,
            lineGap,
            new FontBounds(head.GetXMin(), head.GetYMin(), head.GetXMax(), head.GetYMax()),
            hasCapAndXHeight,
            os2 != null && os2.HasVersion2Metrics() ? os2.GetCapHeight() : 0,
            os2 != null && os2.HasVersion2Metrics() ? os2.GetXHeight() : 0,
            hasUnderline,
            post != null ? post.GetUnderlinePosition() : 0,
            post != null ? post.GetUnderlineThickness() : 0,
            hasStrikeout,
            os2 != null ? os2.GetStrikeoutPosition() : 0,
            os2 != null ? os2.GetStrikeoutSize() : 0,
            hasScriptMetrics,
            os2 != null ? os2.GetSubscriptXSize() : 0,
            os2 != null ? os2.GetSubscriptYSize() : 0,
            os2 != null ? os2.GetSubscriptXOffset() : 0,
            os2 != null ? os2.GetSubscriptYOffset() : 0,
            os2 != null ? os2.GetSuperscriptXSize() : 0,
            os2 != null ? os2.GetSuperscriptYSize() : 0,
            os2 != null ? os2.GetSuperscriptXOffset() : 0,
            os2 != null ? os2.GetSuperscriptYOffset() : 0,
            hasCaret,
            hhea != null ? hhea.GetCaretSlopeRise() : 0,
            hhea != null ? hhea.GetCaretSlopeRun() : 0,
            hhea != null ? hhea.GetCaretOffset() : 0,
            hasVertical,
            vhea != null ? vhea.GetAscender() : 0,
            vhea != null ? vhea.GetDescender() : 0,
            vhea != null ? vhea.GetLineGap() : 0,
            vhea != null ? vhea.GetCaretSlopeRise() : 0,
            vhea != null ? vhea.GetCaretSlopeRun() : 0,
            vhea != null ? vhea.GetCaretOffset() : 0,
            gaspMaximums,
            gaspBehaviors);
    }

    static int[] CreateGaspMaximums(SfntGaspTable? gasp)
    {
        if (gasp == null)
        {
            return new int[0];
        }

        int[] values = new int[gasp.GetRangeCount()];
        int index = 0;

        while (index < values.Length)
        {
            values[index] = gasp.GetRange(index).GetMaximumPpem();
            index += 1;
        }

        return values;
    }

    static int[] CreateGaspBehaviors(SfntGaspTable? gasp)
    {
        if (gasp == null)
        {
            return new int[0];
        }

        int[] values = new int[gasp.GetRangeCount()];
        int index = 0;

        while (index < values.Length)
        {
            values[index] = gasp.GetRange(index).GetBehavior();
            index += 1;
        }

        return values;
    }

    internal static GlyphMetrics CreateGlyph(
        FontFace face,
        int glyphId,
        int glyphYMaximum)
    {
        return CreateGlyph(
            face,
            glyphId,
            glyphId,
            glyphYMaximum);
    }

    internal static GlyphMetrics CreateGlyph(
        FontFace face,
        int glyphId,
        int metricsGlyphId,
        int glyphYMaximum)
    {
        SfntHmtxTable? horizontal = face.GetHmtxTable();
        if (horizontal == null)
        {
            throw FontFormatException.Create("Glyph metrics require supported hhea and hmtx tables.");
        }
        SfntVerticalMetrics vertical = face.GetVerticalMetrics();
        int advanceWidth = horizontal.GetAdvanceWidth(metricsGlyphId);
        return new GlyphMetrics(
            glyphId,
            advanceWidth,
            horizontal.GetLeftSideBearing(metricsGlyphId),
            vertical.GetAdvanceHeight(metricsGlyphId),
            vertical.GetTopSideBearing(metricsGlyphId, glyphYMaximum),
            vertical.GetVerticalOriginY(metricsGlyphId, glyphYMaximum),
            vertical.UsesSynthesizedAdvanceHeights(),
            vertical.UsesVorgOrigins());
    }

    static FontLineMetricSource SelectSource(
        FontMetricSelectionPolicy policy,
        SfntOs2Table? os2,
        SfntHheaTable? hhea)
    {
        bool hasOs2Metrics = os2 != null && os2.HasTypographicMetrics();
        if (policy == FontMetricSelectionPolicy.Typographic)
        {
            if (!hasOs2Metrics) { throw FontFormatException.Create("OS/2 typographic metrics are unavailable."); }
            return FontLineMetricSource.Typographic;
        }
        if (policy == FontMetricSelectionPolicy.Windows)
        {
            if (!hasOs2Metrics) { throw FontFormatException.Create("OS/2 Windows metrics are unavailable."); }
            return FontLineMetricSource.Windows;
        }
        if (policy == FontMetricSelectionPolicy.HorizontalHeader)
        {
            if (hhea == null) { throw FontFormatException.Create("hhea line metrics are unavailable."); }
            return FontLineMetricSource.HorizontalHeader;
        }
        if (os2 != null && hasOs2Metrics && os2.UsesTypographicMetrics())
        {
            return FontLineMetricSource.Typographic;
        }
        if (hasOs2Metrics) { return FontLineMetricSource.Windows; }
        if (hhea != null) { return FontLineMetricSource.HorizontalHeader; }
        throw FontFormatException.Create("No supported horizontal line metrics are available.");
    }

    static void ValidatePolicy(FontMetricSelectionPolicy policy)
    {
        if (policy < FontMetricSelectionPolicy.OpenTypeRecommended ||
            policy > FontMetricSelectionPolicy.HorizontalHeader)
        {
            throw new System.ArgumentOutOfRangeException("policy");
        }
    }
}
