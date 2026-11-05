namespace random.Tests;

public class TriangularSamplerTests
{
    const ulong ZeroOutputSeed = 0x61C8864680B583EBUL;
    const ulong OneOutputSeed = 0xF8364607E9C949BDUL;
    const ulong UInt64MaximumOutputSeed = 0x31628AF67B2131ABUL;

    [Test]
    public void StandardConstructorMatchesExplicitUnitTriangle()
    {
        RandomGenerator standardGenerator = Create(42UL, RandomMathMode.Portable);
        RandomGenerator explicitGenerator = Create(42UL, RandomMathMode.Portable);
        TriangularSampler standard = new TriangularSampler(standardGenerator, 0.0, 1.0, 0.5);
        TriangularSampler explicitSampler = new TriangularSampler(explicitGenerator, 0.0, 1.0, 0.5);

        for (int index = 0; index < 128; index++)
        {
            Assert.That(Bits(standard.Sample()), Is.EqualTo(Bits(explicitSampler.Sample())));
        }

        Assert.That(standardGenerator.ExportState().ToBytes(), Is.EqualTo(explicitGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void SamplesMatchCancellationSafeBranchesInBothMathModes()
    {
        double[][] parameters =
        [
            [-2.0, 5.0, 1.5],
            [-2.0, 5.0, -2.0],
            [-2.0, 5.0, 5.0],
            [-double.MaxValue, double.MaxValue, 0.0]
        ];
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (double[] values in parameters)
        {
            foreach (RandomMathMode mode in modes)
            {
                RandomGenerator actualGenerator = Create(987UL, mode);
                RandomGenerator referenceGenerator = Create(987UL, mode);
                TriangularSampler sampler = new TriangularSampler(actualGenerator, values[0], values[1], values[2]);

                for (int index = 0; index < 256; index++)
                {
                    double unit = referenceGenerator.NextDouble();
                    double expected = ReferenceSample(unit, values[0], values[1], values[2], mode);
                    Assert.That(Bits(sampler.Sample()), Is.EqualTo(Bits(expected)));
                }

                Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
            }
        }
    }

    [Test]
    public void EqualBoundsReturnPointMassWithoutConsumption()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            RandomGenerator generator = Create(71UL, mode);
            byte[] before = generator.ExportState().ToBytes();
            TriangularSampler sampler = new TriangularSampler(generator, -91.5, -91.5, -91.5);
            double[] values = new double[16];

            double scalar = sampler.Sample();
            sampler.Fill(values);

            Assert.Multiple(() =>
            {
                Assert.That(scalar, Is.EqualTo(-91.5));
                Assert.That(values, Is.All.EqualTo(-91.5));
                Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
            });
        }
    }

    [Test]
    public void FillMatchesRepeatedSamplesAndEmptyFillConsumesNothing()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            RandomGenerator fillGenerator = Create(123UL, mode);
            RandomGenerator scalarGenerator = Create(123UL, mode);
            TriangularSampler fillSampler = new TriangularSampler(fillGenerator, -4.0, 7.0, 2.0);
            TriangularSampler scalarSampler = new TriangularSampler(scalarGenerator, -4.0, 7.0, 2.0);
            double[] actual = new double[257];
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
        Assert.That(Assert.Throws<RandomArgumentException>(() => new TriangularSampler(NullReference.Value<global::random.IRandomSource>(), 0.0, 1.0, 0.5))?.ArgumentName(), Is.EqualTo("generator"));
        Assert.That(Assert.Throws<RandomArgumentException>(() => new TriangularSampler(NullReference.Value<global::random.IRandomSource>(), 0.0, 1.0, 0.5))?.ArgumentName(), Is.EqualTo("generator"));

        RandomGenerator generator = Create(23UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        object[][] cases =
        [
            [(TestDelegate)(() => new TriangularSampler(generator, double.NaN, 1.0, 0.5)), "minimum"],
            [(TestDelegate)(() => new TriangularSampler(generator, 0.0, double.PositiveInfinity, 0.5)), "maximum"],
            [(TestDelegate)(() => new TriangularSampler(generator, 2.0, 1.0, 1.5)), "maximum"],
            [(TestDelegate)(() => new TriangularSampler(generator, 0.0, 1.0, double.NaN)), "mode"],
            [(TestDelegate)(() => new TriangularSampler(generator, 0.0, 1.0, -double.Epsilon)), "mode"],
            [(TestDelegate)(() => new TriangularSampler(generator, 0.0, 1.0, PortableMath.NextUp(1.0))), "mode"],
            [(TestDelegate)(() => new TriangularSampler(generator, 1.0, 1.0, 0.0)), "mode"]
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        }
    }

    [Test]
    public void ClosedUniformZeroMapsExactlyToMinimumForEveryModePosition()
    {
        double[] modes = [-3.0, 0.0, 5.0];
        foreach (double mode in modes)
        {
            RandomGenerator generator = Create(ZeroOutputSeed, RandomMathMode.Portable);
            RandomGenerator reference = Create(ZeroOutputSeed, RandomMathMode.Portable);
            TriangularSampler sampler = new TriangularSampler(generator, -3.0, 5.0, mode);
            double unit = reference.NextDouble();

            Assert.Multiple(() =>
            {
                Assert.That(unit, Is.EqualTo(0.0));
                Assert.That(sampler.Sample(), Is.EqualTo(-3.0));
                Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(reference.ExportState().ToBytes()));
            });
        }
    }

    [Test]
    public void NearEndpointGridValuesStayInsideBounds()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            double nearMinimum = new TriangularSampler(Create(OneOutputSeed, mode), -3.0, 5.0, 0.0).Sample();
            double nearMaximum = new TriangularSampler(Create(UInt64MaximumOutputSeed, mode), -3.0, 5.0, 5.0).Sample();
            double adjacentMaximum = PortableMath.NextUp(1.0);
            TriangularSampler adjacent = new TriangularSampler(Create(77UL, mode), 1.0, adjacentMaximum, 1.0);

            Assert.Multiple(() =>
            {
                Assert.That(nearMinimum, Is.GreaterThanOrEqualTo(-3.0).And.LessThanOrEqualTo(5.0));
                Assert.That(nearMaximum, Is.GreaterThanOrEqualTo(-3.0).And.LessThanOrEqualTo(5.0));
            });
            for (int index = 0; index < 128; index++)
            {
                Assert.That(adjacent.Sample(), Is.EqualTo(1.0).Or.EqualTo(adjacentMaximum));
            }
        }
    }

    [Test]
    public void OverflowingWidthProducesFiniteInRangeValuesAcrossBothBranches()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            TriangularSampler sampler = new TriangularSampler(Create(211UL, mode), -double.MaxValue, double.MaxValue, 0.0);
            bool foundNegative = false;
            bool foundPositive = false;
            for (int index = 0; index < 10000; index++)
            {
                double value = sampler.Sample();
                Assert.That(double.IsFinite(value), Is.True);
                Assert.That(value, Is.InRange(-double.MaxValue, double.MaxValue));
                if (value < 0.0)
                {
                    foundNegative = true;
                }
                else if (value > 0.0)
                {
                    foundPositive = true;
                }
            }

            Assert.Multiple(() =>
            {
                Assert.That(foundNegative, Is.True);
                Assert.That(foundPositive, Is.True);
            });
        }
    }

    [Test]
    public void DeterministicMeansIncludeEndpointModes()
    {
        const int sampleCount = 120000;
        TriangularSampler centered = new TriangularSampler(Create(301UL, RandomMathMode.Portable), -2.0, 4.0, 1.0);
        TriangularSampler falling = new TriangularSampler(Create(302UL, RandomMathMode.Portable), -2.0, 4.0, -2.0);
        TriangularSampler rising = new TriangularSampler(Create(303UL, RandomMathMode.Portable), -2.0, 4.0, 4.0);
        double centeredSum = 0.0;
        double fallingSum = 0.0;
        double risingSum = 0.0;

        for (int index = 0; index < sampleCount; index++)
        {
            centeredSum += centered.Sample();
            fallingSum += falling.Sample();
            risingSum += rising.Sample();
        }

        Assert.Multiple(() =>
        {
            Assert.That(centeredSum / sampleCount, Is.EqualTo(1.0).Within(0.02));
            Assert.That(fallingSum / sampleCount, Is.EqualTo(0.0).Within(0.02));
            Assert.That(risingSum / sampleCount, Is.EqualTo(2.0).Within(0.02));
        });
    }

    static double ReferenceSample(double unit, double minimum, double maximum, double mode, RandomMathMode mathMode)
    {
        if (unit == 0.0 || minimum == maximum)
        {
            return minimum;
        }

        double width = maximum - minimum;
        bool overflowingWidth = double.IsPositiveInfinity(width);
        double fraction;
        if (overflowingWidth)
        {
            fraction = (mode * 0.5 - minimum * 0.5) / (maximum * 0.5 - minimum * 0.5);
        }
        else
        {
            fraction = (mode - minimum) / width;
        }

        double result;
        if (unit < fraction)
        {
            double weight = Sqrt(unit * fraction, mathMode);
            result = overflowingWidth ? minimum * (1.0 - weight) + maximum * weight : minimum + width * weight;
        }
        else
        {
            double complement = Sqrt((1.0 - unit) * (1.0 - fraction), mathMode);
            result = overflowingWidth ? minimum * complement + maximum * (1.0 - complement) : maximum - width * complement;
        }

        if (result < minimum)
        {
            return minimum;
        }

        if (result > maximum)
        {
            return maximum;
        }

        return result;
    }

    static double Sqrt(double value, RandomMathMode mathMode)
    {
        return mathMode == RandomMathMode.Portable ? PortableMath.Sqrt(value) : Math.Sqrt(value);
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
