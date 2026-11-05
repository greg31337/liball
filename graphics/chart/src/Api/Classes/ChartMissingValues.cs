namespace chart;
/// <summary>Applies missing-value policies consistently across chart data and builders.</summary>
public class ChartMissingValues : IChartMissingValues
{
    /// <summary>Initializes a stateless missing-value processing facade.</summary>
    public ChartMissingValues()
    {
    }

    /// <summary>Resolves one numeric array with the requested policy.</summary>
    /// <param name = "values">The numeric values.</param>
    /// <param name = "policy">The missing-value policy.</param>
    /// <returns>The resolved values.</returns>
    public double[] ResolveValues(double[] values, ChartMissingValuePolicy policy)
    {
        return ResolveArray(values, policy);
    }

    /// <summary>Resolves aligned x and y arrays while preserving row alignment.</summary>
    /// <param name = "xValues">The horizontal values.</param>
    /// <param name = "yValues">The vertical values.</param>
    /// <param name = "policy">The missing-value policy.</param>
    /// <returns>The resolved points.</returns>
    public ChartPoint[] ResolvePoints(double[] xValues, double[] yValues, ChartMissingValuePolicy policy)
    {
        return ResolvePointArray(xValues, yValues, policy);
    }

    /// <summary>Resolves and maps aligned coordinates through a cartesian transform.</summary>
    /// <param name = "transform">The cartesian transform.</param>
    /// <param name = "xValues">The horizontal values.</param>
    /// <param name = "yValues">The vertical values.</param>
    /// <param name = "policy">The missing-value policy.</param>
    /// <returns>The transformed points.</returns>
    public ChartPoint[] TransformPoints(ChartCartesianTransform transform, double[] xValues, double[] yValues, ChartMissingValuePolicy policy)
    {
        if (transform == null)
        {
            throw new System.ArgumentNullException("transform");
        }

        ChartPoint[] points = ResolvePointArray(xValues, yValues, policy);
        ChartPoint[] result = new ChartPoint[points.Length];
        int index = 0;
        while (index < points.Length)
        {
            if (IsMissing(points[index].X) || IsMissing(points[index].Y))
            {
                result[index] = new ChartPoint(double.NaN, double.NaN);
            }
            else
            {
                result[index] = transform.MapPoint(points[index].X, points[index].Y);
            }

            index = index + 1;
        }

        return result;
    }

    /// <summary>Resolves every numeric column in a table.</summary>
    /// <param name = "table">The source table.</param>
    /// <param name = "policy">The missing-value policy.</param>
    /// <returns>A new resolved table.</returns>
    public ChartDataTable ResolveTable(ChartDataTable table, ChartMissingValuePolicy policy)
    {
        return ResolveDataTable(table, policy);
    }

    /// <summary>Computes a finite domain after applying a missing-value policy.</summary>
    /// <param name = "values">The numeric values.</param>
    /// <param name = "policy">The missing-value policy.</param>
    /// <returns>The resolved finite domain.</returns>
    public ChartRange ResolveDomain(double[] values, ChartMissingValuePolicy policy)
    {
        return ResolveValueDomain(values, policy);
    }

    /// <summary>Creates a policy-aware line series.</summary>
    /// <param name = "id">The series identifier.</param>
    /// <param name = "xValues">The horizontal values.</param>
    /// <param name = "yValues">The vertical values.</param>
    /// <param name = "policy">The missing-value policy.</param>
    /// <param name = "style">The rendering style.</param>
    /// <returns>The resulting series.</returns>
    public ChartCartesianSeries CreateLineSeries(string id, double[] xValues, double[] yValues, ChartMissingValuePolicy policy, ChartRenderStyle style)
    {
        return CreatePolicyLineSeries(id, xValues, yValues, policy, ChartLineInterpolation.Linear, 1, style);
    }

    /// <summary>Creates a policy-aware scatter series.</summary>
    /// <param name = "id">The series identifier.</param>
    /// <param name = "xValues">The horizontal values.</param>
    /// <param name = "yValues">The vertical values.</param>
    /// <param name = "policy">The missing-value policy.</param>
    /// <param name = "shape">The marker shape.</param>
    /// <param name = "markerSize">The marker size.</param>
    /// <param name = "style">The rendering style.</param>
    /// <returns>The resulting series.</returns>
    public ChartCartesianSeries CreateScatterSeries(string id, double[] xValues, double[] yValues, ChartMissingValuePolicy policy, ChartMarkerShape shape, double markerSize, ChartRenderStyle style)
    {
        return CreatePolicyScatterSeries(id, xValues, yValues, policy, shape, markerSize, style);
    }

    /// <summary>Creates a policy-aware vertical bar series.</summary>
    /// <param name = "id">The series identifier.</param>
    /// <param name = "xValues">The horizontal values.</param>
    /// <param name = "yValues">The vertical values.</param>
    /// <param name = "policy">The missing-value policy.</param>
    /// <param name = "barWidth">The bar width.</param>
    /// <param name = "baseline">The baseline.</param>
    /// <param name = "style">The rendering style.</param>
    /// <returns>The resulting series.</returns>
    public ChartCartesianSeries CreateVerticalBarSeries(string id, double[] xValues, double[] yValues, ChartMissingValuePolicy policy, double barWidth, double baseline, ChartRenderStyle style)
    {
        return CreatePolicyVerticalBarSeries(id, xValues, yValues, policy, barWidth, baseline, style);
    }

    internal static double[] ResolveArray(double[] values, ChartMissingValuePolicy policy)
    {
        if (values == null)
        {
            throw new System.ArgumentNullException("values");
        }

        ValidatePolicy(policy);
        if (policy == ChartMissingValuePolicy.Skip)
        {
            return RemoveMissing(values);
        }

        double[] result = ChartDataMatrix.CopyDoubleArray(values);
        if (policy == ChartMissingValuePolicy.Gap)
        {
            return result;
        }

        if (policy == ChartMissingValuePolicy.Zero)
        {
            FillZero(result);
        }
        else if (policy == ChartMissingValuePolicy.CarryForward)
        {
            FillCarryForward(result);
        }
        else
        {
            FillInterpolation(result);
        }

        return result;
    }

    internal static ChartPoint[] ResolvePointArray(double[] xValues, double[] yValues, ChartMissingValuePolicy policy)
    {
        ValidatePairShape(xValues, yValues);
        ValidatePolicy(policy);
        if (policy == ChartMissingValuePolicy.Skip)
        {
            return RemoveMissingRows(xValues, yValues);
        }

        if (policy == ChartMissingValuePolicy.Gap)
        {
            ChartPoint[] gaps = new ChartPoint[xValues.Length];
            int gapIndex = 0;
            while (gapIndex < xValues.Length)
            {
                if (IsMissing(xValues[gapIndex]) || IsMissing(yValues[gapIndex]))
                {
                    gaps[gapIndex] = new ChartPoint(double.NaN, double.NaN);
                }
                else
                {
                    gaps[gapIndex] = new ChartPoint(xValues[gapIndex], yValues[gapIndex]);
                }

                gapIndex = gapIndex + 1;
            }

            return gaps;
        }

        double[] resolvedX = ResolveArray(xValues, policy);
        double[] resolvedY = ResolveArray(yValues, policy);
        return BuildPoints(resolvedX, resolvedY);
    }

    internal static ChartPoint[] ResolveBuilderPoints(double[] xValues, double[] yValues, ChartMissingValuePolicy policy)
    {
        if (policy == ChartMissingValuePolicy.Gap)
        {
            return ResolvePointArray(xValues, yValues, ChartMissingValuePolicy.Skip);
        }

        return ResolvePointArray(xValues, yValues, policy);
    }

    internal static ChartDataTable ResolveDataTable(ChartDataTable table, ChartMissingValuePolicy policy)
    {
        if (table == null)
        {
            throw new System.ArgumentNullException("table");
        }

        ValidatePolicy(policy);
        if (policy == ChartMissingValuePolicy.Skip)
        {
            return ResolveSkippedTable(table);
        }

        ChartDataTable result = new ChartDataTable();
        int columnIndex = 0;
        while (columnIndex < table.ColumnCount)
        {
            ChartDataColumn column = table.Columns[columnIndex];
            if (column.Kind == ChartDataKind.Number)
            {
                result.AddNumberColumn(column.Name, ResolveArray(column.NumberValues, policy));
            }
            else if (column.Kind == ChartDataKind.Text)
            {
                result.AddTextColumn(column.Name, column.CopyTextValues());
            }
            else if (column.Kind == ChartDataKind.DateTime)
            {
                result.AddDateTimeColumn(column.Name, column.CopyDateTimeValues());
            }
            else if (column.Kind == ChartDataKind.Category)
            {
                result.AddCategoryColumn(column.Name, column.CopyCategoryValues());
            }
            else
            {
                ChartDataMatrix matrix = column.GetMatrix();
                double[] resolved = ResolveArray(matrix.CopyValues(), policy);
                result.AddMatrixColumn(column.Name, ChartDataMatrix.FromValues(matrix.Rows, matrix.Columns, resolved));
            }

            columnIndex = columnIndex + 1;
        }

        return result;
    }

    internal static ChartDataTable ResolveSkippedTable(ChartDataTable table)
    {
        int matrixIndex = FindMatrixColumn(table);
        if (matrixIndex >= 0)
        {
            if (table.ColumnCount != 1)
            {
                throw new System.ArgumentException("Skip policy cannot align a matrix column with other table columns.");
            }

            ChartDataMatrix matrix = table.Columns[matrixIndex].GetMatrix();
            double[] compact = RemoveMissing(matrix.CopyValues());
            ChartDataTable matrixTable = new ChartDataTable();
            if (compact.Length > 0)
            {
                matrixTable.AddMatrixColumn(table.Columns[matrixIndex].Name, ChartDataMatrix.FromValues(1, compact.Length, compact));
            }

            return matrixTable;
        }

        bool[] keep = new bool[table.RowCount];
        int keptCount = 0;
        int row = 0;
        while (row < table.RowCount)
        {
            bool rowIsValid = true;
            int columnIndex = 0;
            while (columnIndex < table.ColumnCount)
            {
                ChartDataColumn column = table.Columns[columnIndex];
                if (column.Kind == ChartDataKind.Number && IsMissing(column.NumberValues[row]))
                {
                    rowIsValid = false;
                }

                columnIndex = columnIndex + 1;
            }

            keep[row] = rowIsValid;
            if (rowIsValid)
            {
                keptCount = keptCount + 1;
            }

            row = row + 1;
        }

        ChartDataTable result = new ChartDataTable();
        int sourceColumn = 0;
        while (sourceColumn < table.ColumnCount)
        {
            ChartDataColumn column = table.Columns[sourceColumn];
            if (column.Kind == ChartDataKind.Number)
            {
                result.AddNumberColumn(column.Name, SelectNumbers(column.NumberValues, keep, keptCount));
            }
            else if (column.Kind == ChartDataKind.Text)
            {
                result.AddTextColumn(column.Name, SelectText(column.TextValues, keep, keptCount));
            }
            else if (column.Kind == ChartDataKind.DateTime)
            {
                result.AddDateTimeColumn(column.Name, SelectDates(column.DateTimeValues, keep, keptCount));
            }
            else
            {
                result.AddCategoryColumn(column.Name, SelectText(column.CategoryValues, keep, keptCount));
            }

            sourceColumn = sourceColumn + 1;
        }

        return result;
    }

    internal static ChartRange ResolveValueDomain(double[] values, ChartMissingValuePolicy policy)
    {
        double[] resolved = ResolveArray(values, policy);
        bool found = false;
        double minimum = 0.0;
        double maximum = 0.0;
        int index = 0;
        while (index < resolved.Length)
        {
            if (!IsMissing(resolved[index]))
            {
                if (!found)
                {
                    minimum = resolved[index];
                    maximum = resolved[index];
                    found = true;
                }
                else
                {
                    if (resolved[index] < minimum)
                    {
                        minimum = resolved[index];
                    }

                    if (resolved[index] > maximum)
                    {
                        maximum = resolved[index];
                    }
                }
            }

            index = index + 1;
        }

        if (!found)
        {
            throw new System.ArgumentException("Missing-value processing did not leave a finite domain.");
        }

        return new ChartRange(minimum, maximum);
    }

    internal static ChartCartesianSeries CreatePolicyLineSeries(string id, double[] xValues, double[] yValues, ChartMissingValuePolicy policy, ChartLineInterpolation interpolation, int samplesPerSegment, ChartRenderStyle style)
    {
        ValidatePairShape(xValues, yValues);
        ValidatePolicy(policy);
        if (policy != ChartMissingValuePolicy.Gap)
        {
            ChartPoint[] points = ResolvePointArray(xValues, yValues, policy);
            if (policy == ChartMissingValuePolicy.Skip && points.Length < 2)
            {
                ChartPlot.ValidateId(id);
                ChartLineAreaChartsOperations.ValidateInterpolation(interpolation);
                ChartLineAreaChartsOperations.ValidateStyle(style);
                return new ChartCartesianSeries(id, ChartCartesianSeriesKind.Line);
            }

            return ChartLineAreaChartsOperations.CreateLineSeries(id, PointXValues(points), PointYValues(points), interpolation, samplesPerSegment, style);
        }

        ChartPlot.ValidateId(id);
        ChartLineAreaChartsOperations.ValidateInterpolation(interpolation);
        ChartLineAreaChartsOperations.ValidateStyle(style);
        ChartCartesianSeries series = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Line);
        int start = 0;
        int segment = 0;
        while (start < xValues.Length)
        {
            while (start < xValues.Length && (IsMissing(xValues[start]) || IsMissing(yValues[start])))
            {
                start = start + 1;
            }

            int end = start;
            while (end < xValues.Length && !IsMissing(xValues[end]) && !IsMissing(yValues[end]))
            {
                end = end + 1;
            }

            if (end - start >= 2)
            {
                double[] segmentX = CopyNumberRange(xValues, start, end);
                double[] segmentY = CopyNumberRange(yValues, start, end);
                ChartPoint[] linePoints = ChartLineAreaChartsOperations.CreateLinePoints(segmentX, segmentY, interpolation, samplesPerSegment);
                string suffix = segment.ToString(System.Globalization.CultureInfo.InvariantCulture);
                series.AddCommand(ChartRenderCommand.CreatePolyline(id + "-line-" + suffix, linePoints, style));
                segment = segment + 1;
            }

            start = end + 1;
        }

        return series;
    }

    internal static ChartCartesianSeries CreatePolicyAreaSeries(string id, double[] xValues, double[] yValues, ChartMissingValuePolicy policy, double baseline, ChartRenderStyle style)
    {
        ValidatePairShape(xValues, yValues);
        ValidatePolicy(policy);
        if (policy != ChartMissingValuePolicy.Gap)
        {
            ChartPoint[] points = ResolvePointArray(xValues, yValues, policy);
            if (policy == ChartMissingValuePolicy.Skip && points.Length < 2)
            {
                ChartPlot.ValidateId(id);
                ChartLinearScale.ValidateFinite(baseline, "baseline");
                ChartLineAreaChartsOperations.ValidateStyle(style);
                return new ChartCartesianSeries(id, ChartCartesianSeriesKind.Area);
            }

            return ChartLineAreaChartsOperations.CreateAreaSeries(id, PointXValues(points), PointYValues(points), baseline, style);
        }

        ChartPlot.ValidateId(id);
        ChartLinearScale.ValidateFinite(baseline, "baseline");
        ChartLineAreaChartsOperations.ValidateStyle(style);
        ChartCartesianSeries series = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Area);
        int start = 0;
        int segment = 0;
        while (start < xValues.Length)
        {
            while (start < xValues.Length && (IsMissing(xValues[start]) || IsMissing(yValues[start])))
            {
                start = start + 1;
            }

            int end = start;
            while (end < xValues.Length && !IsMissing(xValues[end]) && !IsMissing(yValues[end]))
            {
                end = end + 1;
            }

            if (end - start >= 2)
            {
                double[] segmentX = CopyNumberRange(xValues, start, end);
                double[] segmentY = CopyNumberRange(yValues, start, end);
                double[] lower = ChartLineAreaChartsOperations.ConstantValues(segmentX.Length, baseline);
                ChartPoint[] polygon = ChartLineAreaChartsOperations.CreateAreaPolygonPoints(segmentX, segmentY, lower);
                string suffix = segment.ToString(System.Globalization.CultureInfo.InvariantCulture);
                series.AddCommand(ChartRenderCommand.CreatePolygon(id + "-area-" + suffix, polygon, style));
                segment = segment + 1;
            }

            start = end + 1;
        }

        return series;
    }

    internal static ChartCartesianSeries CreatePolicyScatterSeries(string id, double[] xValues, double[] yValues, ChartMissingValuePolicy policy, ChartMarkerShape shape, double markerSize, ChartRenderStyle style)
    {
        ChartPoint[] points = ResolveBuilderPoints(xValues, yValues, policy);
        if (points.Length == 0)
        {
            ChartPlot.ValidateId(id);
            ChartRenderCommand.ValidateMarkerShape(shape);
            ChartRenderStyle.ValidatePositive(markerSize, "markerSize");
            ChartLineAreaChartsOperations.ValidateStyle(style);
            return new ChartCartesianSeries(id, ChartCartesianSeriesKind.Scatter);
        }

        return ChartScatterChartsOperations.CreateScatterSeries(id, PointXValues(points), PointYValues(points), shape, markerSize, style);
    }

    internal static ChartCartesianSeries CreatePolicyVerticalBarSeries(string id, double[] xValues, double[] yValues, ChartMissingValuePolicy policy, double barWidth, double baseline, ChartRenderStyle style)
    {
        ChartPoint[] points = ResolveBuilderPoints(xValues, yValues, policy);
        if (points.Length == 0)
        {
            ChartPlot.ValidateId(id);
            ChartRenderStyle.ValidatePositive(barWidth, "barWidth");
            ChartLinearScale.ValidateFinite(baseline, "baseline");
            ChartLineAreaChartsOperations.ValidateStyle(style);
            return new ChartCartesianSeries(id, ChartCartesianSeriesKind.Bar);
        }

        return ChartBarChartsOperations.CreateVerticalBarSeries(id, PointXValues(points), PointYValues(points), barWidth, baseline, style);
    }

    internal static bool IsMissing(double value)
    {
        return double.IsNaN(value) || double.IsInfinity(value);
    }

    internal static void ValidatePolicy(ChartMissingValuePolicy policy)
    {
        int value = (int)policy;
        if (value < 0 || value > 4)
        {
            throw new System.ArgumentOutOfRangeException("policy");
        }
    }

    internal static void ValidatePairShape(double[] xValues, double[] yValues)
    {
        if (xValues == null)
        {
            throw new System.ArgumentNullException("xValues");
        }

        if (yValues == null)
        {
            throw new System.ArgumentNullException("yValues");
        }

        if (xValues.Length != yValues.Length || xValues.Length == 0)
        {
            throw new System.ArgumentException("Coordinate arrays must have the same positive length.");
        }
    }

    internal static double[] RemoveMissing(double[] values)
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

    internal static void FillZero(double[] values)
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

    internal static void FillCarryForward(double[] values)
    {
        double previous = 0.0;
        bool found = false;
        int index = 0;
        while (index < values.Length)
        {
            if (IsMissing(values[index]))
            {
                values[index] = found ? previous : 0.0;
            }
            else
            {
                previous = values[index];
                found = true;
            }

            index = index + 1;
        }
    }

    internal static void FillInterpolation(double[] values)
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

    internal static ChartPoint[] RemoveMissingRows(double[] xValues, double[] yValues)
    {
        ChartPoint[] buffer = new ChartPoint[xValues.Length];
        int count = 0;
        int index = 0;
        while (index < xValues.Length)
        {
            if (!IsMissing(xValues[index]) && !IsMissing(yValues[index]))
            {
                buffer[count] = new ChartPoint(xValues[index], yValues[index]);
                count = count + 1;
            }

            index = index + 1;
        }

        ChartPoint[] result = new ChartPoint[count];
        System.Array.Copy(buffer, result, count);
        return result;
    }

    internal static ChartPoint[] BuildPoints(double[] xValues, double[] yValues)
    {
        ChartPoint[] points = new ChartPoint[xValues.Length];
        int index = 0;
        while (index < points.Length)
        {
            points[index] = new ChartPoint(xValues[index], yValues[index]);
            index = index + 1;
        }

        return points;
    }

    internal static double[] PointXValues(ChartPoint[] points)
    {
        double[] values = new double[points.Length];
        int index = 0;
        while (index < points.Length)
        {
            values[index] = points[index].X;
            index = index + 1;
        }

        return values;
    }

    internal static double[] PointYValues(ChartPoint[] points)
    {
        double[] values = new double[points.Length];
        int index = 0;
        while (index < points.Length)
        {
            values[index] = points[index].Y;
            index = index + 1;
        }

        return values;
    }

    internal static double[] CopyNumberRange(double[] values, int start, int end)
    {
        double[] result = new double[end - start];
        System.Array.Copy(values, start, result, 0, result.Length);
        return result;
    }

    internal static int FindMatrixColumn(ChartDataTable table)
    {
        int index = 0;
        while (index < table.ColumnCount)
        {
            if (table.Columns[index].Kind == ChartDataKind.Matrix)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static double[] SelectNumbers(double[] values, bool[] keep, int count)
    {
        double[] result = new double[count];
        int output = 0;
        int index = 0;
        while (index < values.Length)
        {
            if (keep[index])
            {
                result[output] = values[index];
                output = output + 1;
            }

            index = index + 1;
        }

        return result;
    }

    internal static string? [] SelectText(string? [] values, bool[] keep, int count)
    {
        string? [] result = new string? [count];
        int output = 0;
        int index = 0;
        while (index < values.Length)
        {
            if (keep[index])
            {
                result[output] = values[index];
                output = output + 1;
            }

            index = index + 1;
        }

        return result;
    }

    internal static System.DateTime[] SelectDates(System.DateTime[] values, bool[] keep, int count)
    {
        System.DateTime[] result = new System.DateTime[count];
        int output = 0;
        int index = 0;
        while (index < values.Length)
        {
            if (keep[index])
            {
                result[output] = values[index];
                output = output + 1;
            }

            index = index + 1;
        }

        return result;
    }
}
