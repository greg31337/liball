namespace chart;
/// <summary>Stores one table, mark, encoding, style, and stack mode in a declarative layered view.</summary>
public class ChartDeclarativeLayer
{
    /// <summary>The stable identifier prefix for series created by this layer.</summary>
    public string Id;
    /// <summary>The mark constructed for this layer.</summary>
    public ChartDeclarativeMark Mark;
    /// <summary>The source table.</summary>
    public ChartDataTable Table;
    /// <summary>The visual channel encoding.</summary>
    public ChartVisualEncoding Encoding;
    /// <summary>The default retained render style.</summary>
    public ChartRenderStyle Style;
    /// <summary>The stack mode applied across detail groups.</summary>
    public ChartStackMode StackMode;

    /// <summary>Initializes an unstacked declarative layer.</summary>
    /// <param name = "id">The stable identifier prefix.</param>
    /// <param name = "mark">The mark constructed for this layer.</param>
    /// <param name = "table">The source data table.</param>
    /// <param name = "encoding">The visual channel encoding.</param>
    /// <param name = "style">The default retained render style.</param>
    public ChartDeclarativeLayer(string id, ChartDeclarativeMark mark, ChartDataTable table, ChartVisualEncoding encoding, ChartRenderStyle style)
    {
        Id = id;
        Mark = mark;
        Table = table;
        Encoding = encoding;
        Style = style;
        StackMode = ChartStackMode.None;
    }
}
