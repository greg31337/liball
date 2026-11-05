namespace chart;
/// <summary>
/// Represents a render style used by the chart model or rendering pipeline.
/// </summary>
public class ChartRenderStyle : IChartRenderStyle
{
    internal bool HasFill;
    internal bool FillNone;
    internal ChartColor FillColor;
    internal bool HasStroke;
    internal bool StrokeNone;
    internal ChartColor StrokeColor;
    internal double StrokeWidth;
    internal ChartLineCap LineCap;
    internal ChartLineJoin LineJoin;
    internal ChartFillRule FillRule;
    internal double Opacity;
    internal string FontFamily;
    internal int FontSize;
    internal int TextScale;
    internal ChartRenderQuality Quality;
    internal ChartBlendMode BlendMode;
    internal double[] DashArray;
    internal double MiterLimit;
    internal double FillOpacity;
    internal double StrokeOpacity;
    internal bool HasBorder;
    internal ChartColor BorderColor;
    internal double BorderWidth;
    internal ChartFontWeight FontWeight;
    internal ChartFontStyle FontStyle;
    internal bool HasTextOutline;
    internal ChartColor TextOutlineColor;
    internal double TextOutlineWidth;
    internal bool HasTextShadow;
    internal ChartColor TextShadowColor;
    internal double TextShadowOffsetX;
    internal double TextShadowOffsetY;
    internal double TextShadowBlur;
    internal bool HasFillGradient;
    internal ChartPoint FillGradientStart;
    internal ChartPoint FillGradientEnd;
    internal ChartColor[] FillGradientColors;
    internal double[] FillGradientOffsets;
    internal bool HasStrokeGradient;
    internal ChartPoint StrokeGradientStart;
    internal ChartPoint StrokeGradientEnd;
    internal ChartColor[] StrokeGradientColors;
    internal double[] StrokeGradientOffsets;
    internal bool HasFillPattern;
    internal RawBitmap FillPattern;
    internal bool HasStrokePattern;
    internal RawBitmap StrokePattern;
    /// <summary>
    /// Initializes an empty render style with library defaults.
    /// </summary>
    public ChartRenderStyle()
    {
        HasFill = false;
        FillNone = false;
        FillColor = ChartColor.Black();
        HasStroke = false;
        StrokeNone = false;
        StrokeColor = ChartColor.Black();
        StrokeWidth = 1.0;
        LineCap = ChartLineCap.Butt;
        LineJoin = ChartLineJoin.Miter;
        FillRule = ChartFillRule.Nonzero;
        Opacity = 1.0;
        FontFamily = "Arial";
        FontSize = 12;
        TextScale = 1;
        Quality = ChartRenderQuality.Antialiased;
        BlendMode = ChartBlendMode.SourceOver;
        DashArray = new double[0];
        MiterLimit = 4.0;
        FillOpacity = 1.0;
        StrokeOpacity = 1.0;
        HasBorder = false;
        BorderColor = ChartColor.Black();
        BorderWidth = 1.0;
        FontWeight = ChartFontWeight.Normal;
        FontStyle = ChartFontStyle.Normal;
        HasTextOutline = false;
        TextOutlineColor = ChartColor.Transparent();
        TextOutlineWidth = 0.0;
        HasTextShadow = false;
        TextShadowColor = ChartColor.Transparent();
        TextShadowOffsetX = 0.0;
        TextShadowOffsetY = 0.0;
        TextShadowBlur = 0.0;
        HasFillGradient = false;
        FillGradientStart = new ChartPoint(0.0, 0.0);
        FillGradientEnd = new ChartPoint(1.0, 0.0);
        FillGradientColors = new ChartColor[0];
        FillGradientOffsets = new double[0];
        HasStrokeGradient = false;
        StrokeGradientStart = new ChartPoint(0.0, 0.0);
        StrokeGradientEnd = new ChartPoint(1.0, 0.0);
        StrokeGradientColors = new ChartColor[0];
        StrokeGradientOffsets = new double[0];
        HasFillPattern = false;
        FillPattern = new RawBitmap(1, 1, ChartPixelFormat.Rgba32);
        HasStrokePattern = false;
        StrokePattern = new RawBitmap(1, 1, ChartPixelFormat.Rgba32);
    }

    internal static ChartRenderStyle CreateFill(ChartColor color)
    {
        ChartRenderStyle style = new ChartRenderStyle();
        style.SetFill(color);
        return style;
    }

    internal static ChartRenderStyle CreateStroke(ChartColor color, double strokeWidth)
    {
        ChartRenderStyle style = new ChartRenderStyle();
        style.SetNoFill();
        style.SetStroke(color, strokeWidth);
        return style;
    }

    internal static ChartRenderStyle CreateFillStroke(ChartColor fillColor, ChartColor strokeColor, double strokeWidth)
    {
        ChartRenderStyle style = new ChartRenderStyle();
        style.SetFill(fillColor);
        style.SetStroke(strokeColor, strokeWidth);
        return style;
    }

    /// <summary>
    /// Sets the fill configuration.
    /// </summary>
    /// <param name = "color">The color value to apply or encode.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public void SetFill(ChartColor color)
    {
        if (color == null)
        {
            throw new System.ArgumentNullException("color");
        }

        HasFill = true;
        FillNone = false;
        FillColor = color.Copy();
        HasFillGradient = false;
        HasFillPattern = false;
    }

    /// <summary>
    /// Disables the fill setting.
    /// </summary>
    public void SetNoFill()
    {
        HasFill = true;
        FillNone = true;
        HasFillGradient = false;
        HasFillPattern = false;
    }

    /// <summary>
    /// Sets the stroke configuration.
    /// </summary>
    /// <param name = "color">The color value to apply or encode.</param>
    /// <param name = "strokeWidth">The stroke width in chart units, which must be positive.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public void SetStroke(ChartColor color, double strokeWidth)
    {
        if (color == null)
        {
            throw new System.ArgumentNullException("color");
        }

        ValidatePositive(strokeWidth, "strokeWidth");
        HasStroke = true;
        StrokeNone = false;
        StrokeColor = color.Copy();
        StrokeWidth = strokeWidth;
        HasBorder = false;
        HasStrokeGradient = false;
        HasStrokePattern = false;
    }

    /// <summary>
    /// Disables the stroke setting.
    /// </summary>
    public void SetNoStroke()
    {
        HasStroke = true;
        StrokeNone = true;
        HasBorder = false;
        HasStrokeGradient = false;
        HasStrokePattern = false;
    }

    /// <summary>Uses a linear color gradient for command fills.</summary>
    /// <param name = "start">The gradient start point in scene coordinates.</param>
    /// <param name = "end">The gradient end point in scene coordinates.</param>
    /// <param name = "colors">The colors at the gradient stops.</param>
    /// <param name = "offsets">The corresponding offsets from zero through one.</param>
    public void SetFillGradient(ChartPoint start, ChartPoint end, ChartColor[] colors, double[] offsets)
    {
        ValidateGradient(start, end, colors, offsets);
        HasFill = true;
        FillNone = false;
        HasFillGradient = true;
        HasFillPattern = false;
        FillGradientStart = start.Copy();
        FillGradientEnd = end.Copy();
        FillGradientColors = CopyColors(colors);
        FillGradientOffsets = CopyOffsets(offsets);
        FillColor = colors[0].Copy();
    }

    /// <summary>Uses a linear color gradient for command strokes.</summary>
    /// <param name = "start">The gradient start point in scene coordinates.</param>
    /// <param name = "end">The gradient end point in scene coordinates.</param>
    /// <param name = "colors">The colors at the gradient stops.</param>
    /// <param name = "offsets">The corresponding offsets from zero through one.</param>
    /// <param name = "strokeWidth">The positive stroke width.</param>
    public void SetStrokeGradient(ChartPoint start, ChartPoint end, ChartColor[] colors, double[] offsets, double strokeWidth)
    {
        ValidatePositive(strokeWidth, "strokeWidth");
        ValidateGradient(start, end, colors, offsets);
        HasStroke = true;
        StrokeNone = false;
        HasStrokeGradient = true;
        HasStrokePattern = false;
        StrokeGradientStart = start.Copy();
        StrokeGradientEnd = end.Copy();
        StrokeGradientColors = CopyColors(colors);
        StrokeGradientOffsets = CopyOffsets(offsets);
        StrokeColor = colors[0].Copy();
        StrokeWidth = strokeWidth;
    }

    /// <summary>Uses a repeating raw bitmap for command fills.</summary>
    /// <param name = "bitmap">The raw bitmap tile.</param>
    public void SetFillPattern(RawBitmap bitmap)
    {
        if (bitmap == null)
        {
            throw new System.ArgumentNullException("bitmap");
        }

        HasFill = true;
        FillNone = false;
        HasFillGradient = false;
        HasFillPattern = true;
        FillPattern = CopyBitmap(bitmap);
        FillColor = bitmap.GetPixel(0, 0);
    }

    /// <summary>Uses a repeating raw bitmap for command strokes.</summary>
    /// <param name = "bitmap">The raw bitmap tile.</param>
    /// <param name = "strokeWidth">The positive stroke width.</param>
    public void SetStrokePattern(RawBitmap bitmap, double strokeWidth)
    {
        if (bitmap == null)
        {
            throw new System.ArgumentNullException("bitmap");
        }

        ValidatePositive(strokeWidth, "strokeWidth");
        HasStroke = true;
        StrokeNone = false;
        HasStrokeGradient = false;
        HasStrokePattern = true;
        StrokePattern = CopyBitmap(bitmap);
        StrokeColor = bitmap.GetPixel(0, 0);
        StrokeWidth = strokeWidth;
    }

    /// <summary>
    /// Sets the line configuration.
    /// </summary>
    /// <param name = "lineCap">The line cap.</param>
    /// <param name = "lineJoin">The line join.</param>
    public void SetLine(ChartLineCap lineCap, ChartLineJoin lineJoin)
    {
        LineCap = lineCap;
        LineJoin = lineJoin;
    }

    /// <summary>Sets the repeating stroke dash lengths.</summary>
    /// <param name = "dashArray">The positive alternating dash and gap lengths.</param>
    public void SetDashArray(double[] dashArray)
    {
        DashArray = ChartStyle.CopyDashArray(dashArray);
    }

    /// <summary>Restores a solid stroke.</summary>
    public void ClearDashArray()
    {
        DashArray = new double[0];
    }

    /// <summary>Sets the miter length limit used by miter joins.</summary>
    /// <param name = "miterLimit">The positive miter limit.</param>
    public void SetMiterLimit(double miterLimit)
    {
        ValidatePositive(miterLimit, "miterLimit");
        MiterLimit = miterLimit;
    }

    /// <summary>Sets fill opacity independently from overall opacity.</summary>
    /// <param name = "fillOpacity">The opacity from zero through one.</param>
    public void SetFillOpacity(double fillOpacity)
    {
        ChartStyle.ValidateOpacity(fillOpacity, "fillOpacity");
        FillOpacity = fillOpacity;
    }

    /// <summary>Sets stroke opacity independently from overall opacity.</summary>
    /// <param name = "strokeOpacity">The opacity from zero through one.</param>
    public void SetStrokeOpacity(double strokeOpacity)
    {
        ChartStyle.ValidateOpacity(strokeOpacity, "strokeOpacity");
        StrokeOpacity = strokeOpacity;
    }

    /// <summary>Sets the border used as the stroke around filled commands.</summary>
    /// <param name = "color">The border color.</param>
    /// <param name = "width">The positive border width.</param>
    public void SetBorder(ChartColor color, double width)
    {
        SetStroke(color, width);
        HasBorder = true;
        BorderColor = color.Copy();
        BorderWidth = width;
    }

    /// <summary>
    /// Sets the fill rule configuration.
    /// </summary>
    /// <param name = "fillRule">The fill rule.</param>
    public void SetFillRule(ChartFillRule fillRule)
    {
        FillRule = fillRule;
    }

    /// <summary>
    /// Sets the opacity configuration.
    /// </summary>
    /// <param name = "opacity">The opacity from zero for transparent through one for opaque.</param>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public void SetOpacity(double opacity)
    {
        if (opacity < 0.0 || opacity > 1.0 || double.IsNaN(opacity) || double.IsInfinity(opacity))
        {
            throw new System.ArgumentOutOfRangeException("opacity");
        }

        Opacity = opacity;
    }

    /// <summary>
    /// Sets the font configuration.
    /// </summary>
    /// <param name = "fontFamily">The SVG font-family name; bitmap output uses the built-in glyph shapes.</param>
    /// <param name = "fontSize">The font size in pixels, which must be positive.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public void SetFont(string fontFamily, int fontSize)
    {
        if (fontFamily == null)
        {
            throw new System.ArgumentNullException("fontFamily");
        }

        if (fontSize <= 0)
        {
            throw new System.ArgumentOutOfRangeException("fontSize");
        }

        FontFamily = fontFamily;
        FontSize = fontSize;
    }

    /// <summary>Sets the font weight.</summary>
    /// <param name = "fontWeight">The font weight.</param>
    public void SetFontWeight(ChartFontWeight fontWeight)
    {
        ChartStyle.ValidateFontWeight(fontWeight);
        FontWeight = fontWeight;
    }

    /// <summary>Sets the font posture.</summary>
    /// <param name = "fontStyle">The font posture.</param>
    public void SetFontStyle(ChartFontStyle fontStyle)
    {
        ChartStyle.ValidateFontStyle(fontStyle);
        FontStyle = fontStyle;
    }

    /// <summary>Sets a text outline.</summary>
    /// <param name = "color">The outline color.</param>
    /// <param name = "width">The positive outline width.</param>
    public void SetTextOutline(ChartColor color, double width)
    {
        if (color == null)
        {
            throw new System.ArgumentNullException("color");
        }

        ValidatePositive(width, "width");
        HasTextOutline = true;
        TextOutlineColor = color.Copy();
        TextOutlineWidth = width;
    }

    /// <summary>Clears the text outline.</summary>
    public void ClearTextOutline()
    {
        HasTextOutline = false;
        TextOutlineColor = ChartColor.Transparent();
        TextOutlineWidth = 0.0;
    }

    /// <summary>Sets a text shadow.</summary>
    /// <param name = "color">The shadow color.</param>
    /// <param name = "offsetX">The horizontal shadow offset.</param>
    /// <param name = "offsetY">The vertical shadow offset.</param>
    /// <param name = "blur">The nonnegative blur radius.</param>
    public void SetTextShadow(ChartColor color, double offsetX, double offsetY, double blur)
    {
        if (color == null)
        {
            throw new System.ArgumentNullException("color");
        }

        ChartLinearScale.ValidateFinite(offsetX, "offsetX");
        ChartLinearScale.ValidateFinite(offsetY, "offsetY");
        ChartStyle.ValidateNonNegative(blur, "blur");
        HasTextShadow = true;
        TextShadowColor = color.Copy();
        TextShadowOffsetX = offsetX;
        TextShadowOffsetY = offsetY;
        TextShadowBlur = blur;
    }

    /// <summary>Clears the text shadow.</summary>
    public void ClearTextShadow()
    {
        HasTextShadow = false;
        TextShadowColor = ChartColor.Transparent();
        TextShadowOffsetX = 0.0;
        TextShadowOffsetY = 0.0;
        TextShadowBlur = 0.0;
    }

    /// <summary>
    /// Sets the text scale configuration.
    /// </summary>
    /// <param name = "textScale">The text scale.</param>
    public void SetTextScale(int textScale)
    {
        ChartBitmapFont.ValidateScale(textScale);
        TextScale = textScale;
    }

    /// <summary>
    /// Sets the quality configuration.
    /// </summary>
    /// <param name = "quality">The quality.</param>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public void SetQuality(ChartRenderQuality quality)
    {
        int value = (int)quality;
        if (value < 0 || value > 1)
        {
            throw new System.ArgumentOutOfRangeException("quality");
        }

        Quality = quality;
    }

    /// <summary>
    /// Sets the blend mode configuration.
    /// </summary>
    /// <param name = "blendMode">The blend mode.</param>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public void SetBlendMode(ChartBlendMode blendMode)
    {
        int value = (int)blendMode;
        if (value < 0 || value > 6)
        {
            throw new System.ArgumentOutOfRangeException("blendMode");
        }

        BlendMode = blendMode;
    }

    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartRenderStyle Copy()
    {
        ChartRenderStyle style = new ChartRenderStyle();
        style.HasFill = HasFill;
        style.FillNone = FillNone;
        style.FillColor = FillColor.Copy();
        style.HasStroke = HasStroke;
        style.StrokeNone = StrokeNone;
        style.StrokeColor = StrokeColor.Copy();
        style.StrokeWidth = StrokeWidth;
        style.LineCap = LineCap;
        style.LineJoin = LineJoin;
        style.FillRule = FillRule;
        style.Opacity = Opacity;
        style.FontFamily = FontFamily;
        style.FontSize = FontSize;
        style.TextScale = TextScale;
        style.Quality = Quality;
        style.BlendMode = BlendMode;
        style.DashArray = ChartStyle.CopyDashArrayAllowEmpty(DashArray);
        style.MiterLimit = MiterLimit;
        style.FillOpacity = FillOpacity;
        style.StrokeOpacity = StrokeOpacity;
        style.HasBorder = HasBorder;
        style.BorderColor = BorderColor.Copy();
        style.BorderWidth = BorderWidth;
        style.FontWeight = FontWeight;
        style.FontStyle = FontStyle;
        style.HasTextOutline = HasTextOutline;
        style.TextOutlineColor = TextOutlineColor.Copy();
        style.TextOutlineWidth = TextOutlineWidth;
        style.HasTextShadow = HasTextShadow;
        style.TextShadowColor = TextShadowColor.Copy();
        style.TextShadowOffsetX = TextShadowOffsetX;
        style.TextShadowOffsetY = TextShadowOffsetY;
        style.TextShadowBlur = TextShadowBlur;
        style.HasFillGradient = HasFillGradient;
        style.FillGradientStart = FillGradientStart.Copy();
        style.FillGradientEnd = FillGradientEnd.Copy();
        style.FillGradientColors = CopyColors(FillGradientColors);
        style.FillGradientOffsets = CopyOffsets(FillGradientOffsets);
        style.HasStrokeGradient = HasStrokeGradient;
        style.StrokeGradientStart = StrokeGradientStart.Copy();
        style.StrokeGradientEnd = StrokeGradientEnd.Copy();
        style.StrokeGradientColors = CopyColors(StrokeGradientColors);
        style.StrokeGradientOffsets = CopyOffsets(StrokeGradientOffsets);
        style.HasFillPattern = HasFillPattern;
        style.FillPattern = CopyBitmap(FillPattern);
        style.HasStrokePattern = HasStrokePattern;
        style.StrokePattern = CopyBitmap(StrokePattern);
        return style;
    }

    internal static void ValidateGradient(ChartPoint start, ChartPoint end, ChartColor[] colors, double[] offsets)
    {
        if (start == null)
        {
            throw new System.ArgumentNullException("start");
        }

        if (end == null)
        {
            throw new System.ArgumentNullException("end");
        }

        ChartRenderCommand.ValidateFinitePair(start.X, start.Y, "start");
        ChartRenderCommand.ValidateFinitePair(end.X, end.Y, "end");
        SvgWriter.ValidateGradient(colors, offsets);
    }

    internal static ChartColor[] CopyColors(ChartColor[] colors)
    {
        if (colors == null)
        {
            throw new System.ArgumentNullException("colors");
        }

        ChartColor[] copy = new ChartColor[colors.Length];
        int index = 0;
        while (index < colors.Length)
        {
            if (colors[index] == null)
            {
                throw new System.ArgumentException("Colors cannot contain null values.");
            }

            copy[index] = colors[index].Copy();
            index = index + 1;
        }

        return copy;
    }

    internal static double[] CopyOffsets(double[] offsets)
    {
        if (offsets == null)
        {
            throw new System.ArgumentNullException("offsets");
        }

        double[] copy = new double[offsets.Length];
        System.Array.Copy(offsets, copy, offsets.Length);
        return copy;
    }

    internal static RawBitmap CopyBitmap(RawBitmap bitmap)
    {
        if (bitmap == null)
        {
            throw new System.ArgumentNullException("bitmap");
        }

        return bitmap.Copy();
    }

    internal SvgStyle ToSvgStyle()
    {
        SvgStyle style = new SvgStyle();
        if (HasFill)
        {
            if (FillNone)
            {
                style.SetNoFill();
            }
            else
            {
                style.SetFill(FillColor);
            }
        }

        if (HasStroke)
        {
            if (StrokeNone)
            {
                style.SetNoStroke();
            }
            else
            {
                style.SetStroke(StrokeColor, StrokeWidth);
            }
        }

        if (Opacity < 1.0)
        {
            style.SetOpacity(Opacity);
        }

        if (FontFamily.Length > 0)
        {
            style.SetFont(FontFamily, FontSize);
        }

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
        if (HasTextOutline)
        {
            style.SetTextOutline(TextOutlineColor, TextOutlineWidth);
        }

        if (HasTextShadow)
        {
            style.SetTextShadow(TextShadowColor, TextShadowOffsetX, TextShadowOffsetY, TextShadowBlur);
        }

        return style;
    }

    internal ChartColor RasterFillColor()
    {
        return ApplyOpacity(FillColor, Opacity * FillOpacity);
    }

    internal ChartColor RasterStrokeColor()
    {
        return ApplyOpacity(StrokeColor, Opacity * StrokeOpacity);
    }

    internal static ChartColor ApplyOpacity(ChartColor color, double opacity)
    {
        if (color == null)
        {
            throw new System.ArgumentNullException("color");
        }

        if (opacity < 0.0 || opacity > 1.0 || double.IsNaN(opacity) || double.IsInfinity(opacity))
        {
            throw new System.ArgumentOutOfRangeException("opacity");
        }

        byte alpha = ChartColor.RoundToByte(color.A * opacity);
        return new ChartColor(color.R, color.G, color.B, alpha);
    }

    internal static void ValidatePositive(double value, string name)
    {
        ChartLinearScale.ValidateFinite(value, name);
        if (value <= 0.0)
        {
            throw new System.ArgumentOutOfRangeException(name);
        }
    }

    /// <summary>Returns the current has fill.</summary>
    /// <returns>The current field value.</returns>
    public bool GetHasFill()
    {
        return HasFill;
    }

    /// <summary>Replaces the current has fill.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetHasFill(bool value)
    {
        HasFill = value;
    }

    /// <summary>Returns the current fill none.</summary>
    /// <returns>The current field value.</returns>
    public bool GetFillNone()
    {
        return FillNone;
    }

    /// <summary>Replaces the current fill none.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetFillNone(bool value)
    {
        FillNone = value;
    }

    /// <summary>Returns the current fill color.</summary>
    /// <returns>The current field value.</returns>
    public ChartColor GetFillColor()
    {
        return FillColor;
    }

    /// <summary>Replaces the current fill color.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetFillColor(ChartColor value)
    {
        FillColor = value;
    }

    /// <summary>Returns the current has stroke.</summary>
    /// <returns>The current field value.</returns>
    public bool GetHasStroke()
    {
        return HasStroke;
    }

    /// <summary>Replaces the current has stroke.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetHasStroke(bool value)
    {
        HasStroke = value;
    }

    /// <summary>Returns the current stroke none.</summary>
    /// <returns>The current field value.</returns>
    public bool GetStrokeNone()
    {
        return StrokeNone;
    }

    /// <summary>Replaces the current stroke none.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetStrokeNone(bool value)
    {
        StrokeNone = value;
    }

    /// <summary>Returns the current stroke color.</summary>
    /// <returns>The current field value.</returns>
    public ChartColor GetStrokeColor()
    {
        return StrokeColor;
    }

    /// <summary>Replaces the current stroke color.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetStrokeColor(ChartColor value)
    {
        StrokeColor = value;
    }

    /// <summary>Returns the current stroke width.</summary>
    /// <returns>The current field value.</returns>
    public double GetStrokeWidth()
    {
        return StrokeWidth;
    }

    /// <summary>Replaces the current stroke width.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetStrokeWidth(double value)
    {
        StrokeWidth = value;
    }

    /// <summary>Returns the current line cap.</summary>
    /// <returns>The current field value.</returns>
    public ChartLineCap GetLineCap()
    {
        return LineCap;
    }

    /// <summary>Replaces the current line cap.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetLineCap(ChartLineCap value)
    {
        LineCap = value;
    }

    /// <summary>Returns the current line join.</summary>
    /// <returns>The current field value.</returns>
    public ChartLineJoin GetLineJoin()
    {
        return LineJoin;
    }

    /// <summary>Replaces the current line join.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetLineJoin(ChartLineJoin value)
    {
        LineJoin = value;
    }

    /// <summary>Returns the current fill rule.</summary>
    /// <returns>The current field value.</returns>
    public ChartFillRule GetFillRule()
    {
        return FillRule;
    }

    /// <summary>Returns the current opacity.</summary>
    /// <returns>The current field value.</returns>
    public double GetOpacity()
    {
        return Opacity;
    }

    /// <summary>Returns the current font family.</summary>
    /// <returns>The current field value.</returns>
    public string GetFontFamily()
    {
        return FontFamily;
    }

    /// <summary>Replaces the current font family.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetFontFamily(string value)
    {
        FontFamily = value;
    }

    /// <summary>Returns the current font size.</summary>
    /// <returns>The current field value.</returns>
    public int GetFontSize()
    {
        return FontSize;
    }

    /// <summary>Replaces the current font size.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetFontSize(int value)
    {
        FontSize = value;
    }

    /// <summary>Returns the current text scale.</summary>
    /// <returns>The current field value.</returns>
    public int GetTextScale()
    {
        return TextScale;
    }

    /// <summary>Returns the current quality.</summary>
    /// <returns>The current field value.</returns>
    public ChartRenderQuality GetQuality()
    {
        return Quality;
    }

    /// <summary>Returns the current blend mode.</summary>
    /// <returns>The current field value.</returns>
    public ChartBlendMode GetBlendMode()
    {
        return BlendMode;
    }

    /// <summary>Returns an independent copy of the stroke dash array.</summary>
    /// <returns>The configured dash lengths.</returns>
    public double[] GetDashArray()
    {
        return ChartStyle.CopyDashArrayAllowEmpty(DashArray);
    }

    /// <summary>Returns the miter limit.</summary>
    /// <returns>The positive miter limit.</returns>
    public double GetMiterLimit()
    {
        return MiterLimit;
    }

    /// <summary>Returns the independent fill opacity.</summary>
    /// <returns>The opacity from zero through one.</returns>
    public double GetFillOpacity()
    {
        return FillOpacity;
    }

    /// <summary>Returns the independent stroke opacity.</summary>
    /// <returns>The opacity from zero through one.</returns>
    public double GetStrokeOpacity()
    {
        return StrokeOpacity;
    }

    /// <summary>Returns whether the stroke was configured as a border.</summary>
    /// <returns>The current border state.</returns>
    public bool GetHasBorder()
    {
        return HasBorder;
    }

    /// <summary>Returns the border color.</summary>
    /// <returns>An independent color copy.</returns>
    public ChartColor GetBorderColor()
    {
        return BorderColor.Copy();
    }

    /// <summary>Returns the border width.</summary>
    /// <returns>The positive border width.</returns>
    public double GetBorderWidth()
    {
        return BorderWidth;
    }

    /// <summary>Returns the font weight.</summary>
    /// <returns>The configured font weight.</returns>
    public ChartFontWeight GetFontWeight()
    {
        return FontWeight;
    }

    /// <summary>Returns the font posture.</summary>
    /// <returns>The configured font posture.</returns>
    public ChartFontStyle GetFontStyle()
    {
        return FontStyle;
    }

    /// <summary>Returns whether a text outline is enabled.</summary>
    /// <returns>The current outline state.</returns>
    public bool GetHasTextOutline()
    {
        return HasTextOutline;
    }

    /// <summary>Returns the text outline color.</summary>
    /// <returns>An independent color copy.</returns>
    public ChartColor GetTextOutlineColor()
    {
        return TextOutlineColor.Copy();
    }

    /// <summary>Returns the text outline width.</summary>
    /// <returns>The positive outline width when enabled.</returns>
    public double GetTextOutlineWidth()
    {
        return TextOutlineWidth;
    }

    /// <summary>Returns whether a text shadow is enabled.</summary>
    /// <returns>The current shadow state.</returns>
    public bool GetHasTextShadow()
    {
        return HasTextShadow;
    }

    /// <summary>Returns the text shadow color.</summary>
    /// <returns>An independent color copy.</returns>
    public ChartColor GetTextShadowColor()
    {
        return TextShadowColor.Copy();
    }

    /// <summary>Returns the horizontal text shadow offset.</summary>
    /// <returns>The horizontal offset.</returns>
    public double GetTextShadowOffsetX()
    {
        return TextShadowOffsetX;
    }

    /// <summary>Returns the vertical text shadow offset.</summary>
    /// <returns>The vertical offset.</returns>
    public double GetTextShadowOffsetY()
    {
        return TextShadowOffsetY;
    }

    /// <summary>Returns the text shadow blur radius.</summary>
    /// <returns>The nonnegative blur radius.</returns>
    public double GetTextShadowBlur()
    {
        return TextShadowBlur;
    }

    /// <summary>Returns whether a linear fill gradient is enabled.</summary>
    /// <returns>The current gradient state.</returns>
    public bool GetHasFillGradient()
    {
        return HasFillGradient;
    }

    /// <summary>Returns an independent copy of the fill gradient colors.</summary>
    /// <returns>The configured gradient colors.</returns>
    public ChartColor[] GetFillGradientColors()
    {
        return CopyColors(FillGradientColors);
    }

    /// <summary>Returns an independent copy of the fill gradient start point.</summary>
    /// <returns>The configured start point.</returns>
    public ChartPoint GetFillGradientStart()
    {
        return FillGradientStart.Copy();
    }

    /// <summary>Returns an independent copy of the fill gradient end point.</summary>
    /// <returns>The configured end point.</returns>
    public ChartPoint GetFillGradientEnd()
    {
        return FillGradientEnd.Copy();
    }

    /// <summary>Returns an independent copy of the fill gradient offsets.</summary>
    /// <returns>The configured gradient offsets.</returns>
    public double[] GetFillGradientOffsets()
    {
        return CopyOffsets(FillGradientOffsets);
    }

    /// <summary>Returns whether a linear stroke gradient is enabled.</summary>
    /// <returns>The current gradient state.</returns>
    public bool GetHasStrokeGradient()
    {
        return HasStrokeGradient;
    }

    /// <summary>Returns an independent copy of the stroke gradient start point.</summary>
    /// <returns>The configured start point.</returns>
    public ChartPoint GetStrokeGradientStart()
    {
        return StrokeGradientStart.Copy();
    }

    /// <summary>Returns an independent copy of the stroke gradient end point.</summary>
    /// <returns>The configured end point.</returns>
    public ChartPoint GetStrokeGradientEnd()
    {
        return StrokeGradientEnd.Copy();
    }

    /// <summary>Returns an independent copy of the stroke gradient colors.</summary>
    /// <returns>The configured gradient colors.</returns>
    public ChartColor[] GetStrokeGradientColors()
    {
        return CopyColors(StrokeGradientColors);
    }

    /// <summary>Returns an independent copy of the stroke gradient offsets.</summary>
    /// <returns>The configured gradient offsets.</returns>
    public double[] GetStrokeGradientOffsets()
    {
        return CopyOffsets(StrokeGradientOffsets);
    }

    /// <summary>Returns whether a repeating fill pattern is enabled.</summary>
    /// <returns>The current pattern state.</returns>
    public bool GetHasFillPattern()
    {
        return HasFillPattern;
    }

    /// <summary>Returns an independent copy of the fill pattern bitmap.</summary>
    /// <returns>The configured bitmap tile.</returns>
    public RawBitmap GetFillPattern()
    {
        return CopyBitmap(FillPattern);
    }

    /// <summary>Returns whether a repeating stroke pattern is enabled.</summary>
    /// <returns>The current pattern state.</returns>
    public bool GetHasStrokePattern()
    {
        return HasStrokePattern;
    }

    /// <summary>Returns an independent copy of the stroke pattern bitmap.</summary>
    /// <returns>The configured bitmap tile.</returns>
    public RawBitmap GetStrokePattern()
    {
        return CopyBitmap(StrokePattern);
    }
}
