namespace random.Tests;

public class SeedExpanderTests
{
    [Test]
    public void KnownAnswerLocksCanonicalCounterModeLayout()
    {
        // Provenance: independently generated SHA-256 counter-mode fixture for RNDEXP01 v1,
        // using the exact SeedCodec UInt64(42), algorithm, purpose, length, and LE counter fields.
        byte[] seed = SeedCodec.EncodeUInt64(42UL);
        byte[] expanded = SeedExpander.Expand(
            seed,
            RandomAlgorithm.Pcg64DxsmV1,
            1,
            "engine-state",
            64);

        Assert.That(
            Convert.ToHexString(expanded),
            Is.EqualTo("41A3927196DDA0F115CAAD52038E463F04D313483D8560826F9F43F30D21769502E964516F2F55DE718208D59003A343E05592657738D928C9C7BCBE82FAB940"));
    }

    [Test]
    public void ExpansionIsDeterministicAndDoesNotMutateSeedMaterial()
    {
        byte[] seed = SeedCodec.EncodeText("replay seed");
        byte[] before = (byte[])seed.Clone();

        byte[] first = SeedExpander.Expand(seed, RandomAlgorithm.Xoshiro256StarStarV1, 1, "engine-state", 80);
        byte[] second = SeedExpander.Expand(seed, RandomAlgorithm.Xoshiro256StarStarV1, 1, "engine-state", 80);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(second));
            Assert.That(first.Length, Is.EqualTo(80));
            Assert.That(seed, Is.EqualTo(before));
        });
    }

    [Test]
    public void RequestedLengthControlsExactOutputAndItsDomain()
    {
        byte[] seed = SeedCodec.EncodeBytes([1, 2, 3]);
        byte[] empty = SeedExpander.Expand(seed, RandomAlgorithm.SplitMix64V1, 1, "engine-state", 0);
        byte[] one = SeedExpander.Expand(seed, RandomAlgorithm.SplitMix64V1, 1, "engine-state", 1);
        byte[] thirtyTwo = SeedExpander.Expand(seed, RandomAlgorithm.SplitMix64V1, 1, "engine-state", 32);
        byte[] thirtyThree = SeedExpander.Expand(seed, RandomAlgorithm.SplitMix64V1, 1, "engine-state", 33);

        Assert.Multiple(() =>
        {
            Assert.That(empty, Is.Empty);
            Assert.That(one.Length, Is.EqualTo(1));
            Assert.That(thirtyTwo.Length, Is.EqualTo(32));
            Assert.That(thirtyThree.Length, Is.EqualTo(33));
            Assert.That(thirtyTwo, Is.Not.EqualTo(thirtyThree.AsSpan(0, 32).ToArray()));
        });
    }

    [Test]
    public void AlgorithmVersionPurposeAndSeedTypeAreSeparateDomains()
    {
        byte[] unsignedSeed = SeedCodec.EncodeUInt64(0UL);
        byte[] signedSeed = SeedCodec.EncodeInt64(0L);
        byte[][] outputs =
        [
            SeedExpander.Expand(unsignedSeed, RandomAlgorithm.Pcg64DxsmV1, 1, "engine-state", 32),
            SeedExpander.Expand(unsignedSeed, RandomAlgorithm.Xoshiro256StarStarV1, 1, "engine-state", 32),
            SeedExpander.Expand(unsignedSeed, RandomAlgorithm.Pcg64DxsmV1, 2, "engine-state", 32),
            SeedExpander.Expand(unsignedSeed, RandomAlgorithm.Pcg64DxsmV1, 1, "stream-selector", 32),
            SeedExpander.Expand(signedSeed, RandomAlgorithm.Pcg64DxsmV1, 1, "engine-state", 32)
        ];

        Assert.That(outputs.Select(Convert.ToHexString).Distinct().Count(), Is.EqualTo(outputs.Length));
    }

    [Test]
    public void CounterProducesIndependentSuccessiveBlocks()
    {
        byte[] expanded = SeedExpander.Expand(
            SeedCodec.EncodeUInt32Array([1U, 2U, 3U]),
            RandomAlgorithm.Philox4x64TenV1,
            1,
            "engine-state",
            96);

        byte[] first = expanded.AsSpan(0, 32).ToArray();
        byte[] second = expanded.AsSpan(32, 32).ToArray();
        byte[] third = expanded.AsSpan(64, 32).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.EqualTo(second));
            Assert.That(second, Is.Not.EqualTo(third));
            Assert.That(first, Is.Not.EqualTo(third));
        });
    }

    [Test]
    public void InvalidInputsFailBeforeExpansion()
    {
        byte[] seed = SeedCodec.EncodeUInt64(1UL);

        AssertArgument("seedMaterial", () => SeedExpander.Expand(NullReference.Value<byte[]>(), RandomAlgorithm.Pcg64DxsmV1, 1, "state", 1));
        AssertArgument("algorithm", () => SeedExpander.Expand(seed, RandomAlgorithm.None, 1, "state", 1));
        AssertArgument("algorithm", () => SeedExpander.Expand(seed, RandomAlgorithm.SystemCryptoV1, 1, "state", 1));
        AssertArgument("algorithmVersion", () => SeedExpander.Expand(seed, RandomAlgorithm.Pcg64DxsmV1, 0, "state", 1));
        AssertArgument("purpose", () => SeedExpander.Expand(seed, RandomAlgorithm.Pcg64DxsmV1, 1, NullReference.Value<string>(), 1));
        AssertArgument("purpose", () => SeedExpander.Expand(seed, RandomAlgorithm.Pcg64DxsmV1, 1, "", 1));
        AssertArgument("purpose", () => SeedExpander.Expand(seed, RandomAlgorithm.Pcg64DxsmV1, 1, "\uD800", 1));
        AssertArgument("outputLength", () => SeedExpander.Expand(seed, RandomAlgorithm.Pcg64DxsmV1, 1, "state", -1));
    }

    static void AssertArgument(string expectedName, TestDelegate action)
    {
        RandomArgumentException error = Assert.Throws<RandomArgumentException>(action);
        Assert.That(error.ArgumentName(), Is.EqualTo(expectedName));
    }
}
