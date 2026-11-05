namespace chart;
/// <summary>Provides the public chart polar charts operations.</summary>
public class ChartPolarCharts : IChartPolarCharts
{
    /// <summary>Initializes a stateless chart polar charts facade.</summary>
    public ChartPolarCharts()
    {
    }

    /// <summary>Creates a polar line scene directly from values.</summary>
    /// <param name = "id">The command identifier.</param><param name = "width">The scene width.</param><param name = "height">The scene height.</param>
    /// <param name = "angleValues">The angle values.</param><param name = "radiusValues">The radius values.</param><param name = "transform">The polar transform.</param>
    /// <param name = "closed">Whether the line is closed.</param><param name = "style">The render style.</param>
    /// <returns>The newly created render scene.</returns>
    public ChartRenderScene CreatePolarLineScene(string id, int width, int height, double[] angleValues, double[] radiusValues, ChartPolarTransform transform, bool closed, ChartRenderStyle style)
    {
        return ChartPolarChartsOperations.CreatePolarLineScene(id, width, height, angleValues, radiusValues, transform, closed, style);
    }

    /// <summary>Creates a polar scatter scene directly from values.</summary>
    /// <param name = "id">The command identifier.</param><param name = "width">The scene width.</param><param name = "height">The scene height.</param>
    /// <param name = "angleValues">The angle values.</param><param name = "radiusValues">The radius values.</param><param name = "transform">The polar transform.</param>
    /// <param name = "shape">The marker shape.</param><param name = "markerSize">The marker size.</param><param name = "style">The render style.</param>
    /// <returns>The newly created render scene.</returns>
    public ChartRenderScene CreatePolarScatterScene(string id, int width, int height, double[] angleValues, double[] radiusValues, ChartPolarTransform transform, ChartMarkerShape shape, double markerSize, ChartRenderStyle style)
    {
        return ChartPolarChartsOperations.CreatePolarScatterScene(id, width, height, angleValues, radiusValues, transform, shape, markerSize, style);
    }

    /// <summary>Creates a polar bar scene directly from values.</summary>
    /// <param name = "id">The command identifier.</param><param name = "width">The scene width.</param><param name = "height">The scene height.</param>
    /// <param name = "angleValues">The angle values.</param><param name = "radiusValues">The radius values.</param><param name = "transform">The polar transform.</param>
    /// <param name = "angleWidth">The angular bar width.</param><param name = "baselineValue">The radial baseline.</param><param name = "style">The render style.</param>
    /// <returns>The newly created render scene.</returns>
    public ChartRenderScene CreatePolarBarScene(string id, int width, int height, double[] angleValues, double[] radiusValues, ChartPolarTransform transform, double angleWidth, double baselineValue, ChartRenderStyle style)
    {
        return ChartPolarChartsOperations.CreatePolarBarScene(id, width, height, angleValues, radiusValues, transform, angleWidth, baselineValue, style);
    }

    /// <summary>Creates a radar scene directly from values.</summary>
    /// <param name = "id">The command identifier.</param><param name = "width">The scene width.</param><param name = "height">The scene height.</param>
    /// <param name = "values">The radial values.</param><param name = "transform">The polar transform.</param><param name = "style">The render style.</param>
    /// <returns>The newly created render scene.</returns>
    public ChartRenderScene CreateRadarScene(string id, int width, int height, double[] values, ChartPolarTransform transform, ChartRenderStyle style)
    {
        return ChartPolarChartsOperations.CreateRadarScene(id, width, height, values, transform, style);
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
    public void AddPolarLine(IChartRenderTarget scene, string id, double[] angleValues, double[] radiusValues, ChartPolarTransform transform, bool closed, ChartRenderStyle style)
    {
        ChartPolarChartsOperations.AddPolarLine(scene, id, angleValues, radiusValues, transform, closed, style);
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
    public void AddPolarScatter(IChartRenderTarget scene, string id, double[] angleValues, double[] radiusValues, ChartPolarTransform transform, ChartMarkerShape shape, double markerSize, ChartRenderStyle style)
    {
        ChartPolarChartsOperations.AddPolarScatter(scene, id, angleValues, radiusValues, transform, shape, markerSize, style);
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
    public void AddPolarBars(IChartRenderTarget scene, string id, double[] angleValues, double[] radiusValues, ChartPolarTransform transform, double angleWidth, double baselineValue, ChartRenderStyle style)
    {
        ChartPolarChartsOperations.AddPolarBars(scene, id, angleValues, radiusValues, transform, angleWidth, baselineValue, style);
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
    public void AddRadar(IChartRenderTarget scene, string id, double[] values, ChartPolarTransform transform, ChartRenderStyle style)
    {
        ChartPolarChartsOperations.AddRadar(scene, id, values, transform, style);
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
    public void AddRadarSpokes(IChartRenderTarget scene, string id, int spokeCount, ChartPolarTransform transform, double value, ChartRenderStyle style)
    {
        ChartPolarChartsOperations.AddRadarSpokes(scene, id, spokeCount, transform, value, style);
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
    public ChartRenderScene CreateGaugeScene(string id, int width, int height, double centerX, double centerY, double innerRadius, double outerRadius, double minimumValue, double maximumValue, double value, double startAngle, double endAngle, ChartRenderStyle trackStyle, ChartRenderStyle valueStyle, ChartRenderStyle needleStyle)
    {
        return ChartPolarChartsOperations.CreateGaugeScene(id, width, height, centerX, centerY, innerRadius, outerRadius, minimumValue, maximumValue, value, startAngle, endAngle, trackStyle, valueStyle, needleStyle);
    }

    /// <summary>Creates a bullet chart scene directly from values.</summary>
    /// <param name = "id">The command identifier.</param><param name = "width">The scene width.</param><param name = "height">The scene height.</param>
    /// <param name = "x">The bullet x position.</param><param name = "y">The bullet y position.</param><param name = "bulletWidth">The bullet width.</param><param name = "bulletHeight">The bullet height.</param>
    /// <param name = "minimumValue">The minimum value.</param><param name = "maximumValue">The maximum value.</param><param name = "value">The displayed value.</param><param name = "targetValue">The target value.</param>
    /// <param name = "orientation">The bullet orientation.</param><param name = "backgroundStyle">The background style.</param><param name = "valueStyle">The value style.</param><param name = "targetStyle">The target style.</param>
    /// <returns>The newly created render scene.</returns>
    public ChartRenderScene CreateBulletScene(string id, int width, int height, double x, double y, double bulletWidth, double bulletHeight, double minimumValue, double maximumValue, double value, double targetValue, ChartOrientation orientation, ChartRenderStyle backgroundStyle, ChartRenderStyle valueStyle, ChartRenderStyle targetStyle)
    {
        return ChartPolarChartsOperations.CreateBulletScene(id, width, height, x, y, bulletWidth, bulletHeight, minimumValue, maximumValue, value, targetValue, orientation, backgroundStyle, valueStyle, targetStyle);
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
    public void AddGauge(IChartRenderTarget scene, string id, double centerX, double centerY, double innerRadius, double outerRadius, double minimumValue, double maximumValue, double value, double startAngle, double endAngle, ChartRenderStyle trackStyle, ChartRenderStyle valueStyle, ChartRenderStyle needleStyle)
    {
        ChartPolarChartsOperations.AddGauge(scene, id, centerX, centerY, innerRadius, outerRadius, minimumValue, maximumValue, value, startAngle, endAngle, trackStyle, valueStyle, needleStyle);
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
    public void AddBullet(IChartRenderTarget scene, string id, double x, double y, double width, double height, double minimumValue, double maximumValue, double value, double targetValue, ChartOrientation orientation, ChartRenderStyle backgroundStyle, ChartRenderStyle valueStyle, ChartRenderStyle targetStyle)
    {
        ChartPolarChartsOperations.AddBullet(scene, id, x, y, width, height, minimumValue, maximumValue, value, targetValue, orientation, backgroundStyle, valueStyle, targetStyle);
    }
}
