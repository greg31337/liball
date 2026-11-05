namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartRenderStyle"/>.</summary>
public interface IChartRenderStyle
{
    /// <summary>
    /// Sets the fill configuration.
    /// </summary>
    /// <param name = "color">The color value to apply or encode.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    void SetFill(ChartColor color);
    /// <summary>
    /// Disables the fill setting.
    /// </summary>
    void SetNoFill();
    /// <summary>
    /// Sets the stroke configuration.
    /// </summary>
    /// <param name = "color">The color value to apply or encode.</param>
    /// <param name = "strokeWidth">The stroke width in chart units, which must be positive.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    void SetStroke(ChartColor color, double strokeWidth);
    /// <summary>
    /// Disables the stroke setting.
    /// </summary>
    void SetNoStroke();
    /// <summary>Uses a linear color gradient for command fills.</summary>
    /// <param name = "start">The gradient start point in scene coordinates.</param>
    /// <param name = "end">The gradient end point in scene coordinates.</param>
    /// <param name = "colors">The colors at the gradient stops.</param>
    /// <param name = "offsets">The corresponding offsets from zero through one.</param>
    void SetFillGradient(ChartPoint start, ChartPoint end, ChartColor[] colors, double[] offsets);
    /// <summary>Uses a linear color gradient for command strokes.</summary>
    /// <param name = "start">The gradient start point in scene coordinates.</param>
    /// <param name = "end">The gradient end point in scene coordinates.</param>
    /// <param name = "colors">The colors at the gradient stops.</param>
    /// <param name = "offsets">The corresponding offsets from zero through one.</param>
    /// <param name = "strokeWidth">The positive stroke width.</param>
    void SetStrokeGradient(ChartPoint start, ChartPoint end, ChartColor[] colors, double[] offsets, double strokeWidth);
    /// <summary>Uses a repeating raw bitmap for command fills.</summary>
    /// <param name = "bitmap">The raw bitmap tile.</param>
    void SetFillPattern(RawBitmap bitmap);
    /// <summary>Uses a repeating raw bitmap for command strokes.</summary>
    /// <param name = "bitmap">The raw bitmap tile.</param>
    /// <param name = "strokeWidth">The positive stroke width.</param>
    void SetStrokePattern(RawBitmap bitmap, double strokeWidth);
    /// <summary>
    /// Sets the line configuration.
    /// </summary>
    /// <param name = "lineCap">The line cap.</param>
    /// <param name = "lineJoin">The line join.</param>
    void SetLine(ChartLineCap lineCap, ChartLineJoin lineJoin);
    /// <summary>Sets the repeating stroke dash lengths.</summary>
    /// <param name = "dashArray">The positive alternating dash and gap lengths.</param>
    void SetDashArray(double[] dashArray);
    /// <summary>Restores a solid stroke.</summary>
    void ClearDashArray();
    /// <summary>Sets the miter length limit used by miter joins.</summary>
    /// <param name = "miterLimit">The positive miter limit.</param>
    void SetMiterLimit(double miterLimit);
    /// <summary>Sets fill opacity independently from overall opacity.</summary>
    /// <param name = "fillOpacity">The opacity from zero through one.</param>
    void SetFillOpacity(double fillOpacity);
    /// <summary>Sets stroke opacity independently from overall opacity.</summary>
    /// <param name = "strokeOpacity">The opacity from zero through one.</param>
    void SetStrokeOpacity(double strokeOpacity);
    /// <summary>Sets the border used as the stroke around filled commands.</summary>
    /// <param name = "color">The border color.</param>
    /// <param name = "width">The positive border width.</param>
    void SetBorder(ChartColor color, double width);
    /// <summary>
    /// Sets the fill rule configuration.
    /// </summary>
    /// <param name = "fillRule">The fill rule.</param>
    void SetFillRule(ChartFillRule fillRule);
    /// <summary>
    /// Sets the opacity configuration.
    /// </summary>
    /// <param name = "opacity">The opacity from zero for transparent through one for opaque.</param>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    void SetOpacity(double opacity);
    /// <summary>
    /// Sets the font configuration.
    /// </summary>
    /// <param name = "fontFamily">The SVG font-family name; bitmap output uses the built-in glyph shapes.</param>
    /// <param name = "fontSize">The font size in pixels, which must be positive.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    void SetFont(string fontFamily, int fontSize);
    /// <summary>Sets the font weight.</summary>
    /// <param name = "fontWeight">The font weight.</param>
    void SetFontWeight(ChartFontWeight fontWeight);
    /// <summary>Sets the font posture.</summary>
    /// <param name = "fontStyle">The font posture.</param>
    void SetFontStyle(ChartFontStyle fontStyle);
    /// <summary>Sets a text outline.</summary>
    /// <param name = "color">The outline color.</param>
    /// <param name = "width">The positive outline width.</param>
    void SetTextOutline(ChartColor color, double width);
    /// <summary>Clears the text outline.</summary>
    void ClearTextOutline();
    /// <summary>Sets a text shadow.</summary>
    /// <param name = "color">The shadow color.</param>
    /// <param name = "offsetX">The horizontal shadow offset.</param>
    /// <param name = "offsetY">The vertical shadow offset.</param>
    /// <param name = "blur">The nonnegative blur radius.</param>
    void SetTextShadow(ChartColor color, double offsetX, double offsetY, double blur);
    /// <summary>Clears the text shadow.</summary>
    void ClearTextShadow();
    /// <summary>
    /// Sets the text scale configuration.
    /// </summary>
    /// <param name = "textScale">The text scale.</param>
    void SetTextScale(int textScale);
    /// <summary>
    /// Sets the quality configuration.
    /// </summary>
    /// <param name = "quality">The quality.</param>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    void SetQuality(ChartRenderQuality quality);
    /// <summary>
    /// Sets the blend mode configuration.
    /// </summary>
    /// <param name = "blendMode">The blend mode.</param>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    void SetBlendMode(ChartBlendMode blendMode);
    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartRenderStyle Copy();
    /// <summary>Returns the current has fill.</summary>
    /// <returns>The current field value.</returns>
    bool GetHasFill();
    /// <summary>Replaces the current has fill.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetHasFill(bool value);
    /// <summary>Returns the current fill none.</summary>
    /// <returns>The current field value.</returns>
    bool GetFillNone();
    /// <summary>Replaces the current fill none.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetFillNone(bool value);
    /// <summary>Returns the current fill color.</summary>
    /// <returns>The current field value.</returns>
    ChartColor GetFillColor();
    /// <summary>Replaces the current fill color.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetFillColor(ChartColor value);
    /// <summary>Returns the current has stroke.</summary>
    /// <returns>The current field value.</returns>
    bool GetHasStroke();
    /// <summary>Replaces the current has stroke.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetHasStroke(bool value);
    /// <summary>Returns the current stroke none.</summary>
    /// <returns>The current field value.</returns>
    bool GetStrokeNone();
    /// <summary>Replaces the current stroke none.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetStrokeNone(bool value);
    /// <summary>Returns the current stroke color.</summary>
    /// <returns>The current field value.</returns>
    ChartColor GetStrokeColor();
    /// <summary>Replaces the current stroke color.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetStrokeColor(ChartColor value);
    /// <summary>Returns the current stroke width.</summary>
    /// <returns>The current field value.</returns>
    double GetStrokeWidth();
    /// <summary>Replaces the current stroke width.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetStrokeWidth(double value);
    /// <summary>Returns the current line cap.</summary>
    /// <returns>The current field value.</returns>
    ChartLineCap GetLineCap();
    /// <summary>Replaces the current line cap.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetLineCap(ChartLineCap value);
    /// <summary>Returns the current line join.</summary>
    /// <returns>The current field value.</returns>
    ChartLineJoin GetLineJoin();
    /// <summary>Replaces the current line join.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetLineJoin(ChartLineJoin value);
    /// <summary>Returns the current fill rule.</summary>
    /// <returns>The current field value.</returns>
    ChartFillRule GetFillRule();
    /// <summary>Returns the current opacity.</summary>
    /// <returns>The current field value.</returns>
    double GetOpacity();
    /// <summary>Returns the current font family.</summary>
    /// <returns>The current field value.</returns>
    string GetFontFamily();
    /// <summary>Replaces the current font family.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetFontFamily(string value);
    /// <summary>Returns the current font size.</summary>
    /// <returns>The current field value.</returns>
    int GetFontSize();
    /// <summary>Replaces the current font size.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetFontSize(int value);
    /// <summary>Returns the current text scale.</summary>
    /// <returns>The current field value.</returns>
    int GetTextScale();
    /// <summary>Returns the current quality.</summary>
    /// <returns>The current field value.</returns>
    ChartRenderQuality GetQuality();
    /// <summary>Returns the current blend mode.</summary>
    /// <returns>The current field value.</returns>
    ChartBlendMode GetBlendMode();
    /// <summary>Returns an independent copy of the stroke dash array.</summary>
    /// <returns>The configured dash lengths.</returns>
    double[] GetDashArray();
    /// <summary>Returns the miter limit.</summary>
    /// <returns>The positive miter limit.</returns>
    double GetMiterLimit();
    /// <summary>Returns the independent fill opacity.</summary>
    /// <returns>The opacity from zero through one.</returns>
    double GetFillOpacity();
    /// <summary>Returns the independent stroke opacity.</summary>
    /// <returns>The opacity from zero through one.</returns>
    double GetStrokeOpacity();
    /// <summary>Returns whether the stroke was configured as a border.</summary>
    /// <returns>The current border state.</returns>
    bool GetHasBorder();
    /// <summary>Returns the border color.</summary>
    /// <returns>An independent color copy.</returns>
    ChartColor GetBorderColor();
    /// <summary>Returns the border width.</summary>
    /// <returns>The positive border width.</returns>
    double GetBorderWidth();
    /// <summary>Returns the font weight.</summary>
    /// <returns>The configured font weight.</returns>
    ChartFontWeight GetFontWeight();
    /// <summary>Returns the font posture.</summary>
    /// <returns>The configured font posture.</returns>
    ChartFontStyle GetFontStyle();
    /// <summary>Returns whether a text outline is enabled.</summary>
    /// <returns>The current outline state.</returns>
    bool GetHasTextOutline();
    /// <summary>Returns the text outline color.</summary>
    /// <returns>An independent color copy.</returns>
    ChartColor GetTextOutlineColor();
    /// <summary>Returns the text outline width.</summary>
    /// <returns>The positive outline width when enabled.</returns>
    double GetTextOutlineWidth();
    /// <summary>Returns whether a text shadow is enabled.</summary>
    /// <returns>The current shadow state.</returns>
    bool GetHasTextShadow();
    /// <summary>Returns the text shadow color.</summary>
    /// <returns>An independent color copy.</returns>
    ChartColor GetTextShadowColor();
    /// <summary>Returns the horizontal text shadow offset.</summary>
    /// <returns>The horizontal offset.</returns>
    double GetTextShadowOffsetX();
    /// <summary>Returns the vertical text shadow offset.</summary>
    /// <returns>The vertical offset.</returns>
    double GetTextShadowOffsetY();
    /// <summary>Returns the text shadow blur radius.</summary>
    /// <returns>The nonnegative blur radius.</returns>
    double GetTextShadowBlur();
    /// <summary>Returns whether a linear fill gradient is enabled.</summary>
    /// <returns>The current gradient state.</returns>
    bool GetHasFillGradient();
    /// <summary>Returns an independent copy of the fill gradient colors.</summary>
    /// <returns>The configured gradient colors.</returns>
    ChartColor[] GetFillGradientColors();
    /// <summary>Returns an independent copy of the fill gradient start point.</summary>
    /// <returns>The configured start point.</returns>
    ChartPoint GetFillGradientStart();
    /// <summary>Returns an independent copy of the fill gradient end point.</summary>
    /// <returns>The configured end point.</returns>
    ChartPoint GetFillGradientEnd();
    /// <summary>Returns an independent copy of the fill gradient offsets.</summary>
    /// <returns>The configured gradient offsets.</returns>
    double[] GetFillGradientOffsets();
    /// <summary>Returns whether a linear stroke gradient is enabled.</summary>
    /// <returns>The current gradient state.</returns>
    bool GetHasStrokeGradient();
    /// <summary>Returns an independent copy of the stroke gradient start point.</summary>
    /// <returns>The configured start point.</returns>
    ChartPoint GetStrokeGradientStart();
    /// <summary>Returns an independent copy of the stroke gradient end point.</summary>
    /// <returns>The configured end point.</returns>
    ChartPoint GetStrokeGradientEnd();
    /// <summary>Returns an independent copy of the stroke gradient colors.</summary>
    /// <returns>The configured gradient colors.</returns>
    ChartColor[] GetStrokeGradientColors();
    /// <summary>Returns an independent copy of the stroke gradient offsets.</summary>
    /// <returns>The configured gradient offsets.</returns>
    double[] GetStrokeGradientOffsets();
    /// <summary>Returns whether a repeating fill pattern is enabled.</summary>
    /// <returns>The current pattern state.</returns>
    bool GetHasFillPattern();
    /// <summary>Returns an independent copy of the fill pattern bitmap.</summary>
    /// <returns>The configured bitmap tile.</returns>
    RawBitmap GetFillPattern();
    /// <summary>Returns whether a repeating stroke pattern is enabled.</summary>
    /// <returns>The current pattern state.</returns>
    bool GetHasStrokePattern();
    /// <summary>Returns an independent copy of the stroke pattern bitmap.</summary>
    /// <returns>The configured bitmap tile.</returns>
    RawBitmap GetStrokePattern();
}
