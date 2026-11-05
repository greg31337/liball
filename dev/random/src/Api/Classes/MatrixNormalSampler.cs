using System;

namespace random;

/// <summary>
/// Generates row-major matrices from a matrix-normal distribution.
/// </summary>
/// <remarks>
/// Samples use the parameterization <c>X = mean + rowFactor * Z * columnFactor^T</c>,
/// where <c>Z</c> contains independent standard normal values,
/// <c>rowScale = rowFactor * rowFactor^T</c>, and
/// <c>columnScale = columnFactor * columnFactor^T</c>. Thus the covariance between
/// entries <c>(r, c)</c> and <c>(s, d)</c> is
/// <c>rowScale[r, s] * columnScale[c, d]</c>. Inputs are copied during construction.
/// The sampler retains the supplied mutable generator and is not safe for concurrent
/// use without external synchronization.
/// </remarks>
public class MatrixNormalSampler : IMatrixNormalSampler
{
    NormalSampler normalSampler;
    double[] mean = new double[0];
    double[] rowFactor = new double[0];
    double[] columnFactor = new double[0];
    double[] standard = new double[0];
    double[] intermediate = new double[0];
    double[] result = new double[0];
    int rows;
    int columns;

    /// <summary>
    /// Creates a matrix-normal sampler from row and column scale matrices.
    /// </summary>
    /// <param name="generator">The generator that supplies random values and selects the math mode.</param>
    /// <param name="mean">The finite mean matrix in row-major order.</param>
    /// <param name="rows">The positive mean and row-scale row count.</param>
    /// <param name="columns">The positive mean and column-scale column count.</param>
    /// <param name="rowScale">The finite symmetric positive-definite row scale matrix in row-major order.</param>
    /// <param name="columnScale">The finite symmetric positive-definite column scale matrix in row-major order.</param>
    /// <remarks>The mean and both scale matrices are copied through private storage and Cholesky factors.</remarks>
    /// <exception cref="RandomArgumentException">The generator, mean, or a scale is null; a dimension or storage length is invalid; the mean is non-finite; or a scale is non-finite, asymmetric, or not positive definite.</exception>
    public MatrixNormalSampler(IRandomSource generator, double[] mean, int rows, int columns, double[] rowScale, double[] columnScale)
    {
        Guard.NotNull(generator, "generator");
        int elementCount = ValidateMean(mean, rows, columns);
        Guard.MatrixElementCount(rows, rows, "rows", "rows");
        Guard.MatrixElementCount(columns, columns, "columns", "columns");
        double[] validatedRowFactor = ArrayMatrix.Cholesky(rowScale, rows, "rowScale");
        double[] validatedColumnFactor = ArrayMatrix.Cholesky(columnScale, columns, "columnScale");
        normalSampler = new NormalSampler(generator, 0.0, 1.0);
        Initialize(generator, mean, rows, columns, validatedRowFactor, validatedColumnFactor, elementCount);
    }

    /// <summary>
    /// Creates a matrix-normal sampler from lower-triangular row and column scale factors.
    /// </summary>
    /// <param name="generator">The generator that supplies random values and selects the math mode.</param>
    /// <param name="mean">The finite mean matrix in row-major order.</param>
    /// <param name="rows">The positive mean and row-factor row count.</param>
    /// <param name="columns">The positive mean and column-factor column count.</param>
    /// <param name="rowScaleFactor">The finite lower-triangular row factor with a positive diagonal, in row-major order.</param>
    /// <param name="columnScaleFactor">The finite lower-triangular column factor with a positive diagonal, in row-major order.</param>
    /// <returns>A sampler that uses the supplied row and column scale factors.</returns>
    /// <remarks>The mean and both factors are copied.</remarks>
    /// <exception cref="RandomArgumentException">The generator, mean, or a factor is null; a dimension or storage length is invalid; the mean is non-finite; or a factor is non-finite, is not lower triangular, or has a nonpositive diagonal.</exception>
    internal static MatrixNormalSampler FromCholesky(IRandomSource generator, double[] mean, int rows, int columns, double[] rowScaleFactor, double[] columnScaleFactor)
    {
        Guard.NotNull(generator, "generator");
        int elementCount = ValidateMean(mean, rows, columns);
        double[] validatedRowFactor = MatrixVariateOperations.ValidateCholeskyFactor(rowScaleFactor, rows, "rowScaleFactor", "rows");
        double[] validatedColumnFactor = MatrixVariateOperations.ValidateCholeskyFactor(columnScaleFactor, columns, "columnScaleFactor", "columns");
        MatrixNormalSampler sampler = new MatrixNormalSampler(
            generator,
            mean,
            rows,
            columns,
            IdentityMatrix(rows),
            IdentityMatrix(columns));
        sampler.Initialize(generator, mean, rows, columns, validatedRowFactor, validatedColumnFactor, elementCount);
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
    /// Returns the number of rows in each sampled matrix.
    /// </summary>
    /// <returns>The positive row count.</returns>
    public int RowCount()
    {
        return rows;
    }

    /// <summary>
    /// Returns the number of columns in each sampled matrix.
    /// </summary>
    /// <returns>The positive column count.</returns>
    public int ColumnCount()
    {
        return columns;
    }

    /// <summary>
    /// Returns the exact row-major storage length required for one sampled matrix.
    /// </summary>
    /// <returns>The product of <see cref="RowCount()"/> and <see cref="ColumnCount()"/>.</returns>
    public int RequiredOutputLength()
    {
        return result.Length;
    }

    /// <summary>
    /// Returns the exact storage length required for a sample-major matrix batch.
    /// </summary>
    /// <param name="sampleCount">The nonnegative number of matrix samples.</param>
    /// <returns>The product of the sample count and <see cref="RequiredOutputLength()"/>.</returns>
    /// <exception cref="RandomArgumentException">The sample count is negative or the required length exceeds <c>int.MaxValue</c>.</exception>
    public int RequiredBatchOutputLength(int sampleCount)
    {
        return MultivariateDistributions.RequiredVectorOutputLength(sampleCount, result.Length);
    }

    /// <summary>
    /// Creates one row-major matrix from this matrix-normal distribution.
    /// </summary>
    /// <returns>A new matrix with <see cref="RequiredOutputLength()"/> entries.</returns>
    public double[] Sample()
    {
        double[] destination = new double[result.Length];
        SampleInto(destination.AsSpan());
        return destination;
    }

    /// <summary>
    /// Fills a sample-major batch with row-major matrices from this matrix-normal distribution.
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
    /// Writes one row-major matrix from this matrix-normal distribution.
    /// </summary>
    /// <param name="destination">The destination, whose length must equal <see cref="RequiredOutputLength()"/>.</param>
    /// <remarks>
    /// Entry at row <c>r</c> and column <c>c</c> is written at
    /// <c>destination[r * ColumnCount() + c]</c>. Standard normal values are drawn
    /// in that same row-major order.
    /// </remarks>
    /// <exception cref="RandomArgumentException">The destination length does not equal <see cref="RequiredOutputLength()"/>.</exception>
    public void SampleInto(Span<double> destination)
    {
        Guard.ExactSpanLength(destination.Length, result.Length, "destination");
        int index = 0;
        while (index < standard.Length)
        {
            standard[index] = normalSampler.Sample();
            index++;
        }

        MatrixVariateOperations.TransformMatrixNormal(rowFactor, columnFactor, standard, rows, columns, intermediate, result);
        index = 0;
        while (index < result.Length)
        {
            result[index] += mean[index];
            index++;
        }

        MatrixVariateOperations.Copy(result, destination);
    }

    void Initialize(IRandomSource generator, double[] mean, int rows, int columns, double[] rowFactor, double[] columnFactor, int elementCount)
    {
        this.mean = (double[])mean.Clone();
        this.rowFactor = (double[])rowFactor.Clone();
        this.columnFactor = (double[])columnFactor.Clone();
        this.rows = rows;
        this.columns = columns;
        standard = new double[elementCount];
        intermediate = new double[elementCount];
        result = new double[elementCount];
    }

    static int ValidateMean(double[] mean, int rows, int columns)
    {
        Guard.NotNull(mean, "mean");
        Guard.MatrixShape(mean.Length, rows, columns, "mean", "rows", "columns");
        int index = 0;
        while (index < mean.Length)
        {
            if (!double.IsFinite(mean[index]))
            {
                throw RandomArgumentException.Create("mean", "Every mean element must be finite.");
            }

            index++;
        }

        return mean.Length;
    }
}
