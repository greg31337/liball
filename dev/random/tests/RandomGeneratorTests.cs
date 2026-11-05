namespace random.Tests;

public class RandomGeneratorTests
{
    [Test]
    public void DefaultFactorySelectsPcg64DxsmWithExplicitMathMode()
    {
        RandomGenerator generator = RandomGenerator.CreateDefault(42UL, RandomMathMode.Portable);
        Pcg64DxsmEngine direct = Pcg64DxsmEngine.FromSeed((UInt128)42, (UInt128)0);

        Assert.Multiple(() =>
        {
            Assert.That(generator.Info().Algorithm(), Is.EqualTo(RandomAlgorithm.Pcg64DxsmV1));
            Assert.That(generator.MathMode(), Is.EqualTo(RandomMathMode.Portable));
            Assert.That(generator.NextRawWord(), Is.EqualTo(direct.NextUInt64()));
        });
    }

    [Test]
    public void NamedFactoriesSelectEveryDeterministicAlgorithm()
    {
        RandomGenerator[] generators =
        [
            RandomGenerator.CreatePcg64Dxsm((UInt128)1, (UInt128)2, RandomMathMode.System),
            RandomGenerator.CreateXoshiro256StarStar(3UL, RandomMathMode.System),
            RandomGenerator.CreatePhilox4x64Ten((UInt128)4, RandomMathMode.System),
            RandomGenerator.CreateMt19937(5U, RandomMathMode.System),
            RandomGenerator.CreateMt19937Numpy(6U, RandomMathMode.System),
            RandomGenerator.CreateSplitMix64(7UL, RandomMathMode.System)
        ];
        RandomAlgorithm[] expected =
        [
            RandomAlgorithm.Pcg64DxsmV1,
            RandomAlgorithm.Xoshiro256StarStarV1,
            RandomAlgorithm.Philox4x64TenV1,
            RandomAlgorithm.Mt19937V1,
            RandomAlgorithm.Mt19937NumpyV1,
            RandomAlgorithm.SplitMix64V1
        ];

        for (int index = 0; index < generators.Length; index++)
        {
            Assert.Multiple(() =>
            {
                Assert.That(generators[index].Info().Algorithm(), Is.EqualTo(expected[index]));
                Assert.That(generators[index].MathMode(), Is.EqualTo(RandomMathMode.System));
                Assert.That(generators[index].Info().IsCryptographic(), Is.False);
            });
        }
    }

    [Test]
    public void AlgorithmSpecificFactoryInputsMapDirectlyToEngines()
    {
        RandomGenerator pcg = RandomGenerator.CreatePcg64Dxsm((UInt128)11, (UInt128)12, RandomMathMode.Portable);
        Pcg64DxsmEngine directPcg = Pcg64DxsmEngine.FromSeed((UInt128)11, (UInt128)12);
        RandomGenerator philox = RandomGenerator.CreatePhilox4x64Ten([13UL, 14UL], [15UL, 16UL, 17UL, 18UL], RandomMathMode.Portable);
        Philox4x64TenEngine directPhilox = Philox4x64TenEngine.FromKeyAndCounter([13UL, 14UL], [15UL, 16UL, 17UL, 18UL]);
        RandomGenerator referenceMt = RandomGenerator.CreateMt19937([0x123U, 0x234U, 0x345U, 0x456U], RandomMathMode.Portable);
        RandomGenerator numpyMt = RandomGenerator.CreateMt19937Numpy([0x123U, 0x234U, 0x345U, 0x456U], RandomMathMode.Portable);

        Assert.Multiple(() =>
        {
            Assert.That(pcg.NextRawWord(), Is.EqualTo(directPcg.NextUInt64()));
            Assert.That(philox.NextRawWord(), Is.EqualTo(directPhilox.NextUInt64()));
            Assert.That(referenceMt.NextRawWord(), Is.EqualTo(1067595299UL));
            Assert.That(numpyMt.NextRawWord(), Is.EqualTo(0x2238C27FUL));
        });
    }

    [Test]
    public void ResetReplaysTheFactoryInitialStateForEveryEngine()
    {
        RandomGenerator[] generators = CreateGenerators();
        foreach (RandomGenerator generator in generators)
        {
            ulong first = generator.NextRawWord();
            generator.NextRawWord();
            generator.NextRawWord();
            generator.Reset();

            Assert.That(generator.NextRawWord(), Is.EqualTo(first));
        }
    }

    [Test]
    public void CloneCopiesCurrentPositionMathModeAndOriginalResetPoint()
    {
        RandomGenerator original = RandomGenerator.CreateXoshiro256StarStar(123456789UL, RandomMathMode.System);
        ulong initial = original.NextRawWord();
        original.NextRawWord();
        RandomGenerator clone = original.Clone();
        ulong expected = original.NextRawWord();

        Assert.Multiple(() =>
        {
            Assert.That(clone.MathMode(), Is.EqualTo(RandomMathMode.System));
            Assert.That(clone.Info().Algorithm(), Is.EqualTo(RandomAlgorithm.Xoshiro256StarStarV1));
            Assert.That(clone.NextRawWord(), Is.EqualTo(expected));
        });

        clone.NextRawWord();
        original.Reset();
        clone.Reset();
        Assert.Multiple(() =>
        {
            Assert.That(original.NextRawWord(), Is.EqualTo(initial));
            Assert.That(clone.NextRawWord(), Is.EqualTo(initial));
        });
    }

    [Test]
    public void StateImportResumesAndUsesImportedPositionAsResetPoint()
    {
        SplitMix64Engine source = SplitMix64Engine.FromSeed(987654321UL);
        source.NextUInt64();
        source.NextUInt64();
        RandomState importedState = source.ExportState();
        SplitMix64Engine expected = SplitMix64Engine.FromState(importedState);
        ulong first = expected.NextUInt64();
        RandomGenerator generator = RandomGenerator.FromState(importedState, RandomMathMode.Portable);

        Assert.That(generator.NextRawWord(), Is.EqualTo(first));
        generator.NextRawWord();
        generator.Reset();
        Assert.Multiple(() =>
        {
            Assert.That(generator.Info().Algorithm(), Is.EqualTo(RandomAlgorithm.SplitMix64V1));
            Assert.That(generator.MathMode(), Is.EqualTo(RandomMathMode.Portable));
            Assert.That(generator.NextRawWord(), Is.EqualTo(first));
        });
    }

    [Test]
    public void GeneratorMetadataAddsResetWithoutChangingEngineMetadata()
    {
        RandomGenerator generator = RandomGenerator.CreatePcg64Dxsm((UInt128)1, (UInt128)2, RandomMathMode.Portable);
        RandomAlgorithmInfo generatorInfo = generator.Info();
        RandomAlgorithmInfo engineInfo = Pcg64DxsmEngine.FromSeed((UInt128)1, (UInt128)2).Info();

        Assert.Multiple(() =>
        {
            Assert.That(generatorInfo.Capabilities(), Is.EqualTo(new[]
            {
                RandomCapability.State,
                RandomCapability.Clone,
                RandomCapability.Advance,
                RandomCapability.Reset,
                RandomCapability.Spawn
            }));
            Assert.That(generatorInfo.HasCapability(RandomCapability.Reset), Is.True);
            Assert.That(generatorInfo.HasCapability(RandomCapability.Spawn), Is.True);
            Assert.That(engineInfo.HasCapability(RandomCapability.Reset), Is.False);
        });
    }

    [Test]
    public void FactoriesDefensivelyCopyArrayInputs()
    {
        uint[] referenceSeed = [0x123U, 0x234U, 0x345U, 0x456U];
        uint[] numpySeed = [0x123U, 0x234U, 0x345U, 0x456U];
        ulong[] key = [1UL, 2UL];
        ulong[] counter = [3UL, 4UL, 5UL, 6UL];
        RandomGenerator reference = RandomGenerator.CreateMt19937(referenceSeed, RandomMathMode.Portable);
        RandomGenerator numpy = RandomGenerator.CreateMt19937Numpy(numpySeed, RandomMathMode.Portable);
        RandomGenerator philox = RandomGenerator.CreatePhilox4x64Ten(key, counter, RandomMathMode.Portable);
        referenceSeed[0] = 0U;
        numpySeed[0] = 0U;
        key[0] = 0UL;
        counter[0] = 0UL;

        Assert.Multiple(() =>
        {
            Assert.That(reference.NextRawWord(), Is.EqualTo(1067595299UL));
            Assert.That(numpy.NextRawWord(), Is.EqualTo(0x2238C27FUL));
            Assert.That(philox.NextRawWord(), Is.EqualTo(Philox4x64TenEngine.FromKeyAndCounter([1UL, 2UL], [3UL, 4UL, 5UL, 6UL]).NextUInt64()));
        });
    }

    [Test]
    public void InvalidMathModeIsRejectedBeforeSeedValidation()
    {
        RandomMathMode invalid = (RandomMathMode)99;

        Assert.Multiple(() =>
        {
            RandomArgumentException defaultError = ReflectionInfo.Required<RandomArgumentException>(Assert.Throws<RandomArgumentException>(() => RandomGenerator.CreateDefault(0UL, invalid)));
            RandomArgumentException mtError = ReflectionInfo.Required<RandomArgumentException>(Assert.Throws<RandomArgumentException>(() => RandomGenerator.CreateMt19937((uint[])NullReference.Value<uint[]>(), invalid)));
            RandomArgumentException philoxError = ReflectionInfo.Required<RandomArgumentException>(Assert.Throws<RandomArgumentException>(() => RandomGenerator.CreatePhilox4x64Ten((ulong[])NullReference.Value<ulong[]>(), NullReference.Value<ulong[]>(), invalid)));
            RandomArgumentException stateError = ReflectionInfo.Required<RandomArgumentException>(Assert.Throws<RandomArgumentException>(() => RandomGenerator.FromState(NullReference.Value<global::random.RandomState>(), invalid)));
            Assert.That(defaultError.ArgumentName(), Is.EqualTo("mathMode"));
            Assert.That(mtError.ArgumentName(), Is.EqualTo("mathMode"));
            Assert.That(philoxError.ArgumentName(), Is.EqualTo("mathMode"));
            Assert.That(stateError.ArgumentName(), Is.EqualTo("mathMode"));
        });
    }

    [Test]
    public void NullInputsUseTheirPublicArgumentNames()
    {
        Assert.Multiple(() =>
        {
            RandomArgumentException mtError = ReflectionInfo.Required<RandomArgumentException>(Assert.Throws<RandomArgumentException>(() => RandomGenerator.CreateMt19937((uint[])NullReference.Value<uint[]>(), RandomMathMode.Portable)));
            RandomArgumentException numpyError = ReflectionInfo.Required<RandomArgumentException>(Assert.Throws<RandomArgumentException>(() => RandomGenerator.CreateMt19937Numpy((uint[])NullReference.Value<uint[]>(), RandomMathMode.Portable)));
            RandomArgumentException stateError = ReflectionInfo.Required<RandomArgumentException>(Assert.Throws<RandomArgumentException>(() => RandomGenerator.FromState(NullReference.Value<global::random.RandomState>(), RandomMathMode.Portable)));
            Assert.That(mtError.ArgumentName(), Is.EqualTo("seed"));
            Assert.That(numpyError.ArgumentName(), Is.EqualTo("seed"));
            Assert.That(stateError.ArgumentName(), Is.EqualTo("randomState"));
        });
    }

    static RandomGenerator[] CreateGenerators()
    {
        return
        [
            RandomGenerator.CreatePcg64Dxsm((UInt128)11, (UInt128)12, RandomMathMode.Portable),
            RandomGenerator.CreateXoshiro256StarStar(13UL, RandomMathMode.Portable),
            RandomGenerator.CreatePhilox4x64Ten((UInt128)14, RandomMathMode.Portable),
            RandomGenerator.CreateMt19937(15U, RandomMathMode.Portable),
            RandomGenerator.CreateMt19937Numpy(16U, RandomMathMode.Portable),
            RandomGenerator.CreateSplitMix64(17UL, RandomMathMode.Portable)
        ];
    }
}
