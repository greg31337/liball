namespace random.Tests;

public class RandomFactorySeedTests
{
    static readonly RandomAlgorithm[] DeterministicAlgorithms =
    [
        RandomAlgorithm.Pcg64DxsmV1,
        RandomAlgorithm.Xoshiro256StarStarV1,
        RandomAlgorithm.Philox4x64TenV1,
        RandomAlgorithm.Mt19937V1,
        RandomAlgorithm.Mt19937NumpyV1,
        RandomAlgorithm.SplitMix64V1
    ];

    [Test]
    public void NamedPublicFactoriesPreserveAlgorithmModeAndInitialStream()
    {
        IRandomFactory factory = new RandomFactory();
        RandomGenerator[] actual =
        [
            factory.CreateDefaultGenerator(1UL, RandomMathMode.System),
            factory.CreatePcg64Dxsm((UInt128)2, (UInt128)3, RandomMathMode.System),
            factory.CreateXoshiro256StarStar(4UL, RandomMathMode.System),
            factory.CreatePhilox4x64Ten((UInt128)5, RandomMathMode.System),
            factory.CreateMt19937(6U, RandomMathMode.System),
            factory.CreateMt19937FromArray([7U, 8U, 9U], RandomMathMode.System),
            factory.CreateMt19937Numpy(10U, RandomMathMode.System),
            factory.CreateMt19937NumpyFromArray([11U, 12U, 13U], RandomMathMode.System),
            factory.CreateSplitMix64(14UL, RandomMathMode.System)
        ];
        RandomGenerator[] expected =
        [
            RandomGenerator.CreateDefault(1UL, RandomMathMode.System),
            RandomGenerator.CreatePcg64Dxsm((UInt128)2, (UInt128)3, RandomMathMode.System),
            RandomGenerator.CreateXoshiro256StarStar(4UL, RandomMathMode.System),
            RandomGenerator.CreatePhilox4x64Ten((UInt128)5, RandomMathMode.System),
            RandomGenerator.CreateMt19937(6U, RandomMathMode.System),
            RandomGenerator.CreateMt19937([7U, 8U, 9U], RandomMathMode.System),
            RandomGenerator.CreateMt19937Numpy(10U, RandomMathMode.System),
            RandomGenerator.CreateMt19937Numpy([11U, 12U, 13U], RandomMathMode.System),
            RandomGenerator.CreateSplitMix64(14UL, RandomMathMode.System)
        ];

        Assert.That(actual, Has.Length.EqualTo(expected.Length));
        for (int index = 0; index < actual.Length; index++)
        {
            Assert.Multiple(() =>
            {
                Assert.That(actual[index].Info().Algorithm(), Is.EqualTo(expected[index].Info().Algorithm()));
                Assert.That(actual[index].MathMode(), Is.EqualTo(RandomMathMode.System));
                Assert.That(actual[index].NextRawWord(), Is.EqualTo(expected[index].NextRawWord()));
                Assert.That(actual[index].NextRawWord(), Is.EqualTo(expected[index].NextRawWord()));
            });
        }
    }

    [Test]
    public void CounterAndArrayPublicFactoriesCopyCallerInputs()
    {
        IRandomFactory factory = new RandomFactory();
        ulong[] key = [1UL, 2UL];
        ulong[] counter = [3UL, 4UL, 5UL, 6UL];
        uint[] referenceSeed = [0x123U, 0x234U, 0x345U, 0x456U];
        uint[] numpySeed = [0x123U, 0x234U, 0x345U, 0x456U];
        RandomGenerator philox = factory.CreatePhilox4x64TenAtCounter(key, counter, RandomMathMode.Portable);
        RandomGenerator reference = factory.CreateMt19937FromArray(referenceSeed, RandomMathMode.Portable);
        RandomGenerator numpy = factory.CreateMt19937NumpyFromArray(numpySeed, RandomMathMode.Portable);
        key[0] = 99UL;
        counter[0] = 99UL;
        referenceSeed[0] = 99U;
        numpySeed[0] = 99U;

        Assert.Multiple(() =>
        {
            Assert.That(philox.NextRawWord(), Is.EqualTo(Philox4x64TenEngine.FromKeyAndCounter([1UL, 2UL], [3UL, 4UL, 5UL, 6UL]).NextUInt64()));
            Assert.That(reference.NextRawWord(), Is.EqualTo(1067595299UL));
            Assert.That(numpy.NextRawWord(), Is.EqualTo(0x2238C27FUL));
        });
    }

    [Test]
    public void PublicSeedFactoriesPreserveDomainsAndCopyCallerInputs()
    {
        IRandomFactory factory = new RandomFactory();
        byte[] bytes = [1, 2, 3, 4];
        uint[] words = [0x04030201U];
        SeedSequence fromBytes = factory.CreateSeedFromBytes(bytes);
        SeedSequence fromWords = factory.CreateSeedFromUInt32Array(words);
        bytes[0] = 99;
        words[0] = 99U;

        Assert.Multiple(() =>
        {
            Assert.That(factory.CreateSeedFromUInt64(42UL).GenerateBytes(32), Is.EqualTo(SeedSequence.FromUInt64(42UL).GenerateBytes(32)));
            Assert.That(factory.CreateSeedFromInt64(-42L).GenerateBytes(32), Is.EqualTo(SeedSequence.FromInt64(-42L).GenerateBytes(32)));
            Assert.That(fromBytes.GenerateBytes(32), Is.EqualTo(SeedSequence.FromBytes([1, 2, 3, 4]).GenerateBytes(32)));
            Assert.That(fromWords.GenerateBytes(32), Is.EqualTo(SeedSequence.FromUInt32Array([0x04030201U]).GenerateBytes(32)));
            Assert.That(factory.CreateSeedFromText("factory seed").GenerateBytes(32), Is.EqualTo(SeedSequence.FromText("factory seed").GenerateBytes(32)));
            Assert.That(factory.CreateSeedFromUInt64(0UL).GenerateBytes(32), Is.Not.EqualTo(factory.CreateSeedFromInt64(0L).GenerateBytes(32)));
        });
    }

    [Test]
    public void PublicStateDecodersAndRestoreResumeTheCompleteGeneratorState()
    {
        IRandomFactory factory = new RandomFactory();
        RandomGenerator source = factory.CreateSplitMix64(123456789UL, RandomMathMode.Portable);
        source.NextBoolean();
        source.NextBits(17);
        RandomState exported = source.ExportState();
        byte[] encoded = exported.ToBytes();
        RandomState fromBytes = factory.CreateStateFromBytes(encoded);
        RandomState fromBase64 = factory.CreateStateFromBase64(exported.ToBase64());
        encoded[0] ^= 0xFF;
        RandomGenerator binary = factory.RestoreGenerator(fromBytes, RandomMathMode.System);
        RandomGenerator text = factory.RestoreGenerator(fromBase64, RandomMathMode.System);
        bool expectedBoolean = source.NextBoolean();

        Assert.Multiple(() =>
        {
            Assert.That(fromBytes.ToBytes(), Is.EqualTo(exported.ToBytes()));
            Assert.That(fromBase64.ToBytes(), Is.EqualTo(exported.ToBytes()));
            Assert.That(binary.MathMode(), Is.EqualTo(RandomMathMode.System));
            Assert.That(text.MathMode(), Is.EqualTo(RandomMathMode.System));
            Assert.That(binary.NextBoolean(), Is.EqualTo(expectedBoolean));
            Assert.That(text.NextBoolean(), Is.EqualTo(expectedBoolean));
        });

        ulong expectedRaw = source.NextRawWord();
        Assert.Multiple(() =>
        {
            Assert.That(binary.NextRawWord(), Is.EqualTo(expectedRaw));
            Assert.That(text.NextRawWord(), Is.EqualTo(expectedRaw));
        });
    }

    [Test]
    public void SeedSequenceCreatesEveryNamedDeterministicAlgorithmWithReplay()
    {
        IRandomFactory factory = new RandomFactory();
        SeedSequence sequence = factory.CreateSeedFromText("canonical replay").SpawnPath([3UL, 5UL, 8UL]);

        foreach (RandomAlgorithm algorithm in DeterministicAlgorithms)
        {
            RandomGenerator first = factory.CreateGeneratorFromSeedSequence(algorithm, sequence, RandomMathMode.Portable);
            RandomGenerator second = factory.CreateGeneratorFromSeedSequence(algorithm, sequence, RandomMathMode.Portable);

            Assert.Multiple(() =>
            {
                Assert.That(first.Info().Algorithm(), Is.EqualTo(algorithm));
                Assert.That(first.MathMode(), Is.EqualTo(RandomMathMode.Portable));
                Assert.That(first.NextRawWord(), Is.EqualTo(second.NextRawWord()));
                Assert.That(first.NextRawWord(), Is.EqualTo(second.NextRawWord()));
            });
        }
    }

    [Test]
    public void SeedSequencePathIdentityMatchesGeneratorSpawning()
    {
        IRandomFactory factory = new RandomFactory();
        SeedSequence root = factory.CreateSeedFromUInt64(123456789UL);

        foreach (RandomAlgorithm algorithm in DeterministicAlgorithms)
        {
            RandomGenerator generatorChild = factory.CreateGeneratorFromSeedSequence(algorithm, root, RandomMathMode.System).Spawn(42UL).Spawn(99UL);
            RandomGenerator sequenceChild = factory.CreateGeneratorFromSeedSequence(algorithm, root.SpawnPath([42UL, 99UL]), RandomMathMode.System);

            Assert.Multiple(() =>
            {
                Assert.That(generatorChild.MathMode(), Is.EqualTo(RandomMathMode.System));
                Assert.That(generatorChild.NextRawWord(), Is.EqualTo(sequenceChild.NextRawWord()));
                Assert.That(generatorChild.NextRawWord(), Is.EqualTo(sequenceChild.NextRawWord()));
            });
        }
    }

    [Test]
    public void CanonicalSeedFactoryPathsMatchTheirSeedSequencesAndCopyInputs()
    {
        IRandomFactory factory = new RandomFactory();

        foreach (RandomAlgorithm algorithm in DeterministicAlgorithms)
        {
            byte[] bytes = [1, 2, 3, 4, 5, 6, 7, 8];
            uint[] words = [0x04030201U, 0x08070605U];
            RandomGenerator fromBytes = factory.CreateGeneratorFromBytes(algorithm, bytes, RandomMathMode.Portable);
            RandomGenerator fromWords = factory.CreateGeneratorFromUInt32Array(algorithm, words, RandomMathMode.Portable);
            RandomGenerator fromText = factory.CreateGeneratorFromText(algorithm, "seed text", RandomMathMode.Portable);
            bytes[0] = 99;
            words[0] = 99U;

            RandomGenerator byteSequence = factory.CreateGeneratorFromSeedSequence(algorithm, factory.CreateSeedFromBytes([1, 2, 3, 4, 5, 6, 7, 8]), RandomMathMode.Portable);
            RandomGenerator wordSequence = factory.CreateGeneratorFromSeedSequence(algorithm, factory.CreateSeedFromUInt32Array([0x04030201U, 0x08070605U]), RandomMathMode.Portable);
            RandomGenerator textSequence = factory.CreateGeneratorFromSeedSequence(algorithm, factory.CreateSeedFromText("seed text"), RandomMathMode.Portable);

            Assert.Multiple(() =>
            {
                Assert.That(fromBytes.ExportState().ToBytes(), Is.EqualTo(byteSequence.ExportState().ToBytes()));
                Assert.That(fromWords.ExportState().ToBytes(), Is.EqualTo(wordSequence.ExportState().ToBytes()));
                Assert.That(fromText.ExportState().ToBytes(), Is.EqualTo(textSequence.ExportState().ToBytes()));
                Assert.That(fromBytes.ExportState().ToBytes(), Is.Not.EqualTo(fromWords.ExportState().ToBytes()));
                Assert.That(fromBytes.ExportState().ToBytes(), Is.Not.EqualTo(fromText.ExportState().ToBytes()));
            });
        }
    }

    [Test]
    public void SystemEntropyCreatesFreshGeneratorsForEveryNamedAlgorithm()
    {
        IRandomFactory factory = new RandomFactory();

        foreach (RandomAlgorithm algorithm in DeterministicAlgorithms)
        {
            RandomGenerator first = factory.CreateGeneratorFromSystemEntropy(algorithm, RandomMathMode.Portable);
            RandomGenerator second = factory.CreateGeneratorFromSystemEntropy(algorithm, RandomMathMode.Portable);

            Assert.Multiple(() =>
            {
                Assert.That(first.Info().Algorithm(), Is.EqualTo(algorithm));
                Assert.That(first.ExportState().ToBytes(), Is.Not.EqualTo(second.ExportState().ToBytes()));
            });
        }
    }

    [Test]
    public void CanonicalGeneratorFactoriesRejectInvalidArguments()
    {
        IRandomFactory factory = new RandomFactory();
        SeedSequence sequence = factory.CreateSeedFromUInt64(1UL);

        AssertArgument("seedSequence", () => factory.CreateGeneratorFromSeedSequence(RandomAlgorithm.Pcg64DxsmV1, NullReference.Value<global::random.SeedSequence>(), RandomMathMode.Portable));
        AssertArgument("seed", () => factory.CreateGeneratorFromBytes(RandomAlgorithm.Pcg64DxsmV1, NullReference.Value<byte[]>(), RandomMathMode.Portable));
        AssertArgument("seed", () => factory.CreateGeneratorFromText(RandomAlgorithm.Pcg64DxsmV1, NullReference.Value<string>(), RandomMathMode.Portable));
        AssertArgument("seed", () => factory.CreateGeneratorFromUInt32Array(RandomAlgorithm.Pcg64DxsmV1, NullReference.Value<uint[]>(), RandomMathMode.Portable));
        AssertArgument("algorithm", () => factory.CreateGeneratorFromSeedSequence(RandomAlgorithm.None, sequence, RandomMathMode.Portable));
        AssertArgument("algorithm", () => factory.CreateGeneratorFromBytes(RandomAlgorithm.SystemCryptoV1, [], RandomMathMode.Portable));
        AssertArgument("algorithm", () => factory.CreateGeneratorFromText(RandomAlgorithm.HmacDrbgSha256V1, "seed", RandomMathMode.Portable));
        AssertArgument("algorithm", () => factory.CreateGeneratorFromUInt32Array((RandomAlgorithm)999, [], RandomMathMode.Portable));
        AssertArgument("algorithm", () => factory.CreateGeneratorFromSystemEntropy(RandomAlgorithm.None, RandomMathMode.Portable));
        AssertArgument("mathMode", () => factory.CreateGeneratorFromSeedSequence(RandomAlgorithm.SplitMix64V1, sequence, (RandomMathMode)999));
        AssertArgument("mathMode", () => factory.CreateGeneratorFromSystemEntropy(RandomAlgorithm.SplitMix64V1, (RandomMathMode)999));
    }

    static void AssertArgument(string expectedName, TestDelegate action)
    {
        RandomArgumentException error = Assert.Throws<RandomArgumentException>(action);
        Assert.That(error.ArgumentName(), Is.EqualTo(expectedName));
    }
}
