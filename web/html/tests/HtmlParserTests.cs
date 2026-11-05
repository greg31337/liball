using System.Text;

namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlParserTests
{
    [Test]
    public void ConvenienceParsersBuildCorrectedDocumentsFromStringAndSpan()
    {
        HtmlDocument fromString = Html.Parse(
            "<!doctype html><title>A&lt;B</title><p>one<p>two");
        HtmlDocument fromSpan = Html.Parse(
            "<!doctype html><main><b>value</b></main>".AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(fromString.GetDocumentElement().GetLocalName(),
                Is.EqualTo("html"));
            Assert.That(fromString.GetTitle(), Is.EqualTo("A<B"));
            Assert.That(FindAll(fromString.GetBody(), "p"), Has.Length.EqualTo(2));
            Assert.That(fromString.GetBody().GetTextContent(),
                Is.EqualTo("onetwo"));
            Assert.That(FindFirst(fromSpan.GetBody(), "main").GetTextContent(),
                Is.EqualTo("value"));
        });
    }

    [Test]
    public void ParserAppliesTokenizerStateRequestsAcrossIncrementalChunks()
    {
        HtmlParser parser = HtmlParser.Create(HtmlParseOptions.CreateDefault());
        parser.Feed("<!doctype html><title>A<".AsSpan(), false);
        HtmlParseProgress progress = Drain(parser);

        Assert.That(progress.GetStatus(),
            Is.EqualTo(HtmlParseProgressStatus.NeedMoreInput));

        parser.Feed("b></title><p>x".AsSpan(), true);
        HtmlParseResult result = parser.Complete();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.IsComplete(), Is.True);
            Assert.That(result.GetDocument().GetTitle(), Is.EqualTo("A<b>"));
            Assert.That(result.GetDocument().GetBody().GetTextContent(),
                Is.EqualTo("x"));
        });
    }

    [Test]
    public void ParserRecordsEveryDispatchedInsertionModeAndNamespace()
    {
        HtmlParser parser = HtmlParser.Create(HtmlParseOptions.CreateDefault());
        parser.Feed(("<!doctype html><table><tr><td><svg><circle/>"
            + "</svg></td></tr></table>").AsSpan(), true);

        HtmlParseResult result = parser.Complete();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(parser.GetVisitedInsertionModes(),
                Does.Contain(HtmlInsertionMode.InTable));
            Assert.That(parser.GetVisitedInsertionModes(),
                Does.Contain(HtmlInsertionMode.InRow));
            Assert.That(parser.GetVisitedInsertionModes(),
                Does.Contain(HtmlInsertionMode.InCell));
            Assert.That(parser.GetVisitedNamespaces(),
                Does.Contain(HtmlNamespace.Html));
            Assert.That(parser.GetVisitedNamespaces(),
                Does.Contain(HtmlNamespace.Svg));
        });
    }

    [Test]
    public void FullStringParseCarriesBaseUrlAndAllDiagnosticsToTheDocument()
    {
        HtmlParseOptions options = HtmlParseOptions.CreateDefault();
        options.SetBaseUrl("https://example.test/base/");

        HtmlParseResult result = Html.Parse(
            "<!doctype html><div a=one a=two></span>", options,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetDocument().GetBaseUrl(),
                Is.EqualTo("https://example.test/base/"));
            Assert.That(result.GetDiagnostics().Select(value => value.GetCode()),
                Does.Contain(HtmlDiagnosticCode.TokenizerDuplicateAttribute));
            Assert.That(result.GetDiagnostics().Select(value => value.GetCode()),
                Does.Contain(HtmlDiagnosticCode.TreeBuilderUnexpectedToken));
            Assert.That(result.GetDocument().GetDiagnostics(),
                Is.EqualTo(result.GetDiagnostics()));
        });
    }

    [Test]
    public void ByteParseDetectsMetaEncodingAndTracksOriginalByteOffsets()
    {
        byte[] bytes = Encoding.UTF8.GetBytes(
            "<!doctype html><meta charset=utf-8><p>€</p>");
        HtmlParseOptions options = HtmlParseOptions.CreateDefault();
        options.SetTrackByteOffsets(true);

        HtmlParseResult result = Html.Parse(bytes, options,
            CancellationToken.None);
        HtmlNode paragraph = FindFirst(result.GetDocument().GetBody(), "p");
        HtmlNode text = paragraph.GetChildren().Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetDetectedEncodingLabel(), Is.EqualTo("UTF-8"));
            Assert.That(result.GetDetectedEncoding().CodePage, Is.EqualTo(65001));
            Assert.That(result.GetEncodingDetection().GetSource(),
                Is.EqualTo(HtmlEncodingSource.Meta));
            Assert.That(paragraph.GetSourceSpan().GetStart().HasByteOffset(),
                Is.True);
            Assert.That(paragraph.GetSourceSpan().GetStart().GetByteOffset(),
                Is.EqualTo(35));
            Assert.That(text.GetSourceSpan().GetByteLength(), Is.EqualTo(3));
            Assert.That(text.GetTextContent(), Is.EqualTo("€"));
        });
    }

    [Test]
    public void ExplicitEncodingIsUsedForByteSpanInput()
    {
        HtmlParseOptions options = HtmlParseOptions.CreateDefault();
        options.SetEncoding(Encoding.Latin1);
        byte[] bytes = Encoding.Latin1.GetBytes(
            "<!doctype html><p>é</p>");

        HtmlParseResult result = Html.Parse(bytes.AsSpan(), options,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.GetDocument().GetBody().GetTextContent(),
                Is.EqualTo("é"));
            Assert.That(result.GetDetectedEncodingLabel(),
                Is.EqualTo("iso-8859-1"));
            Assert.That(result.GetDetectedEncoding().WebName,
                Is.EqualTo("iso-8859-1"));
            Assert.That(result.GetEncodingDetection().GetSource(),
                Is.EqualTo(HtmlEncodingSource.Explicit));
        });
    }

    [TestCase(true)]
    [TestCase(false)]
    public void StreamParsingHonorsLeaveOpen(bool leaveOpen)
    {
        MemoryStream stream = new MemoryStream(
            "<!doctype html><p>stream</p>"u8.ToArray());
        HtmlParseOptions options = HtmlParseOptions.CreateDefault();
        options.SetEncoding(Encoding.UTF8);
        options.SetLeaveOpen(leaveOpen);

        HtmlParseResult result = Html.Parse(stream, options,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.GetDocument().GetBody().GetTextContent(),
                Is.EqualTo("stream"));
            Assert.That(stream.CanRead, Is.EqualTo(leaveOpen));
        });
    }

    [Test]
    public void TextReaderParsingIsChunkedAndHonorsLeaveOpen()
    {
        TrackingReader reader = new TrackingReader(
            "<!doctype html><p>reader</p>");
        HtmlParseOptions options = HtmlParseOptions.CreateDefault();
        options.SetLeaveOpen(false);

        HtmlParseResult result = Html.Parse(reader, options,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.GetDocument().GetBody().GetTextContent(),
                Is.EqualTo("reader"));
            Assert.That(reader.GetWasDisposed(), Is.True);
        });
    }

    [Test]
    public void CharacterLimitReturnsTheSafelyBuiltPartialDocument()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumInputCharacters(10);
        HtmlParseOptions options = HtmlParseOptions.CreateDefault();
        options.SetLimits(limits);

        HtmlParseResult result = Html.Parse("<p>abc</p><b>ignored</b>",
            options, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.False);
            Assert.That(result.IsComplete(), Is.False);
            Assert.That(result.WasTruncated(), Is.True);
            Assert.That(result.GetDocument().GetBody().GetTextContent(),
                Is.EqualTo("abc"));
            Assert.That(result.GetDiagnostics().Select(value => value.GetCode()),
                Does.Contain(HtmlDiagnosticCode.InputLimitExceeded));
        });
    }

    [Test]
    public void FullParseHonorsCancellationBeforeReadingInput()
    {
        CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();

        Assert.That(() => Html.Parse("<p>x</p>",
            HtmlParseOptions.CreateDefault(), source.Token),
            Throws.InstanceOf<OperationCanceledException>());
    }

    [Test]
    public void ParseOptionsControlScriptingAndSourceLocationTracking()
    {
        HtmlParseOptions scripting = HtmlParseOptions.CreateDefault();
        scripting.SetScriptingEnabled(true);
        HtmlParseResult enabled = Html.Parse(
            "<!doctype html><head><noscript><meta name=x></noscript>",
            scripting, CancellationToken.None);

        HtmlParseOptions noLocations = HtmlParseOptions.CreateDefault();
        noLocations.SetTrackSourceLocations(false);
        HtmlParseResult untracked = Html.Parse("<!doctype html><p>x",
            noLocations, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(FindFirst(enabled.GetDocument().GetHead(), "noscript")
                .GetTextContent(), Is.EqualTo("<meta name=x>"));
            Assert.That(FindFirst(untracked.GetDocument().GetBody(), "p")
                .GetSourceSpan().IsKnown(), Is.False);
        });
    }

    [Test]
    public void ByteLimitReturnsDecodedPartialTreeAndFailureStatus()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumInputBytes(6);
        HtmlParseOptions options = HtmlParseOptions.CreateDefault();
        options.SetEncoding(Encoding.UTF8);
        options.SetLimits(limits);

        HtmlParseResult result = Html.Parse("<p>abc</p>"u8.ToArray(),
            options, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.False);
            Assert.That(result.IsComplete(), Is.False);
            Assert.That(result.WasTruncated(), Is.True);
            Assert.That(result.GetDocument().GetBody().GetTextContent(),
                Is.EqualTo("abc"));
            Assert.That(result.GetDiagnostics().Select(value => value.GetCode()),
                Does.Contain(HtmlDiagnosticCode.InputLimitExceeded));
        });
    }

    [Test]
    public void FragmentParsingUsesHtmlAndForeignContexts()
    {
        HtmlParseResult title = Html.ParseFragment("A<b>", "title");

        HtmlDocument document = new HtmlDocument("", "",
            HtmlDocumentMode.NoQuirks, [], HtmlSourceSpan.CreateUnknown());
        HtmlNode svg = document.CreateElementInNamespace(
            "http://www.w3.org/2000/svg", "svg");
        HtmlParseResult foreign = Html.ParseFragment("<circle/>",
            new HtmlFragmentContext(svg), HtmlParseOptions.CreateDefault(),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(title.GetFragment().GetTextContent(),
                Is.EqualTo("A<b>"));
            Assert.That(title.GetFragment().GetOwnerDocument(),
                Is.SameAs(title.GetDocument()));
            Assert.That(foreign.GetFragment().GetChildren().Single()
                .GetNamespaceUri(),
                Is.EqualTo("http://www.w3.org/2000/svg"));
            Assert.That(foreign.GetFragment().GetChildren().Single()
                .GetLocalName(), Is.EqualTo("circle"));
        });
    }

    static HtmlParseProgress Drain(HtmlParser parser)
    {
        HtmlParseProgress progress = parser.Read();
        while (progress.GetStatus()
            == HtmlParseProgressStatus.TokenProcessed)
        {
            progress = parser.Read();
        }

        return progress;
    }

    static HtmlNode FindFirst(HtmlNode root, string localName)
    {
        HtmlNode[] children = root.GetChildren();
        int index = 0;
        while (index < children.Length)
        {
            if (children[index].GetKind() == HtmlNodeKind.Element
                && children[index].GetLocalName() == localName)
            {
                return children[index];
            }

            HtmlNode nested = FindFirst(children[index], localName);
            if (nested != null)
            {
                return nested;
            }

            index = index + 1;
        }

        return null;
    }

    static HtmlNode[] FindAll(HtmlNode root, string localName)
    {
        return root.GetDescendants().Where(value =>
            value.GetKind() == HtmlNodeKind.Element
            && value.GetLocalName() == localName).ToArray();
    }

    class TrackingReader : StringReader
    {
        bool wasDisposed;

        public TrackingReader(string value)
            : base(value)
        {
            this.wasDisposed = false;
        }

        public bool GetWasDisposed()
        {
            return this.wasDisposed;
        }

        public override int Read(char[] buffer, int index, int count)
        {
            int requested = count < 2 ? count : 2;
            return base.Read(buffer, index, requested);
        }

        protected override void Dispose(bool disposing)
        {
            this.wasDisposed = true;
            base.Dispose(disposing);
        }
    }
}
