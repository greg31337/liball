namespace random.Tests;

public class FloatingUniformTests
{
    const ulong ZeroOutputSeed = 0x61C8864680B583EBUL;
    const ulong OneOutputSeed = 0xF8364607E9C949BDUL;
    const ulong UInt32MaximumOutputSeed = 0x25114ED53327345AUL;
    const ulong UInt64MaximumOutputSeed = 0x31628AF67B2131ABUL;

    [Test]
    public void GridBoundaryConversionsHaveExactEndpointBits()
    {
        Assert.Multiple(() =>
        {
            Assert.That(PortableMath.SingleBits(FloatingUniform.SingleFromNumerator(0U)), Is.EqualTo(0U));
            Assert.That(PortableMath.SingleBits(FloatingUniform.SingleFromNumerator(FloatingUniform.SingleGridSize - 1U)), Is.EqualTo(0x3F7FFFFFU));
            Assert.That(PortableMath.SingleBits(FloatingUniform.SingleFromNumerator(FloatingUniform.SingleGridSize)), Is.EqualTo(0x3F800000U));
            Assert.That(PortableMath.DoubleBits(FloatingUniform.DoubleFromNumerator(0UL)), Is.EqualTo(0UL));
            Assert.That(PortableMath.DoubleBits(FloatingUniform.DoubleFromNumerator(FloatingUniform.DoubleGridSize - 1UL)), Is.EqualTo(0x3FEFFFFFFFFFFFFFUL));
            Assert.That(PortableMath.DoubleBits(FloatingUniform.DoubleFromNumerator(FloatingUniform.DoubleGridSize)), Is.EqualTo(0x3FF0000000000000UL));
        });
    }

    [Test]
    public void SplitMixWordsProduceLockedUnitIntervalVectors()
    {
        Assert.Multiple(() =>
        {
            Assert.That(PortableMath.SingleBits(Create().NextSingleClosedOpen()), Is.EqualTo(0x3F6B6E95U));
            Assert.That(PortableMath.SingleBits(Create().NextSingleOpenOpen()), Is.EqualTo(0x3F6B6E95U));
            Assert.That(PortableMath.SingleBits(Create().NextSingleOpenClosed()), Is.EqualTo(0x3F6B6E96U));
            Assert.That(PortableMath.SingleBits(Create().NextSingleClosedClosed()), Is.EqualTo(0x3E3FADB8U));
            Assert.That(PortableMath.DoubleBits(Create().NextDoubleClosedOpen()), Is.EqualTo(0x3FE732262FEB6E95UL));
            Assert.That(PortableMath.DoubleBits(Create().NextDoubleOpenOpen()), Is.EqualTo(0x3FE732262FEB6E95UL));
            Assert.That(PortableMath.DoubleBits(Create().NextDoubleOpenClosed()), Is.EqualTo(0x3FE732262FEB6E96UL));
            Assert.That(PortableMath.DoubleBits(Create().NextDoubleClosedClosed()), Is.EqualTo(0x3FE7BAE644C5FD6EUL));
        });
    }

    [Test]
    public void ParameterlessMethodsAreClosedOpenAliases()
    {
        RandomGenerator singleDefault = Create();
        RandomGenerator singleExplicit = Create();
        RandomGenerator doubleDefault = Create();
        RandomGenerator doubleExplicit = Create();

        Assert.Multiple(() =>
        {
            Assert.That(PortableMath.SingleBits(singleDefault.NextSingle()), Is.EqualTo(PortableMath.SingleBits(singleExplicit.NextSingleClosedOpen())));
            Assert.That(singleDefault.ExportState().ToBytes(), Is.EqualTo(singleExplicit.ExportState().ToBytes()));
            Assert.That(PortableMath.DoubleBits(doubleDefault.NextDouble()), Is.EqualTo(PortableMath.DoubleBits(doubleExplicit.NextDoubleClosedOpen())));
            Assert.That(doubleDefault.ExportState().ToBytes(), Is.EqualTo(doubleExplicit.ExportState().ToBytes()));
        });
    }

    [Test]
    public void ClosedClosedMethodsCanReturnBothExactEndpoints()
    {
        RandomGenerator singleZero = RandomGenerator.CreateSplitMix64(OneOutputSeed, RandomMathMode.Portable);
        RandomGenerator singleOne = RandomGenerator.CreateSplitMix64(UInt32MaximumOutputSeed, RandomMathMode.Portable);
        RandomGenerator doubleZero = RandomGenerator.CreateSplitMix64(OneOutputSeed, RandomMathMode.Portable);
        RandomGenerator doubleOne = RandomGenerator.CreateSplitMix64(UInt64MaximumOutputSeed, RandomMathMode.Portable);

        Assert.Multiple(() =>
        {
            Assert.That(PortableMath.SingleBits(singleZero.NextSingleClosedClosed()), Is.EqualTo(0U));
            Assert.That(PortableMath.SingleBits(singleOne.NextSingleClosedClosed()), Is.EqualTo(0x3F800000U));
            Assert.That(PortableMath.DoubleBits(doubleZero.NextDoubleClosedClosed()), Is.EqualTo(0UL));
            Assert.That(PortableMath.DoubleBits(doubleOne.NextDoubleClosedClosed()), Is.EqualTo(0x3FF0000000000000UL));
        });
    }

    [Test]
    public void ExcludedEndpointsStayExcludedAtExtremeRawValues()
    {
        RandomGenerator singleClosedZero = RandomGenerator.CreateSplitMix64(ZeroOutputSeed, RandomMathMode.Portable);
        RandomGenerator singleClosedMaximum = RandomGenerator.CreateSplitMix64(UInt32MaximumOutputSeed, RandomMathMode.Portable);
        RandomGenerator singleOpenMinimum = RandomGenerator.CreateSplitMix64(ZeroOutputSeed, RandomMathMode.Portable);
        RandomGenerator singleOpenOne = RandomGenerator.CreateSplitMix64(UInt32MaximumOutputSeed, RandomMathMode.Portable);
        RandomGenerator doubleClosedZero = RandomGenerator.CreateSplitMix64(ZeroOutputSeed, RandomMathMode.Portable);
        RandomGenerator doubleClosedMaximum = RandomGenerator.CreateSplitMix64(UInt64MaximumOutputSeed, RandomMathMode.Portable);
        RandomGenerator doubleOpenMinimum = RandomGenerator.CreateSplitMix64(ZeroOutputSeed, RandomMathMode.Portable);
        RandomGenerator doubleOpenOne = RandomGenerator.CreateSplitMix64(UInt64MaximumOutputSeed, RandomMathMode.Portable);

        Assert.Multiple(() =>
        {
            Assert.That(PortableMath.SingleBits(singleClosedZero.NextSingleClosedOpen()), Is.EqualTo(0U));
            Assert.That(PortableMath.SingleBits(singleClosedMaximum.NextSingleClosedOpen()), Is.EqualTo(0x3F7FFFFFU));
            Assert.That(singleOpenMinimum.NextSingleOpenClosed(), Is.GreaterThan(0.0f));
            Assert.That(PortableMath.SingleBits(singleOpenOne.NextSingleOpenClosed()), Is.EqualTo(0x3F800000U));
            Assert.That(PortableMath.DoubleBits(doubleClosedZero.NextDoubleClosedOpen()), Is.EqualTo(0UL));
            Assert.That(PortableMath.DoubleBits(doubleClosedMaximum.NextDoubleClosedOpen()), Is.EqualTo(0x3FEFFFFFFFFFFFFFUL));
            Assert.That(doubleOpenMinimum.NextDoubleOpenClosed(), Is.GreaterThan(0.0));
            Assert.That(PortableMath.DoubleBits(doubleOpenOne.NextDoubleOpenClosed()), Is.EqualTo(0x3FF0000000000000UL));
        });
    }

    [Test]
    public void OpenOpenMethodsRejectZeroRawNumerators()
    {
        RandomGenerator single = RandomGenerator.CreateSplitMix64(ZeroOutputSeed, RandomMathMode.Portable);
        RandomGenerator doubleGenerator = RandomGenerator.CreateSplitMix64(ZeroOutputSeed, RandomMathMode.Portable);

        Assert.Multiple(() =>
        {
            Assert.That(PortableMath.SingleBits(single.NextSingleOpenOpen()), Is.EqualTo(0x3DEE6D78U));
            Assert.That(single.NextUInt64(), Is.EqualTo(7960286522194355700UL));
            Assert.That(PortableMath.DoubleBits(doubleGenerator.NextDoubleOpenOpen()), Is.EqualTo(0x3F95072F63B9B5E0UL));
            Assert.That(doubleGenerator.NextUInt64(), Is.EqualTo(7960286522194355700UL));
        });
    }

    [Test]
    public void SamplesAlwaysHonorTheirDeclaredIntervals()
    {
        RandomGenerator generator = RandomGenerator.CreatePcg64Dxsm((UInt128)123, (UInt128)456, RandomMathMode.Portable);
        for (int index = 0; index < 10000; index++)
        {
            float singleClosedOpen = generator.NextSingleClosedOpen();
            float singleOpenOpen = generator.NextSingleOpenOpen();
            float singleOpenClosed = generator.NextSingleOpenClosed();
            float singleClosedClosed = generator.NextSingleClosedClosed();
            double doubleClosedOpen = generator.NextDoubleClosedOpen();
            double doubleOpenOpen = generator.NextDoubleOpenOpen();
            double doubleOpenClosed = generator.NextDoubleOpenClosed();
            double doubleClosedClosed = generator.NextDoubleClosedClosed();
            Assert.Multiple(() =>
            {
                Assert.That(singleClosedOpen, Is.GreaterThanOrEqualTo(0.0f).And.LessThan(1.0f));
                Assert.That(singleOpenOpen, Is.GreaterThan(0.0f).And.LessThan(1.0f));
                Assert.That(singleOpenClosed, Is.GreaterThan(0.0f).And.LessThanOrEqualTo(1.0f));
                Assert.That(singleClosedClosed, Is.GreaterThanOrEqualTo(0.0f).And.LessThanOrEqualTo(1.0f));
                Assert.That(doubleClosedOpen, Is.GreaterThanOrEqualTo(0.0).And.LessThan(1.0));
                Assert.That(doubleOpenOpen, Is.GreaterThan(0.0).And.LessThan(1.0));
                Assert.That(doubleOpenClosed, Is.GreaterThan(0.0).And.LessThanOrEqualTo(1.0));
                Assert.That(doubleClosedClosed, Is.GreaterThanOrEqualTo(0.0).And.LessThanOrEqualTo(1.0));
            });
        }
    }

    static RandomGenerator Create()
    {
        return RandomGenerator.CreateSplitMix64(42UL, RandomMathMode.Portable);
    }
}
