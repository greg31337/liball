
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlTreeBuilderInBodyModeTests
{
    [Test]
    public void CharacterRulesIgnoreNullsReconstructTextAndUpdateFramesetFlag()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode body = EnterBody(builder, document);
        builder.SetFramesetOk(true);

        Assert.That(builder.ProcessToken(Character(" \n", 30, 32)), Is.True);
        Assert.That(builder.IsFramesetOk(), Is.True);
        Assert.That(builder.ProcessToken(Character("\0value", 32, 38)),
            Is.True);

        Assert.Multiple(() =>
        {
            Assert.That(body.GetTextContent(), Is.EqualTo(" \nvalue"));
            Assert.That(body.GetChildren(), Has.Length.EqualTo(1));
            Assert.That(builder.IsFramesetOk(), Is.False);
            Assert.That(builder.GetDiagnostics(), Has.Length.EqualTo(1));
            Assert.That(builder.GetDiagnostics()[0].GetSpan()
                .GetStart().GetNormalizedCharacterOffset(), Is.EqualTo(32));
        });
    }

    [Test]
    public void DuplicateHtmlAndBodyTagsMergeOnlyMissingAttributes()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        Assert.That(builder.ProcessToken(Doctype(0, 15)), Is.True);
        HtmlToken htmlToken = Start("html", new[]
        {
            Attribute("id", "original", 15)
        }, 15, 25);
        Assert.That(builder.ProcessToken(htmlToken), Is.True);
        Assert.That(builder.ProcessToken(Start("head", 25, 31)), Is.True);
        Assert.That(builder.ProcessToken(End("head", 31, 38)), Is.True);
        HtmlToken bodyToken = Start("body", new[]
        {
            Attribute("class", "original", 38)
        }, 38, 50);
        Assert.That(builder.ProcessToken(bodyToken), Is.True);

        Assert.That(builder.ProcessToken(Start("html", new[]
        {
            Attribute("id", "replacement", 50),
            Attribute("lang", "en", 51)
        }, 50, 65)), Is.True);
        Assert.That(builder.ProcessToken(Start("body", new[]
        {
            Attribute("class", "replacement", 65),
            Attribute("data-page", "home", 66)
        }, 65, 82)), Is.True);

        HtmlNode html = document.GetDocumentElement();
        HtmlNode body = document.GetBody();
        Assert.Multiple(() =>
        {
            Assert.That(html.GetAttribute("id"), Is.EqualTo("original"));
            Assert.That(html.GetAttribute("lang"), Is.EqualTo("en"));
            Assert.That(body.GetAttribute("class"), Is.EqualTo("original"));
            Assert.That(body.GetAttribute("data-page"), Is.EqualTo("home"));
            Assert.That(builder.GetOpenElements().GetNodes(),
                Is.EqualTo(new[] { html, body }));
            Assert.That(builder.GetDiagnostics(), Has.Length.EqualTo(2));
            Assert.That(document.GetMutationVersionStorage(), Is.Zero);
        });
    }

    [Test]
    public void BlockAndHeadingStartsCloseParagraphsAndPriorHeadings()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode body = EnterBody(builder, document);

        Assert.That(builder.ProcessToken(Start("p", 30, 33)), Is.True);
        Assert.That(builder.ProcessToken(Start("span", 33, 39)), Is.True);
        Assert.That(builder.ProcessToken(Start("div", 39, 44)), Is.True);

        HtmlNode paragraph = body.GetFirstChild();
        HtmlNode div = body.GetLastChild();
        Assert.Multiple(() =>
        {
            Assert.That(paragraph.GetLocalName(), Is.EqualTo("p"));
            Assert.That(paragraph.GetFirstChild().GetLocalName(),
                Is.EqualTo("span"));
            Assert.That(div.GetLocalName(), Is.EqualTo("div"));
            Assert.That(builder.GetOpenElements().GetCurrentNode(),
                Is.SameAs(div));
        });

        Assert.That(builder.ProcessToken(Start("h1", 44, 48)), Is.True);
        HtmlNode firstHeading = div.GetLastChild();
        Assert.That(builder.ProcessToken(Start("h2", 48, 52)), Is.True);
        HtmlNode secondHeading = div.GetLastChild();
        Assert.Multiple(() =>
        {
            Assert.That(firstHeading.GetLocalName(), Is.EqualTo("h1"));
            Assert.That(secondHeading.GetLocalName(), Is.EqualTo("h2"));
            Assert.That(firstHeading.GetNextSibling(), Is.SameAs(secondHeading));
            Assert.That(builder.GetOpenElements().GetCurrentNode(),
                Is.SameAs(secondHeading));
            Assert.That(builder.GetDiagnostics(), Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void ListAndDescriptionStartsImplicitlyClosePriorItems()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode body = EnterBody(builder, document);

        Assert.That(builder.ProcessToken(Start("ul", 30, 34)), Is.True);
        HtmlNode list = body.GetLastChild();
        Assert.That(builder.ProcessToken(Start("li", 34, 38)), Is.True);
        Assert.That(builder.ProcessToken(Character("one", 38, 41)), Is.True);
        Assert.That(builder.ProcessToken(Start("li", 41, 45)), Is.True);
        Assert.That(builder.ProcessToken(Character("two", 45, 48)), Is.True);
        Assert.That(builder.ProcessToken(End("li", 48, 53)), Is.True);
        Assert.That(builder.ProcessToken(End("ul", 53, 58)), Is.True);

        Assert.That(builder.ProcessToken(Start("dl", 58, 62)), Is.True);
        HtmlNode descriptions = body.GetLastChild();
        Assert.That(builder.ProcessToken(Start("dt", 62, 66)), Is.True);
        Assert.That(builder.ProcessToken(Character("term", 66, 70)), Is.True);
        Assert.That(builder.ProcessToken(Start("dd", 70, 74)), Is.True);
        Assert.That(builder.ProcessToken(Character("definition", 74, 84)),
            Is.True);

        Assert.Multiple(() =>
        {
            Assert.That(list.GetChildElements().Select(node =>
                node.GetLocalName()), Is.EqualTo(new[] { "li", "li" }));
            Assert.That(list.GetChildElements().Select(node =>
                node.GetTextContent()), Is.EqualTo(new[] { "one", "two" }));
            Assert.That(descriptions.GetChildElements().Select(node =>
                node.GetLocalName()), Is.EqualTo(new[] { "dt", "dd" }));
            Assert.That(descriptions.GetChildElements().Select(node =>
                node.GetTextContent()),
                Is.EqualTo(new[] { "term", "definition" }));
            Assert.That(builder.IsFramesetOk(), Is.False);
        });
    }

    [Test]
    public void NestedButtonsAndFormsUseScopeBasedRecovery()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode body = EnterBody(builder, document);

        Assert.That(builder.ProcessToken(Start("button", 30, 38)), Is.True);
        HtmlNode firstButton = body.GetLastChild();
        Assert.That(builder.ProcessToken(Start("span", 38, 44)), Is.True);
        Assert.That(builder.ProcessToken(Start("button", 44, 52)), Is.True);
        HtmlNode secondButton = body.GetLastChild();
        Assert.That(builder.ProcessToken(End("button", 52, 61)), Is.True);

        Assert.That(builder.ProcessToken(Start("form", 61, 67)), Is.True);
        HtmlNode form = body.GetLastChild();
        Assert.That(builder.ProcessToken(Start("form", 67, 73)), Is.True);
        Assert.That(builder.ProcessToken(End("form", 73, 80)), Is.True);

        Assert.Multiple(() =>
        {
            Assert.That(firstButton.GetFirstChild().GetLocalName(),
                Is.EqualTo("span"));
            Assert.That(secondButton.GetLocalName(), Is.EqualTo("button"));
            Assert.That(firstButton.GetNextSibling(), Is.SameAs(secondButton));
            Assert.That(form.GetLocalName(), Is.EqualTo("form"));
            Assert.That(body.GetChildElements().Count(node =>
                node.GetLocalName() == "form"), Is.EqualTo(1));
            Assert.That(builder.GetOpenElements().GetCurrentNode(),
                Is.SameAs(body));
            Assert.That(builder.GetDiagnostics(), Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void ActiveFormattingElementsAreReconstructedAfterParagraphClosure()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode body = EnterBody(builder, document);
        HtmlToken boldToken = Start("b", 33, 36);

        Assert.That(builder.ProcessToken(Start("p", 30, 33)), Is.True);
        Assert.That(builder.ProcessToken(boldToken), Is.True);
        Assert.That(builder.ProcessToken(Character("inside", 36, 42)), Is.True);
        Assert.That(builder.ProcessToken(End("p", 42, 46)), Is.True);
        Assert.That(builder.ProcessToken(Character("outside", 46, 53)), Is.True);

        HtmlNode paragraph = body.GetFirstChild();
        HtmlNode reconstructed = body.GetLastChild();
        Assert.Multiple(() =>
        {
            Assert.That(paragraph.GetFirstChild().GetLocalName(),
                Is.EqualTo("b"));
            Assert.That(paragraph.GetTextContent(), Is.EqualTo("inside"));
            Assert.That(reconstructed.GetLocalName(), Is.EqualTo("b"));
            Assert.That(reconstructed.GetTextContent(), Is.EqualTo("outside"));
            Assert.That(reconstructed.GetSourceSpan(),
                Is.SameAs(boldToken.GetSourceSpan()));
            Assert.That(builder.GetFormattingElements().GetElement(0),
                Is.SameAs(reconstructed));
            Assert.That(builder.GetDiagnostics(), Has.Length.EqualTo(1));
        });

        Assert.That(builder.ProcessToken(End("b", 53, 57)), Is.True);
        Assert.That(builder.ProcessToken(Character("plain", 57, 62)), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(builder.GetFormattingElements().GetCount(), Is.Zero);
            Assert.That(body.GetLastChild().GetKind(),
                Is.EqualTo(HtmlNodeKind.Text));
            Assert.That(body.GetLastChild().GetData(), Is.EqualTo("plain"));
        });
    }

    [TestCase("textarea", 1, true)]
    [TestCase("xmp", 2, false)]
    [TestCase("iframe", 2, false)]
    [TestCase("noembed", 2, false)]
    [TestCase("noscript", 2, false)]
    public void BodyTextElementsRequestStateAndRestoreInBodyMode(
        string name, int expectedState, bool stripsLineFeed)
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode body = EnterBody(builder, document);
        if (name == "noscript")
        {
            builder.SetScriptingEnabled(true);
        }

        Assert.That(builder.ProcessToken(Start(name, 30, 40)), Is.True);
        HtmlNode element = builder.GetOpenElements().GetCurrentNode();
        Assert.Multiple(() =>
        {
            Assert.That(builder.GetInsertionMode(),
                Is.EqualTo(HtmlInsertionMode.Text));
            Assert.That(builder.GetOriginalInsertionMode(),
                Is.EqualTo(HtmlInsertionMode.InBody));
            Assert.That((int)builder.GetRequestedTokenizerState(),
                Is.EqualTo(expectedState));
        });

        builder.ClearTokenizerStateRequest();
        string data = stripsLineFeed ? "\nvalue" : "value";
        Assert.That(builder.ProcessToken(Character(data, 40, 46)), Is.True);
        Assert.That(builder.ProcessToken(End(name, 46, 58)), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(element.GetTextContent(), Is.EqualTo("value"));
            Assert.That(builder.GetInsertionMode(),
                Is.EqualTo(HtmlInsertionMode.InBody));
            Assert.That(builder.GetOpenElements().GetCurrentNode(),
                Is.SameAs(body));
        });
    }

    [Test]
    public void PreListingAndPlaintextApplyTokenizerTransitionRules()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode body = EnterBody(builder, document);

        Assert.That(builder.ProcessToken(Start("pre", 30, 35)), Is.True);
        HtmlNode pre = body.GetLastChild();
        Assert.That(builder.ProcessToken(Character("\nfirst", 35, 41)), Is.True);
        Assert.That(builder.ProcessToken(End("pre", 41, 47)), Is.True);

        Assert.That(builder.ProcessToken(Start("plaintext", 47, 58)), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(pre.GetTextContent(), Is.EqualTo("first"));
            Assert.That(builder.GetInsertionMode(),
                Is.EqualTo(HtmlInsertionMode.InBody));
            Assert.That(builder.GetOpenElements().GetCurrentNode()
                .GetLocalName(), Is.EqualTo("plaintext"));
            Assert.That(builder.GetRequestedTokenizerState(),
                Is.EqualTo(HtmlTokenizerState.PlainText));
            Assert.That(builder.IsFramesetOk(), Is.False);
        });
    }

    [Test]
    public void VoidRulesAcknowledgeFlagsAndDistinguishHiddenInputs()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode body = EnterBody(builder, document);
        builder.SetFramesetOk(true);
        HtmlToken hidden = Start("input", new[]
        {
            Attribute("type", "HIDDEN", 30)
        }, true, 30, 45);

        Assert.That(builder.ProcessToken(hidden), Is.True);
        Assert.That(builder.IsFramesetOk(), Is.True);
        Assert.That(builder.ProcessToken(End("br", 45, 50)), Is.True);

        Assert.Multiple(() =>
        {
            Assert.That(hidden.IsSelfClosingAcknowledged(), Is.True);
            Assert.That(body.GetChildElements().Select(node =>
                node.GetLocalName()), Is.EqualTo(new[] { "input", "br" }));
            Assert.That(builder.GetOpenElements().GetCurrentNode(),
                Is.SameAs(body));
            Assert.That(builder.IsFramesetOk(), Is.False);
            Assert.That(builder.GetDiagnostics(), Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void FramesetCanReplaceAnImplicitBodyWhileFramesetRemainsAllowed()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        Assert.That(builder.ProcessToken(Doctype(0, 15)), Is.True);
        Assert.That(builder.ProcessToken(Start("html", 15, 21)), Is.True);
        Assert.That(builder.ProcessToken(Start("head", 21, 27)), Is.True);
        Assert.That(builder.ProcessToken(End("head", 27, 34)), Is.True);
        Assert.That(builder.ProcessToken(Start("span", 34, 40)), Is.True);
        Assert.That(builder.ProcessToken(End("span", 40, 47)), Is.True);
        HtmlNode oldBody = document.GetBody();

        Assert.That(builder.ProcessToken(Start("frameset", 47, 57)), Is.True);

        HtmlNode html = document.GetDocumentElement();
        HtmlNode frameset = html.GetLastChild();
        Assert.Multiple(() =>
        {
            Assert.That(oldBody.GetParent(), Is.Null);
            Assert.That(frameset.GetLocalName(), Is.EqualTo("frameset"));
            Assert.That(html.GetChildElements().Select(node =>
                node.GetLocalName()), Is.EqualTo(new[] { "head", "frameset" }));
            Assert.That(builder.GetOpenElements().GetNodes(),
                Is.EqualTo(new[] { html, frameset }));
            Assert.That(builder.GetInsertionMode(),
                Is.EqualTo(HtmlInsertionMode.InFrameset));
            Assert.That(document.GetMutationVersionStorage(), Is.Zero);
        });
    }

    [Test]
    public void TableSelectAndForeignStartsUseTheirRequiredDestinations()
    {
        HtmlDocument tableDocument = CreateDocument();
        HtmlTreeBuilder tableBuilder = CreateBuilder(tableDocument);
        HtmlNode tableBody = EnterBody(tableBuilder, tableDocument);
        Assert.That(tableBuilder.ProcessToken(Start("p", 30, 33)), Is.True);
        Assert.That(tableBuilder.ProcessToken(Start("table", 33, 40)), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(tableBody.GetChildElements().Select(node =>
                node.GetLocalName()), Is.EqualTo(new[] { "p", "table" }));
            Assert.That(tableBuilder.GetInsertionMode(),
                Is.EqualTo(HtmlInsertionMode.InTable));
        });

        HtmlDocument selectDocument = CreateDocument();
        HtmlTreeBuilder selectBuilder = CreateBuilder(selectDocument);
        EnterBody(selectBuilder, selectDocument);
        Assert.That(selectBuilder.ProcessToken(Start("select", 30, 38)),
            Is.True);
        Assert.That(selectBuilder.GetInsertionMode(),
            Is.EqualTo(HtmlInsertionMode.InSelect));

        HtmlDocument foreignDocument = CreateDocument();
        HtmlTreeBuilder foreignBuilder = CreateBuilder(foreignDocument);
        HtmlNode foreignBody = EnterBody(foreignBuilder, foreignDocument);
        HtmlToken svg = Start("svg", Array.Empty<HtmlAttribute>(), true,
            30, 36);
        Assert.That(foreignBuilder.ProcessToken(svg), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(foreignBody.GetLastChild().GetNamespaceStorage(),
                Is.EqualTo(HtmlNamespace.Svg));
            Assert.That(svg.IsSelfClosingAcknowledged(), Is.True);
            Assert.That(foreignBuilder.GetOpenElements().GetCurrentNode(),
                Is.SameAs(foreignBody));
        });
    }

    [Test]
    public void MissingParagraphEndAndBodyEndRecoverByScope()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode body = EnterBody(builder, document);

        Assert.That(builder.ProcessToken(End("p", 30, 34)), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(body.GetChildren(), Has.Length.EqualTo(1));
            Assert.That(body.GetFirstChild().GetLocalName(), Is.EqualTo("p"));
            Assert.That(body.GetFirstChild().GetSourceSpan().IsKnown(), Is.False);
        });
        Assert.That(builder.ProcessToken(Start("div", 34, 39)), Is.True);
        Assert.That(builder.ProcessToken(End("body", 39, 46)), Is.True);

        Assert.Multiple(() =>
        {
            Assert.That(builder.GetInsertionMode(),
                Is.EqualTo(HtmlInsertionMode.AfterBody));
            Assert.That(builder.GetOpenElements().GetCurrentNode()
                .GetLocalName(), Is.EqualTo("div"));
            Assert.That(builder.GetDiagnostics(), Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void EndOfFileStopsAndReportsUnexpectedOpenElements()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        EnterBody(builder, document);
        Assert.That(builder.ProcessToken(Start("section", 30, 39)), Is.True);
        HtmlToken eof = HtmlToken.CreateEndOfFile(Span(39, 39));

        Assert.That(builder.ProcessToken(eof), Is.True);

        Assert.Multiple(() =>
        {
            Assert.That(builder.IsStopped(), Is.True);
            Assert.That(builder.GetDiagnostics(), Has.Length.EqualTo(1));
            Assert.That(builder.GetDiagnostics()[0].GetSpan(),
                Is.SameAs(eof.GetSourceSpan()));
        });
    }

    [Test]
    public void FormattingReconstructionHonorsTheTreeBuilderNodeBudget()
    {
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumNodes(1);
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = new HtmlTreeBuilder(document,
            new HtmlBudget(limits, CancellationToken.None), null, null);
        HtmlNode html = document.CreateElement("html");
        HtmlNode body = document.CreateElement("body");
        html.InitializeChildrenStorage(new[] { body });
        document.GetRoot().InitializeChildrenStorage(new[] { html });
        Assert.That(builder.GetOpenElements().Push(html), Is.True);
        Assert.That(builder.GetOpenElements().Push(body), Is.True);
        builder.SetInsertionMode(HtmlInsertionMode.InBody);
        HtmlToken token = Start("b", 0, 3);
        HtmlNode oldBold = document.CreateElementStorage(HtmlNamespace.Html,
            HtmlNamespaceData.GetUri(HtmlNamespace.Html), "b", "b", "",
            Array.Empty<HtmlAttribute>(), token.GetSourceSpan());
        builder.GetFormattingElements().Add(oldBold, token);

        Assert.That(builder.ProcessToken(Character("x", 3, 4)), Is.True);

        Assert.Multiple(() =>
        {
            Assert.That(builder.HasTerminalLimitExceeded(), Is.True);
            Assert.That(builder.GetDiagnostics().Single().GetCode(),
                Is.EqualTo(HtmlDiagnosticCode.ResourceLimitExceeded));
            Assert.That(body.GetChildren(), Is.Empty);
            Assert.That(builder.GetFormattingElements().GetElement(0),
                Is.SameAs(oldBold));
        });
    }

    static HtmlNode EnterBody(HtmlTreeBuilder builder, HtmlDocument document)
    {
        Assert.That(builder.ProcessToken(Doctype(0, 15)), Is.True);
        Assert.That(builder.ProcessToken(Start("html", 15, 21)), Is.True);
        Assert.That(builder.ProcessToken(Start("head", 21, 27)), Is.True);
        Assert.That(builder.ProcessToken(End("head", 27, 34)), Is.True);
        Assert.That(builder.ProcessToken(Start("body", 34, 40)), Is.True);
        return document.GetBody();
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

    static HtmlToken Doctype(int start, int end)
    {
        return HtmlToken.CreateDoctype("html", false, Span(start, end));
    }

    static HtmlToken Start(string name, int start, int end)
    {
        return Start(name, Array.Empty<HtmlAttribute>(), false, start, end);
    }

    static HtmlToken Start(string name, HtmlAttribute[] attributes,
        int start, int end)
    {
        return Start(name, attributes, false, start, end);
    }

    static HtmlToken Start(string name, HtmlAttribute[] attributes,
        bool selfClosing, int start, int end)
    {
        return HtmlToken.CreateStartTag(name, attributes, selfClosing,
            Span(start, end));
    }

    static HtmlToken End(string name, int start, int end)
    {
        return HtmlToken.CreateEndTag(name, Array.Empty<HtmlAttribute>(),
            false, Span(start, end));
    }

    static HtmlToken Character(string data, int start, int end)
    {
        return HtmlToken.CreateCharacter(data, Span(start, end));
    }

    static HtmlAttribute Attribute(string name, string value, int offset)
    {
        return HtmlAttribute.Create(name, value, Span(offset, offset + 1));
    }

    static HtmlSourceSpan Span(int start, int end)
    {
        return HtmlSourceSpan.Create(
            HtmlSourcePosition.Create(start, 1, start + 1),
            HtmlSourcePosition.Create(end, 1, end + 1));
    }
}
