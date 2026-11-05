namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartLegend"/>.</summary>
public interface IChartLegend
{
    /// <summary>
    /// Sets the title configuration.
    /// </summary>
    /// <param name = "title">The title.</param>
    void SetTitle(string title);
    /// <summary>
    /// Sets the spacing configuration.
    /// </summary>
    /// <param name = "padding">The padding.</param>
    /// <param name = "entrySpacing">The entry spacing.</param>
    void SetSpacing(double padding, double entrySpacing);
    /// <summary>
    /// Sets the symbol size configuration.
    /// </summary>
    /// <param name = "symbolSize">The symbol size.</param>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    void SetSymbolSize(double symbolSize);
    /// <summary>
    /// Sets the gradient size configuration.
    /// </summary>
    /// <param name = "width">The width in pixels or chart units, which must be positive.</param>
    /// <param name = "height">The height in pixels or chart units, which must be positive.</param>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    void SetGradientSize(double width, double height);
    /// <summary>
    /// Sets the text style configuration.
    /// </summary>
    /// <param name = "textColor">The text color.</param>
    /// <param name = "fontFamily">The SVG font-family name; bitmap output uses the built-in glyph shapes.</param>
    /// <param name = "fontSize">The font size in pixels, which must be positive.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    void SetTextStyle(ChartColor textColor, string fontFamily, int fontSize);
    /// <summary>
    /// Sets the background configuration.
    /// </summary>
    /// <param name = "backgroundColor">The background color.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    void SetBackground(ChartColor backgroundColor);
    /// <summary>
    /// Sets the border configuration.
    /// </summary>
    /// <param name = "borderColor">The border color.</param>
    /// <param name = "borderWidth">The border width.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    void SetBorder(ChartColor borderColor, double borderWidth);
    /// <summary>
    /// Adds the supplied entry to this object.
    /// </summary>
    /// <param name = "entry">The entry.</param>
    /// <returns>The resulting legend entry.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    ChartLegendEntry AddEntry(ChartLegendEntry entry);
    /// <summary>
    /// Adds the supplied symbol entry to this object.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "label">The label.</param>
    /// <param name = "shape">The shape.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The resulting legend entry.</returns>
    ChartLegendEntry AddSymbolEntry(string id, string label, ChartMarkerShape shape, ChartStyle style);
    /// <summary>
    /// Adds the supplied gradient entry to this object.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "label">The label.</param>
    /// <param name = "colors">The colors.</param>
    /// <param name = "offsets">The offsets.</param>
    /// <returns>The resulting legend entry.</returns>
    ChartLegendEntry AddGradientEntry(string id, string label, ChartColor[] colors, double[] offsets);
    /// <summary>
    /// Adds the supplied custom entry to this object.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "label">The label.</param>
    /// <param name = "customMarkup">The custom markup.</param>
    /// <returns>The resulting legend entry.</returns>
    ChartLegendEntry AddCustomEntry(string id, string label, string customMarkup);
    /// <summary>
    /// Returns the requested entry.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting legend entry.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    ChartLegendEntry EntryAt(int index);
    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartLegend Copy();
    /// <summary>Returns the current id.</summary>
    /// <returns>The current field value.</returns>
    string GetId();
    /// <summary>Replaces the current id.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetId(string value);
    /// <summary>Returns the current title.</summary>
    /// <returns>The current field value.</returns>
    string GetTitle();
    /// <summary>Returns the current entries.</summary>
    /// <returns>The current field value.</returns>
    ChartLegendEntry[] GetEntries();
    /// <summary>Replaces the current entries.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetEntries(ChartLegendEntry[] value);
    /// <summary>Returns the current entry count.</summary>
    /// <returns>The current field value.</returns>
    int GetEntryCount();
    /// <summary>Replaces the current entry count.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetEntryCount(int value);
    /// <summary>Returns the current padding.</summary>
    /// <returns>The current field value.</returns>
    double GetPadding();
    /// <summary>Replaces the current padding.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetPadding(double value);
    /// <summary>Returns the current entry spacing.</summary>
    /// <returns>The current field value.</returns>
    double GetEntrySpacing();
    /// <summary>Replaces the current entry spacing.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetEntrySpacing(double value);
    /// <summary>Returns the current symbol size.</summary>
    /// <returns>The current field value.</returns>
    double GetSymbolSize();
    /// <summary>Returns the current gradient width.</summary>
    /// <returns>The current field value.</returns>
    double GetGradientWidth();
    /// <summary>Replaces the current gradient width.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetGradientWidth(double value);
    /// <summary>Returns the current gradient height.</summary>
    /// <returns>The current field value.</returns>
    double GetGradientHeight();
    /// <summary>Replaces the current gradient height.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetGradientHeight(double value);
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
    /// <summary>Returns the current text color.</summary>
    /// <returns>The current field value.</returns>
    ChartColor GetTextColor();
    /// <summary>Replaces the current text color.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetTextColor(ChartColor value);
    /// <summary>Returns the current background color.</summary>
    /// <returns>The current field value.</returns>
    ChartColor GetBackgroundColor();
    /// <summary>Replaces the current background color.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetBackgroundColor(ChartColor value);
    /// <summary>Returns the current has background.</summary>
    /// <returns>The current field value.</returns>
    bool GetHasBackground();
    /// <summary>Replaces the current has background.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetHasBackground(bool value);
    /// <summary>Returns the current border color.</summary>
    /// <returns>The current field value.</returns>
    ChartColor GetBorderColor();
    /// <summary>Replaces the current border color.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetBorderColor(ChartColor value);
    /// <summary>Returns the current border width.</summary>
    /// <returns>The current field value.</returns>
    double GetBorderWidth();
    /// <summary>Replaces the current border width.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetBorderWidth(double value);
    /// <summary>Returns the current show border.</summary>
    /// <returns>The current field value.</returns>
    bool GetShowBorder();
    /// <summary>Replaces the current show border.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetShowBorder(bool value);
}
