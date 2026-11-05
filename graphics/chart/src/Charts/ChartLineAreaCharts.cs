namespace chart;
internal static class ChartLineAreaChartsOperations
{
    /// <summary>
    /// Creates the requested line series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public static ChartCartesianSeries CreateLineSeries(string id, double[] xValues, double[] yValues, ChartRenderStyle style)
    {
        return CreateLineSeries(id, xValues, yValues, ChartLineInterpolation.Linear, 8, style);
    }

    /// <summary>
    /// Creates the requested line series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <param name = "interpolation">The rule used to connect values between adjacent data points.</param>
    /// <param name = "splineSamplesPerSegment">The number of straight segments used to approximate each spline segment.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public static ChartCartesianSeries CreateLineSeries(string id, double[] xValues, double[] yValues, ChartLineInterpolation interpolation, int splineSamplesPerSegment, ChartRenderStyle style)
    {
        ChartPlot.ValidateId(id);
        ValidateInterpolation(interpolation);
        ValidateStyle(style);
        ChartCartesianTransform.ValidateValueArrays(xValues, yValues, 2);
        ChartCartesianSeries series = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Line);
        ChartPoint[] points = CreateLinePoints(xValues, yValues, interpolation, splineSamplesPerSegment);
        series.AddCommand(ChartRenderCommand.CreatePolyline(id + "-line", points, style));
        return series;
    }

    /// <summary>
    /// Creates the requested multi line series from the supplied data and configuration.
    /// </summary>
    /// <param name = "idPrefix">The prefix used to create stable identifiers for generated items.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <param name = "palette">The categorical colors to apply in stable index order.</param>
    /// <param name = "strokeWidth">The stroke width in chart units, which must be positive.</param>
    /// <returns>The newly created cartesian series array.</returns>
    public static ChartCartesianSeries[] CreateMultiLineSeries(string idPrefix, double[] xValues, ChartDataMatrix yValues, ChartPalette palette, double strokeWidth)
    {
        return CreateMultiLineSeries(idPrefix, xValues, yValues, palette, strokeWidth, ChartLineInterpolation.Linear, 8);
    }

    /// <summary>
    /// Creates the requested multi line series from the supplied data and configuration.
    /// </summary>
    /// <param name = "idPrefix">The prefix used to create stable identifiers for generated items.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <param name = "palette">The categorical colors to apply in stable index order.</param>
    /// <param name = "strokeWidth">The stroke width in chart units, which must be positive.</param>
    /// <param name = "interpolation">The rule used to connect values between adjacent data points.</param>
    /// <param name = "splineSamplesPerSegment">The number of straight segments used to approximate each spline segment.</param>
    /// <returns>The newly created cartesian series array.</returns>
    public static ChartCartesianSeries[] CreateMultiLineSeries(string idPrefix, double[] xValues, ChartDataMatrix yValues, ChartPalette palette, double strokeWidth, ChartLineInterpolation interpolation, int splineSamplesPerSegment)
    {
        ChartPlot.ValidateId(idPrefix);
        ValidateMatrixRows(yValues, xValues, 2);
        ValidatePalette(palette);
        ChartRenderStyle.ValidatePositive(strokeWidth, "strokeWidth");
        ValidateInterpolation(interpolation);
        ChartCartesianSeries[] series = new ChartCartesianSeries[yValues.Columns];
        int column = 0;
        while (column < yValues.Columns)
        {
            ChartRenderStyle style = ChartRenderStyle.CreateStroke(palette.GetColor(column), strokeWidth);
            string id = idPrefix + "-" + column.ToString(System.Globalization.CultureInfo.InvariantCulture);
            series[column] = CreateLineSeries(id, xValues, ColumnValues(yValues, column), interpolation, splineSamplesPerSegment, style);
            series[column].SetOrder(column);
            column = column + 1;
        }

        return series;
    }

    /// <summary>
    /// Creates the requested step line series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <param name = "stepInterpolation">The before, after, or middle rule used to place each step transition.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    public static ChartCartesianSeries CreateStepLineSeries(string id, double[] xValues, double[] yValues, ChartLineInterpolation stepInterpolation, ChartRenderStyle style)
    {
        if (stepInterpolation != ChartLineInterpolation.StepBefore && stepInterpolation != ChartLineInterpolation.StepAfter && stepInterpolation != ChartLineInterpolation.StepMiddle)
        {
            throw new System.ArgumentException("Step line series require a step interpolation mode.");
        }

        return CreateLineSeries(id, xValues, yValues, stepInterpolation, 1, style);
    }

    /// <summary>
    /// Creates the requested spline line series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <param name = "samplesPerSegment">The samples per segment.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public static ChartCartesianSeries CreateSplineLineSeries(string id, double[] xValues, double[] yValues, int samplesPerSegment, ChartRenderStyle style)
    {
        return CreateLineSeries(id, xValues, yValues, ChartLineInterpolation.Spline, samplesPerSegment, style);
    }

    /// <summary>Creates a path whose straight segments sample the requested line interpolation.</summary>
    /// <param name = "xValues">The horizontal data values.</param>
    /// <param name = "yValues">The vertical data values.</param>
    /// <param name = "interpolation">The interpolation used between values.</param>
    /// <param name = "samplesPerSegment">The number of samples used for each cubic segment.</param>
    /// <returns>The generated path.</returns>
    public static ChartPath CreateInterpolatedPath(double[] xValues, double[] yValues, ChartLineInterpolation interpolation, int samplesPerSegment)
    {
        ChartPoint[] points = CreateLinePoints(xValues, yValues, interpolation, samplesPerSegment);
        ChartPath path = new ChartPath();
        path.MoveTo(points[0].X, points[0].Y);
        int index = 1;
        while (index < points.Length)
        {
            path.LineTo(points[index].X, points[index].Y);
            index = index + 1;
        }

        return path;
    }

    /// <summary>
    /// Creates the requested area series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <param name = "baseline">The baseline.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public static ChartCartesianSeries CreateAreaSeries(string id, double[] xValues, double[] yValues, double baseline, ChartRenderStyle style)
    {
        ChartCartesianTransform.ValidateValueArrays(xValues, yValues, 2);
        ChartLinearScale.ValidateFinite(baseline, "baseline");
        return CreateAreaSeries(id, xValues, yValues, ConstantValues(xValues.Length, baseline), style);
    }

    /// <summary>
    /// Creates the requested area series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "upperValues">The upper values in data order.</param>
    /// <param name = "lowerValues">The lower values in data order.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    public static ChartCartesianSeries CreateAreaSeries(string id, double[] xValues, double[] upperValues, double[] lowerValues, ChartRenderStyle style)
    {
        ChartPlot.ValidateId(id);
        ValidateStyle(style);
        ChartCartesianTransform.ValidateValueArrays(xValues, upperValues, 2);
        ChartCartesianTransform.ValidateValueArrays(xValues, lowerValues, 2);
        ChartCartesianSeries series = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Area);
        series.AddCommand(ChartRenderCommand.CreatePolygon(id + "-area", CreateAreaPolygonPoints(xValues, upperValues, lowerValues), style));
        return series;
    }

    /// <summary>Creates an interpolated area series between values and a constant baseline.</summary>
    /// <param name = "id">A stable series identifier.</param>
    /// <param name = "xValues">The horizontal data values.</param>
    /// <param name = "yValues">The vertical data values.</param>
    /// <param name = "baseline">The finite area baseline.</param>
    /// <param name = "interpolation">The interpolation used for both area boundaries.</param>
    /// <param name = "samplesPerSegment">The number of samples used for each cubic segment.</param>
    /// <param name = "style">The area render style.</param>
    /// <returns>The generated area series.</returns>
    public static ChartCartesianSeries CreateInterpolatedAreaSeries(string id, double[] xValues, double[] yValues, double baseline, ChartLineInterpolation interpolation, int samplesPerSegment, ChartRenderStyle style)
    {
        ChartCartesianTransform.ValidateValueArrays(xValues, yValues, 2);
        ChartLinearScale.ValidateFinite(baseline, "baseline");
        return CreateInterpolatedRangeAreaSeries(id, xValues, yValues, ConstantValues(xValues.Length, baseline), interpolation, samplesPerSegment, style);
    }

    /// <summary>Creates an interpolated area series between upper and lower value arrays.</summary>
    /// <param name = "id">A stable series identifier.</param>
    /// <param name = "xValues">The horizontal data values.</param>
    /// <param name = "upperValues">The upper boundary values.</param>
    /// <param name = "lowerValues">The lower boundary values.</param>
    /// <param name = "interpolation">The interpolation used for both area boundaries.</param>
    /// <param name = "samplesPerSegment">The number of samples used for each cubic segment.</param>
    /// <param name = "style">The area render style.</param>
    /// <returns>The generated area series.</returns>
    public static ChartCartesianSeries CreateInterpolatedRangeAreaSeries(string id, double[] xValues, double[] upperValues, double[] lowerValues, ChartLineInterpolation interpolation, int samplesPerSegment, ChartRenderStyle style)
    {
        ChartPlot.ValidateId(id);
        ValidateStyle(style);
        ChartCartesianTransform.ValidateValueArrays(xValues, upperValues, 2);
        ChartCartesianTransform.ValidateValueArrays(xValues, lowerValues, 2);
        ChartCartesianSeries series = new ChartCartesianSeries(id, ChartCartesianSeriesKind.Area);
        ChartPoint[] points = CreateInterpolatedAreaPolygonPoints(xValues, upperValues, lowerValues, interpolation, samplesPerSegment);
        series.AddCommand(ChartRenderCommand.CreatePolygon(id + "-area", points, style));
        return series;
    }

    /// <summary>
    /// Creates the requested stacked area series from the supplied data and configuration.
    /// </summary>
    /// <param name = "idPrefix">The prefix used to create stable identifiers for generated items.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "mode">The mode.</param>
    /// <param name = "palette">The categorical colors to apply in stable index order.</param>
    /// <returns>The newly created cartesian series array.</returns>
    public static ChartCartesianSeries[] CreateStackedAreaSeries(string idPrefix, double[] xValues, ChartDataMatrix values, ChartStackMode mode, ChartPalette palette)
    {
        ChartPlot.ValidateId(idPrefix);
        ValidateMatrixRows(values, xValues, 2);
        ValidatePalette(palette);
        ChartStackResult stack = ChartStackerOperations.Stack(values, mode);
        ChartCartesianSeries[] series = new ChartCartesianSeries[values.Columns];
        int column = 0;
        while (column < values.Columns)
        {
            ChartRenderStyle style = ChartRenderStyle.CreateFill(palette.GetColor(column));
            string id = idPrefix + "-" + column.ToString(System.Globalization.CultureInfo.InvariantCulture);
            series[column] = CreateAreaSeries(id, xValues, ColumnValues(stack.Ends, column), ColumnValues(stack.Starts, column), style);
            series[column].SetOrder(column);
            column = column + 1;
        }

        return series;
    }

    /// <summary>
    /// Creates the requested streamgraph series from the supplied data and configuration.
    /// </summary>
    /// <param name = "idPrefix">The prefix used to create stable identifiers for generated items.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "palette">The categorical colors to apply in stable index order.</param>
    /// <returns>The newly created cartesian series array.</returns>
    public static ChartCartesianSeries[] CreateStreamgraphSeries(string idPrefix, double[] xValues, ChartDataMatrix values, ChartPalette palette)
    {
        return CreateStackedAreaSeries(idPrefix, xValues, values, ChartStackMode.Streamgraph, palette);
    }

    internal static ChartPoint[] CreateLinePoints(double[] xValues, double[] yValues, ChartLineInterpolation interpolation, int splineSamplesPerSegment)
    {
        ValidateInterpolation(interpolation);
        ChartCartesianTransform.ValidateValueArrays(xValues, yValues, 2);
        if (interpolation == ChartLineInterpolation.Linear)
        {
            return ChartCartesianSeries.CreateDataPoints(xValues, yValues);
        }

        if (interpolation == ChartLineInterpolation.StepBefore || interpolation == ChartLineInterpolation.StepAfter || interpolation == ChartLineInterpolation.StepMiddle)
        {
            return CreateStepPoints(xValues, yValues, interpolation);
        }

        if (interpolation == ChartLineInterpolation.Spline || interpolation == ChartLineInterpolation.CatmullRom)
        {
            return CreateSplinePoints(xValues, yValues, splineSamplesPerSegment);
        }

        if (interpolation == ChartLineInterpolation.MonotoneCubic)
        {
            return CreateMonotoneCubicPoints(xValues, yValues, splineSamplesPerSegment);
        }

        return CreateNaturalCubicPoints(xValues, yValues, splineSamplesPerSegment);
    }

    internal static ChartPoint[] CreateStepPoints(double[] xValues, double[] yValues, ChartLineInterpolation interpolation)
    {
        ChartCartesianTransform.ValidateValueArrays(xValues, yValues, 2);
        if (interpolation != ChartLineInterpolation.StepBefore && interpolation != ChartLineInterpolation.StepAfter && interpolation != ChartLineInterpolation.StepMiddle)
        {
            throw new System.ArgumentException("Unsupported step interpolation.");
        }

        int count = 1 + (xValues.Length - 1) * 2;
        if (interpolation == ChartLineInterpolation.StepMiddle)
        {
            count = 1 + (xValues.Length - 1) * 3;
        }

        ChartPoint[] points = new ChartPoint[count];
        points[0] = new ChartPoint(xValues[0], yValues[0]);
        int output = 1;
        int index = 1;
        while (index < xValues.Length)
        {
            if (interpolation == ChartLineInterpolation.StepBefore)
            {
                points[output] = new ChartPoint(xValues[index - 1], yValues[index]);
                output = output + 1;
                points[output] = new ChartPoint(xValues[index], yValues[index]);
                output = output + 1;
            }
            else if (interpolation == ChartLineInterpolation.StepAfter)
            {
                points[output] = new ChartPoint(xValues[index], yValues[index - 1]);
                output = output + 1;
                points[output] = new ChartPoint(xValues[index], yValues[index]);
                output = output + 1;
            }
            else
            {
                double middle = (xValues[index - 1] + xValues[index]) * 0.5;
                points[output] = new ChartPoint(middle, yValues[index - 1]);
                output = output + 1;
                points[output] = new ChartPoint(middle, yValues[index]);
                output = output + 1;
                points[output] = new ChartPoint(xValues[index], yValues[index]);
                output = output + 1;
            }

            index = index + 1;
        }

        return points;
    }

    internal static ChartPoint[] CreateSplinePoints(double[] xValues, double[] yValues, int samplesPerSegment)
    {
        ChartCartesianTransform.ValidateValueArrays(xValues, yValues, 2);
        if (samplesPerSegment <= 0)
        {
            throw new System.ArgumentOutOfRangeException("samplesPerSegment");
        }

        long pointCount = (long)(xValues.Length - 1) * samplesPerSegment + 1;
        if (pointCount > int.MaxValue)
        {
            throw new System.ArgumentOutOfRangeException("samplesPerSegment");
        }

        ChartPoint[] points = new ChartPoint[(int)pointCount];
        int output = 0;
        int segment = 0;
        while (segment < xValues.Length - 1)
        {
            int sample = 0;
            while (sample < samplesPerSegment)
            {
                double amount = sample / (double)samplesPerSegment;
                points[output] = CatmullRomPoint(xValues, yValues, segment, amount);
                output = output + 1;
                sample = sample + 1;
            }

            segment = segment + 1;
        }

        points[output] = new ChartPoint(xValues[xValues.Length - 1], yValues[yValues.Length - 1]);
        return points;
    }

    internal static ChartPoint[] CreateMonotoneCubicPoints(double[] xValues, double[] yValues, int samplesPerSegment)
    {
        int pointCount = ValidateCubicSampling(xValues, yValues, samplesPerSegment);
        int segmentCount = xValues.Length - 1;
        double[] widths = new double[segmentCount];
        double[] slopes = new double[segmentCount];
        double[] tangents = new double[xValues.Length];
        int index = 0;
        while (index < segmentCount)
        {
            widths[index] = xValues[index + 1] - xValues[index];
            slopes[index] = (yValues[index + 1] - yValues[index]) / widths[index];
            index = index + 1;
        }

        tangents[0] = slopes[0];
        tangents[tangents.Length - 1] = slopes[slopes.Length - 1];
        index = 1;
        while (index < tangents.Length - 1)
        {
            double previous = slopes[index - 1];
            double next = slopes[index];
            if (previous == 0.0 || next == 0.0 || previous > 0.0 && next < 0.0 || previous < 0.0 && next > 0.0)
            {
                tangents[index] = 0.0;
            }
            else
            {
                double firstWeight = 2.0 * widths[index] + widths[index - 1];
                double secondWeight = widths[index] + 2.0 * widths[index - 1];
                tangents[index] = (firstWeight + secondWeight) / (firstWeight / previous + secondWeight / next);
            }

            index = index + 1;
        }

        ChartPoint[] points = new ChartPoint[pointCount];
        int output = 0;
        int segment = 0;
        while (segment < segmentCount)
        {
            int sample = 0;
            while (sample < samplesPerSegment)
            {
                double amount = sample / (double)samplesPerSegment;
                double amountSquared = amount * amount;
                double amountCubed = amountSquared * amount;
                double firstBasis = 2.0 * amountCubed - 3.0 * amountSquared + 1.0;
                double firstTangentBasis = amountCubed - 2.0 * amountSquared + amount;
                double secondBasis = 0.0 - 2.0 * amountCubed + 3.0 * amountSquared;
                double secondTangentBasis = amountCubed - amountSquared;
                double x = xValues[segment] + widths[segment] * amount;
                double y = firstBasis * yValues[segment] + firstTangentBasis * widths[segment] * tangents[segment] + secondBasis * yValues[segment + 1] + secondTangentBasis * widths[segment] * tangents[segment + 1];
                points[output] = new ChartPoint(x, y);
                output = output + 1;
                sample = sample + 1;
            }

            segment = segment + 1;
        }

        points[output] = new ChartPoint(xValues[xValues.Length - 1], yValues[yValues.Length - 1]);
        return points;
    }

    internal static ChartPoint[] CreateNaturalCubicPoints(double[] xValues, double[] yValues, int samplesPerSegment)
    {
        int pointCount = ValidateCubicSampling(xValues, yValues, samplesPerSegment);
        int length = xValues.Length;
        double[] widths = new double[length - 1];
        double[] alpha = new double[length];
        int index = 0;
        while (index < widths.Length)
        {
            widths[index] = xValues[index + 1] - xValues[index];
            index = index + 1;
        }

        index = 1;
        while (index < length - 1)
        {
            alpha[index] = 3.0 * (yValues[index + 1] - yValues[index]) / widths[index] - 3.0 * (yValues[index] - yValues[index - 1]) / widths[index - 1];
            index = index + 1;
        }

        double[] diagonal = new double[length];
        double[] upper = new double[length];
        double[] solution = new double[length];
        double[] quadratic = new double[length];
        diagonal[0] = 1.0;
        upper[0] = 0.0;
        solution[0] = 0.0;
        index = 1;
        while (index < length - 1)
        {
            diagonal[index] = 2.0 * (xValues[index + 1] - xValues[index - 1]) - widths[index - 1] * upper[index - 1];
            upper[index] = widths[index] / diagonal[index];
            solution[index] = (alpha[index] - widths[index - 1] * solution[index - 1]) / diagonal[index];
            index = index + 1;
        }

        diagonal[length - 1] = 1.0;
        solution[length - 1] = 0.0;
        quadratic[length - 1] = 0.0;
        double[] linear = new double[length - 1];
        double[] cubic = new double[length - 1];
        index = length - 2;
        while (index >= 0)
        {
            quadratic[index] = solution[index] - upper[index] * quadratic[index + 1];
            linear[index] = (yValues[index + 1] - yValues[index]) / widths[index] - widths[index] * (2.0 * quadratic[index] + quadratic[index + 1]) / 3.0;
            cubic[index] = (quadratic[index + 1] - quadratic[index]) / (3.0 * widths[index]);
            index = index - 1;
        }

        ChartPoint[] points = new ChartPoint[pointCount];
        int output = 0;
        int segment = 0;
        while (segment < length - 1)
        {
            int sample = 0;
            while (sample < samplesPerSegment)
            {
                double amount = sample / (double)samplesPerSegment;
                double distance = widths[segment] * amount;
                double x = xValues[segment] + distance;
                double y = yValues[segment] + linear[segment] * distance + quadratic[segment] * distance * distance + cubic[segment] * distance * distance * distance;
                points[output] = new ChartPoint(x, y);
                output = output + 1;
                sample = sample + 1;
            }

            segment = segment + 1;
        }

        points[output] = new ChartPoint(xValues[length - 1], yValues[length - 1]);
        return points;
    }

    internal static int ValidateCubicSampling(double[] xValues, double[] yValues, int samplesPerSegment)
    {
        ChartCartesianTransform.ValidateValueArrays(xValues, yValues, 2);
        if (samplesPerSegment <= 0)
        {
            throw new System.ArgumentOutOfRangeException("samplesPerSegment");
        }

        int index = 1;
        while (index < xValues.Length)
        {
            if (xValues[index] <= xValues[index - 1])
            {
                throw new System.ArgumentException("Cubic interpolation requires strictly increasing horizontal values.");
            }

            index = index + 1;
        }

        long pointCount = (long)(xValues.Length - 1) * samplesPerSegment + 1;
        if (pointCount > int.MaxValue)
        {
            throw new System.ArgumentOutOfRangeException("samplesPerSegment");
        }

        return (int)pointCount;
    }

    internal static ChartPoint CatmullRomPoint(double[] xValues, double[] yValues, int segment, double amount)
    {
        ChartCartesianTransform.ValidateValueArrays(xValues, yValues, 2);
        if (segment < 0 || segment >= xValues.Length - 1)
        {
            throw new System.ArgumentOutOfRangeException("segment");
        }

        if (amount < 0.0 || amount > 1.0 || double.IsNaN(amount) || double.IsInfinity(amount))
        {
            throw new System.ArgumentOutOfRangeException("amount");
        }

        int i0 = ClampIndex(segment - 1, xValues.Length);
        int i1 = ClampIndex(segment, xValues.Length);
        int i2 = ClampIndex(segment + 1, xValues.Length);
        int i3 = ClampIndex(segment + 2, xValues.Length);
        return new ChartPoint(CatmullRomValue(xValues[i0], xValues[i1], xValues[i2], xValues[i3], amount), CatmullRomValue(yValues[i0], yValues[i1], yValues[i2], yValues[i3], amount));
    }

    internal static double CatmullRomValue(double p0, double p1, double p2, double p3, double amount)
    {
        double t2 = amount * amount;
        double t3 = t2 * amount;
        return 0.5 * ((2.0 * p1) + (p2 - p0) * amount + (2.0 * p0 - 5.0 * p1 + 4.0 * p2 - p3) * t2 + (0.0 - p0 + 3.0 * p1 - 3.0 * p2 + p3) * t3);
    }

    internal static ChartPoint[] CreateAreaPolygonPoints(double[] xValues, double[] upperValues, double[] lowerValues)
    {
        ChartCartesianTransform.ValidateValueArrays(xValues, upperValues, 2);
        ChartCartesianTransform.ValidateValueArrays(xValues, lowerValues, 2);
        ChartPoint[] points = new ChartPoint[xValues.Length * 2];
        int index = 0;
        while (index < xValues.Length)
        {
            points[index] = new ChartPoint(xValues[index], upperValues[index]);
            index = index + 1;
        }

        index = 0;
        while (index < xValues.Length)
        {
            int source = xValues.Length - index - 1;
            points[xValues.Length + index] = new ChartPoint(xValues[source], lowerValues[source]);
            index = index + 1;
        }

        return points;
    }

    internal static ChartPoint[] CreateInterpolatedAreaPolygonPoints(double[] xValues, double[] upperValues, double[] lowerValues, ChartLineInterpolation interpolation, int samplesPerSegment)
    {
        ChartPoint[] upperPoints = CreateLinePoints(xValues, upperValues, interpolation, samplesPerSegment);
        ChartPoint[] lowerPoints = CreateLinePoints(xValues, lowerValues, interpolation, samplesPerSegment);
        ChartPoint[] points = new ChartPoint[upperPoints.Length + lowerPoints.Length];
        int index = 0;
        while (index < upperPoints.Length)
        {
            points[index] = upperPoints[index].Copy();
            index = index + 1;
        }

        index = 0;
        while (index < lowerPoints.Length)
        {
            points[upperPoints.Length + index] = lowerPoints[lowerPoints.Length - index - 1].Copy();
            index = index + 1;
        }

        return points;
    }

    internal static double[] ColumnValues(ChartDataMatrix matrix, int column)
    {
        if (matrix == null)
        {
            throw new System.ArgumentNullException("matrix");
        }

        if (column < 0 || column >= matrix.Columns)
        {
            throw new System.ArgumentOutOfRangeException("column");
        }

        double[] values = new double[matrix.Rows];
        int row = 0;
        while (row < matrix.Rows)
        {
            values[row] = matrix.GetValue(row, column);
            row = row + 1;
        }

        return values;
    }

    internal static double[] ConstantValues(int count, double value)
    {
        if (count <= 0)
        {
            throw new System.ArgumentOutOfRangeException("count");
        }

        ChartLinearScale.ValidateFinite(value, "value");
        double[] values = new double[count];
        int index = 0;
        while (index < count)
        {
            values[index] = value;
            index = index + 1;
        }

        return values;
    }

    internal static int ClampIndex(int index, int length)
    {
        if (length <= 0)
        {
            throw new System.ArgumentOutOfRangeException("length");
        }

        if (index < 0)
        {
            return 0;
        }

        if (index >= length)
        {
            return length - 1;
        }

        return index;
    }

    internal static void ValidateMatrixRows(ChartDataMatrix matrix, double[] xValues, int minimumRows)
    {
        if (matrix == null)
        {
            throw new System.ArgumentNullException("matrix");
        }

        if (xValues == null)
        {
            throw new System.ArgumentNullException("xValues");
        }

        if (matrix.Rows != xValues.Length || matrix.Rows < minimumRows)
        {
            throw new System.ArgumentException("Matrix row count must match x values.");
        }

        int index = 0;
        while (index < xValues.Length)
        {
            ChartLinearScale.ValidateFinite(xValues[index], "xValues");
            index = index + 1;
        }

        index = 0;
        while (index < matrix.Values.Length)
        {
            ChartLinearScale.ValidateFinite(matrix.Values[index], "matrix");
            index = index + 1;
        }
    }

    internal static void ValidatePalette(ChartPalette palette)
    {
        if (palette == null)
        {
            throw new System.ArgumentNullException("palette");
        }

        if (palette.Colors == null || palette.Colors.Length == 0)
        {
            throw new System.ArgumentException("Palette cannot be empty.");
        }
    }

    internal static void ValidateStyle(ChartRenderStyle style)
    {
        if (style == null)
        {
            throw new System.ArgumentNullException("style");
        }
    }

    internal static void ValidateInterpolation(ChartLineInterpolation interpolation)
    {
        int value = (int)interpolation;
        if (value < 0 || value > 7)
        {
            throw new System.ArgumentOutOfRangeException("interpolation");
        }
    }
}
