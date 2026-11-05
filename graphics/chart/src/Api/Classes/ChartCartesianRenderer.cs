namespace chart;
/// <summary>
/// Represents a cartesian renderer used by the chart model or rendering pipeline.
/// </summary>
public class ChartCartesianRenderer : IChartRenderer, IChartCartesianRenderer
{
    internal ChartCartesianTransform Transform;
    internal ChartCartesianSeries[] Series;
    internal int SeriesCount;
    internal ChartStreamingSeriesBinding[] StreamingBindings;
    internal int StreamingBindingCount;
    internal bool ClipToPlotArea;
    /// <summary>
    /// Initializes a cartesian renderer from the supplied transform.
    /// </summary>
    /// <param name = "transform">The transform.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartCartesianRenderer(ChartCartesianTransform transform)
    {
        if (transform == null)
        {
            throw new System.ArgumentNullException("transform");
        }

        Transform = new ChartCartesianTransform(transform.PlotArea, transform.XDomain, transform.YDomain);
        Series = new ChartCartesianSeries[4];
        SeriesCount = 0;
        StreamingBindings = new ChartStreamingSeriesBinding[4];
        StreamingBindingCount = 0;
        ClipToPlotArea = true;
    }

    /// <summary>
    /// Adds the supplied series to this object.
    /// </summary>
    /// <param name = "series">The chart series to render, validate, or combine.</param>
    /// <returns>The resulting cartesian series.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public ChartCartesianSeries AddSeries(ChartCartesianSeries series)
    {
        if (series == null)
        {
            throw new System.ArgumentNullException("series");
        }

        EnsureCapacity(SeriesCount + 1);
        Series[SeriesCount] = series.Copy();
        Series[SeriesCount].Sequence = SeriesCount;
        SeriesCount = SeriesCount + 1;
        return Series[SeriesCount - 1];
    }

    /// <summary>Adds a line series that refreshes when its series data changes.</summary>
    /// <param name = "id">The stable series identifier.</param>
    /// <param name = "data">The live x and y series data.</param>
    /// <param name = "style">The rendering style.</param>
    /// <returns>The current retained line series.</returns>
    public ChartCartesianSeries AddStreamingLineSeries(string id, ChartSeriesData data, ChartRenderStyle style)
    {
        ValidateStreamingArguments(data, style);
        ChartStreamingSeriesBinding binding = new ChartStreamingSeriesBinding(id, data, 0, ChartMarkerShape.Circle, 1.0, style, SeriesCount);
        ChartCartesianSeries series = AddSeries(binding.CreateSeries());
        AddStreamingBinding(binding);
        return series;
    }

    /// <summary>Adds a scatter series that refreshes when its series data changes.</summary>
    /// <param name = "id">The stable series identifier.</param>
    /// <param name = "data">The live x and y series data.</param>
    /// <param name = "shape">The marker shape.</param>
    /// <param name = "markerSize">The rendered marker size.</param>
    /// <param name = "style">The rendering style.</param>
    /// <returns>The current retained scatter series.</returns>
    public ChartCartesianSeries AddStreamingScatterSeries(string id, ChartSeriesData data, ChartMarkerShape shape, double markerSize, ChartRenderStyle style)
    {
        ValidateStreamingArguments(data, style);
        ChartStreamingSeriesBinding binding = new ChartStreamingSeriesBinding(id, data, 1, shape, markerSize, style, SeriesCount);
        ChartCartesianSeries series = AddSeries(binding.CreateSeries());
        AddStreamingBinding(binding);
        return series;
    }

    /// <summary>
    /// Returns the requested series.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting cartesian series.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public ChartCartesianSeries SeriesAt(int index)
    {
        if (index < 0 || index >= SeriesCount)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return Series[index];
    }

    /// <summary>
    /// Renders the configured value using the supplied data and configuration.
    /// </summary>
    /// <param name = "width">The width in pixels or chart units, which must be positive.</param>
    /// <param name = "height">The height in pixels or chart units, which must be positive.</param>
    /// <returns>The resulting render scene.</returns>
    public ChartRenderScene Render(int width, int height)
    {
        ChartRenderScene scene = new ChartRenderScene(width, height);
        RenderToScene(scene);
        return scene;
    }

    /// <summary>
    /// Renders to scene using the supplied data and configuration.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public void RenderToScene(ChartRenderScene scene)
    {
        RenderTo(scene);
    }

    /// <summary>
    /// Renders the configured series into a caller-provided target.
    /// </summary>
    /// <param name = "target">The target that receives drawing operations.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public void RenderTo(IChartRenderTarget target)
    {
        if (target == null)
        {
            throw new System.ArgumentNullException("target");
        }

        RefreshStreamingSeries();
        ChartCartesianSeries[] ordered = OrderedSeries();
        int index = 0;
        while (index < ordered.Length)
        {
            RenderSeries(target, ordered[index]);
            index = index + 1;
        }
    }

    internal void RenderSeries(IChartRenderTarget scene, ChartCartesianSeries series)
    {
        if (scene == null)
        {
            throw new System.ArgumentNullException("scene");
        }

        if (series == null)
        {
            throw new System.ArgumentNullException("series");
        }

        int index = 0;
        while (index < series.CommandCount)
        {
            RenderCommand(scene, series, series.Commands[index]);
            index = index + 1;
        }
    }

    internal void RenderCommand(IChartRenderTarget scene, ChartCartesianSeries series, ChartRenderCommand command)
    {
        if (command.Kind == ChartRenderCommandKind.Line)
        {
            AddLineCommand(scene, series, command);
            return;
        }

        if (command.Kind == ChartRenderCommandKind.Polyline)
        {
            AddPolylineCommand(scene, series, command);
            return;
        }

        if (command.Kind == ChartRenderCommandKind.Polygon)
        {
            AddPolygonCommand(scene, series, command);
            return;
        }

        if (command.Kind == ChartRenderCommandKind.Marker)
        {
            AddMarkerCommand(scene, series, command);
            return;
        }

        if (command.Kind == ChartRenderCommandKind.Text)
        {
            AddTextCommand(scene, series, command);
        }
    }

    internal void AddLineCommand(IChartRenderTarget scene, ChartCartesianSeries series, ChartRenderCommand command)
    {
        ChartPoint start = Transform.MapPoint(command.X1, command.Y1);
        ChartPoint end = Transform.MapPoint(command.X2, command.Y2);
        if (ClipToPlotArea && series.ClipToPlotArea)
        {
            ChartCartesianLineClip clip = Transform.ClipPlotLine(start, end);
            if (!clip.Visible)
            {
                return;
            }

            start = clip.Start;
            end = clip.End;
        }

        scene.AddLine(command.Id, start.X, start.Y, end.X, end.Y, command.Style);
    }

    internal void AddPolylineCommand(IChartRenderTarget scene, ChartCartesianSeries series, ChartRenderCommand command)
    {
        ChartPoint[] points = TransformDataPoints(command.Points);
        if (!ClipToPlotArea || !series.ClipToPlotArea)
        {
            scene.AddPolyline(command.Id, points, command.Style);
            return;
        }

        int index = 0;
        while (index < points.Length - 1)
        {
            ChartCartesianLineClip clip = Transform.ClipPlotLine(points[index], points[index + 1]);
            if (clip.Visible)
            {
                scene.AddLine(command.Id + "-segment-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture), clip.Start.X, clip.Start.Y, clip.End.X, clip.End.Y, command.Style);
            }

            index = index + 1;
        }
    }

    internal void AddPolygonCommand(IChartRenderTarget scene, ChartCartesianSeries series, ChartRenderCommand command)
    {
        ChartPoint[] points = TransformDataPoints(command.Points);
        if (ClipToPlotArea && series.ClipToPlotArea)
        {
            points = Transform.ClipPlotPolygon(points);
        }

        if (points.Length >= 3)
        {
            scene.AddPolygon(command.Id, points, command.Style);
        }
    }

    internal void AddMarkerCommand(IChartRenderTarget scene, ChartCartesianSeries series, ChartRenderCommand command)
    {
        ChartPoint point = Transform.MapPoint(command.X1, command.Y1);
        if (ClipToPlotArea && series.ClipToPlotArea && !Transform.ContainsPlotPoint(point))
        {
            return;
        }

        if (command.MarkerShape == ChartMarkerShape.CustomPath)
        {
            scene.AddCustomMarker(command.Id, command.MarkerPath, point.X, point.Y, command.MarkerSize, command.Style);
            return;
        }

        scene.AddMarker(command.Id, command.MarkerShape, point.X, point.Y, command.MarkerSize, command.Style);
    }

    internal void AddTextCommand(IChartRenderTarget scene, ChartCartesianSeries series, ChartRenderCommand command)
    {
        ChartPoint point = Transform.MapPoint(command.X1, command.Y1);
        if (ClipToPlotArea && series.ClipToPlotArea && !Transform.ContainsPlotPoint(point))
        {
            return;
        }

        scene.AddText(command.Id, command.Text, point.X, point.Y, command.Style);
    }

    internal ChartPoint[] TransformDataPoints(ChartPoint[] points)
    {
        ChartRenderCommand.ValidatePoints(points, 1);
        ChartPoint[] transformed = new ChartPoint[points.Length];
        int index = 0;
        while (index < points.Length)
        {
            transformed[index] = Transform.MapPoint(points[index].X, points[index].Y);
            index = index + 1;
        }

        return transformed;
    }

    internal ChartCartesianSeries[] OrderedSeries()
    {
        ChartCartesianSeries[] ordered = new ChartCartesianSeries[SeriesCount];
        int index = 0;
        while (index < SeriesCount)
        {
            ordered[index] = Series[index];
            index = index + 1;
        }

        int outer = 0;
        while (outer < ordered.Length)
        {
            int selected = outer;
            int inner = outer + 1;
            while (inner < ordered.Length)
            {
                if (ComesBefore(ordered[inner], ordered[selected]))
                {
                    selected = inner;
                }

                inner = inner + 1;
            }

            if (selected != outer)
            {
                ChartCartesianSeries temp = ordered[outer];
                ordered[outer] = ordered[selected];
                ordered[selected] = temp;
            }

            outer = outer + 1;
        }

        return ordered;
    }

    internal static bool ComesBefore(ChartCartesianSeries left, ChartCartesianSeries right)
    {
        if (left.Order < right.Order)
        {
            return true;
        }

        if (left.Order > right.Order)
        {
            return false;
        }

        return left.Sequence < right.Sequence;
    }

    internal void EnsureCapacity(int capacity)
    {
        if (capacity <= Series.Length)
        {
            return;
        }

        int newCapacity = ChartArrayUtilities.GrowthCapacity(Series.Length, capacity);
        ChartCartesianSeries[] series = new ChartCartesianSeries[newCapacity];
        System.Array.Copy(Series, series, SeriesCount);
        Series = series;
    }

    internal void AddStreamingBinding(ChartStreamingSeriesBinding binding)
    {
        if (StreamingBindingCount == StreamingBindings.Length)
        {
            int capacity = ChartArrayUtilities.GrowthCapacity(StreamingBindings.Length, StreamingBindingCount + 1);
            ChartStreamingSeriesBinding[] bindings = new ChartStreamingSeriesBinding[capacity];
            System.Array.Copy(StreamingBindings, bindings, StreamingBindingCount);
            StreamingBindings = bindings;
        }

        StreamingBindings[StreamingBindingCount] = binding;
        StreamingBindingCount = StreamingBindingCount + 1;
    }

    internal void RefreshStreamingSeries()
    {
        int index = 0;
        while (index < StreamingBindingCount)
        {
            ChartStreamingSeriesBinding binding = StreamingBindings[index];
            if (binding.Version != binding.Data.Version)
            {
                ChartCartesianSeries previous = Series[binding.SeriesIndex];
                ChartCartesianSeries refreshed = binding.CreateSeries();
                refreshed.Order = previous.Order;
                refreshed.Sequence = previous.Sequence;
                refreshed.ClipToPlotArea = previous.ClipToPlotArea;
                Series[binding.SeriesIndex] = refreshed;
                binding.Version = binding.Data.Version;
            }

            index = index + 1;
        }
    }

    internal static void ValidateStreamingArguments(ChartSeriesData data, ChartRenderStyle style)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        if (style == null)
        {
            throw new System.ArgumentNullException("style");
        }
    }

    /// <summary>Returns the current transform.</summary>
    /// <returns>The current field value.</returns>
    public ChartCartesianTransform GetTransform()
    {
        return Transform;
    }

    /// <summary>Replaces the current transform.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetTransform(ChartCartesianTransform value)
    {
        Transform = value;
    }

    /// <summary>Returns the current series.</summary>
    /// <returns>The current field value.</returns>
    public ChartCartesianSeries[] GetSeries()
    {
        return Series;
    }

    /// <summary>Replaces the current series.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetSeries(ChartCartesianSeries[] value)
    {
        Series = value;
    }

    /// <summary>Returns the current series count.</summary>
    /// <returns>The current field value.</returns>
    public int GetSeriesCount()
    {
        return SeriesCount;
    }

    /// <summary>Replaces the current series count.</summary>
    /// <param name = "value">The replacement field value.</param>
    public void SetSeriesCount(int value)
    {
        SeriesCount = value;
    }
}
