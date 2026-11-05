using System;

namespace random;

/// <summary>
/// Provides one-shot and batch sampling for multivariate and matrix distributions.
/// </summary>
/// <remarks>
/// Batch storage is sample-major: each vector or row-major matrix immediately follows
/// the previous sample. Every method constructs and validates its reusable sampler
/// before writing output, so a destination may alias an input array when their lengths
/// permit. Destination lengths must match exactly; invalid requests consume no random state.
/// With a deterministic generator, Portable math mode provides bit-for-bit replay for the same
/// parameters and component order; System math mode promises statistical equivalence only.
/// </remarks>
internal static class MultivariateDistributions
{
    /// <summary>Returns the exact storage length required for a batch of fixed-length vectors.</summary>
    /// <param name="sampleCount">The nonnegative number of vectors.</param>
    /// <param name="componentCount">The positive number of components per vector.</param>
    /// <returns>The product of sample count and component count.</returns>
    /// <exception cref="RandomArgumentException">A count is invalid or the product exceeds <c>int.MaxValue</c>.</exception>
    public static int RequiredVectorOutputLength(int sampleCount, int componentCount)
    {
        Guard.Count(sampleCount, "sampleCount");
        Guard.PositiveCount(componentCount, "componentCount");
        if (sampleCount > int.MaxValue / componentCount)
        {
            throw RandomArgumentException.Create("sampleCount", "The requested vector batch contains too many elements.");
        }

        return sampleCount * componentCount;
    }

    /// <summary>Returns the exact storage length required for a batch of row-major matrices.</summary>
    /// <param name="sampleCount">The nonnegative number of matrices.</param>
    /// <param name="rows">The positive row count per matrix.</param>
    /// <param name="columns">The positive column count per matrix.</param>
    /// <returns>The product of sample count, rows, and columns.</returns>
    /// <exception cref="RandomArgumentException">A count is invalid or either product exceeds <c>int.MaxValue</c>.</exception>
    public static int RequiredMatrixOutputLength(int sampleCount, int rows, int columns)
    {
        int matrixLength = Guard.MatrixElementCount(rows, columns, "rows", "columns");
        return RequiredVectorOutputLength(sampleCount, matrixLength);
    }

    /// <summary>Writes one Dirichlet probability vector.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="concentrations">The nonempty finite positive concentration vector.</param>
    /// <param name="destination">The destination whose length equals the component count.</param>
    public static void Dirichlet(IRandomSource generator, double[] concentrations, Span<double> destination)
    {
        new DirichletSampler(generator, concentrations).SampleInto(destination);
    }

    /// <summary>Fills a sample-major batch of Dirichlet probability vectors.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="concentrations">The nonempty finite positive concentration vector.</param>
    /// <param name="sampleCount">The nonnegative number of vectors.</param>
    /// <param name="destination">The exact sample-major destination.</param>
    public static void FillDirichlet(IRandomSource generator, double[] concentrations, int sampleCount, Span<double> destination)
    {
        DirichletSampler sampler = new DirichletSampler(generator, concentrations);
        int componentCount = sampler.ComponentCount();
        int requiredLength = RequiredVectorOutputLength(sampleCount, componentCount);
        Guard.ExactSpanLength(destination.Length, requiredLength, "destination");
        int sampleIndex = 0;
        int offset = 0;
        while (sampleIndex < sampleCount)
        {
            sampler.SampleInto(destination.Slice(offset, componentCount));
            offset += componentCount;
            sampleIndex++;
        }
    }

    /// <summary>Writes one multivariate-normal vector from a covariance matrix.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="mean">The finite mean vector.</param>
    /// <param name="covariance">The symmetric positive-definite covariance matrix.</param>
    /// <param name="destination">The destination whose length equals the vector dimension.</param>
    public static void MultivariateNormal(IRandomSource generator, double[] mean, double[] covariance, Span<double> destination)
    {
        new MultivariateNormalSampler(generator, mean, covariance).SampleInto(destination);
    }

    /// <summary>Fills a sample-major batch of multivariate-normal vectors from a covariance matrix.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="mean">The finite mean vector.</param>
    /// <param name="covariance">The symmetric positive-definite covariance matrix.</param>
    /// <param name="sampleCount">The nonnegative number of vectors.</param>
    /// <param name="destination">The exact sample-major destination.</param>
    public static void FillMultivariateNormal(IRandomSource generator, double[] mean, double[] covariance, int sampleCount, Span<double> destination)
    {
        MultivariateNormalSampler sampler = new MultivariateNormalSampler(generator, mean, covariance);
        sampler.Fill(sampleCount, destination);
    }

    /// <summary>Writes one multivariate-normal vector from a lower Cholesky factor.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="mean">The finite mean vector.</param>
    /// <param name="choleskyFactor">The lower covariance factor.</param>
    /// <param name="destination">The destination whose length equals the vector dimension.</param>
    public static void MultivariateNormalFromCholesky(IRandomSource generator, double[] mean, double[] choleskyFactor, Span<double> destination)
    {
        MultivariateNormalSampler.FromCholesky(generator, mean, choleskyFactor).SampleInto(destination);
    }

    /// <summary>Fills a sample-major batch of multivariate-normal vectors from a lower Cholesky factor.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="mean">The finite mean vector.</param>
    /// <param name="choleskyFactor">The lower covariance factor.</param>
    /// <param name="sampleCount">The nonnegative number of vectors.</param>
    /// <param name="destination">The exact sample-major destination.</param>
    public static void FillMultivariateNormalFromCholesky(IRandomSource generator, double[] mean, double[] choleskyFactor, int sampleCount, Span<double> destination)
    {
        MultivariateNormalSampler sampler = MultivariateNormalSampler.FromCholesky(generator, mean, choleskyFactor);
        sampler.Fill(sampleCount, destination);
    }

    /// <summary>Writes one row-major Wishart matrix.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="degreesOfFreedom">The degrees of freedom.</param>
    /// <param name="scale">The symmetric positive-definite scale matrix.</param>
    /// <param name="dimension">The matrix dimension.</param>
    /// <param name="destination">The exact row-major matrix destination.</param>
    public static void Wishart(IRandomSource generator, double degreesOfFreedom, double[] scale, int dimension, Span<double> destination)
    {
        new WishartSampler(generator, degreesOfFreedom, scale, dimension).SampleInto(destination);
    }

    /// <summary>Fills a sample-major batch of row-major Wishart matrices.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="degreesOfFreedom">The degrees of freedom.</param>
    /// <param name="scale">The symmetric positive-definite scale matrix.</param>
    /// <param name="dimension">The matrix dimension.</param>
    /// <param name="sampleCount">The nonnegative number of matrices.</param>
    /// <param name="destination">The exact sample-major destination.</param>
    public static void FillWishart(IRandomSource generator, double degreesOfFreedom, double[] scale, int dimension, int sampleCount, Span<double> destination)
    {
        WishartSampler sampler = new WishartSampler(generator, degreesOfFreedom, scale, dimension);
        int matrixLength = sampler.RequiredOutputLength();
        int requiredLength = RequiredVectorOutputLength(sampleCount, matrixLength);
        Guard.ExactSpanLength(destination.Length, requiredLength, "destination");
        int sampleIndex = 0;
        int offset = 0;
        while (sampleIndex < sampleCount)
        {
            sampler.SampleInto(destination.Slice(offset, matrixLength));
            offset += matrixLength;
            sampleIndex++;
        }
    }

    /// <summary>Writes one row-major inverse-Wishart matrix.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="degreesOfFreedom">The degrees of freedom.</param>
    /// <param name="scale">The symmetric positive-definite inverse-Wishart scale matrix.</param>
    /// <param name="dimension">The matrix dimension.</param>
    /// <param name="destination">The exact row-major matrix destination.</param>
    public static void InverseWishart(IRandomSource generator, double degreesOfFreedom, double[] scale, int dimension, Span<double> destination)
    {
        new InverseWishartSampler(generator, degreesOfFreedom, scale, dimension).SampleInto(destination);
    }

    /// <summary>Fills a sample-major batch of row-major inverse-Wishart matrices.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="degreesOfFreedom">The degrees of freedom.</param>
    /// <param name="scale">The symmetric positive-definite inverse-Wishart scale matrix.</param>
    /// <param name="dimension">The matrix dimension.</param>
    /// <param name="sampleCount">The nonnegative number of matrices.</param>
    /// <param name="destination">The exact sample-major destination.</param>
    public static void FillInverseWishart(IRandomSource generator, double degreesOfFreedom, double[] scale, int dimension, int sampleCount, Span<double> destination)
    {
        InverseWishartSampler sampler = new InverseWishartSampler(generator, degreesOfFreedom, scale, dimension);
        int matrixLength = sampler.RequiredOutputLength();
        int requiredLength = RequiredVectorOutputLength(sampleCount, matrixLength);
        Guard.ExactSpanLength(destination.Length, requiredLength, "destination");
        int sampleIndex = 0;
        int offset = 0;
        while (sampleIndex < sampleCount)
        {
            sampler.SampleInto(destination.Slice(offset, matrixLength));
            offset += matrixLength;
            sampleIndex++;
        }
    }

    /// <summary>Writes one row-major matrix-normal matrix from row and column scale matrices.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="mean">The row-major mean matrix.</param>
    /// <param name="rows">The matrix row count.</param>
    /// <param name="columns">The matrix column count.</param>
    /// <param name="rowScale">The symmetric positive-definite row scale.</param>
    /// <param name="columnScale">The symmetric positive-definite column scale.</param>
    /// <param name="destination">The exact row-major matrix destination.</param>
    public static void MatrixNormal(IRandomSource generator, double[] mean, int rows, int columns, double[] rowScale, double[] columnScale, Span<double> destination)
    {
        new MatrixNormalSampler(generator, mean, rows, columns, rowScale, columnScale).SampleInto(destination);
    }

    /// <summary>Fills a sample-major batch of row-major matrix-normal matrices from scale matrices.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="mean">The row-major mean matrix.</param>
    /// <param name="rows">The matrix row count.</param>
    /// <param name="columns">The matrix column count.</param>
    /// <param name="rowScale">The symmetric positive-definite row scale.</param>
    /// <param name="columnScale">The symmetric positive-definite column scale.</param>
    /// <param name="sampleCount">The nonnegative number of matrices.</param>
    /// <param name="destination">The exact sample-major destination.</param>
    public static void FillMatrixNormal(IRandomSource generator, double[] mean, int rows, int columns, double[] rowScale, double[] columnScale, int sampleCount, Span<double> destination)
    {
        MatrixNormalSampler sampler = new MatrixNormalSampler(generator, mean, rows, columns, rowScale, columnScale);
        int matrixLength = sampler.RequiredOutputLength();
        int requiredLength = RequiredVectorOutputLength(sampleCount, matrixLength);
        Guard.ExactSpanLength(destination.Length, requiredLength, "destination");
        int sampleIndex = 0;
        int offset = 0;
        while (sampleIndex < sampleCount)
        {
            sampler.SampleInto(destination.Slice(offset, matrixLength));
            offset += matrixLength;
            sampleIndex++;
        }
    }

    /// <summary>Writes one row-major matrix-normal matrix from lower row and column factors.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="mean">The row-major mean matrix.</param>
    /// <param name="rows">The matrix row count.</param>
    /// <param name="columns">The matrix column count.</param>
    /// <param name="rowScaleFactor">The lower row scale factor.</param>
    /// <param name="columnScaleFactor">The lower column scale factor.</param>
    /// <param name="destination">The exact row-major matrix destination.</param>
    public static void MatrixNormalFromCholesky(IRandomSource generator, double[] mean, int rows, int columns, double[] rowScaleFactor, double[] columnScaleFactor, Span<double> destination)
    {
        MatrixNormalSampler.FromCholesky(generator, mean, rows, columns, rowScaleFactor, columnScaleFactor).SampleInto(destination);
    }

    /// <summary>Fills a sample-major batch of row-major matrix-normal matrices from lower factors.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="mean">The row-major mean matrix.</param>
    /// <param name="rows">The matrix row count.</param>
    /// <param name="columns">The matrix column count.</param>
    /// <param name="rowScaleFactor">The lower row scale factor.</param>
    /// <param name="columnScaleFactor">The lower column scale factor.</param>
    /// <param name="sampleCount">The nonnegative number of matrices.</param>
    /// <param name="destination">The exact sample-major destination.</param>
    public static void FillMatrixNormalFromCholesky(IRandomSource generator, double[] mean, int rows, int columns, double[] rowScaleFactor, double[] columnScaleFactor, int sampleCount, Span<double> destination)
    {
        MatrixNormalSampler sampler = MatrixNormalSampler.FromCholesky(generator, mean, rows, columns, rowScaleFactor, columnScaleFactor);
        int matrixLength = sampler.RequiredOutputLength();
        int requiredLength = RequiredVectorOutputLength(sampleCount, matrixLength);
        Guard.ExactSpanLength(destination.Length, requiredLength, "destination");
        int sampleIndex = 0;
        int offset = 0;
        while (sampleIndex < sampleCount)
        {
            sampler.SampleInto(destination.Slice(offset, matrixLength));
            offset += matrixLength;
            sampleIndex++;
        }
    }

    /// <summary>Writes one normal-gamma pair in <c>[value, precision]</c> order.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="location">The conditional-normal location.</param>
    /// <param name="meanPrecision">The conditional-normal precision multiplier.</param>
    /// <param name="shape">The gamma shape.</param>
    /// <param name="rate">The gamma rate.</param>
    /// <param name="destination">The exact two-entry destination.</param>
    public static void NormalGamma(IRandomSource generator, double location, double meanPrecision, double shape, double rate, Span<double> destination)
    {
        new NormalGammaSampler(generator, location, meanPrecision, shape, rate).SampleInto(destination);
    }

    /// <summary>Fills a sample-major batch of normal-gamma <c>[value, precision]</c> pairs.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="location">The conditional-normal location.</param>
    /// <param name="meanPrecision">The conditional-normal precision multiplier.</param>
    /// <param name="shape">The gamma shape.</param>
    /// <param name="rate">The gamma rate.</param>
    /// <param name="sampleCount">The nonnegative number of pairs.</param>
    /// <param name="destination">The exact sample-major destination.</param>
    public static void FillNormalGamma(IRandomSource generator, double location, double meanPrecision, double shape, double rate, int sampleCount, Span<double> destination)
    {
        NormalGammaSampler sampler = new NormalGammaSampler(generator, location, meanPrecision, shape, rate);
        int componentCount = sampler.ComponentCount();
        int requiredLength = RequiredVectorOutputLength(sampleCount, componentCount);
        Guard.ExactSpanLength(destination.Length, requiredLength, "destination");
        int sampleIndex = 0;
        int offset = 0;
        while (sampleIndex < sampleCount)
        {
            sampler.SampleInto(destination.Slice(offset, componentCount));
            offset += componentCount;
            sampleIndex++;
        }
    }
}
