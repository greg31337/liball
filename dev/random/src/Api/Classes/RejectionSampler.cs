using System;

namespace random;

/// <summary>
/// Samples through a caller-supplied bounded rejection algorithm.
/// </summary>
/// <remarks>
/// On each attempt, <c>proposal()</c> creates a candidate and
/// <c>acceptance(candidate)</c> returns its acceptance probability in <c>[0, 1]</c>.
/// Only an accepted candidate is passed to <c>value(candidate)</c>. Probabilities zero
/// and one reject or accept without an extra uniform draw. No rejected candidate is
/// ever returned as a fallback. Delegate state remains caller-owned, and delegate
/// exceptions are propagated unchanged. The sampler retains the supplied mutable
/// generator and is not safe for concurrent use without external synchronization.
/// </remarks>
public class RejectionSampler : IRejectionSampler
{
    IRandomSource generator;
    Func<double> proposal;
    Func<double, double> value;
    Func<double, double> acceptance;
    int maximumAttempts;
    int lastAttemptCount;
    long totalAttemptCount;

    /// <summary>
    /// Creates a rejection sampler with a separate accepted-value transform.
    /// </summary>
    /// <param name="generator">The generator used for probabilistic acceptance decisions.</param>
    /// <param name="proposal">The non-null candidate proposal delegate.</param>
    /// <param name="value">The non-null transform invoked only for an accepted candidate.</param>
    /// <param name="acceptance">The non-null delegate returning each candidate's acceptance probability.</param>
    /// <param name="maximumAttempts">The positive attempt budget for each sample.</param>
    /// <exception cref="RandomArgumentException">The generator or a delegate is null, or the attempt budget is not positive.</exception>
    public RejectionSampler(IRandomSource generator, Func<double> proposal, Func<double, double> value, Func<double, double> acceptance, int maximumAttempts)
    {
        Guard.NotNull(generator, "generator");
        Guard.NotNull(proposal, "proposal");
        Guard.NotNull(value, "value");
        Guard.NotNull(acceptance, "acceptance");
        Guard.PositiveCount(maximumAttempts, "maximumAttempts");
        this.generator = generator;
        this.proposal = proposal;
        this.value = value;
        this.acceptance = acceptance;
        this.maximumAttempts = maximumAttempts;
        lastAttemptCount = 0;
        totalAttemptCount = 0L;
    }

    /// <summary>
    /// Creates a rejection sampler that returns each accepted proposal unchanged.
    /// </summary>
    /// <param name="generator">The generator used for probabilistic acceptance decisions.</param>
    /// <param name="proposal">The non-null candidate proposal delegate.</param>
    /// <param name="acceptance">The non-null delegate returning each candidate's acceptance probability.</param>
    /// <param name="maximumAttempts">The positive attempt budget for each sample.</param>
    /// <returns>A rejection sampler with an identity accepted-value transform.</returns>
    /// <exception cref="RandomArgumentException">The generator or a delegate is null, or the attempt budget is not positive.</exception>
    internal static RejectionSampler CreateIdentity(IRandomSource generator, Func<double> proposal, Func<double, double> acceptance, int maximumAttempts)
    {
        return new RejectionSampler(generator, proposal, Identity, acceptance, maximumAttempts);
    }

    static double Identity(double value)
    {
        return value;
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
    /// Runs bounded rejection sampling until one candidate is accepted.
    /// </summary>
    /// <returns>The value transform of the accepted candidate.</returns>
    /// <exception cref="RandomStateException">The acceptance delegate returns a non-finite value or a value outside <c>[0, 1]</c>.</exception>
    /// <exception cref="RandomExhaustedException">No candidate is accepted within <see cref="MaximumAttempts()"/> attempts.</exception>
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

            double candidate = proposal();
            double probability = acceptance(candidate);
            if (!double.IsFinite(probability) || probability < 0.0 || probability > 1.0)
            {
                throw RandomStateException.Create("The rejection acceptance delegate returned an invalid probability.");
            }

            bool accepted = probability >= 1.0;
            if (probability > 0.0 && probability < 1.0)
            {
                accepted = FloatingUniform.NextDoubleClosedOpen(generator) < probability;
            }

            if (accepted)
            {
                return value(candidate);
            }
        }

        throw RandomExhaustedException.Create("The rejection sampler exhausted its attempt budget without accepting a candidate.");
    }

    /// <summary>
    /// Fills a destination through repeated bounded rejection sampling.
    /// </summary>
    /// <param name="destination">The destination values.</param>
    /// <remarks>
    /// The result, delegate calls, diagnostics, and generator state match repeated calls
    /// to <see cref="Sample()"/>. If sampling throws, earlier destination entries remain written.
    /// </remarks>
    /// <exception cref="RandomStateException">An acceptance delegate result is invalid.</exception>
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

    static void ValidateParameters(IRandomSource generator, Func<double> proposal, Func<double, double> acceptance, int maximumAttempts)
    {
        Guard.NotNull(generator, "generator");
        Guard.NotNull(proposal, "proposal");
        Guard.NotNull(acceptance, "acceptance");
        Guard.PositiveCount(maximumAttempts, "maximumAttempts");
    }
}
