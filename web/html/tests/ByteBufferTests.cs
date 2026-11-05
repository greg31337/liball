
namespace UniversalHtml.Tests;

[TestFixture]
public class ByteBufferTests
{
    [Test]
    public void AppendsOwnedBytesAndGrowsWithinLimit()
    {
        byte[] source = [1, 2, 3, 4];
        ByteBuffer buffer = ByteBuffer.Create(1, 8);

        Assert.That(buffer.TryAppend((byte)0), Is.True);
        Assert.That(buffer.TryAppendSpan(source.AsSpan()), Is.True);
        source[0] = 9;

        Assert.That(buffer.GetBytes(), Is.EqualTo(new byte[] { 0, 1, 2, 3, 4 }));
        Assert.That(buffer.GetSpan().SequenceEqual(new byte[] { 0, 1, 2, 3, 4 }), Is.True);
        Assert.That(buffer.GetCapacity(), Is.GreaterThanOrEqualTo(5));
    }

    [Test]
    public void ByteLimitFailureIsAtomic()
    {
        ByteBuffer buffer = ByteBuffer.Create(0, 3);

        Assert.That(buffer.TryAppendBytes(new byte[] { 1, 2 }), Is.True);
        Assert.That(buffer.TryAppendBytes(new byte[] { 3, 4 }), Is.False);

        Assert.That(buffer.GetBytes(), Is.EqualTo(new byte[] { 1, 2 }));
        Assert.That(buffer.WasLimitExceeded(), Is.True);
        Assert.That(buffer.TryAppend((byte)3), Is.True);
    }

    [Test]
    public void InputBuffersShareByteBudget()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumInputBytes(4);
        HtmlBudget budget = new HtmlBudget(limits, CancellationToken.None);
        ByteBuffer first = ByteBuffer.CreateInput(budget, 0);
        ByteBuffer second = ByteBuffer.CreateInput(budget, 0);

        Assert.That(first.TryAppendBytes(new byte[] { 1, 2, 3 }), Is.True);
        Assert.That(second.TryAppend((byte)4), Is.True);
        Assert.That(first.TryAppend((byte)5), Is.False);
        Assert.That(budget.GetUsed(HtmlLimitKind.InputBytes), Is.EqualTo(4));
        Assert.That(budget.GetFirstExceededLimit(), Is.EqualTo(HtmlLimitKind.InputBytes));
    }

    [Test]
    public void ByteBufferClearAndTruncatePreserveOwnershipRules()
    {
        ByteBuffer buffer = ByteBuffer.Create(0, 8);
        Assert.That(buffer.TryAppendBytes(new byte[] { 1, 2, 3 }), Is.True);
        buffer.Truncate(2);
        Assert.That(buffer.GetBytes(), Is.EqualTo(new byte[] { 1, 2 }));
        buffer.Clear();
        Assert.That(buffer.GetLength(), Is.EqualTo(0));

        Assert.Throws<ArgumentNullException>(() => buffer.TryAppendBytes((byte[])null));
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.GetByte(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.Truncate(1));
    }

    [Test]
    public void InputAppendChecksCancellationBeforeMutation()
    {
        CancellationTokenSource source = new CancellationTokenSource();
        HtmlBudget budget = new HtmlBudget(HtmlLimits.CreateDefault(), source.Token);
        ByteBuffer buffer = ByteBuffer.CreateInput(budget, 0);
        source.Cancel();

        Assert.Throws<OperationCanceledException>(() => buffer.TryAppend((byte)1));
        Assert.That(buffer.GetLength(), Is.EqualTo(0));
    }
}
