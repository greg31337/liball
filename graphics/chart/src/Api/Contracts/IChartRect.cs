namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartRect"/>.</summary>
public interface IChartRect
{
    /// <summary>
    /// Applies the left operation to the supplied data and returns the computed result.
    /// </summary>
    /// <returns>The resulting double.</returns>
    double Left();
    /// <summary>
    /// Applies the right operation to the supplied data and returns the computed result.
    /// </summary>
    /// <returns>The resulting double.</returns>
    double Right();
    /// <summary>
    /// Converts this value to p.
    /// </summary>
    /// <returns>The converted double.</returns>
    double Top();
    /// <summary>
    /// Applies the bottom operation to the supplied data and returns the computed result.
    /// </summary>
    /// <returns>The resulting double.</returns>
    double Bottom();
    /// <summary>
    /// Applies the center operation to the supplied data and returns the computed result.
    /// </summary>
    /// <returns>The resulting point.</returns>
    ChartPoint Center();
    /// <summary>
    /// Determines whether this value is empty.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    bool IsEmpty();
    /// <summary>
    /// Determines whether this value contains point.
    /// </summary>
    /// <param name = "point">The point.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    bool ContainsPoint(ChartPoint point);
    /// <summary>
    /// Computes the intersection with s using the supplied data and configuration.
    /// </summary>
    /// <param name = "rect">The rect.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    bool Intersects(ChartRect rect);
    /// <summary>
    /// Computes the intersection with ion using the supplied data and configuration.
    /// </summary>
    /// <param name = "rect">The rect.</param>
    /// <returns>The resulting rect.</returns>
    ChartRect Intersection(ChartRect rect);
    /// <summary>
    /// Computes the union with the configured value using the supplied data and configuration.
    /// </summary>
    /// <param name = "rect">The rect.</param>
    /// <returns>The resulting rect.</returns>
    ChartRect Union(ChartRect rect);
    /// <summary>
    /// Applies the translate operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "dx">The dx.</param>
    /// <param name = "dy">The dy.</param>
    /// <returns>The resulting rect.</returns>
    ChartRect Translate(double dx, double dy);
    /// <summary>
    /// Expands the configured value using the supplied data and configuration.
    /// </summary>
    /// <param name = "dx">The dx.</param>
    /// <param name = "dy">The dy.</param>
    /// <returns>The resulting rect.</returns>
    ChartRect Inflate(double dx, double dy);
    /// <summary>
    /// Shrinks the configured value using the supplied data and configuration.
    /// </summary>
    /// <param name = "dx">The dx.</param>
    /// <param name = "dy">The dy.</param>
    /// <returns>The resulting rect.</returns>
    ChartRect Deflate(double dx, double dy);
    /// <summary>Returns the current x.</summary>
    /// <returns>The current field value.</returns>
    double GetX();
    /// <summary>Replaces the current x.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetX(double value);
    /// <summary>Returns the current y.</summary>
    /// <returns>The current field value.</returns>
    double GetY();
    /// <summary>Replaces the current y.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetY(double value);
    /// <summary>Returns the current width.</summary>
    /// <returns>The current field value.</returns>
    double GetWidth();
    /// <summary>Replaces the current width.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetWidth(double value);
    /// <summary>Returns the current height.</summary>
    /// <returns>The current field value.</returns>
    double GetHeight();
    /// <summary>Replaces the current height.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetHeight(double value);
}
