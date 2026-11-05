namespace random.Tests;

public class Philox4x64TenEngineTests
{
    [Test]
    public void Random123KnownAnswerVectorsMatchAllThreeCanonicalCases()
    {
        // Provenance: Random123's canonical philox4x64 10 known-answer rows.
        // https://github.com/DEShawResearch/random123/blob/main/tests/kat_vectors
        Assert.That(
            Philox4x64TenEngine.GenerateBlock([0UL, 0UL, 0UL, 0UL], [0UL, 0UL]),
            Is.EqualTo(new ulong[] { 0x16554D9ECA36314CUL, 0xDB20FE9D672D0FDCUL, 0xD7E772CEE186176BUL, 0x7E68B68AEC7BA23BUL }));

        Assert.That(
            Philox4x64TenEngine.GenerateBlock([ulong.MaxValue, ulong.MaxValue, ulong.MaxValue, ulong.MaxValue], [ulong.MaxValue, ulong.MaxValue]),
            Is.EqualTo(new ulong[] { 0x87B092C3013FE90BUL, 0x438C3C67BE8D0224UL, 0x9CC7D7C69CD777B6UL, 0xA09CAEBF594F0BA0UL }));

        Assert.That(
            Philox4x64TenEngine.GenerateBlock(
                [0x243F6A8885A308D3UL, 0x13198A2E03707344UL, 0xA4093822299F31D0UL, 0x082EFA98EC4E6C89UL],
                [0x452821E638D01377UL, 0xBE5466CF34E90C6CUL]),
            Is.EqualTo(new ulong[] { 0xA528F45403E61D95UL, 0x38C72DBD566E9788UL, 0xA5A1610E72FD18B5UL, 0x57BD43B5E52B7FE6UL }));
    }

    [Test]
    public void SequentialOutputBuffersFourLanesAndThenIncrementsCounter()
    {
        ulong[] firstBlock = Philox4x64TenEngine.GenerateBlock([0UL, 0UL, 0UL, 0UL], [0UL, 0UL]);
        ulong[] secondBlock = Philox4x64TenEngine.GenerateBlock([1UL, 0UL, 0UL, 0UL], [0UL, 0UL]);
        Philox4x64TenEngine engine = Philox4x64TenEngine.FromKey((UInt128)0);

        Assert.That(engine.CounterWords(), Is.EqualTo(new ulong[] { 0UL, 0UL, 0UL, 0UL }));
        Assert.That(engine.NextUInt64(), Is.EqualTo(firstBlock[0]));
        Assert.Multiple(() =>
        {
            Assert.That(engine.CounterWords(), Is.EqualTo(new ulong[] { 1UL, 0UL, 0UL, 0UL }));
            Assert.That(engine.BufferedLaneIndex(), Is.EqualTo(1));
        });
        Assert.That(engine.NextUInt64(), Is.EqualTo(firstBlock[1]));
        Assert.That(engine.NextUInt64(), Is.EqualTo(firstBlock[2]));
        Assert.That(engine.NextUInt64(), Is.EqualTo(firstBlock[3]));
        Assert.That(engine.BufferedLaneIndex(), Is.EqualTo(4));
        Assert.That(engine.NextUInt64(), Is.EqualTo(secondBlock[0]));
    }

    [Test]
    public void KeyAndCounterInputsAreCopiedAndUseLittleEndianWordOrder()
    {
        ulong[] key = [0x0123456789ABCDEFUL, 0xFEDCBA9876543210UL];
        ulong[] counter = [1UL, 2UL, 3UL, 4UL];
        Philox4x64TenEngine engine = Philox4x64TenEngine.FromKeyAndCounter(key, counter);
        key[0] = 0UL;
        counter[0] = 0UL;

        Assert.Multiple(() =>
        {
            Assert.That(engine.KeyWords(), Is.EqualTo(new ulong[] { 0x0123456789ABCDEFUL, 0xFEDCBA9876543210UL }));
            Assert.That(engine.CounterWords(), Is.EqualTo(new ulong[] { 1UL, 2UL, 3UL, 4UL }));
            Assert.That(engine.StatePayload().AsSpan(0, 8).ToArray(), Is.EqualTo(new byte[] { 1, 0, 0, 0, 0, 0, 0, 0 }));
            Assert.That(engine.StatePayload().AsSpan(32, 8).ToArray(), Is.EqualTo(new byte[] { 0xEF, 0xCD, 0xAB, 0x89, 0x67, 0x45, 0x23, 0x01 }));
        });
    }

    [Test]
    public void MidBlockStateRoundTripAndCloneResumeAtExactLane()
    {
        Philox4x64TenEngine original = Philox4x64TenEngine.FromKeyAndCounter([11UL, 22UL], [33UL, 44UL, 55UL, 66UL]);
        original.NextUInt64();
        original.NextUInt64();
        RandomState snapshot = original.ExportState();
        Philox4x64TenEngine restored = Philox4x64TenEngine.FromState(snapshot);
        Philox4x64TenEngine clone = original.Clone();

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.Payload().Length, Is.EqualTo(Philox4x64TenEngine.StateByteCount));
            Assert.That(restored.BufferedLaneIndex(), Is.EqualTo(2));
            Assert.That(clone.BufferedLaneIndex(), Is.EqualTo(2));
        });

        for (int index = 0; index < 10; index++)
        {
            ulong expected = original.NextUInt64();
            Assert.That(restored.NextUInt64(), Is.EqualTo(expected));
            Assert.That(clone.NextUInt64(), Is.EqualTo(expected));
        }
    }

    [Test]
    public void AbsoluteSeekDiscardsBufferedLanesAndUsesRequestedBlock()
    {
        ulong[] key = [7UL, 9UL];
        Philox4x64TenEngine engine = Philox4x64TenEngine.FromKeyAndCounter(key, [0UL, 0UL, 0UL, 0UL]);
        engine.NextUInt64();
        engine.Seek((UInt128)5);
        ulong[] fifth = Philox4x64TenEngine.GenerateBlock([5UL, 0UL, 0UL, 0UL], key);

        Assert.Multiple(() =>
        {
            Assert.That(engine.BufferedLaneIndex(), Is.EqualTo(4));
            Assert.That(engine.CounterWords(), Is.EqualTo(new ulong[] { 5UL, 0UL, 0UL, 0UL }));
            Assert.That(engine.NextUInt64(), Is.EqualTo(fifth[0]));
        });

        ulong[] absolute = [0xFFFFFFFFFFFFFFFEUL, 2UL, 3UL, 4UL];
        engine.SeekCounter(absolute);
        Assert.That(engine.NextUInt64(), Is.EqualTo(Philox4x64TenEngine.GenerateBlock(absolute, key)[0]));
    }

    [Test]
    public void BlockReservationReturnsStartAndSkipsReservedBlocks()
    {
        ulong[] key = [101UL, 202UL];
        Philox4x64TenEngine parent = Philox4x64TenEngine.FromKeyAndCounter(key, [0UL, 0UL, 0UL, 0UL]);
        ulong[] start = parent.ReserveBlocks((UInt128)3);
        Philox4x64TenEngine reserved = Philox4x64TenEngine.FromKeyAndCounter(key, start);

        Assert.Multiple(() =>
        {
            Assert.That(start, Is.EqualTo(new ulong[] { 0UL, 0UL, 0UL, 0UL }));
            Assert.That(parent.CounterWords(), Is.EqualTo(new ulong[] { 3UL, 0UL, 0UL, 0UL }));
            Assert.That(reserved.NextUInt64(), Is.EqualTo(Philox4x64TenEngine.GenerateBlock(start, key)[0]));
            Assert.That(parent.NextUInt64(), Is.EqualTo(Philox4x64TenEngine.GenerateBlock([3UL, 0UL, 0UL, 0UL], key)[0]));
        });
    }

    [Test]
    public void ReservationDuringBufferedBlockPreservesRemainingLanes()
    {
        ulong[] key = [303UL, 404UL];
        Philox4x64TenEngine engine = Philox4x64TenEngine.FromKeyAndCounter(key, [0UL, 0UL, 0UL, 0UL]);
        ulong[] firstBlock = Philox4x64TenEngine.GenerateBlock([0UL, 0UL, 0UL, 0UL], key);
        engine.NextUInt64();
        ulong[] reservedStart = engine.ReserveBlocks((UInt128)2);

        Assert.That(reservedStart, Is.EqualTo(new ulong[] { 1UL, 0UL, 0UL, 0UL }));
        Assert.That(engine.NextUInt64(), Is.EqualTo(firstBlock[1]));
        Assert.That(engine.NextUInt64(), Is.EqualTo(firstBlock[2]));
        Assert.That(engine.NextUInt64(), Is.EqualTo(firstBlock[3]));
        Assert.That(engine.NextUInt64(), Is.EqualTo(Philox4x64TenEngine.GenerateBlock([3UL, 0UL, 0UL, 0UL], key)[0]));
    }

    [Test]
    public void FinalCounterBlockIsFullyAvailableAndThenExhausts()
    {
        ulong[] maximum = [ulong.MaxValue, ulong.MaxValue, ulong.MaxValue, ulong.MaxValue];
        ulong[] key = [5UL, 6UL];
        ulong[] expected = Philox4x64TenEngine.GenerateBlock(maximum, key);
        Philox4x64TenEngine engine = Philox4x64TenEngine.FromKeyAndCounter(key, maximum);

        for (int index = 0; index < 4; index++)
        {
            Assert.That(engine.NextUInt64(), Is.EqualTo(expected[index]));
        }

        Assert.Multiple(() =>
        {
            Assert.That(engine.CounterExhausted(), Is.True);
            Assert.That(engine.CounterWords(), Is.EqualTo(maximum));
            Assert.Throws<RandomExhaustedException>(() => engine.NextUInt64());
        });
    }

    [Test]
    public void ReservationChecksCounterBoundaryBeforeMutation()
    {
        ulong[] maximum = [ulong.MaxValue, ulong.MaxValue, ulong.MaxValue, ulong.MaxValue];
        Philox4x64TenEngine exact = Philox4x64TenEngine.FromKeyAndCounter([0UL, 0UL], maximum);
        Assert.That(exact.ReserveBlocks((UInt128)1), Is.EqualTo(maximum));
        Assert.That(exact.CounterExhausted(), Is.True);
        Assert.Throws<RandomExhaustedException>(() => exact.NextUInt64());

        Philox4x64TenEngine overflow = Philox4x64TenEngine.FromKeyAndCounter([0UL, 0UL], maximum);
        ulong[] before = overflow.CounterWords();
        Assert.Throws<RandomExhaustedException>(() => overflow.ReserveBlocks((UInt128)2));
        Assert.Multiple(() =>
        {
            Assert.That(overflow.CounterWords(), Is.EqualTo(before));
            Assert.That(overflow.CounterExhausted(), Is.False);
        });
    }

    [Test]
    public void InvalidSerializedStateIsRejectedAtomically()
    {
        Philox4x64TenEngine engine = Philox4x64TenEngine.FromKeyAndCounter([1UL, 2UL], [3UL, 4UL, 5UL, 6UL]);
        byte[] before = engine.StatePayload();
        byte[] badIndex = (byte[])before.Clone();
        BinaryCodec.WriteUInt32(badIndex, 80, 5U);
        byte[] badFlag = (byte[])before.Clone();
        BinaryCodec.WriteByte(badFlag, 84, 2);
        byte[] badReserved = (byte[])before.Clone();
        BinaryCodec.WriteByte(badReserved, 87, 1);
        byte[] badBuffer = (byte[])before.Clone();
        BinaryCodec.WriteUInt64(badBuffer, 48, 1UL);
        byte[] badExhaustedCounter = (byte[])before.Clone();
        BinaryCodec.WriteByte(badExhaustedCounter, 84, 1);
        RandomState wrongAlgorithm = StateCodec.Create(RandomAlgorithm.SplitMix64V1, 1, 1, before);

        Assert.Multiple(() =>
        {
            Assert.Throws<RandomStateException>(() => engine.RestoreStatePayload(badIndex));
            Assert.Throws<RandomStateException>(() => engine.RestoreStatePayload(badFlag));
            Assert.Throws<RandomStateException>(() => engine.RestoreStatePayload(badReserved));
            Assert.Throws<RandomStateException>(() => engine.RestoreStatePayload(badBuffer));
            Assert.Throws<RandomStateException>(() => engine.RestoreStatePayload(badExhaustedCounter));
            Assert.Throws<RandomStateException>(() => engine.RestoreStatePayload(new byte[87]));
            Assert.Throws<RandomStateException>(() => engine.RestoreState(wrongAlgorithm));
        });

        Assert.That(engine.StatePayload(), Is.EqualTo(before));
    }

    [Test]
    public void MetadataDeclaresCounterCapabilitiesAndNonSecurity()
    {
        RandomAlgorithmInfo info = Philox4x64TenEngine.FromKey((UInt128)0).Info();

        Assert.Multiple(() =>
        {
            Assert.That(info.Algorithm(), Is.EqualTo(RandomAlgorithm.Philox4x64TenV1));
            Assert.That(info.Name(), Is.EqualTo("Philox4x64-10"));
            Assert.That(info.Version(), Is.EqualTo(1));
            Assert.That(info.WordBits(), Is.EqualTo(64));
            Assert.That(info.StateBytes(), Is.EqualTo(88));
            Assert.That(info.IsCryptographic(), Is.False);
            Assert.That(info.Capabilities(), Is.EqualTo(new[] { RandomCapability.State, RandomCapability.Clone, RandomCapability.Seek, RandomCapability.BlockReservation }));
            Assert.That(info.HasCapability(RandomCapability.Jump), Is.False);
            Assert.That(info.HasCapability(RandomCapability.Reseed), Is.False);
        });
    }
}
