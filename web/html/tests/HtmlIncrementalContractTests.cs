namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlIncrementalContractTests
{
    [Test]
    public void TokenizerPullsReaderAcrossSingleCharacterBoundaries()
    {
        HtmlTokenizer tokenizer = new HtmlTokenizer(
            new HtmlTokenizerOptions());
        IncrementalContractReader reader = new IncrementalContractReader(
            "<p a='one'>A&amp;B</p>", 1);
        tokenizer.SetReader(reader, false);

        List<HtmlToken> tokens = ReadTokens(tokenizer);

        Assert.Multiple(() =>
        {
            Assert.That(tokens[0].GetKind(),
                Is.EqualTo(HtmlTokenKind.StartTag));
            Assert.That(tokens[^2].GetKind(),
                Is.EqualTo(HtmlTokenKind.EndTag));
            Assert.That(tokens[^1].GetKind(),
                Is.EqualTo(HtmlTokenKind.EndOfFile));
            Assert.That(tokens[0].GetAttributes().Single().GetValue(),
                Is.EqualTo("one"));
            Assert.That(string.Concat(tokens.Where(value =>
                value.GetKind() == HtmlTokenKind.Character).Select(value =>
                    value.GetData())), Is.EqualTo("A&B"));
            Assert.That(reader.GetReadCount(), Is.GreaterThan(10));
            Assert.That(reader.GetWasDisposed(), Is.True);
        });
    }

    [Test]
    public void TokenizerFragmentInitializationSurvivesReset()
    {
        HtmlDocument document = new HtmlApi().ParseString("");
        HtmlFragmentContext context = new HtmlFactory()
            .CreateFragmentContext(document.CreateElement("title"));
        HtmlTokenizer tokenizer = new HtmlTokenizer(
            new HtmlTokenizerOptions());
        tokenizer.InitializeFragment(context, false);
        tokenizer.SetReader(new StringReader("A<b>"), true);
        List<HtmlToken> first = ReadTokens(tokenizer);

        tokenizer.Reset();
        tokenizer.SetReader(new StringReader("C<i>"), true);
        List<HtmlToken> second = ReadTokens(tokenizer);

        Assert.Multiple(() =>
        {
            Assert.That(first[0].GetKind(),
                Is.EqualTo(HtmlTokenKind.Character));
            Assert.That(first[0].GetData(), Is.EqualTo("A<b>"));
            Assert.That(second[0].GetKind(),
                Is.EqualTo(HtmlTokenKind.Character));
            Assert.That(second[0].GetData(), Is.EqualTo("C<i>"));
        });
    }

    [Test]
    public void TokenizerReportsCancellationAndReaderFailure()
    {
        CancellationTokenSource cancellation = new CancellationTokenSource();
        HtmlTokenizerOptions options = new HtmlTokenizerOptions();
        options.SetCancellationToken(cancellation.Token);
        HtmlTokenizer cancelled = new HtmlTokenizer(options);
        cancelled.Feed("text".AsSpan(), false);
        Assert.That(cancelled.Read().GetStatus(),
            Is.EqualTo(HtmlTokenReadStatus.TokenAvailable));
        cancellation.Cancel();

        HtmlTokenReadResult stopped = cancelled.Read();
        HtmlTokenizer failed = new HtmlTokenizer(
            new HtmlTokenizerOptions());
        failed.SetReader(new InvalidCountContractReader(), true);
        HtmlTokenReadResult failure = failed.Read();

        Assert.Multiple(() =>
        {
            Assert.That(stopped.GetStatus(),
                Is.EqualTo(HtmlTokenReadStatus.Cancelled));
            Assert.That(stopped.IsPartial(), Is.True);
            Assert.That(failure.GetStatus(),
                Is.EqualTo(HtmlTokenReadStatus.Failed));
            Assert.That(failure.IsPartial(), Is.False);
        });
    }

    [Test]
    public void TokenizerReaderLimitIsTerminalAndObservable()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumInputCharacters(3);
        HtmlTokenizerOptions options = new HtmlTokenizerOptions();
        options.SetLimits(limits);
        HtmlTokenizer tokenizer = new HtmlTokenizer(options);
        tokenizer.SetReader(new StringReader("abcd"), true);

        HtmlTokenReadResult token = tokenizer.Read();
        HtmlTokenReadResult terminal = tokenizer.Read();

        Assert.Multiple(() =>
        {
            Assert.That(token.GetToken().GetData(), Is.EqualTo("abc"));
            Assert.That(token.IsPartial(), Is.True);
            Assert.That(terminal.GetStatus(),
                Is.EqualTo(HtmlTokenReadStatus.LimitExceeded));
        });
    }

    [Test]
    public void ParserPullsReaderAndReusesFragmentContextAfterReset()
    {
        HtmlDocument document = new HtmlApi().ParseString("");
        HtmlFragmentContext context = new HtmlFactory()
            .CreateFragmentContext(document.CreateElement("table"));
        HtmlParser parser = new HtmlParser(new HtmlParseOptions());
        parser.InitializeFragment(context);
        IncrementalContractReader firstReader =
            new IncrementalContractReader("<tr><td>A", 1);
        parser.SetReader(firstReader, false);

        HtmlParseResult first = parser.Complete();
        parser.Reset();
        parser.SetReader(new StringReader("<tr><td>B"), true);
        HtmlParseResult second = parser.Complete();

        Assert.Multiple(() =>
        {
            Assert.That(first.IsSuccess(), Is.True);
            Assert.That(first.GetFragment().GetTextContent(), Is.EqualTo("A"));
            Assert.That(first.GetFragment().QuerySelector("tbody tr td"),
                Is.Not.Null);
            Assert.That(second.IsSuccess(), Is.True);
            Assert.That(second.GetFragment().GetTextContent(), Is.EqualTo("B"));
            Assert.That(firstReader.GetWasDisposed(), Is.True);
        });
    }

    [Test]
    public void ParserReadReportsProgressAcrossPulledChunkBoundaries()
    {
        HtmlParser parser = new HtmlParser(new HtmlParseOptions());
        parser.SetReader(new IncrementalContractReader(
            "<!doctype html><p>A&amp;B</p>", 1), true);
        List<HtmlParseProgressStatus> statuses = new List<HtmlParseProgressStatus>();

        HtmlParseProgress progress = parser.Read();
        while (progress.GetStatus() == HtmlParseProgressStatus.TokenProcessed)
        {
            statuses.Add(progress.GetStatus());
            progress = parser.Read();
        }

        HtmlParseResult result = parser.Complete();
        Assert.Multiple(() =>
        {
            Assert.That(statuses, Is.Not.Empty);
            Assert.That(progress.GetStatus(),
                Is.EqualTo(HtmlParseProgressStatus.Complete));
            Assert.That(result.GetDocument().GetBody().GetTextContent(),
                Is.EqualTo("A&B"));
        });
    }

    [Test]
    public void ParserReportsCancellationFailureAndLimitStates()
    {
        CancellationTokenSource cancellation = new CancellationTokenSource();
        HtmlParser cancelled = new HtmlParser(new HtmlParseOptions());
        cancelled.SetCancellationToken(cancellation.Token);
        cancellation.Cancel();
        HtmlParseProgress cancelledProgress = cancelled.Read();

        HtmlParser failed = new HtmlParser(new HtmlParseOptions());
        failed.SetReader(new InvalidCountContractReader(), true);
        HtmlParseProgress failedProgress = failed.Read();

        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumInputCharacters(3);
        HtmlParseOptions limitedOptions = new HtmlParseOptions();
        limitedOptions.SetLimits(limits);
        HtmlParser limited = new HtmlParser(limitedOptions);
        limited.SetReader(new StringReader("<p>x"), true);
        HtmlParseProgress limitedProgress = Drain(limited);

        Assert.Multiple(() =>
        {
            Assert.That(cancelledProgress.GetStatus(),
                Is.EqualTo(HtmlParseProgressStatus.Cancelled));
            Assert.That(failedProgress.GetStatus(),
                Is.EqualTo(HtmlParseProgressStatus.Failed));
            Assert.That(limitedProgress.GetStatus(),
                Is.EqualTo(HtmlParseProgressStatus.LimitExceeded));
        });
    }

    [Test]
    public void ParserCanReplaceCancellationTokenAfterReset()
    {
        CancellationTokenSource cancellation = new CancellationTokenSource();
        HtmlParser parser = new HtmlParser(new HtmlParseOptions());
        parser.SetCancellationToken(cancellation.Token);
        cancellation.Cancel();
        Assert.That(parser.Read().GetStatus(),
            Is.EqualTo(HtmlParseProgressStatus.Cancelled));

        parser.Reset();
        parser.SetCancellationToken(CancellationToken.None);
        parser.Feed("<p>reused</p>".AsSpan(), true);
        HtmlParseResult result = parser.Complete();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetDocument().GetBody().GetTextContent(),
                Is.EqualTo("reused"));
        });
    }

    [Test]
    public void ParserConfigurationCanFollowReaderAttachment()
    {
        HtmlDocument document = new HtmlApi().ParseString("");
        HtmlFragmentContext context = new HtmlFactory()
            .CreateFragmentContext(document.CreateElement("title"));
        HtmlParser parser = new HtmlParser(new HtmlParseOptions());
        parser.SetReader(new StringReader("A<b>"), true);
        parser.InitializeFragment(context);
        parser.SetCancellationToken(CancellationToken.None);

        HtmlParseResult result = parser.Complete();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetFragment().GetTextContent(),
                Is.EqualTo("A<b>"));
        });
    }

    static List<HtmlToken> ReadTokens(HtmlTokenizer tokenizer)
    {
        List<HtmlToken> tokens = new List<HtmlToken>();
        HtmlTokenReadResult read = tokenizer.Read();
        while (read.GetStatus() == HtmlTokenReadStatus.TokenAvailable)
        {
            tokens.Add(read.GetToken());
            read = tokenizer.Read();
        }

        Assert.That(read.GetStatus(), Is.EqualTo(HtmlTokenReadStatus.Complete));
        return tokens;
    }

    static HtmlParseProgress Drain(HtmlParser parser)
    {
        HtmlParseProgress progress = parser.Read();
        while (progress.GetStatus() == HtmlParseProgressStatus.TokenProcessed)
        {
            progress = parser.Read();
        }

        return progress;
    }
}

class IncrementalContractReader : StringReader
{
    int maximumChunk;
    int readCount;
    bool wasDisposed;

    public IncrementalContractReader(string value, int maximumChunkValue)
        : base(value)
    {
        this.maximumChunk = maximumChunkValue;
        this.readCount = 0;
        this.wasDisposed = false;
    }

    public override int Read(char[] buffer, int index, int count)
    {
        this.readCount++;
        int requested = count < this.maximumChunk
            ? count : this.maximumChunk;
        return base.Read(buffer, index, requested);
    }

    public int GetReadCount()
    {
        return this.readCount;
    }

    public bool GetWasDisposed()
    {
        return this.wasDisposed;
    }

    protected override void Dispose(bool disposing)
    {
        this.wasDisposed = true;
        base.Dispose(disposing);
    }
}

class InvalidCountContractReader : StringReader
{
    public InvalidCountContractReader() : base("")
    {
    }

    public override int Read(char[] buffer, int index, int count)
    {
        return count + 1;
    }
}
