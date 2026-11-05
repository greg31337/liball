namespace chart;
/// <summary>
/// Stores chart-wide choices and applies them to coordinates, scales, series, data ordering, missing values, rendering, and output.
/// </summary>
/// <remarks>Choices that require specialized inputs use the dedicated chart or scale APIs and report that requirement from the generic operations.</remarks>
public class ChartConfiguration : IChartConfiguration
{
    internal ChartCoordinateUnit CoordinateUnit;
    internal ChartDirection Direction;
    internal ChartMissingValuePolicy MissingValuePolicy;
    internal ChartOutputFormat OutputFormat;
    internal ChartRenderBackend RenderBackend;
    internal ChartScaleKind ScaleKind;
    internal ChartSeriesKind SeriesKind;
    internal ChartSortKind SortKind;
    internal ChartTextAlignment TextAlignment;
    internal ChartVerticalTextAlignment VerticalTextAlignment;
    internal ChartScalePolicy ScalePolicy;

    /// <summary>Initializes a configuration with data coordinates, normal direction, gap handling, linear scaling, line series, and raster PNG output.</summary>
    public ChartConfiguration()
    {
        CoordinateUnit = ChartCoordinateUnit.Data;
        Direction = ChartDirection.Normal;
        MissingValuePolicy = ChartMissingValuePolicy.Gap;
        OutputFormat = ChartOutputFormat.Png;
        RenderBackend = ChartRenderBackend.Raster;
        ScaleKind = ChartScaleKind.Linear;
        SeriesKind = ChartSeriesKind.Line;
        SortKind = ChartSortKind.None;
        TextAlignment = ChartTextAlignment.Left;
        VerticalTextAlignment = ChartVerticalTextAlignment.Baseline;
        ScalePolicy = new ChartScalePolicy();
    }

    /// <summary>Returns the coordinate unit used to interpret positions and dimensions.</summary>
    /// <returns>The configured coordinate unit.</returns>
    public ChartCoordinateUnit GetCoordinateUnit()
    {
        return CoordinateUnit;
    }

    /// <summary>Returns the direction used when mapping ordered values.</summary>
    /// <returns>The configured direction.</returns>
    public ChartDirection GetDirection()
    {
        return Direction;
    }

    /// <summary>Returns the policy used to process missing values.</summary>
    /// <returns>The configured missing value policy.</returns>
    public ChartMissingValuePolicy GetMissingValuePolicy()
    {
        return MissingValuePolicy;
    }

    /// <summary>Returns the requested output format.</summary>
    /// <returns>The configured output format.</returns>
    public ChartOutputFormat GetOutputFormat()
    {
        return OutputFormat;
    }

    /// <summary>Returns the backend used to render chart output.</summary>
    /// <returns>The configured render backend.</returns>
    public ChartRenderBackend GetRenderBackend()
    {
        return RenderBackend;
    }

    /// <summary>Returns the scale kind used to map data values.</summary>
    /// <returns>The configured scale kind.</returns>
    public ChartScaleKind GetScaleKind()
    {
        return ScaleKind;
    }

    /// <summary>Returns an independent copy of the scale policy.</summary>
    /// <returns>The configured scale policy.</returns>
    public ChartScalePolicy GetScalePolicy()
    {
        return ScalePolicy.Copy();
    }

    /// <summary>Returns the kind of series represented by the configuration.</summary>
    /// <returns>The configured series kind.</returns>
    public ChartSeriesKind GetSeriesKind()
    {
        return SeriesKind;
    }

    /// <summary>Returns the ordering applied to chart values.</summary>
    /// <returns>The configured sort kind.</returns>
    public ChartSortKind GetSortKind()
    {
        return SortKind;
    }

    /// <summary>Returns the horizontal alignment used for text.</summary>
    /// <returns>The configured text alignment.</returns>
    public ChartTextAlignment GetTextAlignment()
    {
        return TextAlignment;
    }

    /// <summary>Returns the vertical alignment used for text.</summary>
    /// <returns>The configured vertical text alignment.</returns>
    public ChartVerticalTextAlignment GetVerticalTextAlignment()
    {
        return VerticalTextAlignment;
    }

    /// <summary>Sets the coordinate unit used to interpret positions and dimensions.</summary>
    /// <param name = "value">The configured coordinate unit.</param>
    public void SetCoordinateUnit(ChartCoordinateUnit value)
    {
        ValidateEnum((int)value, 5, "value");
        CoordinateUnit = value;
    }

    /// <summary>Sets the direction used when mapping ordered values.</summary>
    /// <param name = "value">The configured direction.</param>
    public void SetDirection(ChartDirection value)
    {
        ValidateEnum((int)value, 1, "value");
        Direction = value;
    }

    /// <summary>Sets the policy used to process missing values.</summary>
    /// <param name = "value">The configured missing value policy.</param>
    public void SetMissingValuePolicy(ChartMissingValuePolicy value)
    {
        ValidateEnum((int)value, 4, "value");
        MissingValuePolicy = value;
    }

    /// <summary>Sets the requested output format.</summary>
    /// <param name = "value">The configured output format.</param>
    public void SetOutputFormat(ChartOutputFormat value)
    {
        ValidateEnum((int)value, 3, "value");
        OutputFormat = value;
    }

    /// <summary>Sets the backend used to render chart output.</summary>
    /// <param name = "value">The configured render backend.</param>
    public void SetRenderBackend(ChartRenderBackend value)
    {
        ValidateEnum((int)value, 1, "value");
        RenderBackend = value;
    }

    /// <summary>Sets the scale kind used to map data values.</summary>
    /// <param name = "value">The configured scale kind.</param>
    public void SetScaleKind(ChartScaleKind value)
    {
        ValidateEnum((int)value, 19, "value");
        ScaleKind = value;
    }

    /// <summary>Sets the policies used for domain resolution and numeric scale mapping.</summary>
    /// <param name = "policy">The scale policy.</param>
    public void SetScalePolicy(ChartScalePolicy policy)
    {
        if (policy == null)
        {
            throw new System.ArgumentNullException("policy");
        }

        ScalePolicy = policy.Copy();
    }

    /// <summary>Resolves a domain using the configured scale policy.</summary>
    /// <param name = "values">The values used for automatic domain calculation.</param>
    /// <param name = "explicitDomain">The explicit domain used when automatic calculation is disabled.</param>
    /// <returns>The resolved scale domain.</returns>
    public ChartRange ResolveScaleDomain(double[] values, ChartRange? explicitDomain)
    {
        return ScalePolicy.ResolveDomain(values, explicitDomain);
    }

    /// <summary>Resolves scale domains for independent, shared, or grouped subplots.</summary>
    /// <param name = "subplotValues">The values for each subplot.</param>
    /// <param name = "sharingGroups">The sharing-group identifier for each subplot.</param>
    /// <returns>One resolved domain per subplot.</returns>
    public ChartRange[] ResolveSubplotScaleDomains(double[][] subplotValues, string[] sharingGroups)
    {
        return ScalePolicy.ResolveSubplotDomains(subplotValues, sharingGroups);
    }

    /// <summary>Resolves scale domains using sharing groups configured on plots.</summary>
    /// <param name = "plots">The subplots whose sharing groups are used.</param>
    /// <param name = "subplotValues">The values for each subplot.</param>
    /// <param name = "axisKind">The horizontal or vertical axis whose group is used.</param>
    /// <returns>One resolved domain per subplot.</returns>
    public ChartRange[] ResolvePlotScaleDomains(ChartPlot[] plots, double[][] subplotValues, ChartAxisKind axisKind)
    {
        return ScalePolicy.ResolvePlotDomains(plots, subplotValues, axisKind);
    }

    /// <summary>Sets the kind of series represented by the configuration.</summary>
    /// <param name = "value">The configured series kind.</param>
    public void SetSeriesKind(ChartSeriesKind value)
    {
        ValidateEnum((int)value, 31, "value");
        SeriesKind = value;
    }

    /// <summary>Sets the ordering applied to chart values.</summary>
    /// <param name = "value">The configured sort kind.</param>
    public void SetSortKind(ChartSortKind value)
    {
        ValidateEnum((int)value, 6, "value");
        SortKind = value;
    }

    /// <summary>Sets the horizontal alignment used for text.</summary>
    /// <param name = "value">The configured text alignment.</param>
    public void SetTextAlignment(ChartTextAlignment value)
    {
        ValidateEnum((int)value, 5, "value");
        TextAlignment = value;
    }

    /// <summary>Sets the vertical alignment used for text.</summary>
    /// <param name = "value">The configured vertical text alignment.</param>
    public void SetVerticalTextAlignment(ChartVerticalTextAlignment value)
    {
        ValidateEnum((int)value, 3, "value");
        VerticalTextAlignment = value;
    }

    /// <summary>Maps a numeric value with the configured scale and direction.</summary>
    /// <param name = "value">The data value to map.</param>
    /// <param name = "domainMinimum">The lower end of the input domain.</param>
    /// <param name = "domainMaximum">The upper end of the input domain.</param>
    /// <param name = "rangeMinimum">The lower end of the output range.</param>
    /// <param name = "rangeMaximum">The upper end of the output range.</param>
    /// <returns>The mapped value.</returns>
    /// <exception cref = "System.InvalidOperationException">Thrown when the configured scale requires non-numeric or specialized inputs.</exception>
    public double MapNumericValue(double value, double domainMinimum, double domainMaximum, double rangeMinimum, double rangeMaximum)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            if (ScalePolicy.HasUnknownValue)
            {
                return ScalePolicy.UnknownValue;
            }

            throw new System.ArgumentOutOfRangeException("value");
        }

        double mappedValue = value;
        if (ScalePolicy.Clamp)
        {
            double lower = System.Math.Min(domainMinimum, domainMaximum);
            double upper = System.Math.Max(domainMinimum, domainMaximum);
            if (mappedValue < lower)
            {
                mappedValue = lower;
            }

            if (mappedValue > upper)
            {
                mappedValue = upper;
            }
        }

        double outputMinimum = rangeMinimum;
        double outputMaximum = rangeMaximum;
        bool reverse = Direction == ChartDirection.Reversed;
        if (ScalePolicy.Reverse)
        {
            reverse = !reverse;
        }

        if (reverse)
        {
            outputMinimum = rangeMaximum;
            outputMaximum = rangeMinimum;
        }

        if (ScaleKind == ChartScaleKind.Log)
        {
            ChartLogScale scale = new ChartLogScale(domainMinimum, domainMaximum, outputMinimum, outputMaximum, 10.0);
            return scale.Map(mappedValue);
        }

        if (ScaleKind == ChartScaleKind.SymmetricLog)
        {
            ChartSymmetricLogScale scale = new ChartSymmetricLogScale(domainMinimum, domainMaximum, outputMinimum, outputMaximum, 1.0);
            return scale.Map(mappedValue);
        }

        if (ScaleKind == ChartScaleKind.Power)
        {
            ChartPowerScale scale = new ChartPowerScale(domainMinimum, domainMaximum, outputMinimum, outputMaximum, 2.0);
            return scale.Map(mappedValue);
        }

        if (ScaleKind == ChartScaleKind.SquareRoot)
        {
            ChartSquareRootScale scale = new ChartSquareRootScale(domainMinimum, domainMaximum, outputMinimum, outputMaximum);
            return scale.Map(mappedValue);
        }

        if (ScaleKind == ChartScaleKind.Size)
        {
            ChartSizeScale scale = new ChartSizeScale(domainMinimum, domainMaximum, outputMinimum, outputMaximum);
            return scale.Map(mappedValue);
        }

        if (ScaleKind == ChartScaleKind.Diverging)
        {
            ChartDivergingScale scale = new ChartDivergingScale(domainMinimum, (domainMinimum + domainMaximum) / 2.0, domainMaximum, outputMinimum, (outputMinimum + outputMaximum) / 2.0, outputMaximum);
            return scale.Map(mappedValue);
        }

        if (ScaleKind == ChartScaleKind.Radial)
        {
            ChartRadialScale scale = new ChartRadialScale(domainMinimum, domainMaximum, outputMinimum, outputMaximum);
            return scale.Map(mappedValue);
        }

        if (ScaleKind == ChartScaleKind.Angle)
        {
            ChartAngleScale scale = new ChartAngleScale(domainMinimum, domainMaximum, outputMinimum, outputMaximum);
            return scale.Map(mappedValue);
        }

        if (ScaleKind == ChartScaleKind.Opacity)
        {
            ChartOpacityScale scale = new ChartOpacityScale(domainMinimum, domainMaximum, outputMinimum, outputMaximum);
            return scale.Map(mappedValue);
        }

        if (ScaleKind == ChartScaleKind.Linear || ScaleKind == ChartScaleKind.Time || ScaleKind == ChartScaleKind.UtcTime || ScaleKind == ChartScaleKind.Color)
        {
            ChartLinearScale scale = new ChartLinearScale(domainMinimum, domainMaximum, outputMinimum, outputMaximum);
            return scale.Map(mappedValue);
        }

        throw new System.InvalidOperationException("The configured scale requires categories, thresholds, or another specialized scale API.");
    }

    /// <summary>Maps a point expressed in the configured coordinate unit into a reference rectangle.</summary>
    /// <param name = "point">The point to map.</param>
    /// <param name = "bounds">The reference rectangle for plot, paper, axis, and fraction coordinates.</param>
    /// <param name = "xDomain">The horizontal data domain.</param>
    /// <param name = "yDomain">The vertical data domain.</param>
    /// <returns>The mapped point.</returns>
    public ChartPoint MapPoint(ChartPoint point, ChartRect bounds, ChartRange xDomain, ChartRange yDomain)
    {
        ValidatePointInputs(point, bounds, xDomain, yDomain);
        if (CoordinateUnit == ChartCoordinateUnit.Pixel)
        {
            return point.Copy();
        }

        if (CoordinateUnit == ChartCoordinateUnit.Plot)
        {
            return new ChartPoint(bounds.X + point.X, bounds.Y + point.Y);
        }

        if (CoordinateUnit == ChartCoordinateUnit.Data)
        {
            double x = MapNumericValue(point.X, xDomain.Minimum, xDomain.Maximum, bounds.Left(), bounds.Right());
            double y = MapNumericValue(point.Y, yDomain.Minimum, yDomain.Maximum, bounds.Bottom(), bounds.Top());
            return new ChartPoint(x, y);
        }

        double amountX = point.X;
        double amountY = point.Y;
        if (Direction == ChartDirection.Reversed)
        {
            amountX = 1.0 - amountX;
            amountY = 1.0 - amountY;
        }

        return new ChartPoint(bounds.X + amountX * bounds.Width, bounds.Y + amountY * bounds.Height);
    }

    /// <summary>Returns the top-left text origin for an anchor and measured text size.</summary>
    /// <param name = "anchor">The alignment anchor.</param>
    /// <param name = "measuredSize">The measured text size.</param>
    /// <returns>The aligned text origin.</returns>
    public ChartPoint AlignText(ChartPoint anchor, ChartSize measuredSize)
    {
        if (anchor == null)
        {
            throw new System.ArgumentNullException("anchor");
        }

        if (measuredSize == null)
        {
            throw new System.ArgumentNullException("measuredSize");
        }

        if (measuredSize.Width < 0.0 || measuredSize.Height < 0.0)
        {
            throw new System.ArgumentOutOfRangeException("measuredSize");
        }

        double x = anchor.X;
        if (TextAlignment == ChartTextAlignment.Center || TextAlignment == ChartTextAlignment.Middle)
        {
            x = x - measuredSize.Width * 0.5;
        }
        else if (TextAlignment == ChartTextAlignment.Right || TextAlignment == ChartTextAlignment.End)
        {
            x = x - measuredSize.Width;
        }

        double y = anchor.Y;
        if (VerticalTextAlignment == ChartVerticalTextAlignment.Middle)
        {
            y = y - measuredSize.Height * 0.5;
        }
        else if (VerticalTextAlignment == ChartVerticalTextAlignment.Baseline || VerticalTextAlignment == ChartVerticalTextAlignment.Bottom)
        {
            y = y - measuredSize.Height;
        }

        return new ChartPoint(x, y);
    }

    /// <summary>Processes non-finite numeric values using the configured missing-value policy.</summary>
    /// <param name = "values">The values to process.</param>
    /// <returns>A new array containing the resolved values.</returns>
    public double[] ResolveMissingValues(double[] values)
    {
        return ChartMissingValues.ResolveArray(values, MissingValuePolicy);
    }

    /// <summary>Sorts a column according to the configured sort kind.</summary>
    /// <param name = "column">The column whose values determine ordering.</param>
    /// <param name = "ascending"><c>true</c> for ascending order; otherwise descending.</param>
    /// <returns>The ordered row indices.</returns>
    public int[] SortIndices(ChartDataColumn column, bool ascending)
    {
        if (column == null)
        {
            throw new System.ArgumentNullException("column");
        }

        if (SortKind == ChartSortKind.None)
        {
            return SequentialIndices(column.Length);
        }

        if (SortKind == ChartSortKind.Custom)
        {
            throw new System.InvalidOperationException("Custom sorting requires an explicit custom-order data operation.");
        }

        if (SortKind == ChartSortKind.Category || SortKind == ChartSortKind.SeriesName)
        {
            return ChartDataOperationsOperations.SortIndicesByText(column, ascending);
        }

        return ChartDataOperationsOperations.SortIndicesByNumber(column, ascending);
    }

    /// <summary>Creates a cartesian series using the configured series kind.</summary>
    /// <param name = "id">A stable series identifier.</param>
    /// <param name = "xValues">The horizontal data values.</param>
    /// <param name = "yValues">The vertical data values.</param>
    /// <param name = "baseline">The baseline used by area and bar series.</param>
    /// <param name = "itemSize">The marker size or bar width.</param>
    /// <param name = "style">The render style.</param>
    /// <returns>The configured cartesian series.</returns>
    public ChartCartesianSeries CreateCartesianSeries(string id, double[] xValues, double[] yValues, double baseline, double itemSize, ChartRenderStyle style)
    {
        if (SeriesKind == ChartSeriesKind.Line)
        {
            return ChartMissingValues.CreatePolicyLineSeries(id, xValues, yValues, MissingValuePolicy, ChartLineInterpolation.Linear, 1, style);
        }

        if (SeriesKind == ChartSeriesKind.StepLine)
        {
            return ChartMissingValues.CreatePolicyLineSeries(id, xValues, yValues, MissingValuePolicy, ChartLineInterpolation.StepMiddle, 1, style);
        }

        if (SeriesKind == ChartSeriesKind.SplineLine)
        {
            return ChartMissingValues.CreatePolicyLineSeries(id, xValues, yValues, MissingValuePolicy, ChartLineInterpolation.Spline, 8, style);
        }

        if (SeriesKind == ChartSeriesKind.Area)
        {
            return ChartMissingValues.CreatePolicyAreaSeries(id, xValues, yValues, MissingValuePolicy, baseline, style);
        }

        if (SeriesKind == ChartSeriesKind.Scatter)
        {
            return ChartMissingValues.CreatePolicyScatterSeries(id, xValues, yValues, MissingValuePolicy, ChartMarkerShape.Circle, itemSize, style);
        }

        if (SeriesKind == ChartSeriesKind.Bar)
        {
            return ChartMissingValues.CreatePolicyVerticalBarSeries(id, xValues, yValues, MissingValuePolicy, itemSize, baseline, style);
        }

        throw new System.InvalidOperationException("The configured series kind requires a specialized chart builder and additional data.");
    }

    /// <summary>Renders and encodes a retained scene using the configured backend and output format.</summary>
    /// <param name = "scene">The retained scene to render.</param>
    /// <param name = "accessibility">The accessibility metadata used by SVG output.</param>
    /// <returns>The encoded output bytes.</returns>
    public byte[] ExportScene(ChartRenderScene scene, ChartAccessibilityMetadata accessibility)
    {
        if (scene == null)
        {
            throw new System.ArgumentNullException("scene");
        }

        if (accessibility == null)
        {
            throw new System.ArgumentNullException("accessibility");
        }

        if (RenderBackend == ChartRenderBackend.Svg)
        {
            if (OutputFormat != ChartOutputFormat.Svg)
            {
                throw new System.InvalidOperationException("The SVG backend requires SVG output.");
            }

            string svg = ChartExportOperations.ToSvg(scene, accessibility);
            return ChartExportOperations.ToSvgBytes(svg);
        }

        if (OutputFormat == ChartOutputFormat.Svg)
        {
            throw new System.InvalidOperationException("SVG output requires the SVG backend.");
        }

        RawBitmap bitmap = scene.ToRawBitmap();
        if (OutputFormat == ChartOutputFormat.RawBitmap)
        {
            return ChartExportOperations.ToRawBytes(bitmap);
        }

        if (OutputFormat == ChartOutputFormat.Bmp)
        {
            return ChartExportOperations.ToBmpBytes(bitmap);
        }

        return ChartExportOperations.ToPngBytes(bitmap);
    }

    /// <summary>Creates a deep copy that can be modified independently.</summary>
    /// <returns>A new configuration independent of this instance.</returns>
    public ChartConfiguration Copy()
    {
        ChartConfiguration copy = new ChartConfiguration();
        copy.CoordinateUnit = CoordinateUnit;
        copy.Direction = Direction;
        copy.MissingValuePolicy = MissingValuePolicy;
        copy.OutputFormat = OutputFormat;
        copy.RenderBackend = RenderBackend;
        copy.ScaleKind = ScaleKind;
        copy.SeriesKind = SeriesKind;
        copy.SortKind = SortKind;
        copy.TextAlignment = TextAlignment;
        copy.VerticalTextAlignment = VerticalTextAlignment;
        copy.ScalePolicy = ScalePolicy.Copy();
        return copy;
    }

    internal static void ValidateEnum(int value, int maximum, string name)
    {
        if (value < 0 || value > maximum)
        {
            throw new System.ArgumentOutOfRangeException(name);
        }
    }

    internal static void ValidatePointInputs(ChartPoint point, ChartRect bounds, ChartRange xDomain, ChartRange yDomain)
    {
        if (point == null)
        {
            throw new System.ArgumentNullException("point");
        }

        if (bounds == null)
        {
            throw new System.ArgumentNullException("bounds");
        }

        if (xDomain == null)
        {
            throw new System.ArgumentNullException("xDomain");
        }

        if (yDomain == null)
        {
            throw new System.ArgumentNullException("yDomain");
        }

        if (bounds.IsEmpty() || !xDomain.IsValid() || !yDomain.IsValid() || xDomain.Length() == 0.0 || yDomain.Length() == 0.0)
        {
            throw new System.ArgumentException("Coordinate bounds and domains must be non-empty and finite.");
        }
    }

    internal static bool IsMissing(double value)
    {
        return double.IsNaN(value) || double.IsInfinity(value);
    }

    internal static double[] RemoveMissingValues(double[] values)
    {
        double[] buffer = new double[values.Length];
        int count = 0;
        int index = 0;
        while (index < values.Length)
        {
            if (!IsMissing(values[index]))
            {
                buffer[count] = values[index];
                count = count + 1;
            }

            index = index + 1;
        }

        double[] result = new double[count];
        System.Array.Copy(buffer, result, count);
        return result;
    }

    internal static void ReplaceMissingWithZero(double[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (IsMissing(values[index]))
            {
                values[index] = 0.0;
            }

            index = index + 1;
        }
    }

    internal static void CarryMissingValues(double[] values)
    {
        double previous = 0.0;
        bool hasPrevious = false;
        int index = 0;
        while (index < values.Length)
        {
            if (IsMissing(values[index]))
            {
                if (hasPrevious)
                {
                    values[index] = previous;
                }
                else
                {
                    values[index] = 0.0;
                }
            }
            else
            {
                previous = values[index];
                hasPrevious = true;
            }

            index = index + 1;
        }
    }

    internal static void InterpolateMissingValues(double[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (!IsMissing(values[index]))
            {
                index = index + 1;
                continue;
            }

            int start = index;
            while (index < values.Length && IsMissing(values[index]))
            {
                index = index + 1;
            }

            int end = index;
            double left = start > 0 ? values[start - 1] : 0.0;
            double right = end < values.Length ? values[end] : left;
            if (start == 0 && end < values.Length)
            {
                left = right;
            }

            int fill = start;
            while (fill < end)
            {
                double amount = (fill - start + 1.0) / (end - start + 1.0);
                values[fill] = left + (right - left) * amount;
                fill = fill + 1;
            }
        }
    }

    internal static int[] SequentialIndices(int count)
    {
        int[] indices = new int[count];
        int index = 0;
        while (index < count)
        {
            indices[index] = index;
            index = index + 1;
        }

        return indices;
    }
}
