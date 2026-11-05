namespace chart;
/// <summary>Defines one styled span in rich text.</summary>
public interface IChartTextSpan
{
    /// <summary>Returns the span text.</summary>
    /// <returns>The span text.</returns>
    string GetText();
    /// <summary>Sets the span text.</summary>
    /// <param name = "text">The replacement text.</param>
    void SetText(string text);
    /// <summary>Returns an independent rendering style.</summary>
    /// <returns>A copied rendering style.</returns>
    ChartRenderStyle GetStyle();
    /// <summary>Sets the span rendering style.</summary>
    /// <param name = "style">The replacement style.</param>
    void SetStyle(ChartRenderStyle style);
    /// <summary>Creates an independent copy.</summary>
    /// <returns>A copied rich-text span.</returns>
    ChartTextSpan Copy();
}
