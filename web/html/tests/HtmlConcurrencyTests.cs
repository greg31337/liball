namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlConcurrencyTests
{
    const int ConcurrentOperationCount = 32;
    const int ConcurrentConversionCount = 8;

    [Test]
    public void StaticTablesSupportConcurrentReaders()
    {
        bool[] results = new bool[ConcurrentOperationCount];

        Parallel.For(0, results.Length, index =>
        {
            HtmlNamedCharacterReferenceMatch entity =
                HtmlNamedCharacterReferences.FindLongest(
                    index % 2 == 0 ? "NotEqualTilde;tail" : "notin;tail");
            HtmlForeignAttributeAdjustment foreign =
                HtmlForeignNames.GetForeignAttributeAdjustment("XLINK:HREF");
            results[index] = entity.IsMatch()
                && entity.GetConsumedCharacters()
                    == (index % 2 == 0 ? 14 : 6)
                && HtmlEncodingDetector.GetEncodingName(
                    index % 2 == 0 ? " LATIN1 " : "unicode-1-1-utf-8")
                    == (index % 2 == 0 ? "windows-1252" : "UTF-8")
                && HtmlForeignNames.AdjustSvgTagName("lineargradient")
                    == "linearGradient"
                && HtmlForeignNames.AdjustSvgAttributeName("viewbox")
                    == "viewBox"
                && foreign.GetFound()
                && foreign.GetNamespaceUri()
                    == "http://www.w3.org/1999/xlink"
                && CssSafeData.GetPropertyProfile("BACKGROUND-IMAGE")
                    == CssSafePropertyProfile.UrlImage
                && CssSafeData.IsColorKeyword("RebeccaPurple")
                && HtmlEntities.DecodeText("&copy;&NotEqualTilde;")
                    == "©≂̸";
        });

        Assert.That(results, Is.All.True);
    }

    [Test]
    public void FrozenDocumentSupportsConcurrentTreeReadsAndColdTextCaches()
    {
        const string source = "<main id='content'>"
            + "<article class='card'><h1> First  story </h1>"
            + "<p>Alpha <strong>beta</strong>.</p></article>"
            + "<article class='card'><p>Second story.</p></article>"
            + "</main>";
        HtmlDocument reference = Html.Parse(source);
        string expectedText = reference.GetRoot().GetTextContent();
        string expectedNormalized =
            reference.GetRoot().GetNormalizedTextContent();
        string expectedHtml = reference.GetRoot().GetInnerHtml();
        HtmlDocument document = Html.Parse(source);
        HtmlNode root = document.GetRoot();
        bool[] results = new bool[ConcurrentOperationCount];
        document.Freeze();

        Parallel.For(0, results.Length, index =>
        {
            HtmlNode[] cards = document.FindByClass("card");
            results[index] = root.GetTextContent() == expectedText
                && root.GetNormalizedTextContent() == expectedNormalized
                && root.GetInnerHtml() == expectedHtml
                && cards.Length == 2
                && cards[0].GetOwnText() == ""
                && document.FindById("content") != null;
        });

        Assert.Multiple(() =>
        {
            Assert.That(results, Is.All.True);
            Assert.That(document.IsFrozen(), Is.True);
            Assert.That(document.GetSearchIndexBuildCountStorage(),
                Is.EqualTo(1));
        });
    }

    [Test]
    public void CompiledSelectorSupportsConcurrentQueries()
    {
        HtmlDocument document = Html.Parse(
            "<main><article class='card' data-state='ready'>"
            + "<a id='first' href='https://example.test/1'>One</a>"
            + "</article><article class='card' data-state='waiting'>"
            + "<a id='ignored' href='https://example.test/2'>Two</a>"
            + "</article><article class='card' data-state='ready'>"
            + "<a id='second' href='https://example.test/3'>Three</a>"
            + "</article></main>");
        HtmlNode root = document.GetRoot();
        HtmlSelector selector = HtmlSelectors.Compile(
            "article.card[data-state='ready'] > a[href^='https://']")
            .GetSelector();
        bool[] results = new bool[ConcurrentOperationCount];
        document.Freeze();

        Parallel.For(0, results.Length, index =>
        {
            HtmlNode[] matches = selector.FindAll(root);
            HtmlNode first = selector.FindFirst(root);
            results[index] = matches.Length == 2
                && matches[0].GetAttribute("id") == "first"
                && matches[1].GetAttribute("id") == "second"
                && first == matches[0]
                && selector.Matches(matches[1], root);
            matches[0] = null;
        });

        Assert.Multiple(() =>
        {
            Assert.That(results, Is.All.True);
            Assert.That(selector.FindAll(root)[0].GetAttribute("id"),
                Is.EqualTo("first"));
        });
    }

    [Test]
    public void CompiledPolicySupportsConcurrentSanitization()
    {
        HtmlCompiledSanitizerPolicy policy =
            HtmlSanitizerPolicy.CreateRichText().Compile();
        HtmlDocument document = Html.Parse(
            "<main><p style='color:red;position:fixed'>Hello "
            + "<a href='javascript:alert(1)' onclick='bad()'>link</a>"
            + "</p><script>bad()</script></main>");
        document.Freeze();
        string expected = HtmlSanitizer.Sanitize(document.GetRoot(), policy,
            CancellationToken.None).GetHtml();
        bool[] results = new bool[ConcurrentOperationCount];

        Parallel.For(0, results.Length, index =>
        {
            HtmlSanitizeResult result = HtmlSanitizer.Sanitize(
                document.GetRoot(), policy, CancellationToken.None);
            results[index] = result.IsSuccess()
                && !result.WasTruncated()
                && result.GetHtml() == expected;
        });

        Assert.That(results, Is.All.True);
    }

    [Test]
    public void CompiledConversionSettingsSupportConcurrentOperations()
    {
        const string source = "<main><h1>Concurrent title</h1>"
            + "<p>Read <a href='https://example.test/'>the guide</a>.</p>"
            + "</main>";
        HtmlTextOptions textBuilder = HtmlTextOptions.CreateDefault();
        textBuilder.SetLinkStyle(HtmlTextLinkStyle.TextAndUrl);
        HtmlCompiledTextOptions textOptions = textBuilder.Compile();
        HtmlMarkdownOptions markdownBuilder =
            HtmlMarkdownOptions.CreateCommonMark();
        markdownBuilder.SetHeadingStyle(HtmlMarkdownHeadingStyle.Setext);
        HtmlCompiledMarkdownOptions markdownOptions =
            markdownBuilder.Compile();
        HtmlDocument reference = Html.Parse(source);
        string expectedText = HtmlTextConverter.Convert(reference,
            textOptions, CancellationToken.None).GetText();
        string expectedMarkdown = HtmlMarkdownConverter.Convert(reference,
            markdownOptions, CancellationToken.None).GetMarkdown();
        HtmlDocument document = Html.Parse(source);
        bool[] results = new bool[ConcurrentConversionCount];
        document.Freeze();

        Parallel.For(0, results.Length, index =>
        {
            HtmlTextResult textResult = HtmlTextConverter.Convert(document,
                textOptions, CancellationToken.None);
            HtmlMarkdownResult markdownResult =
                HtmlMarkdownConverter.Convert(document, markdownOptions,
                    CancellationToken.None);
            results[index] = textResult.IsSuccess()
                && textResult.IsComplete()
                && textResult.GetText() == expectedText
                && markdownResult.IsSuccess()
                && markdownResult.IsComplete()
                && markdownResult.GetMarkdown() == expectedMarkdown;
        });

        Assert.That(results, Is.All.True);
    }

    [Test]
    public void MutableDocumentUsesOneWriterUntilPublishedAsFrozen()
    {
        HtmlDocument document = Html.Parse("<main></main>");
        HtmlNode main = document.QuerySelector("main");
        HtmlNode[] rejectedChildren = new HtmlNode[ConcurrentOperationCount];
        int index = 0;
        while (index < rejectedChildren.Length)
        {
            rejectedChildren[index] = document.CreateElement("i");
            index = index + 1;
        }

        index = 0;
        while (index < 32)
        {
            HtmlNode child = document.CreateElement("span");
            child.SetAttribute("data-index", index.ToString());
            child.SetTextContent("item " + index.ToString());
            main.Append(child);
            index = index + 1;
        }

        long publishedVersion = document.GetMutationVersionStorage();
        string publishedHtml = main.GetInnerHtml();
        int rejected = 0;
        document.Freeze();

        Parallel.For(0, rejectedChildren.Length, childIndex =>
        {
            try
            {
                main.Append(rejectedChildren[childIndex]);
            }
            catch (InvalidOperationException)
            {
                Interlocked.Increment(ref rejected);
            }
        });

        Assert.Multiple(() =>
        {
            Assert.That(rejected, Is.EqualTo(rejectedChildren.Length));
            Assert.That(document.GetMutationVersionStorage(),
                Is.EqualTo(publishedVersion));
            Assert.That(main.GetChildElements(), Has.Length.EqualTo(32));
            Assert.That(main.GetInnerHtml(), Is.EqualTo(publishedHtml));
            Assert.That(rejectedChildren, Has.All.Matches<HtmlNode>(
                child => child.GetParent() == null));
        });
    }
}
