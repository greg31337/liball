namespace random.Tests;

public class TruncatedNormalSamplerTests
{
    [Test]
    public void StandardConstructorMatchesExplicitNormalParameters()
    {
        RandomGenerator firstGenerator = Create(31UL, RandomMathMode.Portable);
        RandomGenerator secondGenerator = Create(31UL, RandomMathMode.Portable);
        TruncatedNormalSampler first = new TruncatedNormalSampler(firstGenerator, 0.0, 1.0, -0.75, 2.5);
        TruncatedNormalSampler second = new TruncatedNormalSampler(secondGenerator, 0.0, 1.0, -0.75, 2.5);

        for (int index = 0; index < 256; index++)
        {
            Assert.That(Bits(first.Sample()), Is.EqualTo(Bits(second.Sample())));
        }

        Assert.That(firstGenerator.ExportState().ToBytes(), Is.EqualTo(secondGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void UnboundedIntervalMatchesNormalSamplerInBothMathModes()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            RandomGenerator actualGenerator = Create(41UL, mode);
            RandomGenerator referenceGenerator = Create(41UL, mode);
            TruncatedNormalSampler actual = new TruncatedNormalSampler(actualGenerator, -1.25, 2.75, double.NegativeInfinity, double.PositiveInfinity);
            NormalSampler reference = new NormalSampler(referenceGenerator, 0.0, 1.0);

            for (int index = 0; index < 256; index++)
            {
                double expected = -1.25 + 2.75 * reference.Sample();
                Assert.That(Bits(actual.Sample()), Is.EqualTo(Bits(expected)));
            }

            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        }
    }

    [Test]
    public void BroadCentralIntervalMatchesNormalRejection()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            RandomGenerator actualGenerator = Create(51UL, mode);
            RandomGenerator referenceGenerator = Create(51UL, mode);
            TruncatedNormalSampler actual = new TruncatedNormalSampler(actualGenerator, 0.0, 1.0, -2.0, 1.0);
            NormalSampler normal = new NormalSampler(referenceGenerator, 0.0, 1.0);

            for (int index = 0; index < 256; index++)
            {
                double expected;
                do
                {
                    expected = normal.Sample();
                }
                while (expected < -2.0 || expected > 1.0);

                Assert.That(Bits(actual.Sample()), Is.EqualTo(Bits(expected)));
            }

            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        }
    }

    [Test]
    public void OneSidedTailsMatchOptimizedExponentialProposalAndReflectExactly()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            RandomGenerator actualGenerator = Create(61UL, mode);
            RandomGenerator referenceGenerator = Create(61UL, mode);
            RandomGenerator reflectedGenerator = Create(61UL, mode);
            TruncatedNormalSampler actual = new TruncatedNormalSampler(actualGenerator, 0.0, 1.0, 2.0, double.PositiveInfinity);
            TruncatedNormalSampler reflected = new TruncatedNormalSampler(reflectedGenerator, 0.0, 1.0, double.NegativeInfinity, -2.0);

            for (int index = 0; index < 256; index++)
            {
                double expected = ReferencePositiveTail(referenceGenerator, 2.0, double.PositiveInfinity, mode);
                double value = actual.Sample();
                Assert.Multiple(() =>
                {
                    Assert.That(Bits(value), Is.EqualTo(Bits(expected)));
                    Assert.That(Bits(reflected.Sample()), Is.EqualTo(Bits(-value)));
                });
            }

            Assert.Multiple(() =>
            {
                Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
                Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(reflectedGenerator.ExportState().ToBytes()));
            });
        }
    }

    [Test]
    public void FiniteTailIntervalMatchesTruncatedExponentialProposal()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            RandomGenerator actualGenerator = Create(71UL, mode);
            RandomGenerator referenceGenerator = Create(71UL, mode);
            TruncatedNormalSampler actual = new TruncatedNormalSampler(actualGenerator, 0.0, 1.0, 2.0, 5.0);

            for (int index = 0; index < 256; index++)
            {
                double expected = ReferencePositiveTail(referenceGenerator, 2.0, 5.0, mode);
                Assert.That(Bits(actual.Sample()), Is.EqualTo(Bits(expected)));
            }

            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        }
    }

    [Test]
    public void CentralAndRemoteNarrowIntervalsMatchUniformEnvelopeProposal()
    {
        double[][] intervals =
        [
            [-0.01, 0.02],
            [10.0, 10.01],
            [-10.01, -10.0],
            [8.0, Math.BitIncrement(8.0)]
        ];
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];

        foreach (RandomMathMode mode in modes)
        {
            foreach (double[] interval in intervals)
            {
                RandomGenerator actualGenerator = Create(81UL, mode);
                RandomGenerator referenceGenerator = Create(81UL, mode);
                TruncatedNormalSampler actual = new TruncatedNormalSampler(actualGenerator, 0.0, 1.0, interval[0], interval[1]);

                for (int index = 0; index < 256; index++)
                {
                    double expected = ReferenceUniformEnvelope(referenceGenerator, interval[0], interval[1], mode);
                    double value = actual.Sample();
                    Assert.Multiple(() =>
                    {
                        Assert.That(Bits(value), Is.EqualTo(Bits(expected)));
                        Assert.That(value, Is.GreaterThanOrEqualTo(interval[0]).And.LessThanOrEqualTo(interval[1]));
                    });
                }

                Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
            }
        }
    }

    [Test]
    public void LocationScaleBoundsAreAppliedInReturnedCoordinates()
    {
        TruncatedNormalSampler sampler = new TruncatedNormalSampler(Create(91UL, RandomMathMode.Portable), 5.0, 2.0, 4.0, 8.0);

        for (int index = 0; index < 4096; index++)
        {
            Assert.That(sampler.Sample(), Is.InRange(4.0, 8.0));
        }
    }

    [Test]
    public void CentralAndTailMomentsMatchTruncatedNormalTheory()
    {
        const int sampleCount = 100000;
        TruncatedNormalSampler central = new TruncatedNormalSampler(Create(101UL, RandomMathMode.Portable), 0.0, 1.0, -1.0, 2.0);
        TruncatedNormalSampler tail = new TruncatedNormalSampler(Create(102UL, RandomMathMode.Portable), 0.0, 1.0, 2.0, double.PositiveInfinity);
        double centralSum = 0.0;
        double centralSquareSum = 0.0;
        double tailSum = 0.0;
        double tailSquareSum = 0.0;

        for (int index = 0; index < sampleCount; index++)
        {
            double centralValue = central.Sample();
            double tailValue = tail.Sample();
            centralSum += centralValue;
            centralSquareSum += centralValue * centralValue;
            tailSum += tailValue;
            tailSquareSum += tailValue * tailValue;
        }

        double centralMean = centralSum / sampleCount;
        double centralVariance = centralSquareSum / sampleCount - centralMean * centralMean;
        double tailMean = tailSum / sampleCount;
        double tailVariance = tailSquareSum / sampleCount - tailMean * tailMean;
        double centralProbability = Cdf(2.0) - Cdf(-1.0);
        double expectedCentralMean = (Density(-1.0) - Density(2.0)) / centralProbability;
        double expectedCentralVariance = 1.0 +
            (-Density(-1.0) - 2.0 * Density(2.0)) / centralProbability -
            expectedCentralMean * expectedCentralMean;
        double tailProbability = 1.0 - Cdf(2.0);
        double expectedTailMean = Density(2.0) / tailProbability;
        double expectedTailVariance = 1.0 + 2.0 * expectedTailMean - expectedTailMean * expectedTailMean;

        Assert.Multiple(() =>
        {
            Assert.That(centralMean, Is.EqualTo(expectedCentralMean).Within(0.008));
            Assert.That(centralVariance, Is.EqualTo(expectedCentralVariance).Within(0.008));
            Assert.That(tailMean, Is.EqualTo(expectedTailMean).Within(0.006));
            Assert.That(tailVariance, Is.EqualTo(expectedTailVariance).Within(0.006));
        });
    }

    [Test]
    public void ExtremeAndStandardizationCollapsedIntervalsTerminateWithinBounds()
    {
        TruncatedNormalSampler farPositive = new TruncatedNormalSampler(Create(111UL, RandomMathMode.Portable), 0.0, double.Epsilon, 1.0, 2.0);
        TruncatedNormalSampler farNegative = new TruncatedNormalSampler(Create(112UL, RandomMathMode.Portable), 0.0, double.Epsilon, -2.0, -1.0);
        RandomGenerator flatGenerator = Create(113UL, RandomMathMode.Portable);
        byte[] flatBefore = flatGenerator.ExportState().ToBytes();
        TruncatedNormalSampler flat = new TruncatedNormalSampler(flatGenerator, 1.0, double.MaxValue, 1.0, Math.BitIncrement(1.0));

        Assert.Multiple(() =>
        {
            Assert.That(farPositive.Sample(), Is.EqualTo(1.0));
            Assert.That(farNegative.Sample(), Is.EqualTo(-1.0));
            Assert.That(flat.Sample(), Is.InRange(1.0, Math.BitIncrement(1.0)));
            Assert.That(flatGenerator.ExportState().ToBytes(), Is.Not.EqualTo(flatBefore));
        });
    }

    [Test]
    public void EqualBoundsAndZeroDeviationReturnPointMassesWithoutConsumption()
    {
        RandomGenerator generator = Create(121UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        TruncatedNormalSampler equal = new TruncatedNormalSampler(generator, -3.5, 2.0, 7.25, 7.25);
        TruncatedNormalSampler zero = new TruncatedNormalSampler(generator, -3.5, -0.0, -4.0, 10.0);
        double[] values = new double[16];

        Assert.Multiple(() =>
        {
            Assert.That(equal.Sample(), Is.EqualTo(7.25));
            Assert.That(zero.Sample(), Is.EqualTo(-3.5));
        });
        equal.Fill(values);
        Assert.That(values, Is.All.EqualTo(7.25));
        zero.Fill(values);
        Assert.Multiple(() =>
        {
            Assert.That(values, Is.All.EqualTo(-3.5));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [Test]
    public void FillMatchesRepeatedSamplesAcrossRegimes()
    {
        double[][] cases =
        [
            [0.0, 1.0, double.NegativeInfinity, double.PositiveInfinity],
            [0.0, 1.0, -2.0, 1.0],
            [0.0, 1.0, 2.0, double.PositiveInfinity],
            [0.0, 1.0, 2.0, 5.0],
            [0.0, 1.0, 10.0, 10.01],
            [2.5, 1.75, 2.0, 4.0],
            [2.5, 0.0, 2.0, 4.0]
        ];

        foreach (double[] parameters in cases)
        {
            RandomGenerator fillGenerator = Create(131UL, RandomMathMode.Portable);
            RandomGenerator scalarGenerator = Create(131UL, RandomMathMode.Portable);
            TruncatedNormalSampler fillSampler = new TruncatedNormalSampler(fillGenerator, parameters[0], parameters[1], parameters[2], parameters[3]);
            TruncatedNormalSampler scalarSampler = new TruncatedNormalSampler(scalarGenerator, parameters[0], parameters[1], parameters[2], parameters[3]);
            double[] actual = new double[129];
            double[] expected = new double[actual.Length];
            byte[] before = fillGenerator.ExportState().ToBytes();

            fillSampler.Fill(Span<double>.Empty);
            Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(before));
            fillSampler.Fill(actual);
            for (int index = 0; index < expected.Length; index++)
            {
                expected[index] = scalarSampler.Sample();
            }

            for (int index = 0; index < actual.Length; index++)
            {
                Assert.That(Bits(actual[index]), Is.EqualTo(Bits(expected[index])));
            }

            Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(scalarGenerator.ExportState().ToBytes()));
        }
    }

    [Test]
    public void InvalidParametersAreRejectedWithoutConsumption()
    {
        Assert.That(
            Assert.Throws<RandomArgumentException>(() => new TruncatedNormalSampler(NullReference.Value<global::random.IRandomSource>(), 0.0, 1.0, -1.0, 1.0))?.ArgumentName(),
            Is.EqualTo("generator"));

        RandomGenerator generator = Create(141UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        object[][] cases =
        [
            [(TestDelegate)(() => new TruncatedNormalSampler(generator, double.NaN, 1.0, -1.0, 1.0)), "mean"],
            [(TestDelegate)(() => new TruncatedNormalSampler(generator, 0.0, double.PositiveInfinity, -1.0, 1.0)), "standardDeviation"],
            [(TestDelegate)(() => new TruncatedNormalSampler(generator, 0.0, -double.Epsilon, -1.0, 1.0)), "standardDeviation"],
            [(TestDelegate)(() => new TruncatedNormalSampler(generator, 0.0, 1.0, double.NaN, 1.0)), "lowerBound"],
            [(TestDelegate)(() => new TruncatedNormalSampler(generator, 0.0, 1.0, -1.0, double.NaN)), "upperBound"],
            [(TestDelegate)(() => new TruncatedNormalSampler(generator, 0.0, 1.0, 2.0, 1.0)), "upperBound"],
            [(TestDelegate)(() => new TruncatedNormalSampler(generator, 0.0, 1.0, double.PositiveInfinity, double.PositiveInfinity)), "upperBound"],
            [(TestDelegate)(() => new TruncatedNormalSampler(generator, 0.0, 1.0, double.NegativeInfinity, double.NegativeInfinity)), "upperBound"],
            [(TestDelegate)(() => new TruncatedNormalSampler(generator, 0.0, 0.0, 1.0, 2.0)), "standardDeviation"]
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        }
    }

    static double ReferencePositiveTail(RandomGenerator generator, double lower, double upper, RandomMathMode mode)
    {
        double rate = lower > 1.0e154
            ? lower
            : 0.5 * lower + Sqrt(0.25 * lower * lower + 1.0, mode);
        while (true)
        {
            double distance;
            if (double.IsPositiveInfinity(upper))
            {
                distance = -Log(generator.NextDoubleOpenOpen(), mode) / rate;
            }
            else
            {
                double retainedProbability = -Expm1(-rate * (upper - lower), mode);
                distance = -Log1p(-retainedProbability * generator.NextDoubleOpenOpen(), mode) / rate;
            }

            double candidate = Math.Min(upper, lower + distance);
            double difference = candidate - rate;
            if (Log(generator.NextDoubleOpenOpen(), mode) <= -0.5 * difference * difference)
            {
                return candidate;
            }
        }
    }

    static double ReferenceUniformEnvelope(RandomGenerator generator, double lower, double upper, RandomMathMode mode)
    {
        double modeValue = lower > 0.0 ? lower : upper < 0.0 ? upper : 0.0;
        double width = upper - lower;
        while (true)
        {
            double unit = generator.NextDoubleOpenOpen();
            double candidate = unit < 0.5
                ? lower + width * unit
                : upper - width * (1.0 - unit);
            double logAcceptance = -0.5 * (candidate - modeValue) * (candidate + modeValue);
            if (Log(generator.NextDoubleOpenOpen(), mode) <= logAcceptance)
            {
                return candidate;
            }
        }
    }

    static double Cdf(double value)
    {
        return 0.5 * PortableSpecialFunctions.Erfc(-value / 1.41421356237309504880);
    }

    static double Density(double value)
    {
        return PortableMath.Exp(-0.5 * value * value) / 2.50662827463100050242;
    }

    static double Sqrt(double value, RandomMathMode mode)
    {
        return mode == RandomMathMode.Portable ? PortableMath.Sqrt(value) : Math.Sqrt(value);
    }

    static double Log(double value, RandomMathMode mode)
    {
        return mode == RandomMathMode.Portable ? PortableMath.Log(value) : Math.Log(value);
    }

    static double Log1p(double value, RandomMathMode mode)
    {
        if (mode == RandomMathMode.Portable)
        {
            return PortableMath.Log1p(value);
        }

        if (value < -0.5 || value > 0.5)
        {
            return Math.Log(1.0 + value);
        }

        double reduced = value / (2.0 + value);
        double squared = reduced * reduced;
        double term = reduced;
        double sum = term;
        for (int denominator = 3; denominator <= 39; denominator += 2)
        {
            term *= squared;
            sum += term / denominator;
        }

        return 2.0 * sum;
    }

    static double Expm1(double value, RandomMathMode mode)
    {
        if (mode == RandomMathMode.Portable)
        {
            return PortableMath.Expm1(value);
        }

        if (Math.Abs(value) <= 0.5)
        {
            double half = value * 0.5;
            return 2.0 * Math.Exp(half) * Math.Sinh(half);
        }

        return Math.Exp(value) - 1.0;
    }

    static ulong Bits(double value)
    {
        return PortableMath.DoubleBits(value);
    }

    static RandomGenerator Create(ulong seed, RandomMathMode mode)
    {
        return RandomGenerator.CreateSplitMix64(seed, mode);
    }
}
