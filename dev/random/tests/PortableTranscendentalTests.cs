namespace random.Tests;

public class PortableTranscendentalTests
{
    [Test]
    public void LogAndExpLockKnownBitFixtures()
    {
        Assert.Multiple(() =>
        {
            Assert.That(PortableMath.DoubleBits(PortableMath.Log(1.0)), Is.EqualTo(0UL));
            Assert.That(PortableMath.DoubleBits(PortableMath.Log(2.0)), Is.EqualTo(0x3FE62E42FEFA39EFUL));
            Assert.That(PortableMath.DoubleBits(PortableMath.Exp(0.0)), Is.EqualTo(0x3FF0000000000000UL));
            Assert.That(PortableMath.DoubleBits(PortableMath.Exp(1.0)), Is.EqualTo(0x4005BF0A8B145769UL));
            Assert.That(PortableMath.DoubleBits(PortableMath.Expm1(1.0)), Is.EqualTo(0x3FFB7E151628AED2UL));
        });
    }

    [Test]
    public void LogHandlesSubnormalZeroNegativeInfinityAndNaN()
    {
        double minimumSubnormal = PortableMath.DoubleFromBits(1UL);
        double unusualNaN = PortableMath.DoubleFromBits(0xFFF0000000000001UL);

        Assert.Multiple(() =>
        {
            AssertWithinUlps(PortableMath.Log(minimumSubnormal), Math.Log(minimumSubnormal), PortableMath.LogErrorUlps);
            Assert.That(PortableMath.Log(0.0), Is.EqualTo(double.NegativeInfinity));
            Assert.That(PortableMath.Log(-0.0), Is.EqualTo(double.NegativeInfinity));
            Assert.That(PortableMath.DoubleBits(PortableMath.Log(-1.0)), Is.EqualTo(PortableMath.DoubleCanonicalNaN));
            Assert.That(PortableMath.Log(double.PositiveInfinity), Is.EqualTo(double.PositiveInfinity));
            Assert.That(PortableMath.DoubleBits(PortableMath.Log(unusualNaN)), Is.EqualTo(PortableMath.DoubleCanonicalNaN));
        });
    }

    [Test]
    public void Log1pAndExpm1PreserveTinyInputs()
    {
        double tiny = PortableMath.ScaleByPowerOfTwo(1.0, -54);
        double negativeTiny = -tiny;

        Assert.Multiple(() =>
        {
            Assert.That(PortableMath.Log1p(tiny), Is.EqualTo(tiny));
            Assert.That(PortableMath.Log1p(negativeTiny), Is.EqualTo(negativeTiny));
            Assert.That(PortableMath.Expm1(tiny), Is.EqualTo(tiny));
            Assert.That(PortableMath.Expm1(negativeTiny), Is.EqualTo(negativeTiny));
            Assert.That(PortableMath.DoubleBits(PortableMath.Log1p(-0.0)), Is.EqualTo(PortableMath.DoubleSignMask));
            Assert.That(PortableMath.DoubleBits(PortableMath.Expm1(-0.0)), Is.EqualTo(PortableMath.DoubleSignMask));
            Assert.That(PortableMath.Log1p(-1.0), Is.EqualTo(double.NegativeInfinity));
            Assert.That(PortableMath.DoubleBits(PortableMath.Log1p(-2.0)), Is.EqualTo(PortableMath.DoubleCanonicalNaN));
            Assert.That(PortableMath.Expm1(double.NegativeInfinity), Is.EqualTo(-1.0));
        });
    }

    [Test]
    public void ExpHandlesUnderflowOverflowInfinitiesAndNaN()
    {
        double unusualNaN = PortableMath.DoubleFromBits(0x7FF0000000000001UL);

        Assert.Multiple(() =>
        {
            Assert.That(PortableMath.Exp(711.0), Is.EqualTo(double.PositiveInfinity));
            Assert.That(PortableMath.Exp(-747.0), Is.EqualTo(0.0));
            Assert.That(PortableMath.Exp(double.PositiveInfinity), Is.EqualTo(double.PositiveInfinity));
            Assert.That(PortableMath.Exp(double.NegativeInfinity), Is.EqualTo(0.0));
            Assert.That(PortableMath.DoubleBits(PortableMath.Exp(unusualNaN)), Is.EqualTo(PortableMath.DoubleCanonicalNaN));
            Assert.That(PortableMath.DoubleBits(PortableMath.Expm1(unusualNaN)), Is.EqualTo(PortableMath.DoubleCanonicalNaN));
        });
    }

    [Test]
    public void LogKernelStaysWithinSpecifiedUlpBound()
    {
        double[] values =
        [
            PortableMath.DoubleFromBits(1UL),
            PortableMath.DoubleFromBits(0x000FFFFFFFFFFFFFUL),
            PortableMath.DoubleFromBits(0x0010000000000000UL),
            0.125,
            0.5,
            0.75,
            1.0,
            1.25,
            2.0,
            10.0,
            123456789.25,
            double.MaxValue
        ];

        foreach (double value in values)
        {
            AssertWithinUlps(PortableMath.Log(value), Math.Log(value), PortableMath.LogErrorUlps);
        }
    }

    [Test]
    public void ExpKernelStaysWithinSpecifiedUlpBound()
    {
        double[] values = [-745.0, -700.0, -100.0, -10.0, -1.0, -0.5, -0.1, 0.1, 0.5, 1.0, 10.0, 100.0, 700.0, 709.0];

        foreach (double value in values)
        {
            AssertWithinUlps(PortableMath.Exp(value), Math.Exp(value), PortableMath.ExpErrorUlps);
        }
    }

    [Test]
    public void Log1pAndExpm1StayWithinSpecifiedUlpBounds()
    {
        double[] values = [-0.999999999999, -0.5, -0.1, 0.1, 0.5, 1.0, 10.0];
        ulong[] logExpected =
        [
            0xC03BA18C0CB51D2AUL,
            0xBFE62E42FEFA39EFUL,
            0xBFBAF8E8210A415EUL,
            0x3FB8663F793C46C7UL,
            0x3FD9F323ECBF984CUL,
            0x3FE62E42FEFA39EFUL,
            0x40032EE3B77F374CUL
        ];
        ulong[] expExpected =
        [
            0xBFE43A54E4E97B73UL,
            0xBFD92E9A0720D3ECUL,
            0xBFB85C933156A62CUL,
            0x3FBAEC7B35A00D3AUL,
            0x3FE4C2531C3C0D38UL,
            0x3FFB7E151628AED3UL,
            0x40D5825DCF950560UL
        ];

        for (int index = 0; index < values.Length; index++)
        {
            AssertWithinUlps(PortableMath.Log1p(values[index]), PortableMath.DoubleFromBits(logExpected[index]), PortableMath.LogErrorUlps);
            AssertWithinUlps(PortableMath.Expm1(values[index]), PortableMath.DoubleFromBits(expExpected[index]), PortableMath.ExpErrorUlps);
        }
    }

    [Test]
    public void PowerHandlesIntegerAndIeeeSpecialCases()
    {
        double negativeZero = PortableMath.DoubleFromBits(PortableMath.DoubleSignMask);

        Assert.Multiple(() =>
        {
            Assert.That(PortableMath.Power(2.0, 10.0), Is.EqualTo(1024.0));
            Assert.That(PortableMath.Power(2.0, -10.0), Is.EqualTo(1.0 / 1024.0));
            Assert.That(PortableMath.Power(-2.0, 3.0), Is.EqualTo(-8.0));
            Assert.That(PortableMath.Power(-2.0, 4.0), Is.EqualTo(16.0));
            Assert.That(PortableMath.DoubleBits(PortableMath.Power(-2.0, 0.5)), Is.EqualTo(PortableMath.DoubleCanonicalNaN));
            Assert.That(PortableMath.DoubleBits(PortableMath.Power(negativeZero, 3.0)), Is.EqualTo(PortableMath.DoubleSignMask));
            Assert.That(PortableMath.Power(negativeZero, -3.0), Is.EqualTo(double.NegativeInfinity));
            Assert.That(PortableMath.Power(0.0, -2.0), Is.EqualTo(double.PositiveInfinity));
            Assert.That(PortableMath.Power(double.NaN, 0.0), Is.EqualTo(1.0));
            Assert.That(PortableMath.Power(1.0, double.NaN), Is.EqualTo(1.0));
            Assert.That(PortableMath.Power(-1.0, double.PositiveInfinity), Is.EqualTo(1.0));
        });
    }

    [Test]
    public void NonIntegerPowerStaysWithinSpecifiedUlpBound()
    {
        double[][] cases =
        [
            [2.0, 0.5],
            [10.0, 3.25],
            [0.5, -7.75],
            [1.0001, 1234.5],
            [123.456, -0.75]
        ];

        foreach (double[] item in cases)
        {
            double expected = Math.Pow(item[0], item[1]);
            double actual = PortableMath.Power(item[0], item[1]);
            AssertWithinUlps(actual, expected, PortableMath.PowerErrorUlps);
        }
    }

    [Test]
    public void LogAndExpBoundsHoldAcrossDeterministicGrids()
    {
        ulong state = 0xA0761D6478BD642FUL;
        for (int index = 0; index < 512; index++)
        {
            state = state * 0xE7037ED1A0B428DBUL + 0x8EBC6AF09C88C6E3UL;
            ulong bits = state & 0x7FEFFFFFFFFFFFFFUL;
            if (bits == 0UL)
            {
                bits = 1UL;
            }

            double value = PortableMath.DoubleFromBits(bits);
            AssertWithinUlps(PortableMath.Log(value), Math.Log(value), PortableMath.LogErrorUlps);

            double exponent = -700.0 + (1400.0 * index) / 511.0;
            AssertWithinUlps(PortableMath.Exp(exponent), Math.Exp(exponent), PortableMath.ExpErrorUlps);
        }
    }

    static void AssertWithinUlps(double actual, double expected, int maximumUlps)
    {
        if (actual == expected)
        {
            return;
        }

        double spacing = Math.Abs(Math.BitIncrement(expected) - expected);
        if (spacing == 0.0 || double.IsInfinity(spacing))
        {
            spacing = double.Epsilon;
        }

        double error = Math.Abs(actual - expected);
        Assert.That(error, Is.LessThanOrEqualTo(spacing * maximumUlps), $"actual={actual:R}, expected={expected:R}, error={error:R}, spacing={spacing:R}");
    }
}
