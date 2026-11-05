namespace chart;
internal static class ChartPieChartsOperations
{
    /// <summary>
    /// Computes the layout for pie within the supplied bounds.
    /// </summary>
    /// <param name = "labels">The display labels in the same order as their corresponding values.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "centerX">The center x.</param>
    /// <param name = "centerY">The center y.</param>
    /// <param name = "radius">The radius in chart units, which must be positive.</param>
    /// <param name = "startAngle">The starting angle in degrees.</param>
    /// <param name = "order">The order.</param>
    /// <param name = "palette">The categorical colors to apply in stable index order.</param>
    /// <returns>The resulting pie layout.</returns>
    public static ChartPieLayout LayoutPie(string[] labels, double[] values, double centerX, double centerY, double radius, double startAngle, ChartPieSliceOrder order, ChartPalette palette)
    {
        return LayoutDonut(labels, values, centerX, centerY, 0.0, radius, startAngle, order, palette);
    }

    /// <summary>
    /// Computes the layout for donut within the supplied bounds.
    /// </summary>
    /// <param name = "labels">The display labels in the same order as their corresponding values.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "centerX">The center x.</param>
    /// <param name = "centerY">The center y.</param>
    /// <param name = "innerRadius">The inner radius in chart units; zero produces a solid shape.</param>
    /// <param name = "outerRadius">The outer radius in chart units, which must be greater than the inner radius.</param>
    /// <param name = "startAngle">The starting angle in degrees.</param>
    /// <param name = "order">The order.</param>
    /// <param name = "palette">The categorical colors to apply in stable index order.</param>
    /// <returns>The resulting pie layout.</returns>
    public static ChartPieLayout LayoutDonut(string[] labels, double[] values, double centerX, double centerY, double innerRadius, double outerRadius, double startAngle, ChartPieSliceOrder order, ChartPalette palette)
    {
        ValidateLabelsAndValues(labels, values);
        ChartPieSlice.ValidateRadii(innerRadius, outerRadius);
        ValidateOrder(order);
        ChartLineAreaChartsOperations.ValidatePalette(palette);
        int[] ordered = OrderedIndexes(values, order);
        double total = PositiveTotal(values);
        ChartPieSlice[] slices = new ChartPieSlice[values.Length];
        double angle = startAngle;
        int output = 0;
        while (output < ordered.Length)
        {
            int source = ordered[output];
            double sweep = values[source] / total * 360.0;
            slices[output] = new ChartPieSlice(labels[source], values[source], angle, angle + sweep, innerRadius, outerRadius, palette.GetColor(source));
            angle = angle + sweep;
            output = output + 1;
        }

        return new ChartPieLayout(centerX, centerY, slices);
    }

    /// <summary>
    /// Computes the layout for rose within the supplied bounds.
    /// </summary>
    /// <param name = "labels">The display labels in the same order as their corresponding values.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "centerX">The center x.</param>
    /// <param name = "centerY">The center y.</param>
    /// <param name = "innerRadius">The inner radius in chart units; zero produces a solid shape.</param>
    /// <param name = "maximumRadius">The maximum radius.</param>
    /// <param name = "startAngle">The starting angle in degrees.</param>
    /// <param name = "order">The order.</param>
    /// <param name = "palette">The categorical colors to apply in stable index order.</param>
    /// <returns>The resulting pie layout.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    public static ChartPieLayout LayoutRose(string[] labels, double[] values, double centerX, double centerY, double innerRadius, double maximumRadius, double startAngle, ChartPieSliceOrder order, ChartPalette palette)
    {
        ValidateLabelsAndValues(labels, values);
        ChartPieSlice.ValidateRadii(innerRadius, maximumRadius);
        ValidateOrder(order);
        ChartLineAreaChartsOperations.ValidatePalette(palette);
        int[] ordered = OrderedIndexes(values, order);
        double maximum = ChartStatisticsOperations.Max(values);
        if (maximum <= 0.0)
        {
            throw new System.ArgumentException("Rose chart values must include a positive value.");
        }

        ChartPieSlice[] slices = new ChartPieSlice[values.Length];
        double angle = startAngle;
        double sweep = 360.0 / values.Length;
        int output = 0;
        while (output < ordered.Length)
        {
            int source = ordered[output];
            double radius = innerRadius + (maximumRadius - innerRadius) * System.Math.Sqrt(values[source] / maximum);
            slices[output] = new ChartPieSlice(labels[source], values[source], angle, angle + sweep, innerRadius, radius, palette.GetColor(source));
            angle = angle + sweep;
            output = output + 1;
        }

        return new ChartPieLayout(centerX, centerY, slices);
    }

    /// <summary>
    /// Creates the requested pie scene from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "width">The width in pixels or chart units, which must be positive.</param>
    /// <param name = "height">The height in pixels or chart units, which must be positive.</param>
    /// <param name = "layout">The layout.</param>
    /// <returns>The newly created render scene.</returns>
    public static ChartRenderScene CreatePieScene(string id, int width, int height, ChartPieLayout layout)
    {
        ChartRenderScene scene = new ChartRenderScene(width, height);
        AddSlices(scene, id, layout);
        return scene;
    }

    internal static ChartRenderScene CreatePieChartScene(string id, int width, int height, string[] labels, double[] values, double centerX, double centerY, double radius, double startAngle, ChartPieSliceOrder order, ChartPalette palette)
    {
        ChartPieLayout layout = LayoutPie(labels, values, centerX, centerY, radius, startAngle, order, palette);
        return CreatePieScene(id, width, height, layout);
    }

    internal static ChartRenderScene CreateDonutScene(string id, int width, int height, string[] labels, double[] values, double centerX, double centerY, double innerRadius, double outerRadius, double startAngle, ChartPieSliceOrder order, ChartPalette palette)
    {
        ChartPieLayout layout = LayoutDonut(labels, values, centerX, centerY, innerRadius, outerRadius, startAngle, order, palette);
        return CreatePieScene(id, width, height, layout);
    }

    internal static ChartRenderScene CreateRoseScene(string id, int width, int height, string[] labels, double[] values, double centerX, double centerY, double innerRadius, double maximumRadius, double startAngle, ChartPieSliceOrder order, ChartPalette palette)
    {
        ChartPieLayout layout = LayoutRose(labels, values, centerX, centerY, innerRadius, maximumRadius, startAngle, order, palette);
        return CreatePieScene(id, width, height, layout);
    }

    /// <summary>
    /// Adds the supplied slices to this object.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "layout">The layout.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static void AddSlices(IChartRenderTarget scene, string id, ChartPieLayout layout)
    {
        if (scene == null)
        {
            throw new System.ArgumentNullException("scene");
        }

        ChartPlot.ValidateId(id);
        ValidateLayout(layout);
        int index = 0;
        while (index < layout.SliceCount)
        {
            ChartPieSlice slice = layout.SliceAt(index);
            ChartRenderStyle style = ChartRenderStyle.CreateFill(slice.Color);
            scene.AddPath(id + "-slice-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture), CreateSlicePath(layout.CenterX, layout.CenterY, slice), style);
            index = index + 1;
        }
    }

    /// <summary>
    /// Adds the supplied labels and leader lines to this object.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "layout">The layout.</param>
    /// <param name = "labelRadius">The label radius.</param>
    /// <param name = "labelStyle">The rendering style used for label.</param>
    /// <param name = "leaderStyle">The rendering style used for leader.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static void AddLabelsAndLeaderLines(IChartRenderTarget scene, string id, ChartPieLayout layout, double labelRadius, ChartRenderStyle labelStyle, ChartRenderStyle leaderStyle)
    {
        if (scene == null)
        {
            throw new System.ArgumentNullException("scene");
        }

        ChartPlot.ValidateId(id);
        ValidateLayout(layout);
        ChartRenderStyle.ValidatePositive(labelRadius, "labelRadius");
        ChartLineAreaChartsOperations.ValidateStyle(labelStyle);
        ChartLineAreaChartsOperations.ValidateStyle(leaderStyle);
        int index = 0;
        while (index < layout.SliceCount)
        {
            ChartPieSlice slice = layout.SliceAt(index);
            ChartPoint anchor = PointAtAngle(layout.CenterX, layout.CenterY, slice.MidAngle(), slice.OuterRadius);
            ChartPoint label = PointAtAngle(layout.CenterX, layout.CenterY, slice.MidAngle(), labelRadius);
            string suffix = index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            scene.AddLine(id + "-leader-" + suffix, anchor.X, anchor.Y, label.X, label.Y, leaderStyle);
            scene.AddText(id + "-label-" + suffix, slice.Label, label.X, label.Y, labelStyle);
            index = index + 1;
        }
    }

    /// <summary>
    /// Creates the requested slice path from the supplied data and configuration.
    /// </summary>
    /// <param name = "centerX">The center x.</param>
    /// <param name = "centerY">The center y.</param>
    /// <param name = "slice">The slice.</param>
    /// <returns>The newly created path.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static ChartPath CreateSlicePath(double centerX, double centerY, ChartPieSlice slice)
    {
        ValidateLayoutPoint(centerX, centerY);
        if (slice == null)
        {
            throw new System.ArgumentNullException("slice");
        }

        ChartPath path = new ChartPath();
        int segments = ArcSegmentCount(slice.StartAngle, slice.EndAngle);
        if (slice.InnerRadius == 0.0)
        {
            path.MoveTo(centerX, centerY);
            AddArcPoints(path, centerX, centerY, slice.OuterRadius, slice.StartAngle, slice.EndAngle, segments, false);
            path.Close();
            return path;
        }

        AddArcPoints(path, centerX, centerY, slice.OuterRadius, slice.StartAngle, slice.EndAngle, segments, true);
        AddArcPoints(path, centerX, centerY, slice.InnerRadius, slice.EndAngle, slice.StartAngle, segments, false);
        path.Close();
        return path;
    }

    internal static void AddArcPoints(ChartPath path, double centerX, double centerY, double radius, double startAngle, double endAngle, int segments, bool moveToFirst)
    {
        if (path == null)
        {
            throw new System.ArgumentNullException("path");
        }

        ChartRenderStyle.ValidatePositive(radius, "radius");
        if (segments <= 0)
        {
            throw new System.ArgumentOutOfRangeException("segments");
        }

        int index = 0;
        while (index <= segments)
        {
            double amount = index / (double)segments;
            double angle = startAngle + (endAngle - startAngle) * amount;
            ChartPoint point = PointAtAngle(centerX, centerY, angle, radius);
            if (index == 0 && moveToFirst)
            {
                path.MoveTo(point.X, point.Y);
            }
            else
            {
                path.LineTo(point.X, point.Y);
            }

            index = index + 1;
        }
    }

    internal static ChartPoint PointAtAngle(double centerX, double centerY, double angleDegrees, double radius)
    {
        ValidateLayoutPoint(centerX, centerY);
        ChartLinearScale.ValidateFinite(angleDegrees, "angleDegrees");
        ChartRenderStyle.ValidatePositive(radius, "radius");
        double radians = (angleDegrees - 90.0) * System.Math.PI / 180.0;
        return new ChartPoint(centerX + System.Math.Cos(radians) * radius, centerY + System.Math.Sin(radians) * radius);
    }

    internal static int[] OrderedIndexes(double[] values, ChartPieSliceOrder order)
    {
        ChartStatisticsOperations.ValidateFiniteValues(values);
        ValidateOrder(order);
        int[] indexes = new int[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            indexes[index] = index;
            index = index + 1;
        }

        if (order == ChartPieSliceOrder.Original)
        {
            return indexes;
        }

        int outer = 0;
        while (outer < indexes.Length)
        {
            int selected = outer;
            int inner = outer + 1;
            while (inner < indexes.Length)
            {
                if (ComesBefore(values, indexes[inner], indexes[selected], order))
                {
                    selected = inner;
                }

                inner = inner + 1;
            }

            if (selected != outer)
            {
                int temp = indexes[outer];
                indexes[outer] = indexes[selected];
                indexes[selected] = temp;
            }

            outer = outer + 1;
        }

        return indexes;
    }

    internal static bool ComesBefore(double[] values, int left, int right, ChartPieSliceOrder order)
    {
        if (order == ChartPieSliceOrder.Ascending)
        {
            return values[left] < values[right];
        }

        if (order == ChartPieSliceOrder.Descending)
        {
            return values[left] > values[right];
        }

        return left < right;
    }

    internal static double PositiveTotal(double[] values)
    {
        ChartStatisticsOperations.ValidateFiniteValues(values);
        double total = 0.0;
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] < 0.0)
            {
                throw new System.ArgumentOutOfRangeException("values");
            }

            total = total + values[index];
            index = index + 1;
        }

        if (total <= 0.0)
        {
            throw new System.ArgumentException("Pie values must have a positive total.");
        }

        return total;
    }

    internal static int ArcSegmentCount(double startAngle, double endAngle)
    {
        ChartLinearScale.ValidateFinite(startAngle, "startAngle");
        ChartLinearScale.ValidateFinite(endAngle, "endAngle");
        double sweep = System.Math.Abs(endAngle - startAngle);
        double countValue = System.Math.Ceiling(sweep / 15.0);
        if (double.IsInfinity(countValue) || countValue > int.MaxValue)
        {
            throw new System.ArgumentOutOfRangeException("endAngle");
        }

        int count = (int)countValue;
        if (count < 2)
        {
            return 2;
        }

        return count;
    }

    internal static void ValidateLabelsAndValues(string[] labels, double[] values)
    {
        if (labels == null)
        {
            throw new System.ArgumentNullException("labels");
        }

        ChartStatisticsOperations.ValidateFiniteValues(values);
        if (labels.Length != values.Length)
        {
            throw new System.ArgumentException("Label count must match value count.");
        }

        int index = 0;
        while (index < labels.Length)
        {
            ChartTextBlock.ValidateText(labels[index], "labels");
            index = index + 1;
        }

        PositiveTotal(values);
    }

    internal static void ValidateOrder(ChartPieSliceOrder order)
    {
        int value = (int)order;
        if (value < 0 || value > 2)
        {
            throw new System.ArgumentOutOfRangeException("order");
        }
    }

    internal static void ValidateLayout(ChartPieLayout layout)
    {
        if (layout == null)
        {
            throw new System.ArgumentNullException("layout");
        }
    }

    internal static void ValidateLayoutPoint(double centerX, double centerY)
    {
        ChartLinearScale.ValidateFinite(centerX, "centerX");
        ChartLinearScale.ValidateFinite(centerY, "centerY");
    }
}
