
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlTreeBuilderFinalModeTests
{
    [Test]
    public void AfterBodyRoutesWhitespaceAndPlacesCommentsOnHtml()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode body = EnterBody(builder, document);
        HtmlNode html = document.GetDocumentElement();

        Process(builder, End("body", 40));
        Process(builder, Character(" \n", 41));
        HtmlToken commentToken = Comment("after body", 42);
        Process(builder, commentToken);
        Process(builder, HtmlToken.CreateDoctype("html", false, Span(43)));
        HtmlToken duplicateHtml = HtmlToken.CreateStartTag("html", new[]
        {
            HtmlAttribute.Create("lang", "en", Span(44))
        }, false, Span(44));
        Process(builder, duplicateHtml);

        HtmlNode comment = html.GetLastChild();
        Assert.Multiple(() =>
        {
            Assert.That(body.GetTextContent(), Is.EqualTo(" \n"));
            Assert.That(comment.GetKind(), Is.EqualTo(HtmlNodeKind.Comment));
            Assert.That(comment.GetData(), Is.EqualTo("after body"));
            Assert.That(comment.GetSourceSpan(),
                Is.SameAs(commentToken.GetSourceSpan()));
            Assert.That(html.GetAttribute("lang"), Is.EqualTo("en"));
            Assert.That(builder.GetInsertionMode(), Is.EqualTo(
                HtmlInsertionMode.AfterBody));
            Assert.That(builder.GetDiagnostics(), Has.Length.EqualTo(2));
        });

        Process(builder, HtmlToken.CreateEndOfFile(Span(45)));
        Assert.That(builder.IsStopped(), Is.True);
    }

    [Test]
    public void UnexpectedAfterBodyContentReturnsToBodyAndReprocesses()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode body = EnterBody(builder, document);

        Process(builder, End("body", 40));
        Process(builder, Start("section", 41));

        Assert.Multiple(() =>
        {
            Assert.That(body.GetLastChild().GetLocalName(),
                Is.EqualTo("section"));
            Assert.That(builder.GetOpenElements().GetCurrentNode(),
                Is.SameAs(body.GetLastChild()));
            Assert.That(builder.GetInsertionMode(), Is.EqualTo(
                HtmlInsertionMode.InBody));
            Assert.That(builder.GetLastDispatchCount(), Is.EqualTo(2));
            Assert.That(builder.GetDiagnostics(), Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void HtmlEndEntersAfterAfterBodyAndDocumentCommentsStayOutsideHtml()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode body = EnterBody(builder, document);
        HtmlNode html = document.GetDocumentElement();

        Process(builder, End("html", 40));
        Assert.Multiple(() =>
        {
            Assert.That(builder.GetInsertionMode(), Is.EqualTo(
                HtmlInsertionMode.AfterAfterBody));
            Assert.That(builder.GetLastDispatchCount(), Is.EqualTo(2));
        });

        Process(builder, Comment("outside", 41));
        HtmlNode documentComment = document.GetRoot().GetLastChild();
        Process(builder, Character(" ", 42));

        Assert.Multiple(() =>
        {
            Assert.That(documentComment.GetKind(),
                Is.EqualTo(HtmlNodeKind.Comment));
            Assert.That(documentComment.GetParent(),
                Is.SameAs(document.GetRoot()));
            Assert.That(html.GetLastChild(), Is.SameAs(body));
            Assert.That(body.GetTextContent(), Is.EqualTo(" "));
            Assert.That(builder.GetInsertionMode(), Is.EqualTo(
                HtmlInsertionMode.AfterAfterBody));
        });

        Process(builder, Start("main", 43));
        Assert.Multiple(() =>
        {
            Assert.That(body.GetLastChild().GetLocalName(), Is.EqualTo("main"));
            Assert.That(builder.GetInsertionMode(), Is.EqualTo(
                HtmlInsertionMode.InBody));
            Assert.That(builder.GetLastDispatchCount(), Is.EqualTo(2));
        });
    }

    [Test]
    public void FramesetModeBuildsNestedFramesAndTransitionsAfterOuterClose()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode html = EnterFrameset(builder, document);
        HtmlNode frameset = html.GetLastChild();

        Process(builder, Character(" \n", 40));
        Process(builder, Comment("inside", 41));
        HtmlToken frame = Start("frame", true, 42);
        Process(builder, frame);
        Process(builder, Start("frameset", 43));
        HtmlNode nested = frameset.GetLastChild();
        Process(builder, End("frameset", 44));

        Assert.Multiple(() =>
        {
            Assert.That(frameset.GetChildren().Select(value => value.GetName()),
                Is.EqualTo(new[]
                {
                    "#text", "#comment", "frame", "frameset"
                }));
            Assert.That(frame.IsSelfClosingAcknowledged(), Is.True);
            Assert.That(nested.GetLocalName(), Is.EqualTo("frameset"));
            Assert.That(builder.GetOpenElements().GetCurrentNode(),
                Is.SameAs(frameset));
            Assert.That(builder.GetInsertionMode(), Is.EqualTo(
                HtmlInsertionMode.InFrameset));
        });

        Process(builder, End("frameset", 45));
        Assert.Multiple(() =>
        {
            Assert.That(builder.GetOpenElements().GetCurrentNode(),
                Is.SameAs(html));
            Assert.That(builder.GetInsertionMode(), Is.EqualTo(
                HtmlInsertionMode.AfterFrameset));
        });
    }

    [Test]
    public void FramesetModeIgnoresInvalidContentAndReportsOpenFramesetEof()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode html = EnterFrameset(builder, document);
        HtmlNode frameset = html.GetLastChild();

        Process(builder, Character("not allowed", 40));
        Process(builder, Start("div", 41));
        Process(builder, End("body", 42));
        Process(builder, HtmlToken.CreateDoctype("html", false, Span(43)));

        Assert.Multiple(() =>
        {
            Assert.That(frameset.GetChildren(), Is.Empty);
            Assert.That(builder.GetInsertionMode(), Is.EqualTo(
                HtmlInsertionMode.InFrameset));
            Assert.That(builder.GetDiagnostics(), Has.Length.EqualTo(4));
        });

        Process(builder, HtmlToken.CreateEndOfFile(Span(44)));
        Assert.Multiple(() =>
        {
            Assert.That(builder.IsStopped(), Is.True);
            Assert.That(builder.GetDiagnostics(), Has.Length.EqualTo(5));
        });
    }

    [Test]
    public void AfterFramesetSupportsNoframesAndTransitionsPastHtml()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode html = EnterFrameset(builder, document);
        HtmlNode frameset = html.GetLastChild();
        Process(builder, End("frameset", 40));

        Process(builder, Character(" ", 41));
        Process(builder, Comment("after", 42));
        Process(builder, Start("noframes", 43));
        Assert.That(builder.GetInsertionMode(), Is.EqualTo(
            HtmlInsertionMode.Text));
        Process(builder, Character("fallback", 44));
        Process(builder, End("noframes", 45));

        HtmlNode noframes = html.GetLastChild();
        Assert.Multiple(() =>
        {
            Assert.That(frameset.GetNextSibling().GetKind(),
                Is.EqualTo(HtmlNodeKind.Text));
            Assert.That(noframes.GetLocalName(), Is.EqualTo("noframes"));
            Assert.That(noframes.GetTextContent(), Is.EqualTo("fallback"));
            Assert.That(builder.GetInsertionMode(), Is.EqualTo(
                HtmlInsertionMode.AfterFrameset));
        });

        Process(builder, End("html", 46));
        Assert.That(builder.GetInsertionMode(), Is.EqualTo(
            HtmlInsertionMode.AfterAfterFrameset));
        Process(builder, Comment("document", 47));
        Assert.That(document.GetRoot().GetLastChild().GetData(),
            Is.EqualTo("document"));
    }

    [Test]
    public void AfterAfterFramesetAllowsNoframesButIgnoresOtherElements()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode html = EnterFrameset(builder, document);
        Process(builder, End("frameset", 40));
        Process(builder, End("html", 41));

        Process(builder, Start("div", 42));
        Assert.Multiple(() =>
        {
            Assert.That(html.GetChildElements().Select(
                value => value.GetLocalName()),
                Is.EqualTo(new[] { "head", "frameset" }));
            Assert.That(builder.GetInsertionMode(), Is.EqualTo(
                HtmlInsertionMode.AfterAfterFrameset));
        });

        Process(builder, Start("noframes", 43));
        Process(builder, Character("fallback", 44));
        Process(builder, End("noframes", 45));
        Process(builder, HtmlToken.CreateEndOfFile(Span(46)));

        Assert.Multiple(() =>
        {
            Assert.That(html.GetLastChild().GetLocalName(),
                Is.EqualTo("noframes"));
            Assert.That(html.GetLastChild().GetTextContent(),
                Is.EqualTo("fallback"));
            Assert.That(builder.IsStopped(), Is.True);
            Assert.That(builder.GetDiagnostics(), Has.Length.EqualTo(1));
        });
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

    static HtmlNode EnterFrameset(HtmlTreeBuilder builder,
        HtmlDocument document)
    {
        Process(builder, HtmlToken.CreateDoctype("html", false, Span(0)));
        Process(builder, Start("html", 1));
        Process(builder, Start("head", 2));
        Process(builder, End("head", 3));
        Process(builder, Start("frameset", 4));
        return document.GetDocumentElement();
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

    static HtmlToken Start(string name, int offset)
    {
        return Start(name, false, offset);
    }

    static HtmlToken Start(string name, bool selfClosing, int offset)
    {
        return HtmlToken.CreateStartTag(name, Array.Empty<HtmlAttribute>(),
            selfClosing, Span(offset));
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

    static HtmlToken Comment(string data, int offset)
    {
        return HtmlToken.CreateComment(data, Span(offset));
    }

    static HtmlSourceSpan Span(int offset)
    {
        return HtmlSourceSpan.Create(
            HtmlSourcePosition.Create(offset, 1, offset + 1),
            HtmlSourcePosition.Create(offset + 1, 1, offset + 2));
    }
}
