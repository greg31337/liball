namespace chart;
internal static class ChartLabelPlacementChartsOperations
{
    /// <summary>Formats numeric values for use by any label layout operation.</summary>
    /// <param name = "values">The finite numeric values.</param>
    /// <param name = "formatter">The value-label formatter.</param>
    /// <returns>One formatted label per input value.</returns>
    public static string[] FormatValueLabels(double[] values, IChartValueLabelFormatter formatter)
    {
        if (values == null)
        {
            throw new System.ArgumentNullException("values");
        }

        if (formatter == null)
        {
            throw new System.ArgumentNullException("formatter");
        }

        string[] labels = new string[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            ChartLinearScale.ValidateFinite(values[index], "values");
            labels[index] = formatter.FormatValue(values[index]);
            ChartTextBlock.ValidateText(labels[index], "formattedLabel");
            index = index + 1;
        }

        return labels;
    }

    /// <summary>
    /// Computes the layout for point labels within the supplied bounds.
    /// </summary>
    /// <param name = "idPrefix">The prefix used to create stable identifiers for generated items.</param>
    /// <param name = "labels">The display labels in the same order as their corresponding values.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <param name = "labelWidth">The label width.</param>
    /// <param name = "labelHeight">The label height.</param>
    /// <param name = "offset">The offset.</param>
    /// <param name = "position">The position.</param>
    /// <param name = "reduceCollisions">The reduce collisions.</param>
    /// <returns>The resulting placed label array.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    public static ChartPlacedLabel[] LayoutPointLabels(string idPrefix, string[] labels, double[] xValues, double[] yValues, double labelWidth, double labelHeight, double offset, ChartLabelPosition position, bool reduceCollisions)
    {
        ChartPlot.ValidateId(idPrefix);
        ValidateTextLabels(labels, 1);
        ChartCartesianTransform.ValidateValueArrays(xValues, yValues, 1);
        if (labels.Length != xValues.Length)
        {
            throw new System.ArgumentException("Point label count must match point count.");
        }

        ValidateLabelBox(labelWidth, labelHeight);
        ValidateOffset(offset);
        ValidatePosition(position);
        ChartPlacedLabel[] placed = new ChartPlacedLabel[labels.Length];
        int index = 0;
        while (index < labels.Length)
        {
            placed[index] = CreatePlacedLabel(idPrefix, labels[index], index, xValues[index], yValues[index], labelWidth, labelHeight, offset, position);
            index = index + 1;
        }

        if (reduceCollisions)
        {
            ReduceCollisions(placed);
        }

        return placed;
    }

    /// <summary>
    /// Computes the layout for bar labels within the supplied bounds.
    /// </summary>
    /// <param name = "idPrefix">The prefix used to create stable identifiers for generated items.</param>
    /// <param name = "labels">The display labels in the same order as their corresponding values.</param>
    /// <param name = "bars">The bars.</param>
    /// <param name = "labelWidth">The label width.</param>
    /// <param name = "labelHeight">The label height.</param>
    /// <param name = "offset">The offset.</param>
    /// <param name = "position">The position.</param>
    /// <param name = "reduceCollisions">The reduce collisions.</param>
    /// <returns>The resulting placed label array.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static ChartPlacedLabel[] LayoutBarLabels(string idPrefix, string[] labels, ChartCartesianSeries bars, double labelWidth, double labelHeight, double offset, ChartLabelPosition position, bool reduceCollisions)
    {
        ChartPlot.ValidateId(idPrefix);
        ValidateTextLabels(labels, 1);
        if (bars == null)
        {
            throw new System.ArgumentNullException("bars");
        }

        if (labels.Length != bars.CommandCount)
        {
            throw new System.ArgumentException("Bar label count must match bar command count.");
        }

        ValidateLabelBox(labelWidth, labelHeight);
        ValidateOffset(offset);
        ValidatePosition(position);
        ChartPlacedLabel[] placed = new ChartPlacedLabel[labels.Length];
        int index = 0;
        while (index < labels.Length)
        {
            ChartRenderCommand command = bars.CommandAt(index);
            if (command.Kind != ChartRenderCommandKind.Polygon)
            {
                throw new System.ArgumentException("Bar labels require polygon bar commands.");
            }

            ChartRect bounds = BoundsOfPoints(command.Points);
            ChartPoint anchor = AnchorForBounds(bounds, position);
            placed[index] = CreatePlacedLabel(idPrefix, labels[index], index, anchor.X, anchor.Y, labelWidth, labelHeight, offset, position);
            index = index + 1;
        }

        if (reduceCollisions)
        {
            ReduceCollisions(placed);
        }

        return placed;
    }

    /// <summary>
    /// Computes the layout for heatmap cell labels within the supplied bounds.
    /// </summary>
    /// <param name = "idPrefix">The prefix used to create stable identifiers for generated items.</param>
    /// <param name = "labels">The display labels in the same order as their corresponding values.</param>
    /// <param name = "xCenters">The x centers.</param>
    /// <param name = "yCenters">The y centers.</param>
    /// <param name = "cellWidth">The cell width.</param>
    /// <param name = "cellHeight">The cell height.</param>
    /// <param name = "labelWidth">The label width.</param>
    /// <param name = "labelHeight">The label height.</param>
    /// <param name = "reduceCollisions">The reduce collisions.</param>
    /// <returns>The resulting placed label array.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    public static ChartPlacedLabel[] LayoutHeatmapCellLabels(string idPrefix, string[] labels, double[] xCenters, double[] yCenters, double cellWidth, double cellHeight, double labelWidth, double labelHeight, bool reduceCollisions)
    {
        ChartPlot.ValidateId(idPrefix);
        ValidateTextLabels(labels, 1);
        ValidateCenters(xCenters, "xCenters");
        ValidateCenters(yCenters, "yCenters");
        ChartRenderStyle.ValidatePositive(cellWidth, "cellWidth");
        ChartRenderStyle.ValidatePositive(cellHeight, "cellHeight");
        ValidateLabelBox(labelWidth, labelHeight);
        long expected = (long)xCenters.Length * yCenters.Length;
        if (labels.Length != expected)
        {
            throw new System.ArgumentException("Heatmap label count must match cell count.");
        }

        ChartPlacedLabel[] placed = new ChartPlacedLabel[labels.Length];
        int row = 0;
        int output = 0;
        while (row < yCenters.Length)
        {
            int column = 0;
            while (column < xCenters.Length)
            {
                placed[output] = CreatePlacedLabel(idPrefix, labels[output], output, xCenters[column], yCenters[row], labelWidth, labelHeight, 0.0, ChartLabelPosition.Center);
                output = output + 1;
                column = column + 1;
            }

            row = row + 1;
        }

        if (reduceCollisions)
        {
            ReduceCollisions(placed);
        }

        return placed;
    }

    /// <summary>
    /// Computes the layout for pie slice labels within the supplied bounds.
    /// </summary>
    /// <param name = "idPrefix">The prefix used to create stable identifiers for generated items.</param>
    /// <param name = "layout">The layout.</param>
    /// <param name = "labelRadius">The label radius.</param>
    /// <param name = "labelWidth">The label width.</param>
    /// <param name = "labelHeight">The label height.</param>
    /// <param name = "reduceCollisions">The reduce collisions.</param>
    /// <returns>The resulting placed label array.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static ChartPlacedLabel[] LayoutPieSliceLabels(string idPrefix, ChartPieLayout layout, double labelRadius, double labelWidth, double labelHeight, bool reduceCollisions)
    {
        ChartPlot.ValidateId(idPrefix);
        if (layout == null)
        {
            throw new System.ArgumentNullException("layout");
        }

        ChartRenderStyle.ValidatePositive(labelRadius, "labelRadius");
        ValidateLabelBox(labelWidth, labelHeight);
        ChartPlacedLabel[] placed = new ChartPlacedLabel[layout.SliceCount];
        int index = 0;
        while (index < layout.SliceCount)
        {
            ChartPieSlice slice = layout.SliceAt(index);
            ChartPoint anchor = ChartPieChartsOperations.PointAtAngle(layout.CenterX, layout.CenterY, slice.MidAngle(), labelRadius);
            placed[index] = CreatePlacedLabel(idPrefix, slice.Label, index, anchor.X, anchor.Y, labelWidth, labelHeight, 0.0, ChartLabelPosition.Center);
            index = index + 1;
        }

        if (reduceCollisions)
        {
            ReduceCollisions(placed);
        }

        return placed;
    }

    /// <summary>
    /// Computes the layout for contour labels within the supplied bounds.
    /// </summary>
    /// <param name = "idPrefix">The prefix used to create stable identifiers for generated items.</param>
    /// <param name = "labels">The display labels in the same order as their corresponding values.</param>
    /// <param name = "segments">The segments.</param>
    /// <param name = "labelWidth">The label width.</param>
    /// <param name = "labelHeight">The label height.</param>
    /// <param name = "reduceCollisions">The reduce collisions.</param>
    /// <returns>The resulting placed label array.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static ChartPlacedLabel[] LayoutContourLabels(string idPrefix, string[] labels, ChartContourSegment[] segments, double labelWidth, double labelHeight, bool reduceCollisions)
    {
        ChartPlot.ValidateId(idPrefix);
        ValidateTextLabels(labels, 1);
        if (segments == null)
        {
            throw new System.ArgumentNullException("segments");
        }

        if (labels.Length != segments.Length)
        {
            throw new System.ArgumentException("Contour label count must match segment count.");
        }

        ValidateLabelBox(labelWidth, labelHeight);
        ChartPlacedLabel[] placed = new ChartPlacedLabel[labels.Length];
        int index = 0;
        while (index < labels.Length)
        {
            if (segments[index] == null)
            {
                throw new System.ArgumentException("Contour segments cannot contain null values.");
            }

            double anchorX = (segments[index].Start.X + segments[index].End.X) * 0.5;
            double anchorY = (segments[index].Start.Y + segments[index].End.Y) * 0.5;
            placed[index] = CreatePlacedLabel(idPrefix, labels[index], index, anchorX, anchorY, labelWidth, labelHeight, 0.0, ChartLabelPosition.Center);
            index = index + 1;
        }

        if (reduceCollisions)
        {
            ReduceCollisions(placed);
        }

        return placed;
    }

    /// <summary>
    /// Computes the layout for annotation labels within the supplied bounds.
    /// </summary>
    /// <param name = "idPrefix">The prefix used to create stable identifiers for generated items.</param>
    /// <param name = "labels">The display labels in the same order as their corresponding values.</param>
    /// <param name = "annotationBounds">The annotation bounds.</param>
    /// <param name = "labelWidth">The label width.</param>
    /// <param name = "labelHeight">The label height.</param>
    /// <param name = "offset">The offset.</param>
    /// <param name = "position">The position.</param>
    /// <param name = "reduceCollisions">The reduce collisions.</param>
    /// <returns>The resulting placed label array.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static ChartPlacedLabel[] LayoutAnnotationLabels(string idPrefix, string[] labels, ChartRect[] annotationBounds, double labelWidth, double labelHeight, double offset, ChartLabelPosition position, bool reduceCollisions)
    {
        ChartPlot.ValidateId(idPrefix);
        ValidateTextLabels(labels, 1);
        if (annotationBounds == null)
        {
            throw new System.ArgumentNullException("annotationBounds");
        }

        if (labels.Length != annotationBounds.Length)
        {
            throw new System.ArgumentException("Annotation label count must match annotation count.");
        }

        ValidateLabelBox(labelWidth, labelHeight);
        ValidateOffset(offset);
        ValidatePosition(position);
        ChartPlacedLabel[] placed = new ChartPlacedLabel[labels.Length];
        int index = 0;
        while (index < labels.Length)
        {
            ChartAxis.ValidatePlotArea(annotationBounds[index]);
            ChartPoint anchor = AnchorForBounds(annotationBounds[index], position);
            placed[index] = CreatePlacedLabel(idPrefix, labels[index], index, anchor.X, anchor.Y, labelWidth, labelHeight, offset, position);
            index = index + 1;
        }

        if (reduceCollisions)
        {
            ReduceCollisions(placed);
        }

        return placed;
    }

    /// <summary>
    /// Creates the requested label series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "labels">The display labels in the same order as their corresponding values.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public static ChartCartesianSeries CreateLabelSeries(string id, ChartPlacedLabel[] labels, ChartRenderStyle style)
    {
        ChartPlot.ValidateId(id);
        ValidatePlacedLabels(labels);
        ChartLineAreaChartsOperations.ValidateStyle(style);
        ChartCartesianSeries series = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Mixed);
        int index = 0;
        while (index < labels.Length)
        {
            if (labels[index].Visible)
            {
                series.AddText(labels[index].Id, labels[index].Text, labels[index].X, labels[index].Y, style);
            }

            index = index + 1;
        }

        return series;
    }

    /// <summary>
    /// Adds the supplied visible labels to scene to this object.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name = "labels">The display labels in the same order as their corresponding values.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    public static void AddVisibleLabelsToScene(IChartRenderTarget scene, ChartPlacedLabel[] labels, ChartRenderStyle style)
    {
        ChartAnnotationChartsOperations.ValidateScene(scene);
        ValidatePlacedLabels(labels);
        ChartLineAreaChartsOperations.ValidateStyle(style);
        int index = 0;
        while (index < labels.Length)
        {
            if (labels[index].Visible)
            {
                scene.AddText(labels[index].Id, labels[index].Text, labels[index].X, labels[index].Y, style);
            }

            index = index + 1;
        }
    }

    /// <summary>Adds visible labels and leader lines after collision visibility has been resolved.</summary>
    /// <param name = "scene">The retained scene that receives the labels and leaders.</param>
    /// <param name = "labels">The placed labels, including their source anchors and visibility.</param>
    /// <param name = "labelStyle">The label render style.</param>
    /// <param name = "leaderStyle">The leader-line render style.</param>
    public static void AddVisibleLabelsAndLeaderLinesToScene(IChartRenderTarget scene, ChartPlacedLabel[] labels, ChartRenderStyle labelStyle, ChartRenderStyle leaderStyle)
    {
        ChartAnnotationChartsOperations.ValidateScene(scene);
        ValidatePlacedLabels(labels);
        ChartLineAreaChartsOperations.ValidateStyle(labelStyle);
        ChartLineAreaChartsOperations.ValidateStyle(leaderStyle);
        int index = 0;
        while (index < labels.Length)
        {
            ChartPlacedLabel label = labels[index];
            if (label.Visible)
            {
                ChartPoint start = ChartAnnotationChartsOperations.CalloutLeaderStart(label.Bounds(), label.AnchorX, label.AnchorY);
                scene.AddLine(label.Id + "-leader", label.AnchorX, label.AnchorY, start.X, start.Y, leaderStyle);
                scene.AddText(label.Id, label.Text, label.X, label.Y, labelStyle);
            }

            index = index + 1;
        }
    }

    /// <summary>
    /// Applies the reduce collisions operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "labels">The display labels in the same order as their corresponding values.</param>
    /// <returns>The resulting int.</returns>
    public static int ReduceCollisions(ChartPlacedLabel[] labels)
    {
        ValidatePlacedLabels(labels);
        int hidden = 0;
        int index = 0;
        while (index < labels.Length)
        {
            labels[index].Visible = true;
            int previous = 0;
            while (previous < index)
            {
                if (labels[previous].Visible && LabelsOverlap(labels[previous], labels[index]))
                {
                    labels[index].Visible = false;
                }

                previous = previous + 1;
            }

            if (!labels[index].Visible)
            {
                hidden = hidden + 1;
            }

            index = index + 1;
        }

        return hidden;
    }

    internal static ChartPlacedLabel CreatePlacedLabel(string idPrefix, string text, int sourceIndex, double anchorX, double anchorY, double labelWidth, double labelHeight, double offset, ChartLabelPosition position)
    {
        ChartPlot.ValidateId(idPrefix);
        ChartTextBlock.ValidateText(text, "text");
        if (sourceIndex < 0)
        {
            throw new System.ArgumentOutOfRangeException("sourceIndex");
        }

        ChartRenderCommand.ValidateFinitePair(anchorX, anchorY, "anchor");
        ValidateLabelBox(labelWidth, labelHeight);
        ValidateOffset(offset);
        ValidatePosition(position);
        double x = anchorX - labelWidth * 0.5;
        double y = anchorY - labelHeight * 0.5;
        if (position == ChartLabelPosition.Above)
        {
            y = anchorY - offset - labelHeight;
        }
        else if (position == ChartLabelPosition.Below)
        {
            y = anchorY + offset;
        }
        else if (position == ChartLabelPosition.Left)
        {
            x = anchorX - offset - labelWidth;
        }
        else if (position == ChartLabelPosition.Right)
        {
            x = anchorX + offset;
        }

        string id = idPrefix + "-" + sourceIndex.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return new ChartPlacedLabel(id, text, sourceIndex, anchorX, anchorY, x, y, labelWidth, labelHeight, true, position);
    }

    internal static ChartPoint AnchorForBounds(ChartRect bounds, ChartLabelPosition position)
    {
        ChartAxis.ValidatePlotArea(bounds);
        ValidatePosition(position);
        double centerX = bounds.X + bounds.Width * 0.5;
        double centerY = bounds.Y + bounds.Height * 0.5;
        if (position == ChartLabelPosition.Above)
        {
            return new ChartPoint(centerX, bounds.Y);
        }

        if (position == ChartLabelPosition.Below)
        {
            return new ChartPoint(centerX, bounds.Y + bounds.Height);
        }

        if (position == ChartLabelPosition.Left)
        {
            return new ChartPoint(bounds.X, centerY);
        }

        if (position == ChartLabelPosition.Right)
        {
            return new ChartPoint(bounds.X + bounds.Width, centerY);
        }

        return new ChartPoint(centerX, centerY);
    }

    internal static ChartRect BoundsOfPoints(ChartPoint[] points)
    {
        ChartRenderCommand.ValidatePoints(points, 1);
        double left = points[0].X;
        double right = points[0].X;
        double top = points[0].Y;
        double bottom = points[0].Y;
        int index = 1;
        while (index < points.Length)
        {
            if (points[index].X < left)
            {
                left = points[index].X;
            }

            if (points[index].X > right)
            {
                right = points[index].X;
            }

            if (points[index].Y < top)
            {
                top = points[index].Y;
            }

            if (points[index].Y > bottom)
            {
                bottom = points[index].Y;
            }

            index = index + 1;
        }

        return new ChartRect(left, top, right - left, bottom - top);
    }

    internal static bool LabelsOverlap(ChartPlacedLabel first, ChartPlacedLabel second)
    {
        if (first == null)
        {
            throw new System.ArgumentNullException("first");
        }

        if (second == null)
        {
            throw new System.ArgumentNullException("second");
        }

        return first.X < second.X + second.Width && first.X + first.Width > second.X && first.Y < second.Y + second.Height && first.Y + first.Height > second.Y;
    }

    internal static void ValidateTextLabels(string[] labels, int minimumLength)
    {
        if (labels == null)
        {
            throw new System.ArgumentNullException("labels");
        }

        if (labels.Length < minimumLength)
        {
            throw new System.ArgumentException("Not enough labels.");
        }

        int index = 0;
        while (index < labels.Length)
        {
            ChartTextBlock.ValidateText(labels[index], "labels");
            index = index + 1;
        }
    }

    internal static void ValidatePlacedLabels(ChartPlacedLabel[] labels)
    {
        if (labels == null)
        {
            throw new System.ArgumentNullException("labels");
        }

        int index = 0;
        while (index < labels.Length)
        {
            if (labels[index] == null)
            {
                throw new System.ArgumentException("Placed labels cannot contain null values.");
            }

            index = index + 1;
        }
    }

    internal static void ValidateCenters(double[] centers, string name)
    {
        if (centers == null)
        {
            throw new System.ArgumentNullException(name);
        }

        if (centers.Length == 0)
        {
            throw new System.ArgumentException("Centers cannot be empty.");
        }

        int index = 0;
        while (index < centers.Length)
        {
            ChartLinearScale.ValidateFinite(centers[index], name);
            index = index + 1;
        }
    }

    internal static void ValidateLabelBox(double width, double height)
    {
        ChartRenderStyle.ValidatePositive(width, "width");
        ChartRenderStyle.ValidatePositive(height, "height");
    }

    internal static void ValidateOffset(double offset)
    {
        ChartStyle.ValidateNonNegative(offset, "offset");
    }

    internal static void ValidatePosition(ChartLabelPosition position)
    {
        int value = (int)position;
        if (value < 0 || value > 4)
        {
            throw new System.ArgumentOutOfRangeException("position");
        }
    }
}
