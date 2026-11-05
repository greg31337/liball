namespace random.Tests;

public class RandomGeneratorPositionTests
{
    [Test]
    public void PcgAdvancePreservesZeroAndSatisfiesComposition()
    {
        RandomGenerator split = RandomGenerator.CreatePcg64Dxsm((UInt128)123, (UInt128)456, RandomMathMode.Portable);
        RandomGenerator combined = split.Clone();
        byte[] initial = split.ExportState().ToBytes();
        split.Advance((UInt128)0);
        Assert.That(split.ExportState().ToBytes(), Is.EqualTo(initial));

        UInt128 first = ((UInt128)1 << 100) + (UInt128)17;
        UInt128 second = ((UInt128)1 << 80) + (UInt128)29;
        split.Advance(first);
        split.Advance(second);
        combined.Advance(first + second);

        Assert.Multiple(() =>
        {
            Assert.That(split.ExportState().ToBytes(), Is.EqualTo(combined.ExportState().ToBytes()));
            Assert.That(split.NextRawWord(), Is.EqualTo(combined.NextRawWord()));
        });
    }

    [Test]
    public void AdvanceHandlesMaximumDistanceForEachSupportingEngine()
    {
        RandomGenerator pcg = RandomGenerator.CreatePcg64Dxsm((UInt128)1, (UInt128)2, RandomMathMode.Portable);
        Pcg64DxsmEngine directPcg = Pcg64DxsmEngine.FromSeed((UInt128)1, (UInt128)2);
        pcg.Advance(UInt128.MaxValue);
        directPcg.Advance(UInt128.MaxValue);

        RandomGenerator splitMix = RandomGenerator.CreateSplitMix64(3UL, RandomMathMode.Portable);
        SplitMix64Engine directSplitMix = SplitMix64Engine.FromSeed(3UL);
        splitMix.Advance(UInt128.MaxValue);
        directSplitMix.Advance(ulong.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.That(pcg.NextRawWord(), Is.EqualTo(directPcg.NextUInt64()));
            Assert.That(splitMix.NextRawWord(), Is.EqualTo(directSplitMix.NextUInt64()));
        });
    }

    [Test]
    public void SplitMixAdvanceIgnoresCompleteCyclesInUpperDistanceBits()
    {
        RandomGenerator withCycles = RandomGenerator.CreateSplitMix64(4UL, RandomMathMode.Portable);
        RandomGenerator reduced = withCycles.Clone();
        UInt128 distance = ((UInt128)ulong.MaxValue << 64) + (UInt128)12345;
        withCycles.Advance(distance);
        reduced.Advance((UInt128)12345);

        Assert.That(withCycles.ExportState().ToBytes(), Is.EqualTo(reduced.ExportState().ToBytes()));
    }

    [Test]
    public void SeekToUInt128BoundaryClearsBufferedPhiloxBlock()
    {
        RandomGenerator generator = RandomGenerator.CreatePhilox4x64Ten((UInt128)5, RandomMathMode.Portable);
        generator.NextRawWord();
        generator.NextRawWord();
        generator.Seek(UInt128.MaxValue);
        Philox4x64TenEngine direct = Philox4x64TenEngine.FromKey((UInt128)5);
        direct.Seek(UInt128.MaxValue);
        Philox4x64TenEngine snapshot = Philox4x64TenEngine.FromState(generator.ExportState());

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.CounterWords(), Is.EqualTo(new ulong[] { ulong.MaxValue, ulong.MaxValue, 0UL, 0UL }));
            Assert.That(snapshot.BufferedLaneIndex(), Is.EqualTo(4));
            Assert.That(generator.NextRawWord(), Is.EqualTo(direct.NextUInt64()));
        });
    }

    [Test]
    public void FullCounterSeekMakesFinalPhiloxBlockAvailableThenExhausts()
    {
        RandomGenerator generator = RandomGenerator.CreatePhilox4x64Ten((UInt128)6, RandomMathMode.Portable);
        ulong[] maximum = [ulong.MaxValue, ulong.MaxValue, ulong.MaxValue, ulong.MaxValue];
        generator.SeekCounter(maximum);
        Philox4x64TenEngine direct = Philox4x64TenEngine.FromKey((UInt128)6);
        direct.SeekCounter(maximum);

        for (int lane = 0; lane < 4; lane++)
        {
            Assert.That(generator.NextRawWord(), Is.EqualTo(direct.NextUInt64()));
        }

        Assert.Throws<RandomExhaustedException>(() => generator.NextRawWord());
    }

    [Test]
    public void PhiloxBlockReservationReturnsRangeStartAndSkipsReservedBlocks()
    {
        ulong[] key = [101UL, 202UL];
        RandomFactory factory = new RandomFactory();
        RandomGenerator generator = factory.CreatePhilox4x64TenAtCounter(key, [0UL, 0UL, 0UL, 0UL], RandomMathMode.Portable);
        ulong[] firstBlock = Philox4x64TenEngine.GenerateBlock([0UL, 0UL, 0UL, 0UL], key);
        Assert.That(generator.NextRawWord(), Is.EqualTo(firstBlock[0]));

        byte[] beforeZero = generator.ExportState().ToBytes();
        ulong[] zeroStart = generator.ReserveBlocks((UInt128)0);
        Assert.Multiple(() =>
        {
            Assert.That(zeroStart, Is.EqualTo(new ulong[] { 1UL, 0UL, 0UL, 0UL }));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(beforeZero));
        });

        ulong[] reservedStart = generator.ReserveBlocks((UInt128)2);
        Assert.That(reservedStart, Is.EqualTo(new ulong[] { 1UL, 0UL, 0UL, 0UL }));
        reservedStart[0] = 99UL;
        Philox4x64TenEngine snapshot = Philox4x64TenEngine.FromState(generator.ExportState());

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.CounterWords(), Is.EqualTo(new ulong[] { 3UL, 0UL, 0UL, 0UL }));
            Assert.That(generator.NextRawWord(), Is.EqualTo(firstBlock[1]));
            Assert.That(generator.NextRawWord(), Is.EqualTo(firstBlock[2]));
            Assert.That(generator.NextRawWord(), Is.EqualTo(firstBlock[3]));
            Assert.That(generator.NextRawWord(), Is.EqualTo(Philox4x64TenEngine.GenerateBlock([3UL, 0UL, 0UL, 0UL], key)[0]));
        });
    }

    [Test]
    public void PhiloxReservationRejectsCounterOverflowBeforeMutation()
    {
        RandomFactory factory = new RandomFactory();
        ulong[] maximum = [ulong.MaxValue, ulong.MaxValue, ulong.MaxValue, ulong.MaxValue];
        RandomGenerator exact = factory.CreatePhilox4x64TenAtCounter([1UL, 2UL], maximum, RandomMathMode.Portable);
        Assert.That(exact.ReserveBlocks((UInt128)1), Is.EqualTo(maximum));
        Assert.Throws<RandomExhaustedException>(() => exact.NextRawWord());

        RandomGenerator overflow = factory.CreatePhilox4x64TenAtCounter([1UL, 2UL], maximum, RandomMathMode.Portable);
        byte[] before = overflow.ExportState().ToBytes();
        Assert.Throws<RandomExhaustedException>(() => overflow.ReserveBlocks((UInt128)2));
        Assert.That(overflow.ExportState().ToBytes(), Is.EqualTo(before));
    }

    [Test]
    public void JumpAndLongJumpRouteExactlyAndCommute()
    {
        RandomGenerator first = RandomGenerator.CreateXoshiro256StarStar(7UL, RandomMathMode.Portable);
        RandomGenerator second = first.Clone();
        Xoshiro256StarStarEngine direct = Xoshiro256StarStarEngine.FromSeed(7UL);
        first.Jump();
        first.LongJump();
        second.LongJump();
        second.Jump();
        direct.Jump();
        direct.LongJump();
        ulong expected = direct.NextUInt64();

        Assert.Multiple(() =>
        {
            Assert.That(first.ExportState().ToBytes(), Is.EqualTo(second.ExportState().ToBytes()));
            Assert.That(first.NextRawWord(), Is.EqualTo(expected));
            Assert.That(second.NextRawWord(), Is.EqualTo(expected));
        });
    }

    [Test]
    public void PositioningDoesNotChangeTheFactoryResetPoint()
    {
        RandomGenerator generator = RandomGenerator.CreateXoshiro256StarStar(8UL, RandomMathMode.Portable);
        ulong initial = generator.NextRawWord();
        generator.Jump();
        generator.LongJump();
        generator.Reset();

        Assert.That(generator.NextRawWord(), Is.EqualTo(initial));
    }

    [Test]
    public void UnsupportedFacadeOperationsReportCapabilityWithoutMutation()
    {
        RandomGenerator generator = RandomGenerator.CreateMt19937(9U, RandomMathMode.Portable);
        byte[] before = generator.ExportState().ToBytes();
        object[] cases =
        [
            new object[] { (TestDelegate)(() => generator.Advance((UInt128)1)), RandomCapability.Advance },
            new object[] { (TestDelegate)(() => generator.Seek((UInt128)1)), RandomCapability.Seek },
            new object[] { (TestDelegate)(() => generator.SeekCounter((ulong[])NullReference.Value<ulong[]>())), RandomCapability.Seek },
            new object[] { (TestDelegate)(() => generator.ReserveBlocks((UInt128)1)), RandomCapability.BlockReservation },
            new object[] { (TestDelegate)(() => generator.Jump()), RandomCapability.Jump },
            new object[] { (TestDelegate)(() => generator.LongJump()), RandomCapability.LongJump }
        ];

        foreach (object[] testCase in cases)
        {
            RandomCapabilityException error = Assert.Throws<RandomCapabilityException>((TestDelegate)testCase[0]);
            Assert.Multiple(() =>
            {
                Assert.That(error.Algorithm(), Is.EqualTo(RandomAlgorithm.Mt19937V1));
                Assert.That(error.Capability(), Is.EqualTo((RandomCapability)testCase[1]));
                Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
            });
        }
    }

    [Test]
    public void InvalidFullCounterIsRejectedBeforePhiloxMutation()
    {
        RandomGenerator generator = RandomGenerator.CreatePhilox4x64Ten((UInt128)10, RandomMathMode.Portable);
        generator.NextRawWord();
        byte[] before = generator.ExportState().ToBytes();

        Assert.Multiple(() =>
        {
            RandomArgumentException nullError = ReflectionInfo.Required<RandomArgumentException>(Assert.Throws<RandomArgumentException>(() => generator.SeekCounter((ulong[])NullReference.Value<ulong[]>())));
            RandomArgumentException lengthError = ReflectionInfo.Required<RandomArgumentException>(Assert.Throws<RandomArgumentException>(() => generator.SeekCounter([1UL, 2UL, 3UL])));
            Assert.That(nullError.ArgumentName(), Is.EqualTo("absoluteCounter"));
            Assert.That(lengthError.ArgumentName(), Is.EqualTo("absoluteCounter"));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }
}
