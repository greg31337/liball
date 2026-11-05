namespace chart;

/// <summary>
/// Checks render data and returns structured validation issues without rendering it.
/// </summary>
internal static class ChartRenderValidatorOperations
{
    /// <summary>
    /// The stable validation code for invalid document size.
    /// </summary>
    public const string InvalidDocumentSizeCode = "InvalidDocumentSize";
    /// <summary>
    /// The stable validation code for invalid margins.
    /// </summary>
    public const string InvalidMarginsCode = "InvalidMargins";
    /// <summary>
    /// The stable validation code for missing plot.
    /// </summary>
    public const string MissingPlotCode = "MissingPlot";
    /// <summary>
    /// The stable validation code for duplicate plot id.
    /// </summary>
    public const string DuplicatePlotIdCode = "DuplicatePlotId";
    /// <summary>
    /// The stable validation code for invalid plot bounds.
    /// </summary>
    public const string InvalidPlotBoundsCode = "InvalidPlotBounds";
    /// <summary>
    /// The stable validation code for empty inner bounds.
    /// </summary>
    public const string EmptyInnerBoundsCode = "EmptyInnerBounds";
    /// <summary>
    /// The stable validation code for missing axis.
    /// </summary>
    public const string MissingAxisCode = "MissingAxis";
    /// <summary>
    /// The stable validation code for invalid axis domain.
    /// </summary>
    public const string InvalidAxisDomainCode = "InvalidAxisDomain";
    /// <summary>
    /// The stable validation code for invalid axis plot area.
    /// </summary>
    public const string InvalidAxisPlotAreaCode = "InvalidAxisPlotArea";
    /// <summary>
    /// The stable validation code for empty series.
    /// </summary>
    public const string EmptySeriesCode = "EmptySeries";
    /// <summary>
    /// The stable validation code for invalid series command.
    /// </summary>
    public const string InvalidSeriesCommandCode = "InvalidSeriesCommand";
    /// <summary>
    /// The stable validation code for empty scene.
    /// </summary>
    public const string EmptySceneCode = "EmptyScene";
    /// <summary>
    /// The stable validation code for invalid scene command.
    /// </summary>
    public const string InvalidSceneCommandCode = "InvalidSceneCommand";

    /// <summary>
    /// Validates document and returns structured issues without rendering.
    /// </summary>
    /// <param name="document">The chart document whose model, layout, or output is processed.</param>
    /// <returns>The resulting validation result.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static ChartValidationResult ValidateDocument(ChartDocument document)
    {
        if (document == null)
        {
            throw new System.ArgumentNullException("document");
        }

        ChartValidationResult result = new ChartValidationResult();
        if (!IsPositiveFinite(document.Width) || !IsPositiveFinite(document.Height))
        {
            result.AddError(InvalidDocumentSizeCode, "Document size must be finite and positive.", "document", -1);
        }

        ValidateMargins(document.Margins, document.Width, document.Height, result);
        if (document.PlotCount == 0)
        {
            result.AddWarning(MissingPlotCode, "Document has no plots.", "document", -1);
        }

        if (document.Plots == null || document.PlotCount < 0 || document.PlotCount > document.Plots.Length)
        {
            result.AddError(MissingPlotCode, "Document plot storage is invalid.", "document", -1);
            return result;
        }

        int index = 0;
        while (index < document.PlotCount)
        {
            ChartPlot plot = document.Plots[index];
            if (plot == null)
            {
                result.AddError(MissingPlotCode, "Document contains a missing plot.", "plot", index);
            }
            else
            {
                ValidatePlotInto(plot, result);
                int previous = 0;
                while (previous < index)
                {
                    if (document.Plots[previous] != null && document.Plots[previous].Id == plot.Id)
                    {
                        result.AddError(DuplicatePlotIdCode, "Document contains duplicate plot ids.", plot.Id, index);
                    }

                    previous = previous + 1;
                }
            }

            index = index + 1;
        }

        return result;
    }

    /// <summary>
    /// Validates plot and returns structured issues without rendering.
    /// </summary>
    /// <param name="plot">The plot whose bounds, axes, and styles are processed.</param>
    /// <returns>The resulting validation result.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static ChartValidationResult ValidatePlot(ChartPlot plot)
    {
        if (plot == null)
        {
            throw new System.ArgumentNullException("plot");
        }

        ChartValidationResult result = new ChartValidationResult();
        ValidatePlotInto(plot, result);
        return result;
    }

    internal static void ValidatePlotInto(ChartPlot plot, ChartValidationResult result)
    {
        string plotId = plot.Id;
        if (plotId == null)
        {
            plotId = "plot";
        }

        if (plot.Id == null || plot.Id.Length == 0)
        {
            result.AddError(InvalidPlotBoundsCode, "Plot id is missing.", "plot", -1);
        }

        if (!IsPlotAreaValid(plot.Bounds))
        {
            result.AddError(InvalidPlotBoundsCode, "Plot bounds must be finite and non-empty.", plotId, -1);
        }

        ValidateMargins(plot.Margins, plot.Bounds.Width, plot.Bounds.Height, result);
        if (IsPlotAreaValid(plot.Bounds) && IsMarginsFiniteNonNegative(plot.Margins))
        {
            ChartRect inner = plot.InnerBounds();
            if (inner.Width <= 0.0 || inner.Height <= 0.0)
            {
                result.AddError(EmptyInnerBoundsCode, "Plot margins leave no inner plotting area.", plotId, -1);
            }
        }

        if (plot.AxisCount == 0)
        {
            result.AddWarning(MissingAxisCode, "Plot has no axes.", plotId, -1);
        }

        if (plot.Axes == null || plot.AxisCount < 0 || plot.AxisCount > plot.Axes.Length)
        {
            result.AddError(MissingAxisCode, "Plot axis storage is invalid.", plotId, -1);
            return;
        }

        int index = 0;
        while (index < plot.AxisCount)
        {
            ChartAxis axis = plot.Axes[index];
            if (axis == null)
            {
                result.AddError(MissingAxisCode, "Plot contains a missing axis.", plotId, index);
            }
            else
            {
                ValidateAxisInto(axis, result, plotId, index);
            }

            index = index + 1;
        }
    }

    internal static void ValidateAxisInto(ChartAxis axis, ChartValidationResult result, string plotId, int axisIndex)
    {
        if (axis.Domain == null || !IsFinite(axis.Domain.Minimum) || !IsFinite(axis.Domain.Maximum) || axis.Domain.Minimum == axis.Domain.Maximum)
        {
            result.AddError(InvalidAxisDomainCode, "Axis domain must be finite and non-zero.", plotId, axisIndex);
        }

        if (!IsPlotAreaValid(axis.PlotArea))
        {
            result.AddError(InvalidAxisPlotAreaCode, "Axis plot area must be finite and non-empty.", plotId, axisIndex);
        }
    }

    /// <summary>
    /// Validates series and returns structured issues without rendering.
    /// </summary>
    /// <param name="series">The chart series to render, validate, or combine.</param>
    /// <returns>The resulting validation result.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static ChartValidationResult ValidateSeries(ChartCartesianSeries series)
    {
        if (series == null)
        {
            throw new System.ArgumentNullException("series");
        }

        ChartValidationResult result = new ChartValidationResult();
        if (series.CommandCount == 0)
        {
            result.AddError(EmptySeriesCode, "Series has no render commands.", series.Id, -1);
        }

        if (series.Commands == null || series.CommandCount < 0 || series.CommandCount > series.Commands.Length)
        {
            result.AddError(InvalidSeriesCommandCode, "Series command storage is invalid.", series.Id, -1);
            return result;
        }

        int index = 0;
        while (index < series.CommandCount)
        {
            ValidateCommandInto(series.Commands[index], InvalidSeriesCommandCode, series.Id, index, result);
            index = index + 1;
        }

        return result;
    }

    /// <summary>
    /// Validates scene and returns structured issues without rendering.
    /// </summary>
    /// <param name="scene">The retained scene that receives or supplies drawing commands.</param>
    /// <returns>The resulting validation result.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static ChartValidationResult ValidateScene(ChartRenderScene scene)
    {
        if (scene == null)
        {
            throw new System.ArgumentNullException("scene");
        }

        ChartValidationResult result = new ChartValidationResult();
        if (scene.Width <= 0 || scene.Height <= 0)
        {
            result.AddError(InvalidDocumentSizeCode, "Scene size must be positive.", "scene", -1);
        }

        if (scene.CommandCount == 0)
        {
            result.AddWarning(EmptySceneCode, "Scene has no render commands.", "scene", -1);
        }

        if (scene.Commands == null || scene.CommandCount < 0 || scene.CommandCount > scene.Commands.Length)
        {
            result.AddError(InvalidSceneCommandCode, "Scene command storage is invalid.", "scene", -1);
            return result;
        }

        int index = 0;
        while (index < scene.CommandCount)
        {
            ValidateCommandInto(scene.Commands[index], InvalidSceneCommandCode, "scene", index, result);
            index = index + 1;
        }

        return result;
    }

    /// <summary>
    /// Validates cartesian render and returns structured issues without rendering.
    /// </summary>
    /// <param name="plot">The plot whose bounds, axes, and styles are processed.</param>
    /// <param name="series">The chart series to render, validate, or combine.</param>
    /// <param name="xDomain">The x domain.</param>
    /// <param name="yDomain">The y domain.</param>
    /// <returns>The resulting validation result.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static ChartValidationResult ValidateCartesianRender(ChartPlot plot, ChartCartesianSeries[] series, ChartRange xDomain, ChartRange yDomain)
    {
        if (plot == null)
        {
            throw new System.ArgumentNullException("plot");
        }

        if (series == null)
        {
            throw new System.ArgumentNullException("series");
        }

        ChartValidationResult result = ValidatePlot(plot);
        if (!IsDomainValid(xDomain))
        {
            result.AddError(InvalidAxisDomainCode, "X render domain must be finite and non-zero.", plot.Id, -1);
        }

        if (!IsDomainValid(yDomain))
        {
            result.AddError(InvalidAxisDomainCode, "Y render domain must be finite and non-zero.", plot.Id, -1);
        }

        int index = 0;
        while (index < series.Length)
        {
            if (series[index] == null)
            {
                result.AddError(InvalidSeriesCommandCode, "Render series array contains a missing series.", plot.Id, index);
            }
            else
            {
                result.AddResult(ValidateSeries(series[index]));
            }

            index = index + 1;
        }

        return result;
    }

    internal static void ValidateCommandInto(ChartRenderCommand command, string code, string owner, int index, ChartValidationResult result)
    {
        if (command == null)
        {
            result.AddError(code, "Render command is missing.", owner, index);
            return;
        }

        int kind = (int)command.Kind;
        if (kind < 0 || kind > 18)
        {
            result.AddError(code, "Render command kind is invalid.", owner, index);
        }

        if (command.Id == null)
        {
            result.AddError(code, "Render command id is missing.", owner, index);
        }

        if (command.Style == null)
        {
            result.AddError(code, "Render command style is missing.", owner, index);
        }

        if (command.Kind == ChartRenderCommandKind.Line)
        {
            ValidateFinitePair(command.X1, command.Y1, code, owner, index, result);
            ValidateFinitePair(command.X2, command.Y2, code, owner, index, result);
        }
        else if (command.Kind == ChartRenderCommandKind.Polyline)
        {
            ValidatePoints(command.Points, 2, code, owner, index, result);
        }
        else if (command.Kind == ChartRenderCommandKind.Polygon)
        {
            ValidatePoints(command.Points, 3, code, owner, index, result);
        }
        else if (command.Kind == ChartRenderCommandKind.Rect)
        {
            ValidateFinitePair(command.X1, command.Y1, code, owner, index, result);
            if (!IsPositiveFinite(command.Width) || !IsPositiveFinite(command.Height))
            {
                result.AddError(code, "Rectangle command size is invalid.", owner, index);
            }
        }
        else if (command.Kind == ChartRenderCommandKind.Circle)
        {
            ValidateFinitePair(command.X1, command.Y1, code, owner, index, result);
            if (!IsPositiveFinite(command.RadiusX))
            {
                result.AddError(code, "Circle command radius is invalid.", owner, index);
            }
        }
        else if (command.Kind == ChartRenderCommandKind.Ellipse)
        {
            ValidateFinitePair(command.X1, command.Y1, code, owner, index, result);
            if (!IsPositiveFinite(command.RadiusX) || !IsPositiveFinite(command.RadiusY))
            {
                result.AddError(code, "Ellipse command radius is invalid.", owner, index);
            }
        }
        else if (command.Kind == ChartRenderCommandKind.Path)
        {
            ValidatePath(command.Path, code, owner, index, result);
        }
        else if (command.Kind == ChartRenderCommandKind.Text)
        {
            ValidateFinitePair(command.X1, command.Y1, code, owner, index, result);
            if (command.Text == null)
            {
                result.AddError(code, "Text command value is missing.", owner, index);
            }
        }
        else if (command.Kind == ChartRenderCommandKind.Marker)
        {
            ValidateFinitePair(command.X1, command.Y1, code, owner, index, result);
            if (!IsPositiveFinite(command.MarkerSize))
            {
                result.AddError(code, "Marker command size is invalid.", owner, index);
            }
        }
        else if (command.Kind == ChartRenderCommandKind.Arc)
        {
            ValidateFinitePair(command.X1, command.Y1, code, owner, index, result);
            if (!IsPositiveFinite(command.RadiusX) || !IsFinite(command.StartAngle) || !IsFinite(command.EndAngle) || command.StartAngle == command.EndAngle)
            {
                result.AddError(code, "Arc command geometry is invalid.", owner, index);
            }
        }
        else if (command.Kind == ChartRenderCommandKind.Wedge)
        {
            ValidateFinitePair(command.X1, command.Y1, code, owner, index, result);
            if (command.InnerRadius < 0.0 || !IsFinite(command.InnerRadius) || !IsPositiveFinite(command.RadiusX) || command.InnerRadius >= command.RadiusX || !IsFinite(command.StartAngle) || !IsFinite(command.EndAngle) || command.StartAngle == command.EndAngle)
            {
                result.AddError(code, "Wedge command geometry is invalid.", owner, index);
            }
        }
        else if (command.Kind == ChartRenderCommandKind.Image)
        {
            ValidateFinitePair(command.X1, command.Y1, code, owner, index, result);
            if (command.Image == null || !IsPositiveFinite(command.Width) || !IsPositiveFinite(command.Height))
            {
                result.AddError(code, "Image command data or size is invalid.", owner, index);
            }
        }
        else if (command.Kind == ChartRenderCommandKind.PushTransform)
        {
            if (command.Transform == null)
            {
                result.AddError(code, "Transform command value is missing.", owner, index);
            }
        }
        else if (command.Kind == ChartRenderCommandKind.PushClip)
        {
            if (command.ClipBounds == null || !IsPositiveFinite(command.ClipBounds.Width) || !IsPositiveFinite(command.ClipBounds.Height))
            {
                result.AddError(code, "Clip command bounds are invalid.", owner, index);
            }
        }
    }

    internal static void ValidatePoints(ChartPoint[] points, int minimumCount, string code, string owner, int commandIndex, ChartValidationResult result)
    {
        if (points == null)
        {
            result.AddError(code, "Render command points are missing.", owner, commandIndex);
            return;
        }

        if (points.Length < minimumCount)
        {
            result.AddError(code, "Render command has too few points.", owner, commandIndex);
        }

        int index = 0;
        while (index < points.Length)
        {
            if (points[index] == null)
            {
                result.AddError(code, "Render command contains a missing point.", owner, commandIndex);
            }
            else
            {
                ValidateFinitePair(points[index].X, points[index].Y, code, owner, commandIndex, result);
            }

            index = index + 1;
        }
    }

    internal static void ValidatePath(ChartPath path, string code, string owner, int commandIndex, ChartValidationResult result)
    {
        if (path == null)
        {
            result.AddError(code, "Path command value is missing.", owner, commandIndex);
            return;
        }

        int index = 0;
        while (index < path.Commands.Count)
        {
            ChartPathCommand command = path.Commands[index];
            if (command == null)
            {
                result.AddError(code, "Path contains a missing command.", owner, commandIndex);
            }
            else
            {
                int kind = (int)command.Kind;
                if (kind < 0 || kind > 4)
                {
                    result.AddError(code, "Path command kind is invalid.", owner, commandIndex);
                }

                ValidateFinitePair(command.X1, command.Y1, code, owner, commandIndex, result);
                ValidateFinitePair(command.X2, command.Y2, code, owner, commandIndex, result);
                ValidateFinitePair(command.X3, command.Y3, code, owner, commandIndex, result);
            }

            index = index + 1;
        }
    }

    internal static void ValidateFinitePair(double x, double y, string code, string owner, int index, ChartValidationResult result)
    {
        if (!IsFinite(x) || !IsFinite(y))
        {
            result.AddError(code, "Render coordinates must be finite.", owner, index);
        }
    }

    internal static void ValidateMargins(ChartPadding margins, double width, double height, ChartValidationResult result)
    {
        if (margins == null)
        {
            result.AddError(InvalidMarginsCode, "Margins are missing.", "margins", -1);
            return;
        }

        if (!IsMarginsFiniteNonNegative(margins))
        {
            result.AddError(InvalidMarginsCode, "Margins must be finite and non-negative.", "margins", -1);
            return;
        }

        if (IsFinite(width) && IsFinite(height) && (margins.Left + margins.Right >= width || margins.Top + margins.Bottom >= height))
        {
            result.AddWarning(InvalidMarginsCode, "Margins consume the available area.", "margins", -1);
        }
    }

    internal static bool IsMarginsFiniteNonNegative(ChartPadding margins)
    {
        if (margins == null)
        {
            return false;
        }

        return IsFinite(margins.Left) && IsFinite(margins.Top) && IsFinite(margins.Right) && IsFinite(margins.Bottom) && margins.Left >= 0.0 && margins.Top >= 0.0 && margins.Right >= 0.0 && margins.Bottom >= 0.0;
    }

    internal static bool IsDomainValid(ChartRange domain)
    {
        if (domain == null)
        {
            return false;
        }

        return IsFinite(domain.Minimum) && IsFinite(domain.Maximum) && domain.Minimum != domain.Maximum;
    }

    internal static bool IsPlotAreaValid(ChartRect rect)
    {
        if (rect == null)
        {
            return false;
        }

        return IsFinite(rect.X) && IsFinite(rect.Y) && IsPositiveFinite(rect.Width) && IsPositiveFinite(rect.Height);
    }

    internal static bool IsPositiveFinite(double value)
    {
        return IsFinite(value) && value > 0.0;
    }

    internal static bool IsFinite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
