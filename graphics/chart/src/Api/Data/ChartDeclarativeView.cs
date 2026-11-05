namespace chart;
/// <summary>Contains flattened layered series, facet coordinates, and deduplicated shared guide values.</summary>
public class ChartDeclarativeView
{
    /// <summary>The constructed series in layer, facet, and detail order.</summary>
    public ChartCartesianSeries[] Series;
    /// <summary>The zero-based source-layer index for each series.</summary>
    public int[] LayerIndices;
    /// <summary>The row-facet key for each series, or an empty string.</summary>
    public string[] RowFacetKeys;
    /// <summary>The column-facet key for each series, or an empty string.</summary>
    public string[] ColumnFacetKeys;
    /// <summary>The wrap-facet key for each series, or an empty string.</summary>
    public string[] WrapFacetKeys;
    /// <summary>The deduplicated labels for a shared categorical guide.</summary>
    public string[] GuideLabels;
    /// <summary>The categorical colors aligned with the shared guide labels.</summary>
    public ChartColor[] GuideColors;

    /// <summary>Initializes an empty declarative view.</summary>
    public ChartDeclarativeView()
    {
        Series = new ChartCartesianSeries[0];
        LayerIndices = new int[0];
        RowFacetKeys = new string[0];
        ColumnFacetKeys = new string[0];
        WrapFacetKeys = new string[0];
        GuideLabels = new string[0];
        GuideColors = new ChartColor[0];
    }
}
