namespace random.Tests;

public class GeneralizedDistributionSamplerTests
{
    [Test]
    public void GeneralizedNormalMatchesBoostedGammaConstructionInBothMathModes()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            RandomGenerator actualGenerator = Create(42UL, mode);
            RandomGenerator referenceGenerator = Create(42UL, mode);
            GeneralizedNormalSampler actual = new GeneralizedNormalSampler(actualGenerator, -1.25, 2.5, 0.7);
            GammaSampler gamma = new GammaSampler(referenceGenerator, 1.0 / 0.7 + 1.0, 1.0);

            for (int index = 0; index < 128; index++)
            {
                double root = mode == RandomMathMode.Portable ? PortableMath.Power(gamma.Sample(), 1.0 / 0.7) : Math.Pow(gamma.Sample(), 1.0 / 0.7);
                double magnitude = referenceGenerator.NextDoubleOpenOpen() * root;
                double signed = referenceGenerator.NextBoolean() ? magnitude : -magnitude;
                double expected = -1.25 + 2.5 * signed;
                Assert.That(PortableMath.DoubleBits(actual.Sample()), Is.EqualTo(PortableMath.DoubleBits(expected)));
            }

            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        }
    }

    [Test]
    public void SkewedGeneralizedErrorMatchesFernandezSteelConstructionInBothMathModes()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            RandomGenerator actualGenerator = Create(67UL, mode);
            RandomGenerator referenceGenerator = Create(67UL, mode);
            SkewedGeneralizedErrorSampler actual = new SkewedGeneralizedErrorSampler(actualGenerator, 3.0, 1.5, 1.3, 2.0);
            GammaSampler gamma = new GammaSampler(referenceGenerator, 1.0 / 1.3 + 1.0, 1.0);

            for (int index = 0; index < 128; index++)
            {
                double root = mode == RandomMathMode.Portable ? PortableMath.Power(gamma.Sample(), 1.0 / 1.3) : Math.Pow(gamma.Sample(), 1.0 / 1.3);
                double magnitude = referenceGenerator.NextDoubleOpenOpen() * root;
                bool positive = referenceGenerator.NextBooleanWithDoubleProbability(0.8);
                double skewed = positive ? magnitude * 2.0 : -magnitude / 2.0;
                double expected = 3.0 + 1.5 * skewed;
                Assert.That(PortableMath.DoubleBits(actual.Sample()), Is.EqualTo(PortableMath.DoubleBits(expected)));
            }

            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        }
    }

    [Test]
    public void SkewedGeneralizedTMatchesBoostedBetaPrimeConstructionInBothMathModes()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            RandomGenerator actualGenerator = Create(91UL, mode);
            RandomGenerator referenceGenerator = Create(91UL, mode);
            SkewedGeneralizedTSampler actual = new SkewedGeneralizedTSampler(actualGenerator, -2.0, 0.75, 1.4, 3.5, 0.5);
            double inverseBodyShape = 1.0 / 1.4;
            GammaSampler bodyGamma = new GammaSampler(referenceGenerator, inverseBodyShape + 1.0, 1.0);
            GammaSampler tailGamma = new GammaSampler(referenceGenerator, 4.5, 1.0);

            for (int index = 0; index < 128; index++)
            {
                double firstGamma = bodyGamma.Sample();
                double secondGamma = tailGamma.Sample();
                double firstUnit = referenceGenerator.NextDoubleOpenOpen();
                double secondUnit = referenceGenerator.NextDoubleOpenOpen();
                double logarithm;
                if (mode == RandomMathMode.Portable)
                {
                    logarithm = inverseBodyShape * (PortableMath.Log(3.5) + PortableMath.Log(firstGamma) - PortableMath.Log(secondGamma));
                    logarithm += PortableMath.Log(firstUnit);
                    logarithm -= inverseBodyShape / 3.5 * PortableMath.Log(secondUnit);
                }
                else
                {
                    logarithm = inverseBodyShape * (Math.Log(3.5) + Math.Log(firstGamma) - Math.Log(secondGamma));
                    logarithm += Math.Log(firstUnit);
                    logarithm -= inverseBodyShape / 3.5 * Math.Log(secondUnit);
                }

                double magnitude = mode == RandomMathMode.Portable ? PortableMath.Exp(logarithm) : Math.Exp(logarithm);
                bool positive = referenceGenerator.NextBooleanWithDoubleProbability(0.2);
                double skewed = positive ? magnitude * 0.5 : -magnitude / 0.5;
                double expected = -2.0 + 0.75 * skewed;
                Assert.That(PortableMath.DoubleBits(actual.Sample()), Is.EqualTo(PortableMath.DoubleBits(expected)));
            }

            Assert.That(actualGenerator.ExportState().ToBytes(), Is.EqualTo(referenceGenerator.ExportState().ToBytes()));
        }
    }

    [Test]
    public void SymmetricAndStandardFormsHaveTheDocumentedNesting()
    {
        RandomGenerator generalizedGenerator = Create(123UL, RandomMathMode.Portable);
        RandomGenerator skewedGenerator = Create(123UL, RandomMathMode.Portable);
        GeneralizedNormalSampler generalized = new GeneralizedNormalSampler(generalizedGenerator, 0.0, 1.0, 1.7);
        SkewedGeneralizedErrorSampler skewed = new SkewedGeneralizedErrorSampler(skewedGenerator, 0.0, 1.0, 1.7, 1.0);
        SkewedGeneralizedTSampler standardT = new SkewedGeneralizedTSampler(Create(51UL, RandomMathMode.Portable), 0.0, 1.0, 1.3, 4.0, 0.8);
        SkewedGeneralizedTSampler explicitT = new SkewedGeneralizedTSampler(Create(51UL, RandomMathMode.Portable), 0.0, 1.0, 1.3, 4.0, 0.8);

        for (int index = 0; index < 128; index++)
        {
            Assert.Multiple(() =>
            {
                Assert.That(PortableMath.DoubleBits(skewed.Sample()), Is.EqualTo(PortableMath.DoubleBits(generalized.Sample())));
                Assert.That(PortableMath.DoubleBits(standardT.Sample()), Is.EqualTo(PortableMath.DoubleBits(explicitT.Sample())));
            });
        }

        Assert.That(skewedGenerator.ExportState().ToBytes(), Is.EqualTo(generalizedGenerator.ExportState().ToBytes()));
    }

    [Test]
    public void FillMatchesRepeatedSamplesAndEmptyFillConsumesNothing()
    {
        RandomGenerator[] fillGenerators = [Create(17UL, RandomMathMode.Portable), Create(17UL, RandomMathMode.Portable), Create(17UL, RandomMathMode.Portable)];
        RandomGenerator[] scalarGenerators = [Create(17UL, RandomMathMode.Portable), Create(17UL, RandomMathMode.Portable), Create(17UL, RandomMathMode.Portable)];
        GeneralizedNormalSampler generalizedFill = new GeneralizedNormalSampler(fillGenerators[0], 2.0, 3.0, 0.8);
        GeneralizedNormalSampler generalizedScalar = new GeneralizedNormalSampler(scalarGenerators[0], 2.0, 3.0, 0.8);
        SkewedGeneralizedErrorSampler errorFill = new SkewedGeneralizedErrorSampler(fillGenerators[1], 2.0, 3.0, 0.8, 1.7);
        SkewedGeneralizedErrorSampler errorScalar = new SkewedGeneralizedErrorSampler(scalarGenerators[1], 2.0, 3.0, 0.8, 1.7);
        SkewedGeneralizedTSampler tFill = new SkewedGeneralizedTSampler(fillGenerators[2], 2.0, 3.0, 0.8, 2.5, 1.7);
        SkewedGeneralizedTSampler tScalar = new SkewedGeneralizedTSampler(scalarGenerators[2], 2.0, 3.0, 0.8, 2.5, 1.7);

        AssertFill(values => generalizedFill.Fill(values), () => generalizedScalar.Sample(), fillGenerators[0], scalarGenerators[0]);
        AssertFill(values => errorFill.Fill(values), () => errorScalar.Sample(), fillGenerators[1], scalarGenerators[1]);
        AssertFill(values => tFill.Fill(values), () => tScalar.Sample(), fillGenerators[2], scalarGenerators[2]);
    }

    [Test]
    public void ZeroScaleReturnsExactLocationWithoutConsumption()
    {
        RandomGenerator generator = Create(71UL, RandomMathMode.Portable);
        byte[] before = generator.ExportState().ToBytes();
        GeneralizedNormalSampler generalized = new GeneralizedNormalSampler(generator, -91.5, -0.0, 1.2);
        SkewedGeneralizedErrorSampler error = new SkewedGeneralizedErrorSampler(generator, -91.5, -0.0, 1.2, 2.0);
        SkewedGeneralizedTSampler t = new SkewedGeneralizedTSampler(generator, -91.5, -0.0, 1.2, 3.0, 2.0);
        double[] values = new double[8];

        Assert.Multiple(() =>
        {
            Assert.That(generalized.Sample(), Is.EqualTo(-91.5));
            Assert.That(error.Sample(), Is.EqualTo(-91.5));
            Assert.That(t.Sample(), Is.EqualTo(-91.5));
        });
        generalized.Fill(values);
        Assert.That(values, Is.All.EqualTo(-91.5));
        error.Fill(values);
        Assert.That(values, Is.All.EqualTo(-91.5));
        t.Fill(values);
        Assert.Multiple(() =>
        {
            Assert.That(values, Is.All.EqualTo(-91.5));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [Test]
    public void InvalidParametersAreRejectedWithoutConsumption()
    {
        Assert.That(Assert.Throws<RandomArgumentException>(() => new GeneralizedNormalSampler(NullReference.Value<global::random.IRandomSource>(), 0.0, 1.0, 1.0))?.ArgumentName(), Is.EqualTo("generator"));
        Assert.That(Assert.Throws<RandomArgumentException>(() => new SkewedGeneralizedErrorSampler(NullReference.Value<global::random.IRandomSource>(), 0.0, 1.0, 1.0, 1.0))?.ArgumentName(), Is.EqualTo("generator"));
        Assert.That(Assert.Throws<RandomArgumentException>(() => new SkewedGeneralizedTSampler(NullReference.Value<global::random.IRandomSource>(), 0.0, 1.0, 1.0, 1.0, 1.0))?.ArgumentName(), Is.EqualTo("generator"));

        RandomGenerator generator = Create(23UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        object[][] cases =
        [
            [(TestDelegate)(() => new GeneralizedNormalSampler(generator, double.NaN, 1.0, 1.0)), "location"],
            [(TestDelegate)(() => new GeneralizedNormalSampler(generator, 0.0, double.PositiveInfinity, 1.0)), "scale"],
            [(TestDelegate)(() => new GeneralizedNormalSampler(generator, 0.0, -double.Epsilon, 1.0)), "scale"],
            [(TestDelegate)(() => new GeneralizedNormalSampler(generator, 0.0, 1.0, 0.0)), "shape"],
            [(TestDelegate)(() => new GeneralizedNormalSampler(generator, 0.0, 1.0, double.NaN)), "shape"],
            [(TestDelegate)(() => new SkewedGeneralizedErrorSampler(generator, 0.0, 1.0, 1.0, 0.0)), "skewness"],
            [(TestDelegate)(() => new SkewedGeneralizedErrorSampler(generator, 0.0, 1.0, 1.0, double.PositiveInfinity)), "skewness"],
            [(TestDelegate)(() => new SkewedGeneralizedTSampler(generator, 0.0, 1.0, 0.0, 1.0, 1.0)), "shape"],
            [(TestDelegate)(() => new SkewedGeneralizedTSampler(generator, 0.0, 1.0, 1.0, 0.0, 1.0)), "tailShape"],
            [(TestDelegate)(() => new SkewedGeneralizedTSampler(generator, 0.0, 1.0, 1.0, double.NaN, 1.0)), "tailShape"],
            [(TestDelegate)(() => new SkewedGeneralizedTSampler(generator, 0.0, 1.0, 1.0, 1.0, -double.Epsilon)), "skewness"]
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        }
    }

    [Test]
    public void ExtremeShapesAndSkewnessDoNotProduceNaN()
    {
        GeneralizedNormalSampler uniformLimit = new GeneralizedNormalSampler(Create(4UL, RandomMathMode.Portable), 0.0, 1.0, double.MaxValue);
        SkewedGeneralizedTSampler tUniformLimit = new SkewedGeneralizedTSampler(Create(5UL, RandomMathMode.Portable), 0.0, 1.0, double.MaxValue, 2.0, 1.0);
        GeneralizedNormalSampler overflowLimit = new GeneralizedNormalSampler(Create(6UL, RandomMathMode.Portable), 0.0, 1.0, double.Epsilon);
        SkewedGeneralizedErrorSampler leftLimit = new SkewedGeneralizedErrorSampler(Create(7UL, RandomMathMode.Portable), 0.0, 1.0, 1.5, double.Epsilon);
        SkewedGeneralizedErrorSampler rightLimit = new SkewedGeneralizedErrorSampler(Create(8UL, RandomMathMode.Portable), 0.0, 1.0, 1.5, double.MaxValue);
        SkewedGeneralizedTSampler heavyTailLimit = new SkewedGeneralizedTSampler(Create(9UL, RandomMathMode.Portable), 0.0, 1.0, 2.0, double.Epsilon, 1.0);

        for (int index = 0; index < 128; index++)
        {
            double uniformValue = uniformLimit.Sample();
            double tUniformValue = tUniformLimit.Sample();
            Assert.Multiple(() =>
            {
                Assert.That(double.IsFinite(uniformValue), Is.True);
                Assert.That(Math.Abs(uniformValue), Is.LessThanOrEqualTo(1.000000000001));
                Assert.That(double.IsFinite(tUniformValue), Is.True);
                Assert.That(Math.Abs(tUniformValue), Is.LessThanOrEqualTo(1.000000000001));
                Assert.That(double.IsNaN(overflowLimit.Sample()), Is.False);
                Assert.That(double.IsNaN(leftLimit.Sample()), Is.False);
                Assert.That(double.IsNaN(rightLimit.Sample()), Is.False);
                Assert.That(double.IsNaN(heavyTailLimit.Sample()), Is.False);
            });
        }
    }

    [Test]
    public void DeterministicSamplesFollowDocumentedShapeAndSkewConventions()
    {
        const int sampleCount = 100000;
        GeneralizedNormalSampler laplaceShape = new GeneralizedNormalSampler(Create(101UL, RandomMathMode.Portable), 0.0, 1.0, 1.0);
        GeneralizedNormalSampler gaussianShape = new GeneralizedNormalSampler(Create(102UL, RandomMathMode.Portable), 0.0, 1.0, 2.0);
        SkewedGeneralizedErrorSampler skewed = new SkewedGeneralizedErrorSampler(Create(103UL, RandomMathMode.Portable), 0.0, 1.0, 1.5, 2.0);
        SkewedGeneralizedTSampler studentRelation = new SkewedGeneralizedTSampler(Create(104UL, RandomMathMode.Portable), 0.0, Math.Sqrt(2.0), 2.0, 5.0, 1.0);
        double absoluteLaplaceSum = 0.0;
        double gaussianSquareSum = 0.0;
        double studentSquareSum = 0.0;
        int positiveSkewCount = 0;

        for (int index = 0; index < sampleCount; index++)
        {
            absoluteLaplaceSum += Math.Abs(laplaceShape.Sample());
            double gaussian = gaussianShape.Sample();
            gaussianSquareSum += gaussian * gaussian;
            if (skewed.Sample() > 0.0)
            {
                positiveSkewCount++;
            }

            double student = studentRelation.Sample();
            studentSquareSum += student * student;
        }

        Assert.Multiple(() =>
        {
            Assert.That(absoluteLaplaceSum / sampleCount, Is.EqualTo(1.0).Within(0.02));
            Assert.That(gaussianSquareSum / sampleCount, Is.EqualTo(0.5).Within(0.02));
            Assert.That(positiveSkewCount, Is.InRange(79000, 81000));
            Assert.That(studentSquareSum / sampleCount, Is.EqualTo(1.25).Within(0.08));
        });
    }

    static void AssertFill(Action<double[]> fill, Func<double> sample, RandomGenerator fillGenerator, RandomGenerator scalarGenerator)
    {
        byte[] before = fillGenerator.ExportState().ToBytes();
        fill(Array.Empty<double>());
        Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(before));
        double[] actual = new double[129];
        double[] expected = new double[actual.Length];
        fill(actual);
        for (int index = 0; index < expected.Length; index++)
        {
            expected[index] = sample();
        }

        for (int index = 0; index < actual.Length; index++)
        {
            Assert.That(PortableMath.DoubleBits(actual[index]), Is.EqualTo(PortableMath.DoubleBits(expected[index])));
        }

        Assert.That(fillGenerator.ExportState().ToBytes(), Is.EqualTo(scalarGenerator.ExportState().ToBytes()));
    }

    static RandomGenerator Create(ulong seed, RandomMathMode mode)
    {
        return RandomGenerator.CreateSplitMix64(seed, mode);
    }
}
