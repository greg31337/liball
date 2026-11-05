namespace random.Tests;

public class Xoshiro256StarStarEngineTests
{
    [Test]
    public void ReferenceStateProducesOfficialKnownAnswerVector()
    {
        // Provenance: generated from the authors' xoshiro256** 1.0 reference implementation
        // with its published initial state { 1, 2, 3, 4 }.
        // https://prng.di.unimi.it/xoshiro256starstar.c
        Xoshiro256StarStarEngine engine = Xoshiro256StarStarEngine.FromStateWords([1UL, 2UL, 3UL, 4UL]);
        ulong[] expected =
        [
            11520UL,
            0UL,
            1509978240UL,
            1215971899390074240UL,
            1216172134540287360UL,
            607988272756665600UL,
            16172922978634559625UL,
            8476171486693032832UL,
            10595114339597558777UL,
            2904607092377533576UL
        ];

        foreach (ulong value in expected)
        {
            Assert.That(engine.NextUInt64(), Is.EqualTo(value));
        }
    }

    [Test]
    public void UInt64SeedUsesFourSequentialSplitMix64Outputs()
    {
        // Provenance: the reference xoshiro source recommends filling the four state words from
        // SplitMix64; expected words were generated with https://prng.di.unimi.it/splitmix64.c.
        Xoshiro256StarStarEngine engine = Xoshiro256StarStarEngine.FromSeed(100UL);
        ulong[] expectedState =
        [
            0x23259B94F13CF544UL,
            0x03BC38D6C6B89FE4UL,
            0x3E540F97FBD2E5CDUL,
            0x40DBD7E668859A70UL
        ];
        ulong[] expectedOutput =
        [
            0x0AFEE0773A0D8A51UL,
            0x13B0CA759B9B1735UL,
            0x5C76D220F8461395UL,
            0x8852F10B70A289F7UL
        ];

        Assert.That(engine.StateWords(), Is.EqualTo(expectedState));
        foreach (ulong value in expectedOutput)
        {
            Assert.That(engine.NextUInt64(), Is.EqualTo(value));
        }
    }

    [Test]
    public void StatePayloadIsCanonicalLittleEndianAndRoundTrips()
    {
        ulong[] words = [0x0123456789ABCDEFUL, 0xFEDCBA9876543210UL, 0x0F1E2D3C4B5A6978UL, 0x8796A5B4C3D2E1F0UL];
        Xoshiro256StarStarEngine original = Xoshiro256StarStarEngine.FromStateWords(words);
        byte[] payload = original.StatePayload();
        Xoshiro256StarStarEngine fromPayload = Xoshiro256StarStarEngine.FromStatePayload(payload);
        RandomState snapshot = original.ExportState();
        Xoshiro256StarStarEngine fromSnapshot = Xoshiro256StarStarEngine.FromState(snapshot);
        ulong expected = original.NextUInt64();

        words[0] = 0UL;
        ulong[] returned = fromPayload.StateWords();
        returned[1] = 0UL;

        Assert.Multiple(() =>
        {
            Assert.That(payload.Length, Is.EqualTo(32));
            Assert.That(payload.AsSpan(0, 8).ToArray(), Is.EqualTo(new byte[] { 0xEF, 0xCD, 0xAB, 0x89, 0x67, 0x45, 0x23, 0x01 }));
            Assert.That(snapshot.Algorithm(), Is.EqualTo(RandomAlgorithm.Xoshiro256StarStarV1));
            Assert.That(fromPayload.NextUInt64(), Is.EqualTo(expected));
            Assert.That(fromSnapshot.NextUInt64(), Is.EqualTo(expected));
        });
    }

    [Test]
    public void AllZeroStateIsRejectedWhileEveryNonzeroWordPositionIsValid()
    {
        Assert.Throws<RandomStateException>(() => Xoshiro256StarStarEngine.FromStateWords([0UL, 0UL, 0UL, 0UL]));
        Assert.Throws<RandomStateException>(() => Xoshiro256StarStarEngine.FromStatePayload(new byte[32]));

        for (int index = 0; index < 4; index++)
        {
            ulong[] words = new ulong[4];
            words[index] = 1UL;
            Assert.DoesNotThrow(() => Xoshiro256StarStarEngine.FromStateWords(words));
        }
    }

    [Test]
    public void CloneCopiesExactPositionAndEvolvesIndependently()
    {
        Xoshiro256StarStarEngine original = new Xoshiro256StarStarEngine(123456789UL);
        original.NextUInt64();
        original.NextUInt64();
        Xoshiro256StarStarEngine clone = original.Clone();

        Assert.That(clone.NextUInt64(), Is.EqualTo(original.NextUInt64()));
        clone.NextUInt64();
        Assert.That(clone.StateWords(), Is.Not.EqualTo(original.StateWords()));
    }

    [Test]
    public void JumpMatchesOfficialReferencePolynomialState()
    {
        // Provenance: JUMP polynomial from https://prng.di.unimi.it/xoshiro256starstar.c;
        // the post-jump state and outputs were generated from that reference source.
        Xoshiro256StarStarEngine engine = Xoshiro256StarStarEngine.FromStateWords([1UL, 2UL, 3UL, 4UL]);
        engine.Jump();

        Assert.Multiple(() =>
        {
            Assert.That(engine.StateWords(), Is.EqualTo(new ulong[]
            {
                0x8C7A153956B5F3D1UL,
                0x701F1A713401D85EUL,
                0x6527F66A65469085UL,
                0x8386B786C4408050UL
            }));
            Assert.That(engine.NextUInt64(), Is.EqualTo(0xBBD2F312298443D8UL));
            Assert.That(engine.NextUInt64(), Is.EqualTo(0x62E57DB2D5706577UL));
        });
    }

    [Test]
    public void LongJumpMatchesOfficialReferencePolynomialState()
    {
        // Provenance: LONG_JUMP polynomial from https://prng.di.unimi.it/xoshiro256starstar.c;
        // the post-jump state and outputs were generated from that reference source.
        Xoshiro256StarStarEngine engine = Xoshiro256StarStarEngine.FromStateWords([1UL, 2UL, 3UL, 4UL]);
        engine.LongJump();

        Assert.Multiple(() =>
        {
            Assert.That(engine.StateWords(), Is.EqualTo(new ulong[]
            {
                0x096A8EB71295A400UL,
                0xDBF84991E50F4516UL,
                0x534EE745810D2A0EUL,
                0x31655CA1A2215BF1UL
            }));
            Assert.That(engine.NextUInt64(), Is.EqualTo(0x527752A1D792704DUL));
            Assert.That(engine.NextUInt64(), Is.EqualTo(0xD8D8BDEC57599E64UL));
        });
    }

    [Test]
    public void JumpAndLongJumpCompositionIsOrderIndependent()
    {
        Xoshiro256StarStarEngine first = new Xoshiro256StarStarEngine(42UL);
        Xoshiro256StarStarEngine second = first.Clone();
        first.Jump();
        first.LongJump();
        second.LongJump();
        second.Jump();

        Assert.Multiple(() =>
        {
            Assert.That(first.StateWords(), Is.EqualTo(second.StateWords()));
            Assert.That(first.NextUInt64(), Is.EqualTo(second.NextUInt64()));
        });
    }

    [Test]
    public void InvalidRestoreDoesNotChangeExistingState()
    {
        Xoshiro256StarStarEngine engine = new Xoshiro256StarStarEngine(987654321UL);
        ulong[] before = engine.StateWords();
        RandomState wrongAlgorithm = StateCodec.Create(RandomAlgorithm.SplitMix64V1, 1, 1, new byte[32]);
        RandomState shortPayload = StateCodec.Create(RandomAlgorithm.Xoshiro256StarStarV1, 1, 1, new byte[31]);

        Assert.Multiple(() =>
        {
            Assert.Throws<RandomStateException>(() => engine.RestoreStatePayload(new byte[32]));
            Assert.Throws<RandomStateException>(() => engine.RestoreStatePayload(new byte[33]));
            Assert.Throws<RandomStateException>(() => engine.RestoreState(wrongAlgorithm));
            Assert.Throws<RandomStateException>(() => engine.RestoreState(shortPayload));
            RandomArgumentException wordsError = ReflectionInfo.Required<RandomArgumentException>(Assert.Throws<RandomArgumentException>(() => Xoshiro256StarStarEngine.FromStateWords(NullReference.Value<ulong[]>())));
            Assert.That(wordsError.ArgumentName(), Is.EqualTo("stateWords"));
        });

        Assert.That(engine.StateWords(), Is.EqualTo(before));
    }

    [Test]
    public void MetadataDeclaresJumpCapabilitiesAndNonSecurity()
    {
        RandomAlgorithmInfo info = new Xoshiro256StarStarEngine(0UL).Info();

        Assert.Multiple(() =>
        {
            Assert.That(info.Algorithm(), Is.EqualTo(RandomAlgorithm.Xoshiro256StarStarV1));
            Assert.That(info.Name(), Is.EqualTo("xoshiro256**"));
            Assert.That(info.Version(), Is.EqualTo(1));
            Assert.That(info.WordBits(), Is.EqualTo(64));
            Assert.That(info.StateBytes(), Is.EqualTo(32));
            Assert.That(info.IsCryptographic(), Is.False);
            Assert.That(info.Capabilities(), Is.EqualTo(new[] { RandomCapability.State, RandomCapability.Clone, RandomCapability.Jump, RandomCapability.LongJump }));
            Assert.That(info.HasCapability(RandomCapability.Advance), Is.False);
            Assert.That(info.HasCapability(RandomCapability.Reseed), Is.False);
        });
    }
}
