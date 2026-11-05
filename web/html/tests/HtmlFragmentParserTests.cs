
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlFragmentParserTests
{
    const string SvgNamespace = "http://www.w3.org/2000/svg";
    const string MathMlNamespace = "http://www.w3.org/1998/Math/MathML";

    [TestCase("title", 1, true)]
    [TestCase("textarea", 1, true)]
    [TestCase("style", 2, true)]
    [TestCase("xmp", 2, true)]
    [TestCase("iframe", 2, true)]
    [TestCase("noembed", 2, true)]
    [TestCase("noframes", 2, true)]
    [TestCase("noscript", 2, true)]
    [TestCase("noscript", 0, false)]
    [TestCase("script", 47, true)]
    [TestCase("plaintext", 3, true)]
    [TestCase("div", 0, true)]
    public void HtmlContextSelectsItsRequiredTokenizerState(
        string contextName, int expectedState, bool scriptingEnabled)
    {
        HtmlDocument document = CreateDocument();
        HtmlNode context = document.CreateElement(contextName);
        HtmlFragmentParser parser = CreateParser(context, scriptingEnabled);

        Assert.Multiple(() =>
        {
            Assert.That((int)parser.GetTokenizerState(),
                Is.EqualTo(expectedState));
            Assert.That(parser.GetTreeBuilder().IsScriptingEnabled(),
                Is.EqualTo(scriptingEnabled));
        });
    }

    [TestCase("select", 15)]
    [TestCase("table", 8)]
    [TestCase("tbody", 12)]
    [TestCase("thead", 12)]
    [TestCase("tfoot", 12)]
    [TestCase("tr", 13)]
    [TestCase("caption", 10)]
    [TestCase("colgroup", 11)]
    [TestCase("template", 17)]
    [TestCase("body", 6)]
    [TestCase("frameset", 19)]
    [TestCase("html", 2)]
    [TestCase("td", 6)]
    [TestCase("th", 6)]
    [TestCase("head", 6)]
    [TestCase("div", 6)]
    public void HtmlContextResetsToTheAppropriateInsertionMode(
        string contextName, int expectedMode)
    {
        HtmlDocument document = CreateDocument();
        HtmlNode context = document.CreateElement(contextName);
        HtmlFragmentParser parser = CreateParser(context, true);

        Assert.Multiple(() =>
        {
            Assert.That((int)parser.GetTreeBuilder().GetInsertionMode(),
                Is.EqualTo(expectedMode));
            Assert.That(parser.GetTreeBuilder().GetOpenElements().GetNodes(),
                Is.EqualTo(new[] { parser.GetSyntheticRoot() }));
            Assert.That(parser.GetTreeBuilder().GetOpenElements()
                .GetAdjustedCurrentNode(), Is.SameAs(context));
            Assert.That(parser.GetTreeBuilder().GetTemplateInsertionModeCount(),
                Is.EqualTo(contextName == "template" ? 1 : 0));
        });
    }

    [Test]
    public void InitializedTokenizerUsesContextEndTagAndNamespaceRules()
    {
        HtmlDocument htmlDocument = CreateDocument();
        HtmlFragmentParser titleParser = CreateParser(
            htmlDocument.CreateElement("title"), true);
        HtmlTokenizer titleTokenizer = CreateTokenizer();
        titleParser.InitializeTokenizer(titleTokenizer);
        titleTokenizer.Feed("A&amp;</TiTlE><b>".AsSpan(), true);

        HtmlTokenReadResult titleText = titleTokenizer.Read();
        HtmlTokenReadResult titleEnd = titleTokenizer.Read();
        HtmlTokenReadResult followingTag = titleTokenizer.Read();
        Assert.Multiple(() =>
        {
            Assert.That(titleText.GetToken().GetKind(),
                Is.EqualTo(HtmlTokenKind.Character));
            Assert.That(titleText.GetToken().GetData(), Is.EqualTo("A&"));
            Assert.That(titleEnd.GetToken().GetKind(),
                Is.EqualTo(HtmlTokenKind.EndTag));
            Assert.That(titleEnd.GetToken().GetName(), Is.EqualTo("title"));
            Assert.That(followingTag.GetToken().GetKind(),
                Is.EqualTo(HtmlTokenKind.StartTag));
        });

        HtmlDocument svgDocument = CreateDocument();
        HtmlNode svgTitle = svgDocument.CreateElementInNamespace(SvgNamespace, "title");
        HtmlFragmentParser svgParser = CreateParser(svgTitle, true);
        HtmlTokenizer svgTokenizer = CreateTokenizer();
        svgParser.InitializeTokenizer(svgTokenizer);
        svgTokenizer.Feed("<b>".AsSpan(), true);

        Assert.Multiple(() =>
        {
            Assert.That(svgParser.GetTokenizerState(),
                Is.EqualTo(HtmlTokenizerState.Data));
            Assert.That(svgTokenizer.Read().GetToken().GetKind(),
                Is.EqualTo(HtmlTokenKind.StartTag));
        });
    }

    [Test]
    public void TableSelectAndForeignContextsBuildIntoOwnedFragment()
    {
        HtmlDocument tableDocument = CreateDocument();
        HtmlFragmentParser tableParser = CreateParser(
            tableDocument.CreateElement("table"), true);
        Process(tableParser, Start("tr", 1));
        HtmlNode tableBody = tableParser.GetFragment().GetFirstChild();

        HtmlDocument selectDocument = CreateDocument();
        HtmlFragmentParser selectParser = CreateParser(
            selectDocument.CreateElement("select"), true);
        Process(selectParser, Start("option", 1));
        Process(selectParser, Character("value", 2));
        HtmlNode option = selectParser.GetFragment().GetFirstChild();

        HtmlDocument svgDocument = CreateDocument();
        HtmlFragmentParser svgParser = CreateParser(
            svgDocument.CreateElementInNamespace(SvgNamespace, "svg"), true);
        Process(svgParser, Start("lineargradient", 1));
        HtmlNode gradient = svgParser.GetFragment().GetFirstChild();

        HtmlDocument mathDocument = CreateDocument();
        HtmlFragmentParser mathParser = CreateParser(
            mathDocument.CreateElementInNamespace(MathMlNamespace, "mi"), true);
        Process(mathParser, Start("span", 1));
        HtmlNode span = mathParser.GetFragment().GetFirstChild();

        Assert.Multiple(() =>
        {
            Assert.That(tableBody.GetLocalName(), Is.EqualTo("tbody"));
            Assert.That(tableBody.GetFirstChild().GetLocalName(),
                Is.EqualTo("tr"));
            Assert.That(tableBody.GetOwnerDocument(),
                Is.SameAs(tableDocument));
            Assert.That(option.GetLocalName(), Is.EqualTo("option"));
            Assert.That(option.GetTextContent(), Is.EqualTo("value"));
            Assert.That(option.GetOwnerDocument(),
                Is.SameAs(selectDocument));
            Assert.That(gradient.GetLocalName(),
                Is.EqualTo("linearGradient"));
            Assert.That(gradient.GetNamespaceStorage(),
                Is.EqualTo(HtmlNamespace.Svg));
            Assert.That(gradient.GetOwnerDocument(),
                Is.SameAs(svgDocument));
            Assert.That(span.GetNamespaceStorage(),
                Is.EqualTo(HtmlNamespace.Html));
            Assert.That(span.GetOwnerDocument(), Is.SameAs(mathDocument));
        });
    }

    [Test]
    public void FragmentAndInsertedNodesRetainTheContextDocumentOwnership()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode context = document.CreateElement("div");
        HtmlFragmentParser parser = CreateParser(context, true);
        Process(parser, Start("section", 1));

        HtmlNode fragment = parser.GetFragment();
        HtmlNode section = fragment.GetFirstChild();
        Assert.Multiple(() =>
        {
            Assert.That(fragment.GetKind(), Is.EqualTo(HtmlNodeKind.Fragment));
            Assert.That(fragment.GetOwnerDocument(), Is.SameAs(document));
            Assert.That(section.GetOwnerDocument(), Is.SameAs(document));
            Assert.That(section.GetParent(), Is.SameAs(fragment));
            Assert.That(parser.GetSyntheticRoot().GetOwnerDocument(),
                Is.SameAs(document));
            Assert.That(parser.GetSyntheticRoot().GetParent(), Is.Null);
            Assert.That(parser.GetSyntheticRoot().GetChildren(), Is.Empty);
            Assert.That(document.GetRoot().GetChildren(), Is.Empty);
        });
    }

    [Test]
    public void NearestFormAncestorSeedsTheFormPointer()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode form = document.CreateElement("form");
        HtmlNode section = document.CreateElement("section");
        HtmlNode context = document.CreateElement("span");
        form.Append(section);
        section.Append(context);
        HtmlFragmentParser parser = CreateParser(context, true);
        HtmlTreeBuilder builder = parser.GetTreeBuilder();

        Assert.That(builder.GetFormElement(), Is.SameAs(form));
        Process(parser, Start("form", 1));
        Assert.That(parser.GetFragment().GetChildren(), Is.Empty);
        Process(parser, End("form", 2));
        Assert.That(builder.GetFormElement(), Is.Null);
        Process(parser, Start("form", 3));

        Assert.Multiple(() =>
        {
            Assert.That(parser.GetFragment().GetFirstChild().GetLocalName(),
                Is.EqualTo("form"));
            Assert.That(parser.GetFragment().GetFirstChild()
                .GetOwnerDocument(), Is.SameAs(document));
            Assert.That(builder.GetFormElement(),
                Is.SameAs(parser.GetFragment().GetFirstChild()));
            Assert.That(builder.GetDiagnostics(), Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void TemplateContentUsesItsHostAsContext()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode template = document.CreateElement("template");
        HtmlFragmentParser parser = CreateParser(
            template.GetTemplateContent(), true);

        Assert.Multiple(() =>
        {
            Assert.That(parser.GetContextElement(), Is.SameAs(template));
            Assert.That(parser.GetFragment(),
                Is.Not.SameAs(template.GetTemplateContent()));
            Assert.That(parser.GetFragment().GetOwnerDocument(),
                Is.SameAs(document));
            Assert.That(parser.GetTreeBuilder().GetInsertionMode(),
                Is.EqualTo(HtmlInsertionMode.InTemplate));
            Assert.That(parser.GetTreeBuilder().GetCurrentTemplateInsertionMode(),
                Is.EqualTo(HtmlInsertionMode.InTemplate));
        });
    }

    [Test]
    public void FragmentTokenizerCdataFollowsTheForeignContext()
    {
        HtmlDocument svgDocument = CreateDocument();
        HtmlFragmentParser svgParser = CreateParser(
            svgDocument.CreateElementInNamespace(SvgNamespace, "circle"), true);
        HtmlTokenizer svgTokenizer = CreateTokenizer();
        svgParser.InitializeTokenizer(svgTokenizer);
        svgTokenizer.Feed("<![CDATA[value]]>".AsSpan(), true);

        HtmlDocument htmlDocument = CreateDocument();
        HtmlFragmentParser htmlParser = CreateParser(
            htmlDocument.CreateElement("div"), true);
        HtmlTokenizer htmlTokenizer = CreateTokenizer();
        htmlParser.InitializeTokenizer(htmlTokenizer);
        htmlTokenizer.Feed("<![CDATA[value]]>".AsSpan(), true);

        Assert.Multiple(() =>
        {
            Assert.That(svgTokenizer.Read().GetToken().GetKind(),
                Is.EqualTo(HtmlTokenKind.Character));
            Assert.That(htmlTokenizer.Read().GetToken().GetKind(),
                Is.EqualTo(HtmlTokenKind.Comment));
        });

        HtmlDocument integrationDocument = CreateDocument();
        HtmlFragmentParser integrationParser = CreateParser(
            integrationDocument.CreateElementInNamespace(SvgNamespace, "svg"), true);
        HtmlTokenizer integrationTokenizer = CreateTokenizer();
        integrationParser.InitializeTokenizer(integrationTokenizer);
        Process(integrationParser, Start("foreignobject", 1));
        Process(integrationParser, Start("div", 2));
        integrationParser.UpdateTokenizerCdata(integrationTokenizer);
        integrationTokenizer.Feed("<![CDATA[value]]>".AsSpan(), true);
        Assert.That(integrationTokenizer.Read().GetToken().GetKind(),
            Is.EqualTo(HtmlTokenKind.Comment));
    }

    static HtmlFragmentParser CreateParser(HtmlNode target,
        bool scriptingEnabled)
    {
        return HtmlFragmentParser.Create(target, scriptingEnabled,
            new HtmlBudget(HtmlLimits.CreateDefault(),
                CancellationToken.None));
    }

    static HtmlTokenizer CreateTokenizer()
    {
        return HtmlTokenizer.Create(HtmlTokenizerOptions.CreateDefault());
    }

    static void Process(HtmlFragmentParser parser, HtmlToken token)
    {
        Assert.That(parser.GetTreeBuilder().ProcessToken(token), Is.True);
    }

    static HtmlDocument CreateDocument()
    {
        return new HtmlDocument("", "", HtmlDocumentMode.NoQuirks,
            Array.Empty<HtmlDiagnostic>(), HtmlSourceSpan.CreateUnknown());
    }

    static HtmlToken Start(string name, int offset)
    {
        return HtmlToken.CreateStartTag(name, Array.Empty<HtmlAttribute>(),
            false, Span(offset));
    }

    static HtmlToken End(string name, int offset)
    {
        return HtmlToken.CreateEndTag(name, Array.Empty<HtmlAttribute>(),
            false, Span(offset));
    }

    static HtmlToken Character(string data, int offset)
    {
        return HtmlToken.CreateCharacter(data, Span(offset));
    }

    static HtmlSourceSpan Span(int offset)
    {
        return HtmlSourceSpan.Create(
            HtmlSourcePosition.Create(offset, 1, offset + 1),
            HtmlSourcePosition.Create(offset + 1, 1, offset + 2));
    }
}
