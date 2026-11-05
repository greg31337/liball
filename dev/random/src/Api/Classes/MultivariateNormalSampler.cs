using System;

namespace random;

/// <summary>
/// Generates vectors from a multivariate normal distribution.
/// </summary>
/// <remarks>
/// The covariance constructor computes a deterministic lower-triangular Cholesky
/// factor. <see cref="RandomFactory.CreateMultivariateNormalFromCholesky"/> accepts
/// that factor directly. Mean vectors and matrix storage are copied during
/// construction. The sampler retains the supplied mutable generator and is not
/// safe for concurrent use without external synchronization.
/// </remarks>
public class MultivariateNormalSampler : IMultivariateNormalSampler
{
    IRandomSource generator;
    NormalSampler normalSampler;
    double[] mean = new double[0];
    double[] lowerFactor = new double[0];
    double[] standardNormals = new double[0];

    /// <summary>
    /// Creates a multivariate normal sampler from a mean vector and covariance matrix.
    /// </summary>
    /// <param name="generator">The generator that supplies random values and selects the math mode.</param>
    /// <param name="mean">The nonempty finite mean vector.</param>
    /// <param name="covariance">The finite symmetric positive-definite covariance matrix in row-major order.</param>
    /// <remarks>The mean and covariance arrays are copied.</remarks>
    /// <exception cref="RandomArgumentException">The generator, mean, or covariance is null; the mean is empty or non-finite; or the covariance has an invalid shape, is non-finite, asymmetric, or not positive definite.</exception>
    public MultivariateNormalSampler(IRandomSource generator, double[] mean, double[] covariance)
    {
        Guard.NotNull(generator, "generator");
        int dimension = ValidateMean(mean);
        double[] factor = FactorCovariance(covariance, dimension);
        this.generator = generator;
        normalSampler = new NormalSampler(generator, 0.0, 1.0);
        Initialize(generator, mean, factor, dimension);
    }

    /// <summary>
    /// Creates a multivariate normal sampler from a mean vector and lower Cholesky factor.
    /// </summary>
    /// <param name="generator">The generator that supplies random values and selects the math mode.</param>
    /// <param name="mean">The nonempty finite mean vector.</param>
    /// <param name="choleskyFactor">The finite lower-triangular Cholesky factor with a positive diagonal, in row-major order.</param>
    /// <returns>A sampler that uses the caller-supplied factor.</returns>
    /// <remarks>The mean and factor arrays are copied.</remarks>
    /// <exception cref="RandomArgumentException">The generator, mean, or factor is null; the mean is empty or non-finite; or the factor has an invalid shape, is non-finite, is not lower triangular, or has a nonpositive diagonal.</exception>
    internal static MultivariateNormalSampler FromCholesky(IRandomSource generator, double[] mean, double[] choleskyFactor)
    {
        Guard.NotNull(generator, "generator");
        int dimension = ValidateMean(mean);
        ValidateCholeskyFactor(choleskyFactor, dimension);
        MultivariateNormalSampler sampler = new MultivariateNormalSampler(generator, mean, IdentityMatrix(dimension));
        sampler.Initialize(generator, mean, choleskyFactor, dimension);
        return sampler;
    }

    static double[] IdentityMatrix(int dimension)
    {
        double[] identity = new double[dimension * dimension];
        int index = 0;
        while (index < dimension)
        {
            identity[index * dimension + index] = 1.0;
            index++;
        }

        return identity;
    }

    /// <summary>
    /// Returns the number of components represented by each sampled vector.
    /// </summary>
    /// <returns>The positive vector dimension.</returns>
    public int Dimension()
    {
        return mean.Length;
    }

    /// <summary>
    /// Returns the exact storage length required for a row-major batch.
    /// </summary>
    /// <param name="sampleCount">The nonnegative number of vector rows.</param>
    /// <returns>The product of the sample count and <see cref="Dimension()"/>.</returns>
    /// <exception cref="RandomArgumentException">The sample count is negative or the required length exceeds <c>int.MaxValue</c>.</exception>
    public int RequiredOutputLength(int sampleCount)
    {
        Guard.Count(sampleCount, "sampleCount");
        if (sampleCount > int.MaxValue / mean.Length)
        {
            throw RandomArgumentException.Create("sampleCount", "The requested batch contains too many elements.");
        }

        return sampleCount * mean.Length;
    }

    /// <summary>
    /// Returns the exact storage length required for a row-major batch.
    /// </summary>
    /// <param name="sampleCount">The nonnegative number of vector rows.</param>
    /// <returns>The product of the sample count and <see cref="Dimension()"/>.</returns>
    /// <exception cref="RandomArgumentException">The sample count is negative or the required length exceeds <c>int.MaxValue</c>.</exception>
    public int RequiredBatchOutputLength(int sampleCount)
    {
        return RequiredOutputLength(sampleCount);
    }

    /// <summary>
    /// Creates one vector from this multivariate normal distribution.
    /// </summary>
    /// <returns>A new vector with <see cref="Dimension()"/> components.</returns>
    public double[] Sample()
    {
        double[] result = new double[mean.Length];
        SampleInto(result.AsSpan());
        return result;
    }

    /// <summary>
    /// Writes one vector from this multivariate normal distribution.
    /// </summary>
    /// <param name="destination">The destination, whose length must equal <see cref="Dimension()"/>.</param>
    /// <exception cref="RandomArgumentException">The destination length does not equal <see cref="Dimension()"/>.</exception>
    public void SampleInto(Span<double> destination)
    {
        Guard.ExactSpanLength(destination.Length, mean.Length, "destination");
        SampleUnchecked(destination, 0);
    }

    /// <summary>
    /// Fills a row-major batch with vectors from this multivariate normal distribution.
    /// </summary>
    /// <param name="sampleCount">The nonnegative number of vector rows.</param>
    /// <param name="destination">The destination, whose length must equal <see cref="RequiredOutputLength(int)"/> for the sample count.</param>
    /// <remarks>
    /// Row <c>r</c>, component <c>c</c> is written at
    /// <c>destination[r * Dimension() + c]</c>. The result and generator state match
    /// repeated calls to <see cref="SampleInto(Span{double})"/> in increasing row order.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The sample count is invalid or the destination length does not match the required row-major storage.</exception>
    public void Fill(int sampleCount, Span<double> destination)
    {
        int requiredLength = RequiredBatchOutputLength(sampleCount);
        Guard.ExactSpanLength(destination.Length, requiredLength, "destination");
        int row = 0;
        int offset = 0;
        while (row < sampleCount)
        {
            SampleUnchecked(destination, offset);
            offset += mean.Length;
            row++;
        }
    }

    void Initialize(IRandomSource generator, double[] mean, double[] factor, int dimension)
    {
        this.mean = (double[])mean.Clone();
        lowerFactor = (double[])factor.Clone();
        standardNormals = new double[dimension];
    }

    void SampleUnchecked(Span<double> destination, int destinationOffset)
    {
        int component = 0;
        while (component < standardNormals.Length)
        {
            standardNormals[component] = normalSampler.Sample();
            component++;
        }

        int row = 0;
        while (row < mean.Length)
        {
            double value = mean[row];
            int column = 0;
            while (column <= row)
            {
                value += lowerFactor[row * mean.Length + column] * standardNormals[column];
                column++;
            }

            destination[destinationOffset + row] = value;
            row++;
        }
    }

    static int ValidateMean(double[] mean)
    {
        Guard.NotNull(mean, "mean");
        if (mean.Length == 0)
        {
            throw RandomArgumentException.Create("mean", "At least one mean component is required.");
        }

        int index = 0;
        while (index < mean.Length)
        {
            if (!double.IsFinite(mean[index]))
            {
                throw RandomArgumentException.Create("mean", "Every mean component must be finite.");
            }

            index++;
        }

        return mean.Length;
    }

    static int ValidateMatrixStorage(double[] matrix, int dimension, string argumentName)
    {
        Guard.NotNull(matrix, argumentName);
        if (dimension > int.MaxValue / dimension)
        {
            throw RandomArgumentException.Create(argumentName, "The matrix shape contains too many elements.");
        }

        int requiredLength = dimension * dimension;
        if (matrix.Length != requiredLength)
        {
            throw RandomArgumentException.Create(argumentName, "The matrix storage length does not match the vector dimension.");
        }

        int index = 0;
        while (index < matrix.Length)
        {
            if (!double.IsFinite(matrix[index]))
            {
                throw RandomArgumentException.Create(argumentName, "Every matrix element must be finite.");
            }

            index++;
        }

        return requiredLength;
    }

    static double[] FactorCovariance(double[] covariance, int dimension)
    {
        int elementCount = ValidateMatrixStorage(covariance, dimension, "covariance");
        int row = 0;
        while (row < dimension)
        {
            int column = 0;
            while (column < row)
            {
                if (covariance[row * dimension + column] != covariance[column * dimension + row])
                {
                    throw RandomArgumentException.Create("covariance", "The covariance matrix must be symmetric.");
                }

                column++;
            }

            row++;
        }

        double[] result = new double[elementCount];
        row = 0;
        while (row < dimension)
        {
            int column = 0;
            while (column <= row)
            {
                double sum = covariance[row * dimension + column];
                int offset = 0;
                while (offset < column)
                {
                    sum -= result[row * dimension + offset] * result[column * dimension + offset];
                    offset++;
                }

                if (row == column)
                {
                    if (!(sum > 0.0) || !double.IsFinite(sum))
                    {
                        throw RandomArgumentException.Create("covariance", "The covariance matrix must be positive definite.");
                    }

                    result[row * dimension + column] = PortableMath.Sqrt(sum);
                }
                else
                {
                    result[row * dimension + column] = sum / result[column * dimension + column];
                }

                column++;
            }

            row++;
        }

        return result;
    }

    static void ValidateCholeskyFactor(double[] factor, int dimension)
    {
        ValidateMatrixStorage(factor, dimension, "choleskyFactor");
        int row = 0;
        while (row < dimension)
        {
            int column = 0;
            while (column < dimension)
            {
                double value = factor[row * dimension + column];
                if (column > row && value != 0.0)
                {
                    throw RandomArgumentException.Create("choleskyFactor", "The Cholesky factor must be lower triangular.");
                }

                if (column == row && !(value > 0.0))
                {
                    throw RandomArgumentException.Create("choleskyFactor", "The Cholesky factor must have a positive diagonal.");
                }

                column++;
            }

            row++;
        }
    }
}
