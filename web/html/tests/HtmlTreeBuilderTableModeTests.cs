
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlTreeBuilderTableModeTests
{
    [Test]
    public void ExplicitTableStructureTransitionsThroughEveryTableMode()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode body = EnterBody(builder, document);

        Process(builder, Start("table", 40));
        HtmlNode table = body.GetLastChild();
        Assert.That(builder.GetInsertionMode(), Is.EqualTo(
            HtmlInsertionMode.InTable));

        Process(builder, Start("caption", 41));
        Process(builder, Character("title", 42));
        Process(builder, End("caption", 43));
        Process(builder, Start("colgroup", 44));
        HtmlToken col = Start("col", true, 45);
        Process(builder, col);
        Process(builder, End("colgroup", 46));
        Process(builder, Start("thead", 47));
        Process(builder, Start("tr", 48));
        Process(builder, Start("th", 49));
        Process(builder, Character("head", 50));
        Process(builder, End("th", 51));
        Process(builder, End("tr", 52));
        Process(builder, End("thead", 53));
        Process(builder, Start("tbody", 54));
        Process(builder, Start("tr", 55));
        Process(builder, Start("td", 56));
        Process(builder, Character("body", 57));
        Process(builder, End("table", 58));

        HtmlNode[] children = table.GetChildElements();
        Assert.Multiple(() =>
        {
            Assert.That(children.Select(value => value.GetLocalName()),
                Is.EqualTo(new[] { "caption", "colgroup", "thead", "tbody" }));
            Assert.That(children[0].GetTextContent(), Is.EqualTo("title"));
            Assert.That(children[1].GetFirstChild().GetLocalName(),
                Is.EqualTo("col"));
            Assert.That(col.IsSelfClosingAcknowledged(), Is.True);
            Assert.That(children[2].GetTextContent(), Is.EqualTo("head"));
            Assert.That(children[3].GetTextContent(), Is.EqualTo("body"));
            Assert.That(builder.GetInsertionMode(), Is.EqualTo(
                HtmlInsertionMode.InBody));
            Assert.That(builder.GetOpenElements().GetCurrentNode(),
                Is.SameAs(body));
        });
    }

    [Test]
    public void DirectCellGeneratesTbodyAndRowWithUnknownSourceSpans()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode body = EnterBody(builder, document);

        Process(builder, Start("table", 40));
        Process(builder, Start("td", 41));
        Process(builder, Character("value", 42));

        HtmlNode table = body.GetLastChild();
        HtmlNode tbody = table.GetFirstChild();
        HtmlNode row = tbody.GetFirstChild();
        HtmlNode cell = row.GetFirstChild();
        Assert.Multiple(() =>
        {
            Assert.That(tbody.GetLocalName(), Is.EqualTo("tbody"));
            Assert.That(row.GetLocalName(), Is.EqualTo("tr"));
            Assert.That(cell.GetLocalName(), Is.EqualTo("td"));
            Assert.That(cell.GetTextContent(), Is.EqualTo("value"));
            Assert.That(tbody.GetSourceSpan().IsKnown(), Is.False);
            Assert.That(row.GetSourceSpan().IsKnown(), Is.False);
            Assert.That(cell.GetSourceSpan().IsKnown(), Is.True);
            Assert.That(builder.GetInsertionMode(), Is.EqualTo(
                HtmlInsertionMode.InCell));
        });

        Process(builder, End("table", 43));
        Assert.That(builder.GetOpenElements().GetCurrentNode(),
            Is.SameAs(body));
    }

    [Test]
    public void TableTextKeepsWhitespaceAndFosterParentsNonWhitespace()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode body = EnterBody(builder, document);

        Process(builder, Start("table", 40));
        HtmlNode table = body.GetLastChild();
        Process(builder, Character(" \n", 41));
        Assert.That(builder.GetInsertionMode(), Is.EqualTo(
            HtmlInsertionMode.InTableText));
        Process(builder, Start("tbody", 42));
        Process(builder, Character("misplaced", 43));
        Process(builder, Start("tr", 44));

        Assert.Multiple(() =>
        {
            Assert.That(body.GetChildren(), Has.Length.EqualTo(2));
            Assert.That(body.GetFirstChild().GetKind(),
                Is.EqualTo(HtmlNodeKind.Text));
            Assert.That(body.GetFirstChild().GetTextContent(),
                Is.EqualTo("misplaced"));
            Assert.That(body.GetLastChild(), Is.SameAs(table));
            Assert.That(table.GetFirstChild().GetKind(),
                Is.EqualTo(HtmlNodeKind.Text));
            Assert.That(table.GetFirstChild().GetTextContent(),
                Is.EqualTo(" \n"));
            Assert.That(builder.IsFosterParentingEnabled(), Is.False);
            Assert.That(builder.GetInsertionMode(), Is.EqualTo(
                HtmlInsertionMode.InRow));
        });
    }

    [Test]
    public void FosterParentedElementsPrecedeTableButRetainTheirText()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode body = EnterBody(builder, document);

        Process(builder, Start("table", 40));
        HtmlNode table = body.GetLastChild();
        Process(builder, Start("div", 41));
        Process(builder, Character("outside", 42));
        Process(builder, End("div", 43));
        Process(builder, Start("tbody", 44));

        HtmlNode fostered = body.GetFirstChild();
        Assert.Multiple(() =>
        {
            Assert.That(fostered.GetLocalName(), Is.EqualTo("div"));
            Assert.That(fostered.GetTextContent(), Is.EqualTo("outside"));
            Assert.That(fostered.GetNextSibling(), Is.SameAs(table));
            Assert.That(table.GetFirstChild().GetLocalName(),
                Is.EqualTo("tbody"));
            Assert.That(builder.GetInsertionMode(), Is.EqualTo(
                HtmlInsertionMode.InTableBody));
        });
    }

    [Test]
    public void CaptionBoundaryClosesOpenContentAndClearsFormattingMarker()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode body = EnterBody(builder, document);

        Process(builder, Start("table", 40));
        HtmlNode table = body.GetLastChild();
        Process(builder, Start("caption", 41));
        Process(builder, Start("b", 42));
        Process(builder, Character("caption", 43));
        Assert.That(builder.GetFormattingElements().GetCount(),
            Is.EqualTo(2));

        Process(builder, Start("tbody", 44));

        Assert.Multiple(() =>
        {
            Assert.That(table.GetFirstChild().GetLocalName(),
                Is.EqualTo("caption"));
            Assert.That(table.GetFirstChild().GetTextContent(),
                Is.EqualTo("caption"));
            Assert.That(table.GetLastChild().GetLocalName(),
                Is.EqualTo("tbody"));
            Assert.That(builder.GetFormattingElements().GetCount(), Is.Zero);
            Assert.That(builder.GetOpenElements().GetCurrentNode(),
                Is.SameAs(table.GetLastChild()));
            Assert.That(builder.GetDiagnostics(), Is.Not.Empty);
        });
    }

    [Test]
    public void DirectColGeneratesColumnGroupAndBoundaryReprocesses()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode body = EnterBody(builder, document);

        Process(builder, Start("table", 40));
        HtmlNode table = body.GetLastChild();
        HtmlToken col = Start("col", true, 41);
        Process(builder, col);
        Process(builder, Start("tbody", 42));

        HtmlNode colgroup = table.GetFirstChild();
        Assert.Multiple(() =>
        {
            Assert.That(colgroup.GetLocalName(), Is.EqualTo("colgroup"));
            Assert.That(colgroup.GetSourceSpan().IsKnown(), Is.False);
            Assert.That(colgroup.GetFirstChild().GetLocalName(),
                Is.EqualTo("col"));
            Assert.That(colgroup.GetNextSibling().GetLocalName(),
                Is.EqualTo("tbody"));
            Assert.That(col.IsSelfClosingAcknowledged(), Is.True);
            Assert.That(builder.GetInsertionMode(), Is.EqualTo(
                HtmlInsertionMode.InTableBody));
        });
    }

    [Test]
    public void StartingAnotherCellImplicitlyClosesCellAndItsFormatting()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode body = EnterBody(builder, document);

        Process(builder, Start("table", 40));
        Process(builder, Start("tbody", 41));
        Process(builder, Start("tr", 42));
        Process(builder, Start("td", 43));
        Process(builder, Start("b", 44));
        Process(builder, Character("first", 45));
        Process(builder, Start("th", 46));
        Process(builder, Character("second", 47));

        HtmlNode row = body.GetLastChild().GetFirstChild().GetFirstChild();
        HtmlNode[] cells = row.GetChildElements();
        Assert.Multiple(() =>
        {
            Assert.That(cells.Select(value => value.GetLocalName()),
                Is.EqualTo(new[] { "td", "th" }));
            Assert.That(cells[0].GetTextContent(), Is.EqualTo("first"));
            Assert.That(cells[1].GetTextContent(), Is.EqualTo("second"));
            Assert.That(builder.GetFormattingElements().GetCount(),
                Is.EqualTo(1));
            Assert.That(builder.GetFormattingElements().IsMarker(0), Is.True);
            Assert.That(builder.GetInsertionMode(), Is.EqualTo(
                HtmlInsertionMode.InCell));
            Assert.That(builder.GetDiagnostics(), Is.Not.Empty);
        });
    }

    [Test]
    public void HiddenInputStaysInTableAndDoesNotRemainOpen()
    {
        HtmlDocument document = CreateDocument();
        HtmlTreeBuilder builder = CreateBuilder(document);
        HtmlNode body = EnterBody(builder, document);

        Process(builder, Start("table", 40));
        HtmlNode table = body.GetLastChild();
        HtmlToken input = HtmlToken.CreateStartTag("input", new[]
        {
            HtmlAttribute.Create("type", "HIDDEN", Span(41))
        }, true, Span(41));
        Process(builder, input);

        Assert.Multiple(() =>
        {
            Assert.That(table.GetFirstChild().GetLocalName(),
                Is.EqualTo("input"));
            Assert.That(builder.GetOpenElements().GetCurrentNode(),
                Is.SameAs(table));
            Assert.That(builder.GetInsertionMode(), Is.EqualTo(
                HtmlInsertionMode.InTable));
            Assert.That(input.IsSelfClosingAcknowledged(), Is.True);
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

    static HtmlSourceSpan Span(int offset)
    {
        return HtmlSourceSpan.Create(
            HtmlSourcePosition.Create(offset, 1, offset + 1),
            HtmlSourcePosition.Create(offset + 1, 1, offset + 2));
    }
}
