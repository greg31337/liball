using System;

namespace random;

/// <summary>
/// Creates API objects whose implementation constructors are intentionally non-public.
/// </summary>
public interface IRandomFactory
{
    /// <summary>Creates the default deterministic generator.</summary>
    RandomGenerator CreateDefaultGenerator(ulong seed, RandomMathMode mathMode);

    /// <summary>Creates a named deterministic generator from a seed sequence.</summary>
    RandomGenerator CreateGeneratorFromSeedSequence(RandomAlgorithm algorithm, SeedSequence seedSequence, RandomMathMode mathMode);

    /// <summary>Creates a named deterministic generator from a canonical byte seed.</summary>
    RandomGenerator CreateGeneratorFromBytes(RandomAlgorithm algorithm, byte[] seed, RandomMathMode mathMode);

    /// <summary>Creates a named deterministic generator from a canonical UTF-8 text seed.</summary>
    RandomGenerator CreateGeneratorFromText(RandomAlgorithm algorithm, string seed, RandomMathMode mathMode);

    /// <summary>Creates a named deterministic generator from canonical unsigned seed words.</summary>
    RandomGenerator CreateGeneratorFromUInt32Array(RandomAlgorithm algorithm, uint[] seed, RandomMathMode mathMode);

    /// <summary>Creates a named deterministic generator from operating-system entropy.</summary>
    RandomGenerator CreateGeneratorFromSystemEntropy(RandomAlgorithm algorithm, RandomMathMode mathMode);

    /// <summary>Creates a PCG64 DXSM generator.</summary>
    RandomGenerator CreatePcg64Dxsm(UInt128 initialState, UInt128 streamSelector, RandomMathMode mathMode);

    /// <summary>Creates a xoshiro256** generator.</summary>
    RandomGenerator CreateXoshiro256StarStar(ulong seed, RandomMathMode mathMode);

    /// <summary>Creates a Philox4x64-10 generator from a key.</summary>
    RandomGenerator CreatePhilox4x64Ten(UInt128 key, RandomMathMode mathMode);

    /// <summary>Creates a Philox4x64-10 generator at an explicit counter.</summary>
    RandomGenerator CreatePhilox4x64TenAtCounter(ulong[] key, ulong[] counter, RandomMathMode mathMode);

    /// <summary>Creates a reference-seeded MT19937 generator.</summary>
    RandomGenerator CreateMt19937(uint seed, RandomMathMode mathMode);

    /// <summary>Creates a reference-array-seeded MT19937 generator.</summary>
    RandomGenerator CreateMt19937FromArray(uint[] seed, RandomMathMode mathMode);

    /// <summary>Creates a NumPy-compatible MT19937 generator.</summary>
    RandomGenerator CreateMt19937Numpy(uint seed, RandomMathMode mathMode);

    /// <summary>Creates a NumPy-compatible array-seeded MT19937 generator.</summary>
    RandomGenerator CreateMt19937NumpyFromArray(uint[] seed, RandomMathMode mathMode);

    /// <summary>Creates a SplitMix64 generator.</summary>
    RandomGenerator CreateSplitMix64(ulong seed, RandomMathMode mathMode);

    /// <summary>Restores a deterministic generator from exported state.</summary>
    RandomGenerator RestoreGenerator(RandomState randomState, RandomMathMode mathMode);

    /// <summary>Creates a generator backed by system cryptography.</summary>
    SecureRandomGenerator CreateSystemSecureGenerator();

    /// <summary>Creates an HMAC-DRBG generator from system entropy.</summary>
    SecureRandomGenerator CreateHmacDrbg();

    /// <summary>Creates an HMAC-DRBG generator from explicit seed material.</summary>
    SecureRandomGenerator CreateHmacDrbgFromSeedMaterial(byte[] entropy, byte[] nonce, byte[] personalization);

    /// <summary>Creates a seed sequence from an unsigned integer.</summary>
    SeedSequence CreateSeedFromUInt64(ulong seed);

    /// <summary>Creates a seed sequence from a signed integer.</summary>
    SeedSequence CreateSeedFromInt64(long seed);

    /// <summary>Creates a seed sequence from bytes.</summary>
    SeedSequence CreateSeedFromBytes(byte[] seed);

    /// <summary>Creates a seed sequence from text.</summary>
    SeedSequence CreateSeedFromText(string seed);

    /// <summary>Creates a seed sequence from unsigned words.</summary>
    SeedSequence CreateSeedFromUInt32Array(uint[] seed);

    /// <summary>Creates a seed sequence from system entropy.</summary>
    SeedSequence CreateSeedFromSystemEntropy();

    /// <summary>Decodes a random state from bytes.</summary>
    RandomState CreateStateFromBytes(byte[] encoded);

    /// <summary>Decodes a random state from Base64 text.</summary>
    RandomState CreateStateFromBase64(string encoded);

    /// <summary>Creates a standard Burr XII sampler.</summary>
    BurrSampler CreateStandardBurrSampler(IRandomSource generator, double firstShape, double secondShape);

    /// <summary>Creates a standard Cauchy sampler.</summary>
    CauchySampler CreateStandardCauchySampler(IRandomSource generator);

    /// <summary>Creates a unit-scale Erlang sampler.</summary>
    ErlangSampler CreateUnitScaleErlangSampler(IRandomSource generator, int shape);

    /// <summary>Creates a standard exponential sampler.</summary>
    ExponentialSampler CreateStandardExponentialSampler(IRandomSource generator);

    /// <summary>Creates a unit-scale gamma sampler.</summary>
    GammaSampler CreateUnitScaleGammaSampler(IRandomSource generator, double shape);

    /// <summary>Creates a standard generalized-normal sampler.</summary>
    GeneralizedNormalSampler CreateStandardGeneralizedNormalSampler(IRandomSource generator, double shape);

    /// <summary>Creates a standard Gumbel sampler.</summary>
    GumbelSampler CreateStandardGumbelSampler(IRandomSource generator);

    /// <summary>Creates a unit-scale inverse-gamma sampler.</summary>
    InverseGammaSampler CreateUnitScaleInverseGammaSampler(IRandomSource generator, double shape);

    /// <summary>Creates a standard inverse-Gaussian sampler.</summary>
    InverseGaussianSampler CreateStandardInverseGaussianSampler(IRandomSource generator);

    /// <summary>Creates a standard Laplace sampler.</summary>
    LaplaceSampler CreateStandardLaplaceSampler(IRandomSource generator);

    /// <summary>Creates a standard Levy sampler.</summary>
    LevySampler CreateStandardLevySampler(IRandomSource generator);

    /// <summary>Creates a standard log-normal sampler.</summary>
    LogNormalSampler CreateStandardLogNormalSampler(IRandomSource generator);

    /// <summary>Creates a standard logistic sampler.</summary>
    LogisticSampler CreateStandardLogisticSampler(IRandomSource generator);

    /// <summary>Creates a standard Lomax sampler.</summary>
    LomaxSampler CreateStandardLomaxSampler(IRandomSource generator, double shape);

    /// <summary>Creates an unshifted Lomax sampler with an explicit scale.</summary>
    LomaxSampler CreateUnshiftedLomaxSampler(IRandomSource generator, double scale, double shape);

    /// <summary>Creates a standard normal sampler.</summary>
    NormalSampler CreateStandardNormalSampler(IRandomSource generator);

    /// <summary>Creates a standard Pareto type I sampler.</summary>
    ParetoSampler CreateStandardParetoSampler(IRandomSource generator, double shape);

    /// <summary>Creates a standard power-distribution sampler.</summary>
    PowerSampler CreateStandardPowerSampler(IRandomSource generator, double shape);

    /// <summary>Creates a standard Rayleigh sampler.</summary>
    RayleighSampler CreateStandardRayleighSampler(IRandomSource generator);

    /// <summary>Creates a standard skewed generalized-error sampler.</summary>
    SkewedGeneralizedErrorSampler CreateStandardSkewedGeneralizedErrorSampler(IRandomSource generator, double shape, double skewness);

    /// <summary>Creates a standard skewed generalized-t sampler.</summary>
    SkewedGeneralizedTSampler CreateStandardSkewedGeneralizedTSampler(IRandomSource generator, double bodyShape, double tailShape, double skewness);

    /// <summary>Creates a standardized symmetric stable sampler.</summary>
    StableSampler CreateStandardSymmetricStableSampler(IRandomSource generator, double stability);

    /// <summary>Creates a standardized stable sampler with explicit skewness.</summary>
    StableSampler CreateStandardStableSampler(IRandomSource generator, double stability, double skewness);

    /// <summary>Creates a symmetric stable sampler with explicit location and scale.</summary>
    StableSampler CreateLocationScaleSymmetricStableSampler(IRandomSource generator, double location, double scale, double stability);

    /// <summary>Creates the standard triangular distribution.</summary>
    TriangularSampler CreateStandardTriangularSampler(IRandomSource generator);

    /// <summary>Creates a truncated standard-normal sampler.</summary>
    TruncatedNormalSampler CreateStandardTruncatedNormalSampler(IRandomSource generator, double lowerBound, double upperBound);

    /// <summary>Creates a zero-mean-direction von Mises sampler.</summary>
    VonMisesSampler CreateZeroMeanVonMisesSampler(IRandomSource generator, double concentration);

    /// <summary>Creates a standard Weibull sampler.</summary>
    WeibullSampler CreateStandardWeibullSampler(IRandomSource generator, double shape);

    /// <summary>Creates a weighted sampler with its automatically selected method.</summary>
    WeightedSampler CreateWeightedSampler(IRandomSource generator, double[] weights);

    /// <summary>Creates a weighted sampler with an explicit method.</summary>
    WeightedSampler CreateWeightedSamplerWithMethod(IRandomSource generator, double[] weights, WeightedSamplingMethod method);

    /// <summary>Creates a guide-table weighted sampler from weights.</summary>
    WeightedSampler CreateGuideWeightedSampler(IRandomSource generator, double[] weights);

    /// <summary>Creates an alias-table weighted sampler from weights.</summary>
    WeightedSampler CreateAliasWeightedSampler(IRandomSource generator, double[] weights);

    /// <summary>Creates a weighted sampler from a guide table.</summary>
    WeightedSampler CreateWeightedSamplerFromGuideTable(IRandomSource generator, GuideTable table);

    /// <summary>Creates a weighted sampler from an alias table.</summary>
    WeightedSampler CreateWeightedSamplerFromAliasTable(IRandomSource generator, AliasTable table);

    /// <summary>Creates an equal-weight empirical sampler.</summary>
    EmpiricalSampler CreateUnweightedEmpiricalSampler(IRandomSource generator, double[] observations);

    /// <summary>Creates an identity-value rejection sampler.</summary>
    RejectionSampler CreateIdentityRejectionSampler(IRandomSource generator, Func<double> proposal, Func<double, double> acceptance, int maximumAttempts);

    /// <summary>Creates a multivariate-normal sampler from a Cholesky factor.</summary>
    MultivariateNormalSampler CreateMultivariateNormalFromCholesky(IRandomSource generator, double[] mean, double[] choleskyFactor);

    /// <summary>Creates a matrix-normal sampler from Cholesky factors.</summary>
    MatrixNormalSampler CreateMatrixNormalFromCholesky(IRandomSource generator, double[] mean, int rows, int columns, double[] rowScaleFactor, double[] columnScaleFactor);

    /// <summary>Creates a stratified sampler whose grid is inferred per request.</summary>
    StratifiedSampler CreateInferredStratifiedSampler(IRandomSource generator, int dimension, bool centered);

    /// <summary>Creates a stratified sampler from per-dimension stratum counts.</summary>
    StratifiedSampler CreateStratifiedSamplerFromStrata(IRandomSource generator, int[] strataPerDimension, bool centered);

    /// <summary>Triangulates a simple two-dimensional polygon.</summary>
    int[] TriangulatePolygon(double[] vertices);

    /// <summary>Creates a digital-shift scrambler.</summary>
    DigitalScrambler CreateDigitalShift(IRandomSource generator, int dimension);

    /// <summary>Creates a linear-matrix scrambler.</summary>
    DigitalScrambler CreateLinearMatrix(IRandomSource generator, int dimension);

    /// <summary>Creates a combined linear-matrix and digital-shift scrambler.</summary>
    DigitalScrambler CreateLinearMatrixWithDigitalShift(IRandomSource generator, int dimension);

    /// <summary>Creates an identity digital scrambler.</summary>
    DigitalScrambler CreateIdentityScrambler(int dimension);

    /// <summary>Creates an unscrambled Sobol sequence.</summary>
    SobolSequence CreateUnscrambledSobol(int dimension);

    /// <summary>Creates an unscrambled Halton sequence.</summary>
    HaltonSequence CreateUnscrambledHalton(int dimension);

    /// <summary>Calculates token entropy in bits.</summary>
    long TokenEntropyBits(int byteCount);

    /// <summary>Calculates string entropy in bits.</summary>
    double StringEntropyBits(string alphabet, int length);
}
