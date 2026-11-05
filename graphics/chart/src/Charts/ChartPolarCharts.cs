namespace chart;
internal static class ChartPolarChartsOperations
{
    internal static ChartRenderScene CreatePolarLineScene(string id, int width, int height, double[] angleValues, double[] radiusValues, ChartPolarTransform transform, bool closed, ChartRenderStyle style)
    {
        ChartRenderScene scene = new ChartRenderScene(width, height);
        AddPolarLine(scene, id, angleValues, radiusValues, transform, closed, style);
        return scene;
    }

    internal static ChartRenderScene CreatePolarScatterScene(string id, int width, int height, double[] angleValues, double[] radiusValues, ChartPolarTransform transform, ChartMarkerShape shape, double markerSize, ChartRenderStyle style)
    {
        ChartRenderScene scene = new ChartRenderScene(width, height);
        AddPolarScatter(scene, id, angleValues, radiusValues, transform, shape, markerSize, style);
        return scene;
    }

    internal static ChartRenderScene CreatePolarBarScene(string id, int width, int height, double[] angleValues, double[] radiusValues, ChartPolarTransform transform, double angleWidth, double baselineValue, ChartRenderStyle style)
    {
        ChartRenderScene scene = new ChartRenderScene(width, height);
        AddPolarBars(scene, id, angleValues, radiusValues, transform, angleWidth, baselineValue, style);
        return scene;
    }

    internal static ChartRenderScene CreateRadarScene(string id, int width, int height, double[] values, ChartPolarTransform transform, ChartRenderStyle style)
    {
        ChartRenderScene scene = new ChartRenderScene(width, height);
        AddRadar(scene, id, values, transform, style);
        return scene;
    }
    /// <summary>
    /// Adds the supplied polar line to this object.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "angleValues">The angle values in data order.</param>
    /// <param name = "radiusValues">The radius values in data order.</param>
    /// <param name = "transform">The transform.</param>
    /// <param name = "closed">The closed.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    public static void AddPolarLine(IChartRenderTarget scene, string id, double[] angleValues, double[] radiusValues, ChartPolarTransform transform, bool closed, ChartRenderStyle style)
    {
        ValidateSceneIdTransformStyle(scene, id, transform, style);
        ValidateAngleRadiusArrays(angleValues, radiusValues, 2);
        int extra = 0;
        if (closed)
        {
            extra = 1;
        }

        ChartPoint[] points = new ChartPoint[angleValues.Length + extra];
        int index = 0;
        while (index < angleValues.Length)
        {
            points[index] = transform.PointAt(angleValues[index], radiusValues[index]);
            index = index + 1;
        }

        if (closed)
        {
            points[points.Length - 1] = points[0].Copy();
        }

        scene.AddPolyline(id + "-line", points, style);
    }

    /// <summary>
    /// Adds the supplied polar scatter to this object.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "angleValues">The angle values in data order.</param>
    /// <param name = "radiusValues">The radius values in data order.</param>
    /// <param name = "transform">The transform.</param>
    /// <param name = "shape">The shape.</param>
    /// <param name = "markerSize">The marker size.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    public static void AddPolarScatter(IChartRenderTarget scene, string id, double[] angleValues, double[] radiusValues, ChartPolarTransform transform, ChartMarkerShape shape, double markerSize, ChartRenderStyle style)
    {
        ValidateSceneIdTransformStyle(scene, id, transform, style);
        ValidateAngleRadiusArrays(angleValues, radiusValues, 1);
        ChartRenderCommand.ValidateMarkerShape(shape);
        ChartRenderStyle.ValidatePositive(markerSize, "markerSize");
        int index = 0;
        while (index < angleValues.Length)
        {
            ChartPoint point = transform.PointAt(angleValues[index], radiusValues[index]);
            string suffix = index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            scene.AddMarker(id + "-point-" + suffix, shape, point.X, point.Y, markerSize, style);
            index = index + 1;
        }
    }

    /// <summary>
    /// Adds the supplied polar bars to this object.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "angleValues">The angle values in data order.</param>
    /// <param name = "radiusValues">The radius values in data order.</param>
    /// <param name = "transform">The transform.</param>
    /// <param name = "angleWidth">The angle width.</param>
    /// <param name = "baselineValue">The baseline value.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    public static void AddPolarBars(IChartRenderTarget scene, string id, double[] angleValues, double[] radiusValues, ChartPolarTransform transform, double angleWidth, double baselineValue, ChartRenderStyle style)
    {
        ValidateSceneIdTransformStyle(scene, id, transform, style);
        ValidateAngleRadiusArrays(angleValues, radiusValues, 1);
        ChartRenderStyle.ValidatePositive(angleWidth, "angleWidth");
        ChartLinearScale.ValidateFinite(baselineValue, "baselineValue");
        int index = 0;
        while (index < angleValues.Length)
        {
            string suffix = index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            scene.AddPath(id + "-bar-" + suffix, CreatePolarBarPath(transform, angleValues[index], radiusValues[index], angleWidth, baselineValue), style);
            index = index + 1;
        }
    }

    /// <summary>
    /// Adds the supplied radar to this object.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "values">The input values to process.</param>
    /// <param name = "transform">The transform.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    public static void AddRadar(IChartRenderTarget scene, string id, double[] values, ChartPolarTransform transform, ChartRenderStyle style)
    {
        ValidateSceneIdTransformStyle(scene, id, transform, style);
        ChartStatisticsOperations.ValidateFiniteValues(values);
        if (values.Length < 3)
        {
            throw new System.ArgumentException("Radar charts require at least three values.");
        }

        ChartPoint[] points = new ChartPoint[values.Length];
        double step = 360.0 / values.Length;
        int index = 0;
        while (index < values.Length)
        {
            points[index] = transform.PointAt(step * index, values[index]);
            index = index + 1;
        }

        scene.AddPolygon(id + "-radar", points, style);
    }

    /// <summary>
    /// Adds the supplied radar spokes to this object.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "spokeCount">The requested number of spoke entries.</param>
    /// <param name = "transform">The transform.</param>
    /// <param name = "value">The value.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    public static void AddRadarSpokes(IChartRenderTarget scene, string id, int spokeCount, ChartPolarTransform transform, double value, ChartRenderStyle style)
    {
        ValidateSceneIdTransformStyle(scene, id, transform, style);
        if (spokeCount < 3)
        {
            throw new System.ArgumentOutOfRangeException("spokeCount");
        }

        ChartLinearScale.ValidateFinite(value, "value");
        double step = 360.0 / spokeCount;
        int index = 0;
        while (index < spokeCount)
        {
            ChartPoint point = transform.PointAt(step * index, value);
            string suffix = index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            scene.AddLine(id + "-spoke-" + suffix, transform.CenterX, transform.CenterY, point.X, point.Y, style);
            index = index + 1;
        }
    }

    /// <summary>
    /// Creates the requested gauge scene from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "width">The width in pixels or chart units, which must be positive.</param>
    /// <param name = "height">The height in pixels or chart units, which must be positive.</param>
    /// <param name = "centerX">The center x.</param>
    /// <param name = "centerY">The center y.</param>
    /// <param name = "innerRadius">The inner radius in chart units; zero produces a solid shape.</param>
    /// <param name = "outerRadius">The outer radius in chart units, which must be greater than the inner radius.</param>
    /// <param name = "minimumValue">The minimum value.</param>
    /// <param name = "maximumValue">The maximum value.</param>
    /// <param name = "value">The value.</param>
    /// <param name = "startAngle">The starting angle in degrees.</param>
    /// <param name = "endAngle">The ending angle in degrees.</param>
    /// <param name = "trackStyle">The rendering style used for track.</param>
    /// <param name = "valueStyle">The rendering style used for value.</param>
    /// <param name = "needleStyle">The rendering style used for needle.</param>
    /// <returns>The newly created render scene.</returns>
    public static ChartRenderScene CreateGaugeScene(string id, int width, int height, double centerX, double centerY, double innerRadius, double outerRadius, double minimumValue, double maximumValue, double value, double startAngle, double endAngle, ChartRenderStyle trackStyle, ChartRenderStyle valueStyle, ChartRenderStyle needleStyle)
    {
        ChartRenderScene scene = new ChartRenderScene(width, height);
        AddGauge(scene, id, centerX, centerY, innerRadius, outerRadius, minimumValue, maximumValue, value, startAngle, endAngle, trackStyle, valueStyle, needleStyle);
        return scene;
    }

    internal static ChartRenderScene CreateBulletScene(string id, int width, int height, double x, double y, double bulletWidth, double bulletHeight, double minimumValue, double maximumValue, double value, double targetValue, ChartOrientation orientation, ChartRenderStyle backgroundStyle, ChartRenderStyle valueStyle, ChartRenderStyle targetStyle)
    {
        ChartRenderScene scene = new ChartRenderScene(width, height);
        AddBullet(scene, id, x, y, bulletWidth, bulletHeight, minimumValue, maximumValue, value, targetValue, orientation, backgroundStyle, valueStyle, targetStyle);
        return scene;
    }

    /// <summary>
    /// Adds the supplied gauge to this object.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "centerX">The center x.</param>
    /// <param name = "centerY">The center y.</param>
    /// <param name = "innerRadius">The inner radius in chart units; zero produces a solid shape.</param>
    /// <param name = "outerRadius">The outer radius in chart units, which must be greater than the inner radius.</param>
    /// <param name = "minimumValue">The minimum value.</param>
    /// <param name = "maximumValue">The maximum value.</param>
    /// <param name = "value">The value.</param>
    /// <param name = "startAngle">The starting angle in degrees.</param>
    /// <param name = "endAngle">The ending angle in degrees.</param>
    /// <param name = "trackStyle">The rendering style used for track.</param>
    /// <param name = "valueStyle">The rendering style used for value.</param>
    /// <param name = "needleStyle">The rendering style used for needle.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static void AddGauge(IChartRenderTarget scene, string id, double centerX, double centerY, double innerRadius, double outerRadius, double minimumValue, double maximumValue, double value, double startAngle, double endAngle, ChartRenderStyle trackStyle, ChartRenderStyle valueStyle, ChartRenderStyle needleStyle)
    {
        if (scene == null)
        {
            throw new System.ArgumentNullException("scene");
        }

        ChartPlot.ValidateId(id);
        ChartPieChartsOperations.ValidateLayoutPoint(centerX, centerY);
        ChartPieSlice.ValidateRadii(innerRadius, outerRadius);
        ChartLinearScale.ValidateDomain(minimumValue, maximumValue);
        ChartLinearScale.ValidateFinite(value, "value");
        ChartLinearScale.ValidateFinite(startAngle, "startAngle");
        ChartLinearScale.ValidateFinite(endAngle, "endAngle");
        ChartLineAreaChartsOperations.ValidateStyle(trackStyle);
        ChartLineAreaChartsOperations.ValidateStyle(valueStyle);
        ChartLineAreaChartsOperations.ValidateStyle(needleStyle);
        double amount = GaugeAmount(minimumValue, maximumValue, value);
        double valueAngle = startAngle + (endAngle - startAngle) * amount;
        scene.AddPath(id + "-track", CreateAnnularSectorPath(centerX, centerY, innerRadius, outerRadius, startAngle, endAngle), trackStyle);
        scene.AddPath(id + "-value", CreateAnnularSectorPath(centerX, centerY, innerRadius, outerRadius, startAngle, valueAngle), valueStyle);
        ChartPoint needle = ChartPieChartsOperations.PointAtAngle(centerX, centerY, valueAngle, outerRadius);
        scene.AddLine(id + "-needle", centerX, centerY, needle.X, needle.Y, needleStyle);
    }

    /// <summary>
    /// Adds the supplied bullet to this object.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "x">The x.</param>
    /// <param name = "y">The y.</param>
    /// <param name = "width">The width in pixels or chart units, which must be positive.</param>
    /// <param name = "height">The height in pixels or chart units, which must be positive.</param>
    /// <param name = "minimumValue">The minimum value.</param>
    /// <param name = "maximumValue">The maximum value.</param>
    /// <param name = "value">The value.</param>
    /// <param name = "targetValue">The target value.</param>
    /// <param name = "orientation">The orientation.</param>
    /// <param name = "backgroundStyle">The rendering style used for background.</param>
    /// <param name = "valueStyle">The rendering style used for value.</param>
    /// <param name = "targetStyle">The rendering style used for target.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static void AddBullet(IChartRenderTarget scene, string id, double x, double y, double width, double height, double minimumValue, double maximumValue, double value, double targetValue, ChartOrientation orientation, ChartRenderStyle backgroundStyle, ChartRenderStyle valueStyle, ChartRenderStyle targetStyle)
    {
        if (scene == null)
        {
            throw new System.ArgumentNullException("scene");
        }

        ChartPlot.ValidateId(id);
        ChartRenderStyle.ValidatePositive(width, "width");
        ChartRenderStyle.ValidatePositive(height, "height");
        ChartLinearScale.ValidateDomain(minimumValue, maximumValue);
        ChartLinearScale.ValidateFinite(value, "value");
        ChartLinearScale.ValidateFinite(targetValue, "targetValue");
        ValidateBulletOrientation(orientation);
        ChartLineAreaChartsOperations.ValidateStyle(backgroundStyle);
        ChartLineAreaChartsOperations.ValidateStyle(valueStyle);
        ChartLineAreaChartsOperations.ValidateStyle(targetStyle);
        scene.AddRect(id + "-background", x, y, width, height, 0.0, 0.0, backgroundStyle);
        if (orientation == ChartOrientation.Horizontal)
        {
            AddHorizontalBulletValue(scene, id, x, y, width, height, minimumValue, maximumValue, value, targetValue, valueStyle, targetStyle);
        }
        else
        {
            AddVerticalBulletValue(scene, id, x, y, width, height, minimumValue, maximumValue, value, targetValue, valueStyle, targetStyle);
        }
    }

    internal static ChartPath CreatePolarBarPath(ChartPolarTransform transform, double angle, double value, double angleWidth, double baselineValue)
    {
        if (transform == null)
        {
            throw new System.ArgumentNullException("transform");
        }

        ChartLinearScale.ValidateFinite(angle, "angle");
        ChartLinearScale.ValidateFinite(value, "value");
        ChartRenderStyle.ValidatePositive(angleWidth, "angleWidth");
        ChartLinearScale.ValidateFinite(baselineValue, "baselineValue");
        double startAngle = transform.MapAngle(angle - angleWidth * 0.5);
        double endAngle = transform.MapAngle(angle + angleWidth * 0.5);
        return CreateAnnularSectorPath(transform.CenterX, transform.CenterY, transform.MapRadius(baselineValue), transform.MapRadius(value), startAngle, endAngle);
    }

    internal static ChartPath CreateAnnularSectorPath(double centerX, double centerY, double innerRadius, double outerRadius, double startAngle, double endAngle)
    {
        ChartPieChartsOperations.ValidateLayoutPoint(centerX, centerY);
        ChartStyle.ValidateNonNegative(innerRadius, "innerRadius");
        ChartStyle.ValidateNonNegative(outerRadius, "outerRadius");
        ChartLinearScale.ValidateFinite(startAngle, "startAngle");
        ChartLinearScale.ValidateFinite(endAngle, "endAngle");
        double lowRadius = innerRadius;
        double highRadius = outerRadius;
        if (highRadius < lowRadius)
        {
            lowRadius = outerRadius;
            highRadius = innerRadius;
        }

        ChartPath path = new ChartPath();
        if (highRadius == 0.0)
        {
            path.MoveTo(centerX, centerY);
            path.Close();
            return path;
        }

        int segments = ChartPieChartsOperations.ArcSegmentCount(startAngle, endAngle);
        if (lowRadius == 0.0)
        {
            path.MoveTo(centerX, centerY);
            ChartPieChartsOperations.AddArcPoints(path, centerX, centerY, highRadius, startAngle, endAngle, segments, false);
            path.Close();
            return path;
        }

        ChartPieChartsOperations.AddArcPoints(path, centerX, centerY, highRadius, startAngle, endAngle, segments, true);
        ChartPieChartsOperations.AddArcPoints(path, centerX, centerY, lowRadius, endAngle, startAngle, segments, false);
        path.Close();
        return path;
    }

    internal static double GaugeAmount(double minimumValue, double maximumValue, double value)
    {
        ChartLinearScale.ValidateDomain(minimumValue, maximumValue);
        ChartLinearScale.ValidateFinite(value, "value");
        double amount = (value - minimumValue) / (maximumValue - minimumValue);
        return ChartScaleUtilities.ClampUnit(amount);
    }

    internal static void AddHorizontalBulletValue(IChartRenderTarget scene, string id, double x, double y, double width, double height, double minimumValue, double maximumValue, double value, double targetValue, ChartRenderStyle valueStyle, ChartRenderStyle targetStyle)
    {
        double valueAmount = GaugeAmount(minimumValue, maximumValue, value);
        double targetAmount = GaugeAmount(minimumValue, maximumValue, targetValue);
        double valueWidth = width * valueAmount;
        double targetX = x + width * targetAmount;
        if (valueWidth > 0.0)
        {
            scene.AddRect(id + "-value", x, y + height * 0.25, valueWidth, height * 0.5, 0.0, 0.0, valueStyle);
        }

        scene.AddLine(id + "-target", targetX, y, targetX, y + height, targetStyle);
    }

    internal static void AddVerticalBulletValue(IChartRenderTarget scene, string id, double x, double y, double width, double height, double minimumValue, double maximumValue, double value, double targetValue, ChartRenderStyle valueStyle, ChartRenderStyle targetStyle)
    {
        double valueAmount = GaugeAmount(minimumValue, maximumValue, value);
        double targetAmount = GaugeAmount(minimumValue, maximumValue, targetValue);
        double valueHeight = height * valueAmount;
        double valueY = y + height - valueHeight;
        double targetY = y + height - height * targetAmount;
        if (valueHeight > 0.0)
        {
            scene.AddRect(id + "-value", x + width * 0.25, valueY, width * 0.5, valueHeight, 0.0, 0.0, valueStyle);
        }

        scene.AddLine(id + "-target", x, targetY, x + width, targetY, targetStyle);
    }

    internal static void ValidateSceneIdTransformStyle(IChartRenderTarget scene, string id, ChartPolarTransform transform, ChartRenderStyle style)
    {
        if (scene == null)
        {
            throw new System.ArgumentNullException("scene");
        }

        ChartPlot.ValidateId(id);
        if (transform == null)
        {
            throw new System.ArgumentNullException("transform");
        }

        ChartLineAreaChartsOperations.ValidateStyle(style);
    }

    internal static void ValidateAngleRadiusArrays(double[] angleValues, double[] radiusValues, int minimumLength)
    {
        ChartCartesianTransform.ValidateValueArrays(angleValues, radiusValues, minimumLength);
    }

    internal static void ValidateBulletOrientation(ChartOrientation orientation)
    {
        if (orientation != ChartOrientation.Horizontal && orientation != ChartOrientation.Vertical)
        {
            throw new System.ArgumentOutOfRangeException("orientation");
        }
    }
}
