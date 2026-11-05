
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlTreeBuilderForeignContentTests
{
    const string SvgNamespace = "http://www.w3.org/2000/svg";
    const string MathMlNamespace = "http://www.w3.org/1998/Math/MathML";
    const string XLinkNamespace = "http://www.w3.org/1999/xlink";
    const string XmlNamespace = "http://www.w3.org/XML/1998/namespace";
    const string XmlnsNamespace = "http://www.w3.org/2000/xmlns/";

    [Test]
    public void SvgNamesAndAttributesAreAdjustedBeforeIntegrationPoints()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode body = EnterBody(builder, document);
        HtmlAttribute viewBox = Attribute("viewbox", "0 0 10 10", 40);
        HtmlToken svgToken = Start("svg", new[]
        {
            viewBox,
            Attribute("xlink:href", "target", 41),
            Attribute("xml:lang", "en", 42),
            Attribute("xmlns", SvgNamespace, 43),
            Attribute("xmlns:xlink", XLinkNamespace, 44)
        }, false, 40);

        Process(builder, svgToken);
        HtmlNode svg = body.GetLastChild();
        Process(builder, Start("lineargradient", new[]
        {
            Attribute("gradientunits", "userSpaceOnUse", 45)
        }, false, 45));
        HtmlNode gradient = svg.GetLastChild();
        Process(builder, End("lineargradient", 46));
        Process(builder, Start("foreignobject", 47));
        HtmlNode foreignObject = svg.GetLastChild();
        Process(builder, Start("div", 48));
        HtmlNode div = foreignObject.GetLastChild();

        Assert.Multiple(() =>
        {
            Assert.That(svg.GetNamespaceUri(), Is.EqualTo(SvgNamespace));
            Assert.That(svg.GetAttributeInNamespace("", "viewBox"),
                Is.EqualTo("0 0 10 10"));
            Assert.That(svg.GetAttributeInNamespace(XLinkNamespace, "href"),
                Is.EqualTo("target"));
            Assert.That(svg.GetAttributeInNamespace(XmlNamespace, "lang"),
                Is.EqualTo("en"));
            Assert.That(svg.GetAttributeInNamespace(XmlnsNamespace, "xmlns"),
                Is.EqualTo(SvgNamespace));
            Assert.That(svg.GetAttributeInNamespace(XmlnsNamespace, "xlink"),
                Is.EqualTo(XLinkNamespace));
            Assert.That(svg.GetAttributes()[0].GetSourceSpan(),
                Is.SameAs(viewBox.GetSourceSpan()));
            Assert.That(gradient.GetLocalName(),
                Is.EqualTo("linearGradient"));
            Assert.That(gradient.GetAttributeInNamespace("", "gradientUnits"),
                Is.EqualTo("userSpaceOnUse"));
            Assert.That(foreignObject.GetLocalName(),
                Is.EqualTo("foreignObject"));
            Assert.That(div.GetNamespaceStorage(),
                Is.EqualTo(HtmlNamespace.Html));
            Assert.That(builder.DidLastDispatchUseForeignRules(), Is.False);
            Assert.That(builder.GetDiagnostics(), Is.Empty);
        });

        Process(builder, End("div", 49));
        Process(builder, End("foreignobject", 50));
        Assert.That(builder.DidLastDispatchUseForeignRules(), Is.True);
        Process(builder, End("svg", 51));
        Assert.That(builder.GetOpenElements().GetCurrentNode(),
            Is.SameAs(body));
    }

    [Test]
    public void MathMlTextAndAnnotationIntegrationSwitchBackToHtmlRules()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode body = EnterBody(builder, document);

        Process(builder, Start("math", new[]
        {
            Attribute("definitionurl", "terms", 40)
        }, false, 40));
        HtmlNode math = body.GetLastChild();
        Process(builder, Start("mi", 41));
        HtmlNode mi = math.GetLastChild();
        Process(builder, Start("mglyph", 42));
        HtmlNode glyph = mi.GetLastChild();
        Process(builder, End("mglyph", 43));
        Process(builder, Start("span", 44));
        HtmlNode span = mi.GetLastChild();
        Process(builder, End("span", 45));
        Process(builder, End("mi", 46));

        Process(builder, Start("annotation-xml", new[]
        {
            Attribute("encoding", "TEXT/HTML", 47)
        }, false, 47));
        HtmlNode annotation = math.GetLastChild();
        Process(builder, Start("div", 48));
        HtmlNode div = annotation.GetLastChild();
        Process(builder, End("div", 49));
        Process(builder, End("annotation-xml", 50));

        Process(builder, Start("annotation-xml", 51));
        HtmlNode svgAnnotation = math.GetLastChild();
        Process(builder, Start("svg", 52));
        HtmlNode svg = svgAnnotation.GetLastChild();

        Assert.Multiple(() =>
        {
            Assert.That(math.GetNamespaceUri(), Is.EqualTo(MathMlNamespace));
            Assert.That(math.GetAttributeInNamespace("", "definitionURL"),
                Is.EqualTo("terms"));
            Assert.That(glyph.GetNamespaceStorage(),
                Is.EqualTo(HtmlNamespace.MathMl));
            Assert.That(span.GetNamespaceStorage(),
                Is.EqualTo(HtmlNamespace.Html));
            Assert.That(div.GetNamespaceStorage(),
                Is.EqualTo(HtmlNamespace.Html));
            Assert.That(svg.GetNamespaceStorage(),
                Is.EqualTo(HtmlNamespace.Svg));
            Assert.That(builder.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void HtmlBreakoutTagsPopForeignElementsAndReprocessInBody()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode body = EnterBody(builder, document);

        Process(builder, Start("svg", 40));
        HtmlNode firstSvg = body.GetLastChild();
        Process(builder, Start("g", 41));
        Process(builder, Start("p", 42));
        HtmlNode paragraph = body.GetLastChild();
        Process(builder, End("p", 43));

        Process(builder, Start("svg", 44));
        HtmlNode secondSvg = body.GetLastChild();
        Process(builder, Start("font", 45));
        HtmlNode svgFont = secondSvg.GetLastChild();
        Process(builder, End("font", 46));
        Process(builder, Start("g", 47));
        Process(builder, Start("font", new[]
        {
            Attribute("color", "red", 48)
        }, false, 48));
        HtmlNode htmlFont = body.GetLastChild();

        Assert.Multiple(() =>
        {
            Assert.That(firstSvg.GetLastChild().GetLocalName(),
                Is.EqualTo("g"));
            Assert.That(paragraph.GetNamespaceStorage(),
                Is.EqualTo(HtmlNamespace.Html));
            Assert.That(svgFont.GetNamespaceStorage(),
                Is.EqualTo(HtmlNamespace.Svg));
            Assert.That(htmlFont.GetLocalName(), Is.EqualTo("font"));
            Assert.That(htmlFont.GetNamespaceStorage(),
                Is.EqualTo(HtmlNamespace.Html));
            Assert.That(builder.GetDiagnostics(), Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void ForeignEndTagsRecoverAcrossNestedAndHtmlBoundaries()
    {
        HtmlDocument nestedDocument = CreateDocument();
        HtmlTreeBuilder nestedBuilder = CreateBuilder(nestedDocument);
        HtmlNode nestedBody = EnterBody(nestedBuilder, nestedDocument);
        Process(nestedBuilder, Start("svg", 40));
        HtmlNode svg = nestedBody.GetLastChild();
        Process(nestedBuilder, Start("g", 41));
        Process(nestedBuilder, End("SVG", 42));

        Assert.Multiple(() =>
        {
            Assert.That(svg.GetLastChild().GetLocalName(), Is.EqualTo("g"));
            Assert.That(nestedBuilder.GetOpenElements().GetCurrentNode(),
                Is.SameAs(nestedBody));
            Assert.That(nestedBuilder.GetDiagnostics(), Has.Length.EqualTo(1));
        });

        HtmlDocument breakoutDocument = CreateDocument();
        HtmlTreeBuilder breakoutBuilder = CreateBuilder(breakoutDocument);
        HtmlNode breakoutBody = EnterBody(breakoutBuilder, breakoutDocument);
        Process(breakoutBuilder, Start("svg", 40));
        Process(breakoutBuilder, Start("g", 41));
        Process(breakoutBuilder, End("p", 42));

        Assert.Multiple(() =>
        {
            Assert.That(breakoutBody.GetChildElements().Select(node =>
                node.GetLocalName()), Is.EqualTo(new[] { "svg", "p" }));
            Assert.That(breakoutBuilder.GetOpenElements().GetCurrentNode(),
                Is.SameAs(breakoutBody));
            Assert.That(breakoutBuilder.GetDiagnostics(),
                Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void ForeignCharactersSelfClosingElementsAndScriptsUseForeignRules()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode body = EnterBody(builder, document);

        Process(builder, Start("svg", 40));
        HtmlNode svg = body.GetLastChild();
        Process(builder, Character(" \0value", 41));
        Process(builder, HtmlToken.CreateComment("note", Span(42)));
        Process(builder, HtmlToken.CreateDoctype("html", false, Span(43)));
        HtmlToken circleToken = Start("circle", Array.Empty<HtmlAttribute>(),
            true, 44);
        Process(builder, circleToken);
        Process(builder, Start("script", 45));
        HtmlNode script = svg.GetLastChild();
        Process(builder, Character("data", 46));
        Process(builder, End("script", 47));
        HtmlToken scriptToken = Start("script", Array.Empty<HtmlAttribute>(),
            true, 48);
        Process(builder, scriptToken);

        Assert.Multiple(() =>
        {
            Assert.That(svg.GetFirstChild().GetTextContent(),
                Is.EqualTo(" \uFFFDvalue"));
            Assert.That(svg.GetChildren().Count(node =>
                node.GetKind() == HtmlNodeKind.Comment), Is.EqualTo(1));
            Assert.That(circleToken.IsSelfClosingAcknowledged(), Is.True);
            Assert.That(svg.GetChildElements().Any(node =>
                node.GetLocalName() == "circle"), Is.True);
            Assert.That(script.IsScriptInert(), Is.True);
            Assert.That(script.GetTextContent(), Is.EqualTo("data"));
            Assert.That(scriptToken.IsSelfClosingAcknowledged(), Is.True);
            Assert.That(builder.GetOpenElements().GetCurrentNode(),
                Is.SameAs(svg));
            Assert.That(builder.GetDiagnostics(), Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void NamespaceDeclarationMismatchesAreDiagnosedButPreserved()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode body = EnterBody(builder, document);

        Process(builder, Start("svg", new[]
        {
            Attribute("xmlns", SvgNamespace, 40),
            Attribute("xmlns:xlink", XLinkNamespace, 41)
        }, false, 40));
        Process(builder, Start("g", new[]
        {
            Attribute("xmlns", "urn:wrong", 42),
            Attribute("xmlns:xlink", "urn:wrong", 43)
        }, false, 42));
        HtmlNode group = body.GetLastChild().GetLastChild();

        Assert.Multiple(() =>
        {
            Assert.That(group.GetAttributeInNamespace(XmlnsNamespace, "xmlns"),
                Is.EqualTo("urn:wrong"));
            Assert.That(group.GetAttributeInNamespace(XmlnsNamespace, "xlink"),
                Is.EqualTo("urn:wrong"));
            Assert.That(builder.GetDiagnostics(), Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void CdataIsAllowedExactlyWhenAdjustedCurrentNodeIsForeign()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        EnterBody(builder, document);
        Assert.That(builder.IsCdataAllowed(), Is.False);

        Process(builder, Start("svg", 40));
        Assert.That(builder.IsCdataAllowed(), Is.True);
        Process(builder, Start("foreignobject", 41));
        Assert.That(builder.IsCdataAllowed(), Is.True);
        Process(builder, Start("div", 42));
        Assert.That(builder.IsCdataAllowed(), Is.False);
        Process(builder, End("div", 43));
        Assert.That(builder.IsCdataAllowed(), Is.True);
        Process(builder, End("foreignobject", 44));
        Process(builder, End("svg", 45));
        Assert.That(builder.IsCdataAllowed(), Is.False);
    }

    static HtmlNode EnterBody(HtmlTreeBuilder builder, HtmlDocument document)
    {
        Process(builder, HtmlToken.CreateDoctype("html", false, Span(0)));
        Process(builder, Start("html", 1));
        Process(builder, Start("head", 2));
        Process(builder, End("head", 3));
        Process(builder, Start("body", 4));
        return document.GetBody();
    }

    static void Process(HtmlTreeBuilder builder, HtmlToken token)
    {
        Assert.That(builder.ProcessToken(token), Is.True);
    }

    static HtmlTreeBuilder CreateBuilder(HtmlDocument document)
    {
        return new HtmlTreeBuilder(document,
            new HtmlBudget(HtmlLimits.CreateDefault(), CancellationToken.None),
            null, null);
    }

    static HtmlDocument CreateDocument()
    {
        return new HtmlDocument("", "", HtmlDocumentMode.NoQuirks,
            Array.Empty<HtmlDiagnostic>(), HtmlSourceSpan.CreateUnknown());
    }

    static HtmlAttribute Attribute(string name, string value, int offset)
    {
        return HtmlAttribute.Create(name, value, Span(offset));
    }

    static HtmlToken Start(string name, int offset)
    {
        return Start(name, Array.Empty<HtmlAttribute>(), false, offset);
    }

    static HtmlToken Start(string name, HtmlAttribute[] attributes,
        bool selfClosing, int offset)
    {
        return HtmlToken.CreateStartTag(name, attributes, selfClosing,
            Span(offset));
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
