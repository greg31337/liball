using System.Threading;
using email;

namespace email.Tests;

public class EmailProgressTests
{
    [Test]
    public void CountersStartAtZeroAndAccumulateIndependently()
    {
        EmailProgress progress = new EmailProgressOperations().CreateWith();

        progress.AddSourceBytesRead(11);
        progress.AddOutputBytesWritten(12);
        progress.AddDecodedBytesProduced(13);
        progress.AddHeaderCount(14);
        progress.AddEntityCount(15);
        progress.AddOperationCount(16);

        Assert.That(progress.GetSourceBytesRead(), Is.EqualTo(11));
        Assert.That(progress.GetOutputBytesWritten(), Is.EqualTo(12));
        Assert.That(progress.GetDecodedBytesProduced(), Is.EqualTo(13));
        Assert.That(progress.GetHeaderCount(), Is.EqualTo(14));
        Assert.That(progress.GetEntityCount(), Is.EqualTo(15));
        Assert.That(progress.GetOperationCount(), Is.EqualTo(16));
    }

    [Test]
    public void SnapshotIsConsistentAndIndependentOfLaterUpdates()
    {
        EmailProgress progress = CreateProgressWithValues(1, 2, 3, 4, 5, 6);

        EmailProgressSnapshot snapshot = progress.GetSnapshot();
        progress.AddSourceBytesRead(10);
        progress.Reset();

        AssertSnapshot(snapshot, 1, 2, 3, 4, 5, 6);
        AssertSnapshot(progress.GetSnapshot(), 0, 0, 0, 0, 0, 0);
    }

    [Test]
    public void ProgressCanResumeFromSnapshotWithoutSharingState()
    {
        EmailProgressSnapshot initial = new EmailProgressSnapshot(1, 2, 3, 4, 5, 6);
        EmailProgress progress = new EmailProgressOperations().CreateWithInitialValues(initial);

        progress.AddEntityCount(10);

        AssertSnapshot(initial, 1, 2, 3, 4, 5, 6);
        AssertSnapshot(progress.GetSnapshot(), 1, 2, 3, 4, 15, 6);
        Assert.Throws<ArgumentNullException>(() => new EmailProgressOperations().CreateWithInitialValues(null!));
    }

    [Test]
    public void NegativeUpdatesAreRejectedWithoutChangingCounters()
    {
        EmailProgress progress = CreateProgressWithValues(1, 2, 3, 4, 5, 6);
        TestDelegate[] invalidUpdates = new TestDelegate[]
        {
            () => progress.AddSourceBytesRead(-1),
            () => progress.AddOutputBytesWritten(-1),
            () => progress.AddDecodedBytesProduced(-1),
            () => progress.AddHeaderCount(-1),
            () => progress.AddEntityCount(-1),
            () => progress.AddOperationCount(-1)
        };

        int index = 0;
        while (index < invalidUpdates.Length)
        {
            Assert.Throws<ArgumentOutOfRangeException>(invalidUpdates[index]);
            index++;
        }

        AssertSnapshot(progress.GetSnapshot(), 1, 2, 3, 4, 5, 6);
    }

    [Test]
    public void OverflowIsRejectedWithoutChangingCounters()
    {
        EmailProgress progress = new EmailProgressOperations().CreateWithInitialValues(new EmailProgressSnapshot(
            long.MaxValue,
            long.MaxValue,
            long.MaxValue,
            long.MaxValue,
            long.MaxValue,
            long.MaxValue));
        TestDelegate[] overflowingUpdates = new TestDelegate[]
        {
            () => progress.AddSourceBytesRead(1),
            () => progress.AddOutputBytesWritten(1),
            () => progress.AddDecodedBytesProduced(1),
            () => progress.AddHeaderCount(1),
            () => progress.AddEntityCount(1),
            () => progress.AddOperationCount(1)
        };

        int index = 0;
        while (index < overflowingUpdates.Length)
        {
            Assert.Throws<OverflowException>(overflowingUpdates[index]);
            index++;
        }

        AssertSnapshot(
            progress.GetSnapshot(),
            long.MaxValue,
            long.MaxValue,
            long.MaxValue,
            long.MaxValue,
            long.MaxValue,
            long.MaxValue);
    }

    [Test]
    public void SnapshotRejectsNegativeInitialValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailProgressSnapshot(-1, 0, 0, 0, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailProgressSnapshot(0, -1, 0, 0, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailProgressSnapshot(0, 0, -1, 0, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailProgressSnapshot(0, 0, 0, -1, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailProgressSnapshot(0, 0, 0, 0, -1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmailProgressSnapshot(0, 0, 0, 0, 0, -1));
    }

    [Test]
    public void ConcurrentUpdatesAreNotLost()
    {
        EmailProgress progress = new EmailProgressOperations().CreateWith();
        Thread[] workers = new Thread[4];
        int workerIndex = 0;
        while (workerIndex < workers.Length)
        {
            workers[workerIndex] = new Thread(() =>
            {
                int iteration = 0;
                while (iteration < 2000)
                {
                    progress.AddSourceBytesRead(1);
                    progress.AddOperationCount(1);
                    iteration++;
                }
            });
            workers[workerIndex].Start();
            workerIndex++;
        }

        workerIndex = 0;
        while (workerIndex < workers.Length)
        {
            workers[workerIndex].Join();
            workerIndex++;
        }

        Assert.That(progress.GetSourceBytesRead(), Is.EqualTo(8000));
        Assert.That(progress.GetOperationCount(), Is.EqualTo(8000));
    }

    static EmailProgress CreateProgressWithValues(long source, long output, long decoded, long headers, long entities, long operations)
    {
        return new EmailProgressOperations().CreateWithInitialValues(new EmailProgressSnapshot(source, output, decoded, headers, entities, operations));
    }

    static void AssertSnapshot(EmailProgressSnapshot snapshot, long source, long output, long decoded, long headers, long entities, long operations)
    {
        Assert.That(snapshot.GetSourceBytesRead(), Is.EqualTo(source));
        Assert.That(snapshot.GetOutputBytesWritten(), Is.EqualTo(output));
        Assert.That(snapshot.GetDecodedBytesProduced(), Is.EqualTo(decoded));
        Assert.That(snapshot.GetHeaderCount(), Is.EqualTo(headers));
        Assert.That(snapshot.GetEntityCount(), Is.EqualTo(entities));
        Assert.That(snapshot.GetOperationCount(), Is.EqualTo(operations));
    }
}
