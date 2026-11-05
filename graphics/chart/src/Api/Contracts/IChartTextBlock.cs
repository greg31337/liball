namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartTextBlock"/>.</summary>
public interface IChartTextBlock
{
    /// <summary>
    /// Sets the text configuration.
    /// </summary>
    /// <param name = "text">The text to measure, render, or encode.</param>
    void SetText(string text);
    /// <summary>
    /// Sets the style configuration.
    /// </summary>
    /// <param name = "color">The color value to apply or encode.</param>
    /// <param name = "fontFamily">The SVG font-family name; bitmap output uses the built-in glyph shapes.</param>
    /// <param name = "fontSize">The font size in pixels, which must be positive.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    void SetStyle(ChartColor color, string fontFamily, int fontSize);
    /// <summary>
    /// Sets the spacing configuration.
    /// </summary>
    /// <param name = "spacing">The spacing.</param>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    void SetSpacing(double spacing);
    /// <summary>Measures this block's text using the deterministic built-in five-by-seven bitmap font.</summary>
    /// <param name = "scale">The positive integer glyph scale.</param>
    /// <returns>The measured dimensions and line metrics.</returns>
    ChartTextMeasurement MeasureBuiltIn(int scale);
    /// <summary>Enables deterministic word wrapping at the supplied pixel width.</summary>
    /// <param name = "maximumWidth">The positive maximum line width in built-in-font pixels.</param>
    void SetWrapping(int maximumWidth);
    /// <summary>Disables automatic wrapping while preserving explicit newlines.</summary>
    void ClearWrapping();
    /// <summary>Sets horizontal and vertical anchor alignment.</summary>
    /// <param name = "alignment">The horizontal alignment.</param>
    /// <param name = "verticalAlignment">The vertical alignment.</param>
    void SetAlignment(ChartTextAlignment alignment, ChartVerticalTextAlignment verticalAlignment);
    /// <summary>Sets clockwise rotation in degrees for rotation planning and output.</summary>
    /// <param name = "rotationDegrees">The finite rotation angle in degrees.</param>
    void SetRotation(double rotationDegrees);
    /// <summary>Sets the rectangle that clips text output.</summary>
    /// <param name = "clipBounds">The positive clipping rectangle.</param>
    void SetClip(ChartRect clipBounds);
    /// <summary>Clears text clipping.</summary>
    void ClearClip();
    /// <summary>Sets deterministic left-to-right or right-to-left text-element ordering.</summary>
    /// <param name = "direction">The requested ordering direction.</param>
    void SetDirection(ChartTextDirection direction);
    /// <summary>Adds a copied styled span and appends its text to this block.</summary>
    /// <param name = "span">The rich-text span.</param>
    /// <returns>The copied stored span.</returns>
    ChartTextSpan AddRichSpan(ChartTextSpan span);
    /// <summary>Clears all styled spans and their composed text.</summary>
    void ClearRichSpans();
    /// <summary>Returns built-in-font text after direction ordering and one-glyph fallback substitution.</summary>
    /// <returns>The prepared deterministic text.</returns>
    string PreparedBuiltInText();
    /// <summary>Returns prepared built-in-font text with configured wrapping applied.</summary>
    /// <param name = "scale">The positive integer glyph scale.</param>
    /// <returns>The prepared and wrapped deterministic text.</returns>
    string WrappedBuiltInText(int scale);
    /// <summary>Creates a deterministic positioned built-in-font layout.</summary>
    /// <param name = "anchorX">The horizontal anchor coordinate.</param>
    /// <param name = "anchorY">The vertical anchor coordinate.</param>
    /// <param name = "scale">The positive integer glyph scale.</param>
    /// <returns>The positioned text layout, including configured clipping metadata.</returns>
    ChartTextLayout LayoutBuiltIn(int anchorX, int anchorY, int scale);
    /// <summary>Plans configured rotation around a supplied pivot.</summary>
    /// <param name = "layout">The text layout to rotate.</param>
    /// <param name = "pivotX">The horizontal rotation pivot.</param>
    /// <param name = "pivotY">The vertical rotation pivot.</param>
    /// <returns>The rotated bounds and corners.</returns>
    ChartTextRotationPlan PlanBuiltInRotation(ChartTextLayout layout, double pivotX, double pivotY);
    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartTextBlock Copy();
    /// <summary>Returns the current text.</summary>
    /// <returns>The current field value.</returns>
    string GetText();
    /// <summary>Returns the current visible.</summary>
    /// <returns>The current field value.</returns>
    bool GetVisible();
    /// <summary>Replaces the current visible.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetVisible(bool value);
    /// <summary>Returns the current color.</summary>
    /// <returns>The current field value.</returns>
    ChartColor GetColor();
    /// <summary>Replaces the current color.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetColor(ChartColor value);
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
    /// <summary>Returns the current spacing.</summary>
    /// <returns>The current field value.</returns>
    double GetSpacing();
    /// <summary>Returns the wrapping width, or zero when wrapping is disabled.</summary>
    /// <returns>The maximum width.</returns>
    int GetMaximumWidth();
    /// <summary>Returns the horizontal alignment.</summary>
    /// <returns>The configured alignment.</returns>
    ChartTextAlignment GetAlignment();
    /// <summary>Returns the vertical alignment.</summary>
    /// <returns>The configured vertical alignment.</returns>
    ChartVerticalTextAlignment GetVerticalAlignment();
    /// <summary>Returns clockwise rotation in degrees.</summary>
    /// <returns>The configured rotation angle.</returns>
    double GetRotation();
    /// <summary>Returns whether clipping is enabled.</summary>
    /// <returns>The clipping state.</returns>
    bool GetHasClip();
    /// <summary>Returns an independent clipping rectangle.</summary>
    /// <returns>The configured clip.</returns>
    ChartRect GetClipBounds();
    /// <summary>Returns the text-element ordering direction.</summary>
    /// <returns>The configured direction.</returns>
    ChartTextDirection GetDirection();
    /// <summary>Returns the number of stored rich-text spans.</summary>
    /// <returns>The span count.</returns>
    int GetRichSpanCount();
    /// <summary>Returns one stored rich-text span.</summary>
    /// <param name = "index">The zero-based span index.</param>
    /// <returns>The requested stored span.</returns>
    ChartTextSpan RichSpanAt(int index);
}
