namespace chart;
/// <summary>
/// Maps three-component ternary coordinates to and from a nondegenerate triangle.
/// </summary>
public class ChartTernaryTransform : IChartTernaryTransform
{
    internal ChartPoint FirstVertex;
    internal ChartPoint SecondVertex;
    internal ChartPoint ThirdVertex;
    internal double Determinant;
    /// <summary>
    /// Initializes a ternary transform from the vertices representing full first, second, and third components.
    /// </summary>
    /// <param name = "firstVertex">The vertex representing a full first component.</param>
    /// <param name = "secondVertex">The vertex representing a full second component.</param>
    /// <param name = "thirdVertex">The vertex representing a full third component.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a vertex is <c>null</c>.</exception>
    /// <exception cref = "System.ArgumentException">Thrown when a vertex is not finite or the triangle is degenerate.</exception>
    public ChartTernaryTransform(ChartPoint firstVertex, ChartPoint secondVertex, ChartPoint thirdVertex)
    {
        ValidateVertex(firstVertex, "firstVertex");
        ValidateVertex(secondVertex, "secondVertex");
        ValidateVertex(thirdVertex, "thirdVertex");
        double determinant = (secondVertex.Y - thirdVertex.Y) * (firstVertex.X - thirdVertex.X) + (thirdVertex.X - secondVertex.X) * (firstVertex.Y - thirdVertex.Y);
        if (System.Math.Abs(determinant) <= 0.000000000001)
        {
            throw new System.ArgumentException("Ternary transform vertices must form a nondegenerate triangle.");
        }

        FirstVertex = firstVertex.Copy();
        SecondVertex = secondVertex.Copy();
        ThirdVertex = thirdVertex.Copy();
        Determinant = determinant;
    }

    /// <summary>
    /// Maps three nonnegative components to a point in the configured triangle.
    /// </summary>
    /// <param name = "first">The first component.</param>
    /// <param name = "second">The second component.</param>
    /// <param name = "third">The third component.</param>
    /// <returns>The mapped point.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when a component is negative.</exception>
    /// <exception cref = "System.ArgumentException">Thrown when the components do not have a positive finite sum.</exception>
    public ChartPoint Map(double first, double second, double third)
    {
        ValidateComponent(first, "first");
        ValidateComponent(second, "second");
        ValidateComponent(third, "third");
        double total = first + second + third;
        if (double.IsNaN(total) || double.IsInfinity(total) || total <= 0.0)
        {
            throw new System.ArgumentException("Ternary components must have a positive finite sum.");
        }

        double firstWeight = first / total;
        double secondWeight = second / total;
        double thirdWeight = third / total;
        double x = FirstVertex.X * firstWeight + SecondVertex.X * secondWeight + ThirdVertex.X * thirdWeight;
        double y = FirstVertex.Y * firstWeight + SecondVertex.Y * secondWeight + ThirdVertex.Y * thirdWeight;
        return new ChartPoint(x, y);
    }

    /// <summary>
    /// Inverts a point to normalized first, second, and third components whose sum is one.
    /// </summary>
    /// <param name = "point">The point to invert.</param>
    /// <returns>The three normalized components.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when <c>point</c> is <c>null</c>.</exception>
    /// <exception cref = "System.ArgumentException">Thrown when the point is not finite.</exception>
    public double[] Invert(ChartPoint point)
    {
        ValidateVertex(point, "point");
        double first = ((SecondVertex.Y - ThirdVertex.Y) * (point.X - ThirdVertex.X) + (ThirdVertex.X - SecondVertex.X) * (point.Y - ThirdVertex.Y)) / Determinant;
        double second = ((ThirdVertex.Y - FirstVertex.Y) * (point.X - ThirdVertex.X) + (FirstVertex.X - ThirdVertex.X) * (point.Y - ThirdVertex.Y)) / Determinant;
        double third = 1.0 - first - second;
        return new double[] { first, second, third };
    }

    /// <summary>Returns a copy of the vertex representing a full first component.</summary>
    /// <returns>The first vertex.</returns>
    public ChartPoint GetFirstVertex()
    {
        return FirstVertex.Copy();
    }

    /// <summary>Returns a copy of the vertex representing a full second component.</summary>
    /// <returns>The second vertex.</returns>
    public ChartPoint GetSecondVertex()
    {
        return SecondVertex.Copy();
    }

    /// <summary>Returns a copy of the vertex representing a full third component.</summary>
    /// <returns>The third vertex.</returns>
    public ChartPoint GetThirdVertex()
    {
        return ThirdVertex.Copy();
    }

    internal static void ValidateVertex(ChartPoint point, string name)
    {
        if (point == null)
        {
            throw new System.ArgumentNullException(name);
        }

        if (double.IsNaN(point.X) || double.IsInfinity(point.X) || double.IsNaN(point.Y) || double.IsInfinity(point.Y))
        {
            throw new System.ArgumentException("Ternary transform points must be finite.", name);
        }
    }

    internal static void ValidateComponent(double component, string name)
    {
        if (double.IsNaN(component) || double.IsInfinity(component))
        {
            throw new System.ArgumentException("Ternary components must be finite.", name);
        }

        if (component < 0.0)
        {
            throw new System.ArgumentOutOfRangeException(name, "Ternary components cannot be negative.");
        }
    }
}
