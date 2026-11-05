using System;

namespace random;

/// <summary>
/// Restricts a caller-supplied scalar sampler to an inclusive interval.
/// </summary>
/// <remarks>
/// Pass a supported built-in sampler's <c>Sample</c> method as the source delegate.
/// Candidates are requested in source order until one lies within the inclusive bounds.
/// NaN candidates are rejected. Infinite candidates are accepted only by corresponding
/// infinite bounds. No rejected candidate is returned as a fallback. Delegate state
/// remains caller-owned, and delegate exceptions are propagated unchanged.
/// </remarks>
public class TruncatedSampler : ITruncatedSampler
{
    Func<double> source;
    double lowerBound;
    double upperBound;
    int maximumAttempts;
    int lastAttemptCount;
    long totalAttemptCount;

    /// <summary>
    /// Creates a bounded truncation wrapper over a scalar sampling delegate.
    /// </summary>
    /// <param name="source">The non-null scalar sampler delegate.</param>
    /// <param name="lowerBound">The inclusive lower bound, including negative infinity.</param>
    /// <param name="upperBound">The inclusive upper bound, including positive infinity.</param>
    /// <param name="maximumAttempts">The positive attempt budget for each sample.</param>
    /// <exception cref="RandomArgumentException">The source is null, a bound is NaN, the bounds are reversed, equal bounds are non-finite, or the attempt budget is not positive.</exception>
    public TruncatedSampler(Func<double> source, double lowerBound, double upperBound, int maximumAttempts)
    {
        Guard.NotNull(source, "source");
        if (double.IsNaN(lowerBound))
        {
            throw RandomArgumentException.Create("lowerBound", "The lower bound must not be NaN.");
        }

        if (double.IsNaN(upperBound))
        {
            throw RandomArgumentException.Create("upperBound", "The upper bound must not be NaN.");
        }

        if (lowerBound > upperBound)
        {
            throw RandomArgumentException.Create("upperBound", "The upper bound must not be less than the lower bound.");
        }

        if (lowerBound == upperBound && !double.IsFinite(lowerBound))
        {
            throw RandomArgumentException.Create("upperBound", "Equal truncation bounds must be finite.");
        }

        Guard.PositiveCount(maximumAttempts, "maximumAttempts");
        this.source = source;
        this.lowerBound = lowerBound;
        this.upperBound = upperBound;
        this.maximumAttempts = maximumAttempts;
        lastAttemptCount = 0;
        totalAttemptCount = 0L;
    }

    /// <summary>
    /// Returns the per-sample attempt budget.
    /// </summary>
    /// <returns>The positive maximum attempt count.</returns>
    public int MaximumAttempts()
    {
        return maximumAttempts;
    }

    /// <summary>
    /// Returns the attempts made by the latest sample request.
    /// </summary>
    /// <returns>Zero before the first request, otherwise the attempts through acceptance, exhaustion, or a delegate failure.</returns>
    public int LastAttemptCount()
    {
        return lastAttemptCount;
    }

    /// <summary>
    /// Returns the cumulative attempts made by this sampler.
    /// </summary>
    /// <returns>The cumulative count, saturated at <c>long.MaxValue</c>.</returns>
    public long TotalAttemptCount()
    {
        return totalAttemptCount;
    }

    /// <summary>
    /// Returns the first source value inside the configured bounds.
    /// </summary>
    /// <returns>A source value in the inclusive interval.</returns>
    /// <exception cref="RandomExhaustedException">No source value is inside the interval within <see cref="MaximumAttempts()"/> attempts.</exception>
    public double Sample()
    {
        lastAttemptCount = 0;
        while (lastAttemptCount < maximumAttempts)
        {
            lastAttemptCount++;
            if (totalAttemptCount < long.MaxValue)
            {
                totalAttemptCount++;
            }

            double candidate = source();
            if (!double.IsNaN(candidate) && candidate >= lowerBound && candidate <= upperBound)
            {
                return candidate;
            }
        }

        throw RandomExhaustedException.Create("The truncated sampler exhausted its attempt budget without observing a value inside the bounds.");
    }

    /// <summary>
    /// Fills a destination through repeated bounded truncation sampling.
    /// </summary>
    /// <param name="destination">The destination values.</param>
    /// <remarks>
    /// The result, source calls, and diagnostics match repeated calls to <see cref="Sample()"/>.
    /// If sampling throws, earlier destination entries remain written.
    /// </remarks>
    /// <exception cref="RandomExhaustedException">A sample exhausts its attempt budget.</exception>
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
