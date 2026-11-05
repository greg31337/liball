using System;

namespace random;

/// <summary>
/// Generates one point in every cell of a regular unit-hypercube grid.
/// </summary>
/// <remarks>
/// Points are returned in lexicographic cell order with the last coordinate
/// changing fastest. An inferred grid requires the requested point count to be
/// an exact power of the configured dimension. A configured rectangular grid
/// requires its exact product. The sampler retains the supplied mutable generator
/// and is not safe for concurrent use without external synchronization.
/// </remarks>
public class StratifiedSampler : IStratifiedSampler
{
    IRandomSource generator;
    int dimension;
    bool centered;
    int[] configuredStrata;
    int configuredPointCount;

    /// <summary>
    /// Creates a randomized or centered sampler with the same positive stratum count on every axis.
    /// </summary>
    /// <param name="generator">The generator used for randomized within-cell offsets.</param>
    /// <param name="dimension">The positive point dimension.</param>
    /// <param name="strataPerDimension">The positive number of strata on every axis.</param>
    /// <param name="centered">Whether every point is placed at its cell center.</param>
    /// <exception cref="RandomArgumentException">An argument is invalid or the grid contains too many points.</exception>
    public StratifiedSampler(IRandomSource generator, int dimension, int strataPerDimension, bool centered)
    {
        Guard.NotNull(generator, "generator");
        Guard.PositiveCount(dimension, "dimension");
        Guard.PositiveCount(strataPerDimension, "strataPerDimension");
        this.generator = generator;
        this.dimension = dimension;
        this.centered = centered;
        configuredStrata = UniformStrata(dimension, strataPerDimension);
        configuredPointCount = GridPointCount(configuredStrata, "strataPerDimension");
    }

    /// <summary>
    /// Creates a sampler whose equal grid shape is inferred for each request.
    /// </summary>
    /// <param name="generator">The generator used for randomized within-cell offsets.</param>
    /// <param name="dimension">The positive point dimension.</param>
    /// <param name="centered">Whether every point is placed at its cell center.</param>
    /// <returns>A sampler with no fixed grid.</returns>
    /// <exception cref="RandomArgumentException">The generator is null or the dimension is not positive.</exception>
    internal static StratifiedSampler CreateInferred(IRandomSource generator, int dimension, bool centered)
    {
        StratifiedSampler sampler = new StratifiedSampler(generator, dimension, 1, centered);
        sampler.configuredStrata = Array.Empty<int>();
        sampler.configuredPointCount = 0;
        return sampler;
    }

    /// <summary>
    /// Creates a sampler with a fixed rectangular grid shape.
    /// </summary>
    /// <param name="generator">The generator used for randomized within-cell offsets.</param>
    /// <param name="strataPerDimension">The nonempty positive stratum counts, one per axis.</param>
    /// <param name="centered">Whether every point is placed at its cell center.</param>
    /// <returns>A sampler configured with a copy of the supplied grid shape.</returns>
    /// <exception cref="RandomArgumentException">An argument is null or invalid, or the grid contains too many points.</exception>
    internal static StratifiedSampler FromStrata(IRandomSource generator, int[] strataPerDimension, bool centered)
    {
        Guard.NotNull(generator, "generator");
        int[] configuredStrata = ValidateAndCopyStrata(strataPerDimension);
        int pointCount = GridPointCount(configuredStrata, "strataPerDimension");
        StratifiedSampler sampler = new StratifiedSampler(generator, configuredStrata.Length, 1, centered);
        sampler.configuredStrata = configuredStrata;
        sampler.configuredPointCount = pointCount;
        return sampler;
    }

    /// <summary>
    /// Returns the number of coordinates in each point.
    /// </summary>
    /// <returns>The positive point dimension.</returns>
    public int Dimension()
    {
        return dimension;
    }

    /// <summary>
    /// Reports whether points are centered in their cells.
    /// </summary>
    /// <returns><c>true</c> for cell centers; otherwise, <c>false</c>.</returns>
    public bool IsCentered()
    {
        return centered;
    }

    /// <summary>
    /// Returns the fixed grid's point count, or zero when the grid is inferred per request.
    /// </summary>
    /// <returns>The configured grid product, or zero.</returns>
    public int ConfiguredPointCount()
    {
        return configuredPointCount;
    }

    /// <summary>
    /// Returns one complete regular stratified design.
    /// </summary>
    /// <param name="pointCount">The exact grid point count.</param>
    /// <returns>A new point-major array containing one point per cell.</returns>
    /// <exception cref="RandomArgumentException">The point count does not define the configured grid or required storage is invalid.</exception>
    public double[] Sample(int pointCount)
    {
        int requiredLength = RequiredOutputLength(pointCount);
        ResolveStrata(pointCount);
        double[] result = new double[requiredLength];
        SampleInto(pointCount, result);
        return result;
    }

    /// <summary>
    /// Writes one complete regular stratified design.
    /// </summary>
    /// <param name="pointCount">The exact grid point count.</param>
    /// <param name="destination">The exact point-major destination.</param>
    /// <remarks>Point <c>p</c>, coordinate <c>d</c> is stored at <c>destination[p * Dimension() + d]</c>.</remarks>
    /// <exception cref="RandomArgumentException">The point count does not define the configured grid or the destination is invalid.</exception>
    public void SampleInto(int pointCount, Span<double> destination)
    {
        int requiredLength = RequiredOutputLength(pointCount);
        int[] strata = ResolveStrata(pointCount);
        Guard.ExactSpanLength(destination.Length, requiredLength, "destination");
        int[] cell = new int[dimension];
        int point = 0;
        while (point < pointCount)
        {
            int coordinate = 0;
            while (coordinate < dimension)
            {
                double offset = 0.5;
                if (!centered)
                {
                    offset = FloatingUniform.NextDoubleClosedOpen(generator);
                }

                destination[point * dimension + coordinate] = StratifiedValue(cell[coordinate], strata[coordinate], offset);
                coordinate++;
            }

            AdvanceCell(cell, strata);
            point++;
        }
    }

    /// <summary>
    /// Returns the required point-major storage length for a design.
    /// </summary>
    /// <param name="pointCount">The positive number of points.</param>
    /// <returns>The point count multiplied by the dimension.</returns>
    /// <exception cref="RandomArgumentException">The point count is not positive or required storage exceeds <c>int.MaxValue</c>.</exception>
    public int RequiredOutputLength(int pointCount)
    {
        Guard.PositiveCount(pointCount, "pointCount");
        if (pointCount > int.MaxValue / dimension)
        {
            throw RandomArgumentException.Create("pointCount", "The requested design contains too many coordinates.");
        }

        return pointCount * dimension;
    }

    int[] ResolveStrata(int pointCount)
    {
        if (configuredPointCount != 0)
        {
            if (pointCount != configuredPointCount)
            {
                throw RandomArgumentException.Create("pointCount", "The point count must equal the configured grid product.");
            }

            return configuredStrata;
        }

        int strata = ExactPowerRoot(pointCount, dimension);
        return UniformStrata(dimension, strata);
    }

    static int ExactPowerRoot(int pointCount, int dimension)
    {
        int low = 1;
        int high = pointCount;
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            int comparison = ComparePower(middle, dimension, pointCount);
            if (comparison == 0)
            {
                return middle;
            }

            if (comparison < 0)
            {
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        throw RandomArgumentException.Create("pointCount", "The point count must be an exact power of the dimension for an equal regular grid.");
    }

    static int ComparePower(int value, int exponent, int target)
    {
        long product = 1L;
        int index = 0;
        while (index < exponent)
        {
            if (product > target / value)
            {
                return 1;
            }

            product *= value;
            index++;
        }

        if (product < target)
        {
            return -1;
        }

        if (product > target)
        {
            return 1;
        }

        return 0;
    }

    static int[] UniformStrata(int dimension, int strataPerDimension)
    {
        int[] result = new int[dimension];
        int index = 0;
        while (index < dimension)
        {
            result[index] = strataPerDimension;
            index++;
        }

        return result;
    }

    static int[] ValidateAndCopyStrata(int[] strataPerDimension)
    {
        Guard.NotNull(strataPerDimension, "strataPerDimension");
        Guard.PositiveCount(strataPerDimension.Length, "strataPerDimension");
        int[] result = new int[strataPerDimension.Length];
        int index = 0;
        while (index < strataPerDimension.Length)
        {
            if (strataPerDimension[index] <= 0)
            {
                throw RandomArgumentException.Create("strataPerDimension", "Every stratum count must be positive.");
            }

            result[index] = strataPerDimension[index];
            index++;
        }

        return result;
    }

    static int GridPointCount(int[] strata, string argumentName)
    {
        int product = 1;
        int index = 0;
        while (index < strata.Length)
        {
            if (product > int.MaxValue / strata[index])
            {
                throw RandomArgumentException.Create(argumentName, "The grid contains too many points.");
            }

            product *= strata[index];
            index++;
        }

        return product;
    }

    static void AdvanceCell(int[] cell, int[] strata)
    {
        int coordinate = cell.Length - 1;
        while (coordinate >= 0)
        {
            cell[coordinate]++;
            if (cell[coordinate] < strata[coordinate])
            {
                return;
            }

            cell[coordinate] = 0;
            coordinate--;
        }
    }

    static double StratifiedValue(int stratum, int stratumCount, double offset)
    {
        double result = ((double)stratum + offset) / (double)stratumCount;
        if (result >= 1.0)
        {
            return Math.BitDecrement(1.0);
        }

        return result;
    }
}
