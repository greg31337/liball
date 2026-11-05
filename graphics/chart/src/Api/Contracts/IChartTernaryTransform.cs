namespace chart;
/// <summary>Defines transforms between three-component ternary coordinates and chart points.</summary>
public interface IChartTernaryTransform
{
    /// <summary>
    /// Maps three nonnegative components to a point in the configured triangle.
    /// </summary>
    /// <param name = "first">The first component.</param>
    /// <param name = "second">The second component.</param>
    /// <param name = "third">The third component.</param>
    /// <returns>The mapped point.</returns>
    ChartPoint Map(double first, double second, double third);
    /// <summary>
    /// Inverts a point to normalized first, second, and third components whose sum is one.
    /// </summary>
    /// <param name = "point">The point to invert.</param>
    /// <returns>The three normalized components.</returns>
    double[] Invert(ChartPoint point);
    /// <summary>Returns a copy of the vertex representing a full first component.</summary>
    /// <returns>The first vertex.</returns>
    ChartPoint GetFirstVertex();
    /// <summary>Returns a copy of the vertex representing a full second component.</summary>
    /// <returns>The second vertex.</returns>
    ChartPoint GetSecondVertex();
    /// <summary>Returns a copy of the vertex representing a full third component.</summary>
    /// <returns>The third vertex.</returns>
    ChartPoint GetThirdVertex();
}
