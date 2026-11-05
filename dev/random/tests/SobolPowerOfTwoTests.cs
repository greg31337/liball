namespace random.Tests;

public class SobolPowerOfTwoTests
{
    [Test]
    public void PowerOfTwoFillMatchesOrdinaryRecurrenceExactly()
    {
        SobolSequence balanced = SobolSequence.CreateUnscrambled(5);
        SobolSequence ordinary = SobolSequence.CreateUnscrambled(5);

        double[] actual = balanced.FillPowerOfTwo(5);
        double[] expected = ordinary.Fill(32);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(balanced.Index(), Is.EqualTo(32UL));
            Assert.That(balanced.Index(), Is.EqualTo(ordinary.Index()));
        });
    }

    [Test]
    public void DestinationAndAllocatingPowerOfTwoFillsMatchExactly()
    {
        SobolSequence allocating = SobolSequence.CreateUnscrambled(7);
        SobolSequence destination = SobolSequence.CreateUnscrambled(7);
        double[] expected = new double[destination.RequiredPowerOfTwoOutputLength(4)];

        double[] actual = allocating.FillPowerOfTwo(4);
        destination.FillPowerOfTwoInto(4, expected);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(allocating.Index(), Is.EqualTo(destination.Index()));
            Assert.That(allocating.Index(), Is.EqualTo(16UL));
        });
    }

    [Test]
    public void ConsecutiveAlignedBlocksAreAccepted()
    {
        SobolSequence balanced = SobolSequence.CreateUnscrambled(3);
        SobolSequence ordinary = SobolSequence.CreateUnscrambled(3);

        double[] first = balanced.FillPowerOfTwo(2);
        double[] second = balanced.FillPowerOfTwo(2);
        double[] expected = ordinary.Fill(8);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(expected.AsSpan(0, 12).ToArray()));
            Assert.That(second, Is.EqualTo(expected.AsSpan(12, 12).ToArray()));
            Assert.That(balanced.Index(), Is.EqualTo(8UL));
        });
    }

    [Test]
    public void AlignedBlocksOccupyEveryOneDimensionalBinaryCell()
    {
        const int exponent = 6;
        const int pointCount = 1 << exponent;
        SobolSequence sequence = SobolSequence.CreateUnscrambled(8);
        double[] points = sequence.FillPowerOfTwo(exponent);

        for (int coordinate = 0; coordinate < sequence.Dimension(); coordinate++)
        {
            bool[] occupied = new bool[pointCount];
            for (int point = 0; point < pointCount; point++)
            {
                int cell = (int)(points[point * sequence.Dimension() + coordinate] * pointCount);
                Assert.That(occupied[cell], Is.False, $"coordinate {coordinate}, cell {cell}");
                occupied[cell] = true;
            }

            Assert.That(occupied, Is.All.True);
        }
    }

    [Test]
    public void MisalignedBlockIsRejectedWithoutAdvancingOrWriting()
    {
        SobolSequence sequence = SobolSequence.CreateUnscrambled(2);
        sequence.FastForward(2UL);
        double[] destination = new double[8];
        Array.Fill(destination, 17.0);

        Assert.Throws<RandomStateException>(() => sequence.FillPowerOfTwoInto(2, destination));
        Assert.Throws<RandomStateException>(() => sequence.FillPowerOfTwo(2));

        Assert.Multiple(() =>
        {
            Assert.That(sequence.Index(), Is.EqualTo(2UL));
            Assert.That(destination, Is.All.EqualTo(17.0));
        });
    }

    [Test]
    public void InvalidExponentAndDestinationRequestsAreAtomic()
    {
        SobolSequence sequence = SobolSequence.CreateUnscrambled(3);
        sequence.FastForward(8UL);
        double[] destination = [2.0, 3.0, 5.0];
        object[] cases =
        [
            new object[] { (TestDelegate)(() => sequence.FillPowerOfTwoInto(-1, Span<double>.Empty)), "exponent" },
            new object[] { (TestDelegate)(() => sequence.FillPowerOfTwoInto(31, Span<double>.Empty)), "exponent" },
            new object[] { (TestDelegate)(() => sequence.FillPowerOfTwoInto(1, destination)), "destination" },
            new object[] { (TestDelegate)(() => sequence.RequiredPowerOfTwoOutputLength(30)), "exponent" }
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
        }

        Assert.Multiple(() =>
        {
            Assert.That(sequence.Index(), Is.EqualTo(8UL));
            Assert.That(destination, Is.EqualTo(new double[] { 2.0, 3.0, 5.0 }));
        });
    }

    [Test]
    public void ExhaustingPowerOfTwoBlockIsRejectedBeforeWriting()
    {
        SobolSequence sequence = SobolSequence.CreateUnscrambled(2);
        sequence.FastForward(ulong.MaxValue - 3UL);
        double[] destination = new double[8];
        Array.Fill(destination, 23.0);

        Assert.Throws<RandomExhaustedException>(() => sequence.FillPowerOfTwoInto(2, destination));

        Assert.Multiple(() =>
        {
            Assert.That(sequence.Index(), Is.EqualTo(ulong.MaxValue - 3UL));
            Assert.That(destination, Is.All.EqualTo(23.0));
        });
    }

    [Test]
    public void SinglePointPowerOfTwoBlockWorksAtEveryNonterminalIndex()
    {
        SobolSequence balanced = SobolSequence.CreateUnscrambled(4);
        SobolSequence scalar = SobolSequence.CreateUnscrambled(4);
        balanced.FastForward(12345UL);
        scalar.FastForward(12345UL);

        Assert.That(balanced.FillPowerOfTwo(0), Is.EqualTo(scalar.Next()));
        Assert.That(balanced.Index(), Is.EqualTo(12346UL));
    }
}
