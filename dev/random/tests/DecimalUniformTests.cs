namespace random.Tests;

public class DecimalUniformTests
{
    [Test]
    public void Accepted96BitDomainMapsExactlyOntoScale28Grid()
    {
        UInt128 gridSize = DecimalUniform.GridSize();
        UInt128 acceptanceLimit = DecimalUniform.AcceptanceLimit();
        UInt128 domainSize = DecimalUniform.UInt96DomainSize();

        Assert.Multiple(() =>
        {
            Assert.That(acceptanceLimit / gridSize, Is.EqualTo((UInt128)7));
            Assert.That(acceptanceLimit % gridSize, Is.EqualTo((UInt128)0));
            Assert.That(domainSize, Is.GreaterThan(acceptanceLimit));
            Assert.That(domainSize - acceptanceLimit, Is.LessThan(gridSize));
            Assert.That(DecimalUniform.IsAcceptedSample((UInt128)0), Is.True);
            Assert.That(DecimalUniform.IsAcceptedSample(acceptanceLimit - (UInt128)1), Is.True);
            Assert.That(DecimalUniform.IsAcceptedSample(acceptanceLimit), Is.False);
            Assert.That((acceptanceLimit - (UInt128)1) % gridSize, Is.EqualTo(gridSize - (UInt128)1));
        });
    }

    [Test]
    public void GridConversionIncludesZeroAndExcludesOneExactly()
    {
        decimal zero = DecimalUniform.DecimalFromCoefficient((UInt128)0);
        decimal maximum = DecimalUniform.DecimalFromCoefficient(DecimalUniform.GridSize() - (UInt128)1);
        int[] zeroBits = decimal.GetBits(zero);
        int[] maximumBits = decimal.GetBits(maximum);

        Assert.Multiple(() =>
        {
            Assert.That(zero, Is.EqualTo(decimal.Zero));
            Assert.That((zeroBits[3] >> 16) & 0x7F, Is.EqualTo(28));
            Assert.That(maximum, Is.EqualTo(0.9999999999999999999999999999m));
            Assert.That(maximum, Is.LessThan(decimal.One));
            Assert.That((maximumBits[3] >> 16) & 0x7F, Is.EqualTo(28));
            RandomArgumentException error = ReflectionInfo.Required<RandomArgumentException>(Assert.Throws<RandomArgumentException>(() => DecimalUniform.DecimalFromCoefficient(DecimalUniform.GridSize())));
            Assert.That(error.ArgumentName(), Is.EqualTo("coefficient"));
        });
    }

    [Test]
    public void SplitMixWordsProduceLockedDecimalAndConsumption()
    {
        RandomGenerator generator = RandomGenerator.CreateSplitMix64(42UL, RandomMathMode.Portable);

        decimal value = generator.NextDecimal();

        Assert.Multiple(() =>
        {
            Assert.That(value, Is.EqualTo(0.5212780341142175747211488917m));
            Assert.That(generator.NextUInt64(), Is.EqualTo(5139283748462763858UL));
        });
    }

    [Test]
    public void Rejected96BitCandidateConsumesOnlyItsTwoWords()
    {
        RandomGenerator generator = RandomGenerator.CreateSplitMix64(7UL, RandomMathMode.Portable);

        decimal value = generator.NextDecimal();

        Assert.Multiple(() =>
        {
            Assert.That(value, Is.EqualTo(0.1952106790745595452792580610m));
            Assert.That(generator.NextUInt64(), Is.EqualTo(8346079845500723674UL));
        });
    }

    [Test]
    public void PreviousDecimalFindsRepresentableLowerNeighbors()
    {
        Assert.Multiple(() =>
        {
            Assert.That(DecimalUniform.Previous(decimal.One), Is.EqualTo(0.9999999999999999999999999999m));
            Assert.That(DecimalUniform.Previous(0.0000000000000000000000000001m), Is.EqualTo(decimal.Zero));
            Assert.That(DecimalUniform.Previous(decimal.Zero), Is.EqualTo(-0.0000000000000000000000000001m));
            Assert.That(DecimalUniform.Previous(-0.0000000000000000000000000001m), Is.EqualTo(-0.0000000000000000000000000002m));
            Assert.That(DecimalUniform.Previous(decimal.MaxValue), Is.EqualTo(decimal.MaxValue - decimal.One));
        });
    }

    [Test]
    public void RangedDecimalsHandleFullWidthAndExcludedAdjacentMaximum()
    {
        RandomGenerator generator = RandomGenerator.CreatePcg64Dxsm((UInt128)123, (UInt128)456, RandomMathMode.Portable);
        decimal adjacentMinimum = decimal.MaxValue - decimal.One;

        for (int index = 0; index < 1000; index++)
        {
            decimal full = generator.NextDecimalInRange(decimal.MinValue, decimal.MaxValue);
            decimal adjacent = generator.NextDecimalInRange(adjacentMinimum, decimal.MaxValue);
            Assert.Multiple(() =>
            {
                Assert.That(full, Is.GreaterThanOrEqualTo(decimal.MinValue));
                Assert.That(full, Is.LessThan(decimal.MaxValue));
                Assert.That(adjacent, Is.EqualTo(adjacentMinimum));
            });
        }
    }

    [Test]
    public void DegenerateAndInvalidRangesDoNotConsumeState()
    {
        RandomGenerator generator = RandomGenerator.CreateSplitMix64(42UL, RandomMathMode.Portable);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        decimal scaled = 1.2300m;

        decimal result = generator.NextDecimalInRange(scaled, scaled);
        RandomArgumentException error = Assert.Throws<RandomArgumentException>(() => generator.NextDecimalInRange(2.0m, 1.0m));

        Assert.Multiple(() =>
        {
            Assert.That(decimal.GetBits(result), Is.EqualTo(decimal.GetBits(scaled)));
            Assert.That(error.ArgumentName(), Is.EqualTo("maximum"));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        });
    }

    [Test]
    public void UnitAndRangedSamplesAlwaysHonorContracts()
    {
        RandomGenerator generator = RandomGenerator.CreateXoshiro256StarStar(987654321UL, RandomMathMode.Portable);
        for (int index = 0; index < 10000; index++)
        {
            decimal unit = generator.NextDecimal();
            decimal ranged = generator.NextDecimalInRange(-12345678901234567890.25m, 70000000000000000000.75m);
            Assert.Multiple(() =>
            {
                Assert.That(unit, Is.GreaterThanOrEqualTo(decimal.Zero));
                Assert.That(unit, Is.LessThan(decimal.One));
                Assert.That(ranged, Is.GreaterThanOrEqualTo(-12345678901234567890.25m));
                Assert.That(ranged, Is.LessThan(70000000000000000000.75m));
            });
        }
    }
}
