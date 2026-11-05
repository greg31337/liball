namespace random.Tests;

public class PortableSpecialFunctionsTests
{
    [Test]
    public void SpecialFunctionsLockKnownBitFixtures()
    {
        Assert.Multiple(() =>
        {
            Assert.That(PortableMath.DoubleBits(PortableSpecialFunctions.LogGamma(0.5)), Is.EqualTo(0x3FE250D048E7A1C0UL));
            Assert.That(PortableMath.DoubleBits(PortableSpecialFunctions.LogBeta(2.0, 3.0)), Is.EqualTo(0xC003E116BCD39E77UL));
            Assert.That(PortableMath.DoubleBits(PortableSpecialFunctions.RegularizedGammaP(2.5, 1.25)), Is.EqualTo(0x3FCC9BCF3C0293B8UL));
            Assert.That(PortableMath.DoubleBits(PortableSpecialFunctions.RegularizedGammaQ(2.5, 5.0)), Is.EqualTo(0x3FB3429DF9B4ADEAUL));
            Assert.That(PortableMath.DoubleBits(PortableSpecialFunctions.RegularizedBeta(0.75, 2.5, 4.5)), Is.EqualTo(0x3FEF8A717A2E5045UL));
            Assert.That(PortableMath.DoubleBits(PortableSpecialFunctions.Erfc(10.0)), Is.EqualTo(0x36A7D8A7F2A8A31CUL));
        });
    }

    [Test]
    public void LogGammaMatchesReferenceValuesAcrossShapeRanges()
    {
        double[][] cases =
        [
            [0.1, 2.252712651734206],
            [0.5, 0.5723649429247001],
            [1.0, 0.0],
            [1.5, -0.12078223763524522],
            [2.0, 0.0],
            [5.0, 3.1780538303479458],
            [10.0, 12.801827480081469],
            [50.0, 144.5657439463449],
            [100.0, 359.1342053695754]
        ];

        foreach (double[] item in cases)
        {
            AssertClose(PortableSpecialFunctions.LogGamma(item[0]), item[1]);
        }
    }

    [Test]
    public void LogGammaSatisfiesRecurrenceAndLogBetaMatchesReferences()
    {
        double[] values = [0.2, 0.75, 1.25, 3.5, 20.0, 100.0];
        foreach (double value in values)
        {
            double expected = PortableSpecialFunctions.LogGamma(value) + PortableMath.Log(value);
            AssertClose(PortableSpecialFunctions.LogGamma(value + 1.0), expected);
        }

        double[][] betaCases =
        [
            [0.5, 0.5, 1.1447298858494002],
            [2.0, 3.0, -2.4849066497880004],
            [2.5, 4.5, -3.8408317706947397],
            [100.0, 120.0, -152.66146213709442],
            [0.1, 10.0, 2.0270133931824375]
        ];

        foreach (double[] item in betaCases)
        {
            AssertClose(PortableSpecialFunctions.LogBeta(item[0], item[1]), item[2]);
            AssertClose(PortableSpecialFunctions.LogBeta(item[1], item[0]), item[2]);
        }
    }

    [Test]
    public void RegularizedGammaMatchesSeriesAndContinuedFractionReferences()
    {
        double[][] cases =
        [
            [0.5, 0.25, 0.5204998778130465],
            [0.5, 1.0, 0.8427007929497149],
            [1.0, 2.0, 0.8646647167633873],
            [2.5, 1.25, 0.2235049288766773],
            [2.5, 5.0, 0.9247647538534878],
            [10.0, 10.0, 0.5420702855281478],
            [100.0, 110.0, 0.8417213299399129]
        ];

        foreach (double[] item in cases)
        {
            double lower = PortableSpecialFunctions.RegularizedGammaP(item[0], item[1]);
            double upper = PortableSpecialFunctions.RegularizedGammaQ(item[0], item[1]);
            AssertClose(lower, item[2]);
            AssertClose(lower + upper, 1.0);
        }
    }

    [Test]
    public void RegularizedGammaIsBoundedAndMonotone()
    {
        double[] shapes = [0.1, 0.5, 1.0, 2.5, 10.0, 100.0];
        double[] values = [0.0, 0.001, 0.1, 0.5, 1.0, 2.0, 10.0, 100.0, 1000.0];
        foreach (double shape in shapes)
        {
            double previous = 0.0;
            foreach (double value in values)
            {
                double current = PortableSpecialFunctions.RegularizedGammaP(shape, value);
                Assert.That(current, Is.InRange(0.0, 1.0));
                Assert.That(current, Is.GreaterThanOrEqualTo(previous));
                previous = current;
            }
        }
    }

    [Test]
    public void RegularizedBetaMatchesReferencesAndComplementIdentity()
    {
        double[][] cases =
        [
            [0.25, 0.5, 0.5, 0.3333333333333333],
            [0.5, 2.0, 3.0, 0.6875],
            [0.1, 5.0, 2.0, 0.000055000000000000015],
            [0.75, 2.5, 4.5, 0.9856498133035606],
            [0.9, 20.0, 3.0, 0.6200409384119036],
            [0.4, 100.0, 120.0, 0.051042319230408294]
        ];

        foreach (double[] item in cases)
        {
            double result = PortableSpecialFunctions.RegularizedBeta(item[0], item[1], item[2]);
            double complement = PortableSpecialFunctions.RegularizedBeta(1.0 - item[0], item[2], item[1]);
            AssertClose(result, item[3]);
            AssertClose(result + complement, 1.0);
        }
    }

    [Test]
    public void ErfAndErfcMatchCentralAndTailReferences()
    {
        double[][] cases =
        [
            [-3.0, -0.9999779095030014, 1.9999779095030015],
            [-1.0, -0.8427007929497149, 1.8427007929497148],
            [-0.5, -0.5204998778130465, 1.5204998778130465],
            [0.0, 0.0, 1.0],
            [0.5, 0.5204998778130465, 0.4795001221869535],
            [1.0, 0.8427007929497149, 0.15729920705028513],
            [3.0, 0.9999779095030014, 0.00002209049699858544],
            [5.0, 0.9999999999984626, 1.5374597944280351e-12],
            [10.0, 1.0, 2.088487583762545e-45]
        ];

        foreach (double[] item in cases)
        {
            AssertClose(PortableSpecialFunctions.Erf(item[0]), item[1]);
            AssertClose(PortableSpecialFunctions.Erfc(item[0]), item[2]);
        }
    }

    [Test]
    public void SpecialFunctionsHandleBoundariesAndInvalidDomains()
    {
        double negativeZero = PortableMath.DoubleFromBits(PortableMath.DoubleSignMask);
        double unusualNaN = PortableMath.DoubleFromBits(0xFFF0000000000001UL);

        Assert.Multiple(() =>
        {
            Assert.That(PortableSpecialFunctions.LogGamma(double.PositiveInfinity), Is.EqualTo(double.PositiveInfinity));
            AssertCanonicalNaN(PortableSpecialFunctions.LogGamma(0.0));
            AssertCanonicalNaN(PortableSpecialFunctions.LogGamma(-1.0));
            AssertCanonicalNaN(PortableSpecialFunctions.LogBeta(1.0, 0.0));
            Assert.That(PortableSpecialFunctions.RegularizedGammaP(2.0, 0.0), Is.EqualTo(0.0));
            Assert.That(PortableSpecialFunctions.RegularizedGammaQ(2.0, 0.0), Is.EqualTo(1.0));
            Assert.That(PortableSpecialFunctions.RegularizedGammaP(2.0, double.PositiveInfinity), Is.EqualTo(1.0));
            Assert.That(PortableSpecialFunctions.RegularizedGammaQ(2.0, double.PositiveInfinity), Is.EqualTo(0.0));
            AssertCanonicalNaN(PortableSpecialFunctions.RegularizedGammaP(0.0, 1.0));
            AssertCanonicalNaN(PortableSpecialFunctions.RegularizedGammaQ(1.0, -1.0));
            Assert.That(PortableSpecialFunctions.RegularizedBeta(0.0, 2.0, 3.0), Is.EqualTo(0.0));
            Assert.That(PortableSpecialFunctions.RegularizedBeta(1.0, 2.0, 3.0), Is.EqualTo(1.0));
            AssertCanonicalNaN(PortableSpecialFunctions.RegularizedBeta(-0.1, 2.0, 3.0));
            AssertCanonicalNaN(PortableSpecialFunctions.RegularizedBeta(0.5, -2.0, 3.0));
            Assert.That(PortableMath.DoubleBits(PortableSpecialFunctions.Erf(negativeZero)), Is.EqualTo(PortableMath.DoubleSignMask));
            Assert.That(PortableSpecialFunctions.Erf(double.PositiveInfinity), Is.EqualTo(1.0));
            Assert.That(PortableSpecialFunctions.Erf(double.NegativeInfinity), Is.EqualTo(-1.0));
            Assert.That(PortableSpecialFunctions.Erfc(double.PositiveInfinity), Is.EqualTo(0.0));
            Assert.That(PortableSpecialFunctions.Erfc(double.NegativeInfinity), Is.EqualTo(2.0));
            AssertCanonicalNaN(PortableSpecialFunctions.Erf(unusualNaN));
        });
    }

    static void AssertClose(double actual, double expected)
    {
        double scale = Math.Max(1.0, Math.Abs(expected));
        double tolerance = Math.Max(PortableSpecialFunctions.AbsoluteError, PortableSpecialFunctions.RelativeError * scale);
        Assert.That(actual, Is.EqualTo(expected).Within(tolerance), $"actual={actual:R}, expected={expected:R}");
    }

    static void AssertCanonicalNaN(double value)
    {
        Assert.That(PortableMath.DoubleBits(value), Is.EqualTo(PortableMath.DoubleCanonicalNaN));
    }
}
