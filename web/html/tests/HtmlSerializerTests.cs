using System.Text;

namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlSerializerTests
{
    [Test]
    public void DocumentElementAndChildrenEntryPointsUseTheirOwnBoundaries()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode html = document.CreateElement("html");
        HtmlNode head = document.CreateElement("head");
        HtmlNode body = document.CreateElement("body");
        HtmlNode first = document.CreateElement("p");
        HtmlNode second = document.CreateElement("p");
        first.Append(document.CreateText("one"));
        second.Append(document.CreateText("two"));
        body.Append(first);
        body.Append(second);
        html.Append(head);
        html.Append(body);
        document.GetRoot().Append(html);

        const string Whole = "<html><head></head><body><p>one</p>"
            + "<p>two</p></body></html>";

        Assert.Multiple(() =>
        {
            Assert.That(HtmlSerializer.Serialize(document.GetRoot()),
                Is.EqualTo(Whole));
            Assert.That(document.GetRoot().GetOuterHtml(), Is.EqualTo(Whole));
            Assert.That(document.GetRoot().GetInnerHtml(), Is.EqualTo(Whole));
            Assert.That(html.GetOuterHtml(), Is.EqualTo(Whole));
            Assert.That(html.GetInnerHtml(), Is.EqualTo(
                "<head></head><body><p>one</p><p>two</p></body>"));
            Assert.That(body.GetOuterHtml(), Is.EqualTo(
                "<body><p>one</p><p>two</p></body>"));
            Assert.That(body.GetInnerHtml(), Is.EqualTo(
                "<p>one</p><p>two</p>"));
        });
    }

    [Test]
    public void FragmentAndLeafSerializationPreserveTreeOrder()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode fragment = document.CreateFragment();
        HtmlNode emphasis = document.CreateElement("em");
        HtmlNode text = document.CreateText("middle");
        HtmlNode comment = document.CreateComment("after");
        emphasis.Append(document.CreateText("before"));
        fragment.Append(emphasis);
        fragment.Append(text);
        fragment.Append(comment);

        Assert.Multiple(() =>
        {
            Assert.That(fragment.GetOuterHtml(), Is.EqualTo(
                "<em>before</em>middle<!--after-->"));
            Assert.That(fragment.GetInnerHtml(), Is.EqualTo(
                "<em>before</em>middle<!--after-->"));
            Assert.That(emphasis.GetOuterHtml(), Is.EqualTo(
                "<em>before</em>"));
            Assert.That(text.GetOuterHtml(), Is.EqualTo("middle"));
            Assert.That(text.GetInnerHtml(), Is.Empty);
            Assert.That(comment.GetOuterHtml(), Is.EqualTo("<!--after-->"));
            Assert.That(comment.GetInnerHtml(), Is.Empty);
        });
    }

    [Test]
    public void TemplateInnerAndOuterTraversalUseTemplateContent()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode template = document.CreateElement("template");
        HtmlNode strong = document.CreateElement("strong");
        strong.Append(document.CreateText("value"));
        template.Append(strong);

        Assert.Multiple(() =>
        {
            Assert.That(template.GetChildren(), Is.Empty);
            Assert.That(template.GetTemplateContent().GetChildren(),
                Is.EqualTo(new[] { strong }));
            Assert.That(template.GetInnerHtml(),
                Is.EqualTo("<strong>value</strong>"));
            Assert.That(template.GetOuterHtml(), Is.EqualTo(
                "<template><strong>value</strong></template>"));
        });
    }

    [Test]
    public void TextWriterEntryPointsAppendOuterAndInnerMarkup()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode section = document.CreateElement("section");
        HtmlNode paragraph = document.CreateElement("p");
        paragraph.SetAttribute("id", "first");
        paragraph.Append(document.CreateText("value"));
        section.Append(paragraph);
        StringBuilder builder = new StringBuilder("prefix:");
        StringWriter writer = new StringWriter(builder);

        HtmlSerializer.Serialize(section, writer);
        writer.Write('|');
        HtmlSerializer.SerializeChildren(section, writer,
            HtmlSerializerOptions.CreateDefault(), CancellationToken.None);

        Assert.That(builder.ToString(), Is.EqualTo(
            "prefix:<section><p id=\"first\">value</p></section>|"
            + "<p id=\"first\">value</p>"));
    }

    [Test]
    public void PresetsExposeIndependentSerializerConfiguration()
    {
        HtmlSerializerOptions defaults = HtmlSerializerOptions.CreateDefault();
        HtmlSerializerOptions canonical =
            HtmlSerializerOptions.CreateCanonical();
        HtmlSerializerOptions preserve = HtmlSerializerOptions.CreatePreserve();
        HtmlSerializerOptions compact = HtmlSerializerOptions.CreateCompact();
        HtmlSerializerOptions pretty = HtmlSerializerOptions.CreatePretty();
        canonical.SetAsciiOnly(true);
        canonical.SetQuoteMode(HtmlSerializerQuoteMode.Shortest);
        canonical.SetSortAttributes(true);
        canonical.SetSelfCloseForeignElements(true);
        canonical.SetIndentation("\t");
        canonical.SetNewLine("\r\n");
        HtmlLimits limits = canonical.GetLimits();
        limits.SetMaximumOutputCharacters(1234);
        canonical.SetLimits(limits);
        HtmlSerializerOptions copy = canonical.Copy();
        canonical.SetIndentation("changed");
        limits = canonical.GetLimits();
        limits.SetMaximumOutputCharacters(9999);

        Assert.Multiple(() =>
        {
            Assert.That(defaults.GetNameMode(),
                Is.EqualTo(HtmlSerializerNameMode.Canonical));
            Assert.That(defaults.GetQuoteMode(),
                Is.EqualTo(HtmlSerializerQuoteMode.Double));
            Assert.That(defaults.GetPretty(), Is.False);
            Assert.That(defaults.GetOmitOptionalTags(), Is.False);
            Assert.That(preserve.GetNameMode(),
                Is.EqualTo(HtmlSerializerNameMode.Preserve));
            Assert.That(compact.GetOmitOptionalTags(), Is.True);
            Assert.That(pretty.GetPretty(), Is.True);
            Assert.That(copy.GetAsciiOnly(), Is.True);
            Assert.That(copy.GetQuoteMode(),
                Is.EqualTo(HtmlSerializerQuoteMode.Shortest));
            Assert.That(copy.GetSortAttributes(), Is.True);
            Assert.That(copy.GetSelfCloseForeignElements(), Is.True);
            Assert.That(copy.GetIndentation(), Is.EqualTo("\t"));
            Assert.That(copy.GetNewLine(), Is.EqualTo("\r\n"));
            Assert.That(copy.GetLimits().GetMaximumOutputCharacters(),
                Is.EqualTo(1234));
            Assert.That(canonical.GetLimits().GetMaximumOutputCharacters(),
                Is.EqualTo(1234));
        });
    }

    [Test]
    public void PreCanceledSerializationDoesNotModifyWriter()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode element = document.CreateElement("p");
        element.Append(document.CreateText("value"));
        StringWriter writer = new StringWriter();
        writer.Write("existing");
        CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            HtmlSerializer.Serialize(element, writer,
                HtmlSerializerOptions.CreateDefault(), source.Token));
        Assert.That(writer.ToString(), Is.EqualTo("existing"));
    }

    [Test]
    public void KnownNamespacesUseLocalElementNamesAndCanonicalAttributePrefixes()
    {
        const string HtmlUri = "http://www.w3.org/1999/xhtml";
        const string SvgUri = "http://www.w3.org/2000/svg";
        const string XmlUri = "http://www.w3.org/XML/1998/namespace";
        const string XmlnsUri = "http://www.w3.org/2000/xmlns/";
        const string XLinkUri = "http://www.w3.org/1999/xlink";
        HtmlDocument document = CreateDocument();
        HtmlSourceSpan span = HtmlSourceSpan.CreateUnknown();
        HtmlAttribute[] attributes =
        [
            HtmlAttribute.Create("plain", "one", span),
            HtmlAttribute.CreateNamespaced("wrong", "lang", "wrong",
                XmlUri, "en", span),
            HtmlAttribute.CreateNamespaced("wrong", "xmlns", "wrong",
                XmlnsUri, SvgUri, span),
            HtmlAttribute.CreateNamespaced("wrong", "xlink", "wrong",
                XmlnsUri, XLinkUri, span),
            HtmlAttribute.CreateNamespaced("wrong", "href", "wrong",
                XLinkUri, "#target", span),
            HtmlAttribute.CreateNamespaced("p:flag", "flag", "p",
                "urn:example:attributes", "yes", span)
        ];
        HtmlNode gradient = document.CreateElementStorage(HtmlNamespace.Svg,
            SvgUri, "s:linearGradient", "linearGradient", "s", attributes,
            span);
        HtmlNode html = document.CreateElementInNamespace(HtmlUri, "h:DIV");
        HtmlNode custom = document.CreateElementInNamespace("urn:example:elements",
            "w:Card");

        Assert.Multiple(() =>
        {
            Assert.That(gradient.GetOuterHtml(), Is.EqualTo(
                "<linearGradient plain=\"one\" xml:lang=\"en\""
                + " xmlns=\"http://www.w3.org/2000/svg\""
                + " xmlns:xlink=\"http://www.w3.org/1999/xlink\""
                + " xlink:href=\"#target\" p:flag=\"yes\">"
                + "</linearGradient>"));
            Assert.That(html.GetOuterHtml(), Is.EqualTo("<div></div>"));
            Assert.That(custom.GetOuterHtml(),
                Is.EqualTo("<w:Card></w:Card>"));
        });
    }

    [Test]
    public void DoctypesAndCommentsFollowHtmlFragmentSerialization()
    {
        HtmlDocument document = CreateDocument();
        HtmlSourceSpan span = HtmlSourceSpan.CreateUnknown();
        HtmlNode doctype = document.CreateDocumentTypeStorage(true, "html",
            true, "-//Example//DTD HTML Test//EN", true,
            "https://example.test/test.dtd", false, span);
        HtmlNode comment = document.CreateComment("literal <&> -- content");

        Assert.Multiple(() =>
        {
            Assert.That(doctype.GetOuterHtml(), Is.EqualTo("<!DOCTYPE html>"));
            Assert.That(doctype.GetInnerHtml(), Is.Empty);
            Assert.That(comment.GetOuterHtml(),
                Is.EqualTo("<!--literal <&> -- content-->"));
        });
    }

    [Test]
    public void HtmlVoidElementsSuppressChildrenAndEndTagsOnlyInHtmlNamespace()
    {
        const string SvgUri = "http://www.w3.org/2000/svg";
        HtmlDocument document = CreateDocument();
        HtmlNode image = document.CreateElement("img");
        image.SetAttribute("src", "a&b");
        image.Append(document.CreateText("ignored"));
        HtmlNode parameter = document.CreateElement("param");
        HtmlNode foreignBreak = document.CreateElementInNamespace(SvgUri, "br");
        foreignBreak.Append(document.CreateText("kept"));

        Assert.Multiple(() =>
        {
            Assert.That(image.GetOuterHtml(),
                Is.EqualTo("<img src=\"a&amp;b\">"));
            Assert.That(image.GetInnerHtml(), Is.Empty);
            Assert.That(parameter.GetOuterHtml(), Is.EqualTo("<param>"));
            Assert.That(foreignBreak.GetOuterHtml(),
                Is.EqualTo("<br>kept</br>"));
        });
    }

    [Test]
    public void HtmlBooleanAttributesAreMinimizedWithoutAffectingForeignAttributes()
    {
        const string SvgUri = "http://www.w3.org/2000/svg";
        HtmlDocument document = CreateDocument();
        HtmlNode input = document.CreateElement("input");
        input.SetAttribute("disabled", "false");
        input.SetAttribute("required", "required");
        input.SetAttribute("title", "disabled");
        HtmlSourceSpan span = HtmlSourceSpan.CreateUnknown();
        HtmlAttribute disabled = HtmlAttribute.Create("disabled", "disabled",
            span);
        HtmlNode foreign = document.CreateElementStorage(HtmlNamespace.Svg,
            SvgUri, "input", "input", "", new[] { disabled }, span);

        Assert.Multiple(() =>
        {
            Assert.That(input.GetOuterHtml(), Is.EqualTo(
                "<input disabled required title=\"disabled\">"));
            Assert.That(foreign.GetOuterHtml(), Is.EqualTo(
                "<input disabled=\"disabled\"></input>"));
        });
    }

    [Test]
    public void TextAndAttributesEscapeEntitiesOutsideHtmlRawTextElements()
    {
        const string SvgUri = "http://www.w3.org/2000/svg";
        HtmlDocument document = CreateDocument();
        HtmlNode container = document.CreateElement("div");
        container.SetAttribute("data-value", "<&>\u00A0\"'");
        container.Append(document.CreateText("<&>\u00A0\"'"));
        HtmlNode title = document.CreateElement("title");
        title.Append(document.CreateText("a < b & c > d\u00A0"));
        HtmlNode script = document.CreateElement("script");
        script.Append(document.CreateText("if (a < b && c > d) \"\u00A0\";"));
        HtmlNode style = document.CreateElement("style");
        style.Append(document.CreateText("a>b && c<d { content: \"&\"; }"));
        HtmlNode plainText = document.CreateElement("plaintext");
        plainText.Append(document.CreateText("literal <&>\u00A0"));
        HtmlNode foreignStyle = document.CreateElementInNamespace(SvgUri, "style");
        foreignStyle.Append(document.CreateText("escaped <&>\u00A0"));

        Assert.Multiple(() =>
        {
            Assert.That(container.GetOuterHtml(), Is.EqualTo(
                "<div data-value=\"&lt;&amp;&gt;&nbsp;&quot;'\">"
                + "&lt;&amp;&gt;&nbsp;\"'</div>"));
            Assert.That(title.GetOuterHtml(), Is.EqualTo(
                "<title>a &lt; b &amp; c &gt; d&nbsp;</title>"));
            Assert.That(script.GetOuterHtml(), Is.EqualTo(
                "<script>if (a < b && c > d) \"\u00A0\";</script>"));
            Assert.That(style.GetOuterHtml(), Is.EqualTo(
                "<style>a>b && c<d { content: \"&\"; }</style>"));
            Assert.That(plainText.GetOuterHtml(),
                Is.EqualTo("<plaintext>literal <&>\u00A0</plaintext>"));
            Assert.That(foreignStyle.GetOuterHtml(), Is.EqualTo(
                "<style>escaped &lt;&amp;&gt;&nbsp;</style>"));
        });
    }

    [Test]
    public void TemplateContentUsesNormalNestedSerializationRules()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode template = document.CreateElement("template");
        HtmlNode image = document.CreateElement("img");
        image.SetAttribute("itemscope", "itemscope");
        template.Append(document.CreateText("<&>"));
        template.Append(image);

        Assert.Multiple(() =>
        {
            Assert.That(template.GetInnerHtml(),
                Is.EqualTo("&lt;&amp;&gt;<img itemscope>"));
            Assert.That(template.GetOuterHtml(), Is.EqualTo(
                "<template>&lt;&amp;&gt;<img itemscope></template>"));
        });
    }

    [Test]
    public void CanonicalAndPreserveModesControlStoredHtmlNameCase()
    {
        const string HtmlUri = "http://www.w3.org/1999/xhtml";
        HtmlDocument document = CreateDocument();
        HtmlSourceSpan span = HtmlSourceSpan.CreateUnknown();
        HtmlAttribute attribute = HtmlAttribute.Create("DATA-Value", "one",
            span);
        HtmlNode element = document.CreateElementStorage(HtmlNamespace.Html,
            HtmlUri, "My-Widget", "My-Widget", "", new[] { attribute }, span);
        HtmlNode doctype = document.CreateDocumentTypeStorage(true, "HTML",
            false, "", false, "", false, span);
        HtmlSerializerOptions canonical =
            HtmlSerializerOptions.CreateCanonical();
        HtmlSerializerOptions preserve = HtmlSerializerOptions.CreatePreserve();

        Assert.Multiple(() =>
        {
            Assert.That(HtmlSerializer.Serialize(element, canonical,
                CancellationToken.None), Is.EqualTo(
                    "<my-widget data-value=\"one\"></my-widget>"));
            Assert.That(HtmlSerializer.Serialize(element, preserve,
                CancellationToken.None), Is.EqualTo(
                    "<My-Widget DATA-Value=\"one\"></My-Widget>"));
            Assert.That(HtmlSerializer.Serialize(doctype, canonical,
                CancellationToken.None), Is.EqualTo("<!DOCTYPE html>"));
            Assert.That(HtmlSerializer.Serialize(doctype, preserve,
                CancellationToken.None), Is.EqualTo("<!DOCTYPE HTML>"));
        });
    }

    [Test]
    public void QuoteModesEscapeOnlyTheSelectedAttributeDelimiter()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode element = document.CreateElement("div");
        element.SetAttribute("data-one", "one's \"quoted\" value");
        element.SetAttribute("data-two", "\"a\" and \"b\" with 'c'");
        element.SetAttribute("data-three", "it's John's");
        HtmlSerializerOptions single = HtmlSerializerOptions.CreateDefault();
        single.SetQuoteMode(HtmlSerializerQuoteMode.Single);
        HtmlSerializerOptions shortest = HtmlSerializerOptions.CreateDefault();
        shortest.SetQuoteMode(HtmlSerializerQuoteMode.Shortest);

        Assert.Multiple(() =>
        {
            Assert.That(HtmlSerializer.Serialize(element, single,
                CancellationToken.None), Is.EqualTo(
                    "<div data-one='one&#x27;s \"quoted\" value'"
                    + " data-two='\"a\" and \"b\" with &#x27;c&#x27;'"
                    + " data-three='it&#x27;s John&#x27;s'></div>"));
            Assert.That(HtmlSerializer.Serialize(element, shortest,
                CancellationToken.None), Is.EqualTo(
                    "<div data-one='one&#x27;s \"quoted\" value'"
                    + " data-two='\"a\" and \"b\" with &#x27;c&#x27;'"
                    + " data-three=\"it's John's\"></div>"));
        });
    }

    [Test]
    public void AsciiOnlyModeEncodesTextAttributesRawTextAndComments()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode fragment = document.CreateFragment();
        HtmlNode paragraph = document.CreateElement("p");
        paragraph.SetAttribute("title", "© \"é\"");
        paragraph.Append(document.CreateText("© & 💩 é"));
        HtmlNode script = document.CreateElement("script");
        script.Append(document.CreateText("const café = '💩';"));
        HtmlNode comment = document.CreateComment("café");
        fragment.Append(paragraph);
        fragment.Append(script);
        fragment.Append(comment);
        HtmlSerializerOptions options = HtmlSerializerOptions.CreateDefault();
        options.SetAsciiOnly(true);

        string result = HtmlSerializer.Serialize(fragment, options,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(
                "<p title=\"&copy; &quot;&eacute;&quot;\">"
                + "&copy; &amp; &#x1F4A9; &eacute;</p>"
                + "<script>const caf&#xE9; = '&#x1F4A9;';</script>"
                + "<!--caf&#xE9;-->"));
            Assert.That(result.All(value => value <= 127), Is.True);
        });
    }

    [Test]
    public void AttributeSortingIsDeterministicAndDoesNotMutateDomOrder()
    {
        const string SvgUri = "http://www.w3.org/2000/svg";
        const string XLinkUri = "http://www.w3.org/1999/xlink";
        HtmlDocument document = CreateDocument();
        HtmlSourceSpan span = HtmlSourceSpan.CreateUnknown();
        HtmlAttribute z = HtmlAttribute.Create("z", "last", span);
        HtmlAttribute href = HtmlAttribute.CreateNamespaced("xlink:href",
            "href", "xlink", XLinkUri, "#target", span);
        HtmlAttribute a = HtmlAttribute.Create("a", "first", span);
        HtmlNode element = document.CreateElementStorage(HtmlNamespace.Svg,
            SvgUri, "circle", "circle", "", new[] { z, href, a }, span);
        HtmlSerializerOptions preserved = HtmlSerializerOptions.CreateDefault();
        HtmlSerializerOptions sorted = HtmlSerializerOptions.CreateDefault();
        sorted.SetSortAttributes(true);

        Assert.Multiple(() =>
        {
            Assert.That(HtmlSerializer.Serialize(element, preserved,
                CancellationToken.None), Is.EqualTo(
                    "<circle z=\"last\" xlink:href=\"#target\""
                    + " a=\"first\"></circle>"));
            Assert.That(HtmlSerializer.Serialize(element, sorted,
                CancellationToken.None), Is.EqualTo(
                    "<circle a=\"first\" xlink:href=\"#target\""
                    + " z=\"last\"></circle>"));
            Assert.That(element.GetAttributes(), Is.EqualTo(
                new[] { z, href, a }));
        });
    }

    [Test]
    public void ForeignSelfClosingOptionOnlyCollapsesEmptyNonHtmlElements()
    {
        const string SvgUri = "http://www.w3.org/2000/svg";
        HtmlDocument document = CreateDocument();
        HtmlNode fragment = document.CreateFragment();
        HtmlNode circle = document.CreateElementInNamespace(SvgUri, "circle");
        HtmlNode group = document.CreateElementInNamespace(SvgUri, "g");
        HtmlNode path = document.CreateElementInNamespace(SvgUri, "path");
        HtmlNode custom = document.CreateElementInNamespace("urn:example:widgets",
            "w:Card");
        HtmlNode div = document.CreateElement("div");
        group.Append(path);
        fragment.Append(circle);
        fragment.Append(group);
        fragment.Append(custom);
        fragment.Append(div);
        HtmlSerializerOptions options = HtmlSerializerOptions.CreateDefault();
        options.SetSelfCloseForeignElements(true);

        Assert.Multiple(() =>
        {
            Assert.That(circle.GetOuterHtml(),
                Is.EqualTo("<circle></circle>"));
            Assert.That(HtmlSerializer.Serialize(fragment, options,
                CancellationToken.None), Is.EqualTo(
                    "<circle /><g><path /></g><w:Card /><div></div>"));
        });
    }

    [Test]
    public void OutputLimitAllowsExactStringsAndThrowsBeforeOverflowingWrites()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode paragraph = document.CreateElement("p");
        paragraph.Append(document.CreateText("x"));
        HtmlSerializerOptions exact = HtmlSerializerOptions.CreateDefault();
        HtmlLimits limits = exact.GetLimits();
        limits.SetMaximumOutputCharacters(8);
        exact.SetLimits(limits);
        HtmlSerializerOptions shortOptions = exact.Copy();
        limits.SetMaximumOutputCharacters(7);
        shortOptions.SetLimits(limits);
        StringWriter writer = new StringWriter();

        string result = HtmlSerializer.Serialize(paragraph, exact,
            CancellationToken.None);
        HtmlSerializationLimitException stringException =
            Assert.Throws<HtmlSerializationLimitException>(() =>
                HtmlSerializer.Serialize(paragraph, shortOptions,
                    CancellationToken.None));
        HtmlSerializationLimitException writerException =
            Assert.Throws<HtmlSerializationLimitException>(() =>
                HtmlSerializer.Serialize(paragraph, writer, shortOptions,
                    CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo("<p>x</p>"));
            Assert.That(stringException.GetMaximumOutputCharacters(),
                Is.EqualTo(7));
            Assert.That(writerException.GetMaximumOutputCharacters(),
                Is.EqualTo(7));
            Assert.That(writer.ToString(), Is.EqualTo("<p>x</p"));
        });
    }

    [Test]
    public void CancellationBetweenWriterOperationsStopsTraversal()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode paragraph = document.CreateElement("p");
        paragraph.Append(document.CreateText("value"));
        CancellationTokenSource source = new CancellationTokenSource();
        CancelAfterFirstWriteWriter writer = new CancelAfterFirstWriteWriter(
            source);

        Assert.Throws<OperationCanceledException>(() =>
            HtmlSerializer.Serialize(paragraph, writer,
                HtmlSerializerOptions.CreateDefault(), source.Token));
        Assert.That(writer.ToString(), Is.EqualTo("<"));
    }

    [Test]
    public void PrettyModeIndentsBlockOnlyDocumentsWithConfiguredWhitespace()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode doctype = document.CreateDocumentTypeStorage(true, "html",
            false, "", false, "", false, HtmlSourceSpan.CreateUnknown());
        HtmlNode html = document.CreateElement("html");
        HtmlNode head = document.CreateElement("head");
        HtmlNode title = document.CreateElement("title");
        HtmlNode body = document.CreateElement("body");
        HtmlNode main = document.CreateElement("main");
        HtmlNode heading = document.CreateElement("h1");
        HtmlNode paragraph = document.CreateElement("p");
        HtmlNode emphasis = document.CreateElement("em");
        title.Append(document.CreateText("Example"));
        heading.Append(document.CreateText("Heading"));
        emphasis.Append(document.CreateText("world"));
        paragraph.Append(document.CreateText("Hello "));
        paragraph.Append(emphasis);
        paragraph.Append(document.CreateText("."));
        head.Append(title);
        main.Append(heading);
        main.Append(paragraph);
        body.Append(main);
        html.Append(head);
        html.Append(body);
        document.GetRoot().Append(doctype);
        document.GetRoot().Append(html);
        HtmlSerializerOptions options = HtmlSerializerOptions.CreatePretty();
        options.SetIndentation("--");
        options.SetNewLine("\r\n");

        string result = HtmlSerializer.Serialize(document.GetRoot(), options,
            CancellationToken.None);

        Assert.That(result, Is.EqualTo(
            "<!DOCTYPE html>\r\n"
            + "<html>\r\n"
            + "--<head>\r\n"
            + "----<title>Example</title>\r\n"
            + "--</head>\r\n"
            + "--<body>\r\n"
            + "----<main>\r\n"
            + "------<h1>Heading</h1>\r\n"
            + "------<p>Hello <em>world</em>.</p>\r\n"
            + "----</main>\r\n"
            + "--</body>\r\n"
            + "</html>"));
    }

    [Test]
    public void PrettyModePreservesSensitiveAndMixedContentWithoutPadding()
    {
        const string SvgUri = "http://www.w3.org/2000/svg";
        HtmlDocument document = CreateDocument();
        HtmlNode body = document.CreateElement("body");
        HtmlNode pre = document.CreateElement("pre");
        HtmlNode div = document.CreateElement("div");
        HtmlNode span = document.CreateElement("span");
        HtmlNode strong = document.CreateElement("strong");
        HtmlNode script = document.CreateElement("script");
        HtmlNode textarea = document.CreateElement("textarea");
        HtmlNode commentedParagraph = document.CreateElement("p");
        HtmlNode svg = document.CreateElementInNamespace(SvgUri, "svg");
        HtmlNode circle = document.CreateElementInNamespace(SvgUri, "circle");
        pre.Append(document.CreateText("  first\nsecond"));
        span.Append(document.CreateText("A"));
        strong.Append(document.CreateText("B"));
        div.Append(span);
        div.Append(document.CreateText(" "));
        div.Append(strong);
        body.Append(pre);
        body.Append(div);
        script.Append(document.CreateText("if (a < b) { x++; }"));
        textarea.Append(document.CreateText("  a < b\n c"));
        commentedParagraph.Append(document.CreateComment("note"));
        svg.Append(circle);
        HtmlSerializerOptions options = HtmlSerializerOptions.CreatePretty();

        Assert.Multiple(() =>
        {
            Assert.That(HtmlSerializer.Serialize(body, options,
                CancellationToken.None), Is.EqualTo(
                    "<body>\n"
                    + "  <pre>  first\nsecond</pre>\n"
                    + "  <div><span>A</span> <strong>B</strong></div>\n"
                    + "</body>"));
            Assert.That(HtmlSerializer.Serialize(script, options,
                CancellationToken.None), Is.EqualTo(
                    "<script>if (a < b) { x++; }</script>"));
            Assert.That(HtmlSerializer.Serialize(textarea, options,
                CancellationToken.None), Is.EqualTo(
                    "<textarea>  a &lt; b\n c</textarea>"));
            Assert.That(HtmlSerializer.Serialize(commentedParagraph, options,
                CancellationToken.None), Is.EqualTo(
                    "<p><!--note--></p>"));
            Assert.That(HtmlSerializer.Serialize(svg, options,
                CancellationToken.None), Is.EqualTo(
                    "<svg><circle></circle></svg>"));
        });
    }

    [Test]
    public void PrettyModeDoesNotRewriteExistingWhitespaceOnlyTextNodes()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode section = document.CreateElement("section");
        HtmlNode first = document.CreateElement("p");
        HtmlNode second = document.CreateElement("p");
        first.Append(document.CreateText("A"));
        second.Append(document.CreateText("B"));
        section.Append(first);
        section.Append(document.CreateText("\n "));
        section.Append(second);

        Assert.That(HtmlSerializer.Serialize(section,
            HtmlSerializerOptions.CreatePretty(), CancellationToken.None),
            Is.EqualTo("<section><p>A</p>\n <p>B</p></section>"));
    }

    [Test]
    public void CompactModeOmitsSafeDocumentAndParagraphTags()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode doctype = document.CreateDocumentTypeStorage(true, "html",
            false, "", false, "", false, HtmlSourceSpan.CreateUnknown());
        HtmlNode html = document.CreateElement("html");
        HtmlNode head = document.CreateElement("head");
        HtmlNode title = document.CreateElement("title");
        HtmlNode body = document.CreateElement("body");
        HtmlNode first = document.CreateElement("p");
        HtmlNode second = document.CreateElement("p");
        title.Append(document.CreateText("T"));
        first.Append(document.CreateText("A"));
        second.Append(document.CreateText("B"));
        head.Append(title);
        body.Append(first);
        body.Append(second);
        html.Append(head);
        html.Append(body);
        document.GetRoot().Append(doctype);
        document.GetRoot().Append(html);

        string result = HtmlSerializer.Serialize(document.GetRoot(),
            HtmlSerializerOptions.CreateCompact(), CancellationToken.None);

        Assert.That(result,
            Is.EqualTo("<!DOCTYPE html><title>T</title><p>A<p>B"));
    }

    [Test]
    public void CompactModeOmitsSafeListSelectionAndTableTags()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode fragment = document.CreateFragment();
        HtmlNode list = document.CreateElement("ul");
        HtmlNode firstItem = document.CreateElement("li");
        HtmlNode secondItem = document.CreateElement("li");
        HtmlNode select = document.CreateElement("select");
        HtmlNode firstOption = document.CreateElement("option");
        HtmlNode secondOption = document.CreateElement("option");
        HtmlNode table = document.CreateElement("table");
        HtmlNode colgroup = document.CreateElement("colgroup");
        HtmlNode column = document.CreateElement("col");
        HtmlNode tbody = document.CreateElement("tbody");
        HtmlNode firstRow = document.CreateElement("tr");
        HtmlNode secondRow = document.CreateElement("tr");
        HtmlNode firstCell = document.CreateElement("td");
        HtmlNode secondCell = document.CreateElement("td");
        HtmlNode thirdCell = document.CreateElement("td");
        firstItem.Append(document.CreateText("A"));
        secondItem.Append(document.CreateText("B"));
        list.Append(firstItem);
        list.Append(secondItem);
        firstOption.Append(document.CreateText("A"));
        secondOption.Append(document.CreateText("B"));
        select.Append(firstOption);
        select.Append(secondOption);
        colgroup.Append(column);
        firstCell.Append(document.CreateText("A"));
        secondCell.Append(document.CreateText("B"));
        thirdCell.Append(document.CreateText("C"));
        firstRow.Append(firstCell);
        firstRow.Append(secondCell);
        secondRow.Append(thirdCell);
        tbody.Append(firstRow);
        tbody.Append(secondRow);
        table.Append(colgroup);
        table.Append(tbody);
        fragment.Append(list);
        fragment.Append(select);
        fragment.Append(table);

        string result = HtmlSerializer.Serialize(fragment,
            HtmlSerializerOptions.CreateCompact(), CancellationToken.None);

        Assert.That(result, Is.EqualTo(
            "<ul><li>A<li>B</ul>"
            + "<select><option>A<option>B</select>"
            + "<table><col><tr><td>A<td>B<tr><td>C</table>"));
    }

    [Test]
    public void OptionalTagsRemainWhenAttributesOrBoundariesRequireThem()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode html = document.CreateElement("html");
        HtmlNode head = document.CreateElement("head");
        HtmlNode title = document.CreateElement("title");
        HtmlNode body = document.CreateElement("body");
        HtmlNode paragraph = document.CreateElement("p");
        html.SetAttribute("lang", "en");
        body.SetAttribute("class", "page");
        title.Append(document.CreateText("T"));
        paragraph.Append(document.CreateText("A"));
        head.Append(title);
        body.Append(paragraph);
        html.Append(head);
        html.Append(body);
        document.GetRoot().Append(html);
        HtmlDocument guardedDocument = CreateDocument();
        HtmlNode guardedHtml = guardedDocument.CreateElement("html");
        HtmlNode guardedHead = guardedDocument.CreateElement("head");
        HtmlNode guardedTitle = guardedDocument.CreateElement("title");
        HtmlNode guardedBody = guardedDocument.CreateElement("body");
        HtmlNode guardedParagraph = guardedDocument.CreateElement("p");
        guardedTitle.Append(guardedDocument.CreateText("T"));
        guardedParagraph.Append(guardedDocument.CreateText("A"));
        guardedHead.Append(guardedTitle);
        guardedBody.Append(guardedParagraph);
        guardedHtml.Append(guardedDocument.CreateComment("first"));
        guardedHtml.Append(guardedHead);
        guardedHtml.Append(guardedDocument.CreateText(" "));
        guardedHtml.Append(guardedBody);
        guardedDocument.GetRoot().Append(guardedHtml);
        HtmlSerializerOptions compact = HtmlSerializerOptions.CreateCompact();

        Assert.Multiple(() =>
        {
            Assert.That(HtmlSerializer.Serialize(document.GetRoot(), compact,
                CancellationToken.None), Is.EqualTo(
                    "<html lang=\"en\"><title>T</title>"
                    + "<body class=\"page\"><p>A"));
            Assert.That(HtmlSerializer.Serialize(guardedDocument.GetRoot(),
                compact, CancellationToken.None), Is.EqualTo(
                    "<html><!--first--><title>T</title></head> "
                    + "<body><p>A"));
        });
    }

    [Test]
    public void OptionalEndTagsRemainAcrossWhitespaceAndSensitiveParents()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode fragment = document.CreateFragment();
        HtmlNode list = document.CreateElement("ul");
        HtmlNode firstItem = document.CreateElement("li");
        HtmlNode secondItem = document.CreateElement("li");
        HtmlNode anchor = document.CreateElement("a");
        HtmlNode paragraph = document.CreateElement("p");
        firstItem.Append(document.CreateText("A"));
        secondItem.Append(document.CreateText("B"));
        list.Append(firstItem);
        list.Append(document.CreateText(" "));
        list.Append(secondItem);
        paragraph.Append(document.CreateText("C"));
        anchor.Append(paragraph);
        fragment.Append(list);
        fragment.Append(anchor);

        string result = HtmlSerializer.Serialize(fragment,
            HtmlSerializerOptions.CreateCompact(), CancellationToken.None);

        Assert.That(result,
            Is.EqualTo("<ul><li>A</li> <li>B</ul><a><p>C</p></a>"));
    }

    static HtmlDocument CreateDocument()
    {
        return new HtmlDocument("", "", HtmlDocumentMode.NoQuirks,
            Array.Empty<HtmlDiagnostic>(), HtmlSourceSpan.CreateUnknown());
    }
}

class CancelAfterFirstWriteWriter : StringWriter
{
    CancellationTokenSource source;
    bool canceled;

    public CancelAfterFirstWriteWriter(CancellationTokenSource source)
    {
        this.source = source;
        this.canceled = false;
    }

    public override void Write(char value)
    {
        base.Write(value);
        if (!this.canceled)
        {
            this.canceled = true;
            this.source.Cancel();
        }
    }
}
