namespace chart;
/// <summary>Defines missing-value processing across values, rows, tables, transforms, domains, and chart builders.</summary>
public interface IChartMissingValues
{
    /// <summary>Resolves one numeric array with the requested policy.</summary>
    /// <param name = "values">The numeric values.</param>
    /// <param name = "policy">The missing-value policy.</param>
    /// <returns>The resolved values.</returns>
    double[] ResolveValues(double[] values, ChartMissingValuePolicy policy);
    /// <summary>Resolves aligned x and y arrays while preserving row alignment.</summary>
    /// <param name = "xValues">The horizontal values.</param>
    /// <param name = "yValues">The vertical values.</param>
    /// <param name = "policy">The missing-value policy.</param>
    /// <returns>The resolved points.</returns>
    ChartPoint[] ResolvePoints(double[] xValues, double[] yValues, ChartMissingValuePolicy policy);
    /// <summary>Resolves and maps aligned coordinates through a cartesian transform.</summary>
    /// <param name = "transform">The cartesian transform.</param>
    /// <param name = "xValues">The horizontal values.</param>
    /// <param name = "yValues">The vertical values.</param>
    /// <param name = "policy">The missing-value policy.</param>
    /// <returns>The transformed points.</returns>
    ChartPoint[] TransformPoints(ChartCartesianTransform transform, double[] xValues, double[] yValues, ChartMissingValuePolicy policy);
    /// <summary>Resolves every numeric column in a table.</summary>
    /// <param name = "table">The source table.</param>
    /// <param name = "policy">The missing-value policy.</param>
    /// <returns>A new resolved table.</returns>
    ChartDataTable ResolveTable(ChartDataTable table, ChartMissingValuePolicy policy);
    /// <summary>Computes a finite domain after applying a missing-value policy.</summary>
    /// <param name = "values">The numeric values.</param>
    /// <param name = "policy">The missing-value policy.</param>
    /// <returns>The resolved finite domain.</returns>
    ChartRange ResolveDomain(double[] values, ChartMissingValuePolicy policy);
    /// <summary>Creates a policy-aware line series.</summary>
    /// <param name = "id">The series identifier.</param>
    /// <param name = "xValues">The horizontal values.</param>
    /// <param name = "yValues">The vertical values.</param>
    /// <param name = "policy">The missing-value policy.</param>
    /// <param name = "style">The rendering style.</param>
    /// <returns>The resulting series.</returns>
    ChartCartesianSeries CreateLineSeries(string id, double[] xValues, double[] yValues, ChartMissingValuePolicy policy, ChartRenderStyle style);
    /// <summary>Creates a policy-aware scatter series.</summary>
    /// <param name = "id">The series identifier.</param>
    /// <param name = "xValues">The horizontal values.</param>
    /// <param name = "yValues">The vertical values.</param>
    /// <param name = "policy">The missing-value policy.</param>
    /// <param name = "shape">The marker shape.</param>
    /// <param name = "markerSize">The marker size.</param>
    /// <param name = "style">The rendering style.</param>
    /// <returns>The resulting series.</returns>
    ChartCartesianSeries CreateScatterSeries(string id, double[] xValues, double[] yValues, ChartMissingValuePolicy policy, ChartMarkerShape shape, double markerSize, ChartRenderStyle style);
    /// <summary>Creates a policy-aware vertical bar series.</summary>
    /// <param name = "id">The series identifier.</param>
    /// <param name = "xValues">The horizontal values.</param>
    /// <param name = "yValues">The vertical values.</param>
    /// <param name = "policy">The missing-value policy.</param>
    /// <param name = "barWidth">The bar width.</param>
    /// <param name = "baseline">The baseline.</param>
    /// <param name = "style">The rendering style.</param>
    /// <returns>The resulting series.</returns>
    ChartCartesianSeries CreateVerticalBarSeries(string id, double[] xValues, double[] yValues, ChartMissingValuePolicy policy, double barWidth, double baseline, ChartRenderStyle style);
}
