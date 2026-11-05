using System;

namespace random;

/// <summary>
/// Provides uniform sampling for line segments, axis-aligned boxes, circles, spheres, and balls.
/// </summary>
internal static class BallSampler
{
    const double Tau = 6.283185307179586476925286766559005768;

    /// <summary>Returns a uniform point on a line segment in any positive dimension.</summary>
    /// <param name="generator">The generator that supplies the shared interpolation value.</param>
    /// <param name="start">The finite first endpoint.</param>
    /// <param name="end">The finite second endpoint with the same dimension.</param>
    /// <returns>A point with the same dimension as the endpoints.</returns>
    /// <remarks>An equal pair of endpoints returns that point without consuming random state.</remarks>
    /// <exception cref="RandomArgumentException">An argument is null, the endpoints are empty or have different lengths, or a coordinate is non-finite.</exception>
    public static double[] Line(IRandomSource generator, double[] start, double[] end)
    {
        Guard.NotNull(generator, "generator");
        int dimension = ValidateVectors(start, end, "start", "end");
        double[] result = new double[dimension];
        Line(generator, start, end, result);
        return result;
    }

    /// <summary>Writes a uniform point on a line segment in any positive dimension.</summary>
    /// <param name="generator">The generator that supplies the shared interpolation value.</param>
    /// <param name="start">The finite first endpoint.</param>
    /// <param name="end">The finite second endpoint with the same dimension.</param>
    /// <param name="destination">The destination with the same dimension as the endpoints.</param>
    /// <remarks>An equal pair of endpoints returns that point without consuming random state.</remarks>
    /// <exception cref="RandomArgumentException">An argument is null, the endpoints are empty or have different lengths, a coordinate is non-finite, or the destination length is invalid.</exception>
    public static void Line(IRandomSource generator, double[] start, double[] end, Span<double> destination)
    {
        Guard.NotNull(generator, "generator");
        int dimension = ValidateVectors(start, end, "start", "end");
        Guard.ExactSpanLength(destination.Length, dimension, "destination");
        bool distinct = false;
        int index = 0;
        while (index < dimension)
        {
            if (start[index] != end[index])
            {
                distinct = true;
            }

            index++;
        }

        if (!distinct)
        {
            start.CopyTo(destination);
            return;
        }

        double unit = FloatingUniform.NextDoubleClosedOpen(generator);
        index = 0;
        while (index < dimension)
        {
            destination[index] = Interpolate(start[index], end[index], unit);
            index++;
        }
    }

    /// <summary>Returns a uniform point inside a finite axis-aligned box.</summary>
    /// <param name="generator">The generator that supplies independent coordinate values.</param>
    /// <param name="minimum">The finite inclusive lower coordinate bounds.</param>
    /// <param name="maximum">The finite upper coordinate bounds with the same dimension.</param>
    /// <returns>A point with the same dimension as the bounds.</returns>
    /// <remarks>Equal bounds define deterministic coordinates and consume no random state.</remarks>
    /// <exception cref="RandomArgumentException">An argument is null, the bounds are empty or have different lengths, a coordinate is non-finite, or a maximum is below its minimum.</exception>
    public static double[] Box(IRandomSource generator, double[] minimum, double[] maximum)
    {
        Guard.NotNull(generator, "generator");
        int dimension = ValidateBox(minimum, maximum);
        double[] result = new double[dimension];
        Box(generator, minimum, maximum, result);
        return result;
    }

    /// <summary>Writes a uniform point inside a finite axis-aligned box.</summary>
    /// <param name="generator">The generator that supplies independent coordinate values.</param>
    /// <param name="minimum">The finite inclusive lower coordinate bounds.</param>
    /// <param name="maximum">The finite upper coordinate bounds with the same dimension.</param>
    /// <param name="destination">The destination with the same dimension as the bounds.</param>
    /// <remarks>Equal bounds define deterministic coordinates and consume no random state.</remarks>
    /// <exception cref="RandomArgumentException">An argument is null, the bounds are empty or have different lengths, a coordinate is non-finite, a maximum is below its minimum, or the destination length is invalid.</exception>
    public static void Box(IRandomSource generator, double[] minimum, double[] maximum, Span<double> destination)
    {
        Guard.NotNull(generator, "generator");
        int dimension = ValidateBox(minimum, maximum);
        Guard.ExactSpanLength(destination.Length, dimension, "destination");
        int index = 0;
        while (index < dimension)
        {
            destination[index] = FloatingUniform.NextDouble(generator, minimum[index], maximum[index]);
            index++;
        }
    }

    /// <summary>Returns a uniform point on the unit circle.</summary>
    /// <param name="generator">The generator that supplies the angular coordinate.</param>
    /// <returns>Two coordinates whose Euclidean norm is one within floating-point rounding.</returns>
    /// <exception cref="RandomArgumentException">The generator is null.</exception>
    public static double[] UnitCircle(IRandomSource generator)
    {
        double[] result = new double[2];
        UnitCircle(generator, result);
        return result;
    }

    /// <summary>Writes a uniform point on the unit circle.</summary>
    /// <param name="generator">The generator that supplies the angular coordinate.</param>
    /// <param name="destination">The destination of exactly two coordinates.</param>
    /// <exception cref="RandomArgumentException">The generator is null or the destination length is not two.</exception>
    public static void UnitCircle(IRandomSource generator, Span<double> destination)
    {
        ValidateFixedDestination(generator, destination.Length, 2);
        double angle = Tau * FloatingUniform.NextDoubleClosedOpen(generator);
        destination[0] = Cos(generator, angle);
        destination[1] = Sin(generator, angle);
    }

    /// <summary>Returns a uniform point inside the unit disk.</summary>
    /// <param name="generator">The generator that supplies radial and angular coordinates.</param>
    /// <returns>Two coordinates with Euclidean norm at most one.</returns>
    /// <exception cref="RandomArgumentException">The generator is null.</exception>
    public static double[] UnitDisk(IRandomSource generator)
    {
        double[] result = new double[2];
        UnitDisk(generator, result);
        return result;
    }

    /// <summary>Writes a uniform point inside the unit disk.</summary>
    /// <param name="generator">The generator that supplies radial and angular coordinates.</param>
    /// <param name="destination">The destination of exactly two coordinates.</param>
    /// <exception cref="RandomArgumentException">The generator is null or the destination length is not two.</exception>
    public static void UnitDisk(IRandomSource generator, Span<double> destination)
    {
        ValidateFixedDestination(generator, destination.Length, 2);
        double radius = Sqrt(generator, FloatingUniform.NextDoubleClosedOpen(generator));
        double angle = Tau * FloatingUniform.NextDoubleClosedOpen(generator);
        destination[0] = radius * Cos(generator, angle);
        destination[1] = radius * Sin(generator, angle);
    }

    /// <summary>Returns a uniform point on the three-dimensional unit sphere.</summary>
    /// <param name="generator">The generator that supplies axial and angular coordinates.</param>
    /// <returns>Three coordinates whose Euclidean norm is one within floating-point rounding.</returns>
    /// <exception cref="RandomArgumentException">The generator is null.</exception>
    public static double[] UnitSphere(IRandomSource generator)
    {
        double[] result = new double[3];
        UnitSphere(generator, result);
        return result;
    }

    /// <summary>Writes a uniform point on the three-dimensional unit sphere.</summary>
    /// <param name="generator">The generator that supplies axial and angular coordinates.</param>
    /// <param name="destination">The destination of exactly three coordinates.</param>
    /// <exception cref="RandomArgumentException">The generator is null or the destination length is not three.</exception>
    public static void UnitSphere(IRandomSource generator, Span<double> destination)
    {
        ValidateFixedDestination(generator, destination.Length, 3);
        double axial = 2.0 * FloatingUniform.NextDoubleClosedOpen(generator) - 1.0;
        double angle = Tau * FloatingUniform.NextDoubleClosedOpen(generator);
        double planar = Sqrt(generator, Math.Max(0.0, 1.0 - axial * axial));
        destination[0] = planar * Cos(generator, angle);
        destination[1] = planar * Sin(generator, angle);
        destination[2] = axial;
    }

    /// <summary>Returns a uniform direction in any positive dimension.</summary>
    /// <param name="generator">The generator that supplies normal direction components.</param>
    /// <param name="dimension">The positive vector dimension.</param>
    /// <returns>A vector whose Euclidean norm is one within floating-point rounding.</returns>
    /// <exception cref="RandomArgumentException">The generator is null or the dimension is not positive.</exception>
    public static double[] UnitVector(IRandomSource generator, int dimension)
    {
        Guard.NotNull(generator, "generator");
        Guard.PositiveCount(dimension, "dimension");
        double[] result = new double[dimension];
        UnitVectorInto(generator, result);
        return result;
    }

    /// <summary>Writes a uniform direction whose dimension is the destination length.</summary>
    /// <param name="generator">The generator that supplies normal direction components.</param>
    /// <param name="destination">The nonempty destination vector.</param>
    /// <exception cref="RandomArgumentException">The generator is null or the destination is empty.</exception>
    public static void UnitVectorInto(IRandomSource generator, Span<double> destination)
    {
        Guard.NotNull(generator, "generator");
        Guard.PositiveCount(destination.Length, "destination");
        double[] direction = new double[destination.Length];
        NormalSampler normalSampler = new NormalSampler(generator, 0.0, 1.0);
        double maximum;
        while (true)
        {
            maximum = 0.0;
            int index = 0;
            while (index < direction.Length)
            {
                double value = normalSampler.Sample();
                direction[index] = value;
                double magnitude = Math.Abs(value);
                if (magnitude > maximum)
                {
                    maximum = magnitude;
                }

                index++;
            }

            if (maximum != 0.0)
            {
                break;
            }
        }

        double scaledSquaredNorm = 0.0;
        int component = 0;
        while (component < direction.Length)
        {
            double scaled = direction[component] / maximum;
            scaledSquaredNorm += scaled * scaled;
            component++;
        }

        double scale = 1.0 / Sqrt(generator, scaledSquaredNorm);
        component = 0;
        while (component < destination.Length)
        {
            destination[component] = (direction[component] / maximum) * scale;
            component++;
        }
    }

    /// <summary>Returns a uniform point inside a unit ball of any positive dimension.</summary>
    /// <param name="generator">The generator that supplies normal direction components and a radius.</param>
    /// <param name="dimension">The positive vector dimension.</param>
    /// <returns>A vector with Euclidean norm at most one.</returns>
    /// <exception cref="RandomArgumentException">The generator is null or the dimension is not positive.</exception>
    public static double[] UnitBall(IRandomSource generator, int dimension)
    {
        Guard.NotNull(generator, "generator");
        Guard.PositiveCount(dimension, "dimension");
        double[] result = new double[dimension];
        UnitBall(generator, result);
        return result;
    }

    /// <summary>Writes a uniform point inside a unit ball whose dimension is the destination length.</summary>
    /// <param name="generator">The generator that supplies normal direction components and a radius.</param>
    /// <param name="destination">The nonempty destination vector.</param>
    /// <exception cref="RandomArgumentException">The generator is null or the destination is empty.</exception>
    public static void UnitBall(IRandomSource generator, Span<double> destination)
    {
        Guard.NotNull(generator, "generator");
        Guard.PositiveCount(destination.Length, "destination");
        double[] direction = new double[destination.Length];
        NormalSampler normalSampler = new NormalSampler(generator, 0.0, 1.0);
        double maximum;
        while (true)
        {
            maximum = 0.0;
            int index = 0;
            while (index < direction.Length)
            {
                double value = normalSampler.Sample();
                direction[index] = value;
                double magnitude = Math.Abs(value);
                if (magnitude > maximum)
                {
                    maximum = magnitude;
                }

                index++;
            }

            if (maximum != 0.0)
            {
                break;
            }
        }

        double scaledSquaredNorm = 0.0;
        int component = 0;
        while (component < direction.Length)
        {
            double scaled = direction[component] / maximum;
            scaledSquaredNorm += scaled * scaled;
            component++;
        }

        double radius = Power(generator, FloatingUniform.NextDoubleClosedOpen(generator), 1.0 / (double)direction.Length);
        double scale = radius / Sqrt(generator, scaledSquaredNorm);
        component = 0;
        double outputSquaredNorm = 0.0;
        while (component < direction.Length)
        {
            double value = (direction[component] / maximum) * scale;
            destination[component] = value;
            outputSquaredNorm += value * value;
            component++;
        }

        if (outputSquaredNorm > 1.0)
        {
            double correction = PortableMath.NextDown(1.0) / Sqrt(generator, outputSquaredNorm);
            component = 0;
            while (component < destination.Length)
            {
                destination[component] *= correction;
                component++;
            }
        }
    }

    static int ValidateVectors(double[] first, double[] second, string firstName, string secondName)
    {
        Guard.NotNull(first, firstName);
        Guard.NotNull(second, secondName);
        if (first.Length == 0)
        {
            throw RandomArgumentException.Create(firstName, "At least one coordinate is required.");
        }

        if (second.Length != first.Length)
        {
            throw RandomArgumentException.Create(secondName, "The coordinate vectors must have equal lengths.");
        }

        int index = 0;
        while (index < first.Length)
        {
            if (!double.IsFinite(first[index]))
            {
                throw RandomArgumentException.Create(firstName, "Every coordinate must be finite.");
            }

            if (!double.IsFinite(second[index]))
            {
                throw RandomArgumentException.Create(secondName, "Every coordinate must be finite.");
            }

            index++;
        }

        return first.Length;
    }

    static int ValidateBox(double[] minimum, double[] maximum)
    {
        int dimension = ValidateVectors(minimum, maximum, "minimum", "maximum");
        int index = 0;
        while (index < dimension)
        {
            if (maximum[index] < minimum[index])
            {
                throw RandomArgumentException.Create("maximum", "Every maximum coordinate must be greater than or equal to its minimum.");
            }

            index++;
        }

        return dimension;
    }

    static void ValidateFixedDestination(IRandomSource generator, int actualLength, int requiredLength)
    {
        Guard.NotNull(generator, "generator");
        Guard.ExactSpanLength(actualLength, requiredLength, "destination");
    }

    static double Interpolate(double start, double end, double unit)
    {
        if (start == end)
        {
            return start;
        }

        double width = end - start;
        double result;
        if (double.IsInfinity(width))
        {
            result = start * (1.0 - unit) + end * unit;
        }
        else
        {
            result = start + width * unit;
        }

        if (start < end)
        {
            if (result < start)
            {
                return start;
            }

            if (result >= end)
            {
                return PortableMath.NextDown(end);
            }

            return result;
        }

        if (result > start)
        {
            return start;
        }

        if (result <= end)
        {
            return PortableMath.NextUp(end);
        }

        return result;
    }

    static double Sqrt(IRandomSource generator, double value)
    {
        if (generator.MathMode() == RandomMathMode.Portable)
        {
            return PortableMath.Sqrt(value);
        }

        return Math.Sqrt(value);
    }

    static double Power(IRandomSource generator, double value, double exponent)
    {
        if (generator.MathMode() == RandomMathMode.Portable)
        {
            return PortableMath.Power(value, exponent);
        }

        return Math.Pow(value, exponent);
    }

    static double Sin(IRandomSource generator, double value)
    {
        if (generator.MathMode() == RandomMathMode.Portable)
        {
            return PortableMath.Sin(value);
        }

        return Math.Sin(value);
    }

    static double Cos(IRandomSource generator, double value)
    {
        if (generator.MathMode() == RandomMathMode.Portable)
        {
            return PortableMath.Cos(value);
        }

        return Math.Cos(value);
    }
}
