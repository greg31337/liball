namespace chart;

/// <summary>Retains a live series-data binding for a cartesian renderer.</summary>
internal class ChartStreamingSeriesBinding
{
    internal string Id;
    internal ChartSeriesData Data;
    internal int Kind;
    internal ChartMarkerShape Shape;
    internal double MarkerSize;
    internal ChartRenderStyle Style;
    internal int Version;
    internal int SeriesIndex;

    internal ChartStreamingSeriesBinding(string id, ChartSeriesData data, int kind, ChartMarkerShape shape, double markerSize, ChartRenderStyle style, int seriesIndex)
    {
        Id = id;
        Data = data;
        Kind = kind;
        Shape = shape;
        MarkerSize = markerSize;
        Style = style.Copy();
        Version = data.Version;
        SeriesIndex = seriesIndex;
    }

    internal ChartCartesianSeries CreateSeries()
    {
        if (Kind == 0)
        {
            return Data.CreateLineSeries(Id, Style);
        }

        return Data.CreateScatterSeries(Id, Shape, MarkerSize, Style);
    }
}
