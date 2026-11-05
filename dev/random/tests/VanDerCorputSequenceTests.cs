namespace random.Tests;

public class VanDerCorputSequenceTests
{
    [Test]
    public void BaseTwoMatchesKnownRadicalInverseValuesExactly()
    {
        VanDerCorputSequence sequence = new VanDerCorputSequence(2);
        double[] expected =
        [
            0.0,
            0.5,
            0.25,
            0.75,
            0.125,
            0.625,
            0.375,
            0.875,
            0.0625
        ];

        Assert.That(sequence.Base(), Is.EqualTo(2));
        for (int index = 0; index < expected.Length; index++)
        {
            Assert.That(sequence.Index(), Is.EqualTo((ulong)index));
            Assert.That(sequence.Next(), Is.EqualTo(expected[index]));
        }

        Assert.That(sequence.Index(), Is.EqualTo((ulong)expected.Length));
    }

    [Test]
    public void OtherBasesMatchKnownRadicalInverseValues()
    {
        VanDerCorputSequence baseThree = new VanDerCorputSequence(3);
        double[] expected =
        [
            0.0,
            1.0 / 3.0,
            2.0 / 3.0,
            1.0 / 9.0,
            4.0 / 9.0,
            7.0 / 9.0,
            2.0 / 9.0,
            5.0 / 9.0,
            8.0 / 9.0,
            1.0 / 27.0
        ];

        for (int index = 0; index < expected.Length; index++)
        {
            Assert.That(baseThree.Next(), Is.EqualTo(expected[index]).Within(1.0e-15));
        }
    }

    [Test]
    public void FillMatchesRepeatedNextCallsExactly()
    {
        VanDerCorputSequence fillSequence = new VanDerCorputSequence(7);
        VanDerCorputSequence scalarSequence = new VanDerCorputSequence(7);
        fillSequence.Skip(123456UL);
        scalarSequence.Skip(123456UL);
        double[] actual = new double[257];
        double[] expected = new double[actual.Length];

        fillSequence.Fill(actual);
        for (int index = 0; index < expected.Length; index++)
        {
            expected[index] = scalarSequence.Next();
        }

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(fillSequence.Index(), Is.EqualTo(scalarSequence.Index()));
            Assert.That(fillSequence.Index(), Is.EqualTo(123713UL));
        });
    }

    [Test]
    public void SkipMatchesDiscardedValuesAndResetReplaysFromZero()
    {
        VanDerCorputSequence skipped = new VanDerCorputSequence(11);
        VanDerCorputSequence advanced = new VanDerCorputSequence(11);
        double[] discarded = new double[1000];
        advanced.Fill(discarded);

        skipped.Skip(1000UL);
        Assert.Multiple(() =>
        {
            Assert.That(skipped.Index(), Is.EqualTo(1000UL));
            Assert.That(skipped.Next(), Is.EqualTo(advanced.Next()));
        });

        skipped.Reset();
        Assert.Multiple(() =>
        {
            Assert.That(skipped.Index(), Is.Zero);
            Assert.That(skipped.Next(), Is.Zero);
            Assert.That(skipped.Index(), Is.EqualTo(1UL));
        });
    }

    [Test]
    public void ValuesRemainInTheClosedOpenUnitIntervalNearTheIndexLimit()
    {
        int[] bases = [2, 3, 10, int.MaxValue];
        foreach (int numberBase in bases)
        {
            VanDerCorputSequence sequence = new VanDerCorputSequence(numberBase);
            sequence.Skip(ulong.MaxValue - 4UL);
            double[] values = new double[4];
            sequence.Fill(values);

            Assert.That(sequence.Index(), Is.EqualTo(ulong.MaxValue));
            foreach (double value in values)
            {
                Assert.That(value, Is.GreaterThanOrEqualTo(0.0));
                Assert.That(value, Is.LessThan(1.0));
            }
        }
    }

    [Test]
    public void InvalidBasesAreRejected()
    {
        int[] invalidBases = [int.MinValue, -1, 0, 1];
        foreach (int numberBase in invalidBases)
        {
            RandomArgumentException error = Assert.Throws<RandomArgumentException>(() => new VanDerCorputSequence(numberBase));
            Assert.That(error.ArgumentName(), Is.EqualTo("numberBase"));
        }
    }

    [Test]
    public void ExhaustingOperationsAreAtomic()
    {
        VanDerCorputSequence sequence = new VanDerCorputSequence(2);
        sequence.Skip(ulong.MaxValue - 1UL);
        double[] destination = [17.0, 19.0];

        Assert.Throws<RandomExhaustedException>(() => sequence.Fill(destination));
        Assert.Multiple(() =>
        {
            Assert.That(sequence.Index(), Is.EqualTo(ulong.MaxValue - 1UL));
            Assert.That(destination, Is.EqualTo(new double[] { 17.0, 19.0 }));
        });

        Assert.That(sequence.Next(), Is.GreaterThanOrEqualTo(0.0));
        Assert.That(sequence.Index(), Is.EqualTo(ulong.MaxValue));
        Assert.Throws<RandomExhaustedException>(() => sequence.Next());
        Assert.Throws<RandomExhaustedException>(() => sequence.Skip(1UL));
        Assert.That(sequence.Index(), Is.EqualTo(ulong.MaxValue));
    }

    [Test]
    public void EmptyFillAndZeroSkipDoNotChangeTheIndex()
    {
        VanDerCorputSequence sequence = new VanDerCorputSequence(5);
        sequence.Skip(123UL);

        sequence.Fill(Span<double>.Empty);
        sequence.Skip(0UL);

        Assert.That(sequence.Index(), Is.EqualTo(123UL));
    }
}
