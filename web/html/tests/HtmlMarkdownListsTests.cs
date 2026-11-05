
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlMarkdownListsTests
{
    [Test]
    public void NestedListsUseConfiguredMarkersAndContinuationIndentation()
    {
        HtmlNode fragment = ParseFragment(
            "<ul><li>Parent<ul><li>Child<br>continued</li></ul></li>"
            + "<li>Next</li></ul>");
        HtmlMarkdownOptions options =
            HtmlMarkdownOptions.CreateCommonMark();
        options.SetUnorderedListMarker("*");

        string markdown = Render(fragment, options);

        Assert.That(markdown, Is.EqualTo(
            "* Parent\n  * Child  \n    continued\n* Next"));
    }

    [Test]
    public void OrderedListsSupportSourceSequentialAndOneNumbering()
    {
        HtmlNode fragment = ParseFragment(
            "<ol start=3><li>three</li><li value=7>seven</li>"
            + "<li>eight</li></ol>");
        HtmlMarkdownOptions preserve =
            HtmlMarkdownOptions.CreateCommonMark();
        HtmlMarkdownOptions sequential = preserve.Copy();
        sequential.SetOrderedListNumbering(
            HtmlMarkdownOrderedListNumbering.Sequential);
        HtmlMarkdownOptions one = preserve.Copy();
        one.SetOrderedListNumbering(
            HtmlMarkdownOrderedListNumbering.One);

        Assert.Multiple(() =>
        {
            Assert.That(Render(fragment, preserve), Is.EqualTo(
                "3. three\n7. seven\n8. eight"));
            Assert.That(Render(fragment, sequential), Is.EqualTo(
                "3. three\n4. seven\n5. eight"));
            Assert.That(Render(fragment, one), Is.EqualTo(
                "1. three\n1. seven\n1. eight"));
        });
    }

    [Test]
    public void ReversedSequentialListsCountDownFromTheirSourceStart()
    {
        HtmlNode fragment = ParseFragment(
            "<ol reversed start=5><li>five</li><li value=2>two</li>"
            + "<li>one</li></ol>");
        HtmlMarkdownOptions preserve =
            HtmlMarkdownOptions.CreateCommonMark();
        HtmlMarkdownOptions sequential = preserve.Copy();
        sequential.SetOrderedListNumbering(
            HtmlMarkdownOrderedListNumbering.Sequential);

        Assert.Multiple(() =>
        {
            Assert.That(Render(fragment, preserve), Is.EqualTo(
                "5. five\n2. two\n1. one"));
            Assert.That(Render(fragment, sequential), Is.EqualTo(
                "5. five\n4. two\n3. one"));
        });
    }

    [Test]
    public void LooseListsSeparateItemsAndContinuationBlocks()
    {
        HtmlNode fragment = ParseFragment(
            "<ul><li><p>First</p><p>Second</p></li>"
            + "<li><p>Third</p></li></ul>");

        string markdown = Render(fragment,
            HtmlMarkdownOptions.CreateCommonMark());

        Assert.That(markdown, Is.EqualTo(
            "- First\n\n  Second\n\n- Third"));
    }

    [Test]
    public void MarkerWidthControlsContinuationBlockIndentation()
    {
        HtmlNode fragment = ParseFragment(
            "<ol start=10><li>intro<h3>Head</h3><p>tail</p></li></ol>");

        string markdown = Render(fragment,
            HtmlMarkdownOptions.CreateCommonMark());

        Assert.That(markdown, Is.EqualTo(
            "10. intro\n\n    ### Head\n\n    tail"));
    }

    [Test]
    public void GitHubProfileEmitsOnlyLeadingTaskCheckboxesAsMarkers()
    {
        HtmlNode fragment = ParseFragment(
            "<ul><li><input type=checkbox checked>Done</li>"
            + "<li><label><input type=checkbox> Todo</label></li>"
            + "<li>Later <input type=checkbox checked></li></ul>");

        string common = Render(fragment,
            HtmlMarkdownOptions.CreateCommonMark());
        string github = Render(fragment,
            HtmlMarkdownOptions.CreateGitHubFlavored());
        string wrapped = Render(ParseFragment(
            "<ul><li><p><span><input type=checkbox checked></span>"
            + "Wrapped</p></li></ul>"),
            HtmlMarkdownOptions.CreateGitHubFlavored());

        Assert.Multiple(() =>
        {
            Assert.That(common, Is.EqualTo("- Done\n- Todo\n- Later"));
            Assert.That(github, Is.EqualTo(
                "- [x] Done\n- [ ] Todo\n- Later"));
            Assert.That(wrapped, Is.EqualTo("- [x] Wrapped"));
        });
    }

    [Test]
    public void EmptyParentItemsCanContainNestedLists()
    {
        HtmlNode fragment = ParseFragment(
            "<ul><li><ul><li>child</li></ul></li></ul>");

        string markdown = Render(fragment,
            HtmlMarkdownOptions.CreateCommonMark());

        Assert.That(markdown, Is.EqualTo("-\n  - child"));
    }

    [Test]
    public void NestedBlockquotesPrefixContentBlankLinesAndLists()
    {
        HtmlNode fragment = ParseFragment(
            "<blockquote><p>Outer</p><blockquote><p>Inner<br>line</p>"
            + "<ul><li>item</li></ul></blockquote><p>End</p></blockquote>");

        string markdown = Render(fragment,
            HtmlMarkdownOptions.CreateCommonMark());

        Assert.That(markdown, Is.EqualTo(
            "> Outer\n>\n> > Inner  \n> > line\n> >\n> > - item\n>\n> End"));
    }

    [Test]
    public void BlockquotesPreserveConfiguredCrLfLineEndings()
    {
        HtmlNode fragment = ParseFragment(
            "<blockquote><p>One</p><p>Two</p></blockquote>");
        HtmlMarkdownOptions options =
            HtmlMarkdownOptions.CreateCommonMark();
        options.SetNewLine("\r\n");

        string markdown = Render(fragment, options);

        Assert.That(markdown, Is.EqualTo("> One\r\n>\r\n> Two"));
    }

    static string Render(HtmlNode node, HtmlMarkdownOptions options)
    {
        HtmlMarkdownWriter writer = HtmlMarkdownWriter.Create(
            options.Compile(), CancellationToken.None);
        writer.WriteNode(node);
        return writer.Complete();
    }

    static HtmlNode ParseFragment(string html)
    {
        return Html.ParseFragment(html, "body").GetFragment();
    }
}
