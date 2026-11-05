using System;

namespace random;

/// <summary>
/// Triangulates and uniformly samples finite simple polygons.
/// </summary>
/// <remarks>
/// Vertices are supplied as consecutive <c>x, y</c> coordinate pairs. Construction validates
/// the complete boundary, performs deterministic ear-clipping triangulation, and defensively
/// copies the coordinates. The sampler retains the supplied mutable generator and requires
/// external synchronization when shared between callers.
/// </remarks>
public class PolytopeSampler : IPolytopeSampler
{
    const double MachineEpsilon = 2.220446049250313080847263336181640625e-16;
    IRandomSource generator;
    double[] vertices;
    int[] triangleIndexes;
    double[] cumulativeAreas;

    /// <summary>Creates a reusable uniform sampler for a finite simple polygon.</summary>
    /// <param name="generator">The generator that supplies triangle and barycentric selections.</param>
    /// <param name="vertices">At least three finite <c>x, y</c> vertex pairs in boundary order.</param>
    /// <remarks>Clockwise and counterclockwise boundaries are accepted. The input array is copied.</remarks>
    /// <exception cref="RandomArgumentException">The generator or vertices are null, coordinate storage is invalid or non-finite, or the polygon is degenerate, repeated, or self-intersecting.</exception>
    public PolytopeSampler(IRandomSource generator, double[] vertices)
    {
        Guard.NotNull(generator, "generator");
        int[] validatedTriangles = ValidateAndTriangulate(vertices);
        double[] normalized = Normalize(vertices);
        double[] areas = TriangleAreas(normalized, validatedTriangles);
        CategoricalProbabilityTable table = new CategoricalProbabilityTable(areas, "vertices");
        double[] cumulative = new double[areas.Length];
        int index = 0;
        while (index < cumulative.Length)
        {
            cumulative[index] = table.CumulativeProbability(index);
            index++;
        }

        this.generator = generator;
        this.vertices = (double[])vertices.Clone();
        triangleIndexes = validatedTriangles;
        cumulativeAreas = cumulative;
    }

    /// <summary>Returns a deterministic ear-clipping triangulation of a finite simple polygon.</summary>
    /// <param name="vertices">At least three finite <c>x, y</c> vertex pairs in boundary order.</param>
    /// <returns>Three original vertex indexes per triangle, preserving boundary orientation.</returns>
    /// <exception cref="RandomArgumentException">The vertices are null, coordinate storage is invalid or non-finite, or the polygon is degenerate, repeated, or self-intersecting.</exception>
    internal static int[] Triangulate(double[] vertices)
    {
        return ValidateAndTriangulate(vertices);
    }

    /// <summary>Returns the polygon vertex count.</summary>
    /// <returns>The number of boundary vertices.</returns>
    public int VertexCount()
    {
        return vertices.Length / 2;
    }

    /// <summary>Returns the triangulation triangle count.</summary>
    /// <returns>Exactly <see cref="VertexCount()"/> minus two.</returns>
    public int TriangleCount()
    {
        return triangleIndexes.Length / 3;
    }

    /// <summary>Returns a defensive copy of the triangulation indexes.</summary>
    /// <returns>Three original polygon vertex indexes per triangle.</returns>
    public int[] TriangleIndexes()
    {
        return (int[])triangleIndexes.Clone();
    }

    /// <summary>Returns the required row-major storage for a point batch.</summary>
    /// <param name="sampleCount">The nonnegative number of two-coordinate points.</param>
    /// <returns>Exactly twice the sample count.</returns>
    /// <exception cref="RandomArgumentException">The sample count is negative or the required storage exceeds <c>int.MaxValue</c>.</exception>
    public int RequiredOutputLength(int sampleCount)
    {
        Guard.Count(sampleCount, "sampleCount");
        if (sampleCount > int.MaxValue / 2)
        {
            throw RandomArgumentException.Create("sampleCount", "The requested batch contains too many coordinates.");
        }

        return sampleCount * 2;
    }

    /// <summary>Returns one uniformly sampled point inside the polygon.</summary>
    /// <returns>Two coordinates in a new array.</returns>
    public double[] Sample()
    {
        double[] result = new double[2];
        SampleInto(result);
        return result;
    }

    /// <summary>Writes one uniformly sampled point inside the polygon.</summary>
    /// <param name="destination">The destination of exactly two coordinates.</param>
    /// <exception cref="RandomArgumentException">The destination length is not two.</exception>
    public void SampleInto(Span<double> destination)
    {
        Guard.ExactSpanLength(destination.Length, 2, "destination");
        SampleUnchecked(destination, 0);
    }

    /// <summary>Fills a row-major batch with uniformly sampled polygon points.</summary>
    /// <param name="sampleCount">The nonnegative number of points.</param>
    /// <param name="destination">The destination of exactly <see cref="RequiredOutputLength(int)"/> coordinates.</param>
    /// <remarks>Each point occupies consecutive <c>x, y</c> entries and matches repeated calls to <see cref="SampleInto(Span{double})"/>.</remarks>
    /// <exception cref="RandomArgumentException">The sample count is invalid or the destination length does not match it.</exception>
    public void Fill(int sampleCount, Span<double> destination)
    {
        int requiredLength = RequiredOutputLength(sampleCount);
        Guard.ExactSpanLength(destination.Length, requiredLength, "destination");
        int sampleIndex = 0;
        int offset = 0;
        while (sampleIndex < sampleCount)
        {
            SampleUnchecked(destination, offset);
            offset += 2;
            sampleIndex++;
        }
    }

    void SampleUnchecked(Span<double> destination, int offset)
    {
        int triangle = SelectTriangle();
        int triangleOffset = triangle * 3;
        int first = triangleIndexes[triangleOffset];
        int second = triangleIndexes[triangleOffset + 1];
        int third = triangleIndexes[triangleOffset + 2];
        double firstWeight = FloatingUniform.NextDoubleClosedOpen(generator);
        double secondWeight = FloatingUniform.NextDoubleClosedOpen(generator);
        if (firstWeight + secondWeight > 1.0)
        {
            firstWeight = 1.0 - firstWeight;
            secondWeight = 1.0 - secondWeight;
        }

        double originWeight = 1.0 - firstWeight - secondWeight;
        destination[offset] = WeightedCoordinate(first, second, third, 0, originWeight, firstWeight, secondWeight);
        destination[offset + 1] = WeightedCoordinate(first, second, third, 1, originWeight, firstWeight, secondWeight);
    }

    int SelectTriangle()
    {
        if (cumulativeAreas.Length == 1)
        {
            return 0;
        }

        double unit = FloatingUniform.NextDoubleClosedOpen(generator);
        int triangle = 0;
        while (triangle < cumulativeAreas.Length - 1 && unit >= cumulativeAreas[triangle])
        {
            triangle++;
        }

        return triangle;
    }

    double WeightedCoordinate(int first, int second, int third, int coordinate, double firstWeight, double secondWeight, double thirdWeight)
    {
        double firstValue = vertices[first * 2 + coordinate];
        double secondValue = vertices[second * 2 + coordinate];
        double thirdValue = vertices[third * 2 + coordinate];
        double maximumMagnitude = Math.Max(Math.Abs(firstValue), Math.Max(Math.Abs(secondValue), Math.Abs(thirdValue)));
        if (maximumMagnitude == 0.0)
        {
            return 0.0;
        }

        double value = firstWeight * (firstValue / maximumMagnitude) +
            secondWeight * (secondValue / maximumMagnitude) +
            thirdWeight * (thirdValue / maximumMagnitude);
        double result = value * maximumMagnitude;
        double minimum = Math.Min(firstValue, Math.Min(secondValue, thirdValue));
        double maximum = Math.Max(firstValue, Math.Max(secondValue, thirdValue));
        if (result < minimum)
        {
            return minimum;
        }

        if (result > maximum)
        {
            return maximum;
        }

        return result;
    }

    static int[] ValidateAndTriangulate(double[] vertices)
    {
        Guard.NotNull(vertices, "vertices");
        if (vertices.Length < 6 || (vertices.Length & 1) != 0)
        {
            throw RandomArgumentException.Create("vertices", "A polygon requires at least three complete coordinate pairs.");
        }

        int coordinate = 0;
        while (coordinate < vertices.Length)
        {
            if (!double.IsFinite(vertices[coordinate]))
            {
                throw RandomArgumentException.Create("vertices", "Every polygon coordinate must be finite.");
            }

            coordinate++;
        }

        int vertexCount = vertices.Length / 2;
        if (vertexCount - 2 > int.MaxValue / 3)
        {
            throw RandomArgumentException.Create("vertices", "The polygon triangulation would contain too many indexes.");
        }

        double[] normalized = Normalize(vertices);
        double tolerance = Math.Min(1.0e-10, (double)vertexCount * 64.0 * MachineEpsilon);
        ValidateDistinctVertices(normalized, vertexCount);
        ValidateSimpleBoundary(normalized, vertexCount, tolerance);
        double signedArea = SignedDoubleArea(normalized, vertexCount);
        if (Math.Abs(signedArea) <= tolerance)
        {
            throw RandomArgumentException.Create("vertices", "The polygon must have nonzero area.");
        }

        return EarClip(normalized, vertexCount, signedArea > 0.0 ? 1.0 : -1.0, tolerance);
    }

    static double[] Normalize(double[] vertices)
    {
        int vertexCount = vertices.Length / 2;
        double originX = vertices[0];
        double originY = vertices[1];
        bool halve = false;
        int vertex = 1;
        while (vertex < vertexCount)
        {
            if (double.IsInfinity(vertices[vertex * 2] - originX) || double.IsInfinity(vertices[vertex * 2 + 1] - originY))
            {
                halve = true;
            }

            vertex++;
        }

        double[] normalized = new double[vertices.Length];
        double maximum = 0.0;
        vertex = 0;
        while (vertex < vertexCount)
        {
            double x;
            double y;
            if (halve)
            {
                x = vertices[vertex * 2] * 0.5 - originX * 0.5;
                y = vertices[vertex * 2 + 1] * 0.5 - originY * 0.5;
            }
            else
            {
                x = vertices[vertex * 2] - originX;
                y = vertices[vertex * 2 + 1] - originY;
            }

            normalized[vertex * 2] = x;
            normalized[vertex * 2 + 1] = y;
            maximum = Math.Max(maximum, Math.Max(Math.Abs(x), Math.Abs(y)));
            vertex++;
        }

        if (maximum == 0.0)
        {
            throw RandomArgumentException.Create("vertices", "The polygon vertices must not all be equal.");
        }

        int coordinate = 0;
        while (coordinate < normalized.Length)
        {
            normalized[coordinate] /= maximum;
            coordinate++;
        }

        return normalized;
    }

    static void ValidateDistinctVertices(double[] vertices, int vertexCount)
    {
        int first = 0;
        while (first < vertexCount)
        {
            int second = first + 1;
            while (second < vertexCount)
            {
                if (X(vertices, first) == X(vertices, second) && Y(vertices, first) == Y(vertices, second))
                {
                    throw RandomArgumentException.Create("vertices", "Polygon vertices must be distinct.");
                }

                second++;
            }

            first++;
        }
    }

    static void ValidateSimpleBoundary(double[] vertices, int vertexCount, double tolerance)
    {
        int vertex = 0;
        while (vertex < vertexCount)
        {
            int previous = (vertex + vertexCount - 1) % vertexCount;
            int next = (vertex + 1) % vertexCount;
            if (Math.Abs(Cross(vertices, previous, vertex, next)) <= tolerance)
            {
                double incomingX = X(vertices, vertex) - X(vertices, previous);
                double incomingY = Y(vertices, vertex) - Y(vertices, previous);
                double outgoingX = X(vertices, next) - X(vertices, vertex);
                double outgoingY = Y(vertices, next) - Y(vertices, vertex);
                double direction = incomingX * outgoingX + incomingY * outgoingY;
                if (direction < -tolerance)
                {
                    throw RandomArgumentException.Create("vertices", "Adjacent polygon edges must not reverse and overlap.");
                }
            }

            vertex++;
        }

        int firstEdge = 0;
        while (firstEdge < vertexCount)
        {
            int firstNext = (firstEdge + 1) % vertexCount;
            int secondEdge = firstEdge + 1;
            while (secondEdge < vertexCount)
            {
                int secondNext = (secondEdge + 1) % vertexCount;
                bool adjacent = firstNext == secondEdge || secondNext == firstEdge;
                if (!adjacent && SegmentsIntersect(vertices, firstEdge, firstNext, secondEdge, secondNext, tolerance))
                {
                    throw RandomArgumentException.Create("vertices", "The polygon boundary must not self-intersect or touch itself.");
                }

                secondEdge++;
            }

            firstEdge++;
        }
    }

    static bool SegmentsIntersect(double[] vertices, int first, int second, int third, int fourth, double tolerance)
    {
        double firstSide = Cross(vertices, first, second, third);
        double secondSide = Cross(vertices, first, second, fourth);
        double thirdSide = Cross(vertices, third, fourth, first);
        double fourthSide = Cross(vertices, third, fourth, second);
        if (((firstSide > tolerance && secondSide < -tolerance) || (firstSide < -tolerance && secondSide > tolerance)) &&
            ((thirdSide > tolerance && fourthSide < -tolerance) || (thirdSide < -tolerance && fourthSide > tolerance)))
        {
            return true;
        }

        if (Math.Abs(firstSide) <= tolerance && OnSegment(vertices, first, second, third, tolerance))
        {
            return true;
        }

        if (Math.Abs(secondSide) <= tolerance && OnSegment(vertices, first, second, fourth, tolerance))
        {
            return true;
        }

        if (Math.Abs(thirdSide) <= tolerance && OnSegment(vertices, third, fourth, first, tolerance))
        {
            return true;
        }

        return Math.Abs(fourthSide) <= tolerance && OnSegment(vertices, third, fourth, second, tolerance);
    }

    static bool OnSegment(double[] vertices, int first, int second, int point, double tolerance)
    {
        double pointX = X(vertices, point);
        double pointY = Y(vertices, point);
        return pointX >= Math.Min(X(vertices, first), X(vertices, second)) - tolerance &&
            pointX <= Math.Max(X(vertices, first), X(vertices, second)) + tolerance &&
            pointY >= Math.Min(Y(vertices, first), Y(vertices, second)) - tolerance &&
            pointY <= Math.Max(Y(vertices, first), Y(vertices, second)) + tolerance;
    }

    static double SignedDoubleArea(double[] vertices, int vertexCount)
    {
        double area = 0.0;
        double compensation = 0.0;
        int vertex = 0;
        while (vertex < vertexCount)
        {
            int next = (vertex + 1) % vertexCount;
            double term = X(vertices, vertex) * Y(vertices, next) - X(vertices, next) * Y(vertices, vertex);
            double adjusted = term - compensation;
            double sum = area + adjusted;
            compensation = (sum - area) - adjusted;
            area = sum;
            vertex++;
        }

        return area;
    }

    static int[] EarClip(double[] vertices, int vertexCount, double orientation, double tolerance)
    {
        int[] remaining = new int[vertexCount];
        int index = 0;
        while (index < vertexCount)
        {
            remaining[index] = index;
            index++;
        }

        int[] triangles = new int[(vertexCount - 2) * 3];
        int remainingCount = vertexCount;
        int triangleOffset = 0;
        while (remainingCount > 3)
        {
            bool clipped = false;
            int position = 0;
            while (position < remainingCount)
            {
                int previous = remaining[(position + remainingCount - 1) % remainingCount];
                int current = remaining[position];
                int next = remaining[(position + 1) % remainingCount];
                if (orientation * Cross(vertices, previous, current, next) > tolerance &&
                    EarContainsNoVertex(vertices, remaining, remainingCount, previous, current, next, orientation, tolerance))
                {
                    triangles[triangleOffset] = previous;
                    triangles[triangleOffset + 1] = current;
                    triangles[triangleOffset + 2] = next;
                    triangleOffset += 3;
                    int move = position;
                    while (move < remainingCount - 1)
                    {
                        remaining[move] = remaining[move + 1];
                        move++;
                    }

                    remainingCount--;
                    clipped = true;
                    break;
                }

                position++;
            }

            if (!clipped)
            {
                throw RandomArgumentException.Create("vertices", "The polygon could not be triangulated without degenerate triangles.");
            }
        }

        if (orientation * Cross(vertices, remaining[0], remaining[1], remaining[2]) <= tolerance)
        {
            throw RandomArgumentException.Create("vertices", "The polygon triangulation contains a degenerate triangle.");
        }

        triangles[triangleOffset] = remaining[0];
        triangles[triangleOffset + 1] = remaining[1];
        triangles[triangleOffset + 2] = remaining[2];
        return triangles;
    }

    static bool EarContainsNoVertex(double[] vertices, int[] remaining, int remainingCount, int first, int second, int third, double orientation, double tolerance)
    {
        int position = 0;
        while (position < remainingCount)
        {
            int point = remaining[position];
            if (point != first && point != second && point != third)
            {
                double firstSide = orientation * Cross(vertices, first, second, point);
                double secondSide = orientation * Cross(vertices, second, third, point);
                double thirdSide = orientation * Cross(vertices, third, first, point);
                if (firstSide >= -tolerance && secondSide >= -tolerance && thirdSide >= -tolerance)
                {
                    return false;
                }
            }

            position++;
        }

        return true;
    }

    static double[] TriangleAreas(double[] vertices, int[] triangles)
    {
        double[] areas = new double[triangles.Length / 3];
        int triangle = 0;
        while (triangle < areas.Length)
        {
            int offset = triangle * 3;
            areas[triangle] = Math.Abs(Cross(vertices, triangles[offset], triangles[offset + 1], triangles[offset + 2]));
            triangle++;
        }

        return areas;
    }

    static double Cross(double[] vertices, int first, int second, int third)
    {
        double firstX = X(vertices, second) - X(vertices, first);
        double firstY = Y(vertices, second) - Y(vertices, first);
        double secondX = X(vertices, third) - X(vertices, first);
        double secondY = Y(vertices, third) - Y(vertices, first);
        return firstX * secondY - firstY * secondX;
    }

    static double X(double[] vertices, int vertex)
    {
        return vertices[vertex * 2];
    }

    static double Y(double[] vertices, int vertex)
    {
        return vertices[vertex * 2 + 1];
    }
}
