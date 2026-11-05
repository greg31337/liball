
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlInputTests
{
    [Test]
    public void NormalizesNewlinesAndNullsWithOriginalSourcePositions()
    {
        HtmlBudget budget = new HtmlBudget(HtmlLimits.CreateDefault(), CancellationToken.None);
        HtmlDiagnosticCollector diagnostics = new HtmlDiagnosticCollector(budget);
        HtmlInput input = HtmlInput.Create(budget, diagnostics);

        Assert.That(input.TryAppend("a\r\nb\rc\nd\0e"), Is.True);
        input.Complete();

        Assert.Multiple(() =>
        {
            Assert.That(input.GetString(), Is.EqualTo("a\nb\nc\nd\uFFFDe"));
            Assert.That(input.GetLength(), Is.EqualTo(9));
            Assert.That(input.GetOriginalCharacterLength(), Is.EqualTo(10));
            AssertPosition(input.GetSourcePositionAt(0), 0, 0, 1, 1);
            AssertPosition(input.GetSourcePositionAt(2), 2, 3, 2, 1);
            AssertPosition(input.GetSourcePositionAt(4), 4, 5, 3, 1);
            AssertPosition(input.GetSourcePositionAt(9), 9, 10, 4, 4);
        });

        HtmlDiagnostic[] found = diagnostics.GetDiagnostics();
        Assert.That(found, Has.Length.EqualTo(1));
        Assert.That(found[0].GetCode(), Is.EqualTo(HtmlDiagnosticCode.InputNullCharacter));
        Assert.That(found[0].GetSeverity(), Is.EqualTo(HtmlDiagnosticSeverity.Warning));
        Assert.That(found[0].GetSpan().GetNormalizedCharacterLength(), Is.EqualTo(1));
        Assert.That(found[0].GetSpan().GetCharacterLength(), Is.EqualTo(1));
        AssertPosition(found[0].GetSpan().GetStart(), 7, 8, 4, 2);
    }

    [Test]
    public void ResolvesCrLfAndSurrogatePairsAcrossArbitraryChunks()
    {
        HtmlBudget budget = new HtmlBudget(HtmlLimits.CreateDefault(), CancellationToken.None);
        HtmlDiagnosticCollector diagnostics = new HtmlDiagnosticCollector(budget);
        HtmlInput input = HtmlInput.Create(budget, diagnostics);
        char[] finalChunk = ['\uDE00', 'C'];

        Assert.That(input.TryAppend("A\r"), Is.True);
        Assert.That(input.TryAppendSpan(ReadOnlySpan<char>.Empty), Is.True);
        Assert.That(input.Peek(), Is.EqualTo('A'));
        Assert.That(input.PeekAhead(1), Is.EqualTo(-1));
        Assert.That(input.IsComplete(), Is.False);
        Assert.That(input.TryAppend("\nB\uD83D"), Is.True);
        Assert.That(input.TryAppendSpan(finalChunk.AsSpan()), Is.True);
        finalChunk[1] = 'X';
        input.Complete();

        Assert.Multiple(() =>
        {
            Assert.That(input.GetString(), Is.EqualTo("A\nB\U0001F600C"));
            Assert.That(input.GetLength(), Is.EqualTo(6));
            Assert.That(input.GetOriginalCharacterLength(), Is.EqualTo(7));
            Assert.That(input.PeekAhead(4), Is.EqualTo('\uDE00'));
            AssertPosition(input.GetSourcePositionAt(2), 2, 3, 2, 1);
            AssertPosition(input.GetSourcePositionAt(3), 3, 4, 2, 2);
            AssertPosition(input.GetSourcePositionAt(5), 5, 6, 2, 4);
            Assert.That(diagnostics.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void LookaheadAndReconsumeContinueAcrossAppendedChunks()
    {
        HtmlBudget budget = new HtmlBudget(HtmlLimits.CreateDefault(), CancellationToken.None);
        HtmlDiagnosticCollector diagnostics = new HtmlDiagnosticCollector(budget);
        HtmlInput input = HtmlInput.Create(budget, diagnostics);

        input.TryAppend("ab");
        Assert.That(input.Read(), Is.EqualTo('a'));
        Assert.That(input.PeekAhead(1), Is.EqualTo(-1));
        input.TryAppend("cd");
        Assert.That(input.PeekAhead(2), Is.EqualTo('d'));
        Assert.That(input.Read(), Is.EqualTo('b'));
        input.Reconsume();
        Assert.That(input.GetPosition(), Is.EqualTo(1));
        Assert.That(input.GetSourcePosition().GetCharacterOffset(), Is.EqualTo(1));
        Assert.That(input.Read(), Is.EqualTo('b'));
        Assert.That(input.Read(), Is.EqualTo('c'));
        Assert.That(input.Read(), Is.EqualTo('d'));
        Assert.That(input.Read(), Is.EqualTo(-1));
        Assert.That(input.IsEnd(), Is.False);

        input.Complete();
        Assert.That(input.IsEnd(), Is.True);
    }

    [Test]
    public void CompactionDiscardsConsumedCharactersAndPreservesAbsoluteLocations()
    {
        HtmlBudget budget = new HtmlBudget(HtmlLimits.CreateDefault(), CancellationToken.None);
        HtmlDiagnosticCollector diagnostics = new HtmlDiagnosticCollector(budget);
        HtmlInput input = HtmlInput.Create(budget, diagnostics);

        input.TryAppend("abc");
        Assert.That(input.Read(), Is.EqualTo('a'));
        Assert.That(input.Read(), Is.EqualTo('b'));
        input.Reconsume();
        input.Compact();

        Assert.Multiple(() =>
        {
            Assert.That(input.GetBufferStart(), Is.EqualTo(1));
            Assert.That(input.GetBufferedLength(), Is.EqualTo(2));
            Assert.That(input.GetString(), Is.EqualTo("bc"));
            Assert.That(input.GetPosition(), Is.EqualTo(1));
            Assert.That(input.GetSourcePosition().GetNormalizedCharacterOffset(), Is.EqualTo(1));
        });
        Assert.Throws<InvalidOperationException>(input.Reconsume);

        Assert.That(input.Read(), Is.EqualTo('b'));
        input.Compact();
        input.TryAppend("de");
        Assert.That(input.GetString(), Is.EqualTo("cde"));
        Assert.That(input.GetSourcePositionAt(4).GetCharacterOffset(), Is.EqualTo(4));
    }

    [Test]
    public void ReplacesUnpairedSurrogatesAndFlushesPendingInputAtCompletion()
    {
        HtmlBudget budget = new HtmlBudget(HtmlLimits.CreateDefault(), CancellationToken.None);
        HtmlDiagnosticCollector diagnostics = new HtmlDiagnosticCollector(budget);
        HtmlInput input = HtmlInput.Create(budget, diagnostics);

        input.TryAppend("\uD800");
        input.TryAppendSpan(ReadOnlySpan<char>.Empty);
        Assert.That(input.GetString(), Is.Empty);
        input.TryAppend("x\uDC00\r");
        Assert.That(input.GetString(), Is.EqualTo("\uFFFDx\uFFFD"));
        input.Complete();

        Assert.That(input.GetString(), Is.EqualTo("\uFFFDx\uFFFD\n"));
        Assert.That(diagnostics.GetDiagnostics().Select(value => value.GetCode()), Is.EqualTo(new[]
        {
            HtmlDiagnosticCode.InputInvalidScalar,
            HtmlDiagnosticCode.InputInvalidScalar
        }));
        Assert.That(input.GetSourceSpan(0, 1).GetCharacterLength(), Is.EqualTo(1));
        Assert.That(input.GetSourceSpan(3, 4).GetCharacterLength(), Is.EqualTo(1));
    }

    [Test]
    public void InputCharacterLimitCountsOriginalCharactersAndRejectsAtomically()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumInputCharacters(3);
        HtmlBudget budget = new HtmlBudget(limits, CancellationToken.None);
        HtmlDiagnosticCollector diagnostics = new HtmlDiagnosticCollector(budget);
        HtmlInput input = HtmlInput.Create(budget, diagnostics);

        Assert.That(input.TryAppend("\r\n"), Is.True);
        Assert.That(input.TryAppend("ab"), Is.False);
        Assert.That(input.GetString(), Is.EqualTo("\n"));
        Assert.That(input.GetOriginalCharacterLength(), Is.EqualTo(2));
        Assert.That(input.TryAppend("x"), Is.True);
        input.Complete();

        Assert.That(input.GetString(), Is.EqualTo("\nx"));
        Assert.That(budget.GetUsed(HtmlLimitKind.InputCharacters), Is.EqualTo(3));
        Assert.That(budget.GetFirstExceededLimit(), Is.EqualTo(HtmlLimitKind.InputCharacters));
    }

    [Test]
    public void CancellationIsCheckedBeforeFurtherInputOrMovement()
    {
        CancellationTokenSource source = new CancellationTokenSource();
        HtmlBudget budget = new HtmlBudget(HtmlLimits.CreateDefault(), source.Token);
        HtmlDiagnosticCollector diagnostics = new HtmlDiagnosticCollector(budget);
        HtmlInput input = HtmlInput.Create(budget, diagnostics);
        input.TryAppend("a");
        source.Cancel();

        Assert.Throws<OperationCanceledException>(() => input.TryAppend("b"));
        Assert.Throws<OperationCanceledException>(() => input.Peek());
        Assert.Throws<OperationCanceledException>(() => input.Read());
        Assert.Throws<OperationCanceledException>(input.Reconsume);
        Assert.Throws<OperationCanceledException>(input.Complete);
        Assert.That(input.GetString(), Is.EqualTo("a"));
        Assert.That(input.GetPosition(), Is.EqualTo(0));
    }

    [Test]
    public void ValidatesInvalidOperationsAndCompletionIsIdempotent()
    {
        HtmlBudget budget = new HtmlBudget(HtmlLimits.CreateDefault(), CancellationToken.None);
        HtmlDiagnosticCollector diagnostics = new HtmlDiagnosticCollector(budget);
        HtmlInput input = HtmlInput.Create(budget, diagnostics);

        Assert.Throws<ArgumentNullException>(() => input.TryAppend((string)null));
        Assert.Throws<ArgumentOutOfRangeException>(() => input.PeekAhead(-1));
        Assert.Throws<InvalidOperationException>(input.Reconsume);
        input.Complete();
        Assert.DoesNotThrow(input.Complete);
        Assert.Throws<InvalidOperationException>(() => input.TryAppend("x"));
        Assert.Throws<ArgumentOutOfRangeException>(() => input.GetSourcePositionAt(1));
        Assert.Throws<ArgumentNullException>(() => HtmlInput.Create(null, diagnostics));
        Assert.Throws<ArgumentNullException>(() => HtmlInput.Create(budget, null));
    }

    static void AssertPosition(HtmlSourcePosition position, int normalizedOffset, int characterOffset, int line, int column)
    {
        Assert.That(position.GetNormalizedCharacterOffset(), Is.EqualTo(normalizedOffset));
        Assert.That(position.GetCharacterOffset(), Is.EqualTo(characterOffset));
        Assert.That(position.GetLine(), Is.EqualTo(line));
        Assert.That(position.GetColumn(), Is.EqualTo(column));
    }
}
