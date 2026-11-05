namespace chart;
internal class ChartResolvedStyle
{
    internal bool FillNone;
    internal ChartColor FillColor;
    internal bool StrokeNone;
    internal ChartColor StrokeColor;
    internal double StrokeWidth;
    internal double Opacity;
    internal string FontFamily;
    internal int FontSize;
    internal double MarkerSize;
    internal double[] DashArray;
    internal double MiterLimit;
    internal double FillOpacity;
    internal double StrokeOpacity;
    internal bool BorderNone;
    internal ChartColor BorderColor;
    internal double BorderWidth;
    internal ChartFontWeight FontWeight;
    internal ChartFontStyle FontStyle;
    internal ChartColor TextOutlineColor;
    internal double TextOutlineWidth;
    internal ChartColor TextShadowColor;
    internal double TextShadowOffsetX;
    internal double TextShadowOffsetY;
    internal double TextShadowBlur;
    internal ChartPath CustomMarkerPath;
    internal ChartResolvedStyle()
    {
        FillNone = false;
        FillColor = ChartColor.FromRgb(31, 119, 180);
        StrokeNone = false;
        StrokeColor = ChartColor.Black();
        StrokeWidth = 1.0;
        Opacity = 1.0;
        FontFamily = "Arial";
        FontSize = 12;
        MarkerSize = 4.0;
        DashArray = new double[0];
        MiterLimit = 4.0;
        FillOpacity = 1.0;
        StrokeOpacity = 1.0;
        BorderNone = true;
        BorderColor = ChartColor.Black();
        BorderWidth = 1.0;
        FontWeight = ChartFontWeight.Normal;
        FontStyle = ChartFontStyle.Normal;
        TextOutlineColor = ChartColor.Transparent();
        TextOutlineWidth = 0.0;
        TextShadowColor = ChartColor.Transparent();
        TextShadowOffsetX = 0.0;
        TextShadowOffsetY = 0.0;
        TextShadowBlur = 0.0;
        CustomMarkerPath = new ChartPath();
    }

    internal ChartResolvedStyle Copy()
    {
        ChartResolvedStyle style = new ChartResolvedStyle();
        style.FillNone = FillNone;
        style.FillColor = FillColor.Copy();
        style.StrokeNone = StrokeNone;
        style.StrokeColor = StrokeColor.Copy();
        style.StrokeWidth = StrokeWidth;
        style.Opacity = Opacity;
        style.FontFamily = FontFamily;
        style.FontSize = FontSize;
        style.MarkerSize = MarkerSize;
        style.DashArray = ChartStyle.CopyDashArrayAllowEmpty(DashArray);
        style.MiterLimit = MiterLimit;
        style.FillOpacity = FillOpacity;
        style.StrokeOpacity = StrokeOpacity;
        style.BorderNone = BorderNone;
        style.BorderColor = BorderColor.Copy();
        style.BorderWidth = BorderWidth;
        style.FontWeight = FontWeight;
        style.FontStyle = FontStyle;
        style.TextOutlineColor = TextOutlineColor.Copy();
        style.TextOutlineWidth = TextOutlineWidth;
        style.TextShadowColor = TextShadowColor.Copy();
        style.TextShadowOffsetX = TextShadowOffsetX;
        style.TextShadowOffsetY = TextShadowOffsetY;
        style.TextShadowBlur = TextShadowBlur;
        style.CustomMarkerPath = ChartRenderCommand.CopyPath(CustomMarkerPath);
        return style;
    }

    internal SvgStyle ToSvgStyle()
    {
        SvgStyle style = new SvgStyle();
        if (FillNone)
        {
            style.SetNoFill();
        }
        else
        {
            style.SetFill(FillColor);
        }

        if (StrokeNone)
        {
            style.SetNoStroke();
        }
        else
        {
            style.SetStroke(StrokeColor, StrokeWidth);
        }

        style.SetOpacity(Opacity);
        style.SetFont(FontFamily, FontSize);
        if (DashArray.Length > 0)
        {
            style.SetDashArray(DashArray);
        }

        if (MiterLimit != 4.0)
        {
            style.SetMiterLimit(MiterLimit);
        }

        if (FillOpacity < 1.0)
        {
            style.SetFillOpacity(FillOpacity);
        }

        if (StrokeOpacity < 1.0)
        {
            style.SetStrokeOpacity(StrokeOpacity);
        }

        if (FontWeight != ChartFontWeight.Normal)
        {
            style.SetFontWeight(FontWeight);
        }

        if (FontStyle != ChartFontStyle.Normal)
        {
            style.SetFontStyle(FontStyle);
        }
        if (TextOutlineWidth > 0.0)
        {
            style.SetTextOutline(TextOutlineColor, TextOutlineWidth);
        }

        if (TextShadowColor.A > 0)
        {
            style.SetTextShadow(TextShadowColor, TextShadowOffsetX, TextShadowOffsetY, TextShadowBlur);
        }

        return style;
    }
}

internal static class ChartStyleCascade
{
    internal static ChartResolvedStyle ResolveStyle(ChartTheme theme, ChartStyle chartStyle, ChartStyle plotStyle, ChartStyle seriesStyle, ChartStyle pointStyle, int seriesIndex)
    {
        if (theme == null)
        {
            throw new System.ArgumentNullException("theme");
        }

        if (chartStyle == null)
        {
            throw new System.ArgumentNullException("chartStyle");
        }

        if (plotStyle == null)
        {
            throw new System.ArgumentNullException("plotStyle");
        }

        if (seriesStyle == null)
        {
            throw new System.ArgumentNullException("seriesStyle");
        }

        if (pointStyle == null)
        {
            throw new System.ArgumentNullException("pointStyle");
        }

        ChartResolvedStyle resolved = new ChartResolvedStyle();
        ApplyStyle(resolved, ChartTheme.LibraryDefaultStyle());
        ApplyStyle(resolved, theme.Style);
        if (seriesIndex >= 0 && !seriesStyle.HasFillColor && !pointStyle.HasFillColor)
        {
            resolved.FillNone = false;
            resolved.FillColor = theme.SeriesColor(seriesIndex);
        }

        ApplyStyle(resolved, chartStyle);
        ApplyStyle(resolved, plotStyle);
        ApplyStyle(resolved, seriesStyle);
        ApplyStyle(resolved, pointStyle);
        return resolved;
    }

    internal static ChartResolvedStyle ResolveDocumentStyle(ChartDocument document, ChartPlot plot, ChartStyle seriesStyle, ChartStyle pointStyle, int seriesIndex)
    {
        if (document == null)
        {
            throw new System.ArgumentNullException("document");
        }

        if (plot == null)
        {
            throw new System.ArgumentNullException("plot");
        }

        return ResolveStyle(document.Theme, document.Style, plot.Style, seriesStyle, pointStyle, seriesIndex);
    }

    internal static void ApplyStyle(ChartResolvedStyle resolved, ChartStyle style)
    {
        if (resolved == null)
        {
            throw new System.ArgumentNullException("resolved");
        }

        if (style == null)
        {
            throw new System.ArgumentNullException("style");
        }

        if (style.HasFillColor)
        {
            resolved.FillNone = style.FillNone;
            resolved.FillColor = style.FillColor.Copy();
        }

        if (style.HasStrokeColor)
        {
            resolved.StrokeNone = style.StrokeNone;
            resolved.StrokeColor = style.StrokeColor.Copy();
        }

        if (style.HasStrokeWidth)
        {
            resolved.StrokeWidth = style.StrokeWidth;
        }

        if (style.HasOpacity)
        {
            resolved.Opacity = style.Opacity;
        }

        if (style.HasFontFamily)
        {
            resolved.FontFamily = style.FontFamily;
        }

        if (style.HasFontSize)
        {
            resolved.FontSize = style.FontSize;
        }

        if (style.HasMarkerSize)
        {
            resolved.MarkerSize = style.MarkerSize;
        }

        if (style.HasDashArray)
        {
            resolved.DashArray = ChartStyle.CopyDashArrayAllowEmpty(style.DashArray);
        }

        if (style.HasMiterLimit)
        {
            resolved.MiterLimit = style.MiterLimit;
        }

        if (style.HasFillOpacity)
        {
            resolved.FillOpacity = style.FillOpacity;
        }

        if (style.HasStrokeOpacity)
        {
            resolved.StrokeOpacity = style.StrokeOpacity;
        }

        if (style.HasBorder)
        {
            resolved.BorderNone = style.BorderNone;
            resolved.BorderColor = style.BorderColor.Copy();
            resolved.BorderWidth = style.BorderWidth;
            resolved.StrokeNone = style.BorderNone;
            resolved.StrokeColor = style.BorderColor.Copy();
            resolved.StrokeWidth = style.BorderWidth;
        }

        if (style.HasFontWeight)
        {
            resolved.FontWeight = style.FontWeight;
        }

        if (style.HasFontStyle)
        {
            resolved.FontStyle = style.FontStyle;
        }

        if (style.HasTextOutline)
        {
            resolved.TextOutlineColor = style.TextOutlineColor.Copy();
            resolved.TextOutlineWidth = style.TextOutlineWidth;
        }

        if (style.HasTextShadow)
        {
            resolved.TextShadowColor = style.TextShadowColor.Copy();
            resolved.TextShadowOffsetX = style.TextShadowOffsetX;
            resolved.TextShadowOffsetY = style.TextShadowOffsetY;
            resolved.TextShadowBlur = style.TextShadowBlur;
        }

        if (style.HasCustomMarker)
        {
            resolved.CustomMarkerPath = ChartRenderCommand.CopyPath(style.CustomMarkerPath);
        }
    }
}
