namespace chart;
/// <summary>
/// Represents a style used by the chart model or rendering pipeline.
/// </summary>
public class ChartStyle : IChartStyle
{
    internal bool HasFillColor;
    internal bool FillNone;
    internal ChartColor FillColor;
    internal bool HasStrokeColor;
    internal bool StrokeNone;
    internal ChartColor StrokeColor;
    internal bool HasStrokeWidth;
    internal double StrokeWidth;
    internal bool HasOpacity;
    internal double Opacity;
    internal bool HasFontFamily;
    internal string FontFamily;
    internal bool HasFontSize;
    internal int FontSize;
    internal bool HasMarkerSize;
    internal double MarkerSize;
    internal bool HasDashArray;
    internal double[] DashArray;
    internal bool HasMiterLimit;
    internal double MiterLimit;
    internal bool HasFillOpacity;
    internal double FillOpacity;
    internal bool HasStrokeOpacity;
    internal double StrokeOpacity;
    internal bool HasBorder;
    internal bool BorderNone;
    internal ChartColor BorderColor;
    internal double BorderWidth;
    internal bool HasFontWeight;
    internal ChartFontWeight FontWeight;
    internal bool HasFontStyle;
    internal ChartFontStyle FontStyle;
    internal bool HasTextOutline;
    internal ChartColor TextOutlineColor;
    internal double TextOutlineWidth;
    internal bool HasTextShadow;
    internal ChartColor TextShadowColor;
    internal double TextShadowOffsetX;
    internal double TextShadowOffsetY;
    internal double TextShadowBlur;
    internal bool HasCustomMarker;
    internal ChartPath CustomMarkerPath;
    /// <summary>
    /// Initializes an empty style with library defaults.
    /// </summary>
    public ChartStyle()
    {
        FillColor = ChartColor.Black();
        StrokeColor = ChartColor.Black();
        FontFamily = "";
        Opacity = 1.0;
        MarkerSize = 4.0;
        DashArray = new double[0];
        MiterLimit = 4.0;
        FillOpacity = 1.0;
        StrokeOpacity = 1.0;
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

        HasFillColor = true;
        FillNone = false;
        FillColor = color.Copy();
    }

    /// <summary>
    /// Disables the fill setting.
    /// </summary>
    public void SetNoFill()
    {
        HasFillColor = true;
        FillNone = true;
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

        ValidateNonNegative(strokeWidth, "strokeWidth");
        HasStrokeColor = true;
        StrokeNone = false;
        StrokeColor = color.Copy();
        HasStrokeWidth = true;
        StrokeWidth = strokeWidth;
    }

    /// <summary>
    /// Disables the stroke setting.
    /// </summary>
    public void SetNoStroke()
    {
        HasStrokeColor = true;
        StrokeNone = true;
    }

    /// <summary>
    /// Sets the stroke width configuration.
    /// </summary>
    /// <param name = "strokeWidth">The stroke width in chart units, which must be positive.</param>
    public void SetStrokeWidth(double strokeWidth)
    {
        ValidateNonNegative(strokeWidth, "strokeWidth");
        HasStrokeWidth = true;
        StrokeWidth = strokeWidth;
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

        HasOpacity = true;
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

        HasFontFamily = true;
        FontFamily = fontFamily;
        HasFontSize = true;
        FontSize = fontSize;
    }

    /// <summary>
    /// Sets the marker size configuration.
    /// </summary>
    /// <param name = "markerSize">The marker size.</param>
    public void SetMarkerSize(double markerSize)
    {
        ValidateNonNegative(markerSize, "markerSize");
        HasMarkerSize = true;
        MarkerSize = markerSize;
    }

    /// <summary>Sets the repeating stroke dash lengths.</summary>
    /// <param name = "dashArray">The positive alternating dash and gap lengths.</param>
    public void SetDashArray(double[] dashArray)
    {
        DashArray = CopyDashArray(dashArray);
        HasDashArray = true;
    }

    /// <summary>Restores a solid stroke.</summary>
    public void ClearDashArray()
    {
        HasDashArray = true;
        DashArray = new double[0];
    }

    /// <summary>Sets the miter length limit used by miter joins.</summary>
    /// <param name = "miterLimit">The positive miter limit.</param>
    public void SetMiterLimit(double miterLimit)
    {
        ChartRenderStyle.ValidatePositive(miterLimit, "miterLimit");
        HasMiterLimit = true;
        MiterLimit = miterLimit;
    }

    /// <summary>Sets fill opacity independently from overall opacity.</summary>
    /// <param name = "fillOpacity">The opacity from zero through one.</param>
    public void SetFillOpacity(double fillOpacity)
    {
        ValidateOpacity(fillOpacity, "fillOpacity");
        HasFillOpacity = true;
        FillOpacity = fillOpacity;
    }

    /// <summary>Sets stroke opacity independently from overall opacity.</summary>
    /// <param name = "strokeOpacity">The opacity from zero through one.</param>
    public void SetStrokeOpacity(double strokeOpacity)
    {
        ValidateOpacity(strokeOpacity, "strokeOpacity");
        HasStrokeOpacity = true;
        StrokeOpacity = strokeOpacity;
    }

    /// <summary>Sets a border that is resolved as the outer stroke of a filled item.</summary>
    /// <param name = "color">The border color.</param>
    /// <param name = "width">The positive border width.</param>
    public void SetBorder(ChartColor color, double width)
    {
        if (color == null)
        {
            throw new System.ArgumentNullException("color");
        }

        ChartRenderStyle.ValidatePositive(width, "width");
        HasBorder = true;
        BorderNone = false;
        BorderColor = color.Copy();
        BorderWidth = width;
    }

    /// <summary>Disables the border.</summary>
    public void SetNoBorder()
    {
        HasBorder = true;
        BorderNone = true;
    }

    /// <summary>Sets the font weight.</summary>
    /// <param name = "fontWeight">The font weight.</param>
    public void SetFontWeight(ChartFontWeight fontWeight)
    {
        ValidateFontWeight(fontWeight);
        HasFontWeight = true;
        FontWeight = fontWeight;
    }

    /// <summary>Sets the font posture.</summary>
    /// <param name = "fontStyle">The font posture.</param>
    public void SetFontStyle(ChartFontStyle fontStyle)
    {
        ValidateFontStyle(fontStyle);
        HasFontStyle = true;
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

        ChartRenderStyle.ValidatePositive(width, "width");
        HasTextOutline = true;
        TextOutlineColor = color.Copy();
        TextOutlineWidth = width;
    }

    /// <summary>Clears the text outline.</summary>
    public void ClearTextOutline()
    {
        HasTextOutline = true;
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
        ValidateNonNegative(blur, "blur");
        HasTextShadow = true;
        TextShadowColor = color.Copy();
        TextShadowOffsetX = offsetX;
        TextShadowOffsetY = offsetY;
        TextShadowBlur = blur;
    }

    /// <summary>Clears the text shadow.</summary>
    public void ClearTextShadow()
    {
        HasTextShadow = true;
        TextShadowColor = ChartColor.Transparent();
        TextShadowOffsetX = 0.0;
        TextShadowOffsetY = 0.0;
        TextShadowBlur = 0.0;
    }

    /// <summary>Sets a reusable normalized custom marker path.</summary>
    /// <param name = "markerPath">The custom marker path.</param>
    public void SetCustomMarker(ChartPath markerPath)
    {
        CustomMarkerPath = ChartRenderCommand.CopyPath(markerPath);
        HasCustomMarker = true;
    }

    /// <summary>Clears the custom marker definition.</summary>
    public void ClearCustomMarker()
    {
        HasCustomMarker = true;
        CustomMarkerPath = new ChartPath();
    }

    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartStyle Copy()
    {
        ChartStyle style = new ChartStyle();
        style.HasFillColor = HasFillColor;
        style.FillNone = FillNone;
        style.FillColor = FillColor.Copy();
        style.HasStrokeColor = HasStrokeColor;
        style.StrokeNone = StrokeNone;
        style.StrokeColor = StrokeColor.Copy();
        style.HasStrokeWidth = HasStrokeWidth;
        style.StrokeWidth = StrokeWidth;
        style.HasOpacity = HasOpacity;
        style.Opacity = Opacity;
        style.HasFontFamily = HasFontFamily;
        style.FontFamily = FontFamily;
        style.HasFontSize = HasFontSize;
        style.FontSize = FontSize;
        style.HasMarkerSize = HasMarkerSize;
        style.MarkerSize = MarkerSize;
        style.HasDashArray = HasDashArray;
        style.DashArray = CopyDashArrayAllowEmpty(DashArray);
        style.HasMiterLimit = HasMiterLimit;
        style.MiterLimit = MiterLimit;
        style.HasFillOpacity = HasFillOpacity;
        style.FillOpacity = FillOpacity;
        style.HasStrokeOpacity = HasStrokeOpacity;
        style.StrokeOpacity = StrokeOpacity;
        style.HasBorder = HasBorder;
        style.BorderNone = BorderNone;
        style.BorderColor = BorderColor.Copy();
        style.BorderWidth = BorderWidth;
        style.HasFontWeight = HasFontWeight;
        style.FontWeight = FontWeight;
        style.HasFontStyle = HasFontStyle;
        style.FontStyle = FontStyle;
        style.HasTextOutline = HasTextOutline;
        style.TextOutlineColor = TextOutlineColor.Copy();
        style.TextOutlineWidth = TextOutlineWidth;
        style.HasTextShadow = HasTextShadow;
        style.TextShadowColor = TextShadowColor.Copy();
        style.TextShadowOffsetX = TextShadowOffsetX;
        style.TextShadowOffsetY = TextShadowOffsetY;
        style.TextShadowBlur = TextShadowBlur;
        style.HasCustomMarker = HasCustomMarker;
        style.CustomMarkerPath = ChartRenderCommand.CopyPath(CustomMarkerPath);
        return style;
    }

    internal static void ValidateNonNegative(double value, string name)
    {
        ChartLinearScale.ValidateFinite(value, name);
        if (value < 0.0)
        {
            throw new System.ArgumentOutOfRangeException(name);
        }
    }

    internal static void ValidateOpacity(double value, string name)
    {
        if (value < 0.0 || value > 1.0 || double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new System.ArgumentOutOfRangeException(name);
        }
    }

    internal static double[] CopyDashArray(double[] values)
    {
        if (values == null)
        {
            throw new System.ArgumentNullException("dashArray");
        }

        if (values.Length == 0)
        {
            throw new System.ArgumentException("Dash array must contain at least one length.");
        }

        int index = 0;
        while (index < values.Length)
        {
            ChartRenderStyle.ValidatePositive(values[index], "dashArray");
            index = index + 1;
        }

        return CopyDashArrayAllowEmpty(values);
    }

    internal static double[] CopyDashArrayAllowEmpty(double[] values)
    {
        double[] copy = new double[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }

    internal static void ValidateFontWeight(ChartFontWeight fontWeight)
    {
        int value = (int)fontWeight;
        if (value < 0 || value > 1)
        {
            throw new System.ArgumentOutOfRangeException("fontWeight");
        }
    }

    internal static void ValidateFontStyle(ChartFontStyle fontStyle)
    {
        int value = (int)fontStyle;
        if (value < 0 || value > 2)
        {
            throw new System.ArgumentOutOfRangeException("fontStyle");
        }
    }

    /// <summary>Returns the current has fill color.</summary>
    /// <returns>The current field value.</returns>
    public bool GetHasFillColor()
    {
        return HasFillColor;
    }

    /// <summary>Replaces the current has fill color.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetHasFillColor(bool value)
    {
        HasFillColor = value;
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

    /// <summary>Returns the current has stroke color.</summary>
    /// <returns>The current field value.</returns>
    public bool GetHasStrokeColor()
    {
        return HasStrokeColor;
    }

    /// <summary>Replaces the current has stroke color.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetHasStrokeColor(bool value)
    {
        HasStrokeColor = value;
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

    /// <summary>Returns the current has stroke width.</summary>
    /// <returns>The current field value.</returns>
    public bool GetHasStrokeWidth()
    {
        return HasStrokeWidth;
    }

    /// <summary>Replaces the current has stroke width.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetHasStrokeWidth(bool value)
    {
        HasStrokeWidth = value;
    }

    /// <summary>Returns the current stroke width.</summary>
    /// <returns>The current field value.</returns>
    public double GetStrokeWidth()
    {
        return StrokeWidth;
    }

    /// <summary>Returns the current has opacity.</summary>
    /// <returns>The current field value.</returns>
    public bool GetHasOpacity()
    {
        return HasOpacity;
    }

    /// <summary>Replaces the current has opacity.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetHasOpacity(bool value)
    {
        HasOpacity = value;
    }

    /// <summary>Returns the current opacity.</summary>
    /// <returns>The current field value.</returns>
    public double GetOpacity()
    {
        return Opacity;
    }

    /// <summary>Returns the current has font family.</summary>
    /// <returns>The current field value.</returns>
    public bool GetHasFontFamily()
    {
        return HasFontFamily;
    }

    /// <summary>Replaces the current has font family.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetHasFontFamily(bool value)
    {
        HasFontFamily = value;
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

    /// <summary>Returns the current has font size.</summary>
    /// <returns>The current field value.</returns>
    public bool GetHasFontSize()
    {
        return HasFontSize;
    }

    /// <summary>Replaces the current has font size.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetHasFontSize(bool value)
    {
        HasFontSize = value;
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

    /// <summary>Returns the current has marker size.</summary>
    /// <returns>The current field value.</returns>
    public bool GetHasMarkerSize()
    {
        return HasMarkerSize;
    }

    /// <summary>Replaces the current has marker size.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetHasMarkerSize(bool value)
    {
        HasMarkerSize = value;
    }

    /// <summary>Returns the current marker size.</summary>
    /// <returns>The current field value.</returns>
    public double GetMarkerSize()
    {
        return MarkerSize;
    }

    /// <summary>Returns whether a dash array override is present.</summary>
    /// <returns>The current override state.</returns>
    public bool GetHasDashArray()
    {
        return HasDashArray;
    }

    /// <summary>Returns an independent copy of the stroke dash array.</summary>
    /// <returns>The configured dash lengths.</returns>
    public double[] GetDashArray()
    {
        return CopyDashArrayAllowEmpty(DashArray);
    }

    /// <summary>Returns whether a miter limit override is present.</summary>
    /// <returns>The current override state.</returns>
    public bool GetHasMiterLimit()
    {
        return HasMiterLimit;
    }

    /// <summary>Returns the configured miter limit.</summary>
    /// <returns>The positive miter limit.</returns>
    public double GetMiterLimit()
    {
        return MiterLimit;
    }

    /// <summary>Returns whether a fill opacity override is present.</summary>
    /// <returns>The current override state.</returns>
    public bool GetHasFillOpacity()
    {
        return HasFillOpacity;
    }

    /// <summary>Returns the independent fill opacity.</summary>
    /// <returns>The opacity from zero through one.</returns>
    public double GetFillOpacity()
    {
        return FillOpacity;
    }

    /// <summary>Returns whether a stroke opacity override is present.</summary>
    /// <returns>The current override state.</returns>
    public bool GetHasStrokeOpacity()
    {
        return HasStrokeOpacity;
    }

    /// <summary>Returns the independent stroke opacity.</summary>
    /// <returns>The opacity from zero through one.</returns>
    public double GetStrokeOpacity()
    {
        return StrokeOpacity;
    }

    /// <summary>Returns whether a border override is present.</summary>
    /// <returns>The current override state.</returns>
    public bool GetHasBorder()
    {
        return HasBorder;
    }

    /// <summary>Returns whether the border is disabled.</summary>
    /// <returns><c>true</c> when no border is requested.</returns>
    public bool GetBorderNone()
    {
        return BorderNone;
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

    /// <summary>Returns whether a font weight override is present.</summary>
    /// <returns>The current override state.</returns>
    public bool GetHasFontWeight()
    {
        return HasFontWeight;
    }

    /// <summary>Returns the font weight.</summary>
    /// <returns>The configured font weight.</returns>
    public ChartFontWeight GetFontWeight()
    {
        return FontWeight;
    }

    /// <summary>Returns whether a font style override is present.</summary>
    /// <returns>The current override state.</returns>
    public bool GetHasFontStyle()
    {
        return HasFontStyle;
    }

    /// <summary>Returns the font posture.</summary>
    /// <returns>The configured font posture.</returns>
    public ChartFontStyle GetFontStyle()
    {
        return FontStyle;
    }

    /// <summary>Returns whether a text outline override is present.</summary>
    /// <returns>The current override state.</returns>
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
    /// <returns>The nonnegative outline width.</returns>
    public double GetTextOutlineWidth()
    {
        return TextOutlineWidth;
    }

    /// <summary>Returns whether a text shadow override is present.</summary>
    /// <returns>The current override state.</returns>
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

    /// <summary>Returns whether a custom marker override is present.</summary>
    /// <returns>The current override state.</returns>
    public bool GetHasCustomMarker()
    {
        return HasCustomMarker;
    }

    /// <summary>Returns an independent copy of the custom marker path.</summary>
    /// <returns>The configured custom marker definition.</returns>
    public ChartPath GetCustomMarker()
    {
        return ChartRenderCommand.CopyPath(CustomMarkerPath);
    }
}
