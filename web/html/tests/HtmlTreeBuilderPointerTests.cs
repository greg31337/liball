
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlTreeBuilderPointerTests
{
    [Test]
    public void FormPointerSurvivesRemovalFromOpenElementsAndBlocksNestedForm()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode body = EnterBody(builder, document);

        Process(builder, Start("form", 40));
        HtmlNode firstForm = body.GetLastChild();
        Process(builder, Start("div", 41));
        HtmlNode container = firstForm.GetLastChild();
        Process(builder, Start("form", 42));

        Assert.Multiple(() =>
        {
            Assert.That(builder.GetFormElement(), Is.SameAs(firstForm));
            Assert.That(container.GetChildren(), Is.Empty);
            Assert.That(builder.GetOpenElements().GetCurrentNode(),
                Is.SameAs(container));
        });

        Process(builder, End("form", 43));
        Assert.Multiple(() =>
        {
            Assert.That(builder.GetFormElement(), Is.Null);
            Assert.That(builder.GetOpenElements().Contains(firstForm), Is.False);
            Assert.That(builder.GetOpenElements().GetCurrentNode(),
                Is.SameAs(container));
        });

        Process(builder, Start("form", 44));
        HtmlNode secondForm = container.GetLastChild();
        Assert.Multiple(() =>
        {
            Assert.That(secondForm.GetLocalName(), Is.EqualTo("form"));
            Assert.That(builder.GetFormElement(), Is.SameAs(secondForm));
        });
    }

    [Test]
    public void TableFormPointerRemainsSetAfterTheFormIsPopped()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode body = EnterBody(builder, document);

        Process(builder, Start("table", 40));
        HtmlNode table = body.GetLastChild();
        Process(builder, Start("form", 41));
        HtmlNode firstForm = table.GetLastChild();

        Assert.Multiple(() =>
        {
            Assert.That(builder.GetFormElement(), Is.SameAs(firstForm));
            Assert.That(builder.GetOpenElements().Contains(firstForm), Is.False);
            Assert.That(builder.GetOpenElements().GetCurrentNode(),
                Is.SameAs(table));
        });

        Process(builder, Start("form", 42));
        Assert.That(table.GetChildElements().Count(node =>
            node.GetLocalName() == "form"), Is.EqualTo(1));

        Process(builder, End("form", 43));
        Assert.That(builder.GetFormElement(), Is.Null);
        Process(builder, Start("form", 44));

        Assert.Multiple(() =>
        {
            Assert.That(table.GetChildElements().Count(node =>
                node.GetLocalName() == "form"), Is.EqualTo(2));
            Assert.That(builder.GetFormElement(),
                Is.SameAs(table.GetLastChild()));
        });
    }

    [Test]
    public void TemplateFormDoesNotReplaceTheOuterFormPointer()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode body = EnterBody(builder, document);

        Process(builder, Start("form", 40));
        HtmlNode outerForm = body.GetLastChild();
        Process(builder, Start("template", 41));
        HtmlNode template = outerForm.GetLastChild();
        Process(builder, Start("form", 42));
        HtmlNode innerForm = template.GetTemplateContent().GetLastChild();

        Assert.Multiple(() =>
        {
            Assert.That(innerForm.GetLocalName(), Is.EqualTo("form"));
            Assert.That(builder.GetFormElement(), Is.SameAs(outerForm));
            Assert.That(builder.GetOpenElements().GetCurrentNode(),
                Is.SameAs(innerForm));
        });

        Process(builder, End("form", 43));
        Assert.That(builder.GetFormElement(), Is.SameAs(outerForm));
        Process(builder, End("template", 44));
        Process(builder, End("form", 45));
        Assert.That(builder.GetFormElement(), Is.Null);
    }

    [Test]
    public void HtmlScriptElementsAreAlwaysInertDataNodes()
    {
        HtmlDocument document = CreateDocument();
        HtmlNode script = document.CreateElement("SCRIPT");
        HtmlNode clone = script.Clone(false);
        HtmlNode svgScript = document.CreateElementInNamespace(
            "http://www.w3.org/2000/svg", "script");

        Assert.Multiple(() =>
        {
            Assert.That(script.IsScriptInert(), Is.True);
            Assert.That(clone.IsScriptInert(), Is.True);
            Assert.That(svgScript.IsScriptInert(), Is.True);
            Assert.That(document.CreateElement("div").IsScriptInert(),
                Is.False);
        });
    }

    [Test]
    public void ParsedScriptIsInertAndPreservesScriptData()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode body = EnterBody(builder, document);

        Process(builder, Start("script", 40));
        HtmlNode script = body.GetLastChild();
        Assert.Multiple(() =>
        {
            Assert.That(script.IsScriptInert(), Is.True);
            Assert.That(builder.GetInsertionMode(),
                Is.EqualTo(HtmlInsertionMode.Text));
            Assert.That(builder.GetRequestedTokenizerState(),
                Is.EqualTo(HtmlTokenizerState.ScriptData));
        });

        builder.ClearTokenizerStateRequest();
        Process(builder, Character("window.value = '<b>';", 41));
        Process(builder, End("script", 42));

        Assert.Multiple(() =>
        {
            Assert.That(script.GetTextContent(),
                Is.EqualTo("window.value = '<b>';"));
            Assert.That(script.GetChildElements(), Is.Empty);
            Assert.That(builder.GetInsertionMode(),
                Is.EqualTo(HtmlInsertionMode.InBody));
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
