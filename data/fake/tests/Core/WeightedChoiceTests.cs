using fake;

namespace fake.Tests;

[TestFixture]
public class WeightedChoiceTests
{
    [Test]
    public void SeedZeroWeightedChoiceGoldenResultIsStable()
    {
        string[] values = new string[] { "a", "b", "c" };

        Assert.That(Fake.Create(0).Random().WeightedChoice(values, new double[] { 1.0, 2.0, 7.0 }), Is.EqualTo("c"));
    }

    [Test]
    public void NormalizedAndRelativeWeightsProduceTheSameSequence()
    {
        string[] values = new string[] { "a", "b", "c" };
        FakeRandom normalized = Fake.Create(401).Random();
        FakeRandom relative = Fake.Create(401).Random();

        for (int index = 0; index < 1000; index++)
        {
            string expected = normalized.WeightedChoice(values, new double[] { 0.125, 0.25, 0.625 });
            string actual = relative.WeightedChoice(values, new double[] { 1.0, 2.0, 5.0 });
            Assert.That(actual, Is.EqualTo(expected));
        }
    }

    [Test]
    public void WeightedChoiceDistributionMatchesRelativeWeights()
    {
        string[] values = new string[] { "a", "b", "c" };
        double[] weights = new double[] { 1.0, 2.0, 7.0 };
        int[] counts = new int[values.Length];
        FakeRandom random = Fake.Create(402).Random();

        for (int index = 0; index < 100000; index++)
        {
            string selected = random.WeightedChoice(values, weights);
            counts[Array.IndexOf(values, selected)]++;
        }

        Assert.Multiple(() =>
        {
            Assert.That(counts[0], Is.InRange(9500, 10500));
            Assert.That(counts[1], Is.InRange(19000, 21000));
            Assert.That(counts[2], Is.InRange(69000, 71000));
        });
    }

    [Test]
    public void ZeroWeightValuesAreNeverSelected()
    {
        string[] values = new string[] { "zero-a", "positive-a", "zero-b", "positive-b" };
        double[] weights = new double[] { 0.0, 2.0, 0.0, 3.0 };
        FakeRandom random = Fake.Create(403).Random();

        for (int index = 0; index < 10000; index++)
        {
            Assert.That(random.WeightedChoice(values, weights), Is.EqualTo("positive-a").Or.EqualTo("positive-b"));
        }
    }

    [Test]
    public void ExactCumulativeBoundarySelectsTheNextPositiveValue()
    {
        FakeRandom random = Fake.Create().Random();
        random.Restore(new FakeState(FakeState.StableAlgorithm, 0UL, 1UL, 11093667703980818432UL, 2UL, 3UL));

        string selected = random.WeightedChoice(
            new string[] { "left", "zero", "right" },
            new double[] { 1.0, 0.0, 1.0 });

        Assert.That(selected, Is.EqualTo("right"));
    }

    [Test]
    public void SinglePositiveWeightPreservesObjectIdentityWithoutAdvancingState()
    {
        object selectedObject = new object();
        object[] values = new object[] { new object(), selectedObject, new object() };
        FakeRandom random = Fake.Create(404).Random();
        FakeState before = random.Snapshot();

        object selected = random.WeightedChoiceFromValuesAndWeights(values, new double[] { 0.0, double.MaxValue, 0.0 });

        Assert.That(selected, Is.SameAs(selectedObject));
        AssertStateEqual(random.Snapshot(), before);
    }

    [Test]
    public void WeightedChoiceAllowsNullElements()
    {
        string[] values = new string[] { "not selected", null };

        Assert.That(Fake.Create(405).Random().WeightedChoice(values, new double[] { 0.0, 1.0 }), Is.Null);
    }

    [Test]
    public void WeightedChoiceDoesNotMutateInputs()
    {
        string[] values = new string[] { "a", "b", "c" };
        double[] weights = new double[] { 1.0, 2.0, 3.0 };
        string[] originalValues = (string[])values.Clone();
        double[] originalWeights = (double[])weights.Clone();

        Fake.Create(406).Random().WeightedChoice(values, weights);

        Assert.That(values, Is.EqualTo(originalValues));
        Assert.That(weights, Is.EqualTo(originalWeights));
    }

    [Test]
    public void WeightedChoiceRejectsNullEmptyAndMisalignedInputs()
    {
        FakeRandom random = Fake.Create().Random();
        string[] oneString = new string[] { "a" };
        object[] oneObject = new object[] { new object() };

        Assert.That(Assert.Throws<ArgumentNullException>(() => random.WeightedChoice((string[])null, new double[] { 1.0 })).ParamName, Is.EqualTo("values"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => random.WeightedChoiceFromValuesAndWeights((object[])null, new double[] { 1.0 })).ParamName, Is.EqualTo("values"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => random.WeightedChoice(oneString, null)).ParamName, Is.EqualTo("weights"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => random.WeightedChoiceFromValuesAndWeights(oneObject, null)).ParamName, Is.EqualTo("weights"));
        Assert.That(Assert.Throws<ArgumentException>(() => random.WeightedChoice(Array.Empty<string>(), Array.Empty<double>())).ParamName, Is.EqualTo("values"));
        Assert.That(Assert.Throws<ArgumentException>(() => random.WeightedChoice(oneString, Array.Empty<double>())).ParamName, Is.EqualTo("weights"));
        Assert.That(Assert.Throws<ArgumentException>(() => random.WeightedChoiceFromValuesAndWeights(oneObject, new double[] { 1.0, 2.0 })).ParamName, Is.EqualTo("weights"));
    }

    [Test]
    public void WeightedChoiceRejectsInvalidWeights()
    {
        FakeRandom random = Fake.Create().Random();
        string[] values = new string[] { "a", "b" };

        Assert.That(Assert.Throws<ArgumentException>(() => random.WeightedChoice(values, new double[] { 0.0, 0.0 })).ParamName, Is.EqualTo("weights"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => random.WeightedChoice(values, new double[] { 1.0, -1.0 })).ParamName, Is.EqualTo("weights"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => random.WeightedChoice(values, new double[] { 1.0, double.NaN })).ParamName, Is.EqualTo("weights"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => random.WeightedChoice(values, new double[] { 1.0, double.NegativeInfinity })).ParamName, Is.EqualTo("weights"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => random.WeightedChoice(values, new double[] { 1.0, double.PositiveInfinity })).ParamName, Is.EqualTo("weights"));
    }

    [Test]
    public void WeightedChoiceHonorsConfiguredCountLimit()
    {
        FakeOptions options = new FakeOptions();
        options.MaximumCount = 2;
        FakeRandom random = Fake.Create(options).Random();

        Assert.That(
            Assert.Throws<ArgumentOutOfRangeException>(() => random.WeightedChoice(
                new string[] { "a", "b", "c" },
                new double[] { 1.0, 1.0, 1.0 })).ParamName,
            Is.EqualTo("values"));
        Assert.That(
            Assert.Throws<ArgumentOutOfRangeException>(() => random.WeightedChoice(
                new string[] { "a" },
                new double[] { 1.0, 1.0, 1.0 })).ParamName,
            Is.EqualTo("weights"));
    }

    [Test]
    public void SnapshotRestoreReplaysWeightedChoices()
    {
        FakeRandom random = Fake.Create(407).Random();
        string[] values = new string[] { "a", "b", "c" };
        double[] weights = new double[] { 1.0, 2.0, 3.0 };
        FakeState state = random.Snapshot();
        string[] expected = new string[100];
        string[] actual = new string[100];

        for (int index = 0; index < expected.Length; index++)
        {
            expected[index] = random.WeightedChoice(values, weights);
        }
        random.Restore(state);
        for (int index = 0; index < actual.Length; index++)
        {
            actual[index] = random.WeightedChoice(values, weights);
        }

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void CryptographicModeSupportsWeightedChoice()
    {
        FakeOptions options = new FakeOptions();
        options.RandomMode = RandomMode.Cryptographic;
        FakeRandom random = Fake.Create(options).Random();
        object first = new object();
        object second = new object();
        object[] values = new object[] { first, second };

        for (int index = 0; index < 500; index++)
        {
            Assert.That(random.WeightedChoiceFromValuesAndWeights(values, new double[] { 1.0, 1.0 }), Is.SameAs(first).Or.SameAs(second));
        }
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
