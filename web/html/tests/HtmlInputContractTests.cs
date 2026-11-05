using System.Text;

namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlInputContractTests
{
    [Test]
    public void CharacterSpanOptionsRecordResponseMetadataAndBaseFallback()
    {
        IHtmlApi api = new HtmlApi();
        HtmlParseOptions options = new HtmlParseOptions();
        options.SetResponseUrl("https://example.test/articles/page.html");
        options.SetContentLanguage("fr-CA");

        HtmlParseResult result = api.ParseCharactersWithOptions(
            "<p>bonjour</p>".AsSpan(), options, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetDocument().GetUrl(),
                Is.EqualTo("https://example.test/articles/page.html"));
            Assert.That(result.GetDocument().GetBaseUrl(),
                Is.EqualTo("https://example.test/articles/page.html"));
            Assert.That(result.GetDocument().GetContentLanguage(),
                Is.EqualTo("fr-CA"));
            Assert.That(result.GetDetectedEncoding(), Is.Null);
        });
    }

    [Test]
    public void ExplicitBaseOverridesResponseUrlAndOptionsAreCopied()
    {
        HtmlParseOptions options = new HtmlParseOptions();
        options.SetResponseUrl("https://response.example/page");
        options.SetBaseUrl("https://static.example/assets/");
        options.SetContentLanguage("en");
        HtmlParser parser = new HtmlParser(options);

        options.SetResponseUrl("https://changed.example/");
        options.SetBaseUrl("https://changed.example/base/");
        options.SetContentLanguage("de");
        parser.Feed("<a href=item>item</a>".AsSpan(), true);
        HtmlDocument document = parser.Complete().GetDocument();

        Assert.Multiple(() =>
        {
            Assert.That(document.GetUrl(),
                Is.EqualTo("https://response.example/page"));
            Assert.That(document.GetBaseUrl(),
                Is.EqualTo("https://static.example/assets/"));
            Assert.That(document.GetContentLanguage(), Is.EqualTo("en"));
            Assert.That(document.QuerySelector("a").GetAbsoluteUrl("href")
                .GetUrl(), Is.EqualTo("https://static.example/assets/item"));
        });
    }

    [Test]
    public void TransportEncodingHintParticipatesInByteDetection()
    {
        HtmlParseOptions options = new HtmlParseOptions();
        options.SetTransportEncodingLabel("iso-8859-1");
        byte[] input = Encoding.Latin1.GetBytes("<p>caf\u00e9</p>");

        HtmlParseResult result = new HtmlApi().ParseBytesWithOptions(input,
            options, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.GetDocument().GetBody().GetTextContent(),
                Is.EqualTo("caf\u00e9"));
            Assert.That(result.GetEncodingDetection().GetSource(),
                Is.EqualTo(HtmlEncodingSource.Transport));
            Assert.That(result.GetDetectedEncodingLabel(),
                Is.EqualTo("windows-1252"));
            Assert.That(options.GetTransportEncodingLabel(),
                Is.EqualTo("iso-8859-1"));
        });
    }

    [Test]
    public void EveryFragmentInputFormUsesTheSuppliedContext()
    {
        IHtmlApi api = new HtmlApi();
        HtmlDocument owner = api.ParseString("");
        HtmlFragmentContext context = new HtmlFactory()
            .CreateFragmentContext(owner.CreateElement("title"));
        HtmlParseOptions characters = new HtmlParseOptions();
        HtmlParseOptions bytes = new HtmlParseOptions();
        bytes.SetEncoding(Encoding.UTF8);
        string markup = "A<b>";

        HtmlParseResult fromString = api.ParseFragmentWithOptions(markup,
            context, characters, CancellationToken.None);
        HtmlParseResult fromCharacters =
            api.ParseFragmentCharactersWithOptions(markup.AsSpan(), context,
                characters, CancellationToken.None);
        HtmlParseResult fromBytes = api.ParseFragmentBytesWithOptions(
            Encoding.UTF8.GetBytes(markup), context, bytes,
            CancellationToken.None);
        HtmlParseResult fromByteSpan =
            api.ParseFragmentByteSpanWithOptions(
                Encoding.UTF8.GetBytes(markup).AsSpan(), context, bytes,
                CancellationToken.None);
        MemoryStream stream = new MemoryStream(
            Encoding.UTF8.GetBytes(markup));
        bytes.SetLeaveOpen(false);
        HtmlParseResult fromStream = api.ParseFragmentStream(stream, context,
            bytes, CancellationToken.None);
        TrackingInputReader reader = new TrackingInputReader(markup);
        characters.SetLeaveOpen(false);
        HtmlParseResult fromReader = api.ParseFragmentReader(reader, context,
            characters, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(GetFragmentText(fromString), Is.EqualTo(markup));
            Assert.That(GetFragmentText(fromCharacters), Is.EqualTo(markup));
            Assert.That(GetFragmentText(fromBytes), Is.EqualTo(markup));
            Assert.That(GetFragmentText(fromByteSpan), Is.EqualTo(markup));
            Assert.That(GetFragmentText(fromStream), Is.EqualTo(markup));
            Assert.That(GetFragmentText(fromReader), Is.EqualTo(markup));
            Assert.That(fromBytes.GetEncodingDetection().GetSource(),
                Is.EqualTo(HtmlEncodingSource.Explicit));
            Assert.That(stream.CanRead, Is.False);
            Assert.That(reader.GetWasDisposed(), Is.True);
        });
    }

    [Test]
    public void FragmentByteLocationsAndReaderLimitsRemainObservable()
    {
        IHtmlApi api = new HtmlApi();
        HtmlDocument owner = api.ParseString("");
        HtmlFragmentContext context = new HtmlFactory()
            .CreateFragmentContext(owner.CreateElement("body"));
        HtmlParseOptions byteOptions = new HtmlParseOptions();
        byteOptions.SetEncoding(Encoding.UTF8);
        byteOptions.SetTrackByteOffsets(true);
        HtmlParseResult mapped = api.ParseFragmentByteSpanWithOptions(
            "<p>\u20ac</p>"u8, context, byteOptions,
            CancellationToken.None);

        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumInputCharacters(8);
        HtmlParseOptions readerOptions = new HtmlParseOptions();
        readerOptions.SetLimits(limits);
        HtmlParseResult limited = api.ParseFragmentReader(
            new StringReader("<b>x</b>ignored"), context, readerOptions,
            CancellationToken.None);

        HtmlNode text = mapped.GetFragment().QuerySelector("p")
            .GetChildren().Single();
        Assert.Multiple(() =>
        {
            Assert.That(text.GetSourceSpan().GetByteLength(), Is.EqualTo(3));
            Assert.That(limited.IsSuccess(), Is.False);
            Assert.That(limited.IsComplete(), Is.False);
            Assert.That(limited.WasTruncated(), Is.True);
            Assert.That(GetFragmentText(limited), Is.EqualTo("x"));
            Assert.That(limited.GetDiagnostics().Select(value =>
                value.GetCode()), Does.Contain(
                    HtmlDiagnosticCode.InputLimitExceeded));
        });
    }

    [Test]
    public void FragmentInputsHonorCancellationBeforeReading()
    {
        IHtmlApi api = new HtmlApi();
        HtmlDocument owner = api.ParseString("");
        HtmlFragmentContext context = new HtmlFactory()
            .CreateFragmentContext(owner.CreateElement("body"));
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        TrackingInputReader reader = new TrackingInputReader("<p>x</p>");

        Assert.That(() => api.ParseFragmentReader(reader, context,
            new HtmlParseOptions(), cancellation.Token),
            Throws.InstanceOf<OperationCanceledException>());
        Assert.That(reader.GetReadCount(), Is.Zero);
    }

    static string GetFragmentText(HtmlParseResult result)
    {
        return result.GetFragment().GetTextContent();
    }
}

class TrackingInputReader : StringReader
{
    bool wasDisposed;
    int readCount;

    public TrackingInputReader(string value) : base(value)
    {
        this.wasDisposed = false;
        this.readCount = 0;
    }

    public override int Read(char[] buffer, int index, int count)
    {
        this.readCount++;
        return base.Read(buffer, index, count);
    }

    public bool GetWasDisposed()
    {
        return this.wasDisposed;
    }

    public int GetReadCount()
    {
        return this.readCount;
    }

    protected override void Dispose(bool disposing)
    {
        this.wasDisposed = true;
        base.Dispose(disposing);
    }
}
