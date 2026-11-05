
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlTreeBuilderSelectModeTests
{
    [Test]
    public void SelectKeepsTextAndCommentsWhileIgnoringInvalidContent()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode body = EnterBody(builder, document);

        Process(builder, Start("select", 40));
        HtmlNode select = body.GetLastChild();
        Process(builder, Character("\0first", 41));
        Process(builder, Comment("note", 42));
        Process(builder, Start("div", 43));
        Process(builder, Character("second", 44));

        Assert.Multiple(() =>
        {
            Assert.That(select.GetTextContent(), Is.EqualTo("firstsecond"));
            Assert.That(select.GetChildren().Select(value => value.GetKind()),
                Is.EqualTo(new[]
                {
                    HtmlNodeKind.Text,
                    HtmlNodeKind.Comment,
                    HtmlNodeKind.Text
                }));
            Assert.That(builder.GetOpenElements().GetCurrentNode(),
                Is.SameAs(select));
            Assert.That(builder.GetInsertionMode(), Is.EqualTo(
                HtmlInsertionMode.InSelect));
            Assert.That(builder.GetDiagnostics(), Has.Length.EqualTo(2));
        });
    }

    [Test]
    public void StartingOptionImplicitlyClosesTheCurrentOption()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode body = EnterBody(builder, document);

        Process(builder, Start("select", 40));
        HtmlNode select = body.GetLastChild();
        Process(builder, Start("option", 41));
        Process(builder, Character("one", 42));
        Process(builder, Start("option", 43));
        Process(builder, Character("two", 44));
        Process(builder, End("option", 45));

        HtmlNode[] options = select.GetChildElements();
        Assert.Multiple(() =>
        {
            Assert.That(options.Select(value => value.GetLocalName()),
                Is.EqualTo(new[] { "option", "option" }));
            Assert.That(options.Select(value => value.GetTextContent()),
                Is.EqualTo(new[] { "one", "two" }));
            Assert.That(options[0].GetNextSibling(), Is.SameAs(options[1]));
            Assert.That(builder.GetOpenElements().GetCurrentNode(),
                Is.SameAs(select));
            Assert.That(builder.GetDiagnostics(), Is.Empty);
        });
    }

    [Test]
    public void OptgroupStartsAndEndsCorrectOpenOptionsAndGroups()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode body = EnterBody(builder, document);

        Process(builder, Start("select", 40));
        HtmlNode select = body.GetLastChild();
        Process(builder, Start("optgroup", 41));
        Process(builder, Start("option", 42));
        Process(builder, Character("one", 43));
        Process(builder, Start("optgroup", 44));
        Process(builder, Start("option", 45));
        Process(builder, Character("two", 46));
        Process(builder, End("optgroup", 47));

        HtmlNode[] groups = select.GetChildElements();
        Assert.Multiple(() =>
        {
            Assert.That(groups.Select(value => value.GetLocalName()),
                Is.EqualTo(new[] { "optgroup", "optgroup" }));
            Assert.That(groups[0].GetTextContent(), Is.EqualTo("one"));
            Assert.That(groups[1].GetTextContent(), Is.EqualTo("two"));
            Assert.That(groups[0].GetFirstChild().GetLocalName(),
                Is.EqualTo("option"));
            Assert.That(groups[1].GetFirstChild().GetLocalName(),
                Is.EqualTo("option"));
            Assert.That(builder.GetOpenElements().GetCurrentNode(),
                Is.SameAs(select));
            Assert.That(builder.GetDiagnostics(), Is.Empty);
        });

        Process(builder, End("optgroup", 48));
        Assert.That(builder.GetDiagnostics(), Has.Length.EqualTo(1));
    }

    [Test]
    public void NestedSelectClosesTheExistingSelectWithoutInsertingAnother()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode body = EnterBody(builder, document);

        Process(builder, Start("select", 40));
        HtmlNode select = body.GetLastChild();
        Process(builder, Start("option", 41));
        Process(builder, Start("select", 42));
        Process(builder, Start("p", 43));

        Assert.Multiple(() =>
        {
            Assert.That(body.GetChildElements().Select(
                value => value.GetLocalName()),
                Is.EqualTo(new[] { "select", "p" }));
            Assert.That(select.GetChildElements(), Has.Length.EqualTo(1));
            Assert.That(builder.GetInsertionMode(), Is.EqualTo(
                HtmlInsertionMode.InBody));
            Assert.That(builder.GetOpenElements().GetCurrentNode()
                .GetLocalName(), Is.EqualTo("p"));
            Assert.That(builder.GetDiagnostics(), Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void FormControlClosesSelectAndIsReprocessedInBody()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode body = EnterBody(builder, document);

        Process(builder, Start("select", 40));
        Process(builder, Start("option", 41));
        Process(builder, Character("value", 42));
        HtmlToken input = Start("input", true, 43);
        Process(builder, input);

        HtmlNode[] children = body.GetChildElements();
        Assert.Multiple(() =>
        {
            Assert.That(children.Select(value => value.GetLocalName()),
                Is.EqualTo(new[] { "select", "input" }));
            Assert.That(children[0].GetTextContent(), Is.EqualTo("value"));
            Assert.That(input.IsSelfClosingAcknowledged(), Is.True);
            Assert.That(builder.GetOpenElements().GetCurrentNode(),
                Is.SameAs(body));
            Assert.That(builder.GetInsertionMode(), Is.EqualTo(
                HtmlInsertionMode.InBody));
            Assert.That(builder.GetLastDispatchCount(), Is.EqualTo(2));
        });
    }

    [Test]
    public void SelectInTableIsFosterParentedAndTableStartEscapesIt()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode body = EnterBody(builder, document);

        Process(builder, Start("table", 40));
        HtmlNode table = body.GetLastChild();
        Process(builder, Start("select", 41));
        HtmlNode select = body.GetFirstChild();
        Process(builder, Start("option", 42));
        Process(builder, Character("choice", 43));
        Process(builder, Start("tbody", 44));

        Assert.Multiple(() =>
        {
            Assert.That(body.GetChildElements(), Is.EqualTo(
                new[] { select, table }));
            Assert.That(select.GetLocalName(), Is.EqualTo("select"));
            Assert.That(select.GetTextContent(), Is.EqualTo("choice"));
            Assert.That(table.GetFirstChild().GetLocalName(),
                Is.EqualTo("tbody"));
            Assert.That(builder.GetInsertionMode(), Is.EqualTo(
                HtmlInsertionMode.InTableBody));
            Assert.That(builder.GetOpenElements().GetCurrentNode(),
                Is.SameAs(table.GetFirstChild()));
        });
    }

    [Test]
    public void TableEndEscapesSelectCellRowSectionAndTable()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode body = EnterBody(builder, document);

        Process(builder, Start("table", 40));
        HtmlNode table = body.GetLastChild();
        Process(builder, Start("tbody", 41));
        Process(builder, Start("tr", 42));
        Process(builder, Start("td", 43));
        Process(builder, Start("select", 44));
        Process(builder, Start("option", 45));
        Process(builder, Character("choice", 46));
        Process(builder, End("table", 47));

        HtmlNode select = table.GetFirstChild().GetFirstChild()
            .GetFirstChild().GetFirstChild();
        Assert.Multiple(() =>
        {
            Assert.That(select.GetLocalName(), Is.EqualTo("select"));
            Assert.That(select.GetTextContent(), Is.EqualTo("choice"));
            Assert.That(builder.GetOpenElements().GetCurrentNode(),
                Is.SameAs(body));
            Assert.That(builder.GetInsertionMode(), Is.EqualTo(
                HtmlInsertionMode.InBody));
            Assert.That(builder.GetLastDispatchCount(), Is.EqualTo(5));
        });
    }

    [Test]
    public void OutOfScopeTableEndIsIgnoredWithoutClosingSelect()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode body = EnterBody(builder, document);

        Process(builder, Start("table", 40));
        Process(builder, Start("select", 41));
        HtmlNode select = body.GetFirstChild();
        Process(builder, End("caption", 42));

        Assert.Multiple(() =>
        {
            Assert.That(builder.GetOpenElements().GetCurrentNode(),
                Is.SameAs(select));
            Assert.That(builder.GetInsertionMode(), Is.EqualTo(
                HtmlInsertionMode.InSelectInTable));
            Assert.That(builder.GetDiagnostics(),
                Has.Length.GreaterThanOrEqualTo(2));
        });
    }

    [Test]
    public void ScriptUsesHeadRulesAndReturnsToSelectMode()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode body = EnterBody(builder, document);

        Process(builder, Start("select", 40));
        HtmlNode select = body.GetLastChild();
        Process(builder, Start("script", 41));
        Assert.That(builder.GetInsertionMode(), Is.EqualTo(
            HtmlInsertionMode.Text));
        Process(builder, Character("code", 42));
        Process(builder, End("script", 43));

        Assert.Multiple(() =>
        {
            Assert.That(select.GetFirstChild().GetLocalName(),
                Is.EqualTo("script"));
            Assert.That(select.GetFirstChild().GetTextContent(),
                Is.EqualTo("code"));
            Assert.That(builder.GetOpenElements().GetCurrentNode(),
                Is.SameAs(select));
            Assert.That(builder.GetInsertionMode(), Is.EqualTo(
                HtmlInsertionMode.InSelect));
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
