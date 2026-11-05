
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlMarkdownEscaperTests
{
    [Test]
    public void OrdinaryTextEscapesInlineMarkdownAndHtmlOpeners()
    {
        HtmlMarkdownContextStack stack = CreateStack(
            HtmlMarkdownOptions.CreateCommonMark());
        stack.SetLineStart(false);

        string escaped = stack.Escape(
            "*em* _under_ [label] `code` <tag> &copy; ![alt]");

        Assert.That(escaped, Is.EqualTo(
            "\\*em\\* \\_under\\_ \\[label\\] \\`code\\` "
            + "\\<tag> \\&copy; \\!\\[alt\\]"));
    }

    [Test]
    public void LineStartEscapingPreventsBlockConstructsAcrossLines()
    {
        HtmlMarkdownContextStack stack = CreateStack(
            HtmlMarkdownOptions.CreateCommonMark());

        string escaped = stack.Escape(
            "# heading\n> quote\n- item\n+ item\n"
            + "1. one\n2) two\n1234567890. ten\n"
            + "= underline\n~~~\n``` code\n<div>");

        Assert.That(escaped, Is.EqualTo(
            "\\# heading\n\\> quote\n\\- item\n\\+ item\n"
            + "1\\. one\n2\\) two\n1234567890. ten\n"
            + "\\= underline\n\\~~~\n\\`\\`\\` code\n\\<div>"));
    }

    [Test]
    public void StackTracksLineStartsAcrossChunkedWritesAndBalancesFrames()
    {
        HtmlMarkdownContextStack stack = CreateStack(
            HtmlMarkdownOptions.CreateCommonMark());

        string indentation = stack.Escape("  ");
        string heading = stack.Escape("# title\n");
        stack.PushWithDelimiter(HtmlMarkdownContextKind.Emphasis, "_");
        string emphasis = stack.Escape("a_b");
        HtmlMarkdownContextKind popped = stack.Pop();

        Assert.Multiple(() =>
        {
            Assert.That(indentation, Is.EqualTo("  "));
            Assert.That(heading, Is.EqualTo("\\# title\n"));
            Assert.That(emphasis, Is.EqualTo("a\\_b"));
            Assert.That(popped, Is.EqualTo(
                HtmlMarkdownContextKind.Emphasis));
            Assert.That(stack.GetCurrentKind(),
                Is.EqualTo(HtmlMarkdownContextKind.Text));
            Assert.That(stack.GetCurrentDelimiter(), Is.Empty);
            Assert.That(stack.GetDepth(), Is.EqualTo(1));
            Assert.That(stack.IsAtLineStart(), Is.False);
            Assert.Throws<InvalidOperationException>(() => stack.Pop());
        });
    }

    [Test]
    public void CodeContextsPreserveContentForDelimiterSelectionLater()
    {
        HtmlMarkdownContextStack stack = CreateStack(
            HtmlMarkdownOptions.CreateCommonMark());
        stack.PushWithDelimiter(HtmlMarkdownContextKind.CodeSpan, "``");
        string inline = stack.Escape("a * b [c] ` tick");
        stack.Pop();
        stack.PushWithDelimiter(HtmlMarkdownContextKind.CodeBlock, "````");
        string block = stack.Escape("# not a heading\n``` inside\n");
        stack.Pop();

        Assert.Multiple(() =>
        {
            Assert.That(inline, Is.EqualTo("a * b [c] ` tick"));
            Assert.That(block,
                Is.EqualTo("# not a heading\n``` inside\n"));
            Assert.That(stack.IsAtLineStart(), Is.True);
        });
    }

    [Test]
    public void LinkContextsEscapeTextDestinationsAndTitleDelimiters()
    {
        HtmlMarkdownContextStack stack = CreateStack(
            HtmlMarkdownOptions.CreateCommonMark());
        stack.SetLineStart(false);
        stack.Push(HtmlMarkdownContextKind.LinkText);
        string text = stack.Escape("[label] *strong*");
        stack.Pop();
        stack.Push(HtmlMarkdownContextKind.LinkDestination);
        string destination = stack.Escape(
            "https://x.test/a(b) c<q>\"");
        stack.Pop();
        stack.PushWithDelimiter(HtmlMarkdownContextKind.LinkTitle, "\"");
        string quotedTitle = stack.Escape("say \"hi\" \\ ok");
        stack.Pop();
        stack.PushWithDelimiter(HtmlMarkdownContextKind.LinkTitle, "(");
        string parenthesizedTitle = stack.Escape("close) open(");
        stack.Pop();

        Assert.Multiple(() =>
        {
            Assert.That(text,
                Is.EqualTo("\\[label\\] \\*strong\\*"));
            Assert.That(destination, Is.EqualTo(
                "https://x.test/a\\(b\\)%20c%3Cq%3E%22"));
            Assert.That(quotedTitle,
                Is.EqualTo("say \\\"hi\\\" \\\\ ok"));
            Assert.That(parenthesizedTitle,
                Is.EqualTo("close\\) open("));
        });
    }

    [Test]
    public void TableAndGfmContextsEscapeOnlyTheirAdditionalDelimiters()
    {
        HtmlMarkdownOptions github =
            HtmlMarkdownOptions.CreateGitHubFlavored();
        HtmlMarkdownContextStack stack = CreateStack(github);
        stack.SetLineStart(false);
        string ordinary = stack.Escape("A | ~~old~~");
        stack.Push(HtmlMarkdownContextKind.TableCell);
        string table = stack.Escape("A | *B* ~~old~~");
        stack.Pop();

        Assert.Multiple(() =>
        {
            Assert.That(ordinary,
                Is.EqualTo("A | \\~\\~old\\~\\~"));
            Assert.That(table,
                Is.EqualTo("A \\| \\*B\\* \\~\\~old\\~\\~"));
        });
    }

    [Test]
    public void RawHtmlIsEncodedUnlessTheCompiledProfileIsTrusted()
    {
        HtmlMarkdownContextStack safe = CreateStack(
            HtmlMarkdownOptions.CreateCommonMark());
        safe.Push(HtmlMarkdownContextKind.RawHtml);
        string encoded = safe.Escape("<span title=\"x\">&</span>");

        HtmlMarkdownOptions trustedOptions =
            HtmlMarkdownOptions.CreateCommonMark();
        trustedOptions.SetEnableTrustedRawHtml(true);
        HtmlMarkdownContextStack trusted = CreateStack(trustedOptions);
        trusted.Push(HtmlMarkdownContextKind.RawHtml);
        string raw = trusted.Escape("<span title=\"x\">&</span>");

        Assert.Multiple(() =>
        {
            Assert.That(encoded, Is.EqualTo(
                "&lt;span title=&quot;x&quot;&gt;&amp;&lt;/span&gt;"));
            Assert.That(raw,
                Is.EqualTo("<span title=\"x\">&</span>"));
        });
    }

    [Test]
    public void EscapingPreservesUnicodeScalarsAndCombiningSequences()
    {
        HtmlMarkdownContextStack stack = CreateStack(
            HtmlMarkdownOptions.CreateCommonMark());

        string escaped = stack.Escape(
            "東京 e\u0301 👨‍👩‍👧‍👦 *強調*");

        Assert.That(escaped,
            Is.EqualTo("東京 e\u0301 👨‍👩‍👧‍👦 \\*強調\\*"));
    }

    [Test]
    public void InvalidContextArgumentsAreRejected()
    {
        HtmlCompiledMarkdownOptions options =
            HtmlMarkdownOptions.CreateCommonMark().Compile();
        HtmlMarkdownContextStack stack =
            new HtmlMarkdownContextStack(options);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => stack.Escape(null));
            Assert.Throws<ArgumentNullException>(() =>
                stack.ObserveLiteral(null));
            Assert.Throws<ArgumentNullException>(() =>
                stack.PushWithDelimiter(HtmlMarkdownContextKind.Text, null));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                stack.Push((HtmlMarkdownContextKind)99));
            Assert.Throws<ArgumentNullException>(() =>
                HtmlMarkdownEscaper.Escape("x",
                    HtmlMarkdownContextKind.Text, null, true, options));
        });
    }

    static HtmlMarkdownContextStack CreateStack(
        HtmlMarkdownOptions options)
    {
        return new HtmlMarkdownContextStack(options.Compile());
    }
}
