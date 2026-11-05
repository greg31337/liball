namespace random.Tests;

public class Mt19937EngineTests
{
    [Test]
    public void ScalarSeedMatchesReferenceVector()
    {
        // Provenance: Matsumoto and Nishimura's 2002 mt19937ar reference output for seed 5489.
        // https://www.math.sci.hiroshima-u.ac.jp/m-mat/MT/MT2002/mt19937ar.out
        Mt19937Engine engine = Mt19937Engine.FromSeed(5489U);
        uint[] expected =
        [
            3499211612U,
            581869302U,
            3890346734U,
            3586334585U,
            545404204U,
            4161255391U,
            3922919429U,
            949333985U,
            2715962298U,
            1323567403U
        ];

        foreach (uint value in expected)
        {
            Assert.That(engine.NextUInt32(), Is.EqualTo(value));
        }
    }

    [Test]
    public void ArraySeedMatchesPublishedReferenceVector()
    {
        // Provenance: Matsumoto and Nishimura's init_by_array reference output.
        // https://www.math.sci.hiroshima-u.ac.jp/m-mat/MT/MT2002/mt19937ar.out
        Mt19937Engine engine = Mt19937Engine.FromSeedArray([0x123U, 0x234U, 0x345U, 0x456U]);
        uint[] expected =
        [
            1067595299U,
            955945823U,
            477289528U,
            4107218783U,
            4228976476U,
            3344332714U,
            3355579695U,
            227628506U,
            810200273U,
            2591290167U,
            2560260675U,
            3242736208U,
            646746669U,
            1479517882U,
            4245472273U,
            1143372638U,
            3863670494U,
            3221021970U,
            1773610557U,
            1138697238U
        ];

        foreach (uint value in expected)
        {
            Assert.That(engine.NextUInt32(), Is.EqualTo(value));
        }
    }

    [Test]
    public void ScalarAndArrayInitializationRemainDistinctReferenceModes()
    {
        Mt19937Engine scalar = Mt19937Engine.FromSeed(5489U);
        Mt19937Engine array = Mt19937Engine.FromSeedArray([5489U]);

        Assert.That(array.NextUInt32(), Is.Not.EqualTo(scalar.NextUInt32()));
    }

    [Test]
    public void StatePayloadIncludesAllWordsAndPositionInLittleEndian()
    {
        Mt19937Engine original = Mt19937Engine.FromSeed(0x12345678U);
        for (int draw = 0; draw < 700; draw++)
        {
            original.NextUInt32();
        }

        byte[] payload = original.StatePayload();
        Mt19937Engine fromPayload = Mt19937Engine.FromStatePayload(payload);
        RandomState snapshot = original.ExportState();
        Mt19937Engine fromSnapshot = Mt19937Engine.FromState(snapshot);
        uint[] words = original.StateWords();
        uint expected = original.NextUInt32();

        Assert.Multiple(() =>
        {
            Assert.That(payload.Length, Is.EqualTo(2500));
            Assert.That(payload.AsSpan(0, 4).ToArray(), Is.EqualTo(new byte[] { 76, 0, 0, 0 }));
            Assert.That(BinaryCodec.ReadUInt32(payload, 4), Is.EqualTo(words[0]));
            Assert.That(BinaryCodec.ReadUInt32(payload, 2496), Is.EqualTo(words[623]));
            Assert.That(snapshot.Algorithm(), Is.EqualTo(RandomAlgorithm.Mt19937V1));
            Assert.That(fromPayload.StateIndex(), Is.EqualTo(76));
            Assert.That(fromPayload.NextUInt32(), Is.EqualTo(expected));
            Assert.That(fromSnapshot.NextUInt32(), Is.EqualTo(expected));
        });
    }

    [Test]
    public void SnapshotsPreservePositionsAcrossTwistBoundary()
    {
        Mt19937Engine engine = Mt19937Engine.FromSeed(42U);
        for (int draw = 0; draw < 623; draw++)
        {
            engine.NextUInt32();
        }

        Mt19937Engine beforeLastWord = Mt19937Engine.FromState(engine.ExportState());
        Assert.That(beforeLastWord.StateIndex(), Is.EqualTo(623));
        Assert.That(beforeLastWord.NextUInt32(), Is.EqualTo(engine.NextUInt32()));
        Assert.That(engine.StateIndex(), Is.EqualTo(624));

        Mt19937Engine beforeTwist = Mt19937Engine.FromState(engine.ExportState());
        Assert.That(beforeTwist.NextUInt32(), Is.EqualTo(engine.NextUInt32()));
        Assert.Multiple(() =>
        {
            Assert.That(beforeTwist.StateIndex(), Is.EqualTo(1));
            Assert.That(engine.StateIndex(), Is.EqualTo(1));
            Assert.That(beforeTwist.NextUInt32(), Is.EqualTo(engine.NextUInt32()));
        });
    }

    [Test]
    public void CloneCopiesExactPositionAndEvolvesIndependently()
    {
        Mt19937Engine original = Mt19937Engine.FromSeed(987654321U);
        for (int draw = 0; draw < 800; draw++)
        {
            original.NextUInt32();
        }

        Mt19937Engine clone = original.Clone();
        Assert.That(clone.StateWords(), Is.EqualTo(original.StateWords()));
        Assert.That(clone.StateIndex(), Is.EqualTo(original.StateIndex()));
        Assert.That(clone.NextUInt32(), Is.EqualTo(original.NextUInt32()));

        clone.NextUInt32();
        Assert.That(clone.StateIndex(), Is.Not.EqualTo(original.StateIndex()));
    }

    [Test]
    public void SeedArraysAndReturnedStateWordsAreDefensivelyCopied()
    {
        uint[] seed = [0x123U, 0x234U, 0x345U, 0x456U];
        Mt19937Engine engine = Mt19937Engine.FromSeedArray(seed);
        seed[0] = 0U;
        uint[] words = engine.StateWords();
        words[0] = 0U;

        Assert.Multiple(() =>
        {
            Assert.That(engine.NextUInt32(), Is.EqualTo(1067595299U));
            Assert.That(engine.StateWords()[0], Is.Not.EqualTo(0U));
        });
    }

    [Test]
    public void InvalidStateIsRejectedBeforeExistingEngineChanges()
    {
        Mt19937Engine engine = Mt19937Engine.FromSeed(123U);
        engine.NextUInt32();
        byte[] before = engine.StatePayload();
        byte[] badIndex = (byte[])before.Clone();
        BinaryCodec.WriteUInt32(badIndex, 0, 625U);
        byte[] zeroState = new byte[2500];
        RandomState wrongAlgorithm = StateCodec.Create(RandomAlgorithm.SplitMix64V1, 1, 1, before);
        RandomState shortPayload = StateCodec.Create(RandomAlgorithm.Mt19937V1, 1, 1, new byte[2499]);

        Assert.Multiple(() =>
        {
            Assert.Throws<RandomStateException>(() => engine.RestoreStatePayload(badIndex));
            Assert.Throws<RandomStateException>(() => engine.RestoreStatePayload(zeroState));
            Assert.Throws<RandomStateException>(() => engine.RestoreStatePayload(new byte[2501]));
            Assert.Throws<RandomStateException>(() => engine.RestoreState(wrongAlgorithm));
            Assert.Throws<RandomStateException>(() => engine.RestoreState(shortPayload));
            Assert.Throws<RandomStateException>(() => Mt19937Engine.FromStateWords(new uint[624], 0));
            Assert.Throws<RandomStateException>(() => Mt19937Engine.FromStateWords(new uint[623], 0));
            Assert.Throws<RandomStateException>(() => Mt19937Engine.FromStateWords(engine.StateWords(), -1));
            RandomArgumentException payloadError = ReflectionInfo.Required<RandomArgumentException>(Assert.Throws<RandomArgumentException>(() => engine.RestoreStatePayload(NullReference.Value<byte[]>())));
            Assert.That(payloadError.ArgumentName(), Is.EqualTo("payload"));
        });

        Assert.That(engine.StatePayload(), Is.EqualTo(before));
    }

    [Test]
    public void EmptyAndNullArraySeedsAreRejected()
    {
        Assert.Multiple(() =>
        {
            RandomArgumentException nullError = ReflectionInfo.Required<RandomArgumentException>(Assert.Throws<RandomArgumentException>(() => Mt19937Engine.FromSeedArray(NullReference.Value<uint[]>())));
            RandomArgumentException emptyError = ReflectionInfo.Required<RandomArgumentException>(Assert.Throws<RandomArgumentException>(() => Mt19937Engine.FromSeedArray([])));
            Assert.That(nullError.ArgumentName(), Is.EqualTo("seed"));
            Assert.That(emptyError.ArgumentName(), Is.EqualTo("seed"));
        });
    }

    [Test]
    public void MetadataDeclaresCompatibilityEngineIdentityAndCapabilities()
    {
        RandomAlgorithmInfo info = Mt19937Engine.FromSeed(0U).Info();

        Assert.Multiple(() =>
        {
            Assert.That(info.Algorithm(), Is.EqualTo(RandomAlgorithm.Mt19937V1));
            Assert.That(info.Name(), Is.EqualTo("MT19937"));
            Assert.That(info.Version(), Is.EqualTo(1));
            Assert.That(info.WordBits(), Is.EqualTo(32));
            Assert.That(info.StateBytes(), Is.EqualTo(2500));
            Assert.That(info.IsCryptographic(), Is.False);
            Assert.That(info.Capabilities(), Is.EqualTo(new[] { RandomCapability.State, RandomCapability.Clone }));
            Assert.That(info.HasCapability(RandomCapability.Advance), Is.False);
            Assert.That(info.HasCapability(RandomCapability.Reseed), Is.False);
        });
    }
}
