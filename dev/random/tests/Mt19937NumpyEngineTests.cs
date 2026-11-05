namespace random.Tests;

public class Mt19937NumpyEngineTests
{
    [Test]
    public void NumpySeedSequenceMatchesPublishedReferenceData()
    {
        // Provenance: NumPy's published SeedSequence reference vectors.
        // https://github.com/numpy/numpy/blob/main/numpy/random/tests/test_seed_sequence.py
        uint[] entropy = [3735928559U, 195939070U, 229505742U, 305419896U];
        uint[] expected = [3914649087U, 576849849U, 3593928901U, 2229911004U];

        Assert.That(NumpySeedSequence.GenerateState(entropy, 4), Is.EqualTo(expected));
    }

    [Test]
    public void ScalarSeedZeroMatchesNumpyMt19937Fixture()
    {
        // Provenance: NumPy's checked-in mt19937-testset-2.csv (seed 0x0).
        // https://github.com/numpy/numpy/blob/main/numpy/random/tests/data/mt19937-testset-2.csv
        Mt19937NumpyEngine engine = Mt19937NumpyEngine.FromSeed(0U);
        uint[] expected =
        [
            0x7AB4EA94U,
            0x9B561119U,
            0x4957D02EU,
            0x7DD3FDC2U,
            0x05AFFE54U,
            0x5A01741CU,
            0x8B9E8C1FU,
            0xDA5BF11AU,
            0x00509226U,
            0x64E2EA17U,
            0x82C6DAB5U,
            0xE4302515U
        ];

        foreach (uint value in expected)
        {
            Assert.That(engine.NextUInt32(), Is.EqualTo(value));
        }
    }

    [Test]
    public void ScalarSeedDeadbeafMatchesNumpyMt19937Fixture()
    {
        // Provenance: NumPy's checked-in mt19937-testset-1.csv (seed 0xDEADBEAF).
        // https://github.com/numpy/numpy/blob/main/numpy/random/tests/data/mt19937-testset-1.csv
        Mt19937NumpyEngine engine = Mt19937NumpyEngine.FromSeed(0xDEADBEAFU);
        uint[] expected =
        [
            0xC816921FU,
            0xB3623C6DU,
            0x5FA391BBU,
            0x040178D9U,
            0x7DCC9811U,
            0x548EB8E6U,
            0x92BA3125U,
            0x65FDE68DU,
            0x2F81EC95U,
            0xBD94F7A2U,
            0xDC4D9BCCU,
            0xA672BF13U
        ];

        foreach (uint value in expected)
        {
            Assert.That(engine.NextUInt32(), Is.EqualTo(value));
        }
    }

    [Test]
    public void ArrayEntropyMatchesNumpyMt19937Fixture()
    {
        // Provenance: NumPy 2.0.2, np.random.MT19937([0x123, 0x234, 0x345, 0x456]).random_raw(12).
        // Seed handling is implemented by https://github.com/numpy/numpy/blob/main/numpy/random/bit_generator.pyx.
        Mt19937NumpyEngine engine = Mt19937NumpyEngine.FromSeedArray([0x123U, 0x234U, 0x345U, 0x456U]);
        uint[] expected =
        [
            0x2238C27FU,
            0xF4A396BCU,
            0xF77A0A82U,
            0x200A8172U,
            0x17D26AE3U,
            0xAD6B05F6U,
            0x12D8F239U,
            0xB86A4C39U,
            0x7140C951U,
            0x7524EDF1U,
            0xF3191A72U,
            0x81D7C366U
        ];

        foreach (uint value in expected)
        {
            Assert.That(engine.NextUInt32(), Is.EqualTo(value));
        }
    }

    [Test]
    public void EmptyArrayMatchesNumpyZeroEntropyConvention()
    {
        Mt19937NumpyEngine empty = Mt19937NumpyEngine.FromSeedArray([]);
        Mt19937NumpyEngine zero = Mt19937NumpyEngine.FromSeed(0U);

        Assert.That(empty.StateWords(), Is.EqualTo(zero.StateWords()));
        Assert.That(empty.NextUInt32(), Is.EqualTo(zero.NextUInt32()));
    }

    [Test]
    public void SeededStateUsesNumpyFirstWordAndInitialPosition()
    {
        Mt19937NumpyEngine engine = Mt19937NumpyEngine.FromSeed(42U);
        uint[] words = engine.StateWords();

        Assert.Multiple(() =>
        {
            Assert.That(engine.StateIndex(), Is.EqualTo(623));
            Assert.That(words[0], Is.EqualTo(0x80000000U));
            Assert.That(words.AsSpan(1, 7).ToArray(), Is.EqualTo(new uint[]
            {
                2669555309U,
                2046530742U,
                3581440988U,
                1691623607U,
                2099784219U,
                1184028159U,
                862288241U
            }));
        });
    }

    [Test]
    public void DirectNumpyStateImportPreservesKeyAndPosition()
    {
        Mt19937NumpyEngine seeded = Mt19937NumpyEngine.FromSeed(42U);
        uint[] key = seeded.StateWords();
        Mt19937NumpyEngine imported = Mt19937NumpyEngine.FromNumpyState(key, 0);
        key[0] = 0U;
        uint[] expected =
        [
            0x88102204U,
            0xD7EDAC53U,
            0x8532C888U,
            0x0A0E9E51U,
            0x2D94A1D6U,
            0x28A58337U,
            0x61184D63U,
            0x0BAD5135U
        ];

        Assert.That(imported.StateIndex(), Is.EqualTo(0));
        foreach (uint value in expected)
        {
            Assert.That(imported.NextUInt32(), Is.EqualTo(value));
        }
    }

    [Test]
    public void NumpyStateSnapshotsRoundTripUnderSeparateAlgorithmIdentity()
    {
        Mt19937NumpyEngine original = Mt19937NumpyEngine.FromSeed(123456789U);
        for (int draw = 0; draw < 700; draw++)
        {
            original.NextUInt32();
        }

        RandomState snapshot = original.ExportState();
        Mt19937NumpyEngine restored = Mt19937NumpyEngine.FromState(snapshot);
        Mt19937NumpyEngine clone = original.Clone();
        uint expected = original.NextUInt32();

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.Algorithm(), Is.EqualTo(RandomAlgorithm.Mt19937NumpyV1));
            Assert.That(snapshot.Payload().Length, Is.EqualTo(2500));
            Assert.That(restored.StateIndex(), Is.EqualTo(original.StateIndex() - 1));
            Assert.That(restored.NextUInt32(), Is.EqualTo(expected));
            Assert.That(clone.NextUInt32(), Is.EqualTo(expected));
        });
    }

    [Test]
    public void ReferenceAndNumpyModesRejectEachOthersSnapshotsWithoutMutation()
    {
        Mt19937NumpyEngine numpy = Mt19937NumpyEngine.FromSeed(42U);
        Mt19937Engine reference = Mt19937Engine.FromSeed(42U);
        byte[] numpyBefore = numpy.StatePayload();
        byte[] referenceBefore = reference.StatePayload();

        Assert.Multiple(() =>
        {
            Assert.Throws<RandomStateException>(() => numpy.RestoreState(reference.ExportState()));
            Assert.Throws<RandomStateException>(() => reference.RestoreState(numpy.ExportState()));
            Assert.That(numpy.StatePayload(), Is.EqualTo(numpyBefore));
            Assert.That(reference.StatePayload(), Is.EqualTo(referenceBefore));
            Assert.That(numpy.NextUInt32(), Is.Not.EqualTo(reference.NextUInt32()));
        });
    }

    [Test]
    public void InvalidInputsAndStatesAreRejectedBeforeMutation()
    {
        Mt19937NumpyEngine engine = Mt19937NumpyEngine.FromSeed(7U);
        byte[] before = engine.StatePayload();
        byte[] invalidPosition = (byte[])before.Clone();
        BinaryCodec.WriteUInt32(invalidPosition, 0, 625U);

        Assert.Multiple(() =>
        {
            RandomArgumentException seedError = ReflectionInfo.Required<RandomArgumentException>(Assert.Throws<RandomArgumentException>(() => Mt19937NumpyEngine.FromSeedArray(NullReference.Value<uint[]>())));
            Assert.That(seedError.ArgumentName(), Is.EqualTo("seed"));
            Assert.Throws<RandomStateException>(() => Mt19937NumpyEngine.FromNumpyState(new uint[624], 0));
            Assert.Throws<RandomStateException>(() => Mt19937NumpyEngine.FromNumpyState(engine.StateWords(), 625));
            Assert.Throws<RandomStateException>(() => engine.RestoreStatePayload(invalidPosition));
            Assert.Throws<RandomStateException>(() => engine.RestoreStatePayload(new byte[2499]));
            Assert.That(engine.StatePayload(), Is.EqualTo(before));
        });
    }

    [Test]
    public void MetadataDeclaresSeparateNumpyCompatibilityIdentity()
    {
        RandomAlgorithmInfo info = Mt19937NumpyEngine.FromSeed(0U).Info();

        Assert.Multiple(() =>
        {
            Assert.That(info.Algorithm(), Is.EqualTo(RandomAlgorithm.Mt19937NumpyV1));
            Assert.That(info.Name(), Is.EqualTo("MT19937 (NumPy)"));
            Assert.That(info.Version(), Is.EqualTo(1));
            Assert.That(info.WordBits(), Is.EqualTo(32));
            Assert.That(info.StateBytes(), Is.EqualTo(2500));
            Assert.That(info.IsCryptographic(), Is.False);
            Assert.That(info.Capabilities(), Is.EqualTo(new[] { RandomCapability.State, RandomCapability.Clone }));
        });
    }
}
