
namespace UniversalHtml.Tests;

[TestFixture]
public class CharacterBufferTests
{
    [Test]
    public void AppendsCharactersSpansRepetitionsAndScalars()
    {
        CharacterBuffer buffer = CharacterBuffer.Create(0, 64);

        Assert.That(buffer.TryAppend('a'), Is.True);
        Assert.That(buffer.TryAppendString("bc"), Is.True);
        Assert.That(buffer.TryAppendSpan("de".AsSpan()), Is.True);
        Assert.That(buffer.TryAppendRepeated('!', 2), Is.True);
        Assert.That(buffer.TryAppendScalar(0x1F600), Is.True);
        Assert.That(buffer.TryAppendScalar(-1), Is.True);

        Assert.That(buffer.GetString(), Is.EqualTo("abcde!!😀\uFFFD"));
        Assert.That(buffer.GetLength(), Is.EqualTo(10));
        Assert.That(buffer.GetCapacity(), Is.GreaterThanOrEqualTo(buffer.GetLength()));
        Assert.That(buffer.GetSpan().SequenceEqual("abcde!!😀\uFFFD".AsSpan()), Is.True);
    }

    [Test]
    public void SpanAppendCopiesCallerOwnedData()
    {
        char[] source = ['a', 'b', 'c'];
        CharacterBuffer buffer = CharacterBuffer.Create(1, 8);

        Assert.That(buffer.TryAppendSpan(source.AsSpan()), Is.True);
        source[0] = 'x';

        Assert.That(buffer.GetString(), Is.EqualTo("abc"));
        char[] snapshot = buffer.GetCharacters();
        snapshot[1] = 'x';
        Assert.That(buffer.GetString(), Is.EqualTo("abc"));
    }

    [Test]
    public void LocalLimitFailureIsAtomicAndSticky()
    {
        CharacterBuffer buffer = CharacterBuffer.Create(0, 4);

        Assert.That(buffer.TryAppendString("abc"), Is.True);
        int capacity = buffer.GetCapacity();
        Assert.That(buffer.TryAppendString("de"), Is.False);

        Assert.That(buffer.GetString(), Is.EqualTo("abc"));
        Assert.That(buffer.GetLength(), Is.EqualTo(3));
        Assert.That(buffer.GetCapacity(), Is.EqualTo(capacity));
        Assert.That(buffer.WasLimitExceeded(), Is.True);
        Assert.That(buffer.TryAppend('d'), Is.True);
        Assert.That(buffer.GetString(), Is.EqualTo("abcd"));
    }

    [Test]
    public void SupplementaryScalarAppendIsAtomic()
    {
        CharacterBuffer buffer = CharacterBuffer.Create(0, 1);

        Assert.That(buffer.TryAppendScalar(0x1F600), Is.False);
        Assert.That(buffer.GetLength(), Is.EqualTo(0));
        Assert.That(buffer.TryAppendScalar('a'), Is.True);
        Assert.That(buffer.GetString(), Is.EqualTo("a"));
    }

    [Test]
    public void OutputBuffersShareBudgetAndDoNotRefundTruncation()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumOutputCharacters(5);
        HtmlBudget budget = new HtmlBudget(limits, CancellationToken.None);
        CharacterBuffer first = CharacterBuffer.CreateOutput(budget, 0);
        CharacterBuffer second = CharacterBuffer.CreateOutput(budget, 0);

        Assert.That(first.TryAppendString("abc"), Is.True);
        first.Truncate(1);
        Assert.That(second.TryAppendString("de"), Is.True);
        Assert.That(first.TryAppend('x'), Is.False);

        Assert.That(first.GetString(), Is.EqualTo("a"));
        Assert.That(second.GetString(), Is.EqualTo("de"));
        Assert.That(budget.GetUsed(HtmlLimitKind.OutputCharacters), Is.EqualTo(5));
        Assert.That(budget.GetFirstExceededLimit(), Is.EqualTo(HtmlLimitKind.OutputCharacters));
    }

    [Test]
    public void ClearDoesNotRefundOutputBudget()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumOutputCharacters(3);
        HtmlBudget budget = new HtmlBudget(limits, CancellationToken.None);
        CharacterBuffer buffer = CharacterBuffer.CreateOutput(budget, 0);

        Assert.That(buffer.TryAppendString("abc"), Is.True);
        buffer.Clear();
        Assert.That(buffer.TryAppend('d'), Is.False);
        Assert.That(buffer.GetLength(), Is.EqualTo(0));
    }

    [Test]
    public void OutputAppendChecksCancellationBeforeMutation()
    {
        CancellationTokenSource source = new CancellationTokenSource();
        HtmlBudget budget = new HtmlBudget(HtmlLimits.CreateDefault(), source.Token);
        CharacterBuffer buffer = CharacterBuffer.CreateOutput(budget, 0);
        source.Cancel();

        Assert.Throws<OperationCanceledException>(() => buffer.TryAppendString("value"));
        Assert.That(buffer.GetLength(), Is.EqualTo(0));
    }

    [Test]
    public void CharacterBufferValidatesArgumentsAndIndexes()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CharacterBuffer.Create(-1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => CharacterBuffer.Create(0, -1));
        Assert.Throws<ArgumentException>(() => CharacterBuffer.Create(2, 1));

        CharacterBuffer buffer = CharacterBuffer.Create(0, 3);
        Assert.Throws<ArgumentNullException>(() => buffer.TryAppendString((string)null));
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.TryAppendRepeated('x', -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.GetCharacter(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer.Truncate(1));
    }

    [Test]
    public void CapacityCalculationHandlesUpperIntegerBoundary()
    {
        Assert.That(BufferCapacity.GetNext(0, 1, 8), Is.EqualTo(8));
        Assert.That(BufferCapacity.GetNext(8, 9, 32), Is.EqualTo(16));
        Assert.That(BufferCapacity.GetNext(1_500_000_000, 1_600_000_000, int.MaxValue), Is.EqualTo(int.MaxValue));
        Assert.Throws<ArgumentException>(() => BufferCapacity.GetNext(8, 9, 8));
    }
}
