using System;

namespace random;

/// <summary>
/// Generates Haar-distributed orthogonal matrices using deterministic Gaussian orthonormalization.
/// </summary>
/// <remarks>
/// Independent standard-normal columns are orthonormalized in increasing column order with two
/// modified Gram-Schmidt passes. The resulting row-major matrix is Haar distributed on <c>O(n)</c>;
/// determinants positive and negative one occur with equal probability. The sampler retains the
/// supplied mutable generator and requires external synchronization when shared between callers.
/// </remarks>
public class OrthogonalMatrixSampler : IOrthogonalMatrixSampler
{
    const double MinimumResidualMagnitude = 1.0e-14;
    IRandomSource generator;
    NormalSampler normalSampler;
    int dimension;
    double[] candidate;

    /// <summary>Creates a Haar orthogonal-matrix sampler.</summary>
    /// <param name="generator">The generator that supplies independent standard-normal values.</param>
    /// <param name="dimension">The positive square-matrix dimension.</param>
    /// <exception cref="RandomArgumentException">The generator is null, the dimension is not positive, or a matrix would exceed array limits.</exception>
    public OrthogonalMatrixSampler(IRandomSource generator, int dimension)
    {
        Guard.NotNull(generator, "generator");
        Guard.PositiveCount(dimension, "dimension");
        if (dimension > int.MaxValue / dimension)
        {
            throw RandomArgumentException.Create("dimension", "The matrix contains too many elements.");
        }

        this.generator = generator;
        this.dimension = dimension;
        normalSampler = new NormalSampler(generator, 0.0, 1.0);
        candidate = new double[dimension];
    }

    /// <summary>Returns the square-matrix dimension.</summary>
    /// <returns>The positive row and column count.</returns>
    public int Dimension()
    {
        return dimension;
    }

    /// <summary>Returns the required row-major storage for a matrix batch.</summary>
    /// <param name="sampleCount">The nonnegative matrix count.</param>
    /// <returns>The sample count multiplied by dimension squared.</returns>
    /// <exception cref="RandomArgumentException">The sample count is negative or the required storage exceeds <c>int.MaxValue</c>.</exception>
    public int RequiredOutputLength(int sampleCount)
    {
        Guard.Count(sampleCount, "sampleCount");
        int matrixLength = dimension * dimension;
        if (sampleCount > int.MaxValue / matrixLength)
        {
            throw RandomArgumentException.Create("sampleCount", "The requested batch contains too many matrix elements.");
        }

        return sampleCount * matrixLength;
    }

    /// <summary>Returns one Haar-distributed row-major orthogonal matrix.</summary>
    /// <returns>A new array containing dimension squared entries.</returns>
    public double[] Sample()
    {
        double[] result = new double[dimension * dimension];
        SampleInto(result);
        return result;
    }

    /// <summary>Writes one Haar-distributed row-major orthogonal matrix.</summary>
    /// <param name="destination">The destination of exactly dimension squared entries.</param>
    /// <exception cref="RandomArgumentException">The destination length is invalid.</exception>
    public void SampleInto(Span<double> destination)
    {
        Guard.ExactSpanLength(destination.Length, dimension * dimension, "destination");
        SampleUnchecked(destination, 0);
    }

    /// <summary>Fills a sample-major batch with Haar-distributed row-major orthogonal matrices.</summary>
    /// <param name="sampleCount">The nonnegative matrix count.</param>
    /// <param name="destination">The destination of exactly <see cref="RequiredOutputLength(int)"/> entries.</param>
    /// <remarks>Each matrix matches a repeated call to <see cref="SampleInto(Span{double})"/>.</remarks>
    /// <exception cref="RandomArgumentException">The sample count is invalid or the destination length does not match it.</exception>
    public void Fill(int sampleCount, Span<double> destination)
    {
        int requiredLength = RequiredOutputLength(sampleCount);
        Guard.ExactSpanLength(destination.Length, requiredLength, "destination");
        int sampleIndex = 0;
        int offset = 0;
        int matrixLength = dimension * dimension;
        while (sampleIndex < sampleCount)
        {
            SampleUnchecked(destination, offset);
            offset += matrixLength;
            sampleIndex++;
        }
    }

    void SampleUnchecked(Span<double> destination, int destinationOffset)
    {
        int column = 0;
        while (column < dimension)
        {
            double maximum;
            while (true)
            {
                normalSampler.Fill(candidate);
                int pass = 0;
                while (pass < 2)
                {
                    int previousColumn = 0;
                    while (previousColumn < column)
                    {
                        double projection = 0.0;
                        int row = 0;
                        while (row < dimension)
                        {
                            projection += candidate[row] * destination[destinationOffset + row * dimension + previousColumn];
                            row++;
                        }

                        row = 0;
                        while (row < dimension)
                        {
                            candidate[row] -= projection * destination[destinationOffset + row * dimension + previousColumn];
                            row++;
                        }

                        previousColumn++;
                    }

                    pass++;
                }

                maximum = 0.0;
                int magnitudeIndex = 0;
                while (magnitudeIndex < dimension)
                {
                    maximum = Math.Max(maximum, Math.Abs(candidate[magnitudeIndex]));
                    magnitudeIndex++;
                }

                if (maximum > MinimumResidualMagnitude)
                {
                    break;
                }
            }

            double scaledSquaredNorm = 0.0;
            int component = 0;
            while (component < dimension)
            {
                double scaled = candidate[component] / maximum;
                scaledSquaredNorm += scaled * scaled;
                component++;
            }

            double inverseScaledNorm = 1.0 / Sqrt(scaledSquaredNorm);
            component = 0;
            while (component < dimension)
            {
                destination[destinationOffset + component * dimension + column] =
                    (candidate[component] / maximum) * inverseScaledNorm;
                component++;
            }

            column++;
        }
    }

    double Sqrt(double value)
    {
        if (generator.MathMode() == RandomMathMode.Portable)
        {
            return PortableMath.Sqrt(value);
        }

        return Math.Sqrt(value);
    }
}
