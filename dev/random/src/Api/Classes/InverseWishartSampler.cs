using System;

namespace random;

/// <summary>
/// Generates symmetric positive-definite matrices from an inverse-Wishart distribution.
/// </summary>
/// <remarks>
/// The parameterization is <c>IW(dimension, degreesOfFreedom, scale)</c>. When
/// <c>degreesOfFreedom &gt; dimension + 1</c>, its mean is
/// <c>scale / (degreesOfFreedom - dimension - 1)</c>. Sampling uses a shared
/// lower-triangular Bartlett decomposition and deterministic triangular solves.
/// The scale matrix is copied during construction. The sampler retains the supplied
/// mutable generator and is not safe for concurrent use without external synchronization.
/// </remarks>
public class InverseWishartSampler : IInverseWishartSampler
{
    BartlettDecomposition bartlett;
    double[] scaleFactor;
    double[] bartlettFactor;
    double[] transformedFactor;
    double[] result;
    int dimension;

    /// <summary>
    /// Creates an inverse-Wishart sampler with the requested degrees of freedom and scale matrix.
    /// </summary>
    /// <param name="generator">The generator that supplies random values and selects the math mode.</param>
    /// <param name="degreesOfFreedom">The finite degrees of freedom, which must exceed dimension minus one.</param>
    /// <param name="scale">The finite symmetric positive-definite inverse-Wishart scale matrix in row-major order.</param>
    /// <param name="dimension">The positive number of rows and columns in the scale and sampled matrices.</param>
    /// <remarks>
    /// The scale array is copied through its Cholesky factor. Near-singular Bartlett
    /// factors or extreme scales can produce non-finite entries after division overflow.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The generator or scale is null; the dimension or degrees of freedom is invalid; or the scale has an invalid shape, is non-finite, asymmetric, or not positive definite.</exception>
    public InverseWishartSampler(IRandomSource generator, double degreesOfFreedom, double[] scale, int dimension)
    {
        double[] validatedScaleFactor = MatrixVariateOperations.ValidateAndFactorScale(generator, degreesOfFreedom, scale, dimension);
        this.dimension = dimension;
        scaleFactor = validatedScaleFactor;
        bartlett = new BartlettDecomposition(generator, degreesOfFreedom, dimension);
        bartlettFactor = new double[validatedScaleFactor.Length];
        transformedFactor = new double[validatedScaleFactor.Length];
        result = new double[validatedScaleFactor.Length];
    }

    /// <summary>
    /// Returns the number of rows and columns in each sampled matrix.
    /// </summary>
    /// <returns>The positive matrix dimension.</returns>
    public int Dimension()
    {
        return dimension;
    }

    /// <summary>
    /// Returns the exact row-major storage length required for one sampled matrix.
    /// </summary>
    /// <returns>The square of <see cref="Dimension()"/>.</returns>
    public int RequiredOutputLength()
    {
        return result.Length;
    }

    /// <summary>
    /// Returns the exact storage length required for a row-major batch.
    /// </summary>
    /// <param name="sampleCount">The nonnegative number of matrix samples.</param>
    /// <returns>The product of the sample count and <see cref="RequiredOutputLength()"/>.</returns>
    /// <exception cref="RandomArgumentException">The sample count is negative or the required length exceeds <c>int.MaxValue</c>.</exception>
    public int RequiredBatchOutputLength(int sampleCount)
    {
        return MultivariateDistributions.RequiredVectorOutputLength(sampleCount, result.Length);
    }

    /// <summary>
    /// Creates one row-major matrix from this inverse-Wishart distribution.
    /// </summary>
    /// <returns>A new symmetric matrix with <see cref="RequiredOutputLength()"/> entries.</returns>
    public double[] Sample()
    {
        double[] destination = new double[result.Length];
        SampleInto(destination.AsSpan());
        return destination;
    }

    /// <summary>
    /// Fills a sample-major batch with row-major matrices from this inverse-Wishart distribution.
    /// </summary>
    /// <param name="sampleCount">The nonnegative number of matrix samples.</param>
    /// <param name="destination">The destination, whose length must equal <see cref="RequiredBatchOutputLength(int)"/> for the sample count.</param>
    /// <remarks>The result and generator state match repeated calls to <see cref="SampleInto(Span{double})"/> in increasing sample order.</remarks>
    /// <exception cref="RandomArgumentException">The sample count is invalid or the destination length does not match the required sample-major storage.</exception>
    public void Fill(int sampleCount, Span<double> destination)
    {
        int requiredLength = RequiredBatchOutputLength(sampleCount);
        Guard.ExactSpanLength(destination.Length, requiredLength, "destination");
        int sample = 0;
        int offset = 0;
        while (sample < sampleCount)
        {
            SampleInto(destination.Slice(offset, result.Length));
            offset += result.Length;
            sample++;
        }
    }

    /// <summary>
    /// Writes one row-major matrix from this inverse-Wishart distribution.
    /// </summary>
    /// <param name="destination">The destination, whose length must equal <see cref="RequiredOutputLength()"/>.</param>
    /// <remarks>Entry at row <c>r</c> and column <c>c</c> is written at <c>destination[r * Dimension() + c]</c>.</remarks>
    /// <exception cref="RandomArgumentException">The destination length does not equal <see cref="RequiredOutputLength()"/>.</exception>
    public void SampleInto(Span<double> destination)
    {
        Guard.ExactSpanLength(destination.Length, result.Length, "destination");
        bartlett.Sample(bartlettFactor);
        MatrixVariateOperations.SolveRightTransposeLower(scaleFactor, bartlettFactor, dimension, transformedFactor);
        MatrixVariateOperations.SymmetricProduct(transformedFactor, dimension, result);
        MatrixVariateOperations.Copy(result, destination);
    }
}
