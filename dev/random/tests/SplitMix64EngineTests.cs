namespace random.Tests;

public class SplitMix64EngineTests
{
    [Test]
    public void ReferenceSeedProducesPublishedKnownAnswerVector()
    {
        // Provenance: rand_xoshiro's fixture generated from Sebastiano Vigna's reference C implementation.
        // https://github.com/rust-random/rand_xoshiro/blob/master/src/splitmix64.rs
        // https://prng.di.unimi.it/splitmix64.c
        SplitMix64Engine engine = new SplitMix64Engine(1477776061723855037UL);
        ulong[] expected =
        [
            1985237415132408290UL,
            2979275885539914483UL,
            13511426838097143398UL,
            8488337342461049707UL,
            15141737807933549159UL,
            17093170987380407015UL,
            16389528042912955399UL,
            13177319091862933652UL,
            10841969400225389492UL,
            17094824097954834098UL,
            3336622647361835228UL,
            9678412372263018368UL,
            11111587619974030187UL,
            7882215801036322410UL,
            5709234165213761869UL,
            7799681907651786826UL,
            4616320717312661886UL,
            4251077652075509767UL,
            7836757050122171900UL,
            5054003328188417616UL,
            12919285918354108358UL,
            16477564761813870717UL,
            5124667218451240549UL,
            18099554314556827626UL,
            7603784838804469118UL,
            6358551455431362471UL,
            3037176434532249502UL,
            3217550417701719149UL,
            9958699920490216947UL,
            5965803675992506258UL,
            12000828378049868312UL,
            12720568162811471118UL,
            245696019213873792UL,
            8351371993958923852UL,
            14378754021282935786UL,
            5655432093647472106UL,
            5508031680350692005UL,
            8515198786865082103UL,
            6287793597487164412UL,
            14963046237722101617UL,
            3630795823534910476UL,
            8422285279403485710UL,
            10554287778700714153UL,
            10871906555720704584UL,
            8659066966120258468UL,
            9420238805069527062UL,
            10338115333623340156UL,
            13514802760105037173UL,
            14635952304031724449UL,
            15419692541594102413UL
        ];

        foreach (ulong value in expected)
        {
            Assert.That(engine.NextUInt64(), Is.EqualTo(value));
        }
    }

    [Test]
    public void UInt64SeedMapsDirectlyToLittleEndianState()
    {
        ulong seed = 0x0123456789ABCDEFUL;
        SplitMix64Engine engine = SplitMix64Engine.FromSeed(seed);

        Assert.Multiple(() =>
        {
            Assert.That(engine.StateValue(), Is.EqualTo(seed));
            Assert.That(engine.StatePayload(), Is.EqualTo(new byte[] { 0xEF, 0xCD, 0xAB, 0x89, 0x67, 0x45, 0x23, 0x01 }));
        });
    }

    [Test]
    public void EveryBitPatternIsAValidState()
    {
        SplitMix64Engine zero = SplitMix64Engine.FromStatePayload(new byte[8]);
        SplitMix64Engine maximum = SplitMix64Engine.FromStatePayload([0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF]);

        Assert.Multiple(() =>
        {
            Assert.That(zero.StateValue(), Is.EqualTo(0UL));
            Assert.That(maximum.StateValue(), Is.EqualTo(ulong.MaxValue));
            Assert.That(zero.NextUInt64(), Is.EqualTo(0xE220A8397B1DCDAFUL));
            Assert.That(maximum.NextUInt64(), Is.EqualTo(0xE4D971771B652C20UL));
        });
    }

    [Test]
    public void StateRoundTripResumesAtExactNextOutput()
    {
        SplitMix64Engine original = new SplitMix64Engine(42UL);
        for (int index = 0; index < 17; index++)
        {
            original.NextUInt64();
        }

        RandomState snapshot = original.ExportState();
        SplitMix64Engine restored = SplitMix64Engine.FromState(snapshot);
        SplitMix64Engine fromPayload = SplitMix64Engine.FromStatePayload(snapshot.Payload());
        ulong expected = original.NextUInt64();

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.Algorithm(), Is.EqualTo(RandomAlgorithm.SplitMix64V1));
            Assert.That(snapshot.AlgorithmVersion(), Is.EqualTo(1));
            Assert.That(snapshot.Payload().Length, Is.EqualTo(8));
            Assert.That(restored.NextUInt64(), Is.EqualTo(expected));
            Assert.That(fromPayload.NextUInt64(), Is.EqualTo(expected));
        });
    }

    [Test]
    public void CloneCopiesExactPositionAndThenEvolvesIndependently()
    {
        SplitMix64Engine original = new SplitMix64Engine(123UL);
        original.NextUInt64();
        original.NextUInt64();
        SplitMix64Engine clone = original.Clone();

        Assert.That(clone.NextUInt64(), Is.EqualTo(original.NextUInt64()));
        clone.NextUInt64();
        Assert.That(clone.StateValue(), Is.Not.EqualTo(original.StateValue()));
    }

    [Test]
    public void AdvanceMatchesIterationAndWrapsModuloTwoToTheSixtyFourth()
    {
        SplitMix64Engine iterated = new SplitMix64Engine(987654321UL);
        SplitMix64Engine advanced = new SplitMix64Engine(987654321UL);
        for (int index = 0; index < 1000; index++)
        {
            iterated.NextUInt64();
        }

        advanced.Advance(1000UL);
        // Provenance: independent evaluation of x + 1000 * 0x9E3779B97F4A7C15 modulo 2^64,
        // using the state transition in https://prng.di.unimi.it/splitmix64.c.
        Assert.That(advanced.StateValue(), Is.EqualTo(0x08B37C9975D31AB9UL));
        Assert.That(advanced.StateValue(), Is.EqualTo(iterated.StateValue()));
        Assert.That(advanced.NextUInt64(), Is.EqualTo(iterated.NextUInt64()));

        SplitMix64Engine wrapped = new SplitMix64Engine(ulong.MaxValue);
        wrapped.Advance(ulong.MaxValue);
        ulong expectedState = WideMath.AddModulo64(ulong.MaxValue, WideMath.MultiplyLow64(SplitMix64Engine.Increment, ulong.MaxValue));
        Assert.That(wrapped.StateValue(), Is.EqualTo(0x61C8864680B583EAUL));
        Assert.That(wrapped.StateValue(), Is.EqualTo(expectedState));
    }

    [Test]
    public void InvalidStateIsRejectedBeforeExistingEngineChanges()
    {
        SplitMix64Engine engine = new SplitMix64Engine(0xA5A5A5A5A5A5A5A5UL);
        ulong before = engine.StateValue();
        RandomState wrongAlgorithm = StateCodec.Create(RandomAlgorithm.Pcg64DxsmV1, 1, 1, new byte[8]);
        RandomState wrongAlgorithmVersion = new RandomState(RandomAlgorithm.SplitMix64V1, 2, 1, new byte[8]);
        RandomState wrongTransformVersion = new RandomState(RandomAlgorithm.SplitMix64V1, 1, 2, new byte[8]);
        RandomState shortPayload = StateCodec.Create(RandomAlgorithm.SplitMix64V1, 1, 1, new byte[7]);

        Assert.Multiple(() =>
        {
            Assert.Throws<RandomStateException>(() => engine.RestoreState(wrongAlgorithm));
            Assert.Throws<RandomStateException>(() => engine.RestoreState(wrongAlgorithmVersion));
            Assert.Throws<RandomStateException>(() => engine.RestoreState(wrongTransformVersion));
            Assert.Throws<RandomStateException>(() => engine.RestoreState(shortPayload));
            Assert.Throws<RandomStateException>(() => engine.RestoreStatePayload(new byte[9]));
            RandomArgumentException payloadError = ReflectionInfo.Required<RandomArgumentException>(Assert.Throws<RandomArgumentException>(() => engine.RestoreStatePayload(NullReference.Value<byte[]>())));
            Assert.That(payloadError.ArgumentName(), Is.EqualTo("payload"));
        });

        Assert.That(engine.StateValue(), Is.EqualTo(before));
    }

    [Test]
    public void MetadataDeclaresStateCloneAdvanceAndNonSecurity()
    {
        RandomAlgorithmInfo info = new SplitMix64Engine(0UL).Info();

        Assert.Multiple(() =>
        {
            Assert.That(info.Algorithm(), Is.EqualTo(RandomAlgorithm.SplitMix64V1));
            Assert.That(info.Name(), Is.EqualTo("SplitMix64"));
            Assert.That(info.Version(), Is.EqualTo(1));
            Assert.That(info.WordBits(), Is.EqualTo(64));
            Assert.That(info.StateBytes(), Is.EqualTo(8));
            Assert.That(info.IsCryptographic(), Is.False);
            Assert.That(info.Capabilities(), Is.EqualTo(new[] { RandomCapability.State, RandomCapability.Clone, RandomCapability.Advance }));
            Assert.That(info.HasCapability(RandomCapability.Reseed), Is.False);
            Assert.That(info.HasCapability(RandomCapability.PredictionResistance), Is.False);
        });
    }
}
