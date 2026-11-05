using fake;

namespace fake.Tests;

[TestFixture]
public class SelectionTests
{
    [Test]
    public void SeedZeroSelectionGoldenResultsAreStable()
    {
        string[] values = new string[] { "a", "b", "c", "d", "e" };

        Assert.That(Fake.Create(0).Random().Choice(values), Is.EqualTo("a"));
        Assert.That(
            Fake.Create(0).Random().Choices(values, 8),
            Is.EqualTo(new string[] { "a", "c", "d", "c", "c", "d", "e", "d" }));
        Assert.That(
            Fake.Create(0).Random().Shuffle(values),
            Is.EqualTo(new string[] { "a", "d", "b", "c", "e" }));
    }

    [Test]
    public void StringChoiceIsUniformAcrossDeterministicLargeSample()
    {
        string[] values = new string[] { "a", "b", "c", "d" };
        int[] counts = new int[values.Length];
        FakeRandom random = Fake.Create(301).Random();

        for (int index = 0; index < 100000; index++)
        {
            string selected = random.Choice(values);
            counts[Array.IndexOf(values, selected)]++;
        }

        Assert.That(counts, Has.All.InRange(24000, 26000));
    }

    [Test]
    public void ObjectChoicePreservesObjectIdentity()
    {
        object first = new object();
        object second = new object();
        object third = new object();
        object[] values = new object[] { first, second, third };
        FakeRandom random = Fake.Create(302).Random();

        for (int index = 0; index < 1000; index++)
        {
            object selected = random.ChoiceFromValues(values);
            Assert.That(selected, Is.SameAs(first).Or.SameAs(second).Or.SameAs(third));
        }
    }

    [Test]
    public void StringAndObjectOverloadsUseTheSameIndexSequence()
    {
        string[] strings = new string[] { "a", "b", "c", "d" };
        object[] objects = new object[] { "a", "b", "c", "d" };
        FakeRandom stringRandom = Fake.Create(303).Random();
        FakeRandom objectRandom = Fake.Create(303).Random();

        for (int index = 0; index < 1000; index++)
        {
            Assert.That(objectRandom.ChoiceFromValues(objects), Is.EqualTo(stringRandom.Choice(strings)));
        }
    }

    [Test]
    public void ChoicesWithReplacementCanExceedSourceAndRepeatPositions()
    {
        string[] values = new string[] { "a", "b", "c" };
        FakeRandom random = Fake.Create(304).Random();

        string[] selected = random.Choices(values, 20);

        Assert.That(selected, Has.Length.EqualTo(20));
        Assert.That(selected, Has.All.Matches<string>(value => Array.IndexOf(values, value) >= 0));
        Assert.That(selected.Distinct().Count(), Is.LessThan(selected.Length));
    }

    [Test]
    public void ChoicesWithoutReplacementDoNotRepeatUniqueValues()
    {
        string[] values = Enumerable.Range(0, 100).Select(value => value.ToString()).ToArray();
        FakeRandom random = Fake.Create(305).Random();

        string[] selected = random.ChoicesFromValuesAndCountAndWithReplacement(values, 80, false);

        Assert.That(selected, Has.Length.EqualTo(80));
        Assert.That(selected.Distinct().Count(), Is.EqualTo(selected.Length));
        Assert.That(selected, Has.All.Matches<string>(value => Array.IndexOf(values, value) >= 0));
    }

    [Test]
    public void SampleIsTheWithoutReplacementChoiceOperation()
    {
        object[] values = Enumerable.Range(0, 20).Cast<object>().ToArray();
        FakeRandom sampled = Fake.Create(306).Random();
        FakeRandom chosen = Fake.Create(306).Random();

        object[] expected = chosen.ChoicesFromValuesAndCountAndWithReplacementUsingObjectArrayAndInt32AndBoolean(values, 12, false);
        object[] actual = sampled.SampleFromValuesAndCount(values, 12);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void ShuffleReturnsPermutationWithoutMutatingInput()
    {
        string[] values = Enumerable.Range(0, 50).Select(value => value.ToString()).ToArray();
        string[] original = (string[])values.Clone();
        FakeRandom random = Fake.Create(307).Random();

        string[] shuffled = random.Shuffle(values);

        Assert.That(shuffled, Is.Not.SameAs(values));
        Assert.That(shuffled, Is.Not.EqualTo(values));
        Assert.That(shuffled, Is.EquivalentTo(values));
        Assert.That(values, Is.EqualTo(original));
    }

    [Test]
    public void ThreeItemShuffleIsUniformAcrossDeterministicLargeSample()
    {
        string[] values = new string[] { "a", "b", "c" };
        Dictionary<string, int> counts = new Dictionary<string, int>();
        FakeRandom random = Fake.Create(308).Random();

        for (int index = 0; index < 60000; index++)
        {
            string key = string.Concat(random.Shuffle(values));
            if (!counts.ContainsKey(key))
            {
                counts[key] = 0;
            }
            counts[key]++;
        }

        Assert.That(counts, Has.Count.EqualTo(6));
        Assert.That(counts.Values, Has.All.InRange(9500, 10500));
    }

    [Test]
    public void WithoutReplacementTracksPositionsRatherThanDistinctValues()
    {
        string[] values = new string[] { "same", "same" };
        FakeRandom random = Fake.Create(309).Random();

        string[] selected = random.Sample(values, 2);

        Assert.That(selected, Is.EqualTo(new string[] { "same", "same" }));
    }

    [Test]
    public void ZeroCountAndEmptyShuffleReturnIndependentEmptyArraysWithoutAdvancing()
    {
        FakeRandom random = Fake.Create(310).Random();
        FakeState before = random.Snapshot();
        string[] empty = Array.Empty<string>();

        string[] choices = random.Choices(empty, 0);
        string[] sample = random.Sample(empty, 0);
        string[] shuffled = random.Shuffle(empty);

        Assert.That(choices, Is.Empty.And.Not.SameAs(empty));
        Assert.That(sample, Is.Empty.And.Not.SameAs(empty));
        Assert.That(shuffled, Is.Empty.And.Not.SameAs(empty));
        AssertStateEqual(random.Snapshot(), before);
    }

    [Test]
    public void OneElementOperationsDoNotAdvanceState()
    {
        FakeRandom random = Fake.Create(311).Random();
        FakeState before = random.Snapshot();
        string[] values = new string[] { "only" };

        Assert.That(random.Choice(values), Is.EqualTo("only"));
        Assert.That(random.Choices(values, 5), Is.EqualTo(Enumerable.Repeat("only", 5)));
        Assert.That(random.Sample(values, 1), Is.EqualTo(values));
        Assert.That(random.Shuffle(values), Is.EqualTo(values));

        AssertStateEqual(random.Snapshot(), before);
    }

    [Test]
    public void NullAndEmptyChoiceInputsAreRejected()
    {
        FakeRandom random = Fake.Create().Random();

        Assert.That(Assert.Throws<ArgumentNullException>(() => random.Choice((string[])null)).ParamName, Is.EqualTo("values"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => random.ChoiceFromValues((object[])null)).ParamName, Is.EqualTo("values"));
        Assert.That(Assert.Throws<ArgumentException>(() => random.Choice(Array.Empty<string>())).ParamName, Is.EqualTo("values"));
        Assert.That(Assert.Throws<ArgumentException>(() => random.ChoiceFromValues(Array.Empty<object>())).ParamName, Is.EqualTo("values"));
        Assert.That(Assert.Throws<ArgumentException>(() => random.Choices(Array.Empty<string>(), 1)).ParamName, Is.EqualTo("values"));
        Assert.That(Assert.Throws<ArgumentException>(() => random.SampleFromValuesAndCount(Array.Empty<object>(), 1)).ParamName, Is.EqualTo("values"));
    }

    [Test]
    public void SelectionRejectsInvalidCounts()
    {
        FakeRandom random = Fake.Create().Random();
        string[] values = new string[] { "a", "b" };

        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => random.Choices(values, -1)).ParamName, Is.EqualTo("count"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => random.ChoicesFromValuesAndCountAndWithReplacement(values, 3, false)).ParamName, Is.EqualTo("count"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => random.Sample(values, 3)).ParamName, Is.EqualTo("count"));
    }

    [Test]
    public void SelectionHonorsConfiguredCountLimit()
    {
        FakeOptions options = new FakeOptions();
        options.MaximumCount = 2;
        FakeRandom random = Fake.Create(options).Random();
        string[] tooManyValues = new string[] { "a", "b", "c" };

        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => random.Choice(tooManyValues)).ParamName, Is.EqualTo("values"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => random.Shuffle(tooManyValues)).ParamName, Is.EqualTo("values"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => random.Choices(new string[] { "a" }, 3)).ParamName, Is.EqualTo("count"));
    }

    [Test]
    public void SelectionAllowsNullElementsAndReturnsIndependentArrays()
    {
        string[] values = new string[] { null };
        FakeRandom random = Fake.Create(312).Random();

        Assert.That(random.Choice(values), Is.Null);
        string[] selected = random.Choices(values, 2);
        selected[0] = "changed";

        Assert.That(values[0], Is.Null);
        Assert.That(selected[1], Is.Null);
    }

    [Test]
    public void SnapshotRestoreReplaysMixedSelectionCalls()
    {
        FakeRandom random = Fake.Create(313).Random();
        string[] strings = new string[] { "a", "b", "c", "d", "e" };
        object[] objects = new object[] { 1, 2, 3, 4, 5 };
        FakeState state = random.Snapshot();

        object[] expected = MixedValues(random, strings, objects);
        random.Restore(state);

        Assert.That(MixedValues(random, strings, objects), Is.EqualTo(expected));
    }

    [Test]
    public void CryptographicModeSupportsEverySelectionOperation()
    {
        FakeOptions options = new FakeOptions();
        options.RandomMode = RandomMode.Cryptographic;
        FakeRandom random = Fake.Create(options).Random();
        string[] values = new string[] { "a", "b", "c", "d" };

        for (int index = 0; index < 500; index++)
        {
            Assert.That(random.Choice(values), Is.AnyOf(values));
            Assert.That(random.Choices(values, 8), Has.Length.EqualTo(8));
            Assert.That(random.Sample(values, 3), Is.Unique);
            Assert.That(random.Shuffle(values), Is.EquivalentTo(values));
        }
    }

    static object[] MixedValues(FakeRandom random, string[] strings, object[] objects)
    {
        return new object[]
        {
            random.Choice(strings),
            random.ChoiceFromValues(objects),
            random.Choices(strings, 7),
            random.ChoicesFromValuesAndCountAndWithReplacementUsingObjectArrayAndInt32AndBoolean(objects, 4, false),
            random.Sample(strings, 3),
            random.ShuffleFromValues(objects)
        };
    }

    static void AssertStateEqual(FakeState actual, FakeState expected)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.Algorithm, Is.EqualTo(expected.Algorithm));
            Assert.That(actual.InitialSeed, Is.EqualTo(expected.InitialSeed));
            Assert.That(actual.State0, Is.EqualTo(expected.State0));
            Assert.That(actual.State1, Is.EqualTo(expected.State1));
            Assert.That(actual.State2, Is.EqualTo(expected.State2));
            Assert.That(actual.State3, Is.EqualTo(expected.State3));
        });
    }
}
