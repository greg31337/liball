using System.Collections.Concurrent;
using fake;

namespace fake.Tests;

[TestFixture]
public class SequenceTests
{
    [Test]
    public void DefaultSequenceSupportsNextRepeatFillAndReset()
    {
        SequenceState sequence = Fake.Create(501).Random().Sequence();

        Assert.That(sequence.Next(), Is.EqualTo(0L));
        Assert.That(sequence.Repeat(3), Is.EqualTo(new long[] { 1L, 2L, 3L }));
        long[] destination = new long[3];
        sequence.Fill(destination);
        Assert.That(destination, Is.EqualTo(new long[] { 4L, 5L, 6L }));

        sequence.Reset();

        Assert.That(sequence.Repeat(4), Is.EqualTo(new long[] { 0L, 1L, 2L, 3L }));
    }

    [Test]
    public void FiniteAscendingSequenceExhaustsAtExclusiveBoundary()
    {
        SequenceState sequence = Fake.Create().Random().SequenceFromStartAndIncrementAndBoundaryExclusive(5L, 2L, 12L);

        Assert.That(sequence.Repeat(4), Is.EqualTo(new long[] { 5L, 7L, 9L, 11L }));
        FakeException exception = Assert.Throws<FakeException>(() => sequence.Next());

        Assert.That(exception.Message, Is.EqualTo("Sequence exhausted at its exclusive boundary before position 4."));
        Assert.That(Assert.Throws<FakeException>(() => sequence.Next()).Message, Is.EqualTo(exception.Message));
    }

    [Test]
    public void FiniteDescendingSequenceUsesExclusiveLowerBoundary()
    {
        SequenceState sequence = Fake.Create().Random().SequenceFromStartAndIncrementAndBoundaryExclusive(10L, -3L, -1L);

        Assert.That(sequence.Repeat(4), Is.EqualTo(new long[] { 10L, 7L, 4L, 1L }));
        Assert.That(Assert.Throws<FakeException>(() => sequence.Next()).Message, Does.Contain("exclusive boundary"));
    }

    [Test]
    public void PositiveCycleRestartsBeforeCrossingBoundary()
    {
        SequenceState sequence = Fake.Create().Random().Cycle(5L, 2L, 20L);

        Assert.That(
            sequence.Repeat(18),
            Is.EqualTo(new long[]
            {
                5L, 7L, 9L, 11L, 13L, 15L, 17L, 19L,
                5L, 7L, 9L, 11L, 13L, 15L, 17L, 19L,
                5L, 7L
            }));
    }

    [Test]
    public void NegativeCycleRestartsBeforeCrossingBoundary()
    {
        SequenceState sequence = Fake.Create().Random().Cycle(4L, -3L, -6L);

        Assert.That(sequence.Repeat(10), Is.EqualTo(new long[] { 4L, 1L, -2L, -5L, 4L, 1L, -2L, -5L, 4L, 1L }));
    }

    [Test]
    public void PageIsZeroBasedDeterministicAndDoesNotAdvanceSequence()
    {
        SequenceState sequence = Fake.Create().Random().SequenceFromStartAndIncrement(10L, 3L);

        Assert.That(sequence.Page(2L, 4), Is.EqualTo(new long[] { 34L, 37L, 40L, 43L }));
        Assert.That(sequence.Page(0L, 4), Is.EqualTo(new long[] { 10L, 13L, 16L, 19L }));
        Assert.That(sequence.Next(), Is.EqualTo(10L));
    }

    [Test]
    public void CyclicPagesUseAbsolutePageOffsets()
    {
        SequenceState sequence = Fake.Create().Random().Cycle(5L, 2L, 20L);

        Assert.That(sequence.Page(1000000000L, 3), Is.EqualTo(new long[] { 5L, 7L, 9L }));
        Assert.That(sequence.Page(1000000001L, 3), Is.EqualTo(new long[] { 11L, 13L, 15L }));
        Assert.That(sequence.Next(), Is.EqualTo(5L));
    }

    [Test]
    public void PageReportsFiniteExhaustionAndUnboundedOverflow()
    {
        SequenceState finite = Fake.Create().Random().SequenceFromStartAndIncrementAndBoundaryExclusive(0L, 1L, 5L);
        SequenceState unbounded = Fake.Create().Random().SequenceFromStartAndIncrement(0L, long.MaxValue);

        Assert.That(Assert.Throws<FakeException>(() => finite.Page(1L, 3)).Message, Does.Contain("exclusive boundary"));
        Assert.That(Assert.Throws<FakeException>(() => unbounded.Page(long.MaxValue, 2)).Message, Does.Contain("overflowed"));
    }

    [Test]
    public void SequenceReportsPositiveAndNegativeLongOverflowWithoutAdvancing()
    {
        SequenceState positive = Fake.Create().Random().SequenceFromStartAndIncrement(long.MaxValue, 1L);
        SequenceState negative = Fake.Create().Random().SequenceFromStartAndIncrement(long.MinValue, -1L);

        Assert.That(positive.Next(), Is.EqualTo(long.MaxValue));
        Assert.That(negative.Next(), Is.EqualTo(long.MinValue));
        Assert.That(Assert.Throws<FakeException>(() => positive.Next()).Message, Is.EqualTo("Sequence value overflowed at position 1."));
        Assert.That(Assert.Throws<FakeException>(() => negative.Next()).Message, Is.EqualTo("Sequence value overflowed at position 1."));
    }

    [Test]
    public void FailedRepeatAndFillAreAtomic()
    {
        SequenceState sequence = Fake.Create().Random().SequenceFromStartAndIncrementAndBoundaryExclusive(1L, 1L, 4L);
        long[] destination = new long[] { 90L, 91L, 92L, 93L };

        Assert.Throws<FakeException>(() => sequence.Repeat(4));
        Assert.Throws<FakeException>(() => sequence.Fill(destination));

        Assert.That(destination, Is.EqualTo(new long[] { 90L, 91L, 92L, 93L }));
        Assert.That(sequence.Repeat(3), Is.EqualTo(new long[] { 1L, 2L, 3L }));
    }

    [Test]
    public void EmptyOperationsDoNotAdvanceOrRequireRemainingValues()
    {
        SequenceState sequence = Fake.Create().Random().SequenceFromStartAndIncrementAndBoundaryExclusive(1L, 1L, 2L);
        Assert.That(sequence.Next(), Is.EqualTo(1L));

        Assert.That(sequence.Repeat(0), Is.Empty);
        Assert.DoesNotThrow(() => sequence.Fill(Array.Empty<long>()));
        Assert.That(sequence.Page(long.MaxValue, 0), Is.Empty);
        Assert.Throws<FakeException>(() => sequence.Next());
    }

    [Test]
    public void SequenceFactoriesRejectInvalidConfigurations()
    {
        FakeRandom random = Fake.Create().Random();

        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => random.SequenceFromStartAndIncrement(0L, 0L)).ParamName, Is.EqualTo("increment"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => random.SequenceFromStartAndIncrementAndBoundaryExclusive(5L, 1L, 5L)).ParamName, Is.EqualTo("boundaryExclusive"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => random.SequenceFromStartAndIncrementAndBoundaryExclusive(5L, -1L, 6L)).ParamName, Is.EqualTo("boundaryExclusive"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => random.Cycle(5L, 1L, 4L)).ParamName, Is.EqualTo("boundaryExclusive"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => random.Cycle(5L, 0L, 10L)).ParamName, Is.EqualTo("increment"));
    }

    [Test]
    public void SequenceOperationsValidateArgumentsAndConfiguredLimits()
    {
        FakeOptions options = new FakeOptions();
        options.MaximumCount = 2;
        SequenceState sequence = Fake.Create(options).Random().Sequence();

        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => sequence.Repeat(-1)).ParamName, Is.EqualTo("count"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => sequence.Repeat(3)).ParamName, Is.EqualTo("count"));
        Assert.That(Assert.Throws<ArgumentNullException>(() => sequence.Fill(null)).ParamName, Is.EqualTo("destination"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => sequence.Fill(new long[3])).ParamName, Is.EqualTo("destination"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => sequence.Page(-1L, 1)).ParamName, Is.EqualTo("pageIndex"));
        Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => sequence.Page(0L, 3)).ParamName, Is.EqualTo("pageSize"));
    }

    [Test]
    public void SequenceStatesAreIndependentAndDoNotConsumeRandomEntropy()
    {
        FakeRandom random = Fake.Create(502).Random();
        FakeState before = random.Snapshot();
        SequenceState first = random.SequenceFromStartAndIncrement(2L, 5L);
        SequenceState second = random.SequenceFromStartAndIncrement(2L, 5L);

        Assert.That(first.Repeat(3), Is.EqualTo(second.Repeat(3)));
        Assert.That(first.Next(), Is.EqualTo(17L));
        Assert.That(second.Next(), Is.EqualTo(17L));
        AssertStateEqual(random.Snapshot(), before);
    }

    [Test]
    public void ConcurrentNextCallsProduceEachPositionExactlyOnce()
    {
        SequenceState sequence = Fake.Create().Random().Sequence();
        ConcurrentBag<long> values = new ConcurrentBag<long>();

        Parallel.For(0, 1000, index => values.Add(sequence.Next()));

        Assert.That(values.Order(), Is.EqualTo(Enumerable.Range(0, 1000).Select(value => (long)value)));
    }

    static void AssertStateEqual(FakeState actual, FakeState expected)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.Algorithm, Is.EqualTo(expected.Algorithm));
            Assert.That(actual.InitialSeed, Is.EqualTo(expected.InitialSeed));
            Assert.That(actual.State0, Is.EqualTo(expected.State0));
            Assert.That(actual.State1, Is.EqualTo(expected.State1));
            Assert.That(actual.State2, Is.EqualTo(expected.State2));
            Assert.That(actual.State3, Is.EqualTo(expected.State3));
        });
    }
}
