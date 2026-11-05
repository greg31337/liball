namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlInterfaceTests
{
    [Test]
    public void IncrementalClassesExposeNarrowPublicContracts()
    {
        IHtmlTokenizer tokenizer = HtmlTokenizer.Create(
            HtmlTokenizerOptions.CreateDefault());
        IHtmlParser parser = HtmlParser.Create(HtmlParseOptions.CreateDefault());

        tokenizer.Feed("text".AsSpan(), true);
        parser.Feed("<p>text</p>".AsSpan(), true);

        Assert.Multiple(() =>
        {
            Assert.That(tokenizer.Read().GetStatus(),
                Is.EqualTo(HtmlTokenReadStatus.TokenAvailable));
            Assert.That(parser.Complete().GetDocument().GetBody()
                .GetTextContent(), Is.EqualTo("text"));
        });
    }

    [Test]
    public void ConfigurationContractsCompileOnceAtTheConversionBoundary()
    {
        HtmlCompiledTextOptions compiledText =
            HtmlTextOptions.CreateDefault().Compile();
        HtmlCompiledMarkdownOptions compiledMarkdown =
            HtmlMarkdownOptions.CreateCommonMark().Compile();
        TrackingTextOptions textOptions = new TrackingTextOptions(
            compiledText);
        TrackingMarkdownOptions markdownOptions = new TrackingMarkdownOptions(
            compiledMarkdown);

        HtmlTextResult text = HtmlTextConverter.Convert("<p>value</p>",
            textOptions, CancellationToken.None);
        HtmlMarkdownResult markdown = HtmlMarkdownConverter.Convert(
            "<p>value</p>", markdownOptions, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(text.GetText(), Is.EqualTo("value"));
            Assert.That(markdown.GetMarkdown(), Is.EqualTo("value"));
            Assert.That(textOptions.GetCompileCount(), Is.EqualTo(1));
            Assert.That(markdownOptions.GetCompileCount(), Is.EqualTo(1));
            Assert.That(compiledText.Compile(), Is.SameAs(compiledText));
            Assert.That(compiledMarkdown.Compile(),
                Is.SameAs(compiledMarkdown));
        });
    }

    [Test]
    public void ParserStatusHandlingCanBeTestedWithAStubTokenizer()
    {
        StubTokenizerControl tokenizer = new StubTokenizerControl();
        tokenizer.Add(HtmlTokenReadResult.CreateStatus(
            HtmlTokenReadStatus.NeedMoreInput));
        tokenizer.Add(HtmlTokenReadResult.CreateStatus(
            HtmlTokenReadStatus.Complete));
        HtmlParser parser = HtmlParser.Create(HtmlParseOptions.CreateDefault(),
            tokenizer);

        parser.Feed("ignored".AsSpan(), true);
        HtmlParseProgress first = parser.Read();
        HtmlParseProgress second = parser.Read();
        HtmlParseResult result = parser.Complete();

        Assert.Multiple(() =>
        {
            Assert.That(first.GetStatus(),
                Is.EqualTo(HtmlParseProgressStatus.NeedMoreInput));
            Assert.That(second.GetStatus(),
                Is.EqualTo(HtmlParseProgressStatus.Complete));
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(tokenizer.GetFeedCount(), Is.EqualTo(1));
            Assert.That(tokenizer.GetReadCount(), Is.EqualTo(2));
            Assert.That(tokenizer.GetCdataUpdateCount(), Is.EqualTo(2));
        });
    }
}

class TrackingTextOptions : IHtmlTextOptions
{
    HtmlCompiledTextOptions options;
    int compileCount;

    public TrackingTextOptions(HtmlCompiledTextOptions optionsValue)
    {
        this.options = optionsValue;
        this.compileCount = 0;
    }

    public HtmlCompiledTextOptions Compile()
    {
        this.compileCount++;
        return this.options;
    }

    public int GetCompileCount()
    {
        return this.compileCount;
    }
}

class TrackingMarkdownOptions : IHtmlMarkdownOptions
{
    HtmlCompiledMarkdownOptions options;
    int compileCount;

    public TrackingMarkdownOptions(HtmlCompiledMarkdownOptions optionsValue)
    {
        this.options = optionsValue;
        this.compileCount = 0;
    }

    public HtmlCompiledMarkdownOptions Compile()
    {
        this.compileCount++;
        return this.options;
    }

    public int GetCompileCount()
    {
        return this.compileCount;
    }
}

class StubTokenizerControl : IHtmlTokenizerControl
{
    Queue<HtmlTokenReadResult> results;
    int feedCount;
    int readCount;
    int cdataUpdateCount;

    public StubTokenizerControl()
    {
        this.results = new Queue<HtmlTokenReadResult>();
        this.feedCount = 0;
        this.readCount = 0;
        this.cdataUpdateCount = 0;
    }

    public void Add(HtmlTokenReadResult result)
    {
        this.results.Enqueue(result);
    }

    public void Feed(ReadOnlySpan<char> input, bool isFinal)
    {
        this.feedCount++;
    }

    public HtmlTokenReadResult Read()
    {
        this.readCount++;
        return this.results.Dequeue();
    }

    public void SetCdataAllowed(bool value)
    {
        this.cdataUpdateCount++;
    }

    public void InitializeForFragment(HtmlTokenizerState state,
        string lastStartTagName, bool cdataAllowed)
    {
    }

    public void ApplyTreeBuilderState(HtmlTokenizerState state)
    {
    }

    public void EnableByteOffsets(HtmlDecodedInput input)
    {
    }

    public int GetFeedCount()
    {
        return this.feedCount;
    }

    public int GetReadCount()
    {
        return this.readCount;
    }

    public int GetCdataUpdateCount()
    {
        return this.cdataUpdateCount;
    }
}
