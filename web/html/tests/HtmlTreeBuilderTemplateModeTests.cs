
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlTreeBuilderTemplateModeTests
{
    [Test]
    public void TemplateParsesIntoContentFragmentAndClosesCleanly()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode body = EnterBody(builder, document);

        Process(builder, Start("template", 40));
        HtmlNode template = body.GetLastChild();
        Assert.Multiple(() =>
        {
            Assert.That(template.GetChildren(), Is.Empty);
            Assert.That(template.GetTemplateContent(), Is.Not.Null);
            Assert.That(builder.GetInsertionMode(), Is.EqualTo(
                HtmlInsertionMode.InTemplate));
            Assert.That(builder.GetTemplateInsertionModes(),
                Is.EqualTo(new[] { HtmlInsertionMode.InTemplate }));
            Assert.That(builder.GetFormattingElements().IsMarker(0), Is.True);
        });

        Process(builder, Character("lead", 41));
        Process(builder, Start("div", 42));
        Process(builder, Character("inside", 43));
        Process(builder, End("div", 44));
        Process(builder, End("template", 45));

        HtmlNode content = template.GetTemplateContent();
        Assert.Multiple(() =>
        {
            Assert.That(template.GetChildren(), Is.Empty);
            Assert.That(content.GetChildren().Select(value => value.GetName()),
                Is.EqualTo(new[] { "#text", "div" }));
            Assert.That(template.GetTextContent(), Is.EqualTo("leadinside"));
            Assert.That(builder.GetOpenElements().GetCurrentNode(),
                Is.SameAs(body));
            Assert.That(builder.GetInsertionMode(), Is.EqualTo(
                HtmlInsertionMode.InBody));
            Assert.That(builder.GetTemplateInsertionModeCount(), Is.Zero);
            Assert.That(builder.GetFormattingElements().GetCount(), Is.Zero);
        });
    }

    [Test]
    public void NestedTemplatesKeepIndependentContentsAndModes()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode body = EnterBody(builder, document);

        Process(builder, Start("template", 40));
        HtmlNode outer = body.GetLastChild();
        Process(builder, Start("template", 41));
        HtmlNode inner = outer.GetTemplateContent().GetFirstChild();
        Process(builder, Character("inner", 42));
        Assert.That(builder.GetTemplateInsertionModeCount(), Is.EqualTo(2));
        Process(builder, End("template", 43));
        Assert.Multiple(() =>
        {
            Assert.That(builder.GetInsertionMode(), Is.EqualTo(
                HtmlInsertionMode.InTemplate));
            Assert.That(builder.GetTemplateInsertionModeCount(), Is.EqualTo(1));
        });

        Process(builder, Start("span", 44));
        Process(builder, Character("outer", 45));
        Process(builder, End("span", 46));
        Process(builder, End("template", 47));

        Assert.Multiple(() =>
        {
            Assert.That(inner.GetTemplateContent().GetTextContent(),
                Is.EqualTo("inner"));
            Assert.That(outer.GetTemplateContent().GetLastChild()
                .GetLocalName(), Is.EqualTo("span"));
            Assert.That(outer.GetTemplateContent().GetLastChild()
                .GetTextContent(), Is.EqualTo("outer"));
            Assert.That(builder.GetTemplateInsertionModeCount(), Is.Zero);
            Assert.That(builder.GetFormattingElements().GetCount(), Is.Zero);
        });
    }

    [Test]
    public void TemplateTableTokensSelectAndPreserveTableModes()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode body = EnterBody(builder, document);

        Process(builder, Start("template", 40));
        HtmlNode template = body.GetLastChild();
        Process(builder, Start("tr", 41));
        Assert.Multiple(() =>
        {
            Assert.That(builder.GetInsertionMode(), Is.EqualTo(
                HtmlInsertionMode.InRow));
            Assert.That(builder.GetCurrentTemplateInsertionMode(),
                Is.EqualTo(HtmlInsertionMode.InTableBody));
        });

        Process(builder, Start("td", 42));
        Process(builder, Character("cell", 43));
        Process(builder, End("td", 44));
        Process(builder, End("tr", 45));
        Process(builder, End("template", 46));

        HtmlNode row = template.GetTemplateContent().GetFirstChild();
        Assert.Multiple(() =>
        {
            Assert.That(row.GetLocalName(), Is.EqualTo("tr"));
            Assert.That(row.GetFirstChild().GetLocalName(), Is.EqualTo("td"));
            Assert.That(row.GetTextContent(), Is.EqualTo("cell"));
            Assert.That(builder.GetInsertionMode(), Is.EqualTo(
                HtmlInsertionMode.InBody));
            Assert.That(builder.GetTemplateInsertionModeCount(), Is.Zero);
        });
    }

    [Test]
    public void HeadTextElementInsideTemplateReturnsToTemplateMode()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode body = EnterBody(builder, document);

        Process(builder, Start("template", 40));
        HtmlNode template = body.GetLastChild();
        Process(builder, Start("title", 41));
        Assert.Multiple(() =>
        {
            Assert.That(builder.GetInsertionMode(),
                Is.EqualTo(HtmlInsertionMode.Text));
            Assert.That(builder.GetRequestedTokenizerState(),
                Is.EqualTo(HtmlTokenizerState.Rcdata));
        });

        Process(builder, Character("title", 42));
        Process(builder, End("title", 43));
        Assert.That(builder.GetInsertionMode(), Is.EqualTo(
            HtmlInsertionMode.InTemplate));
        Process(builder, End("template", 44));

        Assert.Multiple(() =>
        {
            Assert.That(template.GetTemplateContent().GetFirstChild()
                .GetLocalName(), Is.EqualTo("title"));
            Assert.That(template.GetTextContent(), Is.EqualTo("title"));
            Assert.That(builder.GetTemplateInsertionModeCount(), Is.Zero);
        });
    }

    [Test]
    public void TemplateEndClearsFormattingEntriesAndInterveningElements()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode body = EnterBody(builder, document);

        Process(builder, Start("template", 40));
        HtmlNode template = body.GetLastChild();
        Process(builder, Start("b", 41));
        Process(builder, Character("bold", 42));
        Assert.That(builder.GetFormattingElements().GetCount(), Is.EqualTo(2));
        Process(builder, End("template", 43));

        Assert.Multiple(() =>
        {
            Assert.That(template.GetTemplateContent().GetFirstChild()
                .GetLocalName(), Is.EqualTo("b"));
            Assert.That(template.GetTextContent(), Is.EqualTo("bold"));
            Assert.That(builder.GetFormattingElements().GetCount(), Is.Zero);
            Assert.That(builder.GetOpenElements().GetCurrentNode(),
                Is.SameAs(body));
            Assert.That(builder.GetDiagnostics(), Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void EndOfFileUnwindsEveryNestedTemplateBeforeStopping()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode body = EnterBody(builder, document);

        Process(builder, Start("template", 40));
        HtmlNode outer = body.GetLastChild();
        Process(builder, Start("div", 41));
        Process(builder, End("div", 42));
        Process(builder, Start("template", 43));
        HtmlNode inner = outer.GetTemplateContent().GetLastChild();
        Process(builder, Character("unfinished", 44));
        Process(builder, HtmlToken.CreateEndOfFile(Span(45)));

        Assert.Multiple(() =>
        {
            Assert.That(inner.GetTemplateContent().GetTextContent(),
                Is.EqualTo("unfinished"));
            Assert.That(builder.IsStopped(), Is.True);
            Assert.That(builder.GetOpenElements().GetCurrentNode(),
                Is.SameAs(body));
            Assert.That(builder.GetInsertionMode(), Is.EqualTo(
                HtmlInsertionMode.InBody));
            Assert.That(builder.GetTemplateInsertionModeCount(), Is.Zero);
            Assert.That(builder.GetFormattingElements().GetCount(), Is.Zero);
            Assert.That(builder.GetDiagnostics(), Has.Length.EqualTo(2));
            Assert.That(builder.GetLastDispatchCount(), Is.EqualTo(4));
        });
    }

    [Test]
    public void ParsedTemplateContentDeepClonesAndTraversesAsAFragment()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode body = EnterBody(builder, document);

        Process(builder, Start("template", 40));
        HtmlNode template = body.GetLastChild();
        Process(builder, Start("section", 41));
        Process(builder, Start("span", 42));
        Process(builder, Character("value", 43));
        Process(builder, End("span", 44));
        Process(builder, End("section", 45));
        Process(builder, End("template", 46));

        HtmlNode clone = template.Clone(true);
        HtmlNode originalContent = template.GetTemplateContent();
        HtmlNode clonedContent = clone.GetTemplateContent();
        HtmlNode[] descendants = originalContent.GetDescendants();
        Assert.Multiple(() =>
        {
            Assert.That(template.GetChildren(), Is.Empty);
            Assert.That(template.GetDescendants(), Is.Empty);
            Assert.That(descendants.Select(value => value.GetName()),
                Is.EqualTo(new[] { "section", "span", "#text" }));
            Assert.That(clone.GetChildren(), Is.Empty);
            Assert.That(clonedContent, Is.Not.SameAs(originalContent));
            Assert.That(clonedContent.GetTextContent(), Is.EqualTo("value"));
            Assert.That(clonedContent.GetFirstChild(),
                Is.Not.SameAs(originalContent.GetFirstChild()));
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
