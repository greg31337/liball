namespace random.Tests;

public class SeedSequenceTests
{
    [Test]
    public void CreationDomainsRemainDistinctAndInputsAreCopied()
    {
        byte[] bytes = new byte[8];
        uint[] words = [0U, 0U];
        SeedSequence fromBytes = SeedSequence.FromBytes(bytes);
        SeedSequence fromWords = SeedSequence.FromUInt32Array(words);
        bytes[0] = 1;
        words[0] = 1U;

        string[] outputs =
        [
            Convert.ToHexString(SeedSequence.FromUInt64(0UL).GenerateBytes(32)),
            Convert.ToHexString(SeedSequence.FromInt64(0L).GenerateBytes(32)),
            Convert.ToHexString(fromBytes.GenerateBytes(32)),
            Convert.ToHexString(SeedSequence.FromText("\0\0\0\0\0\0\0\0").GenerateBytes(32)),
            Convert.ToHexString(fromWords.GenerateBytes(32))
        ];

        Assert.That(outputs.Distinct().Count(), Is.EqualTo(outputs.Length));
        Assert.That(fromBytes.GenerateBytes(32), Is.EqualTo(SeedSequence.FromBytes(new byte[8]).GenerateBytes(32)));
        Assert.That(fromWords.GenerateBytes(32), Is.EqualTo(SeedSequence.FromUInt32Array([0U, 0U]).GenerateBytes(32)));
    }

    [Test]
    public void GenerateBytesIsRepeatableAndLengthSpecific()
    {
        SeedSequence sequence = SeedSequence.FromText("replay").SpawnPath([4UL, 9UL]);
        byte[] first = sequence.GenerateBytes(48);
        byte[] second = sequence.GenerateBytes(48);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(second));
            Assert.That(first.Length, Is.EqualTo(48));
            Assert.That(sequence.GenerateBytes(0), Is.Empty);
            Assert.That(sequence.GenerateBytes(32), Is.Not.EqualTo(first.AsSpan(0, 32).ToArray()));
        });
    }

    [Test]
    public void ExplicitPathAndNestedSpawningHaveTheSameIdentity()
    {
        SeedSequence root = SeedSequence.FromUInt64(123456789UL);
        SeedSequence nested = root.Spawn(7UL).Spawn(11UL).Spawn(13UL);
        SeedSequence explicitPath = root.SpawnPath([7UL, 11UL, 13UL]);
        SeedSequence reordered = root.SpawnPath([13UL, 11UL, 7UL]);

        Assert.Multiple(() =>
        {
            Assert.That(nested.Path(), Is.EqualTo(new ulong[] { 7UL, 11UL, 13UL }));
            Assert.That(nested.GenerateBytes(64), Is.EqualTo(explicitPath.GenerateBytes(64)));
            Assert.That(nested.GenerateBytes(64), Is.Not.EqualTo(reordered.GenerateBytes(64)));
        });
    }

    [Test]
    public void PathsAreDefensivelyCopied()
    {
        ulong[] supplied = [1UL, 2UL, 3UL];
        SeedSequence sequence = SeedSequence.FromUInt64(1UL).SpawnPath(supplied);
        supplied[0] = 99UL;
        ulong[] returned = sequence.Path();
        returned[1] = 99UL;

        Assert.That(sequence.Path(), Is.EqualTo(new ulong[] { 1UL, 2UL, 3UL }));
    }

    [Test]
    public void SpawnManyMatchesIndependentIndexedChildren()
    {
        SeedSequence parent = SeedSequence.FromText("parallel").Spawn(42UL);
        SeedSequence[] children = parent.SpawnMany(5);

        Assert.That(children.Length, Is.EqualTo(5));
        for (int index = 0; index < children.Length; index++)
        {
            Assert.That(children[index].Path(), Is.EqualTo(new ulong[] { 42UL, (ulong)index }));
            Assert.That(children[index].GenerateBytes(32), Is.EqualTo(parent.Spawn((ulong)index).GenerateBytes(32)));
        }

        Assert.That(parent.SpawnMany(0), Is.Empty);
    }

    [Test]
    public void FingerprintIdentifiesRootAcrossDescendants()
    {
        SeedSequence root = SeedSequence.FromUInt64(42UL);
        string fingerprint = root.Fingerprint();

        Assert.Multiple(() =>
        {
            Assert.That(fingerprint, Is.EqualTo("941EED9465661F2340FF8470E511DDD8C95D1337F06DB585EE0581108D7C0C60"));
            Assert.That(fingerprint.Length, Is.EqualTo(64));
            Assert.That(root.Spawn(1UL).Fingerprint(), Is.EqualTo(fingerprint));
            Assert.That(root.SpawnPath([9UL, 8UL, 7UL]).Fingerprint(), Is.EqualTo(fingerprint));
            Assert.That(SeedSequence.FromUInt64(43UL).Fingerprint(), Is.Not.EqualTo(fingerprint));
        });
    }

    [Test]
    public void MaterialHasCanonicalRootAndPathLayout()
    {
        SeedSequence sequence = SeedSequence.FromUInt64(42UL).SpawnPath([0x0123456789ABCDEFUL, ulong.MaxValue]);
        byte[] material = sequence.Material();

        Assert.Multiple(() =>
        {
            Assert.That(material.AsSpan(0, 8).ToArray(), Is.EqualTo(new byte[] { 0x52, 0x4E, 0x44, 0x53, 0x45, 0x51, 0x30, 0x31 }));
            Assert.That(BinaryCodec.ReadUInt32(material, 8), Is.EqualTo(1U));
            Assert.That(BinaryCodec.ReadUInt32(material, 12), Is.EqualTo(28U));
            Assert.That(BinaryCodec.ReadUInt32(material, 16), Is.EqualTo(2U));
            Assert.That(BinaryCodec.ReadUInt64(material, material.Length - 16), Is.EqualTo(0x0123456789ABCDEFUL));
            Assert.That(BinaryCodec.ReadUInt64(material, material.Length - 8), Is.EqualTo(ulong.MaxValue));
        });
    }

    [Test]
    public void InvalidArgumentsUseDedicatedFailures()
    {
        SeedSequence sequence = SeedSequence.FromUInt64(1UL);

        AssertArgument("seed", () => SeedSequence.FromBytes(NullReference.Value<byte[]>()));
        AssertArgument("seed", () => SeedSequence.FromText(NullReference.Value<string>()));
        AssertArgument("seed", () => SeedSequence.FromUInt32Array(NullReference.Value<uint[]>()));
        AssertArgument("path", () => sequence.SpawnPath(NullReference.Value<ulong[]>()));
        AssertArgument("count", () => sequence.SpawnMany(-1));
        AssertArgument("length", () => sequence.GenerateBytes(-1));
    }

    [Test]
    public void SystemEntropyCreatesCanonicalByteSeedRoot()
    {
        SeedSequence sequence = SeedSequence.FromSystemEntropy();
        byte[] material = sequence.Material();
        byte[] firstOutput = sequence.GenerateBytes(32);
        byte[] secondOutput = sequence.GenerateBytes(32);
        int rootOffset = SeedSequence.HeaderSize;

        Assert.Multiple(() =>
        {
            Assert.That(sequence.Path(), Is.Empty);
            Assert.That(BinaryCodec.ReadUInt32(material, 12), Is.EqualTo((uint)(SeedCodec.HeaderSize + SeedSequence.SystemEntropyByteCount)));
            Assert.That(BinaryCodec.ReadUInt64(material, rootOffset), Is.EqualTo(SeedCodec.Magic));
            Assert.That(BinaryCodec.ReadUInt32(material, rootOffset + 8), Is.EqualTo((uint)SeedCodec.EncodingVersion));
            Assert.That(BinaryCodec.ReadUInt32(material, rootOffset + 12), Is.EqualTo((uint)SeedDomain.Bytes));
            Assert.That(BinaryCodec.ReadUInt32(material, rootOffset + 16), Is.EqualTo((uint)SeedSequence.SystemEntropyByteCount));
            Assert.That(firstOutput, Is.EqualTo(secondOutput));
        });
    }

    [Test]
    public void SystemEntropyCallsCreateFreshRoots()
    {
        SeedSequence first = SeedSequence.FromSystemEntropy();
        SeedSequence second = SeedSequence.FromSystemEntropy();

        Assert.That(first.Fingerprint(), Is.Not.EqualTo(second.Fingerprint()));
    }

    static void AssertArgument(string expectedName, TestDelegate action)
    {
        RandomArgumentException error = Assert.Throws<RandomArgumentException>(action);
        Assert.That(error.ArgumentName(), Is.EqualTo(expectedName));
    }
}
