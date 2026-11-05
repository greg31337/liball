namespace random.Tests;

public class RandomAlgorithmInfoTests
{
    [Test]
    public void ConstructorExposesSuppliedMetadata()
    {
        RandomAlgorithmInfo info = new RandomAlgorithmInfo(
            RandomAlgorithm.Pcg64DxsmV1,
            "PCG64 DXSM",
            1,
            1,
            64,
            32,
            false,
            "2^128 64-bit outputs per stream",
            [RandomCapability.State, RandomCapability.Clone, RandomCapability.Advance]);

        Assert.Multiple(() =>
        {
            Assert.That(info.Algorithm(), Is.EqualTo(RandomAlgorithm.Pcg64DxsmV1));
            Assert.That(info.Name(), Is.EqualTo("PCG64 DXSM"));
            Assert.That(info.Version(), Is.EqualTo(1));
            Assert.That(info.TransformVersion(), Is.EqualTo(1));
            Assert.That(info.WordBits(), Is.EqualTo(64));
            Assert.That(info.StateBytes(), Is.EqualTo(32));
            Assert.That(info.IsCryptographic(), Is.False);
            Assert.That(info.PeriodDescription(), Is.EqualTo("2^128 64-bit outputs per stream"));
            Assert.That(info.HasCapability(RandomCapability.State), Is.True);
            Assert.That(info.HasCapability(RandomCapability.Advance), Is.True);
            Assert.That(info.HasCapability(RandomCapability.Jump), Is.False);
            Assert.That(info.HasCapability(RandomCapability.None), Is.False);
        });
    }

    [Test]
    public void CapabilityArraysAreDefensivelyCopied()
    {
        RandomCapability[] supplied = [RandomCapability.Reseed];
        RandomAlgorithmInfo info = new RandomAlgorithmInfo(
            RandomAlgorithm.HmacDrbgSha256V1,
            "HMAC-DRBG-SHA-256",
            1,
            1,
            8,
            0,
            true,
            "Reseed required after 2^48 successful nonempty generate requests",
            supplied);

        supplied[0] = RandomCapability.Jump;
        RandomCapability[] returned = info.Capabilities();
        returned[0] = RandomCapability.Seek;

        Assert.Multiple(() =>
        {
            Assert.That(info.HasCapability(RandomCapability.Reseed), Is.True);
            Assert.That(info.HasCapability(RandomCapability.Jump), Is.False);
            Assert.That(info.HasCapability(RandomCapability.Seek), Is.False);
            Assert.That(info.Capabilities(), Is.EqualTo(new[] { RandomCapability.Reseed }));
        });
    }

    [Test]
    public void ConstructorRejectsInvalidMetadataWithArgumentNames()
    {
        object[] cases =
        {
            new object[] { (TestDelegate)(() => new RandomAlgorithmInfo(RandomAlgorithm.None, "None", 1, 1, 64, 0, false, "Period", [])), "algorithm" },
            new object[] { (TestDelegate)(() => new RandomAlgorithmInfo(RandomAlgorithm.Pcg64DxsmV1, NullReference.Value<string>(), 1, 1, 64, 0, false, "Period", [])), "name" },
            new object[] { (TestDelegate)(() => new RandomAlgorithmInfo(RandomAlgorithm.Pcg64DxsmV1, "", 1, 1, 64, 0, false, "Period", [])), "name" },
            new object[] { (TestDelegate)(() => new RandomAlgorithmInfo(RandomAlgorithm.Pcg64DxsmV1, "PCG", 0, 1, 64, 0, false, "Period", [])), "version" },
            new object[] { (TestDelegate)(() => new RandomAlgorithmInfo(RandomAlgorithm.Pcg64DxsmV1, "PCG", 1, 0, 64, 0, false, "Period", [])), "transformVersion" },
            new object[] { (TestDelegate)(() => new RandomAlgorithmInfo(RandomAlgorithm.Pcg64DxsmV1, "PCG", 1, 1, 0, 0, false, "Period", [])), "wordBits" },
            new object[] { (TestDelegate)(() => new RandomAlgorithmInfo(RandomAlgorithm.Pcg64DxsmV1, "PCG", 1, 1, 64, -1, false, "Period", [])), "stateBytes" },
            new object[] { (TestDelegate)(() => new RandomAlgorithmInfo(RandomAlgorithm.Pcg64DxsmV1, "PCG", 1, 1, 64, 0, false, NullReference.Value<string>(), [])), "periodDescription" },
            new object[] { (TestDelegate)(() => new RandomAlgorithmInfo(RandomAlgorithm.Pcg64DxsmV1, "PCG", 1, 1, 64, 0, false, "", [])), "periodDescription" },
            new object[] { (TestDelegate)(() => new RandomAlgorithmInfo(RandomAlgorithm.Pcg64DxsmV1, "PCG", 1, 1, 64, 0, false, "Period", NullReference.Value<global::random.RandomCapability[]>())), "capabilities" }
        };

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
        }
    }

    [Test]
    public void EveryNamedAlgorithmExposesStableTransformAndPeriodMetadata()
    {
        RandomFactory factory = new RandomFactory();
        RandomAlgorithmInfo[] infos =
        {
            factory.CreatePcg64Dxsm((UInt128)1, (UInt128)3, RandomMathMode.Portable).Info(),
            factory.CreateXoshiro256StarStar(1UL, RandomMathMode.Portable).Info(),
            factory.CreatePhilox4x64Ten((UInt128)1, RandomMathMode.Portable).Info(),
            factory.CreateMt19937(1U, RandomMathMode.Portable).Info(),
            factory.CreateMt19937Numpy(1U, RandomMathMode.Portable).Info(),
            factory.CreateSplitMix64(1UL, RandomMathMode.Portable).Info(),
            factory.CreateSystemSecureGenerator().Info(),
            factory.CreateHmacDrbgFromSeedMaterial(new byte[32], new byte[16], []).Info()
        };
        string[] expectedDescriptions =
        {
            "2^128 64-bit outputs per stream",
            "2^256 - 1 64-bit outputs",
            "2^256 counter blocks of four 64-bit outputs per key; exhausted after the final block",
            "2^19937 - 1 32-bit outputs",
            "2^19937 - 1 32-bit outputs",
            "2^64 64-bit outputs",
            "No library-defined period; operating-system source failures propagate",
            "Reseed required after 2^48 successful nonempty generate requests"
        };

        Assert.That(infos, Has.Length.EqualTo(expectedDescriptions.Length));
        for (int index = 0; index < infos.Length; index++)
        {
            Assert.Multiple(() =>
            {
                Assert.That(infos[index].TransformVersion(), Is.EqualTo(1));
                Assert.That(infos[index].PeriodDescription(), Is.EqualTo(expectedDescriptions[index]));
            });
        }
    }

    [Test]
    public void PersistentEnumIdentifiersRemainStable()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)RandomAlgorithm.None, Is.EqualTo(0));
            Assert.That((int)RandomAlgorithm.Pcg64DxsmV1, Is.EqualTo(1));
            Assert.That((int)RandomAlgorithm.Xoshiro256StarStarV1, Is.EqualTo(2));
            Assert.That((int)RandomAlgorithm.Philox4x64TenV1, Is.EqualTo(3));
            Assert.That((int)RandomAlgorithm.Mt19937V1, Is.EqualTo(4));
            Assert.That((int)RandomAlgorithm.Mt19937NumpyV1, Is.EqualTo(5));
            Assert.That((int)RandomAlgorithm.SplitMix64V1, Is.EqualTo(6));
            Assert.That((int)RandomAlgorithm.SystemCryptoV1, Is.EqualTo(100));
            Assert.That((int)RandomAlgorithm.HmacDrbgSha256V1, Is.EqualTo(101));
            Assert.That((int)RandomMathMode.Portable, Is.EqualTo(0));
            Assert.That((int)RandomMathMode.System, Is.EqualTo(1));
        });
    }
}
