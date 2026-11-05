namespace random.Tests;

public class ContinuousConstructionPathTests
{
    [Test]
    public void StandardLocationScalePathsMatchCanonicalSamplersExactly()
    {
        IRandomFactory factory = new RandomFactory();

        AssertEquivalent(
            generator => factory.CreateStandardBurrSampler(generator, 1.7, 2.3).Sample,
            generator => new BurrSampler(generator, 0.0, 1.0, 1.7, 2.3).Sample,
            101UL);
        AssertEquivalent(
            generator => factory.CreateStandardCauchySampler(generator).Sample,
            generator => new CauchySampler(generator, 0.0, 1.0).Sample,
            102UL);
        AssertEquivalent(
            generator => factory.CreateStandardGeneralizedNormalSampler(generator, 1.7).Sample,
            generator => new GeneralizedNormalSampler(generator, 0.0, 1.0, 1.7).Sample,
            103UL);
        AssertEquivalent(
            generator => factory.CreateStandardGumbelSampler(generator).Sample,
            generator => new GumbelSampler(generator, 0.0, 1.0).Sample,
            104UL);
        AssertEquivalent(
            generator => factory.CreateStandardLaplaceSampler(generator).Sample,
            generator => new LaplaceSampler(generator, 0.0, 1.0).Sample,
            105UL);
        AssertEquivalent(
            generator => factory.CreateStandardLevySampler(generator).Sample,
            generator => new LevySampler(generator, 0.0, 1.0).Sample,
            106UL);
        AssertEquivalent(
            generator => factory.CreateStandardLogNormalSampler(generator).Sample,
            generator => new LogNormalSampler(generator, 0.0, 1.0).Sample,
            107UL);
        AssertEquivalent(
            generator => factory.CreateStandardLogisticSampler(generator).Sample,
            generator => new LogisticSampler(generator, 0.0, 1.0).Sample,
            108UL);
        AssertEquivalent(
            generator => factory.CreateStandardNormalSampler(generator).Sample,
            generator => new NormalSampler(generator, 0.0, 1.0).Sample,
            109UL);
        AssertEquivalent(
            generator => factory.CreateStandardPowerSampler(generator, 1.7).Sample,
            generator => new PowerSampler(generator, 0.0, 1.0, 1.7).Sample,
            110UL);
        AssertEquivalent(
            generator => factory.CreateStandardRayleighSampler(generator).Sample,
            generator => new RayleighSampler(generator, 0.0, 1.0).Sample,
            111UL);
        AssertEquivalent(
            generator => factory.CreateStandardSkewedGeneralizedErrorSampler(generator, 1.7, 0.8).Sample,
            generator => new SkewedGeneralizedErrorSampler(generator, 0.0, 1.0, 1.7, 0.8).Sample,
            112UL);
        AssertEquivalent(
            generator => factory.CreateStandardSkewedGeneralizedTSampler(generator, 1.3, 4.0, 0.8).Sample,
            generator => new SkewedGeneralizedTSampler(generator, 0.0, 1.0, 1.3, 4.0, 0.8).Sample,
            113UL);
        AssertEquivalent(
            generator => factory.CreateStandardTriangularSampler(generator).Sample,
            generator => new TriangularSampler(generator, 0.0, 1.0, 0.5).Sample,
            114UL);
        AssertEquivalent(
            generator => factory.CreateStandardWeibullSampler(generator, 1.7).Sample,
            generator => new WeibullSampler(generator, 0.0, 1.0, 1.7).Sample,
            115UL);
    }

    [Test]
    public void UnitScaleAndSpecialStandardPathsMatchCanonicalSamplersExactly()
    {
        IRandomFactory factory = new RandomFactory();

        AssertEquivalent(
            generator => factory.CreateUnitScaleErlangSampler(generator, 4).Sample,
            generator => new ErlangSampler(generator, 4, 1.0).Sample,
            201UL);
        AssertEquivalent(
            generator => factory.CreateStandardExponentialSampler(generator).Sample,
            generator => new ExponentialSampler(generator, 1.0).Sample,
            202UL);
        AssertEquivalent(
            generator => factory.CreateUnitScaleGammaSampler(generator, 2.75).Sample,
            generator => new GammaSampler(generator, 2.75, 1.0).Sample,
            203UL);
        AssertEquivalent(
            generator => factory.CreateUnitScaleInverseGammaSampler(generator, 2.75).Sample,
            generator => new InverseGammaSampler(generator, 2.75, 1.0).Sample,
            204UL);
        AssertEquivalent(
            generator => factory.CreateStandardInverseGaussianSampler(generator).Sample,
            generator => new InverseGaussianSampler(generator, 1.0, 1.0).Sample,
            205UL);
        AssertEquivalent(
            generator => factory.CreateStandardLomaxSampler(generator, 1.7).Sample,
            generator => new LomaxSampler(generator, 0.0, 1.0, 1.7).Sample,
            206UL);
        AssertEquivalent(
            generator => factory.CreateUnshiftedLomaxSampler(generator, 2.5, 1.7).Sample,
            generator => new LomaxSampler(generator, 0.0, 2.5, 1.7).Sample,
            207UL);
        AssertEquivalent(
            generator => factory.CreateStandardParetoSampler(generator, 1.7).Sample,
            generator => new ParetoSampler(generator, 1.0, 1.7).Sample,
            208UL);
        AssertEquivalent(
            generator => factory.CreateStandardTruncatedNormalSampler(generator, -1.5, 2.25).Sample,
            generator => new TruncatedNormalSampler(generator, 0.0, 1.0, -1.5, 2.25).Sample,
            209UL);
        AssertEquivalent(
            generator => factory.CreateZeroMeanVonMisesSampler(generator, 2.5).Sample,
            generator => new VonMisesSampler(generator, 0.0, 2.5).Sample,
            210UL);
    }

    [Test]
    public void StableNamedPathsMatchTheirExplicitS0ParametersExactly()
    {
        IRandomFactory factory = new RandomFactory();

        AssertEquivalent(
            generator => factory.CreateStandardSymmetricStableSampler(generator, 1.35).Sample,
            generator => new StableSampler(generator, 0.0, 1.0, 1.35, 0.0).Sample,
            301UL);
        AssertEquivalent(
            generator => factory.CreateStandardStableSampler(generator, 1.35, -0.4).Sample,
            generator => new StableSampler(generator, 0.0, 1.0, 1.35, -0.4).Sample,
            302UL);
        AssertEquivalent(
            generator => factory.CreateLocationScaleSymmetricStableSampler(generator, -2.0, 3.0, 1.35).Sample,
            generator => new StableSampler(generator, -2.0, 3.0, 1.35, 0.0).Sample,
            303UL);
    }

    [Test]
    public void NamedPathsPreserveCanonicalValidationAndGeneratorState()
    {
        IRandomFactory factory = new RandomFactory();
        AssertArgument("generator", () => factory.CreateStandardNormalSampler(NullReference.Value<global::random.IRandomSource>()));

        RandomGenerator generator = Create(401UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();

        AssertArgument("firstShape", () => factory.CreateStandardBurrSampler(generator, -1.0, 2.0));
        AssertArgument("shape", () => factory.CreateUnitScaleErlangSampler(generator, 0));
        AssertArgument("scale", () => factory.CreateUnshiftedLomaxSampler(generator, -1.0, 2.0));
        AssertArgument("skewness", () => factory.CreateStandardStableSampler(generator, 1.0, 2.0));
        AssertArgument("upperBound", () => factory.CreateStandardTruncatedNormalSampler(generator, 2.0, 1.0));
        AssertArgument("concentration", () => factory.CreateZeroMeanVonMisesSampler(generator, -1.0));

        Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
    }

    static void AssertEquivalent(Func<IRandomSource, Func<double>> namedPath, Func<IRandomSource, Func<double>> canonicalPath, ulong seed)
    {
        RandomGenerator namedGenerator = Create(seed);
        RandomGenerator canonicalGenerator = Create(seed);
        Func<double> namedSample = namedPath(namedGenerator);
        Func<double> canonicalSample = canonicalPath(canonicalGenerator);

        int index = 0;
        while (index < 32)
        {
            Assert.That(PortableMath.DoubleBits(namedSample()), Is.EqualTo(PortableMath.DoubleBits(canonicalSample())));
            index++;
        }

        Assert.That(namedGenerator.ExportState().ToBytes(), Is.EqualTo(canonicalGenerator.ExportState().ToBytes()));
    }

    static RandomGenerator Create(ulong seed)
    {
        return RandomGenerator.CreateSplitMix64(seed, RandomMathMode.Portable);
    }

    static void AssertArgument(string argumentName, TestDelegate action)
    {
        RandomArgumentException error = Assert.Throws<RandomArgumentException>(action);
        Assert.That(error.ArgumentName(), Is.EqualTo(argumentName));
    }
}
