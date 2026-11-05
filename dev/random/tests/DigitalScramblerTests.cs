namespace random.Tests;

public class DigitalScramblerTests
{
    [Test]
    public void DigitalShiftConsumesOneWordPerCoordinateAndXorsExactly()
    {
        RandomGenerator generator = Create(1001UL);
        RandomGenerator reference = Create(1001UL);
        ulong[] shifts = new ulong[4];
        for (int coordinate = 0; coordinate < shifts.Length; coordinate++)
        {
            shifts[coordinate] = reference.NextUInt64();
        }

        DigitalScrambler scrambler = DigitalScrambler.CreateDigitalShift(generator, 4);
        ulong value = 0x0123456789ABCDEFUL;

        Assert.Multiple(() =>
        {
            Assert.That(scrambler.Dimension(), Is.EqualTo(4));
            Assert.That(scrambler.HasDigitalShift(), Is.True);
            Assert.That(scrambler.HasLinearMatrix(), Is.False);
            for (int coordinate = 0; coordinate < shifts.Length; coordinate++)
            {
                Assert.That(scrambler.Scramble(coordinate, value), Is.EqualTo(value ^ shifts[coordinate]));
            }

            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(reference.ExportState().ToBytes()));
        });
    }

    [Test]
    public void LinearMatrixMatchesIndependentLowerTriangularConstruction()
    {
        RandomGenerator generator = Create(2002UL);
        RandomGenerator reference = Create(2002UL);
        ulong[] matrices = ReferenceMatrices(reference, 3, false, new ulong[3]);
        DigitalScrambler scrambler = DigitalScrambler.CreateLinearMatrix(generator, 3);
        ulong[] values = [0UL, 1UL, 0x8000000000000000UL, 0x0123456789ABCDEFUL, ulong.MaxValue];

        Assert.Multiple(() =>
        {
            Assert.That(scrambler.HasLinearMatrix(), Is.True);
            Assert.That(scrambler.HasDigitalShift(), Is.False);
            for (int coordinate = 0; coordinate < 3; coordinate++)
            {
                foreach (ulong value in values)
                {
                    ulong expected = ApplyMatrix(matrices, coordinate, value);
                    Assert.That(scrambler.Scramble(coordinate, value), Is.EqualTo(expected));
                }
            }

            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(reference.ExportState().ToBytes()));
        });
    }

    [Test]
    public void CombinedScrambleUsesCoordinateMajorMatrixThenShiftOrdering()
    {
        RandomGenerator generator = Create(3003UL);
        RandomGenerator reference = Create(3003UL);
        ulong[] shifts = new ulong[2];
        ulong[] matrices = ReferenceMatrices(reference, 2, true, shifts);
        DigitalScrambler scrambler = DigitalScrambler.CreateLinearMatrixWithDigitalShift(generator, 2);
        ulong value = 0xFEDCBA9876543210UL;

        Assert.Multiple(() =>
        {
            Assert.That(scrambler.HasLinearMatrix(), Is.True);
            Assert.That(scrambler.HasDigitalShift(), Is.True);
            for (int coordinate = 0; coordinate < 2; coordinate++)
            {
                ulong expected = ApplyMatrix(matrices, coordinate, value) ^ shifts[coordinate];
                Assert.That(scrambler.Scramble(coordinate, value), Is.EqualTo(expected));
            }

            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(reference.ExportState().ToBytes()));
        });
    }

    [Test]
    public void SameExplicitChildReplaysAndDifferentChildrenDiverge()
    {
        RandomGenerator parent = RandomGenerator.CreateSplitMix64(4004UL, RandomMathMode.Portable);
        DigitalScrambler first = DigitalScrambler.CreateLinearMatrixWithDigitalShift(parent.Spawn(17UL), 5);
        DigitalScrambler replay = DigitalScrambler.CreateLinearMatrixWithDigitalShift(parent.Spawn(17UL), 5);
        DigitalScrambler different = DigitalScrambler.CreateLinearMatrixWithDigitalShift(parent.Spawn(18UL), 5);
        ulong value = 0x123456789ABCDEF0UL;
        bool anyDifference = false;

        for (int coordinate = 0; coordinate < 5; coordinate++)
        {
            Assert.That(first.Scramble(coordinate, value), Is.EqualTo(replay.Scramble(coordinate, value)));
            if (first.Scramble(coordinate, value) != different.Scramble(coordinate, value))
            {
                anyDifference = true;
            }
        }

        Assert.That(anyDifference, Is.True);
    }

    [Test]
    public void InvalidFactoryArgumentsAndCoordinatesAreAtomic()
    {
        RandomArgumentException nullError = Assert.Throws<RandomArgumentException>(() => DigitalScrambler.CreateDigitalShift(NullReference.Value<global::random.IRandomSource>(), 1));
        Assert.That(nullError.ArgumentName(), Is.EqualTo("generator"));

        RandomGenerator generator = Create(5005UL);
        generator.NextBoolean();
        byte[] before = generator.ExportState().ToBytes();
        object[] factoryCases =
        [
            new object[] { (TestDelegate)(() => DigitalScrambler.CreateDigitalShift(generator, 0)), "dimension" },
            new object[] { (TestDelegate)(() => DigitalScrambler.CreateLinearMatrix(generator, -1)), "dimension" },
            new object[] { (TestDelegate)(() => DigitalScrambler.CreateLinearMatrixWithDigitalShift(generator, 21202)), "dimension" }
        ];

        foreach (object[] testCase in factoryCases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
            Assert.That(generator.ExportState().ToBytes(), Is.EqualTo(before));
        }

        DigitalScrambler scrambler = DigitalScrambler.CreateDigitalShift(Create(5006UL), 2);
        RandomArgumentException low = Assert.Throws<RandomArgumentException>(() => scrambler.Scramble(-1, 0UL));
        RandomArgumentException high = Assert.Throws<RandomArgumentException>(() => scrambler.Scramble(2, 0UL));
        Assert.That(low.ArgumentName(), Is.EqualTo("coordinate"));
        Assert.That(high.ArgumentName(), Is.EqualTo("coordinate"));
    }

    static ulong[] ReferenceMatrices(RandomGenerator generator, int dimension, bool includeShift, ulong[] shifts)
    {
        ulong[] matrices = new ulong[dimension * 64];
        for (int coordinate = 0; coordinate < dimension; coordinate++)
        {
            int offset = coordinate * 64;
            matrices[offset] = 0x8000000000000000UL;
            for (int row = 1; row < 64; row++)
            {
                ulong diagonal = 1UL << (63 - row);
                ulong allowed = ulong.MaxValue << (63 - row);
                matrices[offset + row] = (generator.NextUInt64() & allowed) | diagonal;
            }

            if (includeShift)
            {
                shifts[coordinate] = generator.NextUInt64();
            }
        }

        return matrices;
    }

    static ulong ApplyMatrix(ulong[] matrices, int coordinate, ulong value)
    {
        ulong result = 0UL;
        int offset = coordinate * 64;
        for (int row = 0; row < 64; row++)
        {
            if ((System.Numerics.BitOperations.PopCount(value & matrices[offset + row]) & 1) != 0)
            {
                result |= 1UL << (63 - row);
            }
        }

        return result;
    }

    static RandomGenerator Create(ulong seed)
    {
        return RandomGenerator.CreateSplitMix64(seed, RandomMathMode.Portable);
    }
}
