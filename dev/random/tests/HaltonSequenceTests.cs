namespace random.Tests;

public class HaltonSequenceTests
{
    [Test]
    public void GeneratedPrimeBasesAndInitialPointsMatchReferences()
    {
        HaltonSequence sequence = HaltonSequence.CreateUnscrambled(3);
        double[][] expected =
        [
            [0.0, 0.0, 0.0],
            [0.5, 1.0 / 3.0, 1.0 / 5.0],
            [0.25, 2.0 / 3.0, 2.0 / 5.0],
            [0.75, 1.0 / 9.0, 3.0 / 5.0],
            [0.125, 4.0 / 9.0, 4.0 / 5.0],
            [0.625, 7.0 / 9.0, 1.0 / 25.0]
        ];

        Assert.Multiple(() =>
        {
            Assert.That(sequence.Dimension(), Is.EqualTo(3));
            Assert.That(sequence.Bases(), Is.EqualTo(new int[] { 2, 3, 5 }));
        });

        for (int point = 0; point < expected.Length; point++)
        {
            double[] actual = sequence.Next();
            Assert.That(sequence.Index(), Is.EqualTo((ulong)(point + 1)));
            for (int coordinate = 0; coordinate < actual.Length; coordinate++)
            {
                Assert.That(actual[coordinate], Is.EqualTo(expected[point][coordinate]).Within(1.0e-15));
            }
        }
    }

    [Test]
    public void PrimeBaseGenerationProducesConsecutivePrimesAndDefensiveCopies()
    {
        HaltonSequence sequence = HaltonSequence.CreateUnscrambled(12);
        int[] expected = [2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37];
        int[] first = sequence.Bases();

        Assert.That(first, Is.EqualTo(expected));
        first[0] = 97;

        Assert.That(sequence.Bases(), Is.EqualTo(expected));
        Assert.That(sequence.Next()[0], Is.Zero);
        Assert.That(sequence.Next()[0], Is.EqualTo(0.5));
    }

    [Test]
    public void BlockFillUsesPointMajorLayoutAndMatchesRepeatedNextExactly()
    {
        HaltonSequence blockSequence = HaltonSequence.CreateUnscrambled(5);
        HaltonSequence scalarSequence = HaltonSequence.CreateUnscrambled(5);
        blockSequence.Skip(98765UL);
        scalarSequence.Skip(98765UL);
        double[] actual = new double[blockSequence.RequiredOutputLength(129)];
        double[] expected = new double[actual.Length];

        blockSequence.FillInto(129, actual);
        for (int point = 0; point < 129; point++)
        {
            scalarSequence.NextInto(expected.AsSpan(point * 5, 5));
        }

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(blockSequence.Index(), Is.EqualTo(scalarSequence.Index()));
            Assert.That(blockSequence.Index(), Is.EqualTo(98894UL));
        });
    }

    [Test]
    public void AllocatingBlockFillMatchesDestinationFillExactly()
    {
        HaltonSequence allocating = HaltonSequence.CreateUnscrambled(4);
        HaltonSequence destination = HaltonSequence.CreateUnscrambled(4);
        allocating.Skip(41UL);
        destination.Skip(41UL);
        double[] expected = new double[44];

        double[] actual = allocating.Fill(11);
        destination.FillInto(11, expected);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(allocating.Index(), Is.EqualTo(destination.Index()));
        });
    }

    [Test]
    public void DigitPermutationsAreAppliedAndDefensivelyCopied()
    {
        int[][] permutations =
        [
            [0, 1],
            [0, 2, 1]
        ];
        HaltonSequence sequence = new HaltonSequence(2, permutations);
        permutations[1][1] = 1;
        permutations[1][2] = 2;

        double[][] expected =
        [
            [0.0, 0.0],
            [0.5, 2.0 / 3.0],
            [0.25, 1.0 / 3.0],
            [0.75, 2.0 / 9.0],
            [0.125, 8.0 / 9.0]
        ];

        foreach (double[] point in expected)
        {
            double[] actual = sequence.Next();
            Assert.That(actual[0], Is.EqualTo(point[0]).Within(1.0e-15));
            Assert.That(actual[1], Is.EqualTo(point[1]).Within(1.0e-15));
        }
    }

    [Test]
    public void SkipMatchesDiscardedPointsAndResetReplaysTheOrigin()
    {
        HaltonSequence skipped = HaltonSequence.CreateUnscrambled(4);
        HaltonSequence advanced = HaltonSequence.CreateUnscrambled(4);
        advanced.Fill(1000);

        skipped.Skip(1000UL);
        Assert.That(skipped.Next(), Is.EqualTo(advanced.Next()));

        skipped.Reset();
        Assert.Multiple(() =>
        {
            Assert.That(skipped.Index(), Is.Zero);
            Assert.That(skipped.Next(), Is.EqualTo(new double[] { 0.0, 0.0, 0.0, 0.0 }));
            Assert.That(skipped.Index(), Is.EqualTo(1UL));
        });
    }

    [Test]
    public void InvalidDimensionsAndPermutationsAreRejected()
    {
        object[] cases =
        [
            new object[] { (TestDelegate)(() => HaltonSequence.CreateUnscrambled(0)), "dimension" },
            new object[] { (TestDelegate)(() => HaltonSequence.CreateUnscrambled(-1)), "dimension" },
            new object[] { (TestDelegate)(() => new HaltonSequence(2, NullReference.Value<int[][]>())), "digitPermutations" },
            new object[] { (TestDelegate)(() => new HaltonSequence(2, [[0, 1]])), "digitPermutations" },
            new object[] { (TestDelegate)(() => new HaltonSequence(2, [[0, 1], NullReference.Value<int[]>()])), "digitPermutations" },
            new object[] { (TestDelegate)(() => new HaltonSequence(2, [[0], [0, 1, 2]])), "digitPermutations" },
            new object[] { (TestDelegate)(() => new HaltonSequence(2, [[0, 1], [1, 0, 2]])), "digitPermutations" },
            new object[] { (TestDelegate)(() => new HaltonSequence(2, [[0, 1], [0, 1, 1]])), "digitPermutations" },
            new object[] { (TestDelegate)(() => new HaltonSequence(2, [[0, 1], [0, 1, 3]])), "digitPermutations" },
            new object[] { (TestDelegate)(() => new HaltonSequence(2, [[0, 1], [0, -1, 2]])), "digitPermutations" }
        ];

        foreach (object[] testCase in cases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
        }
    }

    [Test]
    public void InvalidBlockRequestsDoNotAdvanceOrWrite()
    {
        HaltonSequence sequence = HaltonSequence.CreateUnscrambled(3);
        sequence.Skip(17UL);
        double[] destination = [7.0, 11.0, 13.0];

        RandomArgumentException negative = Assert.Throws<RandomArgumentException>(() => sequence.FillInto(-1, Span<double>.Empty));
        Assert.That(negative.ArgumentName(), Is.EqualTo("pointCount"));
        RandomArgumentException shape = Assert.Throws<RandomArgumentException>(() => sequence.FillInto(2, destination));
        Assert.That(shape.ArgumentName(), Is.EqualTo("destination"));
        RandomArgumentException pointShape = Assert.Throws<RandomArgumentException>(() => sequence.NextInto(destination.AsSpan(0, 2)));
        Assert.That(pointShape.ArgumentName(), Is.EqualTo("destination"));

        Assert.Multiple(() =>
        {
            Assert.That(sequence.Index(), Is.EqualTo(17UL));
            Assert.That(destination, Is.EqualTo(new double[] { 7.0, 11.0, 13.0 }));
        });
    }

    [Test]
    public void ExhaustionAndStorageOverflowAreRejectedAtomically()
    {
        HaltonSequence sequence = HaltonSequence.CreateUnscrambled(2);
        sequence.Skip(ulong.MaxValue - 1UL);
        double[] destination = [17.0, 19.0, 23.0, 29.0];

        Assert.Throws<RandomExhaustedException>(() => sequence.FillInto(2, destination));
        Assert.Multiple(() =>
        {
            Assert.That(sequence.Index(), Is.EqualTo(ulong.MaxValue - 1UL));
            Assert.That(destination, Is.EqualTo(new double[] { 17.0, 19.0, 23.0, 29.0 }));
        });

        Assert.Throws<RandomArgumentException>(() => sequence.RequiredOutputLength(int.MaxValue));
        Assert.That(sequence.Index(), Is.EqualTo(ulong.MaxValue - 1UL));
        sequence.NextInto(destination.AsSpan(0, 2));
        Assert.That(sequence.Index(), Is.EqualTo(ulong.MaxValue));
        Assert.Throws<RandomExhaustedException>(() => sequence.NextInto(destination.AsSpan(0, 2)));
    }

    [Test]
    public void EmptyBlockAndZeroSkipDoNotChangeTheIndex()
    {
        HaltonSequence sequence = HaltonSequence.CreateUnscrambled(3);
        sequence.Skip(123UL);

        sequence.FillInto(0, Span<double>.Empty);
        sequence.Skip(0UL);

        Assert.That(sequence.Index(), Is.EqualTo(123UL));
    }
}
