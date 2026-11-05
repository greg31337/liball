using fake;

namespace fake.Tests;

[TestFixture]
public class FloatingRandomTests
{
    [Test]
    public void DefaultValuesAreFiniteAndBelowOne()
    {
        FakeRandom random = Fake.Create(101).Random();

        for (int index = 0; index < 1000; index++)
        {
            float floatValue = random.Float();
            double doubleValue = random.Double();
            decimal decimalValue = random.Decimal();
            Assert.That(float.IsFinite(floatValue), Is.True);
            Assert.That(floatValue, Is.GreaterThanOrEqualTo(0.0f).And.LessThan(1.0f));
            Assert.That(double.IsFinite(doubleValue), Is.True);
            Assert.That(doubleValue, Is.GreaterThanOrEqualTo(0.0).And.LessThan(1.0));
            Assert.That(decimalValue, Is.GreaterThanOrEqualTo(decimal.Zero).And.LessThan(decimal.One));
        }
    }

    [Test]
    public void SeedZeroGoldenValuesAreStable()
    {
        float floatValue = Fake.Create(0).Random().Float();
        double doubleValue = Fake.Create(0).Random().Double();
        decimal decimalValue = Fake.Create(0).Random().Decimal();

        Assert.That(floatValue, Is.EqualTo(0.46073460578918457f));
        Assert.That(doubleValue, Is.EqualTo(0.38662280786919334));
        Assert.That(decimalValue, Is.EqualTo(0.2889659338020877500121739956m));
    }

    [Test]
    public void MaximumOnlyOverloadsStayWithinBounds()
    {
        FakeRandom random = Fake.Create(102).Random();

        for (int index = 0; index < 1000; index++)
        {
            Assert.That(random.FloatFromMaximumExclusive(17.5f), Is.GreaterThanOrEqualTo(0.0f).And.LessThan(17.5f));
            Assert.That(random.DoubleFromMaximumExclusive(123456.75), Is.GreaterThanOrEqualTo(0.0).And.LessThan(123456.75));
            Assert.That(random.DecimalFromMaximumExclusive(987654321.125m), Is.GreaterThanOrEqualTo(0m).And.LessThan(987654321.125m));
        }
    }

    [Test]
    public void ExclusiveRangesStayWithinBounds()
    {
        FakeRandom random = Fake.Create(103).Random();

        for (int index = 0; index < 1000; index++)
        {
            Assert.That(random.FloatFromMinimumAndMaximum(-91.25f, 44.5f), Is.GreaterThanOrEqualTo(-91.25f).And.LessThan(44.5f));
            Assert.That(random.FloatFromMinimumAndMaximum(-500.0f, -10.0f), Is.GreaterThanOrEqualTo(-500.0f).And.LessThan(-10.0f));
            Assert.That(random.DoubleFromMinimumAndMaximum(-1.0e200, 1.0e199), Is.GreaterThanOrEqualTo(-1.0e200).And.LessThan(1.0e199));
            Assert.That(random.DoubleFromMinimumAndMaximum(-1.0e200, -1.0e100), Is.GreaterThanOrEqualTo(-1.0e200).And.LessThan(-1.0e100));
            Assert.That(random.DecimalFromMinimumAndMaximum(-123456789.25m, 987654321.75m), Is.GreaterThanOrEqualTo(-123456789.25m).And.LessThan(987654321.75m));
            Assert.That(random.DecimalFromMinimumAndMaximum(-999999m, -1m), Is.GreaterThanOrEqualTo(-999999m).And.LessThan(-1m));
        }
    }

    [Test]
    public void FullFiniteRangesDoNotOverflowOrProduceNonfiniteValues()
    {
        FakeRandom random = Fake.Create(104).Random();

        for (int index = 0; index < 1000; index++)
        {
            float floatValue = random.FloatFromMinimumAndMaximumAndMaximumInclusive(float.MinValue, float.MaxValue, true);
            double doubleValue = random.DoubleFromMinimumAndMaximumAndMaximumInclusive(double.MinValue, double.MaxValue, true);
            decimal decimalValue = random.DecimalFromMinimumAndMaximumAndMaximumInclusive(decimal.MinValue, decimal.MaxValue, true);
            Assert.That(float.IsFinite(floatValue), Is.True);
            Assert.That(floatValue, Is.InRange(float.MinValue, float.MaxValue));
            Assert.That(double.IsFinite(doubleValue), Is.True);
            Assert.That(doubleValue, Is.InRange(double.MinValue, double.MaxValue));
            Assert.That(decimalValue, Is.InRange(decimal.MinValue, decimal.MaxValue));
        }
    }

    [Test]
    public void InclusiveSingletonsReturnTheBoundWithoutAdvancing()
    {
        FakeRandom random = Fake.Create(105).Random();
        FakeState before = random.Snapshot();

        Assert.That(random.FloatFromMinimumAndMaximumAndMaximumInclusive(-3.5f, -3.5f, true), Is.EqualTo(-3.5f));
        Assert.That(random.DoubleFromMinimumAndMaximumAndMaximumInclusive(8.25, 8.25, true), Is.EqualTo(8.25));
        Assert.That(random.DecimalFromMinimumAndMaximumAndMaximumInclusive(-99.125m, -99.125m, true), Is.EqualTo(-99.125m));

        AssertStateEqual(random.Snapshot(), before);
    }

    [Test]
    public void InclusiveMaximumCanBeProducedForFloatAndDouble()
    {
        FakeRandom floatRandom = Fake.Create().Random();
        FakeRandom doubleRandom = Fake.Create().Random();
        floatRandom.Restore(new FakeState(FakeState.StableAlgorithm, 0UL, 1UL, 2046435670677156295UL, 2UL, 3UL));
        doubleRandom.Restore(new FakeState(FakeState.StableAlgorithm, 0UL, 1UL, 5742191168638972359UL, 2UL, 3UL));

        Assert.That(floatRandom.FloatFromMinimumAndMaximumAndMaximumInclusive(2.0f, 5.0f, true), Is.EqualTo(5.0f));
        Assert.That(doubleRandom.DoubleFromMinimumAndMaximumAndMaximumInclusive(-7.0, 11.0, true), Is.EqualTo(11.0));
    }

    [Test]
    public void ExclusiveAdjacentBoundsReturnTheMinimum()
    {
        float floatMinimum = 1.0f;
        float floatMaximum = MathF.BitIncrement(floatMinimum);
        double doubleMinimum = 1.0;
        double doubleMaximum = Math.BitIncrement(doubleMinimum);
        decimal decimalMinimum = 1.0m;
        decimal decimalMaximum = 1.0000000000000000000000000001m;
        FakeRandom random = Fake.Create(106).Random();

        for (int index = 0; index < 100; index++)
        {
            Assert.That(random.FloatFromMinimumAndMaximum(floatMinimum, floatMaximum), Is.EqualTo(floatMinimum));
            Assert.That(random.DoubleFromMinimumAndMaximum(doubleMinimum, doubleMaximum), Is.EqualTo(doubleMinimum));
            Assert.That(random.DecimalFromMinimumAndMaximum(decimalMinimum, decimalMaximum), Is.EqualTo(decimalMinimum));
        }
    }

    [Test]
    public void MaximumOnlyOverloadsRejectNonpositiveValues()
    {
        FakeRandom random = Fake.Create().Random();

        AssertMaximumException(Assert.Throws<ArgumentOutOfRangeException>(() => random.FloatFromMaximumExclusive(0.0f)));
        AssertMaximumException(Assert.Throws<ArgumentOutOfRangeException>(() => random.FloatFromMaximumExclusive(-1.0f)));
        AssertMaximumException(Assert.Throws<ArgumentOutOfRangeException>(() => random.DoubleFromMaximumExclusive(0.0)));
        AssertMaximumException(Assert.Throws<ArgumentOutOfRangeException>(() => random.DoubleFromMaximumExclusive(-1.0)));
        AssertMaximumException(Assert.Throws<ArgumentOutOfRangeException>(() => random.DecimalFromMaximumExclusive(0m)));
        AssertMaximumException(Assert.Throws<ArgumentOutOfRangeException>(() => random.DecimalFromMaximumExclusive(-1m)));
    }

    [Test]
    public void EmptyAndReversedRangesAreRejected()
    {
        FakeRandom random = Fake.Create().Random();

        AssertRangeException(Assert.Throws<ArgumentOutOfRangeException>(() => random.FloatFromMinimumAndMaximum(1.0f, 1.0f)));
        AssertRangeException(Assert.Throws<ArgumentOutOfRangeException>(() => random.FloatFromMinimumAndMaximumAndMaximumInclusive(2.0f, 1.0f, true)));
        AssertRangeException(Assert.Throws<ArgumentOutOfRangeException>(() => random.DoubleFromMinimumAndMaximum(1.0, 1.0)));
        AssertRangeException(Assert.Throws<ArgumentOutOfRangeException>(() => random.DoubleFromMinimumAndMaximumAndMaximumInclusive(2.0, 1.0, true)));
        AssertRangeException(Assert.Throws<ArgumentOutOfRangeException>(() => random.DecimalFromMinimumAndMaximum(1m, 1m)));
        AssertRangeException(Assert.Throws<ArgumentOutOfRangeException>(() => random.DecimalFromMinimumAndMaximumAndMaximumInclusive(2m, 1m, true)));
    }

    [TestCase(float.NaN, 1.0f, "minimum", "NaN")]
    [TestCase(0.0f, float.NaN, "maximum", "NaN")]
    [TestCase(float.NegativeInfinity, 1.0f, "minimum", "finite")]
    [TestCase(0.0f, float.PositiveInfinity, "maximum", "finite")]
    public void FloatRejectsNaNAndInfinity(float minimum, float maximum, string parameterName, string messagePart)
    {
        FakeRandom random = Fake.Create().Random();

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => random.FloatFromMinimumAndMaximum(minimum, maximum));

        Assert.That(exception.ParamName, Is.EqualTo(parameterName));
        Assert.That(exception.Message, Does.Contain(messagePart));
    }

    [TestCase(double.NaN, 1.0, "minimum", "NaN")]
    [TestCase(0.0, double.NaN, "maximum", "NaN")]
    [TestCase(double.NegativeInfinity, 1.0, "minimum", "finite")]
    [TestCase(0.0, double.PositiveInfinity, "maximum", "finite")]
    public void DoubleRejectsNaNAndInfinity(double minimum, double maximum, string parameterName, string messagePart)
    {
        FakeRandom random = Fake.Create().Random();

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => random.DoubleFromMinimumAndMaximum(minimum, maximum));

        Assert.That(exception.ParamName, Is.EqualTo(parameterName));
        Assert.That(exception.Message, Does.Contain(messagePart));
    }

    [Test]
    public void MaximumOnlyFloatAndDoubleRejectNonfiniteBoundsWithPublicParameterName()
    {
        FakeRandom random = Fake.Create().Random();

        ArgumentOutOfRangeException floatException = Assert.Throws<ArgumentOutOfRangeException>(() => random.FloatFromMaximumExclusive(float.PositiveInfinity));
        ArgumentOutOfRangeException doubleException = Assert.Throws<ArgumentOutOfRangeException>(() => random.DoubleFromMaximumExclusive(double.NaN));

        Assert.That(floatException.ParamName, Is.EqualTo("maximumExclusive"));
        Assert.That(doubleException.ParamName, Is.EqualTo("maximumExclusive"));
    }

    [Test]
    public void DecimalGenerationDoesNotUsePublicOutputSizeLimitForInternalEntropy()
    {
        FakeOptions options = new FakeOptions();
        options.MaximumOutputSize = 1;
        FakeRandom random = Fake.Create(options).Random();

        decimal value = random.Decimal();

        Assert.That(value, Is.GreaterThanOrEqualTo(0m).And.LessThan(1m));
    }

    [Test]
    public void CryptographicFloatingGenerationHonorsBounds()
    {
        FakeOptions options = new FakeOptions();
        options.RandomMode = RandomMode.Cryptographic;
        FakeRandom random = Fake.Create(options).Random();

        for (int index = 0; index < 200; index++)
        {
            float floatValue = random.FloatFromMinimumAndMaximum(-500.0f, 250.0f);
            double doubleValue = random.DoubleFromMinimumAndMaximum(-1.0e100, 1.0e100);
            decimal decimalValue = random.DecimalFromMinimumAndMaximum(-1000000m, 1000000m);
            Assert.That(float.IsFinite(floatValue), Is.True);
            Assert.That(floatValue, Is.GreaterThanOrEqualTo(-500.0f).And.LessThan(250.0f));
            Assert.That(double.IsFinite(doubleValue), Is.True);
            Assert.That(doubleValue, Is.GreaterThanOrEqualTo(-1.0e100).And.LessThan(1.0e100));
            Assert.That(decimalValue, Is.GreaterThanOrEqualTo(-1000000m).And.LessThan(1000000m));
        }
    }

    [Test]
    public void SnapshotRestoreReplaysMixedFloatingCalls()
    {
        FakeRandom random = Fake.Create(107).Random();
        FakeState state = random.Snapshot();

        object[] expected = MixedValues(random);
        random.Restore(state);

        Assert.That(MixedValues(random), Is.EqualTo(expected));
    }

    static object[] MixedValues(FakeRandom random)
    {
        return new object[]
        {
            random.Float(),
            random.FloatFromMinimumAndMaximumAndMaximumInclusive(-10.0f, 30.0f, true),
            random.Double(),
            random.DoubleFromMinimumAndMaximumAndMaximumInclusive(-1.0e250, 1.0e250, false),
            random.Decimal(),
            random.DecimalFromMinimumAndMaximumAndMaximumInclusive(-123456789m, 987654321m, true)
        };
    }

    static void AssertMaximumException(ArgumentOutOfRangeException exception)
    {
        Assert.That(exception.ParamName, Is.EqualTo("maximumExclusive"));
    }

    static void AssertRangeException(ArgumentOutOfRangeException exception)
    {
        Assert.That(exception.ParamName, Is.EqualTo("maximum"));
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
