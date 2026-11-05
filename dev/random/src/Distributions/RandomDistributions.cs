using System;

namespace random;

/// <summary>
/// Provides one-shot scalar and bulk sampling for continuous distributions.
/// </summary>
/// <remarks>
/// Each method constructs the corresponding reusable sampler, so validation, math-mode
/// selection, boundary behavior, and generator consumption are identical to that sampler.
/// Use the sampler classes directly when repeatedly sampling one fixed parameter set. With a
/// deterministic generator, <see cref="RandomMathMode.Portable"/> provides bit-for-bit distribution
/// replay across supported platforms; <see cref="RandomMathMode.System"/> provides statistical
/// equivalence and does not promise identical last bits or post-call state.
/// </remarks>
internal static class RandomDistributions
{
    /// <summary>Returns one continuous uniform value.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="minimum">The finite inclusive minimum.</param>
    /// <param name="maximum">The finite exclusive maximum.</param>
    /// <returns>A uniform value.</returns>
    public static double Uniform(IRandomSource generator, double minimum, double maximum)
    {
        return new UniformSampler(generator, minimum, maximum).Sample();
    }

    /// <summary>Fills values from a continuous uniform distribution.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="minimum">The finite inclusive minimum.</param>
    /// <param name="maximum">The finite exclusive maximum.</param>
    /// <param name="destination">The destination values.</param>
    public static void FillUniform(IRandomSource generator, double minimum, double maximum, Span<double> destination)
    {
        new UniformSampler(generator, minimum, maximum).Fill(destination);
    }

    /// <summary>Returns one normal value.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="mean">The finite mean.</param>
    /// <param name="standardDeviation">The finite nonnegative standard deviation.</param>
    /// <returns>A normal value.</returns>
    public static double Normal(IRandomSource generator, double mean, double standardDeviation)
    {
        return new NormalSampler(generator, mean, standardDeviation).Sample();
    }

    /// <summary>Fills values from a normal distribution.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="mean">The finite mean.</param>
    /// <param name="standardDeviation">The finite nonnegative standard deviation.</param>
    /// <param name="destination">The destination values.</param>
    public static void FillNormal(IRandomSource generator, double mean, double standardDeviation, Span<double> destination)
    {
        new NormalSampler(generator, mean, standardDeviation).Fill(destination);
    }

    /// <summary>Returns one log-normal value.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="logMean">The finite mean of the underlying normal.</param>
    /// <param name="logStandardDeviation">The finite nonnegative standard deviation of the underlying normal.</param>
    /// <returns>A log-normal value.</returns>
    public static double LogNormal(IRandomSource generator, double logMean, double logStandardDeviation)
    {
        return new LogNormalSampler(generator, logMean, logStandardDeviation).Sample();
    }

    /// <summary>Fills values from a log-normal distribution.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="logMean">The finite mean of the underlying normal.</param>
    /// <param name="logStandardDeviation">The finite nonnegative standard deviation of the underlying normal.</param>
    /// <param name="destination">The destination values.</param>
    public static void FillLogNormal(IRandomSource generator, double logMean, double logStandardDeviation, Span<double> destination)
    {
        new LogNormalSampler(generator, logMean, logStandardDeviation).Fill(destination);
    }

    /// <summary>Returns one exponential value.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="scale">The finite nonnegative scale.</param>
    /// <returns>An exponential value.</returns>
    public static double Exponential(IRandomSource generator, double scale)
    {
        return new ExponentialSampler(generator, scale).Sample();
    }

    /// <summary>Fills values from an exponential distribution.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="scale">The finite nonnegative scale.</param>
    /// <param name="destination">The destination values.</param>
    public static void FillExponential(IRandomSource generator, double scale, Span<double> destination)
    {
        new ExponentialSampler(generator, scale).Fill(destination);
    }

    /// <summary>Returns one gamma value.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="shape">The finite positive shape.</param>
    /// <param name="scale">The finite nonnegative scale.</param>
    /// <returns>A gamma value.</returns>
    public static double Gamma(IRandomSource generator, double shape, double scale)
    {
        return new GammaSampler(generator, shape, scale).Sample();
    }

    /// <summary>Fills values from a gamma distribution.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="shape">The finite positive shape.</param>
    /// <param name="scale">The finite nonnegative scale.</param>
    /// <param name="destination">The destination values.</param>
    public static void FillGamma(IRandomSource generator, double shape, double scale, Span<double> destination)
    {
        new GammaSampler(generator, shape, scale).Fill(destination);
    }

    /// <summary>Returns one Erlang value.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="shape">The positive integer shape.</param>
    /// <param name="scale">The finite nonnegative scale.</param>
    /// <returns>An Erlang value.</returns>
    public static double Erlang(IRandomSource generator, int shape, double scale)
    {
        return new ErlangSampler(generator, shape, scale).Sample();
    }

    /// <summary>Fills values from an Erlang distribution.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="shape">The positive integer shape.</param>
    /// <param name="scale">The finite nonnegative scale.</param>
    /// <param name="destination">The destination values.</param>
    public static void FillErlang(IRandomSource generator, int shape, double scale, Span<double> destination)
    {
        new ErlangSampler(generator, shape, scale).Fill(destination);
    }

    /// <summary>Returns one beta value.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="alpha">The finite positive first shape.</param>
    /// <param name="beta">The finite positive second shape.</param>
    /// <returns>A beta value.</returns>
    public static double Beta(IRandomSource generator, double alpha, double beta)
    {
        return new BetaSampler(generator, alpha, beta).Sample();
    }

    /// <summary>Fills values from a beta distribution.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="alpha">The finite positive first shape.</param>
    /// <param name="beta">The finite positive second shape.</param>
    /// <param name="destination">The destination values.</param>
    public static void FillBeta(IRandomSource generator, double alpha, double beta, Span<double> destination)
    {
        new BetaSampler(generator, alpha, beta).Fill(destination);
    }

    /// <summary>Returns one scaled-beta value.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="alpha">The finite positive first shape.</param>
    /// <param name="beta">The finite positive second shape.</param>
    /// <param name="minimum">The finite lower endpoint.</param>
    /// <param name="maximum">The finite upper endpoint.</param>
    /// <returns>A scaled-beta value.</returns>
    public static double ScaledBeta(IRandomSource generator, double alpha, double beta, double minimum, double maximum)
    {
        return new BetaScaledSampler(generator, alpha, beta, minimum, maximum).Sample();
    }

    /// <summary>Fills values from a scaled-beta distribution.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="alpha">The finite positive first shape.</param>
    /// <param name="beta">The finite positive second shape.</param>
    /// <param name="minimum">The finite lower endpoint.</param>
    /// <param name="maximum">The finite upper endpoint.</param>
    /// <param name="destination">The destination values.</param>
    public static void FillScaledBeta(IRandomSource generator, double alpha, double beta, double minimum, double maximum, Span<double> destination)
    {
        new BetaScaledSampler(generator, alpha, beta, minimum, maximum).Fill(destination);
    }

    /// <summary>Returns one chi value.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="degreesOfFreedom">The finite positive degrees of freedom.</param>
    /// <returns>A chi value.</returns>
    public static double Chi(IRandomSource generator, double degreesOfFreedom)
    {
        return new ChiSampler(generator, degreesOfFreedom).Sample();
    }

    /// <summary>Fills values from a chi distribution.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="degreesOfFreedom">The finite positive degrees of freedom.</param>
    /// <param name="destination">The destination values.</param>
    public static void FillChi(IRandomSource generator, double degreesOfFreedom, Span<double> destination)
    {
        new ChiSampler(generator, degreesOfFreedom).Fill(destination);
    }

    /// <summary>Returns one chi-square value.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="degreesOfFreedom">The finite positive degrees of freedom.</param>
    /// <returns>A chi-square value.</returns>
    public static double ChiSquare(IRandomSource generator, double degreesOfFreedom)
    {
        return new ChiSquareSampler(generator, degreesOfFreedom).Sample();
    }

    /// <summary>Fills values from a chi-square distribution.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="degreesOfFreedom">The finite positive degrees of freedom.</param>
    /// <param name="destination">The destination values.</param>
    public static void FillChiSquare(IRandomSource generator, double degreesOfFreedom, Span<double> destination)
    {
        new ChiSquareSampler(generator, degreesOfFreedom).Fill(destination);
    }

    /// <summary>Returns one Student's t value.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="degreesOfFreedom">The finite positive degrees of freedom.</param>
    /// <returns>A Student's t value.</returns>
    public static double StudentT(IRandomSource generator, double degreesOfFreedom)
    {
        return new StudentTSampler(generator, degreesOfFreedom).Sample();
    }

    /// <summary>Fills values from a Student's t distribution.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="degreesOfFreedom">The finite positive degrees of freedom.</param>
    /// <param name="destination">The destination values.</param>
    public static void FillStudentT(IRandomSource generator, double degreesOfFreedom, Span<double> destination)
    {
        new StudentTSampler(generator, degreesOfFreedom).Fill(destination);
    }

    /// <summary>Returns one Fisher-Snedecor F value.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="numeratorDegreesOfFreedom">The finite positive numerator degrees.</param>
    /// <param name="denominatorDegreesOfFreedom">The finite positive denominator degrees.</param>
    /// <returns>An F value.</returns>
    public static double FisherSnedecor(IRandomSource generator, double numeratorDegreesOfFreedom, double denominatorDegreesOfFreedom)
    {
        return new FisherSnedecorSampler(generator, numeratorDegreesOfFreedom, denominatorDegreesOfFreedom).Sample();
    }

    /// <summary>Fills values from a Fisher-Snedecor F distribution.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="numeratorDegreesOfFreedom">The finite positive numerator degrees.</param>
    /// <param name="denominatorDegreesOfFreedom">The finite positive denominator degrees.</param>
    /// <param name="destination">The destination values.</param>
    public static void FillFisherSnedecor(IRandomSource generator, double numeratorDegreesOfFreedom, double denominatorDegreesOfFreedom, Span<double> destination)
    {
        new FisherSnedecorSampler(generator, numeratorDegreesOfFreedom, denominatorDegreesOfFreedom).Fill(destination);
    }

    /// <summary>Returns one Cauchy value.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="location">The finite location.</param>
    /// <param name="scale">The finite nonnegative scale.</param>
    /// <returns>A Cauchy value.</returns>
    public static double Cauchy(IRandomSource generator, double location, double scale)
    {
        return new CauchySampler(generator, location, scale).Sample();
    }

    /// <summary>Fills values from a Cauchy distribution.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="location">The finite location.</param>
    /// <param name="scale">The finite nonnegative scale.</param>
    /// <param name="destination">The destination values.</param>
    public static void FillCauchy(IRandomSource generator, double location, double scale, Span<double> destination)
    {
        new CauchySampler(generator, location, scale).Fill(destination);
    }

    /// <summary>Returns one Laplace value.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="location">The finite location.</param>
    /// <param name="scale">The finite nonnegative scale.</param>
    /// <returns>A Laplace value.</returns>
    public static double Laplace(IRandomSource generator, double location, double scale)
    {
        return new LaplaceSampler(generator, location, scale).Sample();
    }

    /// <summary>Fills values from a Laplace distribution.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="location">The finite location.</param>
    /// <param name="scale">The finite nonnegative scale.</param>
    /// <param name="destination">The destination values.</param>
    public static void FillLaplace(IRandomSource generator, double location, double scale, Span<double> destination)
    {
        new LaplaceSampler(generator, location, scale).Fill(destination);
    }

    /// <summary>Returns one logistic value.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="location">The finite location.</param>
    /// <param name="scale">The finite nonnegative scale.</param>
    /// <returns>A logistic value.</returns>
    public static double Logistic(IRandomSource generator, double location, double scale)
    {
        return new LogisticSampler(generator, location, scale).Sample();
    }

    /// <summary>Fills values from a logistic distribution.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="location">The finite location.</param>
    /// <param name="scale">The finite nonnegative scale.</param>
    /// <param name="destination">The destination values.</param>
    public static void FillLogistic(IRandomSource generator, double location, double scale, Span<double> destination)
    {
        new LogisticSampler(generator, location, scale).Fill(destination);
    }

    /// <summary>Returns one Gumbel value.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="location">The finite location.</param>
    /// <param name="scale">The finite nonnegative scale.</param>
    /// <returns>A Gumbel value.</returns>
    public static double Gumbel(IRandomSource generator, double location, double scale)
    {
        return new GumbelSampler(generator, location, scale).Sample();
    }

    /// <summary>Fills values from a Gumbel distribution.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="location">The finite location.</param>
    /// <param name="scale">The finite nonnegative scale.</param>
    /// <param name="destination">The destination values.</param>
    public static void FillGumbel(IRandomSource generator, double location, double scale, Span<double> destination)
    {
        new GumbelSampler(generator, location, scale).Fill(destination);
    }

    /// <summary>Returns one generalized-normal value.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="location">The finite location.</param>
    /// <param name="scale">The finite nonnegative scale.</param>
    /// <param name="shape">The finite positive shape.</param>
    /// <returns>A generalized-normal value.</returns>
    public static double GeneralizedNormal(IRandomSource generator, double location, double scale, double shape)
    {
        return new GeneralizedNormalSampler(generator, location, scale, shape).Sample();
    }

    /// <summary>Fills values from a generalized-normal distribution.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="location">The finite location.</param>
    /// <param name="scale">The finite nonnegative scale.</param>
    /// <param name="shape">The finite positive shape.</param>
    /// <param name="destination">The destination values.</param>
    public static void FillGeneralizedNormal(IRandomSource generator, double location, double scale, double shape, Span<double> destination)
    {
        new GeneralizedNormalSampler(generator, location, scale, shape).Fill(destination);
    }

    /// <summary>Returns one skewed generalized-error value.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="location">The finite location.</param>
    /// <param name="scale">The finite nonnegative scale.</param>
    /// <param name="shape">The finite positive body shape.</param>
    /// <param name="skewness">The finite skewness in the sampler's supported interval.</param>
    /// <returns>A skewed generalized-error value.</returns>
    public static double SkewedGeneralizedError(IRandomSource generator, double location, double scale, double shape, double skewness)
    {
        return new SkewedGeneralizedErrorSampler(generator, location, scale, shape, skewness).Sample();
    }

    /// <summary>Fills values from a skewed generalized-error distribution.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="location">The finite location.</param>
    /// <param name="scale">The finite nonnegative scale.</param>
    /// <param name="shape">The finite positive body shape.</param>
    /// <param name="skewness">The finite skewness in the sampler's supported interval.</param>
    /// <param name="destination">The destination values.</param>
    public static void FillSkewedGeneralizedError(IRandomSource generator, double location, double scale, double shape, double skewness, Span<double> destination)
    {
        new SkewedGeneralizedErrorSampler(generator, location, scale, shape, skewness).Fill(destination);
    }

    /// <summary>Returns one skewed generalized-t value.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="location">The finite location.</param>
    /// <param name="scale">The finite nonnegative scale.</param>
    /// <param name="bodyShape">The finite positive body shape.</param>
    /// <param name="tailShape">The finite positive tail shape.</param>
    /// <param name="skewness">The finite skewness in the sampler's supported interval.</param>
    /// <returns>A skewed generalized-t value.</returns>
    public static double SkewedGeneralizedT(IRandomSource generator, double location, double scale, double bodyShape, double tailShape, double skewness)
    {
        return new SkewedGeneralizedTSampler(generator, location, scale, bodyShape, tailShape, skewness).Sample();
    }

    /// <summary>Fills values from a skewed generalized-t distribution.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="location">The finite location.</param>
    /// <param name="scale">The finite nonnegative scale.</param>
    /// <param name="bodyShape">The finite positive body shape.</param>
    /// <param name="tailShape">The finite positive tail shape.</param>
    /// <param name="skewness">The finite skewness in the sampler's supported interval.</param>
    /// <param name="destination">The destination values.</param>
    public static void FillSkewedGeneralizedT(IRandomSource generator, double location, double scale, double bodyShape, double tailShape, double skewness, Span<double> destination)
    {
        new SkewedGeneralizedTSampler(generator, location, scale, bodyShape, tailShape, skewness).Fill(destination);
    }

    /// <summary>Returns one Weibull value.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="location">The finite location.</param>
    /// <param name="scale">The finite nonnegative scale.</param>
    /// <param name="shape">The finite positive shape.</param>
    /// <returns>A Weibull value.</returns>
    public static double Weibull(IRandomSource generator, double location, double scale, double shape)
    {
        return new WeibullSampler(generator, location, scale, shape).Sample();
    }

    /// <summary>Fills values from a Weibull distribution.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="location">The finite location.</param>
    /// <param name="scale">The finite nonnegative scale.</param>
    /// <param name="shape">The finite positive shape.</param>
    /// <param name="destination">The destination values.</param>
    public static void FillWeibull(IRandomSource generator, double location, double scale, double shape, Span<double> destination)
    {
        new WeibullSampler(generator, location, scale, shape).Fill(destination);
    }

    /// <summary>Returns one Rayleigh value.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="location">The finite location.</param>
    /// <param name="scale">The finite nonnegative scale.</param>
    /// <returns>A Rayleigh value.</returns>
    public static double Rayleigh(IRandomSource generator, double location, double scale)
    {
        return new RayleighSampler(generator, location, scale).Sample();
    }

    /// <summary>Fills values from a Rayleigh distribution.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="location">The finite location.</param>
    /// <param name="scale">The finite nonnegative scale.</param>
    /// <param name="destination">The destination values.</param>
    public static void FillRayleigh(IRandomSource generator, double location, double scale, Span<double> destination)
    {
        new RayleighSampler(generator, location, scale).Fill(destination);
    }

    /// <summary>Returns one Pareto type I value.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="minimum">The finite positive minimum.</param>
    /// <param name="shape">The finite positive shape.</param>
    /// <returns>A Pareto type I value.</returns>
    public static double Pareto(IRandomSource generator, double minimum, double shape)
    {
        return new ParetoSampler(generator, minimum, shape).Sample();
    }

    /// <summary>Fills values from a Pareto type I distribution.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="minimum">The finite positive minimum.</param>
    /// <param name="shape">The finite positive shape.</param>
    /// <param name="destination">The destination values.</param>
    public static void FillPareto(IRandomSource generator, double minimum, double shape, Span<double> destination)
    {
        new ParetoSampler(generator, minimum, shape).Fill(destination);
    }

    /// <summary>Returns one Lomax value.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="location">The finite location.</param>
    /// <param name="scale">The finite nonnegative scale.</param>
    /// <param name="shape">The finite positive shape.</param>
    /// <returns>A Lomax value.</returns>
    public static double Lomax(IRandomSource generator, double location, double scale, double shape)
    {
        return new LomaxSampler(generator, location, scale, shape).Sample();
    }

    /// <summary>Fills values from a Lomax distribution.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="location">The finite location.</param>
    /// <param name="scale">The finite nonnegative scale.</param>
    /// <param name="shape">The finite positive shape.</param>
    /// <param name="destination">The destination values.</param>
    public static void FillLomax(IRandomSource generator, double location, double scale, double shape, Span<double> destination)
    {
        new LomaxSampler(generator, location, scale, shape).Fill(destination);
    }

    /// <summary>Returns one power-distribution value.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="location">The finite location.</param>
    /// <param name="scale">The finite nonnegative scale.</param>
    /// <param name="shape">The finite positive shape.</param>
    /// <returns>A power-distribution value.</returns>
    public static double Power(IRandomSource generator, double location, double scale, double shape)
    {
        return new PowerSampler(generator, location, scale, shape).Sample();
    }

    /// <summary>Fills values from a power distribution.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="location">The finite location.</param>
    /// <param name="scale">The finite nonnegative scale.</param>
    /// <param name="shape">The finite positive shape.</param>
    /// <param name="destination">The destination values.</param>
    public static void FillPower(IRandomSource generator, double location, double scale, double shape, Span<double> destination)
    {
        new PowerSampler(generator, location, scale, shape).Fill(destination);
    }

    /// <summary>Returns one Burr XII value.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="location">The finite location.</param>
    /// <param name="scale">The finite nonnegative scale.</param>
    /// <param name="firstShape">The finite positive first shape.</param>
    /// <param name="secondShape">The finite positive second shape.</param>
    /// <returns>A Burr XII value.</returns>
    public static double Burr(IRandomSource generator, double location, double scale, double firstShape, double secondShape)
    {
        return new BurrSampler(generator, location, scale, firstShape, secondShape).Sample();
    }

    /// <summary>Fills values from a Burr XII distribution.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="location">The finite location.</param>
    /// <param name="scale">The finite nonnegative scale.</param>
    /// <param name="firstShape">The finite positive first shape.</param>
    /// <param name="secondShape">The finite positive second shape.</param>
    /// <param name="destination">The destination values.</param>
    public static void FillBurr(IRandomSource generator, double location, double scale, double firstShape, double secondShape, Span<double> destination)
    {
        new BurrSampler(generator, location, scale, firstShape, secondShape).Fill(destination);
    }

    /// <summary>Returns one Levy value.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="location">The finite lower endpoint.</param>
    /// <param name="scale">The finite nonnegative scale.</param>
    /// <returns>A Levy value.</returns>
    public static double Levy(IRandomSource generator, double location, double scale)
    {
        return new LevySampler(generator, location, scale).Sample();
    }

    /// <summary>Fills values from a Levy distribution.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="location">The finite lower endpoint.</param>
    /// <param name="scale">The finite nonnegative scale.</param>
    /// <param name="destination">The destination values.</param>
    public static void FillLevy(IRandomSource generator, double location, double scale, Span<double> destination)
    {
        new LevySampler(generator, location, scale).Fill(destination);
    }

    /// <summary>Returns one inverse-gamma value.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="shape">The finite positive shape.</param>
    /// <param name="scale">The finite nonnegative scale.</param>
    /// <returns>An inverse-gamma value.</returns>
    public static double InverseGamma(IRandomSource generator, double shape, double scale)
    {
        return new InverseGammaSampler(generator, shape, scale).Sample();
    }

    /// <summary>Fills values from an inverse-gamma distribution.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="shape">The finite positive shape.</param>
    /// <param name="scale">The finite nonnegative scale.</param>
    /// <param name="destination">The destination values.</param>
    public static void FillInverseGamma(IRandomSource generator, double shape, double scale, Span<double> destination)
    {
        new InverseGammaSampler(generator, shape, scale).Fill(destination);
    }

    /// <summary>Returns one inverse-Gaussian value.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="mean">The finite positive mean.</param>
    /// <param name="shape">The finite positive shape.</param>
    /// <returns>An inverse-Gaussian value.</returns>
    public static double InverseGaussian(IRandomSource generator, double mean, double shape)
    {
        return new InverseGaussianSampler(generator, mean, shape).Sample();
    }

    /// <summary>Fills values from an inverse-Gaussian distribution.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="mean">The finite positive mean.</param>
    /// <param name="shape">The finite positive shape.</param>
    /// <param name="destination">The destination values.</param>
    public static void FillInverseGaussian(IRandomSource generator, double mean, double shape, Span<double> destination)
    {
        new InverseGaussianSampler(generator, mean, shape).Fill(destination);
    }

    /// <summary>Returns one triangular value.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="minimum">The finite minimum.</param>
    /// <param name="maximum">The finite maximum.</param>
    /// <param name="mode">The finite mode within the bounds.</param>
    /// <returns>A triangular value.</returns>
    public static double Triangular(IRandomSource generator, double minimum, double maximum, double mode)
    {
        return new TriangularSampler(generator, minimum, maximum, mode).Sample();
    }

    /// <summary>Fills values from a triangular distribution.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="minimum">The finite minimum.</param>
    /// <param name="maximum">The finite maximum.</param>
    /// <param name="mode">The finite mode within the bounds.</param>
    /// <param name="destination">The destination values.</param>
    public static void FillTriangular(IRandomSource generator, double minimum, double maximum, double mode, Span<double> destination)
    {
        new TriangularSampler(generator, minimum, maximum, mode).Fill(destination);
    }

    /// <summary>Returns one von Mises angle.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="meanDirection">The finite mean direction in radians.</param>
    /// <param name="concentration">The nonnegative concentration, including positive infinity.</param>
    /// <returns>A normalized von Mises angle.</returns>
    public static double VonMises(IRandomSource generator, double meanDirection, double concentration)
    {
        return new VonMisesSampler(generator, meanDirection, concentration).Sample();
    }

    /// <summary>Fills angles from a von Mises distribution.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="meanDirection">The finite mean direction in radians.</param>
    /// <param name="concentration">The nonnegative concentration, including positive infinity.</param>
    /// <param name="destination">The destination angles.</param>
    public static void FillVonMises(IRandomSource generator, double meanDirection, double concentration, Span<double> destination)
    {
        new VonMisesSampler(generator, meanDirection, concentration).Fill(destination);
    }

    /// <summary>Returns one stable-law value in the S0 parameterization.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="location">The finite S0 location.</param>
    /// <param name="scale">The finite nonnegative S0 scale.</param>
    /// <param name="stability">The stability in the sampler's supported interval.</param>
    /// <param name="skewness">The skewness in <c>[-1,1]</c>.</param>
    /// <returns>A stable-law value.</returns>
    public static double Stable(IRandomSource generator, double location, double scale, double stability, double skewness)
    {
        return new StableSampler(generator, location, scale, stability, skewness).Sample();
    }

    /// <summary>Fills values from a stable distribution in the S0 parameterization.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="location">The finite S0 location.</param>
    /// <param name="scale">The finite nonnegative S0 scale.</param>
    /// <param name="stability">The stability in the sampler's supported interval.</param>
    /// <param name="skewness">The skewness in <c>[-1,1]</c>.</param>
    /// <param name="destination">The destination values.</param>
    public static void FillStable(IRandomSource generator, double location, double scale, double stability, double skewness, Span<double> destination)
    {
        new StableSampler(generator, location, scale, stability, skewness).Fill(destination);
    }

    /// <summary>Returns one truncated-normal value.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="mean">The finite untruncated mean.</param>
    /// <param name="standardDeviation">The finite nonnegative untruncated standard deviation.</param>
    /// <param name="lowerBound">The lower bound, including negative infinity.</param>
    /// <param name="upperBound">The upper bound, including positive infinity.</param>
    /// <returns>A truncated-normal value.</returns>
    public static double TruncatedNormal(IRandomSource generator, double mean, double standardDeviation, double lowerBound, double upperBound)
    {
        return new TruncatedNormalSampler(generator, mean, standardDeviation, lowerBound, upperBound).Sample();
    }

    /// <summary>Fills values from a truncated normal distribution.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="mean">The finite untruncated mean.</param>
    /// <param name="standardDeviation">The finite nonnegative untruncated standard deviation.</param>
    /// <param name="lowerBound">The lower bound, including negative infinity.</param>
    /// <param name="upperBound">The upper bound, including positive infinity.</param>
    /// <param name="destination">The destination values.</param>
    public static void FillTruncatedNormal(IRandomSource generator, double mean, double standardDeviation, double lowerBound, double upperBound, Span<double> destination)
    {
        new TruncatedNormalSampler(generator, mean, standardDeviation, lowerBound, upperBound).Fill(destination);
    }

    /// <summary>Returns one noncentral chi-square value.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="degreesOfFreedom">The finite positive degrees of freedom.</param>
    /// <param name="noncentrality">The finite nonnegative noncentrality.</param>
    /// <returns>A noncentral chi-square value.</returns>
    public static double NoncentralChiSquare(IRandomSource generator, double degreesOfFreedom, double noncentrality)
    {
        return new NoncentralChiSquareSampler(generator, degreesOfFreedom, noncentrality).Sample();
    }

    /// <summary>Fills values from a noncentral chi-square distribution.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="degreesOfFreedom">The finite positive degrees of freedom.</param>
    /// <param name="noncentrality">The finite nonnegative noncentrality.</param>
    /// <param name="destination">The destination values.</param>
    public static void FillNoncentralChiSquare(IRandomSource generator, double degreesOfFreedom, double noncentrality, Span<double> destination)
    {
        new NoncentralChiSquareSampler(generator, degreesOfFreedom, noncentrality).Fill(destination);
    }

    /// <summary>Returns one noncentral Fisher-Snedecor F value.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="numeratorDegreesOfFreedom">The finite positive numerator degrees.</param>
    /// <param name="denominatorDegreesOfFreedom">The finite positive denominator degrees.</param>
    /// <param name="noncentrality">The finite nonnegative numerator noncentrality.</param>
    /// <returns>A noncentral F value.</returns>
    public static double NoncentralF(IRandomSource generator, double numeratorDegreesOfFreedom, double denominatorDegreesOfFreedom, double noncentrality)
    {
        return new NoncentralFSampler(generator, numeratorDegreesOfFreedom, denominatorDegreesOfFreedom, noncentrality).Sample();
    }

    /// <summary>Fills values from a noncentral Fisher-Snedecor F distribution.</summary>
    /// <param name="generator">The source generator.</param>
    /// <param name="numeratorDegreesOfFreedom">The finite positive numerator degrees.</param>
    /// <param name="denominatorDegreesOfFreedom">The finite positive denominator degrees.</param>
    /// <param name="noncentrality">The finite nonnegative numerator noncentrality.</param>
    /// <param name="destination">The destination values.</param>
    public static void FillNoncentralF(IRandomSource generator, double numeratorDegreesOfFreedom, double denominatorDegreesOfFreedom, double noncentrality, Span<double> destination)
    {
        new NoncentralFSampler(generator, numeratorDegreesOfFreedom, denominatorDegreesOfFreedom, noncentrality).Fill(destination);
    }
}
