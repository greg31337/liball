using fake;

namespace fake.Tests;

[TestFixture]
public class StateTests
{
    [Test]
    public void SnapshotRestoreReplaysFromExactPosition()
    {
        FakeRandom random = Fake.Create(12345).Random();
        Take(random, 9);
        FakeState state = random.Snapshot();
        ulong[] expected = Take(random, 24);
        Take(random, 5);

        random.Restore(state);

        Assert.That(Take(random, 24), Is.EqualTo(expected));
    }

    [Test]
    public void SnapshotIsIndependentFromStreamAndOtherSnapshots()
    {
        FakeRandom random = Fake.Create(55).Random();
        FakeState first = random.Snapshot();
        random.ULong();
        FakeState second = random.Snapshot();

        Assert.That(second, Is.Not.SameAs(first));
        Assert.That(second.State0, Is.Not.EqualTo(first.State0));
    }

    [Test]
    public void RestoreDefensivelyCopiesCallerState()
    {
        FakeRandom source = Fake.Create(88).Random();
        Take(source, 3);
        FakeState state = source.Snapshot();
        FakeRandom restored = Fake.Create().Random();
        FakeRandom expected = Fake.Create().Random();
        restored.Restore(state);
        expected.Restore(state);

        state.State0 = 0;
        state.State1 = 0;
        state.State2 = 0;
        state.State3 = 0;

        AssertSequencesEqual(restored, expected, 20);
    }

    [Test]
    public void RestoreAlsoRestoresSeedUsedByReset()
    {
        FakeRandom source = Fake.Create(-900).Random();
        Take(source, 4);
        FakeState state = source.Snapshot();
        FakeRandom restored = Fake.Create(1).Random();
        FakeRandom expected = Fake.Create(-900).Random();

        restored.Restore(state);
        restored.Reset();

        AssertSequencesEqual(restored, expected, 20);
    }

    [Test]
    public void RootSnapshotAndRestoreDelegateToRootRandomStream()
    {
        Fake value = Fake.Create(77);
        Take(value.Random(), 2);
        FakeState state = value.Snapshot();
        ulong[] expected = Take(value.Random(), 12);

        value.Restore(state);

        Assert.That(Take(value.Random(), 12), Is.EqualTo(expected));
    }

    [Test]
    public void RootResetReplaysCurrentSeed()
    {
        Fake value = Fake.Create(123);
        ulong[] expected = Take(value.Random(), 12);
        Take(value.Random(), 3);

        value.Reset();

        Assert.That(Take(value.Random(), 12), Is.EqualTo(expected));
    }

    [Test]
    public void RestoreRejectsNullState()
    {
        FakeRandom random = Fake.Create().Random();

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => random.Restore(null));

        Assert.That(exception.ParamName, Is.EqualTo("state"));
    }

    [Test]
    public void RestoreRejectsUnknownAlgorithmWithoutChangingStream()
    {
        FakeRandom random = Fake.Create(12).Random();
        FakeRandom expected = Fake.Create(12).Random();
        FakeState state = random.Snapshot();
        state.Algorithm = "unknown";

        ArgumentException exception = Assert.Throws<ArgumentException>(() => random.Restore(state));

        Assert.That(exception.ParamName, Is.EqualTo("Algorithm"));
        AssertSequencesEqual(random, expected, 12);
    }

    [Test]
    public void RestoreRejectsAllZeroStateWithoutChangingStream()
    {
        FakeRandom random = Fake.Create(12).Random();
        FakeRandom expected = Fake.Create(12).Random();
        FakeState state = random.Snapshot();
        state.State0 = 0;
        state.State1 = 0;
        state.State2 = 0;
        state.State3 = 0;

        ArgumentException exception = Assert.Throws<ArgumentException>(() => random.Restore(state));

        Assert.That(exception.ParamName, Is.EqualTo("state"));
        AssertSequencesEqual(random, expected, 12);
    }

    [Test]
    public void StateCopyPreservesFieldsAndIsIndependent()
    {
        FakeState original = Fake.Create(999).Snapshot();

        FakeState copy = original.Copy();
        original.State0 = 0;

        Assert.That(copy, Is.Not.SameAs(original));
        Assert.That(copy.Algorithm, Is.EqualTo(FakeState.StableAlgorithm));
        Assert.That(copy.State0, Is.Not.Zero);
    }

    [Test]
    public void SeedZeroGoldenSequenceIsStable()
    {
        ulong[] expected = new ulong[]
        {
            11091344671253066420UL,
            13793997310169335082UL,
            1900383378846508768UL,
            7684712102626143532UL,
            13521403990117723737UL,
            18442103541295991498UL,
            7788427924976520344UL,
            9881088229871127103UL,
            15781505947799885617UL,
            16949938600482740797UL
        };

        Assert.That(Take(Fake.Create(0).Random(), expected.Length), Is.EqualTo(expected));
    }

    [Test]
    public void NegativeSeedGoldenSequenceIsStable()
    {
        ulong[] expected = new ulong[]
        {
            10328197420357168392UL,
            14156678507024973869UL,
            9357971779955476126UL,
            13791585006304312367UL,
            10463432026814718762UL,
            13498236496097551653UL
        };

        Assert.That(Take(Fake.Create(-1).Random(), expected.Length), Is.EqualTo(expected));
    }

    [Test]
    public void Utf8StringSeedGoldenSequenceIsStable()
    {
        ulong[] expected = new ulong[]
        {
            9025987417202958220UL,
            9442845966170767777UL,
            10154801524420745176UL,
            5414418851134104357UL,
            8068500802228477934UL,
            7319808901814411094UL
        };
        FakeRandom random = Fake.Create().Random();
        random.ReseedFromSeedUsingString("naïve/東京");

        Assert.That(Take(random, expected.Length), Is.EqualTo(expected));
    }

    static ulong[] Take(FakeRandom random, int count)
    {
        ulong[] values = new ulong[count];
        int index = 0;
        while (index < count)
        {
            values[index] = random.ULong();
            index = index + 1;
        }
        return values;
    }

    static void AssertSequencesEqual(FakeRandom first, FakeRandom second, int count)
    {
        Assert.That(Take(first, count), Is.EqualTo(Take(second, count)));
    }
}
