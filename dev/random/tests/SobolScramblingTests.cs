namespace random.Tests;

public class SobolScramblingTests
{
    [Test]
    public void ScrambledPointsMatchIndependentRawGrayCodeConstruction()
    {
        DigitalScrambler scrambler = DigitalScrambler.CreateLinearMatrixWithDigitalShift(Create(6006UL), 4);
        SobolSequence sequence = new SobolSequence(4, scrambler);

        for (ulong pointIndex = 0UL; pointIndex < 32UL; pointIndex++)
        {
            double[] expected = ReferencePoint(scrambler, pointIndex, 4);
            Assert.That(sequence.Next(), Is.EqualTo(expected), $"point {pointIndex}");
        }
    }

    [Test]
    public void EveryScrambleKindIntegratesWithSobolSequence()
    {
        DigitalScrambler[] scramblers =
        [
            DigitalScrambler.CreateDigitalShift(Create(7007UL), 3),
            DigitalScrambler.CreateLinearMatrix(Create(7008UL), 3),
            DigitalScrambler.CreateLinearMatrixWithDigitalShift(Create(7009UL), 3)
        ];

        foreach (DigitalScrambler scrambler in scramblers)
        {
            SobolSequence sequence = new SobolSequence(3, scrambler);
            for (ulong pointIndex = 0UL; pointIndex < 16UL; pointIndex++)
            {
                Assert.That(sequence.Next(), Is.EqualTo(ReferencePoint(scrambler, pointIndex, 3)));
            }
        }
    }

    [Test]
    public void ScrambledFillFastForwardAndResetReplayExactly()
    {
        DigitalScrambler scrambler = DigitalScrambler.CreateLinearMatrixWithDigitalShift(Create(8008UL), 6);
        SobolSequence block = new SobolSequence(6, scrambler);
        SobolSequence scalar = new SobolSequence(6, scrambler);
        block.FastForward(123456789012345UL);
        scalar.FastForward(123456789012345UL);
        double[] actual = block.Fill(129);
        double[] expected = new double[actual.Length];
        for (int point = 0; point < 129; point++)
        {
            scalar.NextInto(expected.AsSpan(point * 6, 6));
        }

        Assert.That(actual, Is.EqualTo(expected));
        block.Reset();
        scalar.Reset();
        Assert.Multiple(() =>
        {
            Assert.That(block.Index(), Is.Zero);
            Assert.That(block.FillPowerOfTwo(4), Is.EqualTo(scalar.FillPowerOfTwo(4)));
        });
    }

    [Test]
    public void LinearMatrixAndShiftPreservePowerOfTwoCoordinateBalance()
    {
        const int exponent = 6;
        const int pointCount = 1 << exponent;
        DigitalScrambler scrambler = DigitalScrambler.CreateLinearMatrixWithDigitalShift(Create(9009UL), 8);
        SobolSequence sequence = new SobolSequence(8, scrambler);
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
    public void SequenceOperationsDoNotRetainOrConsumeTheScramblingGenerator()
    {
        RandomGenerator generator = Create(10010UL);
        DigitalScrambler scrambler = DigitalScrambler.CreateLinearMatrixWithDigitalShift(generator, 5);
        byte[] afterConstruction = generator.ExportState().ToBytes();
        SobolSequence sequence = new SobolSequence(5, scrambler);

        sequence.FillPowerOfTwo(6);
        sequence.FastForward(123456UL);
        sequence.Next();
        sequence.Reset();

        Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(afterConstruction));
    }

    [Test]
    public void ScramblerValidationCompletesBeforeSequenceAllocationOrMutation()
    {
        DigitalScrambler scrambler = DigitalScrambler.CreateDigitalShift(Create(11011UL), 3);
        RandomArgumentException mismatch = Assert.Throws<RandomArgumentException>(() => new SobolSequence(2, scrambler));
        Assert.That(mismatch.ArgumentName(), Is.EqualTo("scrambler"));
        RandomArgumentException nullError = Assert.Throws<RandomArgumentException>(() => new SobolSequence(2, (DigitalScrambler)NullReference.Value<global::random.DigitalScrambler>()));
        Assert.That(nullError.ArgumentName(), Is.EqualTo("scrambler"));
    }

    static double[] ReferencePoint(DigitalScrambler scrambler, ulong pointIndex, int dimension)
    {
        double[] result = new double[dimension];
        ulong grayCode = pointIndex ^ (pointIndex >> 1);
        ulong[] directions = new ulong[64];
        for (int coordinate = 0; coordinate < dimension; coordinate++)
        {
            SobolDirectionNumbers.Fill(coordinate + 1, directions);
            ulong raw = 0UL;
            ulong remaining = grayCode;
            int directionIndex = 0;
            while (remaining != 0UL)
            {
                if ((remaining & 1UL) != 0UL)
                {
                    raw ^= directions[directionIndex];
                }

                remaining >>= 1;
                directionIndex++;
            }

            result[coordinate] = ToUnitDouble(scrambler.Scramble(coordinate, raw));
        }

        return result;
    }

    static double ToUnitDouble(ulong value)
    {
        double result = PortableMath.ScaleByPowerOfTwo((double)value, -64);
        if (result >= 1.0)
        {
            return Math.BitDecrement(1.0);
        }

        return result;
    }

    static RandomGenerator Create(ulong seed)
    {
        return RandomGenerator.CreateSplitMix64(seed, RandomMathMode.Portable);
    }
}
