namespace random.Tests;

public class HammersleySetTests
{
    [Test]
    public void InitialPointsMatchTheDocumentedDimensionAndIndexLayout()
    {
        HammersleySet set = new HammersleySet(8, 3);
        double[][] expected =
        [
            [0.0, 0.0, 0.0],
            [1.0 / 8.0, 1.0 / 2.0, 1.0 / 3.0],
            [2.0 / 8.0, 1.0 / 4.0, 2.0 / 3.0],
            [3.0 / 8.0, 3.0 / 4.0, 1.0 / 9.0],
            [4.0 / 8.0, 1.0 / 8.0, 4.0 / 9.0],
            [5.0 / 8.0, 5.0 / 8.0, 7.0 / 9.0],
            [6.0 / 8.0, 3.0 / 8.0, 2.0 / 9.0],
            [7.0 / 8.0, 7.0 / 8.0, 5.0 / 9.0]
        ];

        Assert.Multiple(() =>
        {
            Assert.That(set.PointCount(), Is.EqualTo(8));
            Assert.That(set.Dimension(), Is.EqualTo(3));
            Assert.That(set.Bases(), Is.EqualTo(new int[] { 2, 3 }));
            Assert.That(set.Remaining(), Is.EqualTo(8));
        });

        for (int point = 0; point < expected.Length; point++)
        {
            double[] actual = set.Next();
            for (int coordinate = 0; coordinate < actual.Length; coordinate++)
            {
                Assert.That(actual[coordinate], Is.EqualTo(expected[point][coordinate]).Within(1.0e-15));
            }

            Assert.That(set.Index(), Is.EqualTo((ulong)(point + 1)));
            Assert.That(set.Remaining(), Is.EqualTo(7 - point));
        }
    }

    [Test]
    public void BasesAreConsecutivePrimesAndDefensiveCopies()
    {
        HammersleySet set = new HammersleySet(10, 7);
        int[] expected = [2, 3, 5, 7, 11, 13];
        int[] bases = set.Bases();

        Assert.That(bases, Is.EqualTo(expected));
        bases[0] = 97;

        Assert.That(set.Bases(), Is.EqualTo(expected));
    }

    [Test]
    public void BlockFillUsesPointMajorLayoutAndMatchesRepeatedNextExactly()
    {
        HammersleySet blockSet = new HammersleySet(1000, 5);
        HammersleySet scalarSet = new HammersleySet(1000, 5);
        blockSet.Skip(123UL);
        scalarSet.Skip(123UL);
        double[] actual = new double[blockSet.RequiredOutputLength(257)];
        double[] expected = new double[actual.Length];

        blockSet.FillInto(257, actual);
        for (int point = 0; point < 257; point++)
        {
            scalarSet.NextInto(expected.AsSpan(point * 5, 5));
        }

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(blockSet.Index(), Is.EqualTo(scalarSet.Index()));
            Assert.That(blockSet.Index(), Is.EqualTo(380UL));
            Assert.That(blockSet.Remaining(), Is.EqualTo(620));
        });
    }

    [Test]
    public void AllocatingAndDestinationBlockFillsMatchExactly()
    {
        HammersleySet allocating = new HammersleySet(100, 4);
        HammersleySet destination = new HammersleySet(100, 4);
        allocating.Skip(9UL);
        destination.Skip(9UL);
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
    public void SkipMatchesDiscardedPointsAndResetReplaysFromTheOrigin()
    {
        HammersleySet skipped = new HammersleySet(200, 4);
        HammersleySet advanced = new HammersleySet(200, 4);
        advanced.Fill(73);

        skipped.Skip(73UL);
        Assert.That(skipped.Next(), Is.EqualTo(advanced.Next()));

        skipped.Reset();
        Assert.Multiple(() =>
        {
            Assert.That(skipped.Index(), Is.Zero);
            Assert.That(skipped.Remaining(), Is.EqualTo(200));
            Assert.That(skipped.Next(), Is.EqualTo(new double[] { 0.0, 0.0, 0.0, 0.0 }));
        });
    }

    [Test]
    public void OneDimensionalSetIsTheClosedOpenRegularGrid()
    {
        HammersleySet set = new HammersleySet(5, 1);

        Assert.That(set.Bases(), Is.Empty);
        Assert.That(set.Fill(5), Is.EqualTo(new double[] { 0.0, 0.2, 0.4, 0.6, 0.8 }));
        Assert.That(set.Index(), Is.EqualTo(5UL));
    }

    [Test]
    public void FiniteSetExhaustionIsExactAndAtomic()
    {
        HammersleySet set = new HammersleySet(3, 2);
        double[] allPoints = set.Fill(3);
        double[] destination = [17.0, 19.0];

        Assert.Multiple(() =>
        {
            Assert.That(allPoints.Length, Is.EqualTo(6));
            Assert.That(set.Index(), Is.EqualTo(3UL));
            Assert.That(set.Remaining(), Is.Zero);
        });

        Assert.Throws<RandomExhaustedException>(() => set.NextInto(destination));
        Assert.Throws<RandomExhaustedException>(() => set.FillInto(1, destination));
        Assert.Throws<RandomExhaustedException>(() => set.Skip(1UL));
        Assert.Multiple(() =>
        {
            Assert.That(set.Index(), Is.EqualTo(3UL));
            Assert.That(destination, Is.EqualTo(new double[] { 17.0, 19.0 }));
        });

        set.FillInto(0, Span<double>.Empty);
        set.Skip(0UL);
        Assert.That(set.Index(), Is.EqualTo(3UL));
    }

    [Test]
    public void InvalidConstructionAndStorageRequestsAreRejected()
    {
        object[] constructionCases =
        [
            new object[] { (TestDelegate)(() => new HammersleySet(0, 2)), "pointCount" },
            new object[] { (TestDelegate)(() => new HammersleySet(-1, 2)), "pointCount" },
            new object[] { (TestDelegate)(() => new HammersleySet(8, 0)), "dimension" },
            new object[] { (TestDelegate)(() => new HammersleySet(8, -1)), "dimension" }
        ];

        foreach (object[] testCase in constructionCases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>((TestDelegate)testCase[0]);
            Assert.That(error.ArgumentName(), Is.EqualTo((string)testCase[1]));
        }

        HammersleySet set = new HammersleySet(20, 3);
        set.Skip(4UL);
        double[] destination = [7.0, 11.0, 13.0];
        RandomArgumentException negative = Assert.Throws<RandomArgumentException>(() => set.FillInto(-1, Span<double>.Empty));
        Assert.That(negative.ArgumentName(), Is.EqualTo("pointCount"));
        RandomArgumentException blockShape = Assert.Throws<RandomArgumentException>(() => set.FillInto(2, destination));
        Assert.That(blockShape.ArgumentName(), Is.EqualTo("destination"));
        RandomArgumentException pointShape = Assert.Throws<RandomArgumentException>(() => set.NextInto(destination.AsSpan(0, 2)));
        Assert.That(pointShape.ArgumentName(), Is.EqualTo("destination"));

        Assert.Multiple(() =>
        {
            Assert.That(set.Index(), Is.EqualTo(4UL));
            Assert.That(destination, Is.EqualTo(new double[] { 7.0, 11.0, 13.0 }));
        });
    }

    [Test]
    public void OverrunAndOutputLengthOverflowLeaveTheIndexUnchanged()
    {
        HammersleySet set = new HammersleySet(10, 3);
        set.Skip(9UL);
        double[] destination = [2.0, 3.0, 5.0, 7.0, 11.0, 13.0];

        Assert.Throws<RandomExhaustedException>(() => set.FillInto(2, destination));
        Assert.Throws<RandomExhaustedException>(() => set.Skip(2UL));
        RandomArgumentException overflow = Assert.Throws<RandomArgumentException>(() => set.RequiredOutputLength(int.MaxValue));
        Assert.That(overflow.ArgumentName(), Is.EqualTo("pointCount"));

        Assert.Multiple(() =>
        {
            Assert.That(set.Index(), Is.EqualTo(9UL));
            Assert.That(destination, Is.EqualTo(new double[] { 2.0, 3.0, 5.0, 7.0, 11.0, 13.0 }));
        });
    }
}
