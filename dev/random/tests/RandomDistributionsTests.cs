namespace random.Tests;

public class RandomDistributionsTests
{
    delegate double ScalarCall(RandomGenerator generator);
    delegate void FillCall(RandomGenerator generator, Span<double> destination);

    [Test]
    public void ScalarFacadeMethodsMatchReusableSamplers()
    {
        AssertScalar(generator => RandomDistributions.Uniform(generator, -2.0, 3.0), generator => new UniformSampler(generator, -2.0, 3.0).Sample());
        AssertScalar(generator => RandomDistributions.Normal(generator, -2.0, 3.0), generator => new NormalSampler(generator, -2.0, 3.0).Sample());
        AssertScalar(generator => RandomDistributions.LogNormal(generator, -0.5, 0.75), generator => new LogNormalSampler(generator, -0.5, 0.75).Sample());
        AssertScalar(generator => RandomDistributions.Exponential(generator, 2.5), generator => new ExponentialSampler(generator, 2.5).Sample());
        AssertScalar(generator => RandomDistributions.Gamma(generator, 1.7, 2.5), generator => new GammaSampler(generator, 1.7, 2.5).Sample());
        AssertScalar(generator => RandomDistributions.Erlang(generator, 3, 2.5), generator => new ErlangSampler(generator, 3, 2.5).Sample());
        AssertScalar(generator => RandomDistributions.Beta(generator, 1.7, 2.5), generator => new BetaSampler(generator, 1.7, 2.5).Sample());
        AssertScalar(generator => RandomDistributions.ScaledBeta(generator, 1.7, 2.5, -2.0, 3.0), generator => new BetaScaledSampler(generator, 1.7, 2.5, -2.0, 3.0).Sample());
        AssertScalar(generator => RandomDistributions.Chi(generator, 4.5), generator => new ChiSampler(generator, 4.5).Sample());
        AssertScalar(generator => RandomDistributions.ChiSquare(generator, 4.5), generator => new ChiSquareSampler(generator, 4.5).Sample());
        AssertScalar(generator => RandomDistributions.StudentT(generator, 4.5), generator => new StudentTSampler(generator, 4.5).Sample());
        AssertScalar(generator => RandomDistributions.FisherSnedecor(generator, 4.5, 9.0), generator => new FisherSnedecorSampler(generator, 4.5, 9.0).Sample());
        AssertScalar(generator => RandomDistributions.Cauchy(generator, -2.0, 3.0), generator => new CauchySampler(generator, -2.0, 3.0).Sample());
        AssertScalar(generator => RandomDistributions.Laplace(generator, -2.0, 3.0), generator => new LaplaceSampler(generator, -2.0, 3.0).Sample());
        AssertScalar(generator => RandomDistributions.Logistic(generator, -2.0, 3.0), generator => new LogisticSampler(generator, -2.0, 3.0).Sample());
        AssertScalar(generator => RandomDistributions.Gumbel(generator, -2.0, 3.0), generator => new GumbelSampler(generator, -2.0, 3.0).Sample());
        AssertScalar(generator => RandomDistributions.GeneralizedNormal(generator, -2.0, 3.0, 1.7), generator => new GeneralizedNormalSampler(generator, -2.0, 3.0, 1.7).Sample());
        AssertScalar(generator => RandomDistributions.SkewedGeneralizedError(generator, -2.0, 3.0, 1.7, 0.25), generator => new SkewedGeneralizedErrorSampler(generator, -2.0, 3.0, 1.7, 0.25).Sample());
        AssertScalar(generator => RandomDistributions.SkewedGeneralizedT(generator, -2.0, 3.0, 1.7, 2.5, 0.25), generator => new SkewedGeneralizedTSampler(generator, -2.0, 3.0, 1.7, 2.5, 0.25).Sample());
        AssertScalar(generator => RandomDistributions.Weibull(generator, -2.0, 3.0, 1.7), generator => new WeibullSampler(generator, -2.0, 3.0, 1.7).Sample());
        AssertScalar(generator => RandomDistributions.Rayleigh(generator, -2.0, 3.0), generator => new RayleighSampler(generator, -2.0, 3.0).Sample());
        AssertScalar(generator => RandomDistributions.Pareto(generator, 2.0, 1.7), generator => new ParetoSampler(generator, 2.0, 1.7).Sample());
        AssertScalar(generator => RandomDistributions.Lomax(generator, -2.0, 3.0, 1.7), generator => new LomaxSampler(generator, -2.0, 3.0, 1.7).Sample());
        AssertScalar(generator => RandomDistributions.Power(generator, -2.0, 3.0, 1.7), generator => new PowerSampler(generator, -2.0, 3.0, 1.7).Sample());
        AssertScalar(generator => RandomDistributions.Burr(generator, -2.0, 3.0, 1.7, 2.5), generator => new BurrSampler(generator, -2.0, 3.0, 1.7, 2.5).Sample());
        AssertScalar(generator => RandomDistributions.Levy(generator, -2.0, 3.0), generator => new LevySampler(generator, -2.0, 3.0).Sample());
        AssertScalar(generator => RandomDistributions.InverseGamma(generator, 1.7, 2.5), generator => new InverseGammaSampler(generator, 1.7, 2.5).Sample());
        AssertScalar(generator => RandomDistributions.InverseGaussian(generator, 1.7, 2.5), generator => new InverseGaussianSampler(generator, 1.7, 2.5).Sample());
        AssertScalar(generator => RandomDistributions.Triangular(generator, -2.0, 3.0, 0.5), generator => new TriangularSampler(generator, -2.0, 3.0, 0.5).Sample());
        AssertScalar(generator => RandomDistributions.VonMises(generator, 0.5, 2.5), generator => new VonMisesSampler(generator, 0.5, 2.5).Sample());
        AssertScalar(generator => RandomDistributions.Stable(generator, -2.0, 3.0, 1.35, 0.25), generator => new StableSampler(generator, -2.0, 3.0, 1.35, 0.25).Sample());
        AssertScalar(generator => RandomDistributions.TruncatedNormal(generator, -2.0, 3.0, -4.0, 2.0), generator => new TruncatedNormalSampler(generator, -2.0, 3.0, -4.0, 2.0).Sample());
        AssertScalar(generator => RandomDistributions.NoncentralChiSquare(generator, 4.5, 2.5), generator => new NoncentralChiSquareSampler(generator, 4.5, 2.5).Sample());
        AssertScalar(generator => RandomDistributions.NoncentralF(generator, 4.5, 9.0, 2.5), generator => new NoncentralFSampler(generator, 4.5, 9.0, 2.5).Sample());
    }

    [Test]
    public void EveryFillMatchesRepeatedScalarFacadeCalls()
    {
        AssertFill((generator, values) => RandomDistributions.FillUniform(generator, -2.0, 3.0, values), generator => RandomDistributions.Uniform(generator, -2.0, 3.0));
        AssertFill((generator, values) => RandomDistributions.FillNormal(generator, -2.0, 3.0, values), generator => RandomDistributions.Normal(generator, -2.0, 3.0));
        AssertFill((generator, values) => RandomDistributions.FillLogNormal(generator, -0.5, 0.75, values), generator => RandomDistributions.LogNormal(generator, -0.5, 0.75));
        AssertFill((generator, values) => RandomDistributions.FillExponential(generator, 2.5, values), generator => RandomDistributions.Exponential(generator, 2.5));
        AssertFill((generator, values) => RandomDistributions.FillGamma(generator, 1.7, 2.5, values), generator => RandomDistributions.Gamma(generator, 1.7, 2.5));
        AssertFill((generator, values) => RandomDistributions.FillErlang(generator, 3, 2.5, values), generator => RandomDistributions.Erlang(generator, 3, 2.5));
        AssertFill((generator, values) => RandomDistributions.FillBeta(generator, 1.7, 2.5, values), generator => RandomDistributions.Beta(generator, 1.7, 2.5));
        AssertFill((generator, values) => RandomDistributions.FillScaledBeta(generator, 1.7, 2.5, -2.0, 3.0, values), generator => RandomDistributions.ScaledBeta(generator, 1.7, 2.5, -2.0, 3.0));
        AssertFill((generator, values) => RandomDistributions.FillChi(generator, 4.5, values), generator => RandomDistributions.Chi(generator, 4.5));
        AssertFill((generator, values) => RandomDistributions.FillChiSquare(generator, 4.5, values), generator => RandomDistributions.ChiSquare(generator, 4.5));
        AssertFill((generator, values) => RandomDistributions.FillStudentT(generator, 4.5, values), generator => RandomDistributions.StudentT(generator, 4.5));
        AssertFill((generator, values) => RandomDistributions.FillFisherSnedecor(generator, 4.5, 9.0, values), generator => RandomDistributions.FisherSnedecor(generator, 4.5, 9.0));
        AssertFill((generator, values) => RandomDistributions.FillCauchy(generator, -2.0, 3.0, values), generator => RandomDistributions.Cauchy(generator, -2.0, 3.0));
        AssertFill((generator, values) => RandomDistributions.FillLaplace(generator, -2.0, 3.0, values), generator => RandomDistributions.Laplace(generator, -2.0, 3.0));
        AssertFill((generator, values) => RandomDistributions.FillLogistic(generator, -2.0, 3.0, values), generator => RandomDistributions.Logistic(generator, -2.0, 3.0));
        AssertFill((generator, values) => RandomDistributions.FillGumbel(generator, -2.0, 3.0, values), generator => RandomDistributions.Gumbel(generator, -2.0, 3.0));
        AssertFill((generator, values) => RandomDistributions.FillGeneralizedNormal(generator, -2.0, 3.0, 1.7, values), generator => RandomDistributions.GeneralizedNormal(generator, -2.0, 3.0, 1.7));
        AssertFill((generator, values) => RandomDistributions.FillSkewedGeneralizedError(generator, -2.0, 3.0, 1.7, 0.25, values), generator => RandomDistributions.SkewedGeneralizedError(generator, -2.0, 3.0, 1.7, 0.25));
        AssertFill((generator, values) => RandomDistributions.FillSkewedGeneralizedT(generator, -2.0, 3.0, 1.7, 2.5, 0.25, values), generator => RandomDistributions.SkewedGeneralizedT(generator, -2.0, 3.0, 1.7, 2.5, 0.25));
        AssertFill((generator, values) => RandomDistributions.FillWeibull(generator, -2.0, 3.0, 1.7, values), generator => RandomDistributions.Weibull(generator, -2.0, 3.0, 1.7));
        AssertFill((generator, values) => RandomDistributions.FillRayleigh(generator, -2.0, 3.0, values), generator => RandomDistributions.Rayleigh(generator, -2.0, 3.0));
        AssertFill((generator, values) => RandomDistributions.FillPareto(generator, 2.0, 1.7, values), generator => RandomDistributions.Pareto(generator, 2.0, 1.7));
        AssertFill((generator, values) => RandomDistributions.FillLomax(generator, -2.0, 3.0, 1.7, values), generator => RandomDistributions.Lomax(generator, -2.0, 3.0, 1.7));
        AssertFill((generator, values) => RandomDistributions.FillPower(generator, -2.0, 3.0, 1.7, values), generator => RandomDistributions.Power(generator, -2.0, 3.0, 1.7));
        AssertFill((generator, values) => RandomDistributions.FillBurr(generator, -2.0, 3.0, 1.7, 2.5, values), generator => RandomDistributions.Burr(generator, -2.0, 3.0, 1.7, 2.5));
        AssertFill((generator, values) => RandomDistributions.FillLevy(generator, -2.0, 3.0, values), generator => RandomDistributions.Levy(generator, -2.0, 3.0));
        AssertFill((generator, values) => RandomDistributions.FillInverseGamma(generator, 1.7, 2.5, values), generator => RandomDistributions.InverseGamma(generator, 1.7, 2.5));
        AssertFill((generator, values) => RandomDistributions.FillInverseGaussian(generator, 1.7, 2.5, values), generator => RandomDistributions.InverseGaussian(generator, 1.7, 2.5));
        AssertFill((generator, values) => RandomDistributions.FillTriangular(generator, -2.0, 3.0, 0.5, values), generator => RandomDistributions.Triangular(generator, -2.0, 3.0, 0.5));
        AssertFill((generator, values) => RandomDistributions.FillVonMises(generator, 0.5, 2.5, values), generator => RandomDistributions.VonMises(generator, 0.5, 2.5));
        AssertFill((generator, values) => RandomDistributions.FillStable(generator, -2.0, 3.0, 1.35, 0.25, values), generator => RandomDistributions.Stable(generator, -2.0, 3.0, 1.35, 0.25));
        AssertFill((generator, values) => RandomDistributions.FillTruncatedNormal(generator, -2.0, 3.0, -4.0, 2.0, values), generator => RandomDistributions.TruncatedNormal(generator, -2.0, 3.0, -4.0, 2.0));
        AssertFill((generator, values) => RandomDistributions.FillNoncentralChiSquare(generator, 4.5, 2.5, values), generator => RandomDistributions.NoncentralChiSquare(generator, 4.5, 2.5));
        AssertFill((generator, values) => RandomDistributions.FillNoncentralF(generator, 4.5, 9.0, 2.5, values), generator => RandomDistributions.NoncentralF(generator, 4.5, 9.0, 2.5));
    }

    [Test]
    public void FacadeForwardsValidationWithoutConsumingGeneratorState()
    {
        RandomGenerator generator = Create(91UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<RandomArgumentException>(() => RandomDistributions.Normal(NullReference.Value<global::random.IRandomSource>(), 0.0, 1.0))?.ArgumentName(), Is.EqualTo("generator"));
            Assert.That(Assert.Throws<RandomArgumentException>(() => RandomDistributions.Gamma(generator, 0.0, 1.0))?.ArgumentName(), Is.EqualTo("shape"));
            Assert.That(Assert.Throws<RandomArgumentException>(() => RandomDistributions.FillStable(generator, 0.0, 1.0, 1.0, 2.0, Span<double>.Empty))?.ArgumentName(), Is.EqualTo("skewness"));
            Assert.That(Assert.Throws<RandomArgumentException>(() => RandomDistributions.TruncatedNormal(generator, 0.0, 1.0, 2.0, 1.0))?.ArgumentName(), Is.EqualTo("upperBound"));
        });

        Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
    }

    static void AssertScalar(ScalarCall facade, ScalarCall sampler)
    {
        RandomGenerator firstGenerator = Create(41UL);
        RandomGenerator secondGenerator = Create(41UL);
        for (int index = 0; index < 32; index++)
        {
            Assert.That(Bits(facade(firstGenerator)), Is.EqualTo(Bits(sampler(secondGenerator))));
        }

        Assert.That(firstGenerator.ExportState().ToBytes(), Is.EqualTo(secondGenerator.ExportState().ToBytes()));
    }

    static void AssertFill(FillCall fill, ScalarCall scalar)
    {
        RandomGenerator fillGenerator = Create(51UL);
        RandomGenerator scalarGenerator = Create(51UL);
        double[] actual = new double[65];
        double[] expected = new double[actual.Length];
        byte[] before = fillGenerator.ExportState().ToBytes();

        fill(fillGenerator, Span<double>.Empty);
        Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(before));
        fill(fillGenerator, actual);
        for (int index = 0; index < expected.Length; index++)
        {
            expected[index] = scalar(scalarGenerator);
        }

        for (int index = 0; index < actual.Length; index++)
        {
            Assert.That(Bits(actual[index]), Is.EqualTo(Bits(expected[index])));
        }

        Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(scalarGenerator.ExportState().ToBytes()));
    }

    static ulong Bits(double value)
    {
        return PortableMath.DoubleBits(value);
    }

    static RandomGenerator Create(ulong seed)
    {
        return RandomGenerator.CreateSplitMix64(seed, RandomMathMode.Portable);
    }
}
