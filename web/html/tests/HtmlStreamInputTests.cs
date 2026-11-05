using System.Text;

namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlStreamInputTests
{
    [Test]
    public void ArbitraryReaderAndBufferChunkSizesProduceIdenticalNormalizedInput()
    {
        const string source = "A\r\nB\rC\0D\uD83D\uDE00E";
        const string expected = "A\nB\nC\uFFFDD\U0001F600E";
        int[] sizes = { 1, 2, 3, 5, 8 };

        foreach (int readerChunk in sizes)
        {
            foreach (int bufferSize in sizes)
            {
                ChunkedTextReader reader = new ChunkedTextReader(source, readerChunk);
                HtmlBudget budget = new HtmlBudget(HtmlLimits.CreateDefault(), CancellationToken.None);
                HtmlDiagnosticCollector diagnostics = new HtmlDiagnosticCollector(budget);
                HtmlTextReaderInput input = HtmlTextReaderInput.Create(reader, true, bufferSize, budget, diagnostics);

                string actual = ReadAll(input);

                Assert.That(actual, Is.EqualTo(expected), $"reader={readerChunk}, buffer={bufferSize}");
                Assert.That(input.GetString(), Is.EqualTo(expected));
                Assert.That(input.GetOriginalCharacterLength(), Is.EqualTo(source.Length));
                Assert.That(input.IsSourceComplete(), Is.True);
                Assert.That(input.IsEnd(), Is.True);
                Assert.That(input.WasLimitExceeded(), Is.False);
                Assert.That(diagnostics.GetDiagnostics().Select(value => value.GetCode()),
                    Is.EqualTo(new[] { HtmlDiagnosticCode.InputNullCharacter }));
                input.Close();
            }
        }
    }

    [Test]
    public void LookaheadAndReconsumeCrossReaderBoundariesWithoutChangingPosition()
    {
        ChunkedTextReader reader = new ChunkedTextReader("a\r\nbcd", 1);
        HtmlBudget budget = new HtmlBudget(HtmlLimits.CreateDefault(), CancellationToken.None);
        HtmlDiagnosticCollector diagnostics = new HtmlDiagnosticCollector(budget);
        HtmlTextReaderInput input = HtmlTextReaderInput.Create(reader, true, 1, budget, diagnostics);

        Assert.That(input.PeekAhead(3), Is.EqualTo('c'));
        Assert.That(input.GetPosition(), Is.EqualTo(0));
        Assert.That(input.Read(), Is.EqualTo('a'));
        input.Reconsume();
        Assert.That(input.Read(), Is.EqualTo('a'));
        Assert.That(ReadAll(input), Is.EqualTo("\nbcd"));
        AssertPosition(input.GetSourcePositionAt(2), 2, 3, 2, 1);
        Assert.That(input.GetSourceSpan(1, 2).GetCharacterLength(), Is.EqualTo(2));
        Assert.That(reader.GetReadCallCount(), Is.GreaterThan(1));
        input.Close();
    }

    [Test]
    public void ExactInputLimitDoesNotConsumeTheNextReaderCharacter()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumInputCharacters(3);
        HtmlBudget budget = new HtmlBudget(limits, CancellationToken.None);
        HtmlDiagnosticCollector diagnostics = new HtmlDiagnosticCollector(budget);
        ChunkedTextReader reader = new ChunkedTextReader("ab\r\nz", 8);
        HtmlTextReaderInput input = HtmlTextReaderInput.Create(reader, true, 8, budget, diagnostics);

        Assert.That(ReadAll(input), Is.EqualTo("ab\n"));

        Assert.Multiple(() =>
        {
            Assert.That(input.WasLimitExceeded(), Is.True);
            Assert.That(input.IsSourceComplete(), Is.True);
            Assert.That(input.GetOriginalCharacterLength(), Is.EqualTo(3));
            Assert.That(budget.GetUsed(HtmlLimitKind.InputCharacters), Is.EqualTo(3));
            Assert.That(budget.GetFirstExceededLimit(), Is.EqualTo(HtmlLimitKind.InputCharacters));
            Assert.That(reader.Peek(), Is.EqualTo('\n'));
        });

        HtmlDiagnostic[] found = diagnostics.GetDiagnostics();
        Assert.That(found, Has.Length.EqualTo(1));
        Assert.That(found[0].GetCode(), Is.EqualTo(HtmlDiagnosticCode.InputLimitExceeded));
        Assert.That(found[0].GetSeverity(), Is.EqualTo(HtmlDiagnosticSeverity.Error));
        Assert.That(found[0].GetSpan().GetNormalizedCharacterLength(), Is.EqualTo(0));
        Assert.That(found[0].GetSpan().GetStart().GetCharacterOffset(), Is.EqualTo(3));

        input.Close();
        Assert.That(reader.Read(), Is.EqualTo('\n'));
    }

    [Test]
    public void EndOfReaderFinalizesPendingCharacters()
    {
        ChunkedTextReader reader = new ChunkedTextReader("x\uD800", 1);
        HtmlBudget budget = new HtmlBudget(HtmlLimits.CreateDefault(), CancellationToken.None);
        HtmlDiagnosticCollector diagnostics = new HtmlDiagnosticCollector(budget);
        HtmlTextReaderInput input = HtmlTextReaderInput.Create(reader, true, 1, budget, diagnostics);

        Assert.That(ReadAll(input), Is.EqualTo("x\uFFFD"));
        Assert.That(diagnostics.GetDiagnostics().Select(value => value.GetCode()),
            Is.EqualTo(new[] { HtmlDiagnosticCode.InputInvalidScalar }));
        Assert.That(input.IsSourceComplete(), Is.True);
        input.Close();
    }

    [Test]
    public void CloseHonorsLeaveOpenAndIsIdempotent()
    {
        StringReader retainedReader = new StringReader("r");
        HtmlBudget retainedBudget = new HtmlBudget(HtmlLimits.CreateDefault(), CancellationToken.None);
        HtmlDiagnosticCollector retainedDiagnostics = new HtmlDiagnosticCollector(retainedBudget);
        HtmlTextReaderInput retained = HtmlTextReaderInput.Create(
            retainedReader, true, retainedBudget, retainedDiagnostics);
        Assert.That(retained.GetLeaveOpen(), Is.True);
        Assert.That(retained.GetBufferSize(), Is.EqualTo(4096));
        retained.Close();
        Assert.DoesNotThrow(retained.Close);
        Assert.That(retainedReader.Read(), Is.EqualTo('r'));
        Assert.Throws<ObjectDisposedException>(() => retained.Read());

        StringReader ownedReader = new StringReader("o");
        HtmlBudget ownedBudget = new HtmlBudget(HtmlLimits.CreateDefault(), CancellationToken.None);
        HtmlDiagnosticCollector ownedDiagnostics = new HtmlDiagnosticCollector(ownedBudget);
        HtmlTextReaderInput owned = HtmlTextReaderInput.Create(ownedReader, false, ownedBudget, ownedDiagnostics);
        Assert.That(owned.GetLeaveOpen(), Is.False);
        owned.Close();
        Assert.Throws<ObjectDisposedException>(() => ownedReader.Read());
    }

    [Test]
    public void CancellationIsCheckedBeforeReadingFromTheTextReader()
    {
        CancellationTokenSource source = new CancellationTokenSource();
        HtmlBudget budget = new HtmlBudget(HtmlLimits.CreateDefault(), source.Token);
        HtmlDiagnosticCollector diagnostics = new HtmlDiagnosticCollector(budget);
        ChunkedTextReader reader = new ChunkedTextReader("abc", 1);
        HtmlTextReaderInput input = HtmlTextReaderInput.Create(reader, true, 1, budget, diagnostics);
        source.Cancel();

        Assert.Throws<OperationCanceledException>(() => input.Read());
        Assert.That(reader.GetPosition(), Is.EqualTo(0));
        input.Close();
        Assert.That(reader.GetWasDisposed(), Is.False);
    }

    [Test]
    public void FactoryValidatesDependenciesAndBoundedBufferSize()
    {
        HtmlBudget budget = new HtmlBudget(HtmlLimits.CreateDefault(), CancellationToken.None);
        HtmlDiagnosticCollector diagnostics = new HtmlDiagnosticCollector(budget);
        StringReader reader = new StringReader("");

        Assert.Throws<ArgumentNullException>(() => HtmlTextReaderInput.Create(null, true, budget, diagnostics));
        Assert.Throws<ArgumentNullException>(() => HtmlTextReaderInput.Create(reader, true, null, diagnostics));
        Assert.Throws<ArgumentNullException>(() => HtmlTextReaderInput.Create(reader, true, budget, null));
        Assert.Throws<ArgumentOutOfRangeException>(() => HtmlTextReaderInput.Create(reader, true, 0, budget, diagnostics));
        Assert.Throws<ArgumentOutOfRangeException>(() => HtmlTextReaderInput.Create(reader, true, 65537, budget, diagnostics));
    }

    static string ReadAll(HtmlTextReaderInput input)
    {
        StringBuilder builder = new StringBuilder();
        int value = input.Read();
        while (value >= 0)
        {
            builder.Append((char)value);
            value = input.Read();
        }

        return builder.ToString();
    }

    static void AssertPosition(HtmlSourcePosition position, int normalizedOffset, int characterOffset, int line, int column)
    {
        Assert.That(position.GetNormalizedCharacterOffset(), Is.EqualTo(normalizedOffset));
        Assert.That(position.GetCharacterOffset(), Is.EqualTo(characterOffset));
        Assert.That(position.GetLine(), Is.EqualTo(line));
        Assert.That(position.GetColumn(), Is.EqualTo(column));
    }
}

class ChunkedTextReader : TextReader
{
    string text;
    int maximumChunk;
    int position;
    int readCallCount;
    bool disposed;

    public ChunkedTextReader(string text, int maximumChunk)
    {
        this.text = text;
        this.maximumChunk = maximumChunk;
        this.position = 0;
        this.readCallCount = 0;
        this.disposed = false;
    }

    public override int Peek()
    {
        this.ThrowIfDisposed();
        if (this.position == this.text.Length)
        {
            return -1;
        }

        return this.text[this.position];
    }

    public override int Read()
    {
        this.ThrowIfDisposed();
        if (this.position == this.text.Length)
        {
            return -1;
        }

        char value = this.text[this.position];
        this.position++;
        return value;
    }

    public override int Read(char[] buffer, int index, int count)
    {
        this.ThrowIfDisposed();
        this.readCallCount++;
        int remaining = this.text.Length - this.position;
        int read = Math.Min(Math.Min(count, this.maximumChunk), remaining);
        this.text.CopyTo(this.position, buffer, index, read);
        this.position += read;
        return read;
    }

    public int GetPosition()
    {
        return this.position;
    }

    public int GetReadCallCount()
    {
        return this.readCallCount;
    }

    public bool GetWasDisposed()
    {
        return this.disposed;
    }

    protected override void Dispose(bool disposing)
    {
        this.disposed = true;
        base.Dispose(disposing);
    }

    void ThrowIfDisposed()
    {
        if (this.disposed)
        {
            throw new ObjectDisposedException("ChunkedTextReader");
        }
    }
}
