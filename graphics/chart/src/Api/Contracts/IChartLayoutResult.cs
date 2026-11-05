namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartLayoutResult"/>.</summary>
public interface IChartLayoutResult
{
    /// <summary>
    /// Adds the supplied item to this object.
    /// </summary>
    /// <param name = "kind">The kind.</param>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "bounds">The rectangle that constrains the generated layout or drawing.</param>
    /// <param name = "side">The side.</param>
    /// <returns>The resulting layout item.</returns>
    ChartLayoutItem AddItem(ChartLayoutItemKind kind, string id, ChartRect bounds, ChartSide side);
    /// <summary>
    /// Returns the item at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting layout item.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    ChartLayoutItem ItemAt(int index);
    /// <summary>
    /// Returns the zero-based item index.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <returns>The resulting int.</returns>
    int ItemIndex(string id);
    /// <summary>
    /// Applies the item by id operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <returns>The resulting layout item.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    ChartLayoutItem ItemById(string id);
    /// <summary>
    /// Adds the supplied diagnostic overlay to this object.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    void AddDiagnosticOverlay(IChartRenderTarget scene);
    /// <summary>
    /// Adds the supplied diagnostic overlay to this object.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name = "idPrefix">The prefix used to create stable identifiers for generated items.</param>
    void AddDiagnosticOverlayWithPrefix(IChartRenderTarget scene, string idPrefix);
    /// <summary>
    /// Creates a deterministic text trace of the retained drawing commands.
    /// </summary>
    /// <returns>The converted string.</returns>
    string ToTraceString();
    /// <summary>Returns the current figure rect.</summary>
    /// <returns>The current field value.</returns>
    ChartRect GetFigureRect();
    /// <summary>Replaces the current figure rect.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetFigureRect(ChartRect value);
    /// <summary>Returns the current content rect.</summary>
    /// <returns>The current field value.</returns>
    ChartRect GetContentRect();
    /// <summary>Replaces the current content rect.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetContentRect(ChartRect value);
    /// <summary>Returns the current plot area.</summary>
    /// <returns>The current field value.</returns>
    ChartRect GetPlotArea();
    /// <summary>Replaces the current plot area.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetPlotArea(ChartRect value);
    /// <summary>Returns the current items.</summary>
    /// <returns>The current field value.</returns>
    ChartLayoutItem[] GetItems();
    /// <summary>Replaces the current items.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetItems(ChartLayoutItem[] value);
    /// <summary>Returns the current item count.</summary>
    /// <returns>The current field value.</returns>
    int GetItemCount();
    /// <summary>Replaces the current item count.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetItemCount(int value);
}
