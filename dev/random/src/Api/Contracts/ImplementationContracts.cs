using System;

using System.Numerics;

namespace random;

/// <summary>
/// Defines the public operations provided by <see cref="AffineSampler"/>.
/// </summary>
public interface IAffineSampler
{
    /// <summary>
    /// Invokes <see cref="AffineSampler.Sample"/>.
    /// </summary>
    double Sample();

    /// <summary>
    /// Invokes <see cref="AffineSampler.Fill"/>.
    /// </summary>
    void Fill(Span<double> destination);

}

/// <summary>
/// Defines the operations specific to <see cref="RandomGenerator"/> beyond <see cref="IRandomSource"/>.
/// </summary>
public interface IRandomGenerator
{
    /// <summary>
    /// Invokes <see cref="RandomGenerator.Info"/>.
    /// </summary>
    RandomAlgorithmInfo Info();

    /// <summary>
    /// Invokes <see cref="RandomGenerator.Clone"/>.
    /// </summary>
    RandomGenerator Clone();

    /// <summary>
    /// Invokes <see cref="RandomGenerator.Spawn"/>.
    /// </summary>
    RandomGenerator Spawn(ulong index);

    /// <summary>
    /// Invokes <see cref="RandomGenerator.SpawnMany"/>.
    /// </summary>
    RandomGenerator[] SpawnMany(int count);

    /// <summary>
    /// Invokes <see cref="RandomGenerator.NextByteBounded"/>.
    /// </summary>
    byte NextByteBounded(byte bound);

    /// <summary>
    /// Invokes <see cref="RandomGenerator.NextByteInRange"/>.
    /// </summary>
    byte NextByteInRange(byte minimum, byte maximum);

    /// <summary>
    /// Invokes <see cref="RandomGenerator.NextUInt32Bounded"/>.
    /// </summary>
    uint NextUInt32Bounded(uint bound);

    /// <summary>
    /// Invokes <see cref="RandomGenerator.NextUInt32InRange"/>.
    /// </summary>
    uint NextUInt32InRange(uint minimum, uint maximum);

    /// <summary>
    /// Invokes <see cref="RandomGenerator.NextUInt64Bounded"/>.
    /// </summary>
    ulong NextUInt64Bounded(ulong bound);

    /// <summary>
    /// Invokes <see cref="RandomGenerator.NextUInt64InRange"/>.
    /// </summary>
    ulong NextUInt64InRange(ulong minimum, ulong maximum);

    /// <summary>
    /// Invokes <see cref="RandomGenerator.NextInt32"/>.
    /// </summary>
    int NextInt32();

    /// <summary>
    /// Invokes <see cref="RandomGenerator.NextInt32Bounded"/>.
    /// </summary>
    int NextInt32Bounded(int bound);

    /// <summary>
    /// Invokes <see cref="RandomGenerator.NextInt32InRange"/>.
    /// </summary>
    int NextInt32InRange(int minimum, int maximum);

    /// <summary>
    /// Invokes <see cref="RandomGenerator.NextInt64"/>.
    /// </summary>
    long NextInt64();

    /// <summary>
    /// Invokes <see cref="RandomGenerator.NextInt64Bounded"/>.
    /// </summary>
    long NextInt64Bounded(long bound);

    /// <summary>
    /// Invokes <see cref="RandomGenerator.NextInt64InRange"/>.
    /// </summary>
    long NextInt64InRange(long minimum, long maximum);

    /// <summary>
    /// Invokes <see cref="RandomGenerator.NextBigInteger"/>.
    /// </summary>
    BigInteger NextBigInteger(BigInteger bound);

    /// <summary>
    /// Invokes <see cref="RandomGenerator.NextBigIntegerInRange"/>.
    /// </summary>
    BigInteger NextBigIntegerInRange(BigInteger minimum, BigInteger maximum);

    /// <summary>
    /// Invokes <see cref="RandomGenerator.NextSingle"/>.
    /// </summary>
    float NextSingle();

    /// <summary>
    /// Invokes <see cref="RandomGenerator.NextSingleClosedOpen"/>.
    /// </summary>
    float NextSingleClosedOpen();

    /// <summary>
    /// Invokes <see cref="RandomGenerator.NextSingleOpenOpen"/>.
    /// </summary>
    float NextSingleOpenOpen();

    /// <summary>
    /// Invokes <see cref="RandomGenerator.NextSingleOpenClosed"/>.
    /// </summary>
    float NextSingleOpenClosed();

    /// <summary>
    /// Invokes <see cref="RandomGenerator.NextSingleClosedClosed"/>.
    /// </summary>
    float NextSingleClosedClosed();

    /// <summary>
    /// Invokes <see cref="RandomGenerator.NextSingleInRange"/>.
    /// </summary>
    float NextSingleInRange(float minimum, float maximum);

    /// <summary>
    /// Invokes <see cref="RandomGenerator.NextDouble"/>.
    /// </summary>
    double NextDouble();

    /// <summary>
    /// Invokes <see cref="RandomGenerator.NextDoubleClosedOpen"/>.
    /// </summary>
    double NextDoubleClosedOpen();

    /// <summary>
    /// Invokes <see cref="RandomGenerator.NextDoubleOpenOpen"/>.
    /// </summary>
    double NextDoubleOpenOpen();

    /// <summary>
    /// Invokes <see cref="RandomGenerator.NextDoubleOpenClosed"/>.
    /// </summary>
    double NextDoubleOpenClosed();

    /// <summary>
    /// Invokes <see cref="RandomGenerator.NextDoubleClosedClosed"/>.
    /// </summary>
    double NextDoubleClosedClosed();

    /// <summary>
    /// Invokes <see cref="RandomGenerator.NextDoubleInRange"/>.
    /// </summary>
    double NextDoubleInRange(double minimum, double maximum);

    /// <summary>
    /// Invokes <see cref="RandomGenerator.NextDecimal"/>.
    /// </summary>
    decimal NextDecimal();

    /// <summary>
    /// Invokes <see cref="RandomGenerator.NextDecimalInRange"/>.
    /// </summary>
    decimal NextDecimalInRange(decimal minimum, decimal maximum);

    /// <summary>
    /// Invokes <see cref="RandomGenerator.NextBits"/>.
    /// </summary>
    ulong NextBits(int bitCount);

    /// <summary>
    /// Invokes <see cref="RandomGenerator.NextBooleanWithDoubleProbability"/>.
    /// </summary>
    bool NextBooleanWithDoubleProbability(double probability);

    /// <summary>
    /// Invokes <see cref="RandomGenerator.NextBooleanWithSingleProbability"/>.
    /// </summary>
    bool NextBooleanWithSingleProbability(float probability);

    /// <summary>
    /// Invokes <see cref="RandomGenerator.NextSign"/>.
    /// </summary>
    int NextSign();

    /// <summary>
    /// Invokes <see cref="RandomGenerator.NextRademacher"/>.
    /// </summary>
    int NextRademacher();

    /// <summary>
    /// Invokes <see cref="RandomGenerator.NextBytes"/>.
    /// </summary>
    byte[] NextBytes(int length);

    /// <summary>
    /// Invokes <see cref="RandomGenerator.Fill"/>.
    /// </summary>
    void Fill(Span<byte> destination);

    /// <summary>
    /// Invokes <see cref="RandomGenerator.FillBits"/>.
    /// </summary>
    void FillBits(Span<ulong> destination, int bitCount);

    /// <summary>
    /// Invokes <see cref="RandomGenerator.FillUInt32"/>.
    /// </summary>
    void FillUInt32(Span<uint> destination);

    /// <summary>
    /// Invokes <see cref="RandomGenerator.FillInt32"/>.
    /// </summary>
    void FillInt32(Span<int> destination);

    /// <summary>
    /// Invokes <see cref="RandomGenerator.FillUInt64"/>.
    /// </summary>
    void FillUInt64(Span<ulong> destination);

    /// <summary>
    /// Invokes <see cref="RandomGenerator.FillInt64"/>.
    /// </summary>
    void FillInt64(Span<long> destination);

    /// <summary>
    /// Invokes <see cref="RandomGenerator.FillSingles"/>.
    /// </summary>
    void FillSingles(Span<float> destination);

    /// <summary>
    /// Invokes <see cref="RandomGenerator.FillDoubles"/>.
    /// </summary>
    void FillDoubles(Span<double> destination);

    /// <summary>
    /// Invokes <see cref="RandomGenerator.FillDecimals"/>.
    /// </summary>
    void FillDecimals(Span<decimal> destination);

    /// <summary>
    /// Invokes <see cref="RandomGenerator.FillBooleans"/>.
    /// </summary>
    void FillBooleans(Span<bool> destination);

    /// <summary>
    /// Invokes <see cref="RandomGenerator.ExportState"/>.
    /// </summary>
    RandomState ExportState();

    /// <summary>
    /// Invokes <see cref="RandomGenerator.RestoreState"/>.
    /// </summary>
    void RestoreState(RandomState randomState);

    /// <summary>
    /// Invokes <see cref="RandomGenerator.Advance"/>.
    /// </summary>
    void Advance(UInt128 distance);

    /// <summary>
    /// Invokes <see cref="RandomGenerator.Seek"/>.
    /// </summary>
    void Seek(UInt128 blockIndex);

    /// <summary>
    /// Invokes <see cref="RandomGenerator.SeekCounter"/>.
    /// </summary>
    void SeekCounter(ulong[] absoluteCounter);

    /// <summary>
    /// Invokes <see cref="RandomGenerator.ReserveBlocks"/>.
    /// </summary>
    ulong[] ReserveBlocks(UInt128 blockCount);

    /// <summary>
    /// Invokes <see cref="RandomGenerator.Jump"/>.
    /// </summary>
    void Jump();

    /// <summary>
    /// Invokes <see cref="RandomGenerator.LongJump"/>.
    /// </summary>
    void LongJump();

    /// <summary>
    /// Invokes <see cref="RandomGenerator.Reset"/>.
    /// </summary>
    void Reset();

}

/// <summary>
/// Defines the public operations provided by <see cref="AliasTable"/>.
/// </summary>
public interface IAliasTable
{
    /// <summary>
    /// Invokes <see cref="AliasTable.CategoryCount"/>.
    /// </summary>
    int CategoryCount();

    /// <summary>
    /// Invokes <see cref="AliasTable.Sample"/>.
    /// </summary>
    int Sample(IRandomSource generator);

    /// <summary>
    /// Invokes <see cref="AliasTable.Fill"/>.
    /// </summary>
    void Fill(IRandomSource generator, Span<int> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="BernoulliSampler"/>.
/// </summary>
public interface IBernoulliSampler
{
    /// <summary>
    /// Invokes <see cref="BernoulliSampler.Sample"/>.
    /// </summary>
    bool Sample();

    /// <summary>
    /// Invokes <see cref="BernoulliSampler.Fill"/>.
    /// </summary>
    void Fill(Span<bool> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="BetaBinomialSampler"/>.
/// </summary>
public interface IBetaBinomialSampler
{
    /// <summary>
    /// Invokes <see cref="BetaBinomialSampler.Sample"/>.
    /// </summary>
    int Sample();

    /// <summary>
    /// Invokes <see cref="BetaBinomialSampler.Fill"/>.
    /// </summary>
    void Fill(Span<int> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="BetaSampler"/>.
/// </summary>
public interface IBetaSampler
{
    /// <summary>
    /// Invokes <see cref="BetaSampler.Sample"/>.
    /// </summary>
    double Sample();

    /// <summary>
    /// Invokes <see cref="BetaSampler.Fill"/>.
    /// </summary>
    void Fill(Span<double> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="BetaScaledSampler"/>.
/// </summary>
public interface IBetaScaledSampler
{
    /// <summary>
    /// Invokes <see cref="BetaScaledSampler.Sample"/>.
    /// </summary>
    double Sample();

    /// <summary>
    /// Invokes <see cref="BetaScaledSampler.Fill"/>.
    /// </summary>
    void Fill(Span<double> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="BinomialSampler"/>.
/// </summary>
public interface IBinomialSampler
{
    /// <summary>
    /// Invokes <see cref="BinomialSampler.Sample"/>.
    /// </summary>
    int Sample();

    /// <summary>
    /// Invokes <see cref="BinomialSampler.Fill"/>.
    /// </summary>
    void Fill(Span<int> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="BurrSampler"/>.
/// </summary>
public interface IBurrSampler
{
    /// <summary>
    /// Invokes <see cref="BurrSampler.Sample"/>.
    /// </summary>
    double Sample();

    /// <summary>
    /// Invokes <see cref="BurrSampler.Fill"/>.
    /// </summary>
    void Fill(Span<double> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="CategoricalSampler"/>.
/// </summary>
public interface ICategoricalSampler
{
    /// <summary>
    /// Invokes <see cref="CategoricalSampler.Sample"/>.
    /// </summary>
    int Sample();

    /// <summary>
    /// Invokes <see cref="CategoricalSampler.Fill"/>.
    /// </summary>
    void Fill(Span<int> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="CauchySampler"/>.
/// </summary>
public interface ICauchySampler
{
    /// <summary>
    /// Invokes <see cref="CauchySampler.Sample"/>.
    /// </summary>
    double Sample();

    /// <summary>
    /// Invokes <see cref="CauchySampler.Fill"/>.
    /// </summary>
    void Fill(Span<double> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="ChiSampler"/>.
/// </summary>
public interface IChiSampler
{
    /// <summary>
    /// Invokes <see cref="ChiSampler.Sample"/>.
    /// </summary>
    double Sample();

    /// <summary>
    /// Invokes <see cref="ChiSampler.Fill"/>.
    /// </summary>
    void Fill(Span<double> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="ChiSquareSampler"/>.
/// </summary>
public interface IChiSquareSampler
{
    /// <summary>
    /// Invokes <see cref="ChiSquareSampler.Sample"/>.
    /// </summary>
    double Sample();

    /// <summary>
    /// Invokes <see cref="ChiSquareSampler.Fill"/>.
    /// </summary>
    void Fill(Span<double> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="ConwayMaxwellPoissonSampler"/>.
/// </summary>
public interface IConwayMaxwellPoissonSampler
{
    /// <summary>
    /// Invokes <see cref="ConwayMaxwellPoissonSampler.Sample"/>.
    /// </summary>
    long Sample();

    /// <summary>
    /// Invokes <see cref="ConwayMaxwellPoissonSampler.Fill"/>.
    /// </summary>
    void Fill(Span<long> destination);

    /// <summary>
    /// Invokes <see cref="ConwayMaxwellPoissonSampler.NormalizationConverged"/>.
    /// </summary>
    bool NormalizationConverged();

    /// <summary>
    /// Invokes <see cref="ConwayMaxwellPoissonSampler.NormalizationTermCount"/>.
    /// </summary>
    int NormalizationTermCount();

    /// <summary>
    /// Invokes <see cref="ConwayMaxwellPoissonSampler.NormalizationRelativeErrorBound"/>.
    /// </summary>
    double NormalizationRelativeErrorBound();

    /// <summary>
    /// Invokes <see cref="ConwayMaxwellPoissonSampler.LogNormalization"/>.
    /// </summary>
    double LogNormalization();

}

/// <summary>
/// Defines the public operations provided by <see cref="DigitalScrambler"/>.
/// </summary>
public interface IDigitalScrambler
{
    /// <summary>
    /// Invokes <see cref="DigitalScrambler.Dimension"/>.
    /// </summary>
    int Dimension();

    /// <summary>
    /// Invokes <see cref="DigitalScrambler.HasLinearMatrix"/>.
    /// </summary>
    bool HasLinearMatrix();

    /// <summary>
    /// Invokes <see cref="DigitalScrambler.HasDigitalShift"/>.
    /// </summary>
    bool HasDigitalShift();

    /// <summary>
    /// Invokes <see cref="DigitalScrambler.Scramble"/>.
    /// </summary>
    ulong Scramble(int coordinate, ulong value);

}

/// <summary>
/// Defines the public operations provided by <see cref="DirichletSampler"/>.
/// </summary>
public interface IDirichletSampler
{
    /// <summary>
    /// Invokes <see cref="DirichletSampler.ComponentCount"/>.
    /// </summary>
    int ComponentCount();

    /// <summary>
    /// Invokes <see cref="DirichletSampler.RequiredBatchOutputLength"/>.
    /// </summary>
    int RequiredBatchOutputLength(int sampleCount);

    /// <summary>
    /// Invokes <see cref="DirichletSampler.Sample"/>.
    /// </summary>
    double[] Sample();

    /// <summary>
    /// Invokes <see cref="DirichletSampler.SampleInto"/>.
    /// </summary>
    void SampleInto(Span<double> destination);

    /// <summary>
    /// Invokes <see cref="DirichletSampler.Fill"/>.
    /// </summary>
    void Fill(int sampleCount, Span<double> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="DiscreteUniformSampler"/>.
/// </summary>
public interface IDiscreteUniformSampler
{
    /// <summary>
    /// Invokes <see cref="DiscreteUniformSampler.Sample"/>.
    /// </summary>
    int Sample();

    /// <summary>
    /// Invokes <see cref="DiscreteUniformSampler.Fill"/>.
    /// </summary>
    void Fill(Span<int> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="EmpiricalSampler"/>.
/// </summary>
public interface IEmpiricalSampler
{
    /// <summary>
    /// Invokes <see cref="EmpiricalSampler.ObservationCount"/>.
    /// </summary>
    int ObservationCount();

    /// <summary>
    /// Invokes <see cref="EmpiricalSampler.Sample"/>.
    /// </summary>
    double Sample();

    /// <summary>
    /// Invokes <see cref="EmpiricalSampler.Fill"/>.
    /// </summary>
    void Fill(Span<double> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="ErlangSampler"/>.
/// </summary>
public interface IErlangSampler
{
    /// <summary>
    /// Invokes <see cref="ErlangSampler.Sample"/>.
    /// </summary>
    double Sample();

    /// <summary>
    /// Invokes <see cref="ErlangSampler.Fill"/>.
    /// </summary>
    void Fill(Span<double> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="ExponentialSampler"/>.
/// </summary>
public interface IExponentialSampler
{
    /// <summary>
    /// Invokes <see cref="ExponentialSampler.Sample"/>.
    /// </summary>
    double Sample();

    /// <summary>
    /// Invokes <see cref="ExponentialSampler.Fill"/>.
    /// </summary>
    void Fill(Span<double> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="FisherSnedecorSampler"/>.
/// </summary>
public interface IFisherSnedecorSampler
{
    /// <summary>
    /// Invokes <see cref="FisherSnedecorSampler.Sample"/>.
    /// </summary>
    double Sample();

    /// <summary>
    /// Invokes <see cref="FisherSnedecorSampler.Fill"/>.
    /// </summary>
    void Fill(Span<double> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="GammaSampler"/>.
/// </summary>
public interface IGammaSampler
{
    /// <summary>
    /// Invokes <see cref="GammaSampler.Sample"/>.
    /// </summary>
    double Sample();

    /// <summary>
    /// Invokes <see cref="GammaSampler.Fill"/>.
    /// </summary>
    void Fill(Span<double> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="GeneralizedNormalSampler"/>.
/// </summary>
public interface IGeneralizedNormalSampler
{
    /// <summary>
    /// Invokes <see cref="GeneralizedNormalSampler.Sample"/>.
    /// </summary>
    double Sample();

    /// <summary>
    /// Invokes <see cref="GeneralizedNormalSampler.Fill"/>.
    /// </summary>
    void Fill(Span<double> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="GeometricSampler"/>.
/// </summary>
public interface IGeometricSampler
{
    /// <summary>
    /// Invokes <see cref="GeometricSampler.SampleFailures"/>.
    /// </summary>
    long SampleFailures();

    /// <summary>
    /// Invokes <see cref="GeometricSampler.SampleTrials"/>.
    /// </summary>
    long SampleTrials();

    /// <summary>
    /// Invokes <see cref="GeometricSampler.FillFailures"/>.
    /// </summary>
    void FillFailures(Span<long> destination);

    /// <summary>
    /// Invokes <see cref="GeometricSampler.FillTrials"/>.
    /// </summary>
    void FillTrials(Span<long> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="GuideTable"/>.
/// </summary>
public interface IGuideTable
{
    /// <summary>
    /// Invokes <see cref="GuideTable.CategoryCount"/>.
    /// </summary>
    int CategoryCount();

    /// <summary>
    /// Invokes <see cref="GuideTable.Sample"/>.
    /// </summary>
    int Sample(IRandomSource generator);

    /// <summary>
    /// Invokes <see cref="GuideTable.Fill"/>.
    /// </summary>
    void Fill(IRandomSource generator, Span<int> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="GumbelSampler"/>.
/// </summary>
public interface IGumbelSampler
{
    /// <summary>
    /// Invokes <see cref="GumbelSampler.Sample"/>.
    /// </summary>
    double Sample();

    /// <summary>
    /// Invokes <see cref="GumbelSampler.Fill"/>.
    /// </summary>
    void Fill(Span<double> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="HaltonSequence"/>.
/// </summary>
public interface IHaltonSequence
{
    /// <summary>
    /// Invokes <see cref="HaltonSequence.Dimension"/>.
    /// </summary>
    int Dimension();

    /// <summary>
    /// Invokes <see cref="HaltonSequence.Bases"/>.
    /// </summary>
    int[] Bases();

    /// <summary>
    /// Invokes <see cref="HaltonSequence.Index"/>.
    /// </summary>
    ulong Index();

    /// <summary>
    /// Invokes <see cref="HaltonSequence.Next"/>.
    /// </summary>
    double[] Next();

    /// <summary>
    /// Invokes <see cref="HaltonSequence.NextInto"/>.
    /// </summary>
    void NextInto(Span<double> destination);

    /// <summary>
    /// Invokes <see cref="HaltonSequence.Fill"/>.
    /// </summary>
    double[] Fill(int pointCount);

    /// <summary>
    /// Invokes <see cref="HaltonSequence.FillInto"/>.
    /// </summary>
    void FillInto(int pointCount, Span<double> destination);

    /// <summary>
    /// Invokes <see cref="HaltonSequence.RequiredOutputLength"/>.
    /// </summary>
    int RequiredOutputLength(int pointCount);

    /// <summary>
    /// Invokes <see cref="HaltonSequence.Skip"/>.
    /// </summary>
    void Skip(ulong count);

    /// <summary>
    /// Invokes <see cref="HaltonSequence.Reset"/>.
    /// </summary>
    void Reset();

}

/// <summary>
/// Defines the public operations provided by <see cref="HammersleySet"/>.
/// </summary>
public interface IHammersleySet
{
    /// <summary>
    /// Invokes <see cref="HammersleySet.PointCount"/>.
    /// </summary>
    int PointCount();

    /// <summary>
    /// Invokes <see cref="HammersleySet.Dimension"/>.
    /// </summary>
    int Dimension();

    /// <summary>
    /// Invokes <see cref="HammersleySet.Bases"/>.
    /// </summary>
    int[] Bases();

    /// <summary>
    /// Invokes <see cref="HammersleySet.Index"/>.
    /// </summary>
    ulong Index();

    /// <summary>
    /// Invokes <see cref="HammersleySet.Remaining"/>.
    /// </summary>
    int Remaining();

    /// <summary>
    /// Invokes <see cref="HammersleySet.Next"/>.
    /// </summary>
    double[] Next();

    /// <summary>
    /// Invokes <see cref="HammersleySet.NextInto"/>.
    /// </summary>
    void NextInto(Span<double> destination);

    /// <summary>
    /// Invokes <see cref="HammersleySet.Fill"/>.
    /// </summary>
    double[] Fill(int pointCount);

    /// <summary>
    /// Invokes <see cref="HammersleySet.FillInto"/>.
    /// </summary>
    void FillInto(int pointCount, Span<double> destination);

    /// <summary>
    /// Invokes <see cref="HammersleySet.RequiredOutputLength"/>.
    /// </summary>
    int RequiredOutputLength(int pointCount);

    /// <summary>
    /// Invokes <see cref="HammersleySet.Skip"/>.
    /// </summary>
    void Skip(ulong count);

    /// <summary>
    /// Invokes <see cref="HammersleySet.Reset"/>.
    /// </summary>
    void Reset();

}

/// <summary>
/// Defines the public operations provided by <see cref="HypergeometricSampler"/>.
/// </summary>
public interface IHypergeometricSampler
{
    /// <summary>
    /// Invokes <see cref="HypergeometricSampler.Sample"/>.
    /// </summary>
    int Sample();

    /// <summary>
    /// Invokes <see cref="HypergeometricSampler.Fill"/>.
    /// </summary>
    void Fill(Span<int> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="InverseGammaSampler"/>.
/// </summary>
public interface IInverseGammaSampler
{
    /// <summary>
    /// Invokes <see cref="InverseGammaSampler.Sample"/>.
    /// </summary>
    double Sample();

    /// <summary>
    /// Invokes <see cref="InverseGammaSampler.Fill"/>.
    /// </summary>
    void Fill(Span<double> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="InverseGaussianSampler"/>.
/// </summary>
public interface IInverseGaussianSampler
{
    /// <summary>
    /// Invokes <see cref="InverseGaussianSampler.Sample"/>.
    /// </summary>
    double Sample();

    /// <summary>
    /// Invokes <see cref="InverseGaussianSampler.Fill"/>.
    /// </summary>
    void Fill(Span<double> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="InverseTransformSampler"/>.
/// </summary>
public interface IInverseTransformSampler
{
    /// <summary>
    /// Invokes <see cref="InverseTransformSampler.OutputValidation"/>.
    /// </summary>
    InverseTransformOutputValidation OutputValidation();

    /// <summary>
    /// Invokes <see cref="InverseTransformSampler.Sample"/>.
    /// </summary>
    double Sample();

    /// <summary>
    /// Invokes <see cref="InverseTransformSampler.Fill"/>.
    /// </summary>
    void Fill(Span<double> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="InverseWishartSampler"/>.
/// </summary>
public interface IInverseWishartSampler
{
    /// <summary>
    /// Invokes <see cref="InverseWishartSampler.Dimension"/>.
    /// </summary>
    int Dimension();

    /// <summary>
    /// Invokes <see cref="InverseWishartSampler.RequiredOutputLength"/>.
    /// </summary>
    int RequiredOutputLength();

    /// <summary>
    /// Invokes <see cref="InverseWishartSampler.RequiredBatchOutputLength"/>.
    /// </summary>
    int RequiredBatchOutputLength(int sampleCount);

    /// <summary>
    /// Invokes <see cref="InverseWishartSampler.Sample"/>.
    /// </summary>
    double[] Sample();

    /// <summary>
    /// Invokes <see cref="InverseWishartSampler.SampleInto"/>.
    /// </summary>
    void SampleInto(Span<double> destination);

    /// <summary>
    /// Invokes <see cref="InverseWishartSampler.Fill"/>.
    /// </summary>
    void Fill(int sampleCount, Span<double> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="LaplaceSampler"/>.
/// </summary>
public interface ILaplaceSampler
{
    /// <summary>
    /// Invokes <see cref="LaplaceSampler.Sample"/>.
    /// </summary>
    double Sample();

    /// <summary>
    /// Invokes <see cref="LaplaceSampler.Fill"/>.
    /// </summary>
    void Fill(Span<double> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="LatinHypercubeSampler"/>.
/// </summary>
public interface ILatinHypercubeSampler
{
    /// <summary>
    /// Invokes <see cref="LatinHypercubeSampler.Dimension"/>.
    /// </summary>
    int Dimension();

    /// <summary>
    /// Invokes <see cref="LatinHypercubeSampler.IsCentered"/>.
    /// </summary>
    bool IsCentered();

    /// <summary>
    /// Invokes <see cref="LatinHypercubeSampler.Sample"/>.
    /// </summary>
    double[] Sample(int pointCount);

    /// <summary>
    /// Invokes <see cref="LatinHypercubeSampler.SampleInto"/>.
    /// </summary>
    void SampleInto(int pointCount, Span<double> destination);

    /// <summary>
    /// Invokes <see cref="LatinHypercubeSampler.SampleOrthogonal"/>.
    /// </summary>
    double[] SampleOrthogonal(int pointCount);

    /// <summary>
    /// Invokes <see cref="LatinHypercubeSampler.SampleOrthogonalInto"/>.
    /// </summary>
    void SampleOrthogonalInto(int pointCount, Span<double> destination);

    /// <summary>
    /// Invokes <see cref="LatinHypercubeSampler.RequiredOutputLength"/>.
    /// </summary>
    int RequiredOutputLength(int pointCount);

}

/// <summary>
/// Defines the public operations provided by <see cref="LevySampler"/>.
/// </summary>
public interface ILevySampler
{
    /// <summary>
    /// Invokes <see cref="LevySampler.Sample"/>.
    /// </summary>
    double Sample();

    /// <summary>
    /// Invokes <see cref="LevySampler.Fill"/>.
    /// </summary>
    void Fill(Span<double> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="LogisticSampler"/>.
/// </summary>
public interface ILogisticSampler
{
    /// <summary>
    /// Invokes <see cref="LogisticSampler.Sample"/>.
    /// </summary>
    double Sample();

    /// <summary>
    /// Invokes <see cref="LogisticSampler.Fill"/>.
    /// </summary>
    void Fill(Span<double> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="LogNormalSampler"/>.
/// </summary>
public interface ILogNormalSampler
{
    /// <summary>
    /// Invokes <see cref="LogNormalSampler.Sample"/>.
    /// </summary>
    double Sample();

    /// <summary>
    /// Invokes <see cref="LogNormalSampler.Fill"/>.
    /// </summary>
    void Fill(Span<double> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="LogSeriesSampler"/>.
/// </summary>
public interface ILogSeriesSampler
{
    /// <summary>
    /// Invokes <see cref="LogSeriesSampler.Sample"/>.
    /// </summary>
    long Sample();

    /// <summary>
    /// Invokes <see cref="LogSeriesSampler.Fill"/>.
    /// </summary>
    void Fill(Span<long> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="LomaxSampler"/>.
/// </summary>
public interface ILomaxSampler
{
    /// <summary>
    /// Invokes <see cref="LomaxSampler.Sample"/>.
    /// </summary>
    double Sample();

    /// <summary>
    /// Invokes <see cref="LomaxSampler.Fill"/>.
    /// </summary>
    void Fill(Span<double> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="MarkovChain"/>.
/// </summary>
public interface IMarkovChain
{
    /// <summary>
    /// Invokes <see cref="MarkovChain.StateCount"/>.
    /// </summary>
    int StateCount();

    /// <summary>
    /// Invokes <see cref="MarkovChain.TransitionProbability"/>.
    /// </summary>
    double TransitionProbability(int currentState, int nextState);

    /// <summary>
    /// Invokes <see cref="MarkovChain.TransitionTable"/>.
    /// </summary>
    AliasTable TransitionTable(int currentState);

    /// <summary>
    /// Invokes <see cref="MarkovChain.NextState"/>.
    /// </summary>
    int NextState(IRandomSource generator, int currentState);

    /// <summary>
    /// Invokes <see cref="MarkovChain.Path"/>.
    /// </summary>
    int[] Path(IRandomSource generator, int initialState, int stepCount);

    /// <summary>
    /// Invokes <see cref="MarkovChain.PathInto"/>.
    /// </summary>
    void PathInto(IRandomSource generator, int initialState, int stepCount, Span<int> destination);

    /// <summary>
    /// Invokes <see cref="MarkovChain.Paths"/>.
    /// </summary>
    int[] Paths(IRandomSource generator, int initialState, int pathCount, int stepCount);

    /// <summary>
    /// Invokes <see cref="MarkovChain.PathsInto"/>.
    /// </summary>
    void PathsInto(IRandomSource generator, int initialState, int pathCount, int stepCount, Span<int> destination);

    /// <summary>
    /// Invokes <see cref="MarkovChain.PathOutputLength"/>.
    /// </summary>
    int PathOutputLength(int stepCount);

    /// <summary>
    /// Invokes <see cref="MarkovChain.PathBatchOutputLength"/>.
    /// </summary>
    int PathBatchOutputLength(int pathCount, int stepCount);

}

/// <summary>
/// Defines the public operations provided by <see cref="MatrixNormalSampler"/>.
/// </summary>
public interface IMatrixNormalSampler
{
    /// <summary>
    /// Invokes <see cref="MatrixNormalSampler.RowCount"/>.
    /// </summary>
    int RowCount();

    /// <summary>
    /// Invokes <see cref="MatrixNormalSampler.ColumnCount"/>.
    /// </summary>
    int ColumnCount();

    /// <summary>
    /// Invokes <see cref="MatrixNormalSampler.RequiredOutputLength"/>.
    /// </summary>
    int RequiredOutputLength();

    /// <summary>
    /// Invokes <see cref="MatrixNormalSampler.RequiredBatchOutputLength"/>.
    /// </summary>
    int RequiredBatchOutputLength(int sampleCount);

    /// <summary>
    /// Invokes <see cref="MatrixNormalSampler.Sample"/>.
    /// </summary>
    double[] Sample();

    /// <summary>
    /// Invokes <see cref="MatrixNormalSampler.SampleInto"/>.
    /// </summary>
    void SampleInto(Span<double> destination);

    /// <summary>
    /// Invokes <see cref="MatrixNormalSampler.Fill"/>.
    /// </summary>
    void Fill(int sampleCount, Span<double> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="MixtureSampler"/>.
/// </summary>
public interface IMixtureSampler
{
    /// <summary>
    /// Invokes <see cref="MixtureSampler.ComponentCount"/>.
    /// </summary>
    int ComponentCount();

    /// <summary>
    /// Invokes <see cref="MixtureSampler.Sample"/>.
    /// </summary>
    double Sample();

    /// <summary>
    /// Invokes <see cref="MixtureSampler.Fill"/>.
    /// </summary>
    void Fill(Span<double> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="MultinomialSampler"/>.
/// </summary>
public interface IMultinomialSampler
{
    /// <summary>
    /// Invokes <see cref="MultinomialSampler.CategoryCount"/>.
    /// </summary>
    int CategoryCount();

    /// <summary>
    /// Invokes <see cref="MultinomialSampler.RequiredBatchOutputLength"/>.
    /// </summary>
    int RequiredBatchOutputLength(int sampleCount);

    /// <summary>
    /// Invokes <see cref="MultinomialSampler.Sample"/>.
    /// </summary>
    int[] Sample();

    /// <summary>
    /// Invokes <see cref="MultinomialSampler.SampleInto"/>.
    /// </summary>
    void SampleInto(Span<int> destination);

    /// <summary>
    /// Invokes <see cref="MultinomialSampler.Fill"/>.
    /// </summary>
    void Fill(int sampleCount, Span<int> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="MultivariateHypergeometricSampler"/>.
/// </summary>
public interface IMultivariateHypergeometricSampler
{
    /// <summary>
    /// Invokes <see cref="MultivariateHypergeometricSampler.CategoryCount"/>.
    /// </summary>
    int CategoryCount();

    /// <summary>
    /// Invokes <see cref="MultivariateHypergeometricSampler.RequiredBatchOutputLength"/>.
    /// </summary>
    int RequiredBatchOutputLength(int sampleCount);

    /// <summary>
    /// Invokes <see cref="MultivariateHypergeometricSampler.Sample"/>.
    /// </summary>
    int[] Sample();

    /// <summary>
    /// Invokes <see cref="MultivariateHypergeometricSampler.SampleInto"/>.
    /// </summary>
    void SampleInto(Span<int> destination);

    /// <summary>
    /// Invokes <see cref="MultivariateHypergeometricSampler.Fill"/>.
    /// </summary>
    void Fill(int sampleCount, Span<int> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="MultivariateNormalSampler"/>.
/// </summary>
public interface IMultivariateNormalSampler
{
    /// <summary>
    /// Invokes <see cref="MultivariateNormalSampler.Dimension"/>.
    /// </summary>
    int Dimension();

    /// <summary>
    /// Invokes <see cref="MultivariateNormalSampler.RequiredOutputLength"/>.
    /// </summary>
    int RequiredOutputLength(int sampleCount);

    /// <summary>
    /// Invokes <see cref="MultivariateNormalSampler.RequiredBatchOutputLength"/>.
    /// </summary>
    int RequiredBatchOutputLength(int sampleCount);

    /// <summary>
    /// Invokes <see cref="MultivariateNormalSampler.Sample"/>.
    /// </summary>
    double[] Sample();

    /// <summary>
    /// Invokes <see cref="MultivariateNormalSampler.SampleInto"/>.
    /// </summary>
    void SampleInto(Span<double> destination);

    /// <summary>
    /// Invokes <see cref="MultivariateNormalSampler.Fill"/>.
    /// </summary>
    void Fill(int sampleCount, Span<double> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="NegativeBinomialSampler"/>.
/// </summary>
public interface INegativeBinomialSampler
{
    /// <summary>
    /// Invokes <see cref="NegativeBinomialSampler.Sample"/>.
    /// </summary>
    long Sample();

    /// <summary>
    /// Invokes <see cref="NegativeBinomialSampler.Fill"/>.
    /// </summary>
    void Fill(Span<long> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="NoncentralChiSquareSampler"/>.
/// </summary>
public interface INoncentralChiSquareSampler
{
    /// <summary>
    /// Invokes <see cref="NoncentralChiSquareSampler.Sample"/>.
    /// </summary>
    double Sample();

    /// <summary>
    /// Invokes <see cref="NoncentralChiSquareSampler.Fill"/>.
    /// </summary>
    void Fill(Span<double> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="NoncentralFSampler"/>.
/// </summary>
public interface INoncentralFSampler
{
    /// <summary>
    /// Invokes <see cref="NoncentralFSampler.Sample"/>.
    /// </summary>
    double Sample();

    /// <summary>
    /// Invokes <see cref="NoncentralFSampler.Fill"/>.
    /// </summary>
    void Fill(Span<double> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="NormalGammaSampler"/>.
/// </summary>
public interface INormalGammaSampler
{
    /// <summary>
    /// Invokes <see cref="NormalGammaSampler.ComponentCount"/>.
    /// </summary>
    int ComponentCount();

    /// <summary>
    /// Invokes <see cref="NormalGammaSampler.RequiredBatchOutputLength"/>.
    /// </summary>
    int RequiredBatchOutputLength(int sampleCount);

    /// <summary>
    /// Invokes <see cref="NormalGammaSampler.Sample"/>.
    /// </summary>
    double[] Sample();

    /// <summary>
    /// Invokes <see cref="NormalGammaSampler.SampleInto"/>.
    /// </summary>
    void SampleInto(Span<double> destination);

    /// <summary>
    /// Invokes <see cref="NormalGammaSampler.Fill"/>.
    /// </summary>
    void Fill(int sampleCount, Span<double> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="NormalSampler"/>.
/// </summary>
public interface INormalSampler
{
    /// <summary>
    /// Invokes <see cref="NormalSampler.Sample"/>.
    /// </summary>
    double Sample();

    /// <summary>
    /// Invokes <see cref="NormalSampler.Fill"/>.
    /// </summary>
    void Fill(Span<double> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="OrthogonalMatrixSampler"/>.
/// </summary>
public interface IOrthogonalMatrixSampler
{
    /// <summary>
    /// Invokes <see cref="OrthogonalMatrixSampler.Dimension"/>.
    /// </summary>
    int Dimension();

    /// <summary>
    /// Invokes <see cref="OrthogonalMatrixSampler.RequiredOutputLength"/>.
    /// </summary>
    int RequiredOutputLength(int sampleCount);

    /// <summary>
    /// Invokes <see cref="OrthogonalMatrixSampler.Sample"/>.
    /// </summary>
    double[] Sample();

    /// <summary>
    /// Invokes <see cref="OrthogonalMatrixSampler.SampleInto"/>.
    /// </summary>
    void SampleInto(Span<double> destination);

    /// <summary>
    /// Invokes <see cref="OrthogonalMatrixSampler.Fill"/>.
    /// </summary>
    void Fill(int sampleCount, Span<double> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="ParetoSampler"/>.
/// </summary>
public interface IParetoSampler
{
    /// <summary>
    /// Invokes <see cref="ParetoSampler.Sample"/>.
    /// </summary>
    double Sample();

    /// <summary>
    /// Invokes <see cref="ParetoSampler.Fill"/>.
    /// </summary>
    void Fill(Span<double> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="PoissonDiskSampler"/>.
/// </summary>
public interface IPoissonDiskSampler
{
    /// <summary>
    /// Invokes <see cref="PoissonDiskSampler.Dimension"/>.
    /// </summary>
    int Dimension();

    /// <summary>
    /// Invokes <see cref="PoissonDiskSampler.MinimumDistance"/>.
    /// </summary>
    double MinimumDistance();

    /// <summary>
    /// Invokes <see cref="PoissonDiskSampler.MaximumAttemptsPerPoint"/>.
    /// </summary>
    int MaximumAttemptsPerPoint();

    /// <summary>
    /// Invokes <see cref="PoissonDiskSampler.LastAcceptedPointCount"/>.
    /// </summary>
    int LastAcceptedPointCount();

    /// <summary>
    /// Invokes <see cref="PoissonDiskSampler.LastAttemptCount"/>.
    /// </summary>
    long LastAttemptCount();

    /// <summary>
    /// Invokes <see cref="PoissonDiskSampler.TotalAttemptCount"/>.
    /// </summary>
    long TotalAttemptCount();

    /// <summary>
    /// Invokes <see cref="PoissonDiskSampler.Sample"/>.
    /// </summary>
    double[] Sample(int pointCount);

    /// <summary>
    /// Invokes <see cref="PoissonDiskSampler.SampleInto"/>.
    /// </summary>
    void SampleInto(int pointCount, Span<double> destination);

    /// <summary>
    /// Invokes <see cref="PoissonDiskSampler.RequiredOutputLength"/>.
    /// </summary>
    int RequiredOutputLength(int pointCount);

}

/// <summary>
/// Defines the public operations provided by <see cref="PoissonSampler"/>.
/// </summary>
public interface IPoissonSampler
{
    /// <summary>
    /// Invokes <see cref="PoissonSampler.Sample"/>.
    /// </summary>
    long Sample();

    /// <summary>
    /// Invokes <see cref="PoissonSampler.Fill"/>.
    /// </summary>
    void Fill(Span<long> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="PolytopeSampler"/>.
/// </summary>
public interface IPolytopeSampler
{
    /// <summary>
    /// Invokes <see cref="PolytopeSampler.VertexCount"/>.
    /// </summary>
    int VertexCount();

    /// <summary>
    /// Invokes <see cref="PolytopeSampler.TriangleCount"/>.
    /// </summary>
    int TriangleCount();

    /// <summary>
    /// Invokes <see cref="PolytopeSampler.TriangleIndexes"/>.
    /// </summary>
    int[] TriangleIndexes();

    /// <summary>
    /// Invokes <see cref="PolytopeSampler.RequiredOutputLength"/>.
    /// </summary>
    int RequiredOutputLength(int sampleCount);

    /// <summary>
    /// Invokes <see cref="PolytopeSampler.Sample"/>.
    /// </summary>
    double[] Sample();

    /// <summary>
    /// Invokes <see cref="PolytopeSampler.SampleInto"/>.
    /// </summary>
    void SampleInto(Span<double> destination);

    /// <summary>
    /// Invokes <see cref="PolytopeSampler.Fill"/>.
    /// </summary>
    void Fill(int sampleCount, Span<double> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="PowerSampler"/>.
/// </summary>
public interface IPowerSampler
{
    /// <summary>
    /// Invokes <see cref="PowerSampler.Sample"/>.
    /// </summary>
    double Sample();

    /// <summary>
    /// Invokes <see cref="PowerSampler.Fill"/>.
    /// </summary>
    void Fill(Span<double> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="RademacherSampler"/>.
/// </summary>
public interface IRademacherSampler
{
    /// <summary>
    /// Invokes <see cref="RademacherSampler.Sample"/>.
    /// </summary>
    int Sample();

    /// <summary>
    /// Invokes <see cref="RademacherSampler.Fill"/>.
    /// </summary>
    void Fill(Span<int> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="RandomAlgorithmInfo"/>.
/// </summary>
public interface IRandomAlgorithmInfo
{
    /// <summary>
    /// Invokes <see cref="RandomAlgorithmInfo.Algorithm"/>.
    /// </summary>
    RandomAlgorithm Algorithm();

    /// <summary>
    /// Invokes <see cref="RandomAlgorithmInfo.Name"/>.
    /// </summary>
    string Name();

    /// <summary>
    /// Invokes <see cref="RandomAlgorithmInfo.Version"/>.
    /// </summary>
    int Version();

    /// <summary>
    /// Invokes <see cref="RandomAlgorithmInfo.TransformVersion"/>.
    /// </summary>
    int TransformVersion();

    /// <summary>
    /// Invokes <see cref="RandomAlgorithmInfo.WordBits"/>.
    /// </summary>
    int WordBits();

    /// <summary>
    /// Invokes <see cref="RandomAlgorithmInfo.StateBytes"/>.
    /// </summary>
    int StateBytes();

    /// <summary>
    /// Invokes <see cref="RandomAlgorithmInfo.IsCryptographic"/>.
    /// </summary>
    bool IsCryptographic();

    /// <summary>
    /// Invokes <see cref="RandomAlgorithmInfo.PeriodDescription"/>.
    /// </summary>
    string PeriodDescription();

    /// <summary>
    /// Invokes <see cref="RandomAlgorithmInfo.Capabilities"/>.
    /// </summary>
    RandomCapability[] Capabilities();

    /// <summary>
    /// Invokes <see cref="RandomAlgorithmInfo.HasCapability"/>.
    /// </summary>
    bool HasCapability(RandomCapability capability);

}

/// <summary>
/// Defines the public operations provided by <see cref="RandomState"/>.
/// </summary>
public interface IRandomState
{
    /// <summary>
    /// Invokes <see cref="RandomState.Algorithm"/>.
    /// </summary>
    RandomAlgorithm Algorithm();

    /// <summary>
    /// Invokes <see cref="RandomState.AlgorithmVersion"/>.
    /// </summary>
    int AlgorithmVersion();

    /// <summary>
    /// Invokes <see cref="RandomState.TransformVersion"/>.
    /// </summary>
    int TransformVersion();

    /// <summary>
    /// Invokes <see cref="RandomState.ToBytes"/>.
    /// </summary>
    byte[] ToBytes();

    /// <summary>
    /// Invokes <see cref="RandomState.ToBase64"/>.
    /// </summary>
    string ToBase64();

}

/// <summary>
/// Defines the public operations provided by <see cref="RayleighSampler"/>.
/// </summary>
public interface IRayleighSampler
{
    /// <summary>
    /// Invokes <see cref="RayleighSampler.Sample"/>.
    /// </summary>
    double Sample();

    /// <summary>
    /// Invokes <see cref="RayleighSampler.Fill"/>.
    /// </summary>
    void Fill(Span<double> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="RejectionSampler"/>.
/// </summary>
public interface IRejectionSampler
{
    /// <summary>
    /// Invokes <see cref="RejectionSampler.MaximumAttempts"/>.
    /// </summary>
    int MaximumAttempts();

    /// <summary>
    /// Invokes <see cref="RejectionSampler.LastAttemptCount"/>.
    /// </summary>
    int LastAttemptCount();

    /// <summary>
    /// Invokes <see cref="RejectionSampler.TotalAttemptCount"/>.
    /// </summary>
    long TotalAttemptCount();

    /// <summary>
    /// Invokes <see cref="RejectionSampler.Sample"/>.
    /// </summary>
    double Sample();

    /// <summary>
    /// Invokes <see cref="RejectionSampler.Fill"/>.
    /// </summary>
    void Fill(Span<double> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="SecureRandomGenerator"/>.
/// </summary>
public interface ISecureRandomGenerator
{
    /// <summary>
    /// Invokes <see cref="SecureRandomGenerator.Info"/>.
    /// </summary>
    RandomAlgorithmInfo Info();

    /// <summary>
    /// Invokes <see cref="SecureRandomGenerator.ReseedCounter"/>.
    /// </summary>
    ulong ReseedCounter();

    /// <summary>
    /// Invokes <see cref="SecureRandomGenerator.IsReseedRequired"/>.
    /// </summary>
    bool IsReseedRequired();

    /// <summary>
    /// Invokes <see cref="SecureRandomGenerator.Reseed"/>.
    /// </summary>
    void Reseed();

    /// <summary>
    /// Invokes <see cref="SecureRandomGenerator.ReseedWithAdditionalInput"/>.
    /// </summary>
    void ReseedWithAdditionalInput(byte[] additionalInput);

    /// <summary>
    /// Invokes <see cref="SecureRandomGenerator.Generate"/>.
    /// </summary>
    void Generate(Span<byte> destination);

    /// <summary>
    /// Invokes <see cref="SecureRandomGenerator.GenerateWithOptions"/>.
    /// </summary>
    void GenerateWithOptions(Span<byte> destination, byte[] additionalInput, bool predictionResistance);

    /// <summary>
    /// Invokes <see cref="SecureRandomGenerator.NextBytes"/>.
    /// </summary>
    byte[] NextBytes(int length);

    /// <summary>
    /// Invokes <see cref="SecureRandomGenerator.FillBytes"/>.
    /// </summary>
    void FillBytes(Span<byte> destination);

    /// <summary>
    /// Invokes <see cref="SecureRandomGenerator.NextByteBounded"/>.
    /// </summary>
    byte NextByteBounded(byte bound);

    /// <summary>
    /// Invokes <see cref="SecureRandomGenerator.NextByteInRange"/>.
    /// </summary>
    byte NextByteInRange(byte minimum, byte maximum);

    /// <summary>
    /// Invokes <see cref="SecureRandomGenerator.NextUInt32"/>.
    /// </summary>
    uint NextUInt32();

    /// <summary>
    /// Invokes <see cref="SecureRandomGenerator.NextUInt32Bounded"/>.
    /// </summary>
    uint NextUInt32Bounded(uint bound);

    /// <summary>
    /// Invokes <see cref="SecureRandomGenerator.NextUInt32InRange"/>.
    /// </summary>
    uint NextUInt32InRange(uint minimum, uint maximum);

    /// <summary>
    /// Invokes <see cref="SecureRandomGenerator.NextUInt64"/>.
    /// </summary>
    ulong NextUInt64();

    /// <summary>
    /// Invokes <see cref="SecureRandomGenerator.NextUInt64Bounded"/>.
    /// </summary>
    ulong NextUInt64Bounded(ulong bound);

    /// <summary>
    /// Invokes <see cref="SecureRandomGenerator.NextUInt64InRange"/>.
    /// </summary>
    ulong NextUInt64InRange(ulong minimum, ulong maximum);

    /// <summary>
    /// Invokes <see cref="SecureRandomGenerator.NextInt32"/>.
    /// </summary>
    int NextInt32();

    /// <summary>
    /// Invokes <see cref="SecureRandomGenerator.NextInt32Bounded"/>.
    /// </summary>
    int NextInt32Bounded(int bound);

    /// <summary>
    /// Invokes <see cref="SecureRandomGenerator.NextInt32InRange"/>.
    /// </summary>
    int NextInt32InRange(int minimum, int maximum);

    /// <summary>
    /// Invokes <see cref="SecureRandomGenerator.NextInt64"/>.
    /// </summary>
    long NextInt64();

    /// <summary>
    /// Invokes <see cref="SecureRandomGenerator.NextInt64Bounded"/>.
    /// </summary>
    long NextInt64Bounded(long bound);

    /// <summary>
    /// Invokes <see cref="SecureRandomGenerator.NextInt64InRange"/>.
    /// </summary>
    long NextInt64InRange(long minimum, long maximum);

    /// <summary>
    /// Invokes <see cref="SecureRandomGenerator.NextBigInteger"/>.
    /// </summary>
    BigInteger NextBigInteger(BigInteger bound);

    /// <summary>
    /// Invokes <see cref="SecureRandomGenerator.NextBigIntegerInRange"/>.
    /// </summary>
    BigInteger NextBigIntegerInRange(BigInteger minimum, BigInteger maximum);

    /// <summary>
    /// Invokes <see cref="SecureRandomGenerator.NextBoolean"/>.
    /// </summary>
    bool NextBoolean();

    /// <summary>
    /// Invokes <see cref="SecureRandomGenerator.TokenHex"/>.
    /// </summary>
    string TokenHex(int byteCount);

    /// <summary>
    /// Invokes <see cref="SecureRandomGenerator.TokenBase32"/>.
    /// </summary>
    string TokenBase32(int byteCount);

    /// <summary>
    /// Invokes <see cref="SecureRandomGenerator.TokenBase64"/>.
    /// </summary>
    string TokenBase64(int byteCount);

    /// <summary>
    /// Invokes <see cref="SecureRandomGenerator.TokenBase64Url"/>.
    /// </summary>
    string TokenBase64Url(int byteCount);

    /// <summary>
    /// Invokes <see cref="SecureRandomGenerator.String"/>.
    /// </summary>
    string String(string alphabet, int length);

    /// <summary>
    /// Invokes <see cref="SecureRandomGenerator.Password"/>.
    /// </summary>
    string Password(PasswordPolicy policy);

    /// <summary>
    /// Invokes <see cref="SecureRandomGenerator.Uuid4"/>.
    /// </summary>
    Guid Uuid4();

    /// <summary>
    /// Invokes <see cref="SecureRandomGenerator.Uuid7"/>.
    /// </summary>
    Guid Uuid7();

    /// <summary>
    /// Invokes <see cref="SecureRandomGenerator.Uninstantiate"/>.
    /// </summary>
    void Uninstantiate();

}

/// <summary>
/// Defines the public operations provided by <see cref="SeedSequence"/>.
/// </summary>
public interface ISeedSequence
{
    /// <summary>
    /// Invokes <see cref="SeedSequence.Spawn"/>.
    /// </summary>
    SeedSequence Spawn(ulong index);

    /// <summary>
    /// Invokes <see cref="SeedSequence.SpawnPath"/>.
    /// </summary>
    SeedSequence SpawnPath(ulong[] path);

    /// <summary>
    /// Invokes <see cref="SeedSequence.SpawnMany"/>.
    /// </summary>
    SeedSequence[] SpawnMany(int count);

    /// <summary>
    /// Invokes <see cref="SeedSequence.GenerateBytes"/>.
    /// </summary>
    byte[] GenerateBytes(int length);

    /// <summary>
    /// Invokes <see cref="SeedSequence.Fingerprint"/>.
    /// </summary>
    string Fingerprint();

    /// <summary>
    /// Invokes <see cref="SeedSequence.Path"/>.
    /// </summary>
    ulong[] Path();

}

/// <summary>
/// Defines the public operations provided by <see cref="SkewedGeneralizedErrorSampler"/>.
/// </summary>
public interface ISkewedGeneralizedErrorSampler
{
    /// <summary>
    /// Invokes <see cref="SkewedGeneralizedErrorSampler.Sample"/>.
    /// </summary>
    double Sample();

    /// <summary>
    /// Invokes <see cref="SkewedGeneralizedErrorSampler.Fill"/>.
    /// </summary>
    void Fill(Span<double> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="SkewedGeneralizedTSampler"/>.
/// </summary>
public interface ISkewedGeneralizedTSampler
{
    /// <summary>
    /// Invokes <see cref="SkewedGeneralizedTSampler.Sample"/>.
    /// </summary>
    double Sample();

    /// <summary>
    /// Invokes <see cref="SkewedGeneralizedTSampler.Fill"/>.
    /// </summary>
    void Fill(Span<double> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="SobolSequence"/>.
/// </summary>
public interface ISobolSequence
{
    /// <summary>
    /// Invokes <see cref="SobolSequence.Dimension"/>.
    /// </summary>
    int Dimension();

    /// <summary>
    /// Invokes <see cref="SobolSequence.Index"/>.
    /// </summary>
    ulong Index();

    /// <summary>
    /// Invokes <see cref="SobolSequence.Next"/>.
    /// </summary>
    double[] Next();

    /// <summary>
    /// Invokes <see cref="SobolSequence.NextInto"/>.
    /// </summary>
    void NextInto(Span<double> destination);

    /// <summary>
    /// Invokes <see cref="SobolSequence.Fill"/>.
    /// </summary>
    double[] Fill(int pointCount);

    /// <summary>
    /// Invokes <see cref="SobolSequence.FillInto"/>.
    /// </summary>
    void FillInto(int pointCount, Span<double> destination);

    /// <summary>
    /// Invokes <see cref="SobolSequence.FillPowerOfTwo"/>.
    /// </summary>
    double[] FillPowerOfTwo(int exponent);

    /// <summary>
    /// Invokes <see cref="SobolSequence.FillPowerOfTwoInto"/>.
    /// </summary>
    void FillPowerOfTwoInto(int exponent, Span<double> destination);

    /// <summary>
    /// Invokes <see cref="SobolSequence.RequiredPowerOfTwoOutputLength"/>.
    /// </summary>
    int RequiredPowerOfTwoOutputLength(int exponent);

    /// <summary>
    /// Invokes <see cref="SobolSequence.RequiredOutputLength"/>.
    /// </summary>
    int RequiredOutputLength(int pointCount);

    /// <summary>
    /// Invokes <see cref="SobolSequence.Skip"/>.
    /// </summary>
    void Skip(ulong count);

    /// <summary>
    /// Invokes <see cref="SobolSequence.FastForward"/>.
    /// </summary>
    void FastForward(ulong count);

    /// <summary>
    /// Invokes <see cref="SobolSequence.Reset"/>.
    /// </summary>
    void Reset();

}

/// <summary>
/// Defines the public operations provided by <see cref="StableSampler"/>.
/// </summary>
public interface IStableSampler
{
    /// <summary>
    /// Invokes <see cref="StableSampler.Sample"/>.
    /// </summary>
    double Sample();

    /// <summary>
    /// Invokes <see cref="StableSampler.Fill"/>.
    /// </summary>
    void Fill(Span<double> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="StratifiedSampler"/>.
/// </summary>
public interface IStratifiedSampler
{
    /// <summary>
    /// Invokes <see cref="StratifiedSampler.Dimension"/>.
    /// </summary>
    int Dimension();

    /// <summary>
    /// Invokes <see cref="StratifiedSampler.IsCentered"/>.
    /// </summary>
    bool IsCentered();

    /// <summary>
    /// Invokes <see cref="StratifiedSampler.ConfiguredPointCount"/>.
    /// </summary>
    int ConfiguredPointCount();

    /// <summary>
    /// Invokes <see cref="StratifiedSampler.Sample"/>.
    /// </summary>
    double[] Sample(int pointCount);

    /// <summary>
    /// Invokes <see cref="StratifiedSampler.SampleInto"/>.
    /// </summary>
    void SampleInto(int pointCount, Span<double> destination);

    /// <summary>
    /// Invokes <see cref="StratifiedSampler.RequiredOutputLength"/>.
    /// </summary>
    int RequiredOutputLength(int pointCount);

}

/// <summary>
/// Defines the public operations provided by <see cref="StudentTSampler"/>.
/// </summary>
public interface IStudentTSampler
{
    /// <summary>
    /// Invokes <see cref="StudentTSampler.Sample"/>.
    /// </summary>
    double Sample();

    /// <summary>
    /// Invokes <see cref="StudentTSampler.Fill"/>.
    /// </summary>
    void Fill(Span<double> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="TriangularSampler"/>.
/// </summary>
public interface ITriangularSampler
{
    /// <summary>
    /// Invokes <see cref="TriangularSampler.Sample"/>.
    /// </summary>
    double Sample();

    /// <summary>
    /// Invokes <see cref="TriangularSampler.Fill"/>.
    /// </summary>
    void Fill(Span<double> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="TruncatedNormalSampler"/>.
/// </summary>
public interface ITruncatedNormalSampler
{
    /// <summary>
    /// Invokes <see cref="TruncatedNormalSampler.Sample"/>.
    /// </summary>
    double Sample();

    /// <summary>
    /// Invokes <see cref="TruncatedNormalSampler.Fill"/>.
    /// </summary>
    void Fill(Span<double> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="TruncatedSampler"/>.
/// </summary>
public interface ITruncatedSampler
{
    /// <summary>
    /// Invokes <see cref="TruncatedSampler.MaximumAttempts"/>.
    /// </summary>
    int MaximumAttempts();

    /// <summary>
    /// Invokes <see cref="TruncatedSampler.LastAttemptCount"/>.
    /// </summary>
    int LastAttemptCount();

    /// <summary>
    /// Invokes <see cref="TruncatedSampler.TotalAttemptCount"/>.
    /// </summary>
    long TotalAttemptCount();

    /// <summary>
    /// Invokes <see cref="TruncatedSampler.Sample"/>.
    /// </summary>
    double Sample();

    /// <summary>
    /// Invokes <see cref="TruncatedSampler.Fill"/>.
    /// </summary>
    void Fill(Span<double> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="UniformSampler"/>.
/// </summary>
public interface IUniformSampler
{
    /// <summary>
    /// Invokes <see cref="UniformSampler.Sample"/>.
    /// </summary>
    double Sample();

    /// <summary>
    /// Invokes <see cref="UniformSampler.Fill"/>.
    /// </summary>
    void Fill(Span<double> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="VanDerCorputSequence"/>.
/// </summary>
public interface IVanDerCorputSequence
{
    /// <summary>
    /// Invokes <see cref="VanDerCorputSequence.Base"/>.
    /// </summary>
    int Base();

    /// <summary>
    /// Invokes <see cref="VanDerCorputSequence.Index"/>.
    /// </summary>
    ulong Index();

    /// <summary>
    /// Invokes <see cref="VanDerCorputSequence.Next"/>.
    /// </summary>
    double Next();

    /// <summary>
    /// Invokes <see cref="VanDerCorputSequence.Fill"/>.
    /// </summary>
    void Fill(Span<double> destination);

    /// <summary>
    /// Invokes <see cref="VanDerCorputSequence.Skip"/>.
    /// </summary>
    void Skip(ulong count);

    /// <summary>
    /// Invokes <see cref="VanDerCorputSequence.Reset"/>.
    /// </summary>
    void Reset();

}

/// <summary>
/// Defines the public operations provided by <see cref="VonMisesSampler"/>.
/// </summary>
public interface IVonMisesSampler
{
    /// <summary>
    /// Invokes <see cref="VonMisesSampler.Sample"/>.
    /// </summary>
    double Sample();

    /// <summary>
    /// Invokes <see cref="VonMisesSampler.Fill"/>.
    /// </summary>
    void Fill(Span<double> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="WeibullSampler"/>.
/// </summary>
public interface IWeibullSampler
{
    /// <summary>
    /// Invokes <see cref="WeibullSampler.Sample"/>.
    /// </summary>
    double Sample();

    /// <summary>
    /// Invokes <see cref="WeibullSampler.Fill"/>.
    /// </summary>
    void Fill(Span<double> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="WeightedSampler"/>.
/// </summary>
public interface IWeightedSampler
{
    /// <summary>
    /// Invokes <see cref="WeightedSampler.CategoryCount"/>.
    /// </summary>
    int CategoryCount();

    /// <summary>
    /// Invokes <see cref="WeightedSampler.Method"/>.
    /// </summary>
    WeightedSamplingMethod Method();

    /// <summary>
    /// Invokes <see cref="WeightedSampler.SampleIndex"/>.
    /// </summary>
    int SampleIndex();

    /// <summary>
    /// Invokes <see cref="WeightedSampler.FillIndices"/>.
    /// </summary>
    void FillIndices(Span<int> destination);

    /// <summary>
    /// Invokes <see cref="WeightedSampler.UpdateWeights"/>.
    /// </summary>
    void UpdateWeights(double[] weights);

}

/// <summary>
/// Defines the public operations provided by <see cref="WishartSampler"/>.
/// </summary>
public interface IWishartSampler
{
    /// <summary>
    /// Invokes <see cref="WishartSampler.Dimension"/>.
    /// </summary>
    int Dimension();

    /// <summary>
    /// Invokes <see cref="WishartSampler.RequiredOutputLength"/>.
    /// </summary>
    int RequiredOutputLength();

    /// <summary>
    /// Invokes <see cref="WishartSampler.RequiredBatchOutputLength"/>.
    /// </summary>
    int RequiredBatchOutputLength(int sampleCount);

    /// <summary>
    /// Invokes <see cref="WishartSampler.Sample"/>.
    /// </summary>
    double[] Sample();

    /// <summary>
    /// Invokes <see cref="WishartSampler.SampleInto"/>.
    /// </summary>
    void SampleInto(Span<double> destination);

    /// <summary>
    /// Invokes <see cref="WishartSampler.Fill"/>.
    /// </summary>
    void Fill(int sampleCount, Span<double> destination);

}

/// <summary>
/// Defines the public operations provided by <see cref="ZipfSampler"/>.
/// </summary>
public interface IZipfSampler
{
    /// <summary>
    /// Invokes <see cref="ZipfSampler.Sample"/>.
    /// </summary>
    long Sample();

    /// <summary>
    /// Invokes <see cref="ZipfSampler.Fill"/>.
    /// </summary>
    void Fill(Span<long> destination);

}
