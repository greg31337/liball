namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartStyle"/>.</summary>
public interface IChartStyle
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
    /// <summary>
    /// Sets the stroke width configuration.
    /// </summary>
    /// <param name = "strokeWidth">The stroke width in chart units, which must be positive.</param>
    void SetStrokeWidth(double strokeWidth);
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
    /// <summary>
    /// Sets the marker size configuration.
    /// </summary>
    /// <param name = "markerSize">The marker size.</param>
    void SetMarkerSize(double markerSize);
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
    /// <summary>Sets a border that is resolved as the outer stroke of a filled item.</summary>
    /// <param name = "color">The border color.</param>
    /// <param name = "width">The positive border width.</param>
    void SetBorder(ChartColor color, double width);
    /// <summary>Disables the border.</summary>
    void SetNoBorder();
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
    /// <summary>Sets a reusable normalized custom marker path.</summary>
    /// <param name = "markerPath">The custom marker path.</param>
    void SetCustomMarker(ChartPath markerPath);
    /// <summary>Clears the custom marker definition.</summary>
    void ClearCustomMarker();
    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartStyle Copy();
    /// <summary>Returns the current has fill color.</summary>
    /// <returns>The current field value.</returns>
    bool GetHasFillColor();
    /// <summary>Replaces the current has fill color.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetHasFillColor(bool value);
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
    /// <summary>Returns the current has stroke color.</summary>
    /// <returns>The current field value.</returns>
    bool GetHasStrokeColor();
    /// <summary>Replaces the current has stroke color.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetHasStrokeColor(bool value);
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
    /// <summary>Returns the current has stroke width.</summary>
    /// <returns>The current field value.</returns>
    bool GetHasStrokeWidth();
    /// <summary>Replaces the current has stroke width.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetHasStrokeWidth(bool value);
    /// <summary>Returns the current stroke width.</summary>
    /// <returns>The current field value.</returns>
    double GetStrokeWidth();
    /// <summary>Returns the current has opacity.</summary>
    /// <returns>The current field value.</returns>
    bool GetHasOpacity();
    /// <summary>Replaces the current has opacity.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetHasOpacity(bool value);
    /// <summary>Returns the current opacity.</summary>
    /// <returns>The current field value.</returns>
    double GetOpacity();
    /// <summary>Returns the current has font family.</summary>
    /// <returns>The current field value.</returns>
    bool GetHasFontFamily();
    /// <summary>Replaces the current has font family.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetHasFontFamily(bool value);
    /// <summary>Returns the current font family.</summary>
    /// <returns>The current field value.</returns>
    string GetFontFamily();
    /// <summary>Replaces the current font family.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetFontFamily(string value);
    /// <summary>Returns the current has font size.</summary>
    /// <returns>The current field value.</returns>
    bool GetHasFontSize();
    /// <summary>Replaces the current has font size.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetHasFontSize(bool value);
    /// <summary>Returns the current font size.</summary>
    /// <returns>The current field value.</returns>
    int GetFontSize();
    /// <summary>Replaces the current font size.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetFontSize(int value);
    /// <summary>Returns the current has marker size.</summary>
    /// <returns>The current field value.</returns>
    bool GetHasMarkerSize();
    /// <summary>Replaces the current has marker size.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetHasMarkerSize(bool value);
    /// <summary>Returns the current marker size.</summary>
    /// <returns>The current field value.</returns>
    double GetMarkerSize();
    /// <summary>Returns whether a dash array override is present.</summary>
    /// <returns>The current override state.</returns>
    bool GetHasDashArray();
    /// <summary>Returns an independent copy of the stroke dash array.</summary>
    /// <returns>The configured dash lengths.</returns>
    double[] GetDashArray();
    /// <summary>Returns whether a miter limit override is present.</summary>
    /// <returns>The current override state.</returns>
    bool GetHasMiterLimit();
    /// <summary>Returns the configured miter limit.</summary>
    /// <returns>The positive miter limit.</returns>
    double GetMiterLimit();
    /// <summary>Returns whether a fill opacity override is present.</summary>
    /// <returns>The current override state.</returns>
    bool GetHasFillOpacity();
    /// <summary>Returns the independent fill opacity.</summary>
    /// <returns>The opacity from zero through one.</returns>
    double GetFillOpacity();
    /// <summary>Returns whether a stroke opacity override is present.</summary>
    /// <returns>The current override state.</returns>
    bool GetHasStrokeOpacity();
    /// <summary>Returns the independent stroke opacity.</summary>
    /// <returns>The opacity from zero through one.</returns>
    double GetStrokeOpacity();
    /// <summary>Returns whether a border override is present.</summary>
    /// <returns>The current override state.</returns>
    bool GetHasBorder();
    /// <summary>Returns whether the border is disabled.</summary>
    /// <returns><c>true</c> when no border is requested.</returns>
    bool GetBorderNone();
    /// <summary>Returns the border color.</summary>
    /// <returns>An independent color copy.</returns>
    ChartColor GetBorderColor();
    /// <summary>Returns the border width.</summary>
    /// <returns>The positive border width.</returns>
    double GetBorderWidth();
    /// <summary>Returns whether a font weight override is present.</summary>
    /// <returns>The current override state.</returns>
    bool GetHasFontWeight();
    /// <summary>Returns the font weight.</summary>
    /// <returns>The configured font weight.</returns>
    ChartFontWeight GetFontWeight();
    /// <summary>Returns whether a font style override is present.</summary>
    /// <returns>The current override state.</returns>
    bool GetHasFontStyle();
    /// <summary>Returns the font posture.</summary>
    /// <returns>The configured font posture.</returns>
    ChartFontStyle GetFontStyle();
    /// <summary>Returns whether a text outline override is present.</summary>
    /// <returns>The current override state.</returns>
    bool GetHasTextOutline();
    /// <summary>Returns the text outline color.</summary>
    /// <returns>An independent color copy.</returns>
    ChartColor GetTextOutlineColor();
    /// <summary>Returns the text outline width.</summary>
    /// <returns>The nonnegative outline width.</returns>
    double GetTextOutlineWidth();
    /// <summary>Returns whether a text shadow override is present.</summary>
    /// <returns>The current override state.</returns>
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
    /// <summary>Returns whether a custom marker override is present.</summary>
    /// <returns>The current override state.</returns>
    bool GetHasCustomMarker();
    /// <summary>Returns an independent copy of the custom marker path.</summary>
    /// <returns>The configured custom marker definition.</returns>
    ChartPath GetCustomMarker();
}
