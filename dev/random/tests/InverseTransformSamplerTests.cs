namespace random.Tests;

public class InverseTransformSamplerTests
{
    const double Pi = 3.14159265358979323846;
    const ulong ZeroOutputSeed = 0x61C8864680B583EBUL;
    const ulong OneOutputSeed = 0xF8364607E9C949BDUL;
    const ulong HalfOutputSeed = 0x9359B87109D5557EUL;
    const ulong UInt64MaximumOutputSeed = 0x31628AF67B2131ABUL;

    [Test]
    public void ParameterizedSamplesMatchTheirInverseTransformsInBothMathModes()
    {
        RandomMathMode[] modes = [RandomMathMode.Portable, RandomMathMode.System];
        foreach (RandomMathMode mode in modes)
        {
            RandomGenerator cauchyGenerator = Create(987UL, mode);
            RandomGenerator cauchyReference = Create(987UL, mode);
            RandomGenerator laplaceGenerator = Create(987UL, mode);
            RandomGenerator laplaceReference = Create(987UL, mode);
            RandomGenerator logisticGenerator = Create(987UL, mode);
            RandomGenerator logisticReference = Create(987UL, mode);
            RandomGenerator gumbelGenerator = Create(987UL, mode);
            RandomGenerator gumbelReference = Create(987UL, mode);
            CauchySampler cauchy = new CauchySampler(cauchyGenerator, -3.0, 2.5);
            LaplaceSampler laplace = new LaplaceSampler(laplaceGenerator, -3.0, 2.5);
            LogisticSampler logistic = new LogisticSampler(logisticGenerator, -3.0, 2.5);
            GumbelSampler gumbel = new GumbelSampler(gumbelGenerator, -3.0, 2.5);

            for (int index = 0; index < 200; index++)
            {
                double cauchyUnit = cauchyReference.NextDoubleOpenOpen();
                double angle = cauchyUnit < 0.5 ? Pi * cauchyUnit : Pi * (1.0 - cauchyUnit);
                double tangent = mode == RandomMathMode.Portable ? PortableMath.Tan(angle) : Math.Tan(angle);
                double cauchyStandard = cauchyUnit == 0.5 ? 0.0 : cauchyUnit < 0.5 ? -1.0 / tangent : 1.0 / tangent;

                double laplaceUnit = laplaceReference.NextDoubleOpenOpen();
                double laplaceArgument = laplaceUnit < 0.5 ? laplaceUnit + laplaceUnit : (1.0 - laplaceUnit) + (1.0 - laplaceUnit);
                double laplaceLog = mode == RandomMathMode.Portable ? PortableMath.Log(laplaceArgument) : Math.Log(laplaceArgument);
                double laplaceStandard = laplaceUnit < 0.5 ? laplaceLog : -laplaceLog;

                double logisticUnit = logisticReference.NextDoubleOpenOpen();
                double logisticFirst = mode == RandomMathMode.Portable ? PortableMath.Log(logisticUnit) : Math.Log(logisticUnit);
                double logisticSecond = mode == RandomMathMode.Portable ? PortableMath.Log1p(-logisticUnit) : Math.Log(1.0 - logisticUnit);

                double gumbelUnit = gumbelReference.NextDoubleOpenOpen();
                double gumbelInner = mode == RandomMathMode.Portable ? -PortableMath.Log(gumbelUnit) : -Math.Log(gumbelUnit);
                double gumbelStandard = mode == RandomMathMode.Portable ? -PortableMath.Log(gumbelInner) : -Math.Log(gumbelInner);

                Assert.Multiple(() =>
                {
                    Assert.That(PortableMath.DoubleBits(cauchy.Sample()), Is.EqualTo(PortableMath.DoubleBits(-3.0 + 2.5 * cauchyStandard)));
                    Assert.That(PortableMath.DoubleBits(laplace.Sample()), Is.EqualTo(PortableMath.DoubleBits(-3.0 + 2.5 * laplaceStandard)));
                    Assert.That(PortableMath.DoubleBits(logistic.Sample()), Is.EqualTo(PortableMath.DoubleBits(-3.0 + 2.5 * (logisticFirst - logisticSecond))));
                    Assert.That(PortableMath.DoubleBits(gumbel.Sample()), Is.EqualTo(PortableMath.DoubleBits(-3.0 + 2.5 * gumbelStandard)));
                });
            }

            Assert.Multiple(() =>
            {
                Assert.That(cauchyGenerator.ExportState().ToBytes(), Is.EqualTo(cauchyReference.ExportState().ToBytes()));
                Assert.That(laplaceGenerator.ExportState().ToBytes(), Is.EqualTo(laplaceReference.ExportState().ToBytes()));
                Assert.That(logisticGenerator.ExportState().ToBytes(), Is.EqualTo(logisticReference.ExportState().ToBytes()));
                Assert.That(gumbelGenerator.ExportState().ToBytes(), Is.EqualTo(gumbelReference.ExportState().ToBytes()));
            });
        }
    }

    [Test]
    public void StandardConstructorsMatchExplicitLocationAndScale()
    {
        RandomGenerator[] standardGenerators = [Create(42UL), Create(42UL), Create(42UL), Create(42UL)];
        RandomGenerator[] explicitGenerators = [Create(42UL), Create(42UL), Create(42UL), Create(42UL)];
        CauchySampler cauchyStandard = new CauchySampler(standardGenerators[0], 0.0, 1.0);
        CauchySampler cauchyExplicit = new CauchySampler(explicitGenerators[0], 0.0, 1.0);
        LaplaceSampler laplaceStandard = new LaplaceSampler(standardGenerators[1], 0.0, 1.0);
        LaplaceSampler laplaceExplicit = new LaplaceSampler(explicitGenerators[1], 0.0, 1.0);
        LogisticSampler logisticStandard = new LogisticSampler(standardGenerators[2], 0.0, 1.0);
        LogisticSampler logisticExplicit = new LogisticSampler(explicitGenerators[2], 0.0, 1.0);
        GumbelSampler gumbelStandard = new GumbelSampler(standardGenerators[3], 0.0, 1.0);
        GumbelSampler gumbelExplicit = new GumbelSampler(explicitGenerators[3], 0.0, 1.0);

        for (int index = 0; index < 64; index++)
        {
            Assert.Multiple(() =>
            {
                Assert.That(PortableMath.DoubleBits(cauchyStandard.Sample()), Is.EqualTo(PortableMath.DoubleBits(cauchyExplicit.Sample())));
                Assert.That(PortableMath.DoubleBits(laplaceStandard.Sample()), Is.EqualTo(PortableMath.DoubleBits(laplaceExplicit.Sample())));
                Assert.That(PortableMath.DoubleBits(logisticStandard.Sample()), Is.EqualTo(PortableMath.DoubleBits(logisticExplicit.Sample())));
                Assert.That(PortableMath.DoubleBits(gumbelStandard.Sample()), Is.EqualTo(PortableMath.DoubleBits(gumbelExplicit.Sample())));
            });
        }

        for (int index = 0; index < standardGenerators.Length; index++)
        {
            Assert.That(standardGenerators[index].ExportState().ToBytes(), Is.EqualTo(explicitGenerators[index].ExportState().ToBytes()));
        }
    }

    [Test]
    public void FillMatchesRepeatedSamplesAndEmptyFillConsumesNothing()
    {
        RandomGenerator[] fillGenerators = [Create(123UL), Create(123UL), Create(123UL), Create(123UL)];
        RandomGenerator[] scalarGenerators = [Create(123UL), Create(123UL), Create(123UL), Create(123UL)];
        CauchySampler cauchyFill = new CauchySampler(fillGenerators[0], 0.0, 1.0);
        CauchySampler cauchyScalar = new CauchySampler(scalarGenerators[0], 0.0, 1.0);
        LaplaceSampler laplaceFill = new LaplaceSampler(fillGenerators[1], 0.0, 1.0);
        LaplaceSampler laplaceScalar = new LaplaceSampler(scalarGenerators[1], 0.0, 1.0);
        LogisticSampler logisticFill = new LogisticSampler(fillGenerators[2], 0.0, 1.0);
        LogisticSampler logisticScalar = new LogisticSampler(scalarGenerators[2], 0.0, 1.0);
        GumbelSampler gumbelFill = new GumbelSampler(fillGenerators[3], 0.0, 1.0);
        GumbelSampler gumbelScalar = new GumbelSampler(scalarGenerators[3], 0.0, 1.0);

        AssertFill(values => cauchyFill.Fill(values), () => cauchyScalar.Sample(), fillGenerators[0], scalarGenerators[0]);
        AssertFill(values => laplaceFill.Fill(values), () => laplaceScalar.Sample(), fillGenerators[1], scalarGenerators[1]);
        AssertFill(values => logisticFill.Fill(values), () => logisticScalar.Sample(), fillGenerators[2], scalarGenerators[2]);
        AssertFill(values => gumbelFill.Fill(values), () => gumbelScalar.Sample(), fillGenerators[3], scalarGenerators[3]);
    }

    [Test]
    public void ZeroScaleReturnsExactLocationWithoutConsumption()
    {
        RandomGenerator generator = Create(71UL);
        byte[] before = generator.ExportState().ToBytes();
        CauchySampler cauchy = new CauchySampler(generator, -91.5, -0.0);
        LaplaceSampler laplace = new LaplaceSampler(generator, -91.5, -0.0);
        LogisticSampler logistic = new LogisticSampler(generator, -91.5, -0.0);
        GumbelSampler gumbel = new GumbelSampler(generator, -91.5, -0.0);
        double[] values = new double[8];

        Assert.Multiple(() =>
        {
            Assert.That(cauchy.Sample(), Is.EqualTo(-91.5));
            Assert.That(laplace.Sample(), Is.EqualTo(-91.5));
            Assert.That(logistic.Sample(), Is.EqualTo(-91.5));
            Assert.That(gumbel.Sample(), Is.EqualTo(-91.5));
        });
        cauchy.Fill(values);
        Assert.That(values, Is.All.EqualTo(-91.5));
        laplace.Fill(values);
        Assert.That(values, Is.All.EqualTo(-91.5));
        logistic.Fill(values);
        Assert.That(values, Is.All.EqualTo(-91.5));
        gumbel.Fill(values);
        Assert.Multiple(() =>
        {
            Assert.That(values, Is.All.EqualTo(-91.5));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [Test]
    public void ExactMidpointCauchyVariateReturnsTheLocationInBothMathModes()
    {
        foreach (RandomMathMode mode in new[] { RandomMathMode.Portable, RandomMathMode.System })
        {
            RandomGenerator generator = Create(HalfOutputSeed, mode);
            RandomGenerator reference = Create(HalfOutputSeed, mode);
            CauchySampler sampler = new CauchySampler(generator, -91.5, double.MaxValue);

            double value = sampler.Sample();
            Assert.Multiple(() =>
            {
                Assert.That(value, Is.EqualTo(-91.5));
                Assert.That(reference.NextDoubleOpenOpen(), Is.EqualTo(0.5));
                Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(reference.ExportState().ToBytes()));
            });
        }
    }

    [Test]
    public void InvalidParametersAreRejectedWithoutConsumption()
    {
        object[] nullCases =
        [
            (TestDelegate)(() => new CauchySampler(NullReference.Value<global::random.IRandomSource>(), 0.0, 1.0)),
            (TestDelegate)(() => new LaplaceSampler(NullReference.Value<global::random.IRandomSource>(), 0.0, 1.0)),
            (TestDelegate)(() => new LogisticSampler(NullReference.Value<global::random.IRandomSource>(), 0.0, 1.0)),
            (TestDelegate)(() => new GumbelSampler(NullReference.Value<global::random.IRandomSource>(), 0.0, 1.0))
        ];
        foreach (TestDelegate testCase in nullCases)
        {
            Assert.That(Assert.Throws<RandomArgumentException>(testCase)?.ArgumentName(), Is.EqualTo("generator"));
        }

        RandomGenerator generator = Create(23UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        object[] cases =
        [
            new object[] { (TestDelegate)(() => new CauchySampler(generator, double.NaN, 1.0)), "location" },
            new object[] { (TestDelegate)(() => new LaplaceSampler(generator, double.PositiveInfinity, 1.0)), "location" },
            new object[] { (TestDelegate)(() => new LogisticSampler(generator, double.NegativeInfinity, 1.0)), "location" },
            new object[] { (TestDelegate)(() => new GumbelSampler(generator, double.NaN, 1.0)), "location" },
            new object[] { (TestDelegate)(() => new CauchySampler(generator, 0.0, double.NaN)), "scale" },
            new object[] { (TestDelegate)(() => new CauchySampler(generator, 0.0, -double.Epsilon)), "scale" },
            new object[] { (TestDelegate)(() => new LaplaceSampler(generator, 0.0, double.PositiveInfinity)), "scale" },
            new object[] { (TestDelegate)(() => new LaplaceSampler(generator, 0.0, -double.Epsilon)), "scale" },
            new object[] { (TestDelegate)(() => new LogisticSampler(generator, 0.0, -double.Epsilon)), "scale" },
            new object[] { (TestDelegate)(() => new GumbelSampler(generator, 0.0, -double.Epsilon)), "scale" }
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        }
    }

    [Test]
    public void ExtremeOpenInputsRemainFiniteAndProtectedFromPoles()
    {
        CauchySampler cauchyLow = new CauchySampler(Create(OneOutputSeed), 0.0, 1.0);
        CauchySampler cauchyHigh = new CauchySampler(Create(UInt64MaximumOutputSeed), 0.0, 1.0);
        LaplaceSampler laplaceLow = new LaplaceSampler(Create(OneOutputSeed), 0.0, 1.0);
        LaplaceSampler laplaceHigh = new LaplaceSampler(Create(UInt64MaximumOutputSeed), 0.0, 1.0);
        LogisticSampler logisticLow = new LogisticSampler(Create(OneOutputSeed), 0.0, 1.0);
        LogisticSampler logisticHigh = new LogisticSampler(Create(UInt64MaximumOutputSeed), 0.0, 1.0);
        GumbelSampler gumbelLow = new GumbelSampler(Create(OneOutputSeed), 0.0, 1.0);
        GumbelSampler gumbelHigh = new GumbelSampler(Create(UInt64MaximumOutputSeed), 0.0, 1.0);

        double[] values = [cauchyLow.Sample(), cauchyHigh.Sample(), laplaceLow.Sample(), laplaceHigh.Sample(), logisticLow.Sample(), logisticHigh.Sample(), gumbelLow.Sample(), gumbelHigh.Sample()];
        foreach (double value in values)
        {
            Assert.That(double.IsFinite(value), Is.True);
        }

        Assert.Multiple(() =>
        {
            Assert.That(values[0], Is.LessThan(-2.0e15));
            Assert.That(values[1], Is.GreaterThan(2.0e15));
            Assert.That(values[2], Is.EqualTo(-values[3]));
            Assert.That(values[4], Is.EqualTo(-values[5]));
            Assert.That(values[6], Is.LessThan(0.0));
            Assert.That(values[7], Is.GreaterThan(36.0));
        });
    }

    [Test]
    public void OpenTransformsRejectAZeroRawNumerator()
    {
        RandomGenerator[] actualGenerators = [Create(ZeroOutputSeed), Create(ZeroOutputSeed), Create(ZeroOutputSeed), Create(ZeroOutputSeed)];
        RandomGenerator[] references = [Create(ZeroOutputSeed), Create(ZeroOutputSeed), Create(ZeroOutputSeed), Create(ZeroOutputSeed)];
        double[] values =
        [
            new CauchySampler(actualGenerators[0], 0.0, 1.0).Sample(),
            new LaplaceSampler(actualGenerators[1], 0.0, 1.0).Sample(),
            new LogisticSampler(actualGenerators[2], 0.0, 1.0).Sample(),
            new GumbelSampler(actualGenerators[3], 0.0, 1.0).Sample()
        ];

        for (int index = 0; index < references.Length; index++)
        {
            double unit = references[index].NextDoubleOpenOpen();
            Assert.Multiple(() =>
            {
                Assert.That(unit, Is.GreaterThan(0.0).And.LessThan(1.0));
                Assert.That(double.IsFinite(values[index]), Is.True);
                Assert.That(actualGenerators[index].ExportState().ToBytes(), Is.EqualTo(references[index].ExportState().ToBytes()));
            });
        }
    }

    [Test]
    public void PortableSequencesHaveLockedBits()
    {
        ulong[][] expected =
        [
            [0x3FEB4EEBE8D32B97UL, 0xBF869E6DC2471966UL, 0x3FCEC76599BE4EA5UL, 0x400951E89B66AAF5UL],
            [0x3FE31DA90FBCBFE8UL, 0xBF7CE6577C740860UL, 0x3FC4D64511C32A97UL, 0x3FFA2BA26CD364C1UL],
            [0x3FEF000F3B6380A6UL, 0xBF8CCC6CB24F12C0UL, 0x3FD36043A7565F40UL, 0x4001CF7BD6987C93UL],
            [0x3FF224A637131F45UL, 0x3FD6CF043C5B88A8UL, 0x3FE2F22B21265338UL, 0x4002395458A4BF2CUL]
        ];
        CauchySampler cauchy = new CauchySampler(Create(42UL), 0.0, 1.0);
        LaplaceSampler laplace = new LaplaceSampler(Create(42UL), 0.0, 1.0);
        LogisticSampler logistic = new LogisticSampler(Create(42UL), 0.0, 1.0);
        GumbelSampler gumbel = new GumbelSampler(Create(42UL), 0.0, 1.0);

        for (int index = 0; index < expected[0].Length; index++)
        {
            Assert.Multiple(() =>
            {
                Assert.That(PortableMath.DoubleBits(cauchy.Sample()), Is.EqualTo(expected[0][index]));
                Assert.That(PortableMath.DoubleBits(laplace.Sample()), Is.EqualTo(expected[1][index]));
                Assert.That(PortableMath.DoubleBits(logistic.Sample()), Is.EqualTo(expected[2][index]));
                Assert.That(PortableMath.DoubleBits(gumbel.Sample()), Is.EqualTo(expected[3][index]));
            });
        }
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

    static RandomGenerator Create(ulong seed)
    {
        return RandomGenerator.CreateSplitMix64(seed, RandomMathMode.Portable);
    }

    static RandomGenerator Create(ulong seed, RandomMathMode mode)
    {
        return RandomGenerator.CreateSplitMix64(seed, mode);
    }
}
