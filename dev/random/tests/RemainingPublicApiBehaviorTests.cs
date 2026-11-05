namespace random.Tests;

public class RemainingPublicApiBehaviorTests
{
    [Test]
    public void RemainingFactoryPathsExposeCanonicalRuntimeBehavior()
    {
        IRandomFactory factory = new RandomFactory();
        ISecureRandomGenerator secure = factory.CreateHmacDrbg();
        ulong initialCounter = secure.ReseedCounter();
        byte[] output = new byte[17];
        secure.Generate(output);

        ISeedSequence firstSeed = factory.CreateSeedFromSystemEntropy();
        ISeedSequence secondSeed = factory.CreateSeedFromSystemEntropy();
        byte[] firstOutput = firstSeed.GenerateBytes(32);

        Assert.Multiple(() =>
        {
            Assert.That(secure.Info().Algorithm(), Is.EqualTo(RandomAlgorithm.HmacDrbgSha256V1));
            Assert.That(secure.Info().IsCryptographic(), Is.True);
            Assert.That(initialCounter, Is.EqualTo(1UL));
            Assert.That(secure.ReseedCounter(), Is.EqualTo(2UL));
            Assert.That(output, Has.Length.EqualTo(17));
            Assert.That(firstSeed.Path(), Is.Empty);
            Assert.That(firstSeed.Fingerprint(), Is.Not.EqualTo(secondSeed.Fingerprint()));
            Assert.That(firstSeed.GenerateBytes(32), Is.EqualTo(firstOutput));
            Assert.That(factory.TokenEntropyBits(0), Is.Zero);
            Assert.That(factory.TokenEntropyBits(32), Is.EqualTo(256L));
            Assert.That(factory.TokenEntropyBits(int.MaxValue), Is.EqualTo(17179869176L));
            Assert.That(factory.StringEntropyBits("01", 128), Is.EqualTo(128.0));
            Assert.That(factory.StringEntropyBits("0123456789ABCDEF", 20), Is.EqualTo(80.0));
            Assert.That(factory.StringEntropyBits("abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", 20),
                Is.EqualTo(20.0 * Math.Log2(62.0)).Within(1.0e-12));
        });

        AssertArgument("byteCount", () => factory.TokenEntropyBits(-1));
        AssertArgument("alphabet", () => factory.StringEntropyBits(NullReference.Value<string>(), 1));
        AssertArgument("alphabet", () => factory.StringEntropyBits("A", 1));
        AssertArgument("alphabet", () => factory.StringEntropyBits("AAB", 1));
        AssertArgument("length", () => factory.StringEntropyBits("AB", -1));
    }

    [Test]
    public void PublicLimitsAreStableAndControlSamplerBoundaries()
    {
        Assert.Multiple(() =>
        {
            Assert.That(PoissonLimits.MaximumMean, Is.EqualTo(9.2233720064847708e18));
            Assert.That(ConwayMaxwellPoissonLimits.MaximumNormalizationTerms, Is.EqualTo(100001));
            Assert.That(ConwayMaxwellPoissonLimits.NormalizationRelativeTolerance, Is.EqualTo(1.0e-13));
            Assert.That(PoissonLimits.MaximumMean, Is.EqualTo(PoissonSampler.MaximumMean));
            Assert.That(ConwayMaxwellPoissonLimits.MaximumNormalizationTerms, Is.EqualTo(ConwayMaxwellPoissonSampler.MaximumNormalizationTerms));
            Assert.That(ConwayMaxwellPoissonLimits.NormalizationRelativeTolerance, Is.EqualTo(ConwayMaxwellPoissonSampler.NormalizationRelativeTolerance));
            Assert.DoesNotThrow(() => new PoissonSampler(Create(201UL), PoissonLimits.MaximumMean));
        });
        AssertArgument("mean", () => new PoissonSampler(Create(202UL), Math.BitIncrement(PoissonLimits.MaximumMean)));

        ConwayMaxwellPoissonSampler sampler = new ConwayMaxwellPoissonSampler(Create(203UL), 2.0, 1.0);
        Assert.Multiple(() =>
        {
            Assert.That(sampler.NormalizationTermCount(), Is.GreaterThan(0).And.LessThanOrEqualTo(ConwayMaxwellPoissonLimits.MaximumNormalizationTerms));
            Assert.That(sampler.NormalizationRelativeErrorBound(), Is.LessThanOrEqualTo(ConwayMaxwellPoissonLimits.NormalizationRelativeTolerance));
            Assert.That(sampler.NormalizationConverged(), Is.True);
        });
    }

    [Test]
    public void ExceptionConstructorsAcceptAbsentCausesAndRetainDiagnostics()
    {
        RandomArgumentException argument = new RandomArgumentException("count", "invalid count", null);
        RandomCapabilityException capability = new RandomCapabilityException(
            RandomAlgorithm.Mt19937V1,
            RandomCapability.Seek,
            "seek is unsupported",
            null);
        Exception[] dedicated =
        [
            new RandomStateException("state", null),
            new RandomExhaustedException("exhausted", null),
            new RandomReseedRequiredException("reseed", null),
            new RandomSecurityException("security", null)
        ];

        Assert.Multiple(() =>
        {
            Assert.That(argument.GetType(), Is.EqualTo(typeof(RandomArgumentException)));
            Assert.That(argument.ArgumentName(), Is.EqualTo("count"));
            Assert.That(argument.Message, Is.EqualTo("invalid count"));
            Assert.That(argument.InnerException, Is.Null);
            Assert.That(capability.GetType(), Is.EqualTo(typeof(RandomCapabilityException)));
            Assert.That(capability.Algorithm(), Is.EqualTo(RandomAlgorithm.Mt19937V1));
            Assert.That(capability.Capability(), Is.EqualTo(RandomCapability.Seek));
            Assert.That(capability.Message, Is.EqualTo("seek is unsupported"));
            Assert.That(capability.InnerException, Is.Null);
            Assert.That(dedicated.Select(error => error.GetType()), Is.EqualTo(new Type[]
            {
                typeof(RandomStateException),
                typeof(RandomExhaustedException),
                typeof(RandomReseedRequiredException),
                typeof(RandomSecurityException)
            }));
            Assert.That(dedicated.Select(error => error.Message), Is.EqualTo(new[] { "state", "exhausted", "reseed", "security" }));
            Assert.That(dedicated, Has.All.Property("InnerException").Null);
        });
    }

    [Test]
    public void CoreInterfacesPreserveConcreteReplayAndDefensiveBoundaries()
    {
        IRandomFactory factory = new RandomFactory();
        ulong[] suppliedPath = [3UL, 5UL, 8UL];
        SeedSequence concreteSeed = factory.CreateSeedFromText("interface replay").SpawnPath(suppliedPath);
        ISeedSequence seed = concreteSeed;
        suppliedPath[0] = 99UL;
        ulong[] returnedPath = seed.Path();
        returnedPath[1] = 99UL;

        RandomGenerator concreteGenerator = factory.CreateGeneratorFromSeedSequence(RandomAlgorithm.SplitMix64V1, concreteSeed, RandomMathMode.Portable);
        RandomGenerator reference = factory.CreateGeneratorFromSeedSequence(RandomAlgorithm.SplitMix64V1, concreteSeed, RandomMathMode.Portable);
        IRandomGenerator generator = concreteGenerator;
        IRandomSource source = concreteGenerator;
        IRandomAlgorithmInfo info = generator.Info();
        RandomCapability[] returnedCapabilities = info.Capabilities();
        RandomCapability firstCapability = returnedCapabilities[0];
        returnedCapabilities[0] = RandomCapability.None;

        ulong firstActual = source.NextUInt64();
        ulong firstExpected = reference.NextUInt64();
        RandomState concreteState = generator.ExportState();
        IRandomState state = concreteState;
        byte[] stateBytes = state.ToBytes();
        byte[] changedStateBytes = state.ToBytes();
        changedStateBytes[0] ^= 0xFF;
        RandomGenerator clone = generator.Clone();
        RandomGenerator restored = factory.RestoreGenerator(concreteState, RandomMathMode.Portable);
        ulong secondExpected = reference.NextUInt64();

        Assert.Multiple(() =>
        {
            Assert.That(seed.Path(), Is.EqualTo(new ulong[] { 3UL, 5UL, 8UL }));
            Assert.That(firstActual, Is.EqualTo(firstExpected));
            Assert.That(info.Algorithm(), Is.EqualTo(RandomAlgorithm.SplitMix64V1));
            Assert.That(info.HasCapability(firstCapability), Is.True);
            Assert.That(info.Capabilities()[0], Is.EqualTo(firstCapability));
            Assert.That(state.Algorithm(), Is.EqualTo(RandomAlgorithm.SplitMix64V1));
            Assert.That(state.AlgorithmVersion(), Is.EqualTo(info.Version()));
            Assert.That(state.TransformVersion(), Is.EqualTo(info.TransformVersion()));
            Assert.That(state.ToBytes(), Is.EqualTo(stateBytes));
            Assert.That(Convert.FromBase64String(state.ToBase64()), Is.EqualTo(stateBytes));
            Assert.That(clone.NextUInt64(), Is.EqualTo(secondExpected));
            Assert.That(restored.NextUInt64(), Is.EqualTo(secondExpected));
            Assert.That(source.NextUInt64(), Is.EqualTo(secondExpected));
        });
    }

    static RandomGenerator Create(ulong seed)
    {
        return RandomGenerator.CreateSplitMix64(seed, RandomMathMode.Portable);
    }

    static void AssertArgument(string expectedName, TestDelegate action)
    {
        RandomArgumentException error = Assert.Throws<RandomArgumentException>(action);
        Assert.That(error.ArgumentName(), Is.EqualTo(expectedName));
    }
}
