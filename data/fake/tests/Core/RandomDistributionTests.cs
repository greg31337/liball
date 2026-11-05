using fake;

namespace fake.Tests;

[TestFixture]
public class RandomDistributionTests
{
    [Test]
    public void SeedZeroDistributionGoldenValuesAreStable()
    {
        Assert.That(Fake.Create(0).Random().Uniform(), Is.EqualTo(0.38662280786919334));
        Assert.That(Fake.Create(0).Random().Normal(), Is.EqualTo(-0.9223159458585004).Within(1.0e-15));
        Assert.That(Fake.Create(0).Random().LogNormal(), Is.EqualTo(0.3975971604866076).Within(1.0e-15));
        Assert.That(Fake.Create(0).Random().Exponential(), Is.EqualTo(0.4887752106949317).Within(1.0e-15));
        Assert.That(Fake.Create(0).Random().Triangular(), Is.EqualTo(0.4396719276171685).Within(1.0e-15));
        Assert.That(Fake.Create(0).Random().WeightedDiscrete(new double[] { 1.0, 2.0, 7.0 }), Is.EqualTo(2));
    }

    [Test]
    public void UniformDistributionHasExpectedLargeSampleMoments()
    {
        FakeRandom random = Fake.Create(201).Random();
        Moments moments = Sample(200000, () => random.Uniform());

        Assert.That(moments.Mean, Is.EqualTo(0.5).Within(0.005));
        Assert.That(moments.Variance, Is.EqualTo(1.0 / 12.0).Within(0.003));
    }

    [Test]
    public void UniformRangeOverloadsDelegateToBoundedDoubleBehavior()
    {
        FakeRandom uniform = Fake.Create(202).Random();
        FakeRandom bounded = Fake.Create(202).Random();

        for (int index = 0; index < 1000; index++)
        {
            Assert.That(uniform.UniformFromMinimumAndMaximum(-40.0, 90.0), Is.EqualTo(bounded.DoubleFromMinimumAndMaximum(-40.0, 90.0)));
            Assert.That(uniform.UniformFromMinimumAndMaximumAndMaximumInclusive(-40.0, 90.0, true), Is.EqualTo(bounded.DoubleFromMinimumAndMaximumAndMaximumInclusive(-40.0, 90.0, true)));
        }
    }

    [Test]
    public void NormalDistributionHasExpectedLargeSampleMoments()
    {
        const double mean = 12.5;
        const double standardDeviation = 3.25;
        FakeRandom random = Fake.Create(203).Random();
        Moments moments = Sample(200000, () => random.NormalFromMeanAndStandardDeviation(mean, standardDeviation));

        Assert.That(moments.Mean, Is.EqualTo(mean).Within(0.03));
        Assert.That(Math.Sqrt(moments.Variance), Is.EqualTo(standardDeviation).Within(0.03));
    }

    [Test]
    public void LogNormalDistributionHasExpectedLargeSampleMoments()
    {
        const double mean = 0.2;
        const double standardDeviation = 0.5;
        double expectedMean = Math.Exp(mean + (standardDeviation * standardDeviation / 2.0));
        double expectedVariance = (Math.Exp(standardDeviation * standardDeviation) - 1.0)
            * Math.Exp((2.0 * mean) + (standardDeviation * standardDeviation));
        FakeRandom random = Fake.Create(204).Random();
        Moments moments = Sample(200000, () => random.LogNormalFromMeanAndStandardDeviation(mean, standardDeviation));

        Assert.That(moments.Mean, Is.EqualTo(expectedMean).Within(0.015));
        Assert.That(moments.Variance, Is.EqualTo(expectedVariance).Within(0.025));
    }

    [Test]
    public void ExponentialDistributionHasExpectedLargeSampleMoments()
    {
        const double rate = 2.5;
        double expectedMean = 1.0 / rate;
        double expectedVariance = 1.0 / (rate * rate);
        FakeRandom random = Fake.Create(205).Random();
        Moments moments = Sample(200000, () => random.ExponentialFromRate(rate));

        Assert.That(moments.Mean, Is.EqualTo(expectedMean).Within(0.008));
        Assert.That(moments.Variance, Is.EqualTo(expectedVariance).Within(0.008));
    }

    [Test]
    public void TriangularDistributionHasExpectedLargeSampleMoments()
    {
        const double minimum = -2.0;
        const double maximum = 7.0;
        const double mode = 1.0;
        double expectedMean = (minimum + maximum + mode) / 3.0;
        double expectedVariance = ((minimum * minimum) + (maximum * maximum) + (mode * mode)
            - (minimum * maximum) - (minimum * mode) - (maximum * mode)) / 18.0;
        FakeRandom random = Fake.Create(206).Random();
        Moments moments = Sample(200000, () => random.TriangularFromMinimumAndMaximumAndMode(minimum, maximum, mode));

        Assert.That(moments.Mean, Is.EqualTo(expectedMean).Within(0.02));
        Assert.That(moments.Variance, Is.EqualTo(expectedVariance).Within(0.03));
    }

    [Test]
    public void WeightedDiscreteDistributionMatchesRelativeWeights()
    {
        FakeRandom random = Fake.Create(207).Random();
        double[] weights = new double[] { 1.0, 2.0, 7.0 };
        int[] counts = new int[weights.Length];

        for (int index = 0; index < 200000; index++)
        {
            counts[random.WeightedDiscrete(weights)]++;
        }

        Assert.That(counts[0], Is.InRange(19000, 21000));
        Assert.That(counts[1], Is.InRange(39000, 41000));
        Assert.That(counts[2], Is.InRange(139000, 141000));
    }

    [Test]
    public void ZeroStandardDeviationIsDegenerateAndDoesNotAdvanceState()
    {
        FakeRandom random = Fake.Create(208).Random();
        FakeState before = random.Snapshot();

        Assert.That(random.NormalFromMeanAndStandardDeviation(-17.25, 0.0), Is.EqualTo(-17.25));
        Assert.That(random.LogNormalFromMeanAndStandardDeviation(Math.Log(8.0), 0.0), Is.EqualTo(8.0).Within(1.0e-14));

        AssertStateEqual(random.Snapshot(), before);
    }

    [Test]
    public void ExtremeDistributionParametersProduceFiniteValues()
    {
        FakeRandom random = Fake.Create(209).Random();

        for (int index = 0; index < 1000; index++)
        {
            Assert.That(double.IsFinite(random.NormalFromMeanAndStandardDeviation(double.MaxValue, double.MaxValue)), Is.True);
            Assert.That(double.IsFinite(random.LogNormalFromMeanAndStandardDeviation(1000.0, 1.0)), Is.True);
            Assert.That(random.LogNormalFromMeanAndStandardDeviation(-1000.0, 1.0), Is.GreaterThan(0.0));
            Assert.That(double.IsFinite(random.ExponentialFromRate(double.Epsilon)), Is.True);
            double triangular = random.TriangularFromMinimumAndMaximumAndMode(double.MinValue, double.MaxValue, 0.0);
            Assert.That(double.IsFinite(triangular), Is.True);
            Assert.That(triangular, Is.InRange(double.MinValue, double.MaxValue));
        }
    }

    [Test]
    public void SymmetricTriangularOverloadHandlesFullFiniteRange()
    {
        FakeRandom random = Fake.Create(210).Random();

        for (int index = 0; index < 1000; index++)
        {
            double value = random.TriangularFromMinimumAndMaximum(double.MinValue, double.MaxValue);
            Assert.That(double.IsFinite(value), Is.True);
            Assert.That(value, Is.InRange(double.MinValue, double.MaxValue));
        }
    }

    [Test]
    public void TriangularEndpointModesRemainWithinRange()
    {
        FakeRandom random = Fake.Create(211).Random();

        for (int index = 0; index < 1000; index++)
        {
            Assert.That(random.TriangularFromMinimumAndMaximumAndMode(-5.0, 9.0, -5.0), Is.GreaterThanOrEqualTo(-5.0).And.LessThan(9.0));
            Assert.That(random.TriangularFromMinimumAndMaximumAndMode(-5.0, 9.0, 9.0), Is.GreaterThanOrEqualTo(-5.0).And.LessThan(9.0));
        }
    }

    [Test]
    public void TriangularHandlesAdjacentLargeBoundsWithoutPrecisionCollapse()
    {
        double minimum = Math.BitDecrement(double.MaxValue);
        double maximum = double.MaxValue;
        FakeRandom random = Fake.Create(2111).Random();

        for (int index = 0; index < 1000; index++)
        {
            double lowerMode = random.TriangularFromMinimumAndMaximumAndMode(minimum, maximum, minimum);
            double upperMode = random.TriangularFromMinimumAndMaximumAndMode(minimum, maximum, maximum);
            Assert.That(double.IsFinite(lowerMode), Is.True);
            Assert.That(double.IsFinite(upperMode), Is.True);
            Assert.That(lowerMode, Is.EqualTo(minimum));
            Assert.That(upperMode, Is.EqualTo(minimum));
        }
    }

    [Test]
    public void ScaledWeightsProduceIdenticalDeterministicSelections()
    {
        FakeRandom first = Fake.Create(212).Random();
        FakeRandom second = Fake.Create(212).Random();

        for (int index = 0; index < 1000; index++)
        {
            int expected = first.WeightedDiscrete(new double[] { 1.0, 2.0, 7.0 });
            int actual = second.WeightedDiscrete(new double[] { 10.0, 20.0, 70.0 });
            Assert.That(actual, Is.EqualTo(expected));
        }
    }

    [Test]
    public void ZeroWeightsAreNeverSelectedAndInputIsNotModified()
    {
        FakeRandom random = Fake.Create(213).Random();
        double[] weights = new double[] { 0.0, 3.0, 0.0, 7.0, 0.0 };
        double[] original = (double[])weights.Clone();

        for (int index = 0; index < 10000; index++)
        {
            Assert.That(random.WeightedDiscrete(weights), Is.EqualTo(1).Or.EqualTo(3));
        }

        Assert.That(weights, Is.EqualTo(original));
    }

    [Test]
    public void SinglePositiveWeightIsDegenerateAndDoesNotAdvanceState()
    {
        FakeRandom random = Fake.Create(214).Random();
        FakeState before = random.Snapshot();

        int selected = random.WeightedDiscrete(new double[] { 0.0, 0.0, double.MaxValue, 0.0 });

        Assert.That(selected, Is.EqualTo(2));
        AssertStateEqual(random.Snapshot(), before);
    }

    [TestCase(double.NaN, 1.0, "mean")]
    [TestCase(double.PositiveInfinity, 1.0, "mean")]
    [TestCase(0.0, double.NaN, "standardDeviation")]
    [TestCase(0.0, double.PositiveInfinity, "standardDeviation")]
    [TestCase(0.0, -0.1, "standardDeviation")]
    public void NormalAndLogNormalRejectInvalidParameters(double mean, double standardDeviation, string parameterName)
    {
        FakeRandom random = Fake.Create().Random();

        ArgumentOutOfRangeException normalException = Assert.Throws<ArgumentOutOfRangeException>(() => random.NormalFromMeanAndStandardDeviation(mean, standardDeviation));
        ArgumentOutOfRangeException logNormalException = Assert.Throws<ArgumentOutOfRangeException>(() => random.LogNormalFromMeanAndStandardDeviation(mean, standardDeviation));

        Assert.That(normalException.ParamName, Is.EqualTo(parameterName));
        Assert.That(logNormalException.ParamName, Is.EqualTo(parameterName));
    }

    [TestCase(0.0)]
    [TestCase(-1.0)]
    [TestCase(double.NaN)]
    [TestCase(double.NegativeInfinity)]
    [TestCase(double.PositiveInfinity)]
    public void ExponentialRejectsInvalidRate(double rate)
    {
        FakeRandom random = Fake.Create().Random();

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => random.ExponentialFromRate(rate));

        Assert.That(exception.ParamName, Is.EqualTo("rate"));
    }

    [Test]
    public void TriangularRejectsInvalidBoundsAndModes()
    {
        FakeRandom random = Fake.Create().Random();

        AssertParameter(Assert.Throws<ArgumentOutOfRangeException>(() => random.TriangularFromMinimumAndMaximumAndMode(double.NaN, 1.0, 0.5)), "minimum");
        AssertParameter(Assert.Throws<ArgumentOutOfRangeException>(() => random.TriangularFromMinimumAndMaximumAndMode(0.0, double.PositiveInfinity, 0.5)), "maximum");
        AssertParameter(Assert.Throws<ArgumentOutOfRangeException>(() => random.TriangularFromMinimumAndMaximumAndMode(0.0, 1.0, double.NaN)), "mode");
        AssertParameter(Assert.Throws<ArgumentOutOfRangeException>(() => random.TriangularFromMinimumAndMaximumAndMode(1.0, 1.0, 1.0)), "maximum");
        AssertParameter(Assert.Throws<ArgumentOutOfRangeException>(() => random.TriangularFromMinimumAndMaximumAndMode(0.0, 1.0, -0.1)), "mode");
        AssertParameter(Assert.Throws<ArgumentOutOfRangeException>(() => random.TriangularFromMinimumAndMaximumAndMode(0.0, 1.0, 1.1)), "mode");
    }

    [Test]
    public void WeightedDiscreteRejectsInvalidWeights()
    {
        FakeRandom random = Fake.Create().Random();

        Assert.That(Assert.Throws<ArgumentNullException>(() => random.WeightedDiscrete(null)).ParamName, Is.EqualTo("weights"));
        Assert.That(Assert.Throws<ArgumentException>(() => random.WeightedDiscrete(Array.Empty<double>())).ParamName, Is.EqualTo("weights"));
        Assert.That(Assert.Throws<ArgumentException>(() => random.WeightedDiscrete(new double[] { 0.0, 0.0 })).ParamName, Is.EqualTo("weights"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => random.WeightedDiscrete(new double[] { 1.0, -1.0 })).ParamName, Is.EqualTo("weights"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => random.WeightedDiscrete(new double[] { 1.0, double.NaN })).ParamName, Is.EqualTo("weights"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => random.WeightedDiscrete(new double[] { 1.0, double.PositiveInfinity })).ParamName, Is.EqualTo("weights"));
    }

    [Test]
    public void WeightedDiscreteHonorsConfiguredCountLimit()
    {
        FakeOptions options = new FakeOptions();
        options.MaximumCount = 2;
        FakeRandom random = Fake.Create(options).Random();

        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => random.WeightedDiscrete(new double[] { 1.0, 2.0, 3.0 }));

        Assert.That(exception.ParamName, Is.EqualTo("weights"));
    }

    [Test]
    public void SnapshotRestoreReplaysMixedDistributionCalls()
    {
        FakeRandom random = Fake.Create(215).Random();
        FakeState state = random.Snapshot();

        object[] expected = MixedValues(random);
        random.Restore(state);

        Assert.That(MixedValues(random), Is.EqualTo(expected));
    }

    [Test]
    public void CryptographicModeSupportsEveryDistribution()
    {
        FakeOptions options = new FakeOptions();
        options.RandomMode = RandomMode.Cryptographic;
        FakeRandom random = Fake.Create(options).Random();

        for (int index = 0; index < 1000; index++)
        {
            Assert.That(random.UniformFromMinimumAndMaximum(-5.0, 8.0), Is.GreaterThanOrEqualTo(-5.0).And.LessThan(8.0));
            Assert.That(double.IsFinite(random.Normal()), Is.True);
            Assert.That(random.LogNormal(), Is.GreaterThan(0.0));
            Assert.That(random.Exponential(), Is.GreaterThanOrEqualTo(0.0));
            Assert.That(random.Triangular(), Is.GreaterThanOrEqualTo(0.0).And.LessThan(1.0));
            Assert.That(random.WeightedDiscrete(new double[] { 1.0, 2.0, 3.0 }), Is.InRange(0, 2));
        }
    }

    static object[] MixedValues(FakeRandom random)
    {
        return new object[]
        {
            random.UniformFromMinimumAndMaximum(-9.0, 11.0),
            random.NormalFromMeanAndStandardDeviation(4.0, 2.0),
            random.LogNormalFromMeanAndStandardDeviation(0.2, 0.5),
            random.ExponentialFromRate(3.0),
            random.TriangularFromMinimumAndMaximumAndMode(-5.0, 8.0, 1.0),
            random.WeightedDiscrete(new double[] { 2.0, 3.0, 5.0 })
        };
    }

    static Moments Sample(int count, Func<double> generate)
    {
        double sum = 0.0;
        double sumSquares = 0.0;
        for (int index = 0; index < count; index++)
        {
            double value = generate();
            sum += value;
            sumSquares += value * value;
        }
        double mean = sum / count;
        return new Moments(mean, (sumSquares / count) - (mean * mean));
    }

    static void AssertParameter(ArgumentOutOfRangeException exception, string parameterName)
    {
        Assert.That(exception.ParamName, Is.EqualTo(parameterName));
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

    class Moments
    {
        public double Mean;
        public double Variance;

        public Moments(double mean, double variance)
        {
            Mean = mean;
            Variance = variance;
        }
    }
}
