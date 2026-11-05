using fake;

namespace fake.Tests;

[TestFixture]
public class ProbabilityTests
{
    [Test]
    public void SeedZeroBooleanGoldenSequenceIsStable()
    {
        bool[] expected = new bool[]
        {
            false, false, false, false, true,
            false, false, true, true, true
        };
        FakeRandom random = Fake.Create(0).Random();
        bool[] actual = new bool[expected.Length];

        for (int index = 0; index < actual.Length; index++)
        {
            actual[index] = random.Bool();
        }

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void UniformBooleanHasNoLargeOutcomeSkew()
    {
        FakeRandom random = Fake.Create(120).Random();
        int trueCount = 0;

        for (int index = 0; index < 100000; index++)
        {
            if (random.Bool())
            {
                trueCount++;
            }
        }

        Assert.That(trueCount, Is.InRange(48500, 51500));
    }

    [Test]
    public void ExactProbabilityEndpointsDoNotAdvanceState()
    {
        FakeRandom random = Fake.Create(121).Random();
        FakeState before = random.Snapshot();

        Assert.That(random.Probability(0.0), Is.False);
        Assert.That(random.Probability(1.0), Is.True);
        Assert.That(random.BoolFromProbability(0.0), Is.False);
        Assert.That(random.BoolFromProbability(1.0), Is.True);

        AssertStateEqual(random.Snapshot(), before);
    }

    [Test]
    public void ExactRatioEndpointsDoNotAdvanceState()
    {
        FakeRandom random = Fake.Create(122).Random();
        FakeState before = random.Snapshot();

        Assert.That(random.Ratio(0, 17), Is.False);
        Assert.That(random.Ratio(17, 17), Is.True);
        Assert.That(random.ProbabilityFromNumeratorAndDenominator(0, long.MaxValue), Is.False);
        Assert.That(random.BoolFromNumeratorAndDenominator(long.MaxValue, long.MaxValue), Is.True);

        AssertStateEqual(random.Snapshot(), before);
    }

    [Test]
    public void ProbabilityHasNoLargeExpectedRateSkew()
    {
        FakeRandom random = Fake.Create(123).Random();
        int trueCount = 0;

        for (int index = 0; index < 100000; index++)
        {
            if (random.Probability(0.25))
            {
                trueCount++;
            }
        }

        Assert.That(trueCount, Is.InRange(24000, 26000));
    }

    [Test]
    public void RatioHasNoLargeExpectedRateSkew()
    {
        FakeRandom random = Fake.Create(124).Random();
        int trueCount = 0;

        for (int index = 0; index < 100000; index++)
        {
            if (random.Ratio(1, 4))
            {
                trueCount++;
            }
        }

        Assert.That(trueCount, Is.InRange(24000, 26000));
    }

    [Test]
    public void RatioAliasesProduceTheSameDeterministicDecisions()
    {
        FakeRandom ratio = Fake.Create(125).Random();
        FakeRandom probability = Fake.Create(125).Random();
        FakeRandom boolean = Fake.Create(125).Random();

        for (int index = 0; index < 1000; index++)
        {
            bool expected = ratio.Ratio(7, 19);
            Assert.That(probability.ProbabilityFromNumeratorAndDenominator(7, 19), Is.EqualTo(expected));
            Assert.That(boolean.BoolFromNumeratorAndDenominator(7, 19), Is.EqualTo(expected));
        }
    }

    [Test]
    public void BooleanProbabilityAliasProducesTheSameDeterministicDecisions()
    {
        FakeRandom probability = Fake.Create(1251).Random();
        FakeRandom boolean = Fake.Create(1251).Random();

        for (int index = 0; index < 1000; index++)
        {
            Assert.That(boolean.BoolFromProbability(0.375), Is.EqualTo(probability.Probability(0.375)));
        }
    }

    [TestCase(-0.000001)]
    [TestCase(1.000001)]
    [TestCase(double.NaN)]
    [TestCase(double.NegativeInfinity)]
    [TestCase(double.PositiveInfinity)]
    public void ProbabilityRejectsInvalidValues(double probability)
    {
        FakeRandom random = Fake.Create().Random();
        FakeState before = random.Snapshot();

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => random.Probability(probability));

        Assert.That(exception.ParamName, Is.EqualTo("probability"));
        AssertStateEqual(random.Snapshot(), before);
    }

    [TestCase(1L, 0L, "denominator")]
    [TestCase(1L, -1L, "denominator")]
    [TestCase(-1L, 10L, "numerator")]
    [TestCase(11L, 10L, "numerator")]
    public void RatioRejectsInvalidValues(long numerator, long denominator, string parameterName)
    {
        FakeRandom random = Fake.Create().Random();
        FakeState before = random.Snapshot();

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => random.Ratio(numerator, denominator));

        Assert.That(exception.ParamName, Is.EqualTo(parameterName));
        AssertStateEqual(random.Snapshot(), before);
    }

    [Test]
    public void LargeRatioDenominatorDoesNotOverflow()
    {
        FakeRandom random = Fake.Create(126).Random();

        for (int index = 0; index < 1000; index++)
        {
            bool value = random.Ratio(long.MaxValue - 1L, long.MaxValue);
            Assert.That(value, Is.TypeOf<bool>());
        }
    }

    [Test]
    public void MaybeValueHonorsProbabilityAndRatioEndpoints()
    {
        FakeRandom random = Fake.Create(127).Random();
        object value = new object();
        FakeState before = random.Snapshot();

        Assert.That(random.MaybeFromValueAndProbability(value, 0.0), Is.Null);
        Assert.That(random.MaybeFromValueAndProbability(value, 1.0), Is.SameAs(value));
        Assert.That(random.MaybeFromValueAndNumeratorAndDenominator(value, 0, 9), Is.Null);
        Assert.That(random.MaybeFromValueAndNumeratorAndDenominator(value, 9, 9), Is.SameAs(value));

        AssertStateEqual(random.Snapshot(), before);
    }

    [Test]
    public void DefaultMaybeProducesPresentAndMissingValues()
    {
        FakeRandom random = Fake.Create(128).Random();
        object value = new object();
        int present = 0;
        int missing = 0;

        for (int index = 0; index < 1000; index++)
        {
            object result = random.Maybe(value);
            if (result == null)
            {
                missing++;
            }
            else
            {
                Assert.That(result, Is.SameAs(value));
                present++;
            }
        }

        Assert.That(present, Is.GreaterThan(0));
        Assert.That(missing, Is.GreaterThan(0));
    }

    [Test]
    public void MaybeFactoryIsLazyAndInvokedOnceWhenSelected()
    {
        FakeRandom random = Fake.Create(129).Random();
        int calls = 0;
        Func<object> factory = () =>
        {
            calls++;
            return "created";
        };

        object missing = random.MaybeFromFactoryAndProbability(factory, 0.0);
        object present = random.MaybeFromFactoryAndProbability(factory, 1.0);

        Assert.That(missing, Is.Null);
        Assert.That(present, Is.EqualTo("created"));
        Assert.That(calls, Is.EqualTo(1));
    }

    [Test]
    public void DefaultMaybeFactoryIsOnlyInvokedForPresentOutcomes()
    {
        FakeRandom random = Fake.Create(1291).Random();
        int calls = 0;
        int present = 0;
        Func<object> factory = () =>
        {
            calls++;
            return new object();
        };

        for (int index = 0; index < 1000; index++)
        {
            if (random.MaybeFromFactory(factory) != null)
            {
                present++;
            }
        }

        Assert.That(present, Is.GreaterThan(0).And.LessThan(1000));
        Assert.That(calls, Is.EqualTo(present));
    }

    [Test]
    public void MaybeFactorySupportsExactRatiosAndNullResults()
    {
        FakeRandom random = Fake.Create(130).Random();
        int calls = 0;
        Func<object> factory = () =>
        {
            calls++;
            return null;
        };

        Assert.That(random.MaybeFromFactoryAndNumeratorAndDenominator(factory, 0, 1), Is.Null);
        Assert.That(random.MaybeFromFactoryAndNumeratorAndDenominator(factory, 1, 1), Is.Null);
        Assert.That(calls, Is.EqualTo(1));
    }

    [Test]
    public void MaybeFactoryRejectsNull()
    {
        FakeRandom random = Fake.Create().Random();

        ArgumentNullException defaultException = Assert.Throws<ArgumentNullException>(() => random.MaybeFromFactory((Func<object>)null));
        ArgumentNullException probabilityException = Assert.Throws<ArgumentNullException>(() => random.MaybeFromFactoryAndProbability((Func<object>)null, 0.5));
        ArgumentNullException ratioException = Assert.Throws<ArgumentNullException>(() => random.MaybeFromFactoryAndNumeratorAndDenominator((Func<object>)null, 1, 2));

        Assert.That(defaultException.ParamName, Is.EqualTo("factory"));
        Assert.That(probabilityException.ParamName, Is.EqualTo("factory"));
        Assert.That(ratioException.ParamName, Is.EqualTo("factory"));
    }

    [Test]
    public void InvalidMaybeProbabilityDoesNotInvokeFactoryOrAdvanceState()
    {
        FakeRandom random = Fake.Create(131).Random();
        FakeState before = random.Snapshot();
        int calls = 0;
        Func<object> factory = () =>
        {
            calls++;
            return "unexpected";
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => random.MaybeFromFactoryAndProbability(factory, 2.0));

        Assert.That(calls, Is.Zero);
        AssertStateEqual(random.Snapshot(), before);
    }

    [Test]
    public void SnapshotRestoreReplaysProbabilityAndMaybeDecisions()
    {
        FakeRandom random = Fake.Create(132).Random();
        FakeState state = random.Snapshot();

        object[] expected = MixedValues(random);
        random.Restore(state);

        Assert.That(MixedValues(random), Is.EqualTo(expected));
    }

    [Test]
    public void CryptographicModeSupportsNontrivialProbabilitiesAndRatios()
    {
        FakeOptions options = new FakeOptions();
        options.RandomMode = RandomMode.Cryptographic;
        FakeRandom random = Fake.Create(options).Random();
        int probabilityTrue = 0;
        int ratioTrue = 0;

        for (int index = 0; index < 500; index++)
        {
            if (random.Probability(0.5))
            {
                probabilityTrue++;
            }
            if (random.Ratio(1, 2))
            {
                ratioTrue++;
            }
        }

        Assert.That(probabilityTrue, Is.InRange(150, 350));
        Assert.That(ratioTrue, Is.InRange(150, 350));
    }

    static object[] MixedValues(FakeRandom random)
    {
        return new object[]
        {
            random.Bool(),
            random.Probability(0.125),
            random.Ratio(5, 11),
            random.Maybe("value"),
            random.MaybeFromValueAndProbability("probability", 0.75),
            random.MaybeFromValueAndNumeratorAndDenominator("ratio", 3, 8)
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
