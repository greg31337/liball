
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlTreeBuilderDocumentModeTests
{
    [Test]
    public void InitialModesBuildExplicitDocumentStructureAndPreserveSources()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlToken comment = HtmlToken.CreateComment("before", Span(0, 9));
        HtmlToken doctype = HtmlToken.CreateDoctype("html", false,
            Span(9, 24));
        HtmlToken html = Start("html", 26, 32);
        HtmlToken head = Start("head", 34, 40);

        Assert.That(builder.ProcessToken(comment), Is.True);
        Assert.That(builder.ProcessToken(doctype), Is.True);
        Assert.That(builder.ProcessToken(Character(" \n\t", 24, 27)),
            Is.True);
        Assert.That(builder.ProcessToken(html), Is.True);
        Assert.That(builder.ProcessToken(Character("\n", 32, 33)),
            Is.True);
        Assert.That(builder.ProcessToken(head), Is.True);

        HtmlNode[] rootChildren = document.GetRoot().GetChildren();
        HtmlNode htmlNode = rootChildren[2];
        HtmlNode headNode = htmlNode.GetFirstChild();
        Assert.Multiple(() =>
        {
            Assert.That(rootChildren.Select(node => node.GetKind()),
                Is.EqualTo(new[]
                {
                    HtmlNodeKind.Comment,
                    HtmlNodeKind.DocumentType,
                    HtmlNodeKind.Element
                }));
            Assert.That(rootChildren[0].GetSourceSpan(),
                Is.SameAs(comment.GetSourceSpan()));
            Assert.That(rootChildren[1].GetSourceSpan(),
                Is.SameAs(doctype.GetSourceSpan()));
            Assert.That(htmlNode.GetSourceSpan(),
                Is.SameAs(html.GetSourceSpan()));
            Assert.That(headNode.GetSourceSpan(),
                Is.SameAs(head.GetSourceSpan()));
            Assert.That(builder.GetHeadElement(), Is.SameAs(headNode));
            Assert.That(builder.GetOpenElements().GetNodes(),
                Is.EqualTo(new[] { htmlNode, headNode }));
            Assert.That(builder.GetInsertionMode(),
                Is.EqualTo(HtmlInsertionMode.InHead));
            Assert.That(builder.GetDiagnostics(), Is.Empty);
            Assert.That(document.GetMutationVersionStorage(), Is.Zero);
        });
    }

    [Test]
    public void MissingDocumentElementsAreGeneratedWhileTokenIsReprocessed()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        builder.SetModeHandler(HtmlInsertionMode.InBody, (current, token) =>
            HtmlTreeBuilderAction.Consume());
        HtmlToken title = Start("title", 0, 7);

        Assert.That(builder.ProcessToken(title), Is.True);

        HtmlNode html = document.GetDocumentElement();
        HtmlNode head = html.GetFirstChild();
        HtmlNode titleNode = head.GetFirstChild();
        Assert.Multiple(() =>
        {
            Assert.That(builder.GetLastDispatchCount(), Is.EqualTo(4));
            Assert.That(html.GetSourceSpan().IsKnown(), Is.False);
            Assert.That(head.GetSourceSpan().IsKnown(), Is.False);
            Assert.That(titleNode.GetSourceSpan(),
                Is.SameAs(title.GetSourceSpan()));
            Assert.That(builder.GetHeadElement(), Is.SameAs(head));
            Assert.That(builder.GetInsertionMode(),
                Is.EqualTo(HtmlInsertionMode.Text));
            Assert.That(builder.GetOriginalInsertionMode(),
                Is.EqualTo(HtmlInsertionMode.InHead));
            Assert.That(builder.GetRequestedTokenizerState(),
                Is.EqualTo(HtmlTokenizerState.Rcdata));
        });

        builder.ClearTokenizerStateRequest();
        Assert.That(builder.ProcessToken(Character("Example", 7, 14)),
            Is.True);
        Assert.That(builder.ProcessToken(End("title", 14, 22)), Is.True);
        Assert.That(builder.ProcessToken(Start("p", 22, 25)), Is.True);

        HtmlNode body = html.GetLastChild();
        Assert.Multiple(() =>
        {
            Assert.That(titleNode.GetTextContent(), Is.EqualTo("Example"));
            Assert.That(body.GetLocalName(), Is.EqualTo("body"));
            Assert.That(body.GetSourceSpan().IsKnown(), Is.False);
            Assert.That(builder.GetOpenElements().GetNodes(),
                Is.EqualTo(new[] { html, body }));
            Assert.That(builder.GetInsertionMode(),
                Is.EqualTo(HtmlInsertionMode.InBody));
            Assert.That(builder.GetLastDispatchCount(), Is.EqualTo(3));
        });
    }

    [Test]
    public void InHeadInsertsWhitespaceCommentsAndAcknowledgesVoidElements()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode head = EnterHead(builder, document);
        HtmlToken whitespace = Character(" \n", 21, 23);
        HtmlToken comment = HtmlToken.CreateComment("inside", Span(23, 35));
        HtmlToken meta = HtmlToken.CreateStartTag("meta",
            Array.Empty<HtmlAttribute>(), true, Span(35, 42));

        Assert.That(builder.ProcessToken(Start("head", 20, 21)), Is.True);
        Assert.That(builder.ProcessToken(whitespace), Is.True);
        Assert.That(builder.ProcessToken(comment), Is.True);
        Assert.That(builder.ProcessToken(meta), Is.True);

        HtmlNode[] children = head.GetChildren();
        Assert.Multiple(() =>
        {
            Assert.That(children.Select(node => node.GetKind()),
                Is.EqualTo(new[]
                {
                    HtmlNodeKind.Text,
                    HtmlNodeKind.Comment,
                    HtmlNodeKind.Element
                }));
            Assert.That(children[0].GetData(), Is.EqualTo(" \n"));
            Assert.That(children[1].GetData(), Is.EqualTo("inside"));
            Assert.That(children[2].GetLocalName(), Is.EqualTo("meta"));
            Assert.That(meta.IsSelfClosingAcknowledged(), Is.True);
            Assert.That(builder.GetOpenElements().GetCurrentNode(),
                Is.SameAs(head));
            Assert.That(builder.GetDiagnostics(), Has.Length.EqualTo(1));
        });
    }

    [TestCase("title", 1)]
    [TestCase("style", 2)]
    [TestCase("noframes", 2)]
    [TestCase("script", 47)]
    public void HeadTextElementsRequestTokenizerStateAndRestoreHeadMode(
        string name, int expectedStateValue)
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode head = EnterHead(builder, document);

        Assert.That(builder.ProcessToken(Start(name, 21, 30)), Is.True);
        HtmlNode element = builder.GetOpenElements().GetCurrentNode();

        Assert.Multiple(() =>
        {
            Assert.That(element.GetLocalName(), Is.EqualTo(name));
            Assert.That(builder.GetInsertionMode(),
                Is.EqualTo(HtmlInsertionMode.Text));
            Assert.That(builder.GetOriginalInsertionMode(),
                Is.EqualTo(HtmlInsertionMode.InHead));
            Assert.That(builder.HasTokenizerStateRequest(), Is.True);
            Assert.That((int)builder.GetRequestedTokenizerState(),
                Is.EqualTo(expectedStateValue));
        });

        builder.ClearTokenizerStateRequest();
        Assert.That(builder.ProcessToken(Character("payload", 30, 37)),
            Is.True);
        Assert.That(builder.ProcessToken(End(name, 37, 48)), Is.True);

        Assert.Multiple(() =>
        {
            Assert.That(element.GetTextContent(), Is.EqualTo("payload"));
            Assert.That(builder.GetOpenElements().GetCurrentNode(),
                Is.SameAs(head));
            Assert.That(builder.GetInsertionMode(),
                Is.EqualTo(HtmlInsertionMode.InHead));
            Assert.That(builder.HasTokenizerStateRequest(), Is.False);
        });
    }

    [Test]
    public void HeadNoscriptUsesScriptingFlagAndItsOwnRecoveryMode()
    {
        HtmlDocument disabledDocument = CreateDocument();
        HtmlTreeBuilder disabled = CreateBuilder(disabledDocument);
        HtmlNode disabledHead = EnterHead(disabled, disabledDocument);

        Assert.That(disabled.ProcessToken(Start("noscript", 21, 31)),
            Is.True);
        HtmlNode noscript = disabled.GetOpenElements().GetCurrentNode();
        Assert.Multiple(() =>
        {
            Assert.That(disabled.GetInsertionMode(),
                Is.EqualTo(HtmlInsertionMode.InHeadNoScript));
            Assert.That(disabled.HasTokenizerStateRequest(), Is.False);
            Assert.That(noscript.GetLocalName(), Is.EqualTo("noscript"));
        });

        Assert.That(disabled.ProcessToken(Character(" ", 31, 32)), Is.True);
        Assert.That(disabled.ProcessToken(Start("head", 32, 38)), Is.True);
        Assert.That(disabled.ProcessToken(End("noscript", 38, 49)), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(noscript.GetTextContent(), Is.EqualTo(" "));
            Assert.That(disabled.GetOpenElements().GetCurrentNode(),
                Is.SameAs(disabledHead));
            Assert.That(disabled.GetInsertionMode(),
                Is.EqualTo(HtmlInsertionMode.InHead));
            Assert.That(disabled.GetDiagnostics(), Has.Length.EqualTo(1));
        });

        HtmlDocument enabledDocument = CreateDocument();
        HtmlTreeBuilder enabled = CreateBuilder(enabledDocument);
        EnterHead(enabled, enabledDocument);
        enabled.SetScriptingEnabled(true);
        Assert.That(enabled.ProcessToken(Start("noscript", 21, 31)), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(enabled.GetInsertionMode(),
                Is.EqualTo(HtmlInsertionMode.Text));
            Assert.That(enabled.GetRequestedTokenizerState(),
                Is.EqualTo(HtmlTokenizerState.RawText));
        });
    }

    [Test]
    public void AfterHeadHandlesLateHeadContentAndExplicitBodyOrFrameset()
    {
        HtmlDocument bodyDocument = CreateDocument();
        HtmlTreeBuilder bodyBuilder = CreateBuilder(bodyDocument);
        HtmlNode head = EnterHead(bodyBuilder, bodyDocument);
        HtmlNode html = bodyDocument.GetDocumentElement();
        Assert.That(bodyBuilder.ProcessToken(End("head", 21, 28)), Is.True);
        HtmlToken lateMeta = HtmlToken.CreateStartTag("meta",
            Array.Empty<HtmlAttribute>(), true, Span(28, 35));

        Assert.That(bodyBuilder.ProcessToken(lateMeta), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(head.GetLastChild().GetLocalName(), Is.EqualTo("meta"));
            Assert.That(lateMeta.IsSelfClosingAcknowledged(), Is.True);
            Assert.That(bodyBuilder.GetOpenElements().GetNodes(),
                Is.EqualTo(new[] { html }));
            Assert.That(bodyBuilder.GetInsertionMode(),
                Is.EqualTo(HtmlInsertionMode.AfterHead));
            Assert.That(bodyBuilder.GetDiagnostics(), Has.Length.EqualTo(1));
        });

        Assert.That(bodyBuilder.ProcessToken(Start("head", 35, 35)), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(bodyBuilder.GetInsertionMode(),
                Is.EqualTo(HtmlInsertionMode.AfterHead));
            Assert.That(bodyBuilder.GetOpenElements().GetNodes(),
                Is.EqualTo(new[] { html }));
            Assert.That(bodyBuilder.GetDiagnostics(), Has.Length.EqualTo(2));
        });

        Assert.That(bodyBuilder.ProcessToken(Start("body", 35, 41)), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(html.GetLastChild().GetLocalName(), Is.EqualTo("body"));
            Assert.That(html.GetLastChild().GetSourceSpan().IsKnown(), Is.True);
            Assert.That(bodyBuilder.GetInsertionMode(),
                Is.EqualTo(HtmlInsertionMode.InBody));
        });

        HtmlDocument framesetDocument = CreateDocument();
        HtmlTreeBuilder framesetBuilder = CreateBuilder(framesetDocument);
        EnterHead(framesetBuilder, framesetDocument);
        Assert.That(framesetBuilder.ProcessToken(End("head", 21, 28)), Is.True);
        Assert.That(framesetBuilder.ProcessToken(
            Start("frameset", 28, 38)), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(framesetDocument.GetDocumentElement().GetLastChild()
                .GetLocalName(), Is.EqualTo("frameset"));
            Assert.That(framesetBuilder.GetInsertionMode(),
                Is.EqualTo(HtmlInsertionMode.InFrameset));
        });
    }

    [Test]
    public void TextModeEofRestoresModeAndReprocessesTheToken()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        EnterHead(builder, document);
        Assert.That(builder.ProcessToken(Start("title", 21, 28)), Is.True);
        builder.ClearTokenizerStateRequest();
        builder.SetModeHandler(HtmlInsertionMode.InBody, (current, token) =>
            HtmlTreeBuilderAction.Stop());

        Assert.That(builder.ProcessToken(
            HtmlToken.CreateEndOfFile(Span(28, 28))), Is.True);

        HtmlNode html = document.GetDocumentElement();
        Assert.Multiple(() =>
        {
            Assert.That(builder.GetLastDispatchCount(), Is.EqualTo(4));
            Assert.That(builder.IsStopped(), Is.True);
            Assert.That(builder.GetInsertionMode(),
                Is.EqualTo(HtmlInsertionMode.InBody));
            Assert.That(builder.GetOpenElements().GetCurrentNode()
                .GetLocalName(), Is.EqualTo("body"));
            Assert.That(html.GetLastChild().GetSourceSpan().IsKnown(), Is.False);
            Assert.That(builder.GetDiagnostics(), Has.Length.EqualTo(1));
            Assert.That(builder.GetDiagnostics()[0].GetSpan()
                .GetStart().GetNormalizedCharacterOffset(), Is.EqualTo(28));
        });
    }

    [Test]
    public void NonConformingDoctypeAndDisallowedHeadEndTagsReportErrors()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlToken doctype = HtmlToken.CreateDoctype(true, "svg", true,
            "legacy", true, "system", false, Span(0, 20));
        HtmlToken endTag = End("section", 26, 36);

        Assert.That(builder.ProcessToken(doctype), Is.True);
        Assert.That(builder.ProcessToken(Start("html", 20, 26)), Is.True);
        Assert.That(builder.ProcessToken(endTag), Is.True);

        Assert.Multiple(() =>
        {
            Assert.That(document.GetRoot().GetFirstChild().GetKind(),
                Is.EqualTo(HtmlNodeKind.DocumentType));
            Assert.That(builder.GetInsertionMode(),
                Is.EqualTo(HtmlInsertionMode.BeforeHead));
            Assert.That(builder.GetDiagnostics(), Has.Length.EqualTo(2));
            Assert.That(builder.GetDiagnostics().Select(item => item.GetSpan()),
                Is.EqualTo(new[]
                {
                    doctype.GetSourceSpan(),
                    endTag.GetSourceSpan()
                }));
        });
    }

    static HtmlNode EnterHead(HtmlTreeBuilder builder, HtmlDocument document)
    {
        Assert.That(builder.ProcessToken(HtmlToken.CreateDoctype("html", false,
            Span(0, 15))), Is.True);
        Assert.That(builder.ProcessToken(Start("html", 15, 21)), Is.True);
        Assert.That(builder.ProcessToken(Start("head", 21, 27)), Is.True);
        return document.GetHead();
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

    static HtmlToken Start(string name, int start, int end)
    {
        return HtmlToken.CreateStartTag(name, Array.Empty<HtmlAttribute>(),
            false, Span(start, end));
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

    static HtmlSourceSpan Span(int start, int end)
    {
        return HtmlSourceSpan.Create(
            HtmlSourcePosition.Create(start, 1, start + 1),
            HtmlSourcePosition.Create(end, 1, end + 1));
    }
}
