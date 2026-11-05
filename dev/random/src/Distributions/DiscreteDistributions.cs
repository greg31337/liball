using System;

namespace random;

/// <summary>
/// Provides one-shot scalar, bulk, and vector sampling for discrete distributions.
/// </summary>
/// <remarks>
/// Each method constructs the corresponding reusable sampler, so validation, math-mode
/// selection, boundary behavior, and generator consumption are identical to that sampler.
/// Use the sampler classes directly when repeatedly sampling one fixed parameter set. Integer-only
/// transforms replay exactly for a named deterministic engine and call sequence. Distributions that
/// invoke mathematical kernels follow the generator's <see cref="RandomMathMode"/>: Portable mode
/// provides cross-platform bit replay, while System mode promises statistical equivalence.
/// </remarks>
internal static class DiscreteDistributions
{
    /// <summary>Returns one Bernoulli outcome.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="probability">The finite probability in <c>[0,1]</c>.</param>
    /// <returns><c>true</c> with the requested probability.</returns>
    public static bool Bernoulli(IRandomSource generator, double probability)
    {
        return new BernoulliSampler(generator, probability).Sample();
    }

    /// <summary>Fills Bernoulli outcomes.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="probability">The finite probability in <c>[0,1]</c>.</param>
    /// <param name="destination">The destination outcomes.</param>
    public static void FillBernoulli(IRandomSource generator, double probability, Span<bool> destination)
    {
        new BernoulliSampler(generator, probability).Fill(destination);
    }

    /// <summary>Returns one Rademacher value.</summary>
    /// <param name="generator">The source generator.</param>
    /// <returns>Either negative one or positive one with equal probability.</returns>
    public static int Rademacher(IRandomSource generator)
    {
        return new RademacherSampler(generator).Sample();
    }

    /// <summary>Fills Rademacher values.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="destination">The destination values.</param>
    public static void FillRademacher(IRandomSource generator, Span<int> destination)
    {
        new RademacherSampler(generator).Fill(destination);
    }

    /// <summary>Returns one inclusive discrete-uniform integer.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The inclusive maximum.</param>
    /// <returns>An integer in the requested inclusive interval.</returns>
    public static int DiscreteUniform(IRandomSource generator, int minimum, int maximum)
    {
        return new DiscreteUniformSampler(generator, minimum, maximum).Sample();
    }

    /// <summary>Fills inclusive discrete-uniform integers.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="minimum">The inclusive minimum.</param>
    /// <param name="maximum">The inclusive maximum.</param>
    /// <param name="destination">The destination integers.</param>
    public static void FillDiscreteUniform(IRandomSource generator, int minimum, int maximum, Span<int> destination)
    {
        new DiscreteUniformSampler(generator, minimum, maximum).Fill(destination);
    }

    /// <summary>Returns one categorical index.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="probabilities">The nonempty finite nonnegative relative probabilities.</param>
    /// <returns>An index into the probability array.</returns>
    public static int Categorical(IRandomSource generator, double[] probabilities)
    {
        return new CategoricalSampler(generator, probabilities).Sample();
    }

    /// <summary>Fills categorical indexes.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="probabilities">The nonempty finite nonnegative relative probabilities.</param>
    /// <param name="destination">The destination indexes.</param>
    public static void FillCategorical(IRandomSource generator, double[] probabilities, Span<int> destination)
    {
        new CategoricalSampler(generator, probabilities).Fill(destination);
    }

    /// <summary>Returns one binomial success count.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="trials">The nonnegative trial count.</param>
    /// <param name="probability">The finite success probability in <c>[0,1]</c>.</param>
    /// <returns>A success count from zero through the trial count.</returns>
    public static int Binomial(IRandomSource generator, int trials, double probability)
    {
        return new BinomialSampler(generator, trials, probability).Sample();
    }

    /// <summary>Fills binomial success counts.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="trials">The nonnegative trial count.</param>
    /// <param name="probability">The finite success probability in <c>[0,1]</c>.</param>
    /// <param name="destination">The destination counts.</param>
    public static void FillBinomial(IRandomSource generator, int trials, double probability, Span<int> destination)
    {
        new BinomialSampler(generator, trials, probability).Fill(destination);
    }

    /// <summary>Returns one beta-binomial success count.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="trials">The nonnegative trial count.</param>
    /// <param name="alpha">The finite positive first beta shape.</param>
    /// <param name="beta">The finite positive second beta shape.</param>
    /// <returns>A success count from zero through the trial count.</returns>
    public static int BetaBinomial(IRandomSource generator, int trials, double alpha, double beta)
    {
        return new BetaBinomialSampler(generator, trials, alpha, beta).Sample();
    }

    /// <summary>Fills beta-binomial success counts.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="trials">The nonnegative trial count.</param>
    /// <param name="alpha">The finite positive first beta shape.</param>
    /// <param name="beta">The finite positive second beta shape.</param>
    /// <param name="destination">The destination counts.</param>
    public static void FillBetaBinomial(IRandomSource generator, int trials, double alpha, double beta, Span<int> destination)
    {
        new BetaBinomialSampler(generator, trials, alpha, beta).Fill(destination);
    }

    /// <summary>Returns one geometric failure count.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="probability">The finite success probability in <c>(0,1]</c>.</param>
    /// <returns>The failures before the first success.</returns>
    public static long GeometricFailures(IRandomSource generator, double probability)
    {
        return new GeometricSampler(generator, probability).SampleFailures();
    }

    /// <summary>Fills geometric failure counts.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="probability">The finite success probability in <c>(0,1]</c>.</param>
    /// <param name="destination">The destination failure counts.</param>
    public static void FillGeometricFailures(IRandomSource generator, double probability, Span<long> destination)
    {
        new GeometricSampler(generator, probability).FillFailures(destination);
    }

    /// <summary>Returns one geometric trial count.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="probability">The finite success probability in <c>(0,1]</c>.</param>
    /// <returns>The trials through and including the first success.</returns>
    public static long GeometricTrials(IRandomSource generator, double probability)
    {
        return new GeometricSampler(generator, probability).SampleTrials();
    }

    /// <summary>Fills geometric trial counts.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="probability">The finite success probability in <c>(0,1]</c>.</param>
    /// <param name="destination">The destination trial counts.</param>
    public static void FillGeometricTrials(IRandomSource generator, double probability, Span<long> destination)
    {
        new GeometricSampler(generator, probability).FillTrials(destination);
    }

    /// <summary>Returns one negative-binomial failure count.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="successes">The finite positive success shape.</param>
    /// <param name="probability">The finite success probability in <c>(0,1]</c>.</param>
    /// <returns>The failures before the configured success shape.</returns>
    public static long NegativeBinomial(IRandomSource generator, double successes, double probability)
    {
        return new NegativeBinomialSampler(generator, successes, probability).Sample();
    }

    /// <summary>Fills negative-binomial failure counts.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="successes">The finite positive success shape.</param>
    /// <param name="probability">The finite success probability in <c>(0,1]</c>.</param>
    /// <param name="destination">The destination failure counts.</param>
    public static void FillNegativeBinomial(IRandomSource generator, double successes, double probability, Span<long> destination)
    {
        new NegativeBinomialSampler(generator, successes, probability).Fill(destination);
    }

    /// <summary>Returns one Poisson count.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="mean">The supported finite nonnegative mean.</param>
    /// <returns>A nonnegative count.</returns>
    public static long Poisson(IRandomSource generator, double mean)
    {
        return new PoissonSampler(generator, mean).Sample();
    }

    /// <summary>Fills Poisson counts.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="mean">The supported finite nonnegative mean.</param>
    /// <param name="destination">The destination counts.</param>
    public static void FillPoisson(IRandomSource generator, double mean, Span<long> destination)
    {
        new PoissonSampler(generator, mean).Fill(destination);
    }

    /// <summary>Returns one hypergeometric success count.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="populationSize">The nonnegative population size.</param>
    /// <param name="successCount">The number of population successes.</param>
    /// <param name="draws">The number drawn without replacement.</param>
    /// <returns>The successes in the draw.</returns>
    public static int Hypergeometric(IRandomSource generator, int populationSize, int successCount, int draws)
    {
        return new HypergeometricSampler(generator, populationSize, successCount, draws).Sample();
    }

    /// <summary>Fills hypergeometric success counts.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="populationSize">The nonnegative population size.</param>
    /// <param name="successCount">The number of population successes.</param>
    /// <param name="draws">The number drawn without replacement.</param>
    /// <param name="destination">The destination counts.</param>
    public static void FillHypergeometric(IRandomSource generator, int populationSize, int successCount, int draws, Span<int> destination)
    {
        new HypergeometricSampler(generator, populationSize, successCount, draws).Fill(destination);
    }

    /// <summary>Returns one logarithmic-series count.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="probability">The finite probability in <c>[0,1)</c>.</param>
    /// <returns>A positive count.</returns>
    public static long LogSeries(IRandomSource generator, double probability)
    {
        return new LogSeriesSampler(generator, probability).Sample();
    }

    /// <summary>Fills logarithmic-series counts.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="probability">The finite probability in <c>[0,1)</c>.</param>
    /// <param name="destination">The destination counts.</param>
    public static void FillLogSeries(IRandomSource generator, double probability, Span<long> destination)
    {
        new LogSeriesSampler(generator, probability).Fill(destination);
    }

    /// <summary>Returns one Zipf rank.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="exponent">The finite exponent greater than one.</param>
    /// <returns>A positive rank.</returns>
    public static long Zipf(IRandomSource generator, double exponent)
    {
        return new ZipfSampler(generator, exponent).Sample();
    }

    /// <summary>Fills Zipf ranks.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="exponent">The finite exponent greater than one.</param>
    /// <param name="destination">The destination ranks.</param>
    public static void FillZipf(IRandomSource generator, double exponent, Span<long> destination)
    {
        new ZipfSampler(generator, exponent).Fill(destination);
    }

    /// <summary>Returns one Conway-Maxwell-Poisson count.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="rate">The finite nonnegative rate.</param>
    /// <param name="dispersion">The finite nonnegative dispersion.</param>
    /// <returns>A nonnegative count.</returns>
    public static long ConwayMaxwellPoisson(IRandomSource generator, double rate, double dispersion)
    {
        return new ConwayMaxwellPoissonSampler(generator, rate, dispersion).Sample();
    }

    /// <summary>Fills Conway-Maxwell-Poisson counts.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="rate">The finite nonnegative rate.</param>
    /// <param name="dispersion">The finite nonnegative dispersion.</param>
    /// <param name="destination">The destination counts.</param>
    public static void FillConwayMaxwellPoisson(IRandomSource generator, double rate, double dispersion, Span<long> destination)
    {
        new ConwayMaxwellPoissonSampler(generator, rate, dispersion).Fill(destination);
    }

    /// <summary>Creates one multivariate-hypergeometric category-count vector.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="categoryCounts">The nonempty nonnegative category population counts.</param>
    /// <param name="draws">The number drawn without replacement.</param>
    /// <returns>A new vector whose entries sum exactly to the draw count.</returns>
    public static int[] MultivariateHypergeometric(IRandomSource generator, int[] categoryCounts, int draws)
    {
        return new MultivariateHypergeometricSampler(generator, categoryCounts, draws).Sample();
    }

    /// <summary>Writes one multivariate-hypergeometric category-count vector.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="categoryCounts">The nonempty nonnegative category population counts.</param>
    /// <param name="draws">The number drawn without replacement.</param>
    /// <param name="destination">The destination whose length equals the category count.</param>
    public static void MultivariateHypergeometric(IRandomSource generator, int[] categoryCounts, int draws, Span<int> destination)
    {
        new MultivariateHypergeometricSampler(generator, categoryCounts, draws).SampleInto(destination);
    }

    /// <summary>Creates one multinomial category-count vector.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="trials">The nonnegative total trial count.</param>
    /// <param name="probabilities">The nonempty finite nonnegative relative probabilities.</param>
    /// <returns>A new vector whose entries sum exactly to the trial count.</returns>
    public static int[] Multinomial(IRandomSource generator, int trials, double[] probabilities)
    {
        return new MultinomialSampler(generator, trials, probabilities).Sample();
    }

    /// <summary>Writes one multinomial category-count vector.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="trials">The nonnegative total trial count.</param>
    /// <param name="probabilities">The nonempty finite nonnegative relative probabilities.</param>
    /// <param name="destination">The destination whose length equals the category count.</param>
    public static void Multinomial(IRandomSource generator, int trials, double[] probabilities, Span<int> destination)
    {
        new MultinomialSampler(generator, trials, probabilities).SampleInto(destination);
    }
}
