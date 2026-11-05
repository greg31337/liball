namespace random.Tests;

public class Pcg64DxsmEngineTests
{
    [Test]
    public void ImportedNumpyStateProducesOfficialPcg64DxsmVector()
    {
        // Provenance: NumPy's checked-in pcg64dxsm-testset-1.csv (seed 0xDEADBEAF)
        // and the corresponding state exported by NumPy PCG64DXSM.
        // https://github.com/numpy/numpy/blob/main/numpy/random/tests/data/pcg64dxsm-testset-1.csv
        UInt128 state = WideMath.FromParts(0xDCBF51ECA8A06D6DUL, 0xA11C0E6F95BE4591UL);
        UInt128 increment = WideMath.FromParts(0x6F01E045569D93C4UL, 0x40AC10DD049310B5UL);
        Pcg64DxsmEngine engine = Pcg64DxsmEngine.FromStateValues(state, increment);
        ulong[] expected =
        [
            0xDF1DDCF1E22521FEUL,
            0xC71B2F9C706CF151UL,
            0x6922A8CC24AD96B2UL,
            0x82738C549BECCC30UL,
            0x5E8415CDB1F17580UL,
            0x064C54AD0C09CB43UL,
            0x361A17A607DCE278UL,
            0x4346F6AFB7ACAD68UL,
            0x6E9F14D4F6398D6BUL,
            0xF818D4343F8ED822UL,
            0x6327647DAF508ED6UL,
            0xE1D1DBE5496A262AUL,
            0xFC081E619076B2E0UL,
            0x037126563A956AB1UL,
            0x08BB46E155DB16B9UL,
            0x56449F006C9F3FB4UL,
            0x34A9273550941803UL,
            0x5B4DF62660F99462UL,
            0xB8665CAD532E3018UL,
            0x72FC3E5F7F84216AUL
        ];

        foreach (ulong value in expected)
        {
            Assert.That(engine.NextUInt64(), Is.EqualTo(value));
        }
    }

    [Test]
    public void ReferenceInitializationMapsStateAndStreamSelector()
    {
        // Provenance: PCG reference initialization is state=0, step once, add initial state,
        // then step again; sequence selector 54 maps to odd increment 109.
        // https://www.pcg-random.org/download.html
        Pcg64DxsmEngine engine = Pcg64DxsmEngine.FromSeed((UInt128)42, (UInt128)54);
        ulong[] expected =
        [
            0xF0847C9518BDDB90UL,
            0x8E7D5F5514BA8AAAUL,
            0x86FBD36F8028F6FDUL,
            0x8D14B6EDBE9F740AUL,
            0xA85B2896C7CAD55DUL,
            0x8CA3894A1D9227BBUL,
            0x9F804D5DB108F5DFUL,
            0xB0DCD9C3191B2A32UL
        ];

        Assert.Multiple(() =>
        {
            Assert.That(engine.StateValue(), Is.EqualTo(WideMath.FromParts(0x80UL, 0xED5F0774FE8F5330UL)));
            Assert.That(engine.IncrementValue(), Is.EqualTo((UInt128)109));
        });

        foreach (ulong value in expected)
        {
            Assert.That(engine.NextUInt64(), Is.EqualTo(value));
        }
    }

    [Test]
    public void DxsmUsesStateBeforeAdvancing()
    {
        Pcg64DxsmEngine engine = new Pcg64DxsmEngine((UInt128)0, (UInt128)0);

        Assert.Multiple(() =>
        {
            Assert.That(engine.NextUInt64(), Is.EqualTo(0UL));
            Assert.That(engine.NextUInt64(), Is.EqualTo(0x5238EA76D1F0DF4AUL));
            Assert.That(engine.NextUInt64(), Is.EqualTo(0x1A3C4747022E48A4UL));
        });
    }

    [Test]
    public void StatePayloadIsCanonicalLittleEndianAndRoundTrips()
    {
        UInt128 state = WideMath.FromParts(0x0123456789ABCDEFUL, 0xFEDCBA9876543210UL);
        UInt128 increment = WideMath.FromParts(0x0F1E2D3C4B5A6978UL, 0x8796A5B4C3D2E1F1UL);
        Pcg64DxsmEngine original = Pcg64DxsmEngine.FromStateValues(state, increment);
        byte[] payload = original.StatePayload();
        Pcg64DxsmEngine fromPayload = Pcg64DxsmEngine.FromStatePayload(payload);
        RandomState snapshot = original.ExportState();
        Pcg64DxsmEngine fromSnapshot = Pcg64DxsmEngine.FromState(snapshot);
        ulong expected = original.NextUInt64();

        Assert.Multiple(() =>
        {
            Assert.That(payload.Length, Is.EqualTo(32));
            Assert.That(payload.AsSpan(0, 8).ToArray(), Is.EqualTo(new byte[] { 0x10, 0x32, 0x54, 0x76, 0x98, 0xBA, 0xDC, 0xFE }));
            Assert.That(BinaryCodec.ReadUInt128(payload, 0), Is.EqualTo(state));
            Assert.That(BinaryCodec.ReadUInt128(payload, 16), Is.EqualTo(increment));
            Assert.That(snapshot.Algorithm(), Is.EqualTo(RandomAlgorithm.Pcg64DxsmV1));
            Assert.That(fromPayload.NextUInt64(), Is.EqualTo(expected));
            Assert.That(fromSnapshot.NextUInt64(), Is.EqualTo(expected));
        });
    }

    [Test]
    public void CloneCopiesStateAndStreamThenEvolvesIndependently()
    {
        Pcg64DxsmEngine original = new Pcg64DxsmEngine((UInt128)12345, (UInt128)67890);
        original.NextUInt64();
        original.Advance((UInt128)100);
        Pcg64DxsmEngine clone = original.Clone();

        Assert.Multiple(() =>
        {
            Assert.That(clone.StateValue(), Is.EqualTo(original.StateValue()));
            Assert.That(clone.IncrementValue(), Is.EqualTo(original.IncrementValue()));
            Assert.That(clone.NextUInt64(), Is.EqualTo(original.NextUInt64()));
        });

        clone.NextUInt64();
        Assert.That(clone.StateValue(), Is.Not.EqualTo(original.StateValue()));
    }

    [Test]
    public void LogarithmicAdvanceMatchesIterationAndLargeReferenceState()
    {
        UInt128 initialState = WideMath.FromParts(0xDCBF51ECA8A06D6DUL, 0xA11C0E6F95BE4591UL);
        UInt128 increment = WideMath.FromParts(0x6F01E045569D93C4UL, 0x40AC10DD049310B5UL);
        Pcg64DxsmEngine iterated = Pcg64DxsmEngine.FromStateValues(initialState, increment);
        Pcg64DxsmEngine advanced = iterated.Clone();
        for (int index = 0; index < 1000; index++)
        {
            iterated.NextUInt64();
        }

        advanced.Advance((UInt128)1000);
        Assert.That(advanced.StateValue(), Is.EqualTo(iterated.StateValue()));
        Assert.That(advanced.StateValue(), Is.EqualTo(WideMath.FromParts(0x15340C5E5B54EC94UL, 0xA92969BD09525529UL)));

        UInt128 largeDistance = ((UInt128)1 << 100) + (UInt128)12345;
        Pcg64DxsmEngine large = Pcg64DxsmEngine.FromStateValues(initialState, increment);
        large.Advance(largeDistance);
        Assert.That(large.StateValue(), Is.EqualTo(WideMath.FromParts(0xB5FB0A6DB1F5F4B1UL, 0xA839849C3DBE3022UL)));
    }

    [Test]
    public void AdvanceMatchesNumpyLargeDistanceFixture()
    {
        // Provenance: NumPy's TestPCG64DXSM.test_advance_large fixture for seed 38219308213743.
        // Decimal states in the upstream test are represented here as equivalent UInt128 hex words.
        // https://github.com/numpy/numpy/blob/main/numpy/random/tests/test_direct.py
        UInt128 initialState = WideMath.FromParts(0xD85F72C717AD8F6CUL, 0x6962E34F5A12F2CDUL);
        UInt128 increment = WideMath.FromParts(0x7E77D7ADF0727810UL, 0x0A8857710C4874EDUL);
        UInt128 distance = WideMath.FromParts(0x0000000100000001UL, 0x0000000100010116UL);
        Pcg64DxsmEngine engine = Pcg64DxsmEngine.FromStateValues(initialState, increment);

        engine.Advance(distance);

        Assert.That(engine.StateValue(), Is.EqualTo(WideMath.FromParts(
            0xD0FA1CE6DEC18CF6UL,
            0xD3AAB6E704C1466FUL)));
    }

    [Test]
    public void AdvanceCompositionAndZeroDistancePreserveLcgAlgebra()
    {
        Pcg64DxsmEngine split = new Pcg64DxsmEngine((UInt128)987654321, (UInt128)123456789);
        Pcg64DxsmEngine combined = split.Clone();
        UInt128 before = split.StateValue();
        split.Advance((UInt128)0);
        Assert.That(split.StateValue(), Is.EqualTo(before));

        UInt128 first = ((UInt128)1 << 90) + (UInt128)17;
        UInt128 second = ((UInt128)1 << 80) + (UInt128)29;
        split.Advance(first);
        split.Advance(second);
        combined.Advance(first + second);

        Assert.Multiple(() =>
        {
            Assert.That(split.StateValue(), Is.EqualTo(combined.StateValue()));
            Assert.That(split.NextUInt64(), Is.EqualTo(combined.NextUInt64()));
        });
    }

    [Test]
    public void StreamSelectorsProduceOddDistinctIncrements()
    {
        Pcg64DxsmEngine first = new Pcg64DxsmEngine((UInt128)1, (UInt128)0);
        Pcg64DxsmEngine second = new Pcg64DxsmEngine((UInt128)1, (UInt128)1);

        Assert.Multiple(() =>
        {
            Assert.That(first.IncrementValue(), Is.EqualTo((UInt128)1));
            Assert.That(second.IncrementValue(), Is.EqualTo((UInt128)3));
            Assert.That((first.IncrementValue() & (UInt128)1), Is.EqualTo((UInt128)1));
            Assert.That((second.IncrementValue() & (UInt128)1), Is.EqualTo((UInt128)1));
            Assert.That(first.NextUInt64(), Is.Not.EqualTo(second.NextUInt64()));
        });
    }

    [Test]
    public void InvalidStateIsRejectedBeforeExistingEngineChanges()
    {
        Pcg64DxsmEngine engine = new Pcg64DxsmEngine((UInt128)123, (UInt128)456);
        UInt128 beforeState = engine.StateValue();
        UInt128 beforeIncrement = engine.IncrementValue();
        byte[] evenIncrement = new byte[32];
        BinaryCodec.WriteUInt128(evenIncrement, 0, (UInt128)99);
        BinaryCodec.WriteUInt128(evenIncrement, 16, (UInt128)2);
        RandomState wrongAlgorithm = StateCodec.Create(RandomAlgorithm.SplitMix64V1, 1, 1, new byte[32]);
        RandomState shortPayload = StateCodec.Create(RandomAlgorithm.Pcg64DxsmV1, 1, 1, new byte[31]);

        Assert.Multiple(() =>
        {
            Assert.Throws<RandomStateException>(() => engine.RestoreStatePayload(evenIncrement));
            Assert.Throws<RandomStateException>(() => engine.RestoreStatePayload(new byte[33]));
            Assert.Throws<RandomStateException>(() => engine.RestoreState(wrongAlgorithm));
            Assert.Throws<RandomStateException>(() => engine.RestoreState(shortPayload));
            Assert.Throws<RandomStateException>(() => Pcg64DxsmEngine.FromStateValues((UInt128)0, (UInt128)0));
            RandomArgumentException payloadError = ReflectionInfo.Required<RandomArgumentException>(Assert.Throws<RandomArgumentException>(() => engine.RestoreStatePayload(NullReference.Value<byte[]>())));
            Assert.That(payloadError.ArgumentName(), Is.EqualTo("payload"));
        });

        Assert.Multiple(() =>
        {
            Assert.That(engine.StateValue(), Is.EqualTo(beforeState));
            Assert.That(engine.IncrementValue(), Is.EqualTo(beforeIncrement));
        });
    }

    [Test]
    public void MetadataDeclaresDefaultEngineIdentityAndCapabilities()
    {
        RandomAlgorithmInfo info = new Pcg64DxsmEngine((UInt128)0, (UInt128)0).Info();

        Assert.Multiple(() =>
        {
            Assert.That(info.Algorithm(), Is.EqualTo(RandomAlgorithm.Pcg64DxsmV1));
            Assert.That(info.Name(), Is.EqualTo("PCG64 DXSM"));
            Assert.That(info.Version(), Is.EqualTo(1));
            Assert.That(info.WordBits(), Is.EqualTo(64));
            Assert.That(info.StateBytes(), Is.EqualTo(32));
            Assert.That(info.IsCryptographic(), Is.False);
            Assert.That(info.Capabilities(), Is.EqualTo(new[] { RandomCapability.State, RandomCapability.Clone, RandomCapability.Advance }));
            Assert.That(info.HasCapability(RandomCapability.Jump), Is.False);
            Assert.That(info.HasCapability(RandomCapability.Reseed), Is.False);
        });
    }
}
