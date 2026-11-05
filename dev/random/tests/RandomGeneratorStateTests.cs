namespace random.Tests;

public class RandomGeneratorStateTests
{
    [Test]
    public void ExportAndImportResumeEveryNamedEngineAtTheExactNextWord()
    {
        RandomGenerator[] generators = CreateGenerators();
        foreach (RandomGenerator generator in generators)
        {
            for (int draw = 0; draw < 7; draw++)
            {
                generator.NextRawWord();
            }

            RandomState exported = generator.ExportState();
            RandomGenerator imported = RandomGenerator.FromState(exported, generator.MathMode());
            ulong expected = generator.NextRawWord();

            Assert.Multiple(() =>
            {
                Assert.That(exported.Algorithm(), Is.EqualTo(generator.Info().Algorithm()));
                Assert.That(imported.Info().Algorithm(), Is.EqualTo(generator.Info().Algorithm()));
                Assert.That(imported.MathMode(), Is.EqualTo(generator.MathMode()));
                Assert.That(imported.NextRawWord(), Is.EqualTo(expected));
            });
        }
    }

    [Test]
    public void BinaryAndBase64GeneratorStateRoundTripsAreCanonical()
    {
        RandomGenerator source = RandomGenerator.CreatePhilox4x64Ten(
            [0x0123456789ABCDEFUL, 0xFEDCBA9876543210UL],
            [1UL, 2UL, 3UL, 4UL],
            RandomMathMode.Portable);
        source.NextRawWord();
        source.NextRawWord();
        RandomState exported = source.ExportState();
        RandomState fromBytes = RandomState.FromBytes(exported.ToBytes());
        RandomState fromBase64 = RandomState.FromBase64(exported.ToBase64());
        RandomGenerator binaryGenerator = RandomGenerator.FromState(fromBytes, RandomMathMode.Portable);
        RandomGenerator base64Generator = RandomGenerator.FromState(fromBase64, RandomMathMode.Portable);
        ulong expected = source.NextRawWord();

        Assert.Multiple(() =>
        {
            Assert.That(fromBytes.ToBytes(), Is.EqualTo(exported.ToBytes()));
            Assert.That(fromBase64.ToBase64(), Is.EqualTo(exported.ToBase64()));
            Assert.That(binaryGenerator.NextRawWord(), Is.EqualTo(expected));
            Assert.That(base64Generator.NextRawWord(), Is.EqualTo(expected));
        });
    }

    [Test]
    public void RestoreRejectsAlgorithmMismatchWithoutChangingEitherState()
    {
        RandomGenerator target = RandomGenerator.CreateMt19937(42U, RandomMathMode.Portable);
        RandomGenerator other = RandomGenerator.CreateMt19937Numpy(42U, RandomMathMode.Portable);
        target.NextRawWord();
        other.NextRawWord();
        byte[] targetBefore = target.ExportState().ToBytes();
        byte[] otherBefore = other.ExportState().ToBytes();

        RandomStateException error = Assert.Throws<RandomStateException>(() => target.RestoreState(other.ExportState()));

        Assert.Multiple(() =>
        {
            Assert.That(error.Message, Does.Contain("different random algorithm"));
            Assert.That(target.ExportState().ToBytes(), Is.EqualTo(targetBefore));
            Assert.That(other.ExportState().ToBytes(), Is.EqualTo(otherBefore));
        });
    }

    [Test]
    public void RestoreRejectsMalformedSameAlgorithmStateAtomically()
    {
        RandomGenerator generator = RandomGenerator.CreateSplitMix64(123UL, RandomMathMode.Portable);
        generator.NextRawWord();
        byte[] before = generator.ExportState().ToBytes();
        RandomState malformed = new RandomState(RandomAlgorithm.SplitMix64V1, 1, 1, new byte[7]);

        Assert.Throws<RandomStateException>(() => generator.RestoreState(malformed));
        Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
    }

    [Test]
    public void RestoreChangesCurrentPositionButNotOriginalResetPoint()
    {
        RandomGenerator target = RandomGenerator.CreateSplitMix64(1UL, RandomMathMode.Portable);
        RandomGenerator importedSource = RandomGenerator.CreateSplitMix64(2UL, RandomMathMode.Portable);
        ulong originalFirst = target.NextRawWord();
        importedSource.NextRawWord();
        RandomState imported = importedSource.ExportState();
        ulong importedNext = importedSource.NextRawWord();

        target.RestoreState(imported);
        Assert.That(target.NextRawWord(), Is.EqualTo(importedNext));
        target.Reset();
        Assert.That(target.NextRawWord(), Is.EqualTo(originalFirst));
    }

    [Test]
    public void RestoreNullUsesThePublicArgumentNameWithoutMutation()
    {
        RandomGenerator generator = RandomGenerator.CreateSplitMix64(9UL, RandomMathMode.Portable);
        byte[] before = generator.ExportState().ToBytes();

        RandomArgumentException error = Assert.Throws<RandomArgumentException>(() => generator.RestoreState(NullReference.Value<global::random.RandomState>()));

        Assert.Multiple(() =>
        {
            Assert.That(error.ArgumentName(), Is.EqualTo("randomState"));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [TestCase("pcg64-dxsm-v1.base64", RandomAlgorithm.Pcg64DxsmV1)]
    [TestCase("xoshiro256-starstar-v1.base64", RandomAlgorithm.Xoshiro256StarStarV1)]
    [TestCase("philox4x64-ten-v1.base64", RandomAlgorithm.Philox4x64TenV1)]
    [TestCase("splitmix64-v1.base64", RandomAlgorithm.SplitMix64V1)]
    public void Base64StateMatchesCheckedInGoldenFile(string fileName, RandomAlgorithm algorithm)
    {
        RandomGenerator generator = CreateGoldenGenerator(algorithm);
        for (int draw = 0; draw < 3; draw++)
        {
            generator.NextRawWord();
        }

        string path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..",
            "..",
            "..",
            "TestData",
            fileName));
        string expected = File.ReadAllText(path).Trim();
        string actual = generator.ExportState().ToBase64();
        RandomState decoded = RandomState.FromBase64(expected);
        RandomGenerator restored = RandomGenerator.FromState(decoded, RandomMathMode.Portable);
        ulong next = generator.NextRawWord();

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(decoded.Algorithm(), Is.EqualTo(algorithm));
            Assert.That(decoded.ToBase64(), Is.EqualTo(expected));
            Assert.That(restored.NextRawWord(), Is.EqualTo(next));
        });
    }

    static RandomGenerator[] CreateGenerators()
    {
        return
        [
            RandomGenerator.CreatePcg64Dxsm((UInt128)1, (UInt128)2, RandomMathMode.Portable),
            RandomGenerator.CreateXoshiro256StarStar(3UL, RandomMathMode.System),
            RandomGenerator.CreatePhilox4x64Ten((UInt128)4, RandomMathMode.Portable),
            RandomGenerator.CreateMt19937(5U, RandomMathMode.System),
            RandomGenerator.CreateMt19937Numpy(6U, RandomMathMode.Portable),
            RandomGenerator.CreateSplitMix64(7UL, RandomMathMode.System)
        ];
    }

    static RandomGenerator CreateGoldenGenerator(RandomAlgorithm algorithm)
    {
        switch (algorithm)
        {
            case RandomAlgorithm.Pcg64DxsmV1:
                return RandomGenerator.CreatePcg64Dxsm((UInt128)1, (UInt128)2, RandomMathMode.Portable);
            case RandomAlgorithm.Xoshiro256StarStarV1:
                return RandomGenerator.CreateXoshiro256StarStar(3UL, RandomMathMode.Portable);
            case RandomAlgorithm.Philox4x64TenV1:
                return RandomGenerator.CreatePhilox4x64Ten((UInt128)4, RandomMathMode.Portable);
            default:
                return RandomGenerator.CreateSplitMix64(7UL, RandomMathMode.Portable);
        }
    }
}
