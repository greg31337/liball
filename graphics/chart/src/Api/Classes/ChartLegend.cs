namespace chart;
/// <summary>
/// Represents a legend used by the chart model or rendering pipeline.
/// </summary>
public class ChartLegend : IChartLegend
{
    internal string Id;
    internal string Title;
    internal ChartLegendEntry[] Entries;
    internal int EntryCount;
    internal double Padding;
    internal double EntrySpacing;
    internal double SymbolSize;
    internal double GradientWidth;
    internal double GradientHeight;
    internal string FontFamily;
    internal int FontSize;
    internal ChartColor TextColor;
    internal ChartColor BackgroundColor;
    internal bool HasBackground;
    internal ChartColor BorderColor;
    internal double BorderWidth;
    internal bool ShowBorder;
    /// <summary>
    /// Initializes a legend from the supplied id.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    public ChartLegend(string id)
    {
        ChartPlot.ValidateId(id);
        Id = id;
        Title = "";
        Entries = new ChartLegendEntry[4];
        EntryCount = 0;
        Padding = 8.0;
        EntrySpacing = 6.0;
        SymbolSize = 12.0;
        GradientWidth = 48.0;
        GradientHeight = 10.0;
        FontFamily = "Arial";
        FontSize = 12;
        TextColor = ChartColor.Black();
        BackgroundColor = ChartColor.Transparent();
        HasBackground = false;
        BorderColor = ChartColor.Black();
        BorderWidth = 1.0;
        ShowBorder = false;
    }

    /// <summary>
    /// Sets the title configuration.
    /// </summary>
    /// <param name = "title">The title.</param>
    public void SetTitle(string title)
    {
        ChartTextBlock.ValidateText(title, "title");
        Title = title;
    }

    /// <summary>
    /// Sets the spacing configuration.
    /// </summary>
    /// <param name = "padding">The padding.</param>
    /// <param name = "entrySpacing">The entry spacing.</param>
    public void SetSpacing(double padding, double entrySpacing)
    {
        ChartStyle.ValidateNonNegative(padding, "padding");
        ChartStyle.ValidateNonNegative(entrySpacing, "entrySpacing");
        Padding = padding;
        EntrySpacing = entrySpacing;
    }

    /// <summary>
    /// Sets the symbol size configuration.
    /// </summary>
    /// <param name = "symbolSize">The symbol size.</param>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public void SetSymbolSize(double symbolSize)
    {
        ChartStyle.ValidateNonNegative(symbolSize, "symbolSize");
        if (symbolSize == 0.0)
        {
            throw new System.ArgumentOutOfRangeException("symbolSize");
        }

        SymbolSize = symbolSize;
    }

    /// <summary>
    /// Sets the gradient size configuration.
    /// </summary>
    /// <param name = "width">The width in pixels or chart units, which must be positive.</param>
    /// <param name = "height">The height in pixels or chart units, which must be positive.</param>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public void SetGradientSize(double width, double height)
    {
        ChartStyle.ValidateNonNegative(width, "width");
        ChartStyle.ValidateNonNegative(height, "height");
        if (width == 0.0 || height == 0.0)
        {
            throw new System.ArgumentOutOfRangeException("width");
        }

        GradientWidth = width;
        GradientHeight = height;
    }

    /// <summary>
    /// Sets the text style configuration.
    /// </summary>
    /// <param name = "textColor">The text color.</param>
    /// <param name = "fontFamily">The SVG font-family name; bitmap output uses the built-in glyph shapes.</param>
    /// <param name = "fontSize">The font size in pixels, which must be positive.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public void SetTextStyle(ChartColor textColor, string fontFamily, int fontSize)
    {
        if (textColor == null)
        {
            throw new System.ArgumentNullException("textColor");
        }

        if (fontFamily == null)
        {
            throw new System.ArgumentNullException("fontFamily");
        }

        if (fontSize <= 0)
        {
            throw new System.ArgumentOutOfRangeException("fontSize");
        }

        TextColor = textColor.Copy();
        FontFamily = fontFamily;
        FontSize = fontSize;
    }

    /// <summary>
    /// Sets the background configuration.
    /// </summary>
    /// <param name = "backgroundColor">The background color.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public void SetBackground(ChartColor backgroundColor)
    {
        if (backgroundColor == null)
        {
            throw new System.ArgumentNullException("backgroundColor");
        }

        BackgroundColor = backgroundColor.Copy();
        HasBackground = !backgroundColor.IsTransparent();
    }

    /// <summary>
    /// Sets the border configuration.
    /// </summary>
    /// <param name = "borderColor">The border color.</param>
    /// <param name = "borderWidth">The border width.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public void SetBorder(ChartColor borderColor, double borderWidth)
    {
        if (borderColor == null)
        {
            throw new System.ArgumentNullException("borderColor");
        }

        ChartStyle.ValidateNonNegative(borderWidth, "borderWidth");
        BorderColor = borderColor.Copy();
        BorderWidth = borderWidth;
        ShowBorder = borderWidth > 0.0;
    }

    /// <summary>
    /// Adds the supplied entry to this object.
    /// </summary>
    /// <param name = "entry">The entry.</param>
    /// <returns>The resulting legend entry.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartLegendEntry AddEntry(ChartLegendEntry entry)
    {
        if (entry == null)
        {
            throw new System.ArgumentNullException("entry");
        }

        EnsureCapacity(EntryCount + 1);
        Entries[EntryCount] = entry.Copy();
        EntryCount = EntryCount + 1;
        return Entries[EntryCount - 1];
    }

    /// <summary>
    /// Adds the supplied symbol entry to this object.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "label">The label.</param>
    /// <param name = "shape">The shape.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The resulting legend entry.</returns>
    public ChartLegendEntry AddSymbolEntry(string id, string label, ChartMarkerShape shape, ChartStyle style)
    {
        return AddEntry(ChartLegendEntry.CreateSymbol(id, label, shape, style));
    }

    /// <summary>
    /// Adds the supplied gradient entry to this object.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "label">The label.</param>
    /// <param name = "colors">The colors.</param>
    /// <param name = "offsets">The offsets.</param>
    /// <returns>The resulting legend entry.</returns>
    public ChartLegendEntry AddGradientEntry(string id, string label, ChartColor[] colors, double[] offsets)
    {
        return AddEntry(ChartLegendEntry.CreateGradient(id, label, colors, offsets));
    }

    /// <summary>
    /// Adds the supplied custom entry to this object.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "label">The label.</param>
    /// <param name = "customMarkup">The custom markup.</param>
    /// <returns>The resulting legend entry.</returns>
    public ChartLegendEntry AddCustomEntry(string id, string label, string customMarkup)
    {
        return AddEntry(ChartLegendEntry.CreateCustom(id, label, customMarkup));
    }

    /// <summary>
    /// Returns the requested entry.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting legend entry.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartLegendEntry EntryAt(int index)
    {
        if (index < 0 || index >= EntryCount)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return Entries[index];
    }

    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    public ChartLegend Copy()
    {
        ChartLegend legend = new ChartLegend(Id);
        legend.Title = Title;
        legend.Padding = Padding;
        legend.EntrySpacing = EntrySpacing;
        legend.SymbolSize = SymbolSize;
        legend.GradientWidth = GradientWidth;
        legend.GradientHeight = GradientHeight;
        legend.FontFamily = FontFamily;
        legend.FontSize = FontSize;
        legend.TextColor = TextColor.Copy();
        legend.BackgroundColor = BackgroundColor.Copy();
        legend.HasBackground = HasBackground;
        legend.BorderColor = BorderColor.Copy();
        legend.BorderWidth = BorderWidth;
        legend.ShowBorder = ShowBorder;
        legend.EnsureCapacity(EntryCount);
        int index = 0;
        while (index < EntryCount)
        {
            legend.Entries[index] = Entries[index].Copy();
            index = index + 1;
        }

        legend.EntryCount = EntryCount;
        return legend;
    }

    internal void EnsureCapacity(int capacity)
    {
        if (capacity <= Entries.Length)
        {
            return;
        }

        int newCapacity = ChartArrayUtilities.GrowthCapacity(Entries.Length, capacity);
        ChartLegendEntry[] entries = new ChartLegendEntry[newCapacity];
        System.Array.Copy(Entries, entries, EntryCount);
        Entries = entries;
    }

    /// <summary>Returns the current id.</summary>
    /// <returns>The current field value.</returns>
    public string GetId()
    {
        return Id;
    }

    /// <summary>Replaces the current id.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetId(string value)
    {
        Id = value;
    }

    /// <summary>Returns the current title.</summary>
    /// <returns>The current field value.</returns>
    public string GetTitle()
    {
        return Title;
    }

    /// <summary>Returns the current entries.</summary>
    /// <returns>The current field value.</returns>
    public ChartLegendEntry[] GetEntries()
    {
        return Entries;
    }

    /// <summary>Replaces the current entries.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetEntries(ChartLegendEntry[] value)
    {
        Entries = value;
    }

    /// <summary>Returns the current entry count.</summary>
    /// <returns>The current field value.</returns>
    public int GetEntryCount()
    {
        return EntryCount;
    }

    /// <summary>Replaces the current entry count.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetEntryCount(int value)
    {
        EntryCount = value;
    }

    /// <summary>Returns the current padding.</summary>
    /// <returns>The current field value.</returns>
    public double GetPadding()
    {
        return Padding;
    }

    /// <summary>Replaces the current padding.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetPadding(double value)
    {
        Padding = value;
    }

    /// <summary>Returns the current entry spacing.</summary>
    /// <returns>The current field value.</returns>
    public double GetEntrySpacing()
    {
        return EntrySpacing;
    }

    /// <summary>Replaces the current entry spacing.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetEntrySpacing(double value)
    {
        EntrySpacing = value;
    }

    /// <summary>Returns the current symbol size.</summary>
    /// <returns>The current field value.</returns>
    public double GetSymbolSize()
    {
        return SymbolSize;
    }

    /// <summary>Returns the current gradient width.</summary>
    /// <returns>The current field value.</returns>
    public double GetGradientWidth()
    {
        return GradientWidth;
    }

    /// <summary>Replaces the current gradient width.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetGradientWidth(double value)
    {
        GradientWidth = value;
    }

    /// <summary>Returns the current gradient height.</summary>
    /// <returns>The current field value.</returns>
    public double GetGradientHeight()
    {
        return GradientHeight;
    }

    /// <summary>Replaces the current gradient height.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetGradientHeight(double value)
    {
        GradientHeight = value;
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

    /// <summary>Returns the current text color.</summary>
    /// <returns>The current field value.</returns>
    public ChartColor GetTextColor()
    {
        return TextColor;
    }

    /// <summary>Replaces the current text color.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetTextColor(ChartColor value)
    {
        TextColor = value;
    }

    /// <summary>Returns the current background color.</summary>
    /// <returns>The current field value.</returns>
    public ChartColor GetBackgroundColor()
    {
        return BackgroundColor;
    }

    /// <summary>Replaces the current background color.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetBackgroundColor(ChartColor value)
    {
        BackgroundColor = value;
    }

    /// <summary>Returns the current has background.</summary>
    /// <returns>The current field value.</returns>
    public bool GetHasBackground()
    {
        return HasBackground;
    }

    /// <summary>Replaces the current has background.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetHasBackground(bool value)
    {
        HasBackground = value;
    }

    /// <summary>Returns the current border color.</summary>
    /// <returns>The current field value.</returns>
    public ChartColor GetBorderColor()
    {
        return BorderColor;
    }

    /// <summary>Replaces the current border color.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetBorderColor(ChartColor value)
    {
        BorderColor = value;
    }

    /// <summary>Returns the current border width.</summary>
    /// <returns>The current field value.</returns>
    public double GetBorderWidth()
    {
        return BorderWidth;
    }

    /// <summary>Replaces the current border width.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetBorderWidth(double value)
    {
        BorderWidth = value;
    }

    /// <summary>Returns the current show border.</summary>
    /// <returns>The current field value.</returns>
    public bool GetShowBorder()
    {
        return ShowBorder;
    }

    /// <summary>Replaces the current show border.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetShowBorder(bool value)
    {
        ShowBorder = value;
    }
}
