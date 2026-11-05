namespace chart;
/// <summary>
/// Represents a text block used by the chart model or rendering pipeline.
/// </summary>
public class ChartTextBlock : IChartTextBlock
{
    internal string Text;
    internal bool Visible;
    internal ChartColor Color;
    internal string FontFamily;
    internal int FontSize;
    internal double Spacing;
    internal int MaximumWidth;
    internal ChartTextAlignment Alignment;
    internal ChartVerticalTextAlignment VerticalAlignment;
    internal double RotationDegrees;
    internal bool HasClip;
    internal ChartRect ClipBounds;
    internal ChartTextDirection Direction;
    internal ChartTextSpan[] RichSpans;
    internal int RichSpanCount;
    /// <summary>
    /// Initializes an empty text block with library defaults.
    /// </summary>
    public ChartTextBlock()
    {
        Text = "";
        Visible = false;
        Color = ChartColor.Black();
        FontFamily = "Arial";
        FontSize = 12;
        Spacing = 8.0;
        MaximumWidth = 0;
        Alignment = ChartTextAlignment.Left;
        VerticalAlignment = ChartVerticalTextAlignment.Top;
        RotationDegrees = 0.0;
        HasClip = false;
        ClipBounds = new ChartRect(0.0, 0.0, 1.0, 1.0);
        Direction = ChartTextDirection.LeftToRight;
        RichSpans = new ChartTextSpan[4];
        RichSpanCount = 0;
    }

    internal static ChartTextBlock Create(string text)
    {
        ChartTextBlock block = new ChartTextBlock();
        block.SetText(text);
        return block;
    }

    /// <summary>
    /// Sets the text configuration.
    /// </summary>
    /// <param name = "text">The text to measure, render, or encode.</param>
    public void SetText(string text)
    {
        ValidateText(text, "text");
        Text = text;
        Visible = text.Length > 0;
        RichSpanCount = 0;
    }

    /// <summary>
    /// Sets the style configuration.
    /// </summary>
    /// <param name = "color">The color value to apply or encode.</param>
    /// <param name = "fontFamily">The SVG font-family name; bitmap output uses the built-in glyph shapes.</param>
    /// <param name = "fontSize">The font size in pixels, which must be positive.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public void SetStyle(ChartColor color, string fontFamily, int fontSize)
    {
        if (color == null)
        {
            throw new System.ArgumentNullException("color");
        }

        ValidateText(fontFamily, "fontFamily");
        if (fontSize <= 0)
        {
            throw new System.ArgumentOutOfRangeException("fontSize");
        }

        Color = color.Copy();
        FontFamily = fontFamily;
        FontSize = fontSize;
    }

    /// <summary>
    /// Sets the spacing configuration.
    /// </summary>
    /// <param name = "spacing">The spacing.</param>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public void SetSpacing(double spacing)
    {
        ChartLinearScale.ValidateFinite(spacing, "spacing");
        if (spacing < 0.0)
        {
            throw new System.ArgumentOutOfRangeException("spacing");
        }

        Spacing = spacing;
    }

    /// <summary>Measures this block's text using the deterministic built-in five-by-seven bitmap font.</summary>
    /// <param name = "scale">The positive integer glyph scale.</param>
    /// <returns>The measured dimensions and line metrics.</returns>
    public ChartTextMeasurement MeasureBuiltIn(int scale)
    {
        return ChartBitmapFont.FiveBySeven().MeasureText(WrappedBuiltInText(scale), scale);
    }

    /// <summary>Enables deterministic word wrapping at the supplied pixel width.</summary>
    /// <param name = "maximumWidth">The positive maximum line width in built-in-font pixels.</param>
    public void SetWrapping(int maximumWidth)
    {
        if (maximumWidth <= 0)
        {
            throw new System.ArgumentOutOfRangeException("maximumWidth");
        }

        MaximumWidth = maximumWidth;
    }

    /// <summary>Disables automatic wrapping while preserving explicit newlines.</summary>
    public void ClearWrapping()
    {
        MaximumWidth = 0;
    }

    /// <summary>Sets horizontal and vertical anchor alignment.</summary>
    /// <param name = "alignment">The horizontal alignment.</param>
    /// <param name = "verticalAlignment">The vertical alignment.</param>
    public void SetAlignment(ChartTextAlignment alignment, ChartVerticalTextAlignment verticalAlignment)
    {
        ChartTextLayoutOperations.ValidateAlignment(alignment, verticalAlignment);
        Alignment = alignment;
        VerticalAlignment = verticalAlignment;
    }

    /// <summary>Sets clockwise rotation in degrees for rotation planning and output.</summary>
    /// <param name = "rotationDegrees">The finite rotation angle in degrees.</param>
    public void SetRotation(double rotationDegrees)
    {
        ChartLinearScale.ValidateFinite(rotationDegrees, "rotationDegrees");
        RotationDegrees = rotationDegrees;
    }

    /// <summary>Sets the rectangle that clips text output.</summary>
    /// <param name = "clipBounds">The positive clipping rectangle.</param>
    public void SetClip(ChartRect clipBounds)
    {
        ChartAxis.ValidatePlotArea(clipBounds);
        HasClip = true;
        ClipBounds = ChartPlot.CopyRect(clipBounds);
    }

    /// <summary>Clears text clipping.</summary>
    public void ClearClip()
    {
        HasClip = false;
    }

    /// <summary>Sets deterministic left-to-right or right-to-left text-element ordering.</summary>
    /// <param name = "direction">The requested ordering direction.</param>
    public void SetDirection(ChartTextDirection direction)
    {
        ChartTextLayoutOperations.ValidateDirection(direction);
        Direction = direction;
    }

    /// <summary>Adds a copied styled span and appends its text to this block.</summary>
    /// <param name = "span">The rich-text span.</param>
    /// <returns>The copied stored span.</returns>
    public ChartTextSpan AddRichSpan(ChartTextSpan span)
    {
        if (span == null)
        {
            throw new System.ArgumentNullException("span");
        }

        if (RichSpanCount == 0 && Text.Length > 0)
        {
            EnsureRichSpanCapacity(2);
            ChartRenderStyle baseStyle = ChartRenderStyle.CreateFill(Color);
            baseStyle.SetFont(FontFamily, FontSize);
            RichSpans[0] = new ChartTextSpan(Text, baseStyle);
            RichSpanCount = 1;
        }

        EnsureRichSpanCapacity(RichSpanCount + 1);
        RichSpans[RichSpanCount] = span.Copy();
        RichSpanCount = RichSpanCount + 1;
        Text = Text + span.Text;
        Visible = Text.Length > 0;
        return RichSpans[RichSpanCount - 1];
    }

    /// <summary>Clears all styled spans and their composed text.</summary>
    public void ClearRichSpans()
    {
        RichSpanCount = 0;
        Text = "";
        Visible = false;
    }

    /// <summary>Returns built-in-font text after direction ordering and one-glyph fallback substitution.</summary>
    /// <returns>The prepared deterministic text.</returns>
    public string PreparedBuiltInText()
    {
        return ChartTextLayoutOperations.PrepareBuiltInText(Text, Direction);
    }

    /// <summary>Returns prepared built-in-font text with configured wrapping applied.</summary>
    /// <param name = "scale">The positive integer glyph scale.</param>
    /// <returns>The prepared and wrapped deterministic text.</returns>
    public string WrappedBuiltInText(int scale)
    {
        string prepared = PreparedBuiltInText();
        if (MaximumWidth > 0)
        {
            return ChartTextLayoutOperations.WrapBuiltInText(prepared, MaximumWidth, scale);
        }

        ChartBitmapFont.ValidateScale(scale);
        return prepared;
    }

    /// <summary>Creates a deterministic positioned built-in-font layout.</summary>
    /// <param name = "anchorX">The horizontal anchor coordinate.</param>
    /// <param name = "anchorY">The vertical anchor coordinate.</param>
    /// <param name = "scale">The positive integer glyph scale.</param>
    /// <returns>The positioned text layout, including configured clipping metadata.</returns>
    public ChartTextLayout LayoutBuiltIn(int anchorX, int anchorY, int scale)
    {
        ChartTextAlignment resolvedAlignment = ChartTextLayoutOperations.ResolveAlignment(Alignment, Direction);
        ChartTextLayout layout = ChartBitmapFont.FiveBySeven().CreateTextLayout(WrappedBuiltInText(scale), anchorX, anchorY, scale, resolvedAlignment, VerticalAlignment);
        if (HasClip)
        {
            return ChartTextLayoutOperations.AttachClip(layout, ClipBounds);
        }

        return layout;
    }

    /// <summary>Plans configured rotation around a supplied pivot.</summary>
    /// <param name = "layout">The text layout to rotate.</param>
    /// <param name = "pivotX">The horizontal rotation pivot.</param>
    /// <param name = "pivotY">The vertical rotation pivot.</param>
    /// <returns>The rotated bounds and corners.</returns>
    public ChartTextRotationPlan PlanBuiltInRotation(ChartTextLayout layout, double pivotX, double pivotY)
    {
        ChartLinearScale.ValidateFinite(pivotX, "pivotX");
        ChartLinearScale.ValidateFinite(pivotY, "pivotY");
        double radians = RotationDegrees * System.Math.PI / 180.0;
        return ChartBitmapFont.FiveBySeven().PlanRotation(layout, radians, pivotX, pivotY);
    }

    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartTextBlock Copy()
    {
        ChartTextBlock text = new ChartTextBlock();
        text.Text = Text;
        text.Visible = Visible;
        text.Color = Color.Copy();
        text.FontFamily = FontFamily;
        text.FontSize = FontSize;
        text.Spacing = Spacing;
        text.MaximumWidth = MaximumWidth;
        text.Alignment = Alignment;
        text.VerticalAlignment = VerticalAlignment;
        text.RotationDegrees = RotationDegrees;
        text.HasClip = HasClip;
        text.ClipBounds = ChartPlot.CopyRect(ClipBounds);
        text.Direction = Direction;
        text.EnsureRichSpanCapacity(RichSpanCount);
        int index = 0;
        while (index < RichSpanCount)
        {
            text.RichSpans[index] = RichSpans[index].Copy();
            index = index + 1;
        }

        text.RichSpanCount = RichSpanCount;
        return text;
    }

    internal void EnsureRichSpanCapacity(int capacity)
    {
        if (capacity <= RichSpans.Length)
        {
            return;
        }

        int newCapacity = ChartArrayUtilities.GrowthCapacity(RichSpans.Length, capacity);
        ChartTextSpan[] spans = new ChartTextSpan[newCapacity];
        System.Array.Copy(RichSpans, spans, RichSpanCount);
        RichSpans = spans;
    }

    internal static void ValidateText(string text, string name)
    {
        if (text == null)
        {
            throw new System.ArgumentNullException(name);
        }
    }

    /// <summary>Returns the current text.</summary>
    /// <returns>The current field value.</returns>
    public string GetText()
    {
        return Text;
    }

    /// <summary>Returns the current visible.</summary>
    /// <returns>The current field value.</returns>
    public bool GetVisible()
    {
        return Visible;
    }

    /// <summary>Replaces the current visible.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetVisible(bool value)
    {
        Visible = value;
    }

    /// <summary>Returns the current color.</summary>
    /// <returns>The current field value.</returns>
    public ChartColor GetColor()
    {
        return Color;
    }

    /// <summary>Replaces the current color.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetColor(ChartColor value)
    {
        Color = value;
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

    /// <summary>Returns the current spacing.</summary>
    /// <returns>The current field value.</returns>
    public double GetSpacing()
    {
        return Spacing;
    }

    /// <summary>Returns the wrapping width, or zero when wrapping is disabled.</summary>
    /// <returns>The maximum width.</returns>
    public int GetMaximumWidth()
    {
        return MaximumWidth;
    }

    /// <summary>Returns the horizontal alignment.</summary>
    /// <returns>The configured alignment.</returns>
    public ChartTextAlignment GetAlignment()
    {
        return Alignment;
    }

    /// <summary>Returns the vertical alignment.</summary>
    /// <returns>The configured vertical alignment.</returns>
    public ChartVerticalTextAlignment GetVerticalAlignment()
    {
        return VerticalAlignment;
    }

    /// <summary>Returns clockwise rotation in degrees.</summary>
    /// <returns>The configured rotation angle.</returns>
    public double GetRotation()
    {
        return RotationDegrees;
    }

    /// <summary>Returns whether clipping is enabled.</summary>
    /// <returns>The clipping state.</returns>
    public bool GetHasClip()
    {
        return HasClip;
    }

    /// <summary>Returns an independent clipping rectangle.</summary>
    /// <returns>The configured clip.</returns>
    public ChartRect GetClipBounds()
    {
        return ChartPlot.CopyRect(ClipBounds);
    }

    /// <summary>Returns the text-element ordering direction.</summary>
    /// <returns>The configured direction.</returns>
    public ChartTextDirection GetDirection()
    {
        return Direction;
    }

    /// <summary>Returns the number of stored rich-text spans.</summary>
    /// <returns>The span count.</returns>
    public int GetRichSpanCount()
    {
        return RichSpanCount;
    }

    /// <summary>Returns one stored rich-text span.</summary>
    /// <param name = "index">The zero-based span index.</param>
    /// <returns>The requested stored span.</returns>
    public ChartTextSpan RichSpanAt(int index)
    {
        if (index < 0 || index >= RichSpanCount)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return RichSpans[index];
    }
}
