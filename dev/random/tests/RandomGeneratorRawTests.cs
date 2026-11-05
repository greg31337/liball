namespace random.Tests;

public class RandomGeneratorRawTests
{
    [Test]
    public void UnsignedWordsFollowNativeWidthMappings()
    {
        RandomGenerator pcg32 = RandomGenerator.CreatePcg64Dxsm((UInt128)42, (UInt128)54, RandomMathMode.Portable);
        Pcg64DxsmEngine directPcg = Pcg64DxsmEngine.FromSeed((UInt128)42, (UInt128)54);
        ulong pcgWord = directPcg.NextUInt64();
        RandomGenerator pcg64 = RandomGenerator.CreatePcg64Dxsm((UInt128)42, (UInt128)54, RandomMathMode.Portable);

        RandomGenerator mt32 = RandomGenerator.CreateMt19937(5489U, RandomMathMode.Portable);
        RandomGenerator mt64 = RandomGenerator.CreateMt19937(5489U, RandomMathMode.Portable);
        ulong expectedMt64 = 3499211612UL | (581869302UL << 32);

        Assert.Multiple(() =>
        {
            Assert.That(pcg32.NextUInt32(), Is.EqualTo((uint)(pcgWord & uint.MaxValue)));
            Assert.That(pcg64.NextUInt64(), Is.EqualTo(pcgWord));
            Assert.That(mt32.NextUInt32(), Is.EqualTo(3499211612U));
            Assert.That(mt64.NextUInt64(), Is.EqualTo(expectedMt64));
        });
    }

    [Test]
    public void SignedWordsPreserveEveryRawBit()
    {
        RandomGenerator unsigned32 = RandomGenerator.CreateMt19937(5489U, RandomMathMode.Portable);
        RandomGenerator signed32 = RandomGenerator.CreateMt19937(5489U, RandomMathMode.Portable);
        RandomGenerator unsigned64 = RandomGenerator.CreateSplitMix64(42UL, RandomMathMode.Portable);
        RandomGenerator signed64 = RandomGenerator.CreateSplitMix64(42UL, RandomMathMode.Portable);
        uint bits32 = unsigned32.NextUInt32();
        ulong bits64 = unsigned64.NextUInt64();

        Assert.Multiple(() =>
        {
            Assert.That(signed32.NextInt32(), Is.EqualTo(int.CreateTruncating(bits32)));
            Assert.That(signed64.NextInt64(), Is.EqualTo(long.CreateTruncating(bits64)));
        });
    }

    [Test]
    public void RequestedBitsUseLowBitsAndMinimumNativeWords()
    {
        RandomGenerator mtSmall = RandomGenerator.CreateMt19937(5489U, RandomMathMode.Portable);
        RandomGenerator mtLarge = RandomGenerator.CreateMt19937(5489U, RandomMathMode.Portable);
        RandomGenerator split = RandomGenerator.CreateSplitMix64(42UL, RandomMathMode.Portable);
        SplitMix64Engine direct = SplitMix64Engine.FromSeed(42UL);
        ulong splitWord = direct.NextUInt64();
        ulong combined = 3499211612UL | (581869302UL << 32);

        Assert.Multiple(() =>
        {
            Assert.That(mtSmall.NextBits(17), Is.EqualTo(3499211612UL & BitMath.LowMask64(17)));
            Assert.That(mtSmall.NextUInt32(), Is.EqualTo(581869302U));
            Assert.That(mtLarge.NextBits(47), Is.EqualTo(combined & BitMath.LowMask64(47)));
            Assert.That(split.NextBits(63), Is.EqualTo(splitWord & BitMath.LowMask64(63)));
        });
    }

    [Test]
    public void BooleanReservoirConsumesNativeWordsLeastSignificantBitFirst()
    {
        RandomGenerator split = RandomGenerator.CreateSplitMix64(42UL, RandomMathMode.Portable);
        SplitMix64Engine directSplit = SplitMix64Engine.FromSeed(42UL);
        ulong firstSplit = directSplit.NextUInt64();
        for (int bit = 0; bit < 64; bit++)
        {
            Assert.That(split.NextBoolean(), Is.EqualTo(((firstSplit >> bit) & 1UL) != 0UL));
        }

        ulong secondSplit = directSplit.NextUInt64();
        Assert.That(split.NextBoolean(), Is.EqualTo((secondSplit & 1UL) != 0UL));

        RandomGenerator mt = RandomGenerator.CreateMt19937(5489U, RandomMathMode.Portable);
        uint firstMt = 3499211612U;
        for (int bit = 0; bit < 32; bit++)
        {
            Assert.That(mt.NextBoolean(), Is.EqualTo(((firstMt >> bit) & 1U) != 0U));
        }

        Assert.That(mt.NextBoolean(), Is.EqualTo((581869302U & 1U) != 0U));
    }

    [Test]
    public void RawCallsDiscardUnusedBooleanBits()
    {
        RandomGenerator generator = RandomGenerator.CreateSplitMix64(42UL, RandomMathMode.Portable);
        SplitMix64Engine direct = SplitMix64Engine.FromSeed(42UL);
        ulong first = direct.NextUInt64();
        ulong second = direct.NextUInt64();
        ulong third = direct.NextUInt64();

        Assert.That(generator.NextBoolean(), Is.EqualTo((first & 1UL) != 0UL));
        Assert.That(generator.NextUInt64(), Is.EqualTo(second));
        Assert.That(generator.NextBoolean(), Is.EqualTo((third & 1UL) != 0UL));
    }

    [Test]
    public void ByteGenerationUsesCanonicalLittleEndianNativeWords()
    {
        RandomGenerator split = RandomGenerator.CreateSplitMix64(42UL, RandomMathMode.Portable);
        byte[] splitBytes = split.NextBytes(11);
        SplitMix64Engine directSplit = SplitMix64Engine.FromSeed(42UL);
        byte[] expectedSplit = new byte[11];
        WriteLowBytes(expectedSplit, 0, directSplit.NextUInt64(), 8);
        WriteLowBytes(expectedSplit, 8, directSplit.NextUInt64(), 3);

        RandomGenerator mt = RandomGenerator.CreateMt19937(5489U, RandomMathMode.Portable);
        byte[] mtBytes = new byte[7];
        mt.FillBytes(mtBytes);

        Assert.Multiple(() =>
        {
            Assert.That(splitBytes, Is.EqualTo(expectedSplit));
            Assert.That(mtBytes, Is.EqualTo(new byte[] { 0x5C, 0xBB, 0x91, 0xD0, 0xF6, 0x9E, 0xAE }));
        });
    }

    [Test]
    public void CloneExportImportAndResetPreserveBooleanReservoir()
    {
        RandomGenerator original = RandomGenerator.CreateSplitMix64(123UL, RandomMathMode.Portable);
        for (int bit = 0; bit < 5; bit++)
        {
            original.NextBoolean();
        }

        RandomGenerator clone = original.Clone();
        RandomState snapshot = original.ExportState();
        RandomGenerator imported = RandomGenerator.FromState(snapshot, RandomMathMode.Portable);
        Assert.That(snapshot.Payload().Length, Is.EqualTo(SplitMix64Engine.StateByteCount + GeneratorStateCodec.HeaderSize));

        bool[] expected = new bool[40];
        for (int bit = 0; bit < expected.Length; bit++)
        {
            expected[bit] = original.NextBoolean();
        }

        for (int bit = 0; bit < expected.Length; bit++)
        {
            Assert.Multiple(() =>
            {
                Assert.That(clone.NextBoolean(), Is.EqualTo(expected[bit]));
                Assert.That(imported.NextBoolean(), Is.EqualTo(expected[bit]));
            });
        }

        imported.Reset();
        RandomGenerator replay = RandomGenerator.FromState(snapshot, RandomMathMode.Portable);
        for (int bit = 0; bit < 20; bit++)
        {
            Assert.That(imported.NextBoolean(), Is.EqualTo(replay.NextBoolean()));
        }
    }

    [Test]
    public void EmptyAndZeroBitRequestsConsumeNothingIncludingCachedBits()
    {
        RandomGenerator generator = RandomGenerator.CreatePcg64Dxsm((UInt128)1, (UInt128)2, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();

        Assert.Multiple(() =>
        {
            Assert.That(generator.NextBits(0), Is.EqualTo(0UL));
            Assert.That(generator.NextBytes(0), Is.Empty);
            Assert.DoesNotThrow(() => generator.FillBytes(Span<byte>.Empty));
            Assert.DoesNotThrow(() => generator.Advance((UInt128)0));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [Test]
    public void PositioningClearsReservoirOnlyAfterSupportedOperationSucceeds()
    {
        RandomGenerator generator = RandomGenerator.CreatePcg64Dxsm((UInt128)1, (UInt128)2, RandomMathMode.Portable);
        Pcg64DxsmEngine direct = Pcg64DxsmEngine.FromSeed((UInt128)1, (UInt128)2);
        generator.NextBoolean();
        direct.NextUInt64();
        byte[] beforeFailure = generator.ExportState().ToBytes();

        Assert.Throws<RandomCapabilityException>(() => generator.Jump());
        Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(beforeFailure));

        generator.Advance((UInt128)1);
        direct.NextUInt64();
        Assert.That(generator.NextUInt64(), Is.EqualTo(direct.NextUInt64()));
    }

    [Test]
    public void InvalidBitCountsAndWrappedReservoirsAreRejectedAtomically()
    {
        RandomGenerator generator = RandomGenerator.CreateSplitMix64(321UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        byte[] payload = generator.ExportState().Payload();
        BinaryCodec.WriteUInt32(payload, 24, 65U);
        RandomState invalid = new RandomState(RandomAlgorithm.SplitMix64V1, 1, 1, payload);

        Assert.Multiple(() =>
        {
            RandomArgumentException negative = ReflectionInfo.Required<RandomArgumentException>(Assert.Throws<RandomArgumentException>(() => generator.NextBits(-1)));
            RandomArgumentException large = ReflectionInfo.Required<RandomArgumentException>(Assert.Throws<RandomArgumentException>(() => generator.NextBits(65)));
            Assert.That(negative.ArgumentName(), Is.EqualTo("bitCount"));
            Assert.That(large.ArgumentName(), Is.EqualTo("bitCount"));
            Assert.Throws<RandomStateException>(() => generator.RestoreState(invalid));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    static void WriteLowBytes(byte[] destination, int offset, ulong value, int count)
    {
        for (int index = 0; index < count; index++)
        {
            destination[offset + index] = (byte)(value & 0xFFUL);
            value >>= 8;
        }
    }
}
