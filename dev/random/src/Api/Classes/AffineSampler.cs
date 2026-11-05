using System;

namespace random;

/// <summary>
/// Applies a deterministic affine transform to a caller-supplied scalar sampler.
/// </summary>
/// <remarks>
/// For nonzero scale, each sample is evaluated in the fixed order
/// <c>offset + scale * source()</c>. Delegate state remains caller-owned, delegate
/// exceptions are propagated unchanged, and arithmetic overflow is returned unchanged.
/// </remarks>
public class AffineSampler : IAffineSampler
{
    Func<double> source;
    double offset;
    double scale;

    /// <summary>
    /// Creates an affine wrapper over a scalar sampling delegate.
    /// </summary>
    /// <param name="source">The non-null scalar sampler delegate.</param>
    /// <param name="offset">The finite additive offset.</param>
    /// <param name="scale">The finite signed multiplier.</param>
    /// <remarks>A zero scale defines a point mass at the offset and does not invoke the source.</remarks>
    /// <exception cref="RandomArgumentException">The source is null, or the offset or scale is non-finite.</exception>
    public AffineSampler(Func<double> source, double offset, double scale)
    {
        Guard.NotNull(source, "source");
        Guard.Finite(offset, "offset");
        Guard.Finite(scale, "scale");
        this.source = source;
        this.offset = offset;
        this.scale = scale;
    }

    /// <summary>
    /// Applies the configured affine transform to one source sample.
    /// </summary>
    /// <returns>The offset for zero scale, otherwise <c>offset + scale * source()</c>.</returns>
    public double Sample()
    {
        if (scale == 0.0)
        {
            return offset;
        }

        return offset + scale * source();
    }

    /// <summary>
    /// Fills a destination through repeated affine sampling.
    /// </summary>
    /// <param name="destination">The destination values.</param>
    /// <remarks>The result and source calls match repeated calls to <see cref="Sample()"/>.</remarks>
    public void Fill(Span<double> destination)
    {
        int index = 0;
        while (index < destination.Length)
        {
            destination[index] = Sample();
            index++;
        }
    }
}
