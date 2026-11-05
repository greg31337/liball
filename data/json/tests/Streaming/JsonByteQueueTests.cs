namespace json.Tests;

public class JsonByteQueueTests
{
    [Test]
    public void ZeroLimitUsesSafeDefaultAndNegativeLimitIsRejected()
    {
        JsonByteQueue queue = new JsonByteQueue(0);

        JsonLimitException exception = Assert.Throws<JsonLimitException>(() => new JsonByteQueue(-1))
            ?? throw new InvalidOperationException("Expected JsonLimitException.");

        Assert.Multiple(() =>
        {
            Assert.That(queue.GetMaximumBufferedBytes(), Is.EqualTo(JsonDefaults.MaximumInputBufferBytes));
            Assert.That(queue.GetCapacity(), Is.EqualTo(JsonDefaults.StreamBufferBytes));
            Assert.That(queue.GetBufferedCount(), Is.Zero);
            Assert.That(exception.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InvalidOptions));
        });
    }

    [Test]
    public void EnqueueTakesOwnershipAndPreservesChunkOrder()
    {
        JsonByteQueue queue = new JsonByteQueue(32);
        byte[] first = { 1, 2, 3 };
        byte[] second = { 4, 5 };

        queue.EnqueueBytes(first);
        first[0] = 99;
        queue.EnqueueBytes(second);
        second[0] = 88;

        Assert.Multiple(() =>
        {
            Assert.That(queue.GetBufferedSpan().ToArray(), Is.EqualTo(new byte[] { 1, 2, 3, 4, 5 }));
            Assert.That(queue.CopyBytes(1, 3), Is.EqualTo(new byte[] { 2, 3, 4 }));
            Assert.That(queue.GetByteAt(0), Is.EqualTo(1));
            Assert.That(queue.GetByteAt(4), Is.EqualTo(5));
            Assert.That(queue.GetBufferedCount(), Is.EqualTo(5));
            Assert.That(queue.GetAcceptedByteCount(), Is.EqualTo(5));
            Assert.That(queue.GetConsumedByteCount(), Is.Zero);
        });
    }

    [Test]
    public void TokensCanGrowBeyondTheInitialBuffer()
    {
        JsonByteQueue queue = new JsonByteQueue(50_000);
        byte[] first = Enumerable.Repeat((byte)0x41, 10_000).ToArray();
        byte[] second = Enumerable.Repeat((byte)0x42, 12_000).ToArray();

        queue.EnqueueBytes(first);
        queue.EnqueueBytes(second);

        Assert.Multiple(() =>
        {
            Assert.That(queue.GetCapacity(), Is.GreaterThanOrEqualTo(22_000));
            Assert.That(queue.GetCapacity(), Is.LessThanOrEqualTo(50_000));
            Assert.That(queue.GetBufferedCount(), Is.EqualTo(22_000));
            Assert.That(queue.GetByteAt(9_999), Is.EqualTo(0x41));
            Assert.That(queue.GetByteAt(10_000), Is.EqualTo(0x42));
            Assert.That(queue.GetByteAt(21_999), Is.EqualTo(0x42));
        });
    }

    [Test]
    public void ConsumedHeadSpaceIsCompactedBeforeAppending()
    {
        JsonByteQueue queue = new JsonByteQueue(8);
        queue.EnqueueBytes(new byte[] { 1, 2, 3, 4, 5, 6 });
        queue.ConsumeBytes(4);

        queue.EnqueueBytes(new byte[] { 7, 8, 9, 10 });

        Assert.Multiple(() =>
        {
            Assert.That(queue.GetCapacity(), Is.EqualTo(8));
            Assert.That(queue.GetBufferedSpan().ToArray(), Is.EqualTo(new byte[] { 5, 6, 7, 8, 9, 10 }));
            Assert.That(queue.GetAcceptedByteCount(), Is.EqualTo(10));
            Assert.That(queue.GetConsumedByteCount(), Is.EqualTo(4));
        });

        queue.ConsumeBytes(1);
        queue.CompactBuffer();

        Assert.That(queue.GetBufferedSpan().ToArray(), Is.EqualTo(new byte[] { 6, 7, 8, 9, 10 }));
    }

    [Test]
    public void BufferLimitFailureIsAtomic()
    {
        JsonByteQueue queue = new JsonByteQueue(5);
        queue.EnqueueBytes(new byte[] { 1, 2, 3, 4 });

        JsonLimitException exception = Assert.Throws<JsonLimitException>(() => queue.EnqueueBytes(new byte[] { 5, 6 }))
            ?? throw new InvalidOperationException("Expected JsonLimitException.");

        Assert.Multiple(() =>
        {
            Assert.That(exception.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InputBufferLimitExceeded));
            Assert.That(queue.GetBufferedSpan().ToArray(), Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(queue.GetBufferedCount(), Is.EqualTo(4));
            Assert.That(queue.GetAcceptedByteCount(), Is.EqualTo(4));
            Assert.That(queue.GetConsumedByteCount(), Is.Zero);
        });
    }

    [Test]
    public void RangeAndConsumptionBoundariesAreChecked()
    {
        JsonByteQueue queue = new JsonByteQueue(8);
        queue.EnqueueBytes(new byte[] { 1, 2, 3 });
        queue.EnqueueBytes(Array.Empty<byte>());

        Assert.Multiple(() =>
        {
            Assert.That(queue.CopyBytes(3, 0), Is.Empty);
            Assert.That(() => queue.ConsumeBytes(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => queue.ConsumeBytes(4), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => queue.GetByteAt(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => queue.GetByteAt(3), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => queue.CopyBytes(-1, 0), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => queue.CopyBytes(4, 0), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => queue.CopyBytes(2, 2), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(queue.GetAcceptedByteCount(), Is.EqualTo(3));
        });
    }

    [Test]
    public void ResetClearsBufferedStateAndAccountingForReuse()
    {
        JsonByteQueue queue = new JsonByteQueue(16);
        queue.EnqueueBytes(new byte[] { 1, 2, 3, 4 });
        queue.ConsumeBytes(2);
        int capacity = queue.GetCapacity();

        queue.ResetQueue();

        Assert.Multiple(() =>
        {
            Assert.That(queue.GetBufferedCount(), Is.Zero);
            Assert.That(queue.GetBufferedSpan().IsEmpty, Is.True);
            Assert.That(queue.GetAcceptedByteCount(), Is.Zero);
            Assert.That(queue.GetConsumedByteCount(), Is.Zero);
            Assert.That(queue.GetCapacity(), Is.EqualTo(capacity));
        });

        queue.EnqueueBytes(new byte[] { 9, 10 });

        Assert.Multiple(() =>
        {
            Assert.That(queue.GetBufferedSpan().ToArray(), Is.EqualTo(new byte[] { 9, 10 }));
            Assert.That(queue.GetAcceptedByteCount(), Is.EqualTo(2));
        });
    }
}
