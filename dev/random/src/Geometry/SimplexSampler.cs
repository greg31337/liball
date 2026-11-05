using System;

namespace random;

/// <summary>
/// Provides uniform sampling inside full-dimensional simplexes, triangles, and tetrahedra.
/// </summary>
internal static class SimplexSampler
{
    const double MachineEpsilon = 2.220446049250313080847263336181640625e-16;

    /// <summary>Returns a uniform point inside a full-dimensional simplex.</summary>
    /// <param name="generator">The generator that supplies barycentric weights.</param>
    /// <param name="vertices">The dimension plus one finite vertices in row-major order.</param>
    /// <param name="dimension">The positive coordinate dimension.</param>
    /// <returns>A point containing exactly dimension coordinates.</returns>
    /// <remarks>Vertex row <c>v</c>, coordinate <c>c</c> is stored at <c>vertices[v * dimension + c]</c>.</remarks>
    /// <exception cref="RandomArgumentException">An argument is null, the dimension or vertex storage is invalid, a coordinate is non-finite, or the vertices are affinely dependent.</exception>
    public static double[] Simplex(IRandomSource generator, double[] vertices, int dimension)
    {
        Guard.NotNull(generator, "generator");
        ValidateSimplex(vertices, dimension, "vertices");
        double[] result = new double[dimension];
        Simplex(generator, vertices, dimension, result);
        return result;
    }

    /// <summary>Writes a uniform point inside a full-dimensional simplex.</summary>
    /// <param name="generator">The generator that supplies barycentric weights.</param>
    /// <param name="vertices">The dimension plus one finite vertices in row-major order.</param>
    /// <param name="dimension">The positive coordinate dimension.</param>
    /// <param name="destination">The destination of exactly dimension coordinates.</param>
    /// <remarks>Vertex row <c>v</c>, coordinate <c>c</c> is stored at <c>vertices[v * dimension + c]</c>.</remarks>
    /// <exception cref="RandomArgumentException">An argument is null, the dimension, vertex storage, or destination length is invalid, a coordinate is non-finite, or the vertices are affinely dependent.</exception>
    public static void Simplex(IRandomSource generator, double[] vertices, int dimension, Span<double> destination)
    {
        Guard.NotNull(generator, "generator");
        int vertexCount = ValidateSimplex(vertices, dimension, "vertices");
        Guard.ExactSpanLength(destination.Length, dimension, "destination");
        double[] stableVertices = (double[])vertices.Clone();
        SampleValidated(generator, stableVertices, vertexCount, dimension, destination);
    }

    /// <summary>Returns a uniform point inside a nondegenerate triangle.</summary>
    /// <param name="generator">The generator that supplies barycentric weights.</param>
    /// <param name="first">The finite first vertex.</param>
    /// <param name="second">The finite second vertex with the same coordinate dimension.</param>
    /// <param name="third">The finite third vertex with the same coordinate dimension.</param>
    /// <returns>A point with the same coordinate dimension as the vertices.</returns>
    /// <exception cref="RandomArgumentException">An argument is null, the vertex dimensions are invalid, a coordinate is non-finite, or the vertices are collinear.</exception>
    public static double[] Triangle(IRandomSource generator, double[] first, double[] second, double[] third)
    {
        Guard.NotNull(generator, "generator");
        int dimension = ValidateTriangle(first, second, third);
        double[] result = new double[dimension];
        Triangle(generator, first, second, third, result);
        return result;
    }

    /// <summary>Writes a uniform point inside a nondegenerate triangle.</summary>
    /// <param name="generator">The generator that supplies barycentric weights.</param>
    /// <param name="first">The finite first vertex.</param>
    /// <param name="second">The finite second vertex with the same coordinate dimension.</param>
    /// <param name="third">The finite third vertex with the same coordinate dimension.</param>
    /// <param name="destination">The destination with the same coordinate dimension.</param>
    /// <exception cref="RandomArgumentException">An argument is null, the vertex dimensions or destination length are invalid, a coordinate is non-finite, or the vertices are collinear.</exception>
    public static void Triangle(IRandomSource generator, double[] first, double[] second, double[] third, Span<double> destination)
    {
        Guard.NotNull(generator, "generator");
        int dimension = ValidateTriangle(first, second, third);
        Guard.ExactSpanLength(destination.Length, dimension, "destination");
        double[] vertices = CombineVertices(first, second, third);
        SampleValidated(generator, vertices, 3, dimension, destination);
    }

    /// <summary>Returns a uniform point inside a nondegenerate tetrahedron.</summary>
    /// <param name="generator">The generator that supplies barycentric weights.</param>
    /// <param name="first">The finite first vertex.</param>
    /// <param name="second">The finite second vertex with the same coordinate dimension.</param>
    /// <param name="third">The finite third vertex with the same coordinate dimension.</param>
    /// <param name="fourth">The finite fourth vertex with the same coordinate dimension.</param>
    /// <returns>A point with the same coordinate dimension as the vertices.</returns>
    /// <exception cref="RandomArgumentException">An argument is null, the vertex dimensions are invalid, a coordinate is non-finite, or the vertices are affinely dependent.</exception>
    public static double[] Tetrahedron(IRandomSource generator, double[] first, double[] second, double[] third, double[] fourth)
    {
        Guard.NotNull(generator, "generator");
        int dimension = ValidateTetrahedron(first, second, third, fourth);
        double[] result = new double[dimension];
        Tetrahedron(generator, first, second, third, fourth, result);
        return result;
    }

    /// <summary>Writes a uniform point inside a nondegenerate tetrahedron.</summary>
    /// <param name="generator">The generator that supplies barycentric weights.</param>
    /// <param name="first">The finite first vertex.</param>
    /// <param name="second">The finite second vertex with the same coordinate dimension.</param>
    /// <param name="third">The finite third vertex with the same coordinate dimension.</param>
    /// <param name="fourth">The finite fourth vertex with the same coordinate dimension.</param>
    /// <param name="destination">The destination with the same coordinate dimension.</param>
    /// <exception cref="RandomArgumentException">An argument is null, the vertex dimensions or destination length are invalid, a coordinate is non-finite, or the vertices are affinely dependent.</exception>
    public static void Tetrahedron(IRandomSource generator, double[] first, double[] second, double[] third, double[] fourth, Span<double> destination)
    {
        Guard.NotNull(generator, "generator");
        int dimension = ValidateTetrahedron(first, second, third, fourth);
        Guard.ExactSpanLength(destination.Length, dimension, "destination");
        double[] vertices = CombineVertices(first, second, third, fourth);
        SampleValidated(generator, vertices, 4, dimension, destination);
    }

    static int ValidateSimplex(double[] vertices, int dimension, string degeneracyArgumentName)
    {
        Guard.NotNull(vertices, "vertices");
        Guard.PositiveCount(dimension, "dimension");
        long requiredLength = (long)dimension * ((long)dimension + 1L);
        if (requiredLength > int.MaxValue)
        {
            throw RandomArgumentException.Create("dimension", "The simplex vertex storage would be too large.");
        }

        if (vertices.Length != (int)requiredLength)
        {
            throw RandomArgumentException.Create("vertices", "The vertex storage length must equal dimension times dimension plus one.");
        }

        ValidateFinite(vertices, "vertices");
        ValidateAffineIndependence(vertices, dimension + 1, dimension, degeneracyArgumentName);
        return dimension + 1;
    }

    static int ValidateTriangle(double[] first, double[] second, double[] third)
    {
        int dimension = ValidateVertexSet(first, second, third);
        double[] vertices = CombineVertices(first, second, third);
        ValidateAffineIndependence(vertices, 3, dimension, "third");
        return dimension;
    }

    static int ValidateTetrahedron(double[] first, double[] second, double[] third, double[] fourth)
    {
        int dimension = ValidateVertexSet(first, second, third, fourth);
        double[] vertices = CombineVertices(first, second, third, fourth);
        ValidateAffineIndependence(vertices, 4, dimension, "fourth");
        return dimension;
    }

    static int ValidateVertexSet(double[] first, double[] second, double[] third)
    {
        int dimension = ValidateFirstVertex(first);
        ValidateVertex(second, dimension, "second");
        ValidateVertex(third, dimension, "third");
        return dimension;
    }

    static int ValidateVertexSet(double[] first, double[] second, double[] third, double[] fourth)
    {
        int dimension = ValidateVertexSet(first, second, third);
        ValidateVertex(fourth, dimension, "fourth");
        return dimension;
    }

    static int ValidateFirstVertex(double[] first)
    {
        Guard.NotNull(first, "first");
        if (first.Length == 0)
        {
            throw RandomArgumentException.Create("first", "At least one coordinate is required.");
        }

        ValidateFinite(first, "first");
        return first.Length;
    }

    static void ValidateVertex(double[] vertex, int dimension, string argumentName)
    {
        Guard.NotNull(vertex, argumentName);
        if (vertex.Length != dimension)
        {
            throw RandomArgumentException.Create(argumentName, "Every vertex must have the same coordinate dimension.");
        }

        ValidateFinite(vertex, argumentName);
    }

    static void ValidateFinite(double[] values, string argumentName)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (!double.IsFinite(values[index]))
            {
                throw RandomArgumentException.Create(argumentName, "Every coordinate must be finite.");
            }

            index++;
        }
    }

    static void ValidateAffineIndependence(double[] vertices, int vertexCount, int dimension, string argumentName)
    {
        int edgeCount = vertexCount - 1;
        if (edgeCount > dimension)
        {
            throw RandomArgumentException.Create(argumentName, "The vertices cannot be affinely independent in the coordinate dimension.");
        }

        double[] basis = new double[edgeCount * dimension];
        double[] edge = new double[dimension];
        double tolerance = Math.Min(1.0e-8, (double)dimension * 64.0 * MachineEpsilon);
        int edgeIndex = 0;
        while (edgeIndex < edgeCount)
        {
            int vertexOffset = (edgeIndex + 1) * dimension;
            bool halve = false;
            int coordinate = 0;
            while (coordinate < dimension)
            {
                if (double.IsInfinity(vertices[vertexOffset + coordinate] - vertices[coordinate]))
                {
                    halve = true;
                }

                coordinate++;
            }

            double maximum = 0.0;
            coordinate = 0;
            while (coordinate < dimension)
            {
                double difference;
                if (halve)
                {
                    difference = vertices[vertexOffset + coordinate] * 0.5 - vertices[coordinate] * 0.5;
                }
                else
                {
                    difference = vertices[vertexOffset + coordinate] - vertices[coordinate];
                }

                edge[coordinate] = difference;
                double magnitude = Math.Abs(difference);
                if (magnitude > maximum)
                {
                    maximum = magnitude;
                }

                coordinate++;
            }

            if (maximum == 0.0)
            {
                throw RandomArgumentException.Create(argumentName, "The vertices must be affinely independent.");
            }

            coordinate = 0;
            while (coordinate < dimension)
            {
                edge[coordinate] /= maximum;
                coordinate++;
            }

            int previous = 0;
            while (previous < edgeIndex)
            {
                double projection = 0.0;
                coordinate = 0;
                while (coordinate < dimension)
                {
                    projection += edge[coordinate] * basis[previous * dimension + coordinate];
                    coordinate++;
                }

                coordinate = 0;
                while (coordinate < dimension)
                {
                    edge[coordinate] -= projection * basis[previous * dimension + coordinate];
                    coordinate++;
                }

                previous++;
            }

            double squaredNorm = 0.0;
            coordinate = 0;
            while (coordinate < dimension)
            {
                squaredNorm += edge[coordinate] * edge[coordinate];
                coordinate++;
            }

            double norm = PortableMath.Sqrt(squaredNorm);
            if (!(norm > tolerance))
            {
                throw RandomArgumentException.Create(argumentName, "The vertices must be affinely independent.");
            }

            coordinate = 0;
            while (coordinate < dimension)
            {
                basis[edgeIndex * dimension + coordinate] = edge[coordinate] / norm;
                coordinate++;
            }

            edgeIndex++;
        }
    }

    static void SampleValidated(IRandomSource generator, double[] vertices, int vertexCount, int dimension, Span<double> destination)
    {
        double[] weights = new double[vertexCount];
        double total = 0.0;
        double compensation = 0.0;
        int vertex = 0;
        while (vertex < vertexCount)
        {
            double exponential = -Log(generator, FloatingUniform.NextDoubleOpenOpen(generator));
            weights[vertex] = exponential;
            double adjusted = exponential - compensation;
            double next = total + adjusted;
            compensation = (next - total) - adjusted;
            total = next;
            vertex++;
        }

        double prefix = 0.0;
        vertex = 0;
        while (vertex < vertexCount - 1)
        {
            weights[vertex] /= total;
            prefix += weights[vertex];
            vertex++;
        }

        if (prefix > 1.0)
        {
            double scale = PortableMath.NextDown(1.0) / prefix;
            prefix = 0.0;
            vertex = 0;
            while (vertex < vertexCount - 1)
            {
                weights[vertex] *= scale;
                prefix += weights[vertex];
                vertex++;
            }
        }

        weights[vertexCount - 1] = 1.0 - prefix;
        int coordinate = 0;
        while (coordinate < dimension)
        {
            destination[coordinate] = WeightedCoordinate(vertices, weights, vertexCount, dimension, coordinate);
            coordinate++;
        }
    }

    static double WeightedCoordinate(double[] vertices, double[] weights, int vertexCount, int dimension, int coordinate)
    {
        double maximumMagnitude = 0.0;
        double minimum = vertices[coordinate];
        double maximum = minimum;
        int vertex = 0;
        while (vertex < vertexCount)
        {
            double value = vertices[vertex * dimension + coordinate];
            double magnitude = Math.Abs(value);
            if (magnitude > maximumMagnitude)
            {
                maximumMagnitude = magnitude;
            }

            if (value < minimum)
            {
                minimum = value;
            }

            if (value > maximum)
            {
                maximum = value;
            }

            vertex++;
        }

        if (maximumMagnitude == 0.0)
        {
            return 0.0;
        }

        double sum = 0.0;
        double compensation = 0.0;
        vertex = 0;
        while (vertex < vertexCount)
        {
            double term = weights[vertex] * (vertices[vertex * dimension + coordinate] / maximumMagnitude);
            double adjusted = term - compensation;
            double next = sum + adjusted;
            compensation = (next - sum) - adjusted;
            sum = next;
            vertex++;
        }

        double result = sum * maximumMagnitude;
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

    static double[] CombineVertices(double[] first, double[] second, double[] third)
    {
        int dimension = first.Length;
        if (dimension > int.MaxValue / 3)
        {
            throw RandomArgumentException.Create("first", "The combined vertex storage would be too large.");
        }

        double[] vertices = new double[dimension * 3];
        Array.Copy(first, 0, vertices, 0, dimension);
        Array.Copy(second, 0, vertices, dimension, dimension);
        Array.Copy(third, 0, vertices, dimension * 2, dimension);
        return vertices;
    }

    static double[] CombineVertices(double[] first, double[] second, double[] third, double[] fourth)
    {
        int dimension = first.Length;
        if (dimension > int.MaxValue / 4)
        {
            throw RandomArgumentException.Create("first", "The combined vertex storage would be too large.");
        }

        double[] vertices = new double[dimension * 4];
        Array.Copy(first, 0, vertices, 0, dimension);
        Array.Copy(second, 0, vertices, dimension, dimension);
        Array.Copy(third, 0, vertices, dimension * 2, dimension);
        Array.Copy(fourth, 0, vertices, dimension * 3, dimension);
        return vertices;
    }

    static double Log(IRandomSource generator, double value)
    {
        if (generator.MathMode() == RandomMathMode.Portable)
        {
            return PortableMath.Log(value);
        }

        return Math.Log(value);
    }
}
