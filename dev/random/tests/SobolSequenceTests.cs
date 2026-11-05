namespace random.Tests;

public class SobolSequenceTests
{
    [Test]
    public void InitialTwoDimensionalPointsMatchTheJoeKuoReferenceOrder()
    {
        SobolSequence sequence = SobolSequence.CreateUnscrambled(2);
        double[][] expected =
        [
            [0.0, 0.0],
            [0.5, 0.5],
            [0.75, 0.25],
            [0.25, 0.75],
            [0.375, 0.375],
            [0.875, 0.875],
            [0.625, 0.125],
            [0.125, 0.625]
        ];

        Assert.That(sequence.Dimension(), Is.EqualTo(2));
        for (int point = 0; point < expected.Length; point++)
        {
            Assert.That(sequence.Index(), Is.EqualTo((ulong)point));
            Assert.That(sequence.Next(), Is.EqualTo(expected[point]));
        }

        Assert.That(sequence.Index(), Is.EqualTo(8UL));
    }

    [Test]
    public void InitialThreeDimensionalPointsMatchDirectionNumberReferences()
    {
        SobolSequence sequence = SobolSequence.CreateUnscrambled(3);
        double[][] expected =
        [
            [0.0, 0.0, 0.0],
            [0.5, 0.5, 0.5],
            [0.75, 0.25, 0.25],
            [0.25, 0.75, 0.75],
            [0.375, 0.375, 0.625],
            [0.875, 0.875, 0.125],
            [0.625, 0.125, 0.875],
            [0.125, 0.625, 0.375]
        ];

        foreach (double[] point in expected)
        {
            Assert.That(sequence.Next(), Is.EqualTo(point));
        }
    }

    [Test]
    public void BlockFillUsesPointMajorLayoutAndMatchesRepeatedNextExactly()
    {
        SobolSequence blockSequence = SobolSequence.CreateUnscrambled(7);
        SobolSequence scalarSequence = SobolSequence.CreateUnscrambled(7);
        blockSequence.FastForward(12345UL);
        scalarSequence.FastForward(12345UL);
        double[] actual = new double[blockSequence.RequiredOutputLength(257)];
        double[] expected = new double[actual.Length];

        blockSequence.FillInto(257, actual);
        for (int point = 0; point < 257; point++)
        {
            scalarSequence.NextInto(expected.AsSpan(point * 7, 7));
        }

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(blockSequence.Index(), Is.EqualTo(scalarSequence.Index()));
            Assert.That(blockSequence.Index(), Is.EqualTo(12602UL));
        });
    }

    [Test]
    public void AllocatingAndDestinationBlockFillsMatchExactly()
    {
        SobolSequence allocating = SobolSequence.CreateUnscrambled(5);
        SobolSequence destination = SobolSequence.CreateUnscrambled(5);
        allocating.Skip(77UL);
        destination.Skip(77UL);
        double[] expected = new double[55];

        double[] actual = allocating.Fill(11);
        destination.FillInto(11, expected);

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(allocating.Index(), Is.EqualTo(destination.Index()));
        });
    }

    [Test]
    public void FastForwardMatchesRepeatedRecurrenceAtManyPositions()
    {
        ulong[] positions = [0UL, 1UL, 2UL, 3UL, 31UL, 32UL, 255UL, 1024UL, 10000UL];
        foreach (ulong position in positions)
        {
            SobolSequence direct = SobolSequence.CreateUnscrambled(6);
            SobolSequence repeated = SobolSequence.CreateUnscrambled(6);
            direct.FastForward(position);
            double[] discarded = new double[6];
            ulong step = 0UL;
            while (step < position)
            {
                repeated.NextInto(discarded);
                step++;
            }

            Assert.Multiple(() =>
            {
                Assert.That(direct.Index(), Is.EqualTo(position));
                Assert.That(direct.Next(), Is.EqualTo(repeated.Next()), $"position {position}");
            });
        }
    }

    [Test]
    public void FastForwardSupportsLargeDistancesAndCompositionExactly()
    {
        ulong firstDistance = (1UL << 60) + 123456789UL;
        ulong secondDistance = (1UL << 40) + 98765UL;
        SobolSequence composed = SobolSequence.CreateUnscrambled(8);
        SobolSequence direct = SobolSequence.CreateUnscrambled(8);

        composed.FastForward(firstDistance);
        composed.FastForward(secondDistance);
        direct.FastForward(firstDistance + secondDistance);

        Assert.Multiple(() =>
        {
            Assert.That(composed.Index(), Is.EqualTo(firstDistance + secondDistance));
            Assert.That(composed.Next(), Is.EqualTo(direct.Next()));
        });
    }

    [Test]
    public void ResetReplaysTheSequenceFromTheOrigin()
    {
        SobolSequence sequence = SobolSequence.CreateUnscrambled(4);
        double[] expected = sequence.Fill(32);
        sequence.FastForward(123456UL);

        sequence.Reset();

        Assert.Multiple(() =>
        {
            Assert.That(sequence.Index(), Is.Zero);
            Assert.That(sequence.Fill(32), Is.EqualTo(expected));
        });
    }

    [Test]
    public void MaximumDimensionIsSupportedAndValuesStayInTheUnitCube()
    {
        SobolSequence sequence = SobolSequence.CreateUnscrambled(21201);
        sequence.FastForward(ulong.MaxValue - 4UL);
        double[] points = sequence.Fill(4);

        Assert.That(sequence.Index(), Is.EqualTo(ulong.MaxValue));
        foreach (double value in points)
        {
            Assert.That(value, Is.GreaterThanOrEqualTo(0.0));
            Assert.That(value, Is.LessThan(1.0));
        }
    }

    [Test]
    public void InvalidConstructionAndOutputRequestsAreAtomic()
    {
        object[] constructionCases =
        [
            new object[] { (TestDelegate)(() => SobolSequence.CreateUnscrambled(0)), "dimension" },
            new object[] { (TestDelegate)(() => SobolSequence.CreateUnscrambled(-1)), "dimension" },
            new object[] { (TestDelegate)(() => SobolSequence.CreateUnscrambled(21202)), "dimension" }
        ];

        foreach (object[] testCase in constructionCases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
        }

        SobolSequence sequence = SobolSequence.CreateUnscrambled(3);
        sequence.FastForward(17UL);
        double[] destination = [7.0, 11.0, 13.0];
        RandomArgumentException negative = Assert.Throws<RandomArgumentException>(() => sequence.FillInto(-1, Span<double>.Empty));
        Assert.That(negative.ArgumentName(), Is.EqualTo("pointCount"));
        RandomArgumentException blockShape = Assert.Throws<RandomArgumentException>(() => sequence.FillInto(2, destination));
        Assert.That(blockShape.ArgumentName(), Is.EqualTo("destination"));
        RandomArgumentException pointShape = Assert.Throws<RandomArgumentException>(() => sequence.NextInto(destination.AsSpan(0, 2)));
        Assert.That(pointShape.ArgumentName(), Is.EqualTo("destination"));
        RandomArgumentException overflow = Assert.Throws<RandomArgumentException>(() => sequence.RequiredOutputLength(int.MaxValue));
        Assert.That(overflow.ArgumentName(), Is.EqualTo("pointCount"));

        Assert.Multiple(() =>
        {
            Assert.That(sequence.Index(), Is.EqualTo(17UL));
            Assert.That(destination, Is.EqualTo(new double[] { 7.0, 11.0, 13.0 }));
        });
    }

    [Test]
    public void IndexExhaustionIsExactAndAtomic()
    {
        SobolSequence sequence = SobolSequence.CreateUnscrambled(2);
        sequence.FastForward(ulong.MaxValue - 1UL);
        double[] destination = [17.0, 19.0, 23.0, 29.0];

        Assert.Throws<RandomExhaustedException>(() => sequence.FillInto(2, destination));
        Assert.Multiple(() =>
        {
            Assert.That(sequence.Index(), Is.EqualTo(ulong.MaxValue - 1UL));
            Assert.That(destination, Is.EqualTo(new double[] { 17.0, 19.0, 23.0, 29.0 }));
        });

        sequence.NextInto(destination.AsSpan(0, 2));
        Assert.That(sequence.Index(), Is.EqualTo(ulong.MaxValue));
        Assert.Throws<RandomExhaustedException>(() => sequence.NextInto(destination.AsSpan(0, 2)));
        Assert.Throws<RandomExhaustedException>(() => sequence.Skip(1UL));
        Assert.Throws<RandomExhaustedException>(() => sequence.FastForward(1UL));
        Assert.That(sequence.Index(), Is.EqualTo(ulong.MaxValue));

        sequence.FillInto(0, Span<double>.Empty);
        sequence.Skip(0UL);
        sequence.FastForward(0UL);
        Assert.That(sequence.Index(), Is.EqualTo(ulong.MaxValue));
    }
}
