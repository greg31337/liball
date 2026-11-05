using System;

namespace random;

/// <summary>
/// Creates generators, seeds, state objects, and other API implementations that use named creation paths.
/// </summary>
public class RandomFactory : IRandomFactory
{
    /// <summary>Creates a reusable API factory.</summary>
    public RandomFactory()
    {
    }

    /// <summary>Creates the default deterministic generator.</summary>
    public RandomGenerator CreateDefaultGenerator(ulong seed, RandomMathMode mathMode)
    {
        return RandomGenerator.CreateDefault(seed, mathMode);
    }

    /// <summary>Creates a named deterministic generator from a seed sequence.</summary>
    public RandomGenerator CreateGeneratorFromSeedSequence(RandomAlgorithm algorithm, SeedSequence seedSequence, RandomMathMode mathMode)
    {
        return RandomGenerator.CreateFromSeedSequence(algorithm, seedSequence, mathMode);
    }

    /// <summary>Creates a named deterministic generator from a canonical byte seed.</summary>
    public RandomGenerator CreateGeneratorFromBytes(RandomAlgorithm algorithm, byte[] seed, RandomMathMode mathMode)
    {
        RandomGenerator.ValidateSeedAlgorithm(algorithm);
        RandomGenerator.ValidateMathMode(mathMode);
        return RandomGenerator.CreateFromSeedSequence(algorithm, SeedSequence.FromBytes(seed), mathMode);
    }

    /// <summary>Creates a named deterministic generator from a canonical UTF-8 text seed.</summary>
    public RandomGenerator CreateGeneratorFromText(RandomAlgorithm algorithm, string seed, RandomMathMode mathMode)
    {
        RandomGenerator.ValidateSeedAlgorithm(algorithm);
        RandomGenerator.ValidateMathMode(mathMode);
        return RandomGenerator.CreateFromSeedSequence(algorithm, SeedSequence.FromText(seed), mathMode);
    }

    /// <summary>Creates a named deterministic generator from canonical unsigned seed words.</summary>
    public RandomGenerator CreateGeneratorFromUInt32Array(RandomAlgorithm algorithm, uint[] seed, RandomMathMode mathMode)
    {
        RandomGenerator.ValidateSeedAlgorithm(algorithm);
        RandomGenerator.ValidateMathMode(mathMode);
        return RandomGenerator.CreateFromSeedSequence(algorithm, SeedSequence.FromUInt32Array(seed), mathMode);
    }

    /// <summary>Creates a named deterministic generator from 256 bits supplied directly by the operating-system CSPRNG.</summary>
    /// <remarks>Operating-system failures propagate and no fallback entropy source is used.</remarks>
    public RandomGenerator CreateGeneratorFromSystemEntropy(RandomAlgorithm algorithm, RandomMathMode mathMode)
    {
        RandomGenerator.ValidateSeedAlgorithm(algorithm);
        RandomGenerator.ValidateMathMode(mathMode);
        return RandomGenerator.CreateFromSeedSequence(algorithm, SeedSequence.FromSystemEntropy(), mathMode);
    }

    /// <summary>Creates a PCG64 DXSM generator.</summary>
    public RandomGenerator CreatePcg64Dxsm(UInt128 initialState, UInt128 streamSelector, RandomMathMode mathMode)
    {
        return RandomGenerator.CreatePcg64Dxsm(initialState, streamSelector, mathMode);
    }

    /// <summary>Creates a xoshiro256** generator.</summary>
    public RandomGenerator CreateXoshiro256StarStar(ulong seed, RandomMathMode mathMode)
    {
        return RandomGenerator.CreateXoshiro256StarStar(seed, mathMode);
    }

    /// <summary>Creates a Philox4x64-10 generator from a key.</summary>
    public RandomGenerator CreatePhilox4x64Ten(UInt128 key, RandomMathMode mathMode)
    {
        return RandomGenerator.CreatePhilox4x64Ten(key, mathMode);
    }

    /// <summary>Creates a Philox4x64-10 generator at an explicit counter.</summary>
    public RandomGenerator CreatePhilox4x64TenAtCounter(ulong[] key, ulong[] counter, RandomMathMode mathMode)
    {
        return RandomGenerator.CreatePhilox4x64Ten(key, counter, mathMode);
    }

    /// <summary>Creates a reference-seeded MT19937 generator.</summary>
    public RandomGenerator CreateMt19937(uint seed, RandomMathMode mathMode)
    {
        return RandomGenerator.CreateMt19937(seed, mathMode);
    }

    /// <summary>Creates a reference-array-seeded MT19937 generator.</summary>
    public RandomGenerator CreateMt19937FromArray(uint[] seed, RandomMathMode mathMode)
    {
        return RandomGenerator.CreateMt19937(seed, mathMode);
    }

    /// <summary>Creates a NumPy-compatible MT19937 generator.</summary>
    public RandomGenerator CreateMt19937Numpy(uint seed, RandomMathMode mathMode)
    {
        return RandomGenerator.CreateMt19937Numpy(seed, mathMode);
    }

    /// <summary>Creates a NumPy-compatible array-seeded MT19937 generator.</summary>
    public RandomGenerator CreateMt19937NumpyFromArray(uint[] seed, RandomMathMode mathMode)
    {
        return RandomGenerator.CreateMt19937Numpy(seed, mathMode);
    }

    /// <summary>Creates a SplitMix64 generator.</summary>
    public RandomGenerator CreateSplitMix64(ulong seed, RandomMathMode mathMode)
    {
        return RandomGenerator.CreateSplitMix64(seed, mathMode);
    }

    /// <summary>Restores a deterministic generator from exported state.</summary>
    public RandomGenerator RestoreGenerator(RandomState randomState, RandomMathMode mathMode)
    {
        return RandomGenerator.FromState(randomState, mathMode);
    }

    /// <summary>Creates a generator backed by system cryptography.</summary>
    public SecureRandomGenerator CreateSystemSecureGenerator()
    {
        return SecureRandomGenerator.CreateSystem();
    }

    /// <summary>Creates an HMAC-DRBG generator from system entropy.</summary>
    public SecureRandomGenerator CreateHmacDrbg()
    {
        return SecureRandomGenerator.CreateHmacDrbg();
    }

    /// <summary>Creates an HMAC-DRBG generator from explicit seed material.</summary>
    public SecureRandomGenerator CreateHmacDrbgFromSeedMaterial(byte[] entropy, byte[] nonce, byte[] personalization)
    {
        return SecureRandomGenerator.CreateHmacDrbg(entropy, nonce, personalization);
    }

    /// <summary>Creates a seed sequence from an unsigned integer.</summary>
    public SeedSequence CreateSeedFromUInt64(ulong seed)
    {
        return SeedSequence.FromUInt64(seed);
    }

    /// <summary>Creates a seed sequence from a signed integer.</summary>
    public SeedSequence CreateSeedFromInt64(long seed)
    {
        return SeedSequence.FromInt64(seed);
    }

    /// <summary>Creates a seed sequence from bytes.</summary>
    public SeedSequence CreateSeedFromBytes(byte[] seed)
    {
        return SeedSequence.FromBytes(seed);
    }

    /// <summary>Creates a seed sequence from text.</summary>
    public SeedSequence CreateSeedFromText(string seed)
    {
        return SeedSequence.FromText(seed);
    }

    /// <summary>Creates a seed sequence from unsigned words.</summary>
    public SeedSequence CreateSeedFromUInt32Array(uint[] seed)
    {
        return SeedSequence.FromUInt32Array(seed);
    }

    /// <summary>Creates a seed sequence from system entropy.</summary>
    public SeedSequence CreateSeedFromSystemEntropy()
    {
        return SeedSequence.FromSystemEntropy();
    }

    /// <summary>Decodes a random state from bytes.</summary>
    public RandomState CreateStateFromBytes(byte[] encoded)
    {
        return RandomState.FromBytes(encoded);
    }

    /// <summary>Decodes a random state from Base64 text.</summary>
    public RandomState CreateStateFromBase64(string encoded)
    {
        return RandomState.FromBase64(encoded);
    }

    /// <summary>Creates a standard Burr XII sampler with location zero and scale one.</summary>
    /// <param name="generator">The random source.</param>
    /// <param name="firstShape">The finite positive first shape.</param>
    /// <param name="secondShape">The finite positive second shape.</param>
    /// <returns>A standard Burr XII sampler.</returns>
    public BurrSampler CreateStandardBurrSampler(IRandomSource generator, double firstShape, double secondShape)
    {
        return new BurrSampler(generator, 0.0, 1.0, firstShape, secondShape);
    }

    /// <summary>Creates a standard Cauchy sampler with location zero and scale one.</summary>
    /// <param name="generator">The random source.</param>
    /// <returns>A standard Cauchy sampler.</returns>
    public CauchySampler CreateStandardCauchySampler(IRandomSource generator)
    {
        return new CauchySampler(generator, 0.0, 1.0);
    }

    /// <summary>Creates a unit-scale Erlang sampler.</summary>
    /// <param name="generator">The random source.</param>
    /// <param name="shape">The positive integer shape.</param>
    /// <returns>An Erlang sampler with scale one.</returns>
    public ErlangSampler CreateUnitScaleErlangSampler(IRandomSource generator, int shape)
    {
        return new ErlangSampler(generator, shape, 1.0);
    }

    /// <summary>Creates a standard exponential sampler with scale one.</summary>
    /// <param name="generator">The random source.</param>
    /// <returns>A standard exponential sampler.</returns>
    public ExponentialSampler CreateStandardExponentialSampler(IRandomSource generator)
    {
        return new ExponentialSampler(generator, 1.0);
    }

    /// <summary>Creates a unit-scale gamma sampler.</summary>
    /// <param name="generator">The random source.</param>
    /// <param name="shape">The finite positive shape.</param>
    /// <returns>A gamma sampler with scale one.</returns>
    public GammaSampler CreateUnitScaleGammaSampler(IRandomSource generator, double shape)
    {
        return new GammaSampler(generator, shape, 1.0);
    }

    /// <summary>Creates a standard generalized-normal sampler with location zero and scale one.</summary>
    /// <param name="generator">The random source.</param>
    /// <param name="shape">The finite positive shape.</param>
    /// <returns>A standard generalized-normal sampler.</returns>
    public GeneralizedNormalSampler CreateStandardGeneralizedNormalSampler(IRandomSource generator, double shape)
    {
        return new GeneralizedNormalSampler(generator, 0.0, 1.0, shape);
    }

    /// <summary>Creates a standard Gumbel sampler with location zero and scale one.</summary>
    /// <param name="generator">The random source.</param>
    /// <returns>A standard Gumbel sampler.</returns>
    public GumbelSampler CreateStandardGumbelSampler(IRandomSource generator)
    {
        return new GumbelSampler(generator, 0.0, 1.0);
    }

    /// <summary>Creates a unit-scale inverse-gamma sampler.</summary>
    /// <param name="generator">The random source.</param>
    /// <param name="shape">The finite positive shape.</param>
    /// <returns>An inverse-gamma sampler with scale one.</returns>
    public InverseGammaSampler CreateUnitScaleInverseGammaSampler(IRandomSource generator, double shape)
    {
        return new InverseGammaSampler(generator, shape, 1.0);
    }

    /// <summary>Creates a standard inverse-Gaussian sampler with mean and shape one.</summary>
    /// <param name="generator">The random source.</param>
    /// <returns>A standard inverse-Gaussian sampler.</returns>
    public InverseGaussianSampler CreateStandardInverseGaussianSampler(IRandomSource generator)
    {
        return new InverseGaussianSampler(generator, 1.0, 1.0);
    }

    /// <summary>Creates a standard Laplace sampler with location zero and scale one.</summary>
    /// <param name="generator">The random source.</param>
    /// <returns>A standard Laplace sampler.</returns>
    public LaplaceSampler CreateStandardLaplaceSampler(IRandomSource generator)
    {
        return new LaplaceSampler(generator, 0.0, 1.0);
    }

    /// <summary>Creates a standard Levy sampler with location zero and scale one.</summary>
    /// <param name="generator">The random source.</param>
    /// <returns>A standard Levy sampler.</returns>
    public LevySampler CreateStandardLevySampler(IRandomSource generator)
    {
        return new LevySampler(generator, 0.0, 1.0);
    }

    /// <summary>Creates a standard log-normal sampler with log-mean zero and log-standard-deviation one.</summary>
    /// <param name="generator">The random source.</param>
    /// <returns>A standard log-normal sampler.</returns>
    public LogNormalSampler CreateStandardLogNormalSampler(IRandomSource generator)
    {
        return new LogNormalSampler(generator, 0.0, 1.0);
    }

    /// <summary>Creates a standard logistic sampler with location zero and scale one.</summary>
    /// <param name="generator">The random source.</param>
    /// <returns>A standard logistic sampler.</returns>
    public LogisticSampler CreateStandardLogisticSampler(IRandomSource generator)
    {
        return new LogisticSampler(generator, 0.0, 1.0);
    }

    /// <summary>Creates an unshifted unit-scale Lomax sampler.</summary>
    /// <param name="generator">The random source.</param>
    /// <param name="shape">The finite positive tail shape.</param>
    /// <returns>A standard Lomax sampler.</returns>
    public LomaxSampler CreateStandardLomaxSampler(IRandomSource generator, double shape)
    {
        return new LomaxSampler(generator, 0.0, 1.0, shape);
    }

    /// <summary>Creates an unshifted Lomax sampler with an explicit scale.</summary>
    /// <param name="generator">The random source.</param>
    /// <param name="scale">The finite nonnegative scale.</param>
    /// <param name="shape">The finite positive tail shape.</param>
    /// <returns>An unshifted Lomax sampler.</returns>
    public LomaxSampler CreateUnshiftedLomaxSampler(IRandomSource generator, double scale, double shape)
    {
        return new LomaxSampler(generator, 0.0, scale, shape);
    }

    /// <summary>Creates a standard normal sampler with mean zero and standard deviation one.</summary>
    /// <param name="generator">The random source.</param>
    /// <returns>A standard normal sampler.</returns>
    public NormalSampler CreateStandardNormalSampler(IRandomSource generator)
    {
        return new NormalSampler(generator, 0.0, 1.0);
    }

    /// <summary>Creates a standard Pareto type I sampler with minimum one.</summary>
    /// <param name="generator">The random source.</param>
    /// <param name="shape">The finite positive tail shape.</param>
    /// <returns>A standard Pareto type I sampler.</returns>
    public ParetoSampler CreateStandardParetoSampler(IRandomSource generator, double shape)
    {
        return new ParetoSampler(generator, 1.0, shape);
    }

    /// <summary>Creates a standard power-distribution sampler with location zero and scale one.</summary>
    /// <param name="generator">The random source.</param>
    /// <param name="shape">The finite positive shape.</param>
    /// <returns>A standard power-distribution sampler.</returns>
    public PowerSampler CreateStandardPowerSampler(IRandomSource generator, double shape)
    {
        return new PowerSampler(generator, 0.0, 1.0, shape);
    }

    /// <summary>Creates a standard Rayleigh sampler with location zero and scale one.</summary>
    /// <param name="generator">The random source.</param>
    /// <returns>A standard Rayleigh sampler.</returns>
    public RayleighSampler CreateStandardRayleighSampler(IRandomSource generator)
    {
        return new RayleighSampler(generator, 0.0, 1.0);
    }

    /// <summary>Creates a centered unit-scale skewed generalized-error sampler.</summary>
    /// <param name="generator">The random source.</param>
    /// <param name="shape">The finite positive body shape.</param>
    /// <param name="skewness">The finite positive skewness.</param>
    /// <returns>A standard skewed generalized-error sampler.</returns>
    public SkewedGeneralizedErrorSampler CreateStandardSkewedGeneralizedErrorSampler(IRandomSource generator, double shape, double skewness)
    {
        return new SkewedGeneralizedErrorSampler(generator, 0.0, 1.0, shape, skewness);
    }

    /// <summary>Creates a centered unit-scale skewed generalized-t sampler.</summary>
    /// <param name="generator">The random source.</param>
    /// <param name="bodyShape">The finite positive body shape.</param>
    /// <param name="tailShape">The finite positive tail shape.</param>
    /// <param name="skewness">The finite positive skewness.</param>
    /// <returns>A standard skewed generalized-t sampler.</returns>
    public SkewedGeneralizedTSampler CreateStandardSkewedGeneralizedTSampler(IRandomSource generator, double bodyShape, double tailShape, double skewness)
    {
        return new SkewedGeneralizedTSampler(generator, 0.0, 1.0, bodyShape, tailShape, skewness);
    }

    /// <summary>Creates a standardized symmetric stable sampler.</summary>
    /// <param name="generator">The random source.</param>
    /// <param name="stability">The stability parameter.</param>
    /// <returns>A standardized symmetric stable sampler.</returns>
    public StableSampler CreateStandardSymmetricStableSampler(IRandomSource generator, double stability)
    {
        return new StableSampler(generator, 0.0, 1.0, stability, 0.0);
    }

    /// <summary>Creates a standardized stable sampler with explicit skewness.</summary>
    /// <param name="generator">The random source.</param>
    /// <param name="stability">The stability parameter.</param>
    /// <param name="skewness">The skewness parameter.</param>
    /// <returns>A standardized stable sampler.</returns>
    public StableSampler CreateStandardStableSampler(IRandomSource generator, double stability, double skewness)
    {
        return new StableSampler(generator, 0.0, 1.0, stability, skewness);
    }

    /// <summary>Creates a symmetric stable sampler with explicit location and scale.</summary>
    /// <param name="generator">The random source.</param>
    /// <param name="location">The finite S0 location.</param>
    /// <param name="scale">The finite nonnegative S0 scale.</param>
    /// <param name="stability">The stability parameter.</param>
    /// <returns>A symmetric stable sampler.</returns>
    public StableSampler CreateLocationScaleSymmetricStableSampler(IRandomSource generator, double location, double scale, double stability)
    {
        return new StableSampler(generator, location, scale, stability, 0.0);
    }

    /// <summary>Creates the standard triangular distribution on zero to one with midpoint mode.</summary>
    /// <param name="generator">The random source.</param>
    /// <returns>A standard triangular sampler.</returns>
    public TriangularSampler CreateStandardTriangularSampler(IRandomSource generator)
    {
        return new TriangularSampler(generator, 0.0, 1.0, 0.5);
    }

    /// <summary>Creates a standard-normal sampler restricted to explicit bounds.</summary>
    /// <param name="generator">The random source.</param>
    /// <param name="lowerBound">The inclusive lower bound.</param>
    /// <param name="upperBound">The inclusive upper bound.</param>
    /// <returns>A truncated standard-normal sampler.</returns>
    public TruncatedNormalSampler CreateStandardTruncatedNormalSampler(IRandomSource generator, double lowerBound, double upperBound)
    {
        return new TruncatedNormalSampler(generator, 0.0, 1.0, lowerBound, upperBound);
    }

    /// <summary>Creates a zero-mean-direction von Mises sampler.</summary>
    /// <param name="generator">The random source.</param>
    /// <param name="concentration">The finite nonnegative concentration.</param>
    /// <returns>A zero-mean-direction von Mises sampler.</returns>
    public VonMisesSampler CreateZeroMeanVonMisesSampler(IRandomSource generator, double concentration)
    {
        return new VonMisesSampler(generator, 0.0, concentration);
    }

    /// <summary>Creates a standard Weibull sampler with location zero and scale one.</summary>
    /// <param name="generator">The random source.</param>
    /// <param name="shape">The finite positive shape.</param>
    /// <returns>A standard Weibull sampler.</returns>
    public WeibullSampler CreateStandardWeibullSampler(IRandomSource generator, double shape)
    {
        return new WeibullSampler(generator, 0.0, 1.0, shape);
    }

    /// <summary>Creates a weighted sampler with its automatically selected method.</summary>
    public WeightedSampler CreateWeightedSampler(IRandomSource generator, double[] weights)
    {
        return WeightedSampler.FromWeights(generator, weights);
    }

    /// <summary>Creates a weighted sampler with an explicit method.</summary>
    public WeightedSampler CreateWeightedSamplerWithMethod(IRandomSource generator, double[] weights, WeightedSamplingMethod method)
    {
        return WeightedSampler.FromWeights(generator, weights, method);
    }

    /// <summary>Creates a guide-table weighted sampler from weights.</summary>
    public WeightedSampler CreateGuideWeightedSampler(IRandomSource generator, double[] weights)
    {
        return WeightedSampler.FromGuideWeights(generator, weights);
    }

    /// <summary>Creates an alias-table weighted sampler from weights.</summary>
    public WeightedSampler CreateAliasWeightedSampler(IRandomSource generator, double[] weights)
    {
        return WeightedSampler.FromAliasWeights(generator, weights);
    }

    /// <summary>Creates a weighted sampler from a guide table.</summary>
    public WeightedSampler CreateWeightedSamplerFromGuideTable(IRandomSource generator, GuideTable table)
    {
        return WeightedSampler.FromGuideTable(generator, table);
    }

    /// <summary>Creates a weighted sampler from an alias table.</summary>
    public WeightedSampler CreateWeightedSamplerFromAliasTable(IRandomSource generator, AliasTable table)
    {
        return WeightedSampler.FromAliasTable(generator, table);
    }

    /// <summary>Creates an equal-weight empirical sampler.</summary>
    public EmpiricalSampler CreateUnweightedEmpiricalSampler(IRandomSource generator, double[] observations)
    {
        return EmpiricalSampler.CreateUnweighted(generator, observations);
    }

    /// <summary>Creates an identity-value rejection sampler.</summary>
    public RejectionSampler CreateIdentityRejectionSampler(IRandomSource generator, Func<double> proposal, Func<double, double> acceptance, int maximumAttempts)
    {
        return RejectionSampler.CreateIdentity(generator, proposal, acceptance, maximumAttempts);
    }

    /// <summary>Creates a multivariate-normal sampler from a Cholesky factor.</summary>
    public MultivariateNormalSampler CreateMultivariateNormalFromCholesky(IRandomSource generator, double[] mean, double[] choleskyFactor)
    {
        return MultivariateNormalSampler.FromCholesky(generator, mean, choleskyFactor);
    }

    /// <summary>Creates a matrix-normal sampler from Cholesky factors.</summary>
    public MatrixNormalSampler CreateMatrixNormalFromCholesky(IRandomSource generator, double[] mean, int rows, int columns, double[] rowScaleFactor, double[] columnScaleFactor)
    {
        return MatrixNormalSampler.FromCholesky(generator, mean, rows, columns, rowScaleFactor, columnScaleFactor);
    }

    /// <summary>Creates a stratified sampler whose grid is inferred per request.</summary>
    public StratifiedSampler CreateInferredStratifiedSampler(IRandomSource generator, int dimension, bool centered)
    {
        return StratifiedSampler.CreateInferred(generator, dimension, centered);
    }

    /// <summary>Creates a stratified sampler from per-dimension stratum counts.</summary>
    public StratifiedSampler CreateStratifiedSamplerFromStrata(IRandomSource generator, int[] strataPerDimension, bool centered)
    {
        return StratifiedSampler.FromStrata(generator, strataPerDimension, centered);
    }

    /// <summary>Triangulates a simple two-dimensional polygon.</summary>
    public int[] TriangulatePolygon(double[] vertices)
    {
        return PolytopeSampler.Triangulate(vertices);
    }

    /// <summary>Creates a digital-shift scrambler.</summary>
    public DigitalScrambler CreateDigitalShift(IRandomSource generator, int dimension)
    {
        return DigitalScrambler.CreateDigitalShift(generator, dimension);
    }

    /// <summary>Creates a linear-matrix scrambler.</summary>
    public DigitalScrambler CreateLinearMatrix(IRandomSource generator, int dimension)
    {
        return DigitalScrambler.CreateLinearMatrix(generator, dimension);
    }

    /// <summary>Creates a combined linear-matrix and digital-shift scrambler.</summary>
    public DigitalScrambler CreateLinearMatrixWithDigitalShift(IRandomSource generator, int dimension)
    {
        return DigitalScrambler.CreateLinearMatrixWithDigitalShift(generator, dimension);
    }

    /// <summary>Creates an identity digital scrambler.</summary>
    public DigitalScrambler CreateIdentityScrambler(int dimension)
    {
        return DigitalScrambler.CreateIdentity(dimension);
    }

    /// <summary>Creates an unscrambled Sobol sequence.</summary>
    public SobolSequence CreateUnscrambledSobol(int dimension)
    {
        return SobolSequence.CreateUnscrambled(dimension);
    }

    /// <summary>Creates an unscrambled Halton sequence.</summary>
    public HaltonSequence CreateUnscrambledHalton(int dimension)
    {
        return HaltonSequence.CreateUnscrambled(dimension);
    }

    /// <summary>Calculates token entropy in bits.</summary>
    public long TokenEntropyBits(int byteCount)
    {
        return SecureRandomGenerator.TokenEntropyBits(byteCount);
    }

    /// <summary>Calculates string entropy in bits.</summary>
    public double StringEntropyBits(string alphabet, int length)
    {
        return SecureRandomGenerator.StringEntropyBits(alphabet, length);
    }
}
