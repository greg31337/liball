using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class CommonMarkInlineRulesTests
{
    [Test]
    public void TextRule_ParsesPlainTextRun()
    {
        MarkdownInlineParseContext context = CreateInlineContext("alpha beta");

        Assert.That(CommonMarkInlineRules.TryParseText(context), Is.True);

        Assert.That(context.Position, Is.EqualTo(10));
        Assert.That(context.Container.GetChildCount(), Is.EqualTo(1));
        Assert.That(context.Container.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Text));
        Assert.That(context.Container.GetChild(0).Literal, Is.EqualTo("alpha beta"));
    }

    [Test]
    public void TextRule_StopsPlainRunBeforeSpecialCharacters()
    {
        MarkdownInlineParseContext context = CreateInlineContext("alpha *beta");

        Assert.That(CommonMarkInlineRules.TryParseText(context), Is.True);

        Assert.That(context.Position, Is.EqualTo(6));
        Assert.That(context.Container.GetChild(0).Literal, Is.EqualTo("alpha "));
    }

    [Test]
    public void TextRule_PreservesUnmatchedSpecialCharactersAsText()
    {
        MarkdownInlineParseContext context = CreateInlineContext("*_[]!<~^+=$:&`\\\n");

        while (context.HasCurrentCharacter())
        {
            Assert.That(CommonMarkInlineRules.TryParseText(context), Is.True);
        }

        Assert.That(context.Container.GetChildCount(), Is.EqualTo(16));
        Assert.That(context.Container.GetChild(0).Literal, Is.EqualTo("*"));
        Assert.That(context.Container.GetChild(1).Literal, Is.EqualTo("_"));
        Assert.That(context.Container.GetChild(2).Literal, Is.EqualTo("["));
        Assert.That(context.Container.GetChild(3).Literal, Is.EqualTo("]"));
        Assert.That(context.Container.GetChild(4).Literal, Is.EqualTo("!"));
        Assert.That(context.Container.GetChild(5).Literal, Is.EqualTo("<"));
        Assert.That(context.Container.GetChild(6).Literal, Is.EqualTo("~"));
        Assert.That(context.Container.GetChild(7).Literal, Is.EqualTo("^"));
        Assert.That(context.Container.GetChild(8).Literal, Is.EqualTo("+"));
        Assert.That(context.Container.GetChild(9).Literal, Is.EqualTo("="));
        Assert.That(context.Container.GetChild(10).Literal, Is.EqualTo("$"));
        Assert.That(context.Container.GetChild(11).Literal, Is.EqualTo(":"));
        Assert.That(context.Container.GetChild(12).Literal, Is.EqualTo("&"));
        Assert.That(context.Container.GetChild(13).Literal, Is.EqualTo("`"));
        Assert.That(context.Container.GetChild(14).Literal, Is.EqualTo("\\"));
        Assert.That(context.Container.GetChild(15).Literal, Is.EqualTo("\n"));
    }

    [Test]
    public void Parser_DefaultPipelineCreatesTextChildrenForInlineContainers()
    {
        MarkdownDocument document = MarkdownParser.Parse("# Title\n\nparagraph text");
        MarkdownNode heading = document.Root.GetChild(0);
        MarkdownNode paragraph = document.Root.GetChild(1);

        Assert.That(heading.GetChildCount(), Is.EqualTo(1));
        Assert.That(heading.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Text));
        Assert.That(heading.GetChild(0).Literal, Is.EqualTo("Title"));
        Assert.That(paragraph.GetChildCount(), Is.EqualTo(1));
        Assert.That(paragraph.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Text));
        Assert.That(paragraph.GetChild(0).Literal, Is.EqualTo("paragraph text"));
    }

    [Test]
    public void Parser_DefaultPipelineCreatesSoftLineBreakNodes()
    {
        MarkdownDocument document = MarkdownParser.Parse("alpha\nbeta");
        MarkdownNode paragraph = document.Root.GetChild(0);

        Assert.That(paragraph.GetChildCount(), Is.EqualTo(3));
        Assert.That(paragraph.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Text));
        Assert.That(paragraph.GetChild(0).Literal, Is.EqualTo("alpha"));
        Assert.That(paragraph.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.SoftLineBreak));
        Assert.That(paragraph.GetChild(2).Kind, Is.EqualTo(MarkdownNodeKind.Text));
        Assert.That(paragraph.GetChild(2).Literal, Is.EqualTo("beta"));
    }

    [Test]
    public void Parser_DefaultPipelineCreatesHardLineBreakForBackslash()
    {
        MarkdownDocument document = MarkdownParser.Parse("alpha\\\nbeta");
        MarkdownNode paragraph = document.Root.GetChild(0);

        Assert.That(paragraph.GetChildCount(), Is.EqualTo(3));
        Assert.That(paragraph.GetChild(0).Literal, Is.EqualTo("alpha"));
        Assert.That(paragraph.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.HardLineBreak));
        Assert.That(paragraph.GetChild(2).Literal, Is.EqualTo("beta"));
    }

    [Test]
    public void Parser_DefaultPipelineParsesBackslashEscapedPunctuation()
    {
        MarkdownDocument document = MarkdownParser.Parse("\\*literal\\[\\`");
        MarkdownNode paragraph = document.Root.GetChild(0);

        Assert.That(paragraph.GetChildCount(), Is.EqualTo(4));
        Assert.That(paragraph.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Text));
        Assert.That(paragraph.GetChild(0).Literal, Is.EqualTo("*"));
        Assert.That(paragraph.GetChild(1).Literal, Is.EqualTo("literal"));
        Assert.That(paragraph.GetChild(2).Literal, Is.EqualTo("["));
        Assert.That(paragraph.GetChild(3).Literal, Is.EqualTo("`"));
    }

    [Test]
    public void Parser_DefaultPipelineDoesNotEscapeNonPunctuation()
    {
        MarkdownDocument document = MarkdownParser.Parse("\\a");
        MarkdownNode paragraph = document.Root.GetChild(0);

        Assert.That(paragraph.GetChildCount(), Is.EqualTo(2));
        Assert.That(paragraph.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Text));
        Assert.That(paragraph.GetChild(0).Literal, Is.EqualTo("\\"));
        Assert.That(paragraph.GetChild(1).Literal, Is.EqualTo("a"));
    }

    [Test]
    public void Parser_DefaultPipelineDecodesNamedAndNumericEntityReferences()
    {
        string emoji = char.ConvertFromUtf32(128512);
        MarkdownDocument document = MarkdownParser.Parse("Tom &amp; &#35; &#x1F600;");
        MarkdownNode paragraph = document.Root.GetChild(0);

        Assert.That(paragraph.GetChildCount(), Is.EqualTo(6));
        Assert.That(paragraph.GetChild(0).Literal, Is.EqualTo("Tom "));
        Assert.That(paragraph.GetChild(1).Literal, Is.EqualTo("&"));
        Assert.That(paragraph.GetChild(2).Literal, Is.EqualTo(" "));
        Assert.That(paragraph.GetChild(3).Literal, Is.EqualTo("#"));
        Assert.That(paragraph.GetChild(4).Literal, Is.EqualTo(" "));
        Assert.That(paragraph.GetChild(5).Literal, Is.EqualTo(emoji));
    }

    [Test]
    public void Parser_DefaultPipelineLeavesMalformedEntityReferencesLiteral()
    {
        MarkdownDocument document = MarkdownParser.Parse("&unknown; &amp");
        MarkdownNode paragraph = document.Root.GetChild(0);

        Assert.That(paragraph.GetChildCount(), Is.EqualTo(4));
        Assert.That(paragraph.GetChild(0).Literal, Is.EqualTo("&"));
        Assert.That(paragraph.GetChild(1).Literal, Is.EqualTo("unknown; "));
        Assert.That(paragraph.GetChild(2).Literal, Is.EqualTo("&"));
        Assert.That(paragraph.GetChild(3).Literal, Is.EqualTo("amp"));
    }

    [Test]
    public void Parser_DefaultPipelineDoesNotDecodeEntityReferencesInsideCodeSpans()
    {
        MarkdownDocument document = MarkdownParser.Parse("`&amp;`");
        MarkdownNode paragraph = document.Root.GetChild(0);

        Assert.That(paragraph.GetChildCount(), Is.EqualTo(1));
        Assert.That(paragraph.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.CodeSpan));
        Assert.That(paragraph.GetChild(0).Literal, Is.EqualTo("&amp;"));
    }

    [Test]
    public void Parser_DefaultPipelineCreatesHardLineBreakForTrailingSpaces()
    {
        MarkdownDocument document = MarkdownParser.Parse("alpha   \nbeta");
        MarkdownNode paragraph = document.Root.GetChild(0);

        Assert.That(paragraph.GetChildCount(), Is.EqualTo(3));
        Assert.That(paragraph.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Text));
        Assert.That(paragraph.GetChild(0).Literal, Is.EqualTo("alpha"));
        Assert.That(paragraph.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.HardLineBreak));
        Assert.That(paragraph.GetChild(2).Literal, Is.EqualTo("beta"));
    }

    [Test]
    public void Parser_DefaultPipelineTrimsSingleTrailingSpaceBeforeSoftBreak()
    {
        MarkdownDocument document = MarkdownParser.Parse("alpha \nbeta");
        MarkdownNode paragraph = document.Root.GetChild(0);

        Assert.That(paragraph.GetChildCount(), Is.EqualTo(3));
        Assert.That(paragraph.GetChild(0).Literal, Is.EqualTo("alpha"));
        Assert.That(paragraph.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.SoftLineBreak));
        Assert.That(paragraph.GetChild(2).Literal, Is.EqualTo("beta"));
    }

    [Test]
    public void Parser_DefaultPipelineCreatesCodeSpanNodes()
    {
        MarkdownDocument document = MarkdownParser.Parse("alpha `code` beta");
        MarkdownNode paragraph = document.Root.GetChild(0);

        Assert.That(paragraph.GetChildCount(), Is.EqualTo(3));
        Assert.That(paragraph.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Text));
        Assert.That(paragraph.GetChild(0).Literal, Is.EqualTo("alpha "));
        Assert.That(paragraph.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.CodeSpan));
        Assert.That(paragraph.GetChild(1).Literal, Is.EqualTo("code"));
        Assert.That(paragraph.GetChild(2).Kind, Is.EqualTo(MarkdownNodeKind.Text));
        Assert.That(paragraph.GetChild(2).Literal, Is.EqualTo(" beta"));
    }

    [Test]
    public void CodeSpanRule_MatchesArbitraryBacktickRunLengths()
    {
        MarkdownDocument document = MarkdownParser.Parse("`` code ` here ``");
        MarkdownNode paragraph = document.Root.GetChild(0);

        Assert.That(paragraph.GetChildCount(), Is.EqualTo(1));
        Assert.That(paragraph.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.CodeSpan));
        Assert.That(paragraph.GetChild(0).Literal, Is.EqualTo("code ` here"));
    }

    [Test]
    public void CodeSpanRule_NormalizesLineEndingsAndSurroundingSpaces()
    {
        MarkdownDocument lineEnding = MarkdownParser.Parse("`alpha\nbeta`");
        MarkdownDocument surroundingSpaces = MarkdownParser.Parse("`  alpha  `");
        MarkdownDocument allSpaces = MarkdownParser.Parse("`   `");

        Assert.That(lineEnding.Root.GetChild(0).GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.CodeSpan));
        Assert.That(lineEnding.Root.GetChild(0).GetChild(0).Literal, Is.EqualTo("alpha beta"));
        Assert.That(surroundingSpaces.Root.GetChild(0).GetChild(0).Literal, Is.EqualTo(" alpha "));
        Assert.That(allSpaces.Root.GetChild(0).GetChild(0).Literal, Is.EqualTo("   "));
    }

    [Test]
    public void CodeSpanRule_IgnoresDifferentClosingBacktickRunLengths()
    {
        MarkdownDocument document = MarkdownParser.Parse("``code");
        MarkdownNode paragraph = document.Root.GetChild(0);

        Assert.That(paragraph.GetChildCount(), Is.EqualTo(2));
        Assert.That(paragraph.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Text));
        Assert.That(paragraph.GetChild(0).Literal, Is.EqualTo("``"));
        Assert.That(paragraph.GetChild(1).Literal, Is.EqualTo("code"));
    }

    [Test]
    public void Parser_TextRuleSplitsRunsAroundFutureInlineMarkers()
    {
        MarkdownDocument document = MarkdownParser.Parse("a *b* [c]");
        MarkdownNode paragraph = document.Root.GetChild(0);

        Assert.That(paragraph.GetChildCount(), Is.EqualTo(6));
        Assert.That(paragraph.GetChild(0).Literal, Is.EqualTo("a "));
        Assert.That(paragraph.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.Emphasis));
        Assert.That(paragraph.GetChild(1).GetChildCount(), Is.EqualTo(1));
        Assert.That(paragraph.GetChild(1).GetChild(0).Literal, Is.EqualTo("b"));
        Assert.That(paragraph.GetChild(2).Literal, Is.EqualTo(" "));
        Assert.That(paragraph.GetChild(3).Literal, Is.EqualTo("["));
        Assert.That(paragraph.GetChild(4).Literal, Is.EqualTo("c"));
        Assert.That(paragraph.GetChild(5).Literal, Is.EqualTo("]"));
    }

    [Test]
    public void Parser_DefaultPipelineCreatesUriAutolinkNodes()
    {
        MarkdownDocument document = MarkdownParser.Parse("go <https://example.com/a?b=c> now");
        MarkdownNode paragraph = document.Root.GetChild(0);
        MarkdownNode link = paragraph.GetChild(1);

        Assert.That(paragraph.GetChildCount(), Is.EqualTo(3));
        Assert.That(paragraph.GetChild(0).Literal, Is.EqualTo("go "));
        Assert.That(link.Kind, Is.EqualTo(MarkdownNodeKind.Link));
        Assert.That(link.GetAttributeValue("destination"), Is.EqualTo("https://example.com/a?b=c"));
        Assert.That(link.GetChildCount(), Is.EqualTo(1));
        Assert.That(link.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Text));
        Assert.That(link.GetChild(0).Literal, Is.EqualTo("https://example.com/a?b=c"));
        Assert.That(paragraph.GetChild(2).Literal, Is.EqualTo(" now"));
    }

    [Test]
    public void Parser_DefaultPipelineCreatesEmailAutolinkNodes()
    {
        MarkdownDocument document = MarkdownParser.Parse("email <user.name+tag@example.co.uk>");
        MarkdownNode paragraph = document.Root.GetChild(0);
        MarkdownNode link = paragraph.GetChild(1);

        Assert.That(paragraph.GetChildCount(), Is.EqualTo(2));
        Assert.That(paragraph.GetChild(0).Literal, Is.EqualTo("email "));
        Assert.That(link.Kind, Is.EqualTo(MarkdownNodeKind.Link));
        Assert.That(link.GetAttributeValue("destination"), Is.EqualTo("mailto:user.name+tag@example.co.uk"));
        Assert.That(link.GetChildCount(), Is.EqualTo(1));
        Assert.That(link.GetChild(0).Literal, Is.EqualTo("user.name+tag@example.co.uk"));
    }

    [Test]
    public void AutolinkRule_LeavesMalformedAutolinksAsText()
    {
        string source = "<http://exa mple> <not-email@example>";
        MarkdownDocument document = MarkdownParser.Parse(source);
        MarkdownNode paragraph = document.Root.GetChild(0);

        Assert.That(ContainsInlineKind(paragraph, MarkdownNodeKind.Link), Is.False);
        Assert.That(ConcatenateInlineLiterals(paragraph), Is.EqualTo(source.TrimEnd()));
    }

    [Test]
    public void AutolinkRule_DoesNotParseInsideCodeSpans()
    {
        MarkdownDocument document = MarkdownParser.Parse("`<https://example.com>`");
        MarkdownNode paragraph = document.Root.GetChild(0);

        Assert.That(paragraph.GetChildCount(), Is.EqualTo(1));
        Assert.That(paragraph.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.CodeSpan));
        Assert.That(paragraph.GetChild(0).Literal, Is.EqualTo("<https://example.com>"));
    }

    [Test]
    public void Parser_DefaultPipelineCreatesHtmlInlineTagNodes()
    {
        MarkdownDocument document = MarkdownParser.Parse("a <span data-x=\"a>b\">text</span> b");
        MarkdownNode paragraph = document.Root.GetChild(0);

        Assert.That(paragraph.GetChildCount(), Is.EqualTo(5));
        Assert.That(paragraph.GetChild(0).Literal, Is.EqualTo("a "));
        Assert.That(paragraph.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.HtmlInline));
        Assert.That(paragraph.GetChild(1).Literal, Is.EqualTo("<span data-x=\"a>b\">"));
        Assert.That(paragraph.GetChild(2).Literal, Is.EqualTo("text"));
        Assert.That(paragraph.GetChild(3).Kind, Is.EqualTo(MarkdownNodeKind.HtmlInline));
        Assert.That(paragraph.GetChild(3).Literal, Is.EqualTo("</span>"));
        Assert.That(paragraph.GetChild(4).Literal, Is.EqualTo(" b"));
    }

    [Test]
    public void HtmlInlineRule_ParsesCommentInstructionDeclarationAndCdata()
    {
        MarkdownDocument comment = MarkdownParser.Parse("a <!-- ok -->");
        MarkdownDocument instruction = MarkdownParser.Parse("a <?pi?>");
        MarkdownDocument declaration = MarkdownParser.Parse("a <!DOCTYPE html>");
        MarkdownDocument cdata = MarkdownParser.Parse("a <![CDATA[x<y]]>");

        Assert.That(comment.Root.GetChild(0).GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.HtmlInline));
        Assert.That(comment.Root.GetChild(0).GetChild(1).Literal, Is.EqualTo("<!-- ok -->"));
        Assert.That(instruction.Root.GetChild(0).GetChild(1).Literal, Is.EqualTo("<?pi?>"));
        Assert.That(declaration.Root.GetChild(0).GetChild(1).Literal, Is.EqualTo("<!DOCTYPE html>"));
        Assert.That(cdata.Root.GetChild(0).GetChild(1).Literal, Is.EqualTo("<![CDATA[x<y]]>"));
    }

    [Test]
    public void HtmlInlineRule_LeavesMalformedHtmlAsText()
    {
        string source = "a <span";
        MarkdownDocument document = MarkdownParser.Parse(source);
        MarkdownNode paragraph = document.Root.GetChild(0);

        Assert.That(ContainsInlineKind(paragraph, MarkdownNodeKind.HtmlInline), Is.False);
        Assert.That(ConcatenateInlineLiterals(paragraph), Is.EqualTo(source));
    }

    [Test]
    public void HtmlInlineRule_DoesNotParseInsideCodeSpans()
    {
        MarkdownDocument document = MarkdownParser.Parse("`<span>`");
        MarkdownNode paragraph = document.Root.GetChild(0);

        Assert.That(paragraph.GetChildCount(), Is.EqualTo(1));
        Assert.That(paragraph.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.CodeSpan));
        Assert.That(paragraph.GetChild(0).Literal, Is.EqualTo("<span>"));
    }

    [Test]
    public void Parser_DefaultPipelineCreatesImageNodes()
    {
        MarkdownDocument document = MarkdownParser.Parse("see ![alt](image.png \"title\") now");
        MarkdownNode paragraph = document.Root.GetChild(0);
        MarkdownNode image = paragraph.GetChild(1);

        Assert.That(paragraph.GetChildCount(), Is.EqualTo(3));
        Assert.That(paragraph.GetChild(0).Literal, Is.EqualTo("see "));
        Assert.That(image.Kind, Is.EqualTo(MarkdownNodeKind.Image));
        Assert.That(image.GetAttributeValue("destination"), Is.EqualTo("image.png"));
        Assert.That(image.GetAttributeValue("title"), Is.EqualTo("title"));
        Assert.That(image.GetChildCount(), Is.EqualTo(1));
        Assert.That(image.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Text));
        Assert.That(image.GetChild(0).Literal, Is.EqualTo("alt"));
        Assert.That(paragraph.GetChild(2).Literal, Is.EqualTo(" now"));
    }

    [Test]
    public void ImageRule_ParsesNestedInlineAltContent()
    {
        MarkdownDocument document = MarkdownParser.Parse("![**alt** &amp; text](image.png)");
        MarkdownNode paragraph = document.Root.GetChild(0);
        MarkdownNode image = paragraph.GetChild(0);

        Assert.That(image.Kind, Is.EqualTo(MarkdownNodeKind.Image));
        Assert.That(image.GetAttributeValue("destination"), Is.EqualTo("image.png"));
        Assert.That(image.GetChildCount(), Is.EqualTo(4));
        Assert.That(image.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.StrongEmphasis));
        Assert.That(image.GetChild(0).GetChild(0).Literal, Is.EqualTo("alt"));
        Assert.That(image.GetChild(1).Literal, Is.EqualTo(" "));
        Assert.That(image.GetChild(2).Literal, Is.EqualTo("&"));
        Assert.That(image.GetChild(3).Literal, Is.EqualTo(" text"));
    }

    [Test]
    public void ImageRule_ParsesFullReferenceImages()
    {
        MarkdownDocument document = MarkdownParser.Parse("![alt][img]\n\n[img]: /image.png \"title\"");
        MarkdownNode paragraph = document.Root.GetChild(0);
        MarkdownNode image = paragraph.GetChild(0);

        Assert.That(paragraph.GetChildCount(), Is.EqualTo(1));
        Assert.That(image.Kind, Is.EqualTo(MarkdownNodeKind.Image));
        Assert.That(image.GetAttributeValue("destination"), Is.EqualTo("/image.png"));
        Assert.That(image.GetAttributeValue("title"), Is.EqualTo("title"));
        Assert.That(image.GetChildCount(), Is.EqualTo(1));
        Assert.That(image.GetChild(0).Literal, Is.EqualTo("alt"));
    }

    [Test]
    public void ImageRule_ParsesCollapsedReferenceImages()
    {
        MarkdownDocument document = MarkdownParser.Parse("![img][]\n\n[img]: /image.png");
        MarkdownNode paragraph = document.Root.GetChild(0);
        MarkdownNode image = paragraph.GetChild(0);

        Assert.That(image.Kind, Is.EqualTo(MarkdownNodeKind.Image));
        Assert.That(image.GetAttributeValue("destination"), Is.EqualTo("/image.png"));
        Assert.That(image.GetChildCount(), Is.EqualTo(1));
        Assert.That(image.GetChild(0).Literal, Is.EqualTo("img"));
    }

    [Test]
    public void ImageRule_ParsesShortcutReferenceImages()
    {
        MarkdownDocument document = MarkdownParser.Parse("![img]\n\n[img]: /image.png");
        MarkdownNode paragraph = document.Root.GetChild(0);
        MarkdownNode image = paragraph.GetChild(0);

        Assert.That(image.Kind, Is.EqualTo(MarkdownNodeKind.Image));
        Assert.That(image.GetAttributeValue("destination"), Is.EqualTo("/image.png"));
        Assert.That(image.GetChildCount(), Is.EqualTo(1));
        Assert.That(image.GetChild(0).Literal, Is.EqualTo("img"));
    }

    [Test]
    public void ImageRule_LeavesMalformedImagesAsText()
    {
        string source = "a ![broken](url";
        MarkdownDocument document = MarkdownParser.Parse(source);
        MarkdownNode paragraph = document.Root.GetChild(0);

        Assert.That(ContainsInlineKind(paragraph, MarkdownNodeKind.Image), Is.False);
        Assert.That(ConcatenateInlineLiterals(paragraph), Is.EqualTo(source));
    }

    [Test]
    public void ImageRule_DoesNotParseInsideCodeSpans()
    {
        MarkdownDocument document = MarkdownParser.Parse("`![x](url)`");
        MarkdownNode paragraph = document.Root.GetChild(0);

        Assert.That(paragraph.GetChildCount(), Is.EqualTo(1));
        Assert.That(paragraph.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.CodeSpan));
        Assert.That(paragraph.GetChild(0).Literal, Is.EqualTo("![x](url)"));
    }

    [Test]
    public void Parser_DefaultPipelineCreatesInlineLinkNodes()
    {
        MarkdownDocument document = MarkdownParser.Parse("see [site](https://example.com) now");
        MarkdownNode paragraph = document.Root.GetChild(0);
        MarkdownNode link = paragraph.GetChild(1);

        Assert.That(paragraph.GetChildCount(), Is.EqualTo(3));
        Assert.That(paragraph.GetChild(0).Literal, Is.EqualTo("see "));
        Assert.That(link.Kind, Is.EqualTo(MarkdownNodeKind.Link));
        Assert.That(link.GetAttributeValue("destination"), Is.EqualTo("https://example.com"));
        Assert.That(link.HasAttribute("title"), Is.False);
        Assert.That(link.GetChildCount(), Is.EqualTo(1));
        Assert.That(link.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Text));
        Assert.That(link.GetChild(0).Literal, Is.EqualTo("site"));
        Assert.That(paragraph.GetChild(2).Literal, Is.EqualTo(" now"));
    }

    [Test]
    public void LinkRule_ParsesTitleAndNestedInlineLabel()
    {
        MarkdownDocument document = MarkdownParser.Parse("[**site** &amp; docs](<https://example.com/a b> \"Example title\")");
        MarkdownNode paragraph = document.Root.GetChild(0);
        MarkdownNode link = paragraph.GetChild(0);

        Assert.That(paragraph.GetChildCount(), Is.EqualTo(1));
        Assert.That(link.Kind, Is.EqualTo(MarkdownNodeKind.Link));
        Assert.That(link.GetAttributeValue("destination"), Is.EqualTo("https://example.com/a b"));
        Assert.That(link.GetAttributeValue("title"), Is.EqualTo("Example title"));
        Assert.That(link.GetChildCount(), Is.EqualTo(4));
        Assert.That(link.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.StrongEmphasis));
        Assert.That(link.GetChild(0).GetChild(0).Literal, Is.EqualTo("site"));
        Assert.That(link.GetChild(1).Literal, Is.EqualTo(" "));
        Assert.That(link.GetChild(2).Literal, Is.EqualTo("&"));
        Assert.That(link.GetChild(3).Literal, Is.EqualTo(" docs"));
    }

    [Test]
    public void LinkRule_AllowsEmptyLabelAndDestination()
    {
        MarkdownDocument document = MarkdownParser.Parse("[]()");
        MarkdownNode paragraph = document.Root.GetChild(0);
        MarkdownNode link = paragraph.GetChild(0);

        Assert.That(paragraph.GetChildCount(), Is.EqualTo(1));
        Assert.That(link.Kind, Is.EqualTo(MarkdownNodeKind.Link));
        Assert.That(link.GetAttributeValue("destination"), Is.EqualTo(""));
        Assert.That(link.GetChildCount(), Is.EqualTo(0));
    }

    [Test]
    public void LinkRule_DecodesDestinationAndTitleEscapes()
    {
        MarkdownDocument document = MarkdownParser.Parse("[x](a\\)b&amp;c \"a \\\"quote\\\" &amp;\")");
        MarkdownNode paragraph = document.Root.GetChild(0);
        MarkdownNode link = paragraph.GetChild(0);

        Assert.That(link.Kind, Is.EqualTo(MarkdownNodeKind.Link));
        Assert.That(link.GetAttributeValue("destination"), Is.EqualTo("a)b&c"));
        Assert.That(link.GetAttributeValue("title"), Is.EqualTo("a \"quote\" &"));
    }

    [Test]
    public void LinkRule_ParsesBalancedBareDestinationParentheses()
    {
        MarkdownDocument document = MarkdownParser.Parse("[x](foo(bar))");
        MarkdownNode paragraph = document.Root.GetChild(0);
        MarkdownNode link = paragraph.GetChild(0);

        Assert.That(link.Kind, Is.EqualTo(MarkdownNodeKind.Link));
        Assert.That(link.GetAttributeValue("destination"), Is.EqualTo("foo(bar)"));
    }

    [Test]
    public void LinkRule_RejectsUnescapedBareDestinationWhitespace()
    {
        string source = "[x](foo bar)";
        MarkdownDocument document = MarkdownParser.Parse(source);
        MarkdownNode paragraph = document.Root.GetChild(0);

        Assert.That(ContainsInlineKind(paragraph, MarkdownNodeKind.Link), Is.False);
        Assert.That(ConcatenateInlineLiterals(paragraph), Is.EqualTo(source));
    }

    [Test]
    public void LinkRule_LeavesMalformedInlineLinksAsText()
    {
        string source = "a [broken](url";
        MarkdownDocument document = MarkdownParser.Parse(source);
        MarkdownNode paragraph = document.Root.GetChild(0);

        Assert.That(ContainsInlineKind(paragraph, MarkdownNodeKind.Link), Is.False);
        Assert.That(ConcatenateInlineLiterals(paragraph), Is.EqualTo(source));
    }

    [Test]
    public void LinkRule_LeavesInlineLinksWithMissingTitleAsText()
    {
        string source = "a [broken](url ";
        MarkdownDocument document = MarkdownParser.Parse(source);
        MarkdownNode paragraph = document.Root.GetChild(0);
        MarkdownInlineLinkParseResult result = CommonMarkInlineRules.ParseInlineLinkDestinationAndTitle("[broken](url ", 8);

        Assert.That(result.Success, Is.False);
        Assert.That(ContainsInlineKind(paragraph, MarkdownNodeKind.Link), Is.False);
        Assert.That(ConcatenateInlineLiterals(paragraph), Is.EqualTo(source.TrimEnd()));
    }

    [Test]
    public void LinkRule_DoesNotParseInsideCodeSpans()
    {
        MarkdownDocument document = MarkdownParser.Parse("`[x](url)`");
        MarkdownNode paragraph = document.Root.GetChild(0);

        Assert.That(paragraph.GetChildCount(), Is.EqualTo(1));
        Assert.That(paragraph.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.CodeSpan));
        Assert.That(paragraph.GetChild(0).Literal, Is.EqualTo("[x](url)"));
    }

    [Test]
    public void ReferenceLinkRule_ParsesFullReferenceLinks()
    {
        MarkdownDocument document = MarkdownParser.Parse("[text][ref]\n\n[ref]: /url \"title\"");
        MarkdownNode paragraph = document.Root.GetChild(0);
        MarkdownNode link = paragraph.GetChild(0);

        Assert.That(paragraph.GetChildCount(), Is.EqualTo(1));
        Assert.That(link.Kind, Is.EqualTo(MarkdownNodeKind.Link));
        Assert.That(link.GetAttributeValue("destination"), Is.EqualTo("/url"));
        Assert.That(link.GetAttributeValue("title"), Is.EqualTo("title"));
        Assert.That(link.GetChildCount(), Is.EqualTo(1));
        Assert.That(link.GetChild(0).Literal, Is.EqualTo("text"));
    }

    [Test]
    public void ReferenceLinkRule_ParsesCollapsedReferenceLinks()
    {
        MarkdownDocument document = MarkdownParser.Parse("[ref][]\n\n[ref]: /url");
        MarkdownNode paragraph = document.Root.GetChild(0);
        MarkdownNode link = paragraph.GetChild(0);

        Assert.That(paragraph.GetChildCount(), Is.EqualTo(1));
        Assert.That(link.Kind, Is.EqualTo(MarkdownNodeKind.Link));
        Assert.That(link.GetAttributeValue("destination"), Is.EqualTo("/url"));
        Assert.That(link.GetChildCount(), Is.EqualTo(1));
        Assert.That(link.GetChild(0).Literal, Is.EqualTo("ref"));
    }

    [Test]
    public void ReferenceLinkRule_ParsesShortcutReferenceLinks()
    {
        MarkdownDocument document = MarkdownParser.Parse("[ref]\n\n[ref]: /url");
        MarkdownNode paragraph = document.Root.GetChild(0);
        MarkdownNode link = paragraph.GetChild(0);

        Assert.That(paragraph.GetChildCount(), Is.EqualTo(1));
        Assert.That(link.Kind, Is.EqualTo(MarkdownNodeKind.Link));
        Assert.That(link.GetAttributeValue("destination"), Is.EqualTo("/url"));
        Assert.That(link.GetChildCount(), Is.EqualTo(1));
        Assert.That(link.GetChild(0).Literal, Is.EqualTo("ref"));
    }

    [Test]
    public void ReferenceLinkRule_NormalizesReferenceLabels()
    {
        MarkdownDocument document = MarkdownParser.Parse("[A   B]\n\n[a b]: /url");
        MarkdownNode paragraph = document.Root.GetChild(0);
        MarkdownNode link = paragraph.GetChild(0);

        Assert.That(link.Kind, Is.EqualTo(MarkdownNodeKind.Link));
        Assert.That(link.GetAttributeValue("destination"), Is.EqualTo("/url"));
        Assert.That(link.GetChild(0).Literal, Is.EqualTo("A   B"));
    }

    [Test]
    public void ReferenceLinkRule_UsesExplicitFullReferenceLabel()
    {
        MarkdownDocument document = MarkdownParser.Parse("[text][ref]\n\n[text]: /text\n[ref]: /ref");
        MarkdownNode paragraph = document.Root.GetChild(0);
        MarkdownNode link = paragraph.GetChild(0);

        Assert.That(link.Kind, Is.EqualTo(MarkdownNodeKind.Link));
        Assert.That(link.GetAttributeValue("destination"), Is.EqualTo("/ref"));
        Assert.That(link.GetChild(0).Literal, Is.EqualTo("text"));
    }

    [Test]
    public void ReferenceLinkRule_UsesFirstMatchingDefinition()
    {
        MarkdownDocument document = MarkdownParser.Parse("[ref]\n\n[ref]: /first\n[ref]: /second");
        MarkdownNode paragraph = document.Root.GetChild(0);
        MarkdownNode link = paragraph.GetChild(0);

        Assert.That(link.Kind, Is.EqualTo(MarkdownNodeKind.Link));
        Assert.That(link.GetAttributeValue("destination"), Is.EqualTo("/first"));
    }

    [Test]
    public void ReferenceLinkRule_LeavesMissingReferencesAsText()
    {
        string source = "[text][missing]\n\n[text]: /shortcut";
        MarkdownDocument document = MarkdownParser.Parse(source);
        MarkdownNode paragraph = document.Root.GetChild(0);

        Assert.That(ContainsInlineKind(paragraph, MarkdownNodeKind.Link), Is.False);
        Assert.That(ConcatenateInlineLiterals(paragraph), Is.EqualTo("[text][missing]"));
    }

    [Test]
    public void LinkRule_PrefersNestedDirectLinks()
    {
        MarkdownDocument document = MarkdownParser.Parse("[outer [inner](inner)](outer)");
        MarkdownNode paragraph = document.Root.GetChild(0);
        MarkdownNode link = paragraph.GetChild(2);

        Assert.That(paragraph.GetChildCount(), Is.EqualTo(5));
        Assert.That(link.Kind, Is.EqualTo(MarkdownNodeKind.Link));
        Assert.That(link.GetAttributeValue("destination"), Is.EqualTo("inner"));
        Assert.That(ContainsInlineKind(link, MarkdownNodeKind.Link), Is.False);
        Assert.That(ConcatenateInlineLiterals(link), Is.EqualTo("inner"));
    }

    [Test]
    public void LinkRule_PrefersNestedReferenceLinks()
    {
        MarkdownDocument document = MarkdownParser.Parse("[outer [inner][ref]](outer)\n\n[ref]: inner");
        MarkdownNode paragraph = document.Root.GetChild(0);
        MarkdownNode link = paragraph.GetChild(2);

        Assert.That(link.Kind, Is.EqualTo(MarkdownNodeKind.Link));
        Assert.That(link.GetAttributeValue("destination"), Is.EqualTo("inner"));
        Assert.That(ContainsInlineKind(link, MarkdownNodeKind.Link), Is.False);
        Assert.That(ConcatenateInlineLiterals(link), Is.EqualTo("inner"));
    }

    [Test]
    public void AutolinkRule_DoesNotCreateNestedLinks()
    {
        MarkdownDocument document = MarkdownParser.Parse("[<https://inner.example>](outer)");
        MarkdownNode paragraph = document.Root.GetChild(0);
        MarkdownNode link = paragraph.GetChild(0);

        Assert.That(link.Kind, Is.EqualTo(MarkdownNodeKind.Link));
        Assert.That(ContainsInlineKind(link, MarkdownNodeKind.Link), Is.False);
        Assert.That(ConcatenateInlineLiterals(link), Is.EqualTo("<https://inner.example>"));
    }

    [Test]
    public void LinkRule_AllowsImagesInsideLinks()
    {
        MarkdownDocument document = MarkdownParser.Parse("[![alt](image.png)](outer)");
        MarkdownNode paragraph = document.Root.GetChild(0);
        MarkdownNode link = paragraph.GetChild(0);

        Assert.That(link.Kind, Is.EqualTo(MarkdownNodeKind.Link));
        Assert.That(link.GetAttributeValue("destination"), Is.EqualTo("outer"));
        Assert.That(link.GetChildCount(), Is.EqualTo(1));
        Assert.That(link.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Image));
        Assert.That(link.GetChild(0).GetAttributeValue("destination"), Is.EqualTo("image.png"));
    }

    [Test]
    public void LinkRule_PrefersInlineLinksOverReferenceLinks()
    {
        MarkdownDocument document = MarkdownParser.Parse("[ref](/direct)\n\n[ref]: /reference");
        MarkdownNode paragraph = document.Root.GetChild(0);
        MarkdownNode link = paragraph.GetChild(0);

        Assert.That(link.Kind, Is.EqualTo(MarkdownNodeKind.Link));
        Assert.That(link.GetAttributeValue("destination"), Is.EqualTo("/direct"));
    }

    [Test]
    public void Parser_DefaultPipelineCreatesEmphasisNodes()
    {
        MarkdownDocument document = MarkdownParser.Parse("*alpha* _beta_");
        MarkdownNode paragraph = document.Root.GetChild(0);

        Assert.That(paragraph.GetChildCount(), Is.EqualTo(3));
        Assert.That(paragraph.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Emphasis));
        Assert.That(paragraph.GetChild(0).GetChildCount(), Is.EqualTo(1));
        Assert.That(paragraph.GetChild(0).GetChild(0).Literal, Is.EqualTo("alpha"));
        Assert.That(paragraph.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.Text));
        Assert.That(paragraph.GetChild(1).Literal, Is.EqualTo(" "));
        Assert.That(paragraph.GetChild(2).Kind, Is.EqualTo(MarkdownNodeKind.Emphasis));
        Assert.That(paragraph.GetChild(2).GetChildCount(), Is.EqualTo(1));
        Assert.That(paragraph.GetChild(2).GetChild(0).Literal, Is.EqualTo("beta"));
    }

    [Test]
    public void Parser_DefaultPipelineCreatesStrongEmphasisNodes()
    {
        MarkdownDocument document = MarkdownParser.Parse("**alpha** __beta__");
        MarkdownNode paragraph = document.Root.GetChild(0);

        Assert.That(paragraph.GetChildCount(), Is.EqualTo(3));
        Assert.That(paragraph.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.StrongEmphasis));
        Assert.That(paragraph.GetChild(0).GetChildCount(), Is.EqualTo(1));
        Assert.That(paragraph.GetChild(0).GetChild(0).Literal, Is.EqualTo("alpha"));
        Assert.That(paragraph.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.Text));
        Assert.That(paragraph.GetChild(1).Literal, Is.EqualTo(" "));
        Assert.That(paragraph.GetChild(2).Kind, Is.EqualTo(MarkdownNodeKind.StrongEmphasis));
        Assert.That(paragraph.GetChild(2).GetChildCount(), Is.EqualTo(1));
        Assert.That(paragraph.GetChild(2).GetChild(0).Literal, Is.EqualTo("beta"));
    }

    [Test]
    public void EmphasisRule_ParsesNestedInlineContent()
    {
        MarkdownDocument document = MarkdownParser.Parse("*a **b** &amp; c*");
        MarkdownNode paragraph = document.Root.GetChild(0);
        MarkdownNode emphasis = paragraph.GetChild(0);

        Assert.That(paragraph.GetChildCount(), Is.EqualTo(1));
        Assert.That(emphasis.Kind, Is.EqualTo(MarkdownNodeKind.Emphasis));
        Assert.That(emphasis.GetChildCount(), Is.EqualTo(5));
        Assert.That(emphasis.GetChild(0).Literal, Is.EqualTo("a "));
        Assert.That(emphasis.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.StrongEmphasis));
        Assert.That(emphasis.GetChild(1).GetChildCount(), Is.EqualTo(1));
        Assert.That(emphasis.GetChild(1).GetChild(0).Literal, Is.EqualTo("b"));
        Assert.That(emphasis.GetChild(2).Literal, Is.EqualTo(" "));
        Assert.That(emphasis.GetChild(3).Literal, Is.EqualTo("&"));
        Assert.That(emphasis.GetChild(4).Literal, Is.EqualTo(" c"));
    }

    [Test]
    public void EmphasisRule_DoesNotParseIntrawordUnderscores()
    {
        MarkdownDocument document = MarkdownParser.Parse("foo_bar_baz");
        MarkdownNode paragraph = document.Root.GetChild(0);

        Assert.That(ContainsInlineKind(paragraph, MarkdownNodeKind.Emphasis), Is.False);
        Assert.That(ContainsInlineKind(paragraph, MarkdownNodeKind.StrongEmphasis), Is.False);
        Assert.That(ConcatenateInlineLiterals(paragraph), Is.EqualTo("foo_bar_baz"));
    }

    [Test]
    public void EmphasisRule_LeavesUnmatchedOrWhitespaceDelimitersAsText()
    {
        string source = "a * alpha* ** beta**";
        MarkdownDocument document = MarkdownParser.Parse(source);
        MarkdownNode paragraph = document.Root.GetChild(0);

        Assert.That(ContainsInlineKind(paragraph, MarkdownNodeKind.Emphasis), Is.False);
        Assert.That(ContainsInlineKind(paragraph, MarkdownNodeKind.StrongEmphasis), Is.False);
        Assert.That(ConcatenateInlineLiterals(paragraph), Is.EqualTo(source));
    }

    [Test]
    public void EmphasisRule_DoesNotParseInsideCodeSpans()
    {
        MarkdownDocument document = MarkdownParser.Parse("`*not emphasis*`");
        MarkdownNode paragraph = document.Root.GetChild(0);

        Assert.That(paragraph.GetChildCount(), Is.EqualTo(1));
        Assert.That(paragraph.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.CodeSpan));
        Assert.That(paragraph.GetChild(0).Literal, Is.EqualTo("*not emphasis*"));
    }

    [Test]
    public void CommonMarkPresetsRegisterTextRule()
    {
        MarkdownRuleList inlineRules = MarkdownPipeline.CommonMarkPreset().GetInlineRules();

        Assert.That(MarkdownPipeline.CommonMarkPreset().GetInlineRules().Contains(CommonMarkInlineRules.TextRuleName), Is.True);
        Assert.That(MarkdownPipeline.CommonMarkPreset().GetInlineRules().Contains(CommonMarkInlineRules.CodeSpanRuleName), Is.True);
        Assert.That(MarkdownPipeline.CommonMarkPreset().GetInlineRules().Contains(CommonMarkInlineRules.HardLineBreakRuleName), Is.True);
        Assert.That(MarkdownPipeline.CommonMarkPreset().GetInlineRules().Contains(CommonMarkInlineRules.BackslashEscapeRuleName), Is.True);
        Assert.That(MarkdownPipeline.CommonMarkPreset().GetInlineRules().Contains(CommonMarkInlineRules.EntityReferenceRuleName), Is.True);
        Assert.That(MarkdownPipeline.CommonMarkPreset().GetInlineRules().Contains(CommonMarkInlineRules.AutolinkRuleName), Is.True);
        Assert.That(MarkdownPipeline.CommonMarkPreset().GetInlineRules().Contains(CommonMarkInlineRules.HtmlInlineRuleName), Is.True);
        Assert.That(MarkdownPipeline.CommonMarkPreset().GetInlineRules().Contains(CommonMarkInlineRules.ImageRuleName), Is.True);
        Assert.That(MarkdownPipeline.CommonMarkPreset().GetInlineRules().Contains(CommonMarkInlineRules.LinkRuleName), Is.True);
        Assert.That(MarkdownPipeline.CommonMarkPreset().GetInlineRules().Contains(CommonMarkInlineRules.EmphasisRuleName), Is.True);
        Assert.That(MarkdownPipeline.CommonMarkPreset().GetInlineRules().Contains(CommonMarkInlineRules.SoftLineBreakRuleName), Is.True);
        Assert.That(MarkdownPipeline.GfmPreset().GetInlineRules().Contains(CommonMarkInlineRules.TextRuleName), Is.True);
        Assert.That(MarkdownPipeline.AdvancedPreset().GetInlineRules().Contains(CommonMarkInlineRules.TextRuleName), Is.True);
        Assert.That(MarkdownPipeline.ZeroPreset().GetInlineRules().Contains(CommonMarkInlineRules.TextRuleName), Is.False);
        Assert.That(MarkdownPipeline.ZeroPreset().GetInlineRules().Contains(CommonMarkInlineRules.CodeSpanRuleName), Is.False);
        Assert.That(MarkdownPipeline.ZeroPreset().GetInlineRules().Contains(CommonMarkInlineRules.HardLineBreakRuleName), Is.False);
        Assert.That(MarkdownPipeline.ZeroPreset().GetInlineRules().Contains(CommonMarkInlineRules.BackslashEscapeRuleName), Is.False);
        Assert.That(MarkdownPipeline.ZeroPreset().GetInlineRules().Contains(CommonMarkInlineRules.EntityReferenceRuleName), Is.False);
        Assert.That(MarkdownPipeline.ZeroPreset().GetInlineRules().Contains(CommonMarkInlineRules.AutolinkRuleName), Is.False);
        Assert.That(MarkdownPipeline.ZeroPreset().GetInlineRules().Contains(CommonMarkInlineRules.HtmlInlineRuleName), Is.False);
        Assert.That(MarkdownPipeline.ZeroPreset().GetInlineRules().Contains(CommonMarkInlineRules.ImageRuleName), Is.False);
        Assert.That(MarkdownPipeline.ZeroPreset().GetInlineRules().Contains(CommonMarkInlineRules.LinkRuleName), Is.False);
        Assert.That(MarkdownPipeline.ZeroPreset().GetInlineRules().Contains(CommonMarkInlineRules.EmphasisRuleName), Is.False);
        Assert.That(MarkdownPipeline.ZeroPreset().GetInlineRules().Contains(CommonMarkInlineRules.SoftLineBreakRuleName), Is.False);
        Assert.That(MarkdownPipelineBuilder.CommonMarkPreset().GetInlineRules().Contains(CommonMarkInlineRules.TextRuleName), Is.True);
        Assert.That(MarkdownPipelineBuilder.GfmPreset().GetInlineRules().Contains(CommonMarkInlineRules.TextRuleName), Is.True);
        Assert.That(MarkdownPipelineBuilder.AdvancedPreset().GetInlineRules().Contains(CommonMarkInlineRules.TextRuleName), Is.True);
        Assert.That(MarkdownPipelineBuilder.ZeroPreset().GetInlineRules().Contains(CommonMarkInlineRules.TextRuleName), Is.False);
        Assert.That(inlineRules.GetAt(0).Name, Is.EqualTo(CommonMarkInlineRules.CodeSpanRuleName));
        Assert.That(inlineRules.GetAt(1).Name, Is.EqualTo(CommonMarkInlineRules.HardLineBreakRuleName));
        Assert.That(inlineRules.GetAt(2).Name, Is.EqualTo(CommonMarkInlineRules.BackslashEscapeRuleName));
        Assert.That(inlineRules.GetAt(3).Name, Is.EqualTo(CommonMarkInlineRules.EntityReferenceRuleName));
        Assert.That(inlineRules.GetAt(4).Name, Is.EqualTo(CommonMarkInlineRules.AutolinkRuleName));
        Assert.That(inlineRules.GetAt(5).Name, Is.EqualTo(CommonMarkInlineRules.HtmlInlineRuleName));
        Assert.That(inlineRules.GetAt(6).Name, Is.EqualTo(CommonMarkInlineRules.ImageRuleName));
        Assert.That(inlineRules.GetAt(7).Name, Is.EqualTo(CommonMarkInlineRules.LinkRuleName));
        Assert.That(inlineRules.GetAt(8).Name, Is.EqualTo(CommonMarkInlineRules.EmphasisRuleName));
        Assert.That(inlineRules.GetAt(9).Name, Is.EqualTo(CommonMarkInlineRules.SoftLineBreakRuleName));
        Assert.That(inlineRules.GetAt(10).Name, Is.EqualTo(CommonMarkInlineRules.TextRuleName));
    }

    [Test]
    public void Builder_UseCommonMarkRegistersTextRuleIdempotently()
    {
        MarkdownPipelineBuilder builder = new MarkdownPipelineBuilder();

        builder.UseCommonMark();
        builder.UseCommonMark();

        Assert.That(builder.GetInlineRules().GetCount(), Is.EqualTo(11));
        Assert.That(builder.GetInlineRules().Contains(CommonMarkInlineRules.CodeSpanRuleName), Is.True);
        Assert.That(builder.GetInlineRules().Contains(CommonMarkInlineRules.HardLineBreakRuleName), Is.True);
        Assert.That(builder.GetInlineRules().Contains(CommonMarkInlineRules.BackslashEscapeRuleName), Is.True);
        Assert.That(builder.GetInlineRules().Contains(CommonMarkInlineRules.EntityReferenceRuleName), Is.True);
        Assert.That(builder.GetInlineRules().Contains(CommonMarkInlineRules.AutolinkRuleName), Is.True);
        Assert.That(builder.GetInlineRules().Contains(CommonMarkInlineRules.HtmlInlineRuleName), Is.True);
        Assert.That(builder.GetInlineRules().Contains(CommonMarkInlineRules.ImageRuleName), Is.True);
        Assert.That(builder.GetInlineRules().Contains(CommonMarkInlineRules.LinkRuleName), Is.True);
        Assert.That(builder.GetInlineRules().Contains(CommonMarkInlineRules.EmphasisRuleName), Is.True);
        Assert.That(builder.GetInlineRules().Contains(CommonMarkInlineRules.SoftLineBreakRuleName), Is.True);
        Assert.That(builder.GetInlineRules().Contains(CommonMarkInlineRules.TextRuleName), Is.True);

        builder.UseZero();

        Assert.That(builder.GetInlineRules().Contains(CommonMarkInlineRules.CodeSpanRuleName), Is.False);
        Assert.That(builder.GetInlineRules().Contains(CommonMarkInlineRules.HardLineBreakRuleName), Is.False);
        Assert.That(builder.GetInlineRules().Contains(CommonMarkInlineRules.BackslashEscapeRuleName), Is.False);
        Assert.That(builder.GetInlineRules().Contains(CommonMarkInlineRules.EntityReferenceRuleName), Is.False);
        Assert.That(builder.GetInlineRules().Contains(CommonMarkInlineRules.AutolinkRuleName), Is.False);
        Assert.That(builder.GetInlineRules().Contains(CommonMarkInlineRules.HtmlInlineRuleName), Is.False);
        Assert.That(builder.GetInlineRules().Contains(CommonMarkInlineRules.ImageRuleName), Is.False);
        Assert.That(builder.GetInlineRules().Contains(CommonMarkInlineRules.LinkRuleName), Is.False);
        Assert.That(builder.GetInlineRules().Contains(CommonMarkInlineRules.EmphasisRuleName), Is.False);
        Assert.That(builder.GetInlineRules().Contains(CommonMarkInlineRules.SoftLineBreakRuleName), Is.False);
        Assert.That(builder.GetInlineRules().Contains(CommonMarkInlineRules.TextRuleName), Is.False);
    }

    [Test]
    public void CommonMarkInlineRules_RejectInvalidArguments()
    {
        Assert.That(() => CommonMarkInlineRules.RegisterCommonMarkInlineRules(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.TryParseCodeSpan(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.TryParseHardLineBreak(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.TryParseBackslashEscape(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.TryParseEntityReference(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.TryParseAutolink(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.TryParseHtmlInline(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.TryParseImage(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.TryParseReferenceImage(null!, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.TryParseLink(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.TryParseReferenceLink(null!, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.TryParseEmphasis(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.TryParseSoftLineBreak(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.TryParseText(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.IsLinkParsingProhibited(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.FindTextRunEnd(null!, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.FindTextRunEnd("text", -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.FindTextRunEnd("text", 5), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.HasHardLineBreakSpaces(null!, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.HasHardLineBreakSpaces("text", -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.HasHardLineBreakSpaces("text", 5), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.TrimTrailingSpacesFromLastText(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.CountBacktickRun(null!, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.CountBacktickRun("text", -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.CountBacktickRun("text", 5), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.FindClosingBacktickRun(null!, 0, 1), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.FindClosingBacktickRun("text", -1, 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.FindClosingBacktickRun("text", 5, 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.FindClosingBacktickRun("text", 0, 0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.NormalizeCodeSpanLiteral(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.ReplaceLineEndingsWithSpaces(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.IsAllSpaces(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownInlineLinkParseResult.Create(true, 1, null!, ""), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => MarkdownInlineLinkParseResult.Create(true, 1, "", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.FindHtmlInlineEnd(null!, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.FindHtmlInlineEnd("<>", -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.FindHtmlInlineEnd("<>", 2), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.FindHtmlInlineTerminator(null!, 0, ">"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.FindHtmlInlineTerminator("<>", 0, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.FindHtmlInlineTerminator("<>", -1, ">"), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.FindHtmlInlineTerminator("<>", 0, ""), Throws.TypeOf<ArgumentException>());
        Assert.That(() => CommonMarkInlineRules.FindHtmlInlineTagEnd(null!, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.FindHtmlInlineTagEnd("<a>", -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.FindHtmlInlineTagEnd("<a>", 3), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.FindHtmlInlineQuotedAttributeEnd(null!, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.FindHtmlInlineQuotedAttributeEnd("\"\"", -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.FindHtmlInlineQuotedAttributeEnd("\"\"", 2), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.FindLinkLabelEnd(null!, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.FindLinkLabelEnd("[]", -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.FindLinkLabelEnd("[]", 2), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.ParseInlineLinkDestinationAndTitle(null!, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.ParseInlineLinkDestinationAndTitle("()", -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.ParseInlineLinkDestinationAndTitle("()", 2), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.DecodeLinkAttributeText(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.FindAutolinkEnd(null!, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.FindAutolinkEnd("<>", -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.FindAutolinkEnd("<>", 2), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.IsUriAutolink(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.GetAutolinkSchemeEnd(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.IsEmailAutolink(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.GetSingleAtSignIndex(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.IsEmailDomain(null!, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.IsEmailDomain("", -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.IsEmailDomain("", 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.IsEmailDomainLabel(null!, 0, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.IsEmailDomainLabel("", -1, 0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.IsEmailDomainLabel("", 0, 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.IsEmailTopLevelDomain(null!, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.IsEmailTopLevelDomain("", -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.IsEmailTopLevelDomain("", 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.FindLinkReferenceDefinition(null!, "label"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.FindLinkReferenceDefinition(new MarkdownParseContext(), null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.SkipInlineLinkWhitespace(null!, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.SkipInlineLinkWhitespace("", -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.SkipInlineLinkWhitespace("", 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.FindInlineLinkAngleDestinationEnd(null!, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.FindInlineLinkAngleDestinationEnd("<>", -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.FindInlineLinkAngleDestinationEnd("<>", 2), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.FindInlineLinkBareDestinationEnd(null!, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.FindInlineLinkBareDestinationEnd("", -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.FindInlineLinkBareDestinationEnd("", 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.FindInlineLinkTitleEnd(null!, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.FindInlineLinkTitleEnd("\"\"", -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.FindInlineLinkTitleEnd("\"\"", 2), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.CountDelimiterRun(null!, 0, '*'), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.CountDelimiterRun("text", -1, '*'), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.CountDelimiterRun("text", 5, '*'), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.CountDelimiterRun("text", 0, 'x'), Throws.TypeOf<ArgumentException>());
        Assert.That(() => CommonMarkInlineRules.FindClosingDelimiterRun(null!, 0, '*', 1), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.FindClosingDelimiterRun("text", -1, '*', 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.FindClosingDelimiterRun("text", 5, '*', 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.FindClosingDelimiterRun("text", 0, 'x', 1), Throws.TypeOf<ArgumentException>());
        Assert.That(() => CommonMarkInlineRules.FindClosingDelimiterRun("text", 0, '*', 0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.CanOpenDelimiter(null!, 0, '*', 1), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.CanOpenDelimiter("text", -1, '*', 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.CanOpenDelimiter("text", 5, '*', 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.CanOpenDelimiter("text", 0, 'x', 1), Throws.TypeOf<ArgumentException>());
        Assert.That(() => CommonMarkInlineRules.CanOpenDelimiter("text", 0, '*', 0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.CanCloseDelimiter(null!, 0, '*', 1), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.CanCloseDelimiter("text", -1, '*', 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.CanCloseDelimiter("text", 5, '*', 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.CanCloseDelimiter("text", 0, 'x', 1), Throws.TypeOf<ArgumentException>());
        Assert.That(() => CommonMarkInlineRules.CanCloseDelimiter("text", 0, '*', 0), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.AppendDelimitedInline(null!, MarkdownNodeKind.Emphasis, 0, 0, 1), Throws.TypeOf<ArgumentNullException>());

        MarkdownInlineParseContext context = CreateInlineContext("*x*");
        Assert.That(() => CommonMarkInlineRules.AppendHtmlInline(null!, "", 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.AppendHtmlInline(context, null!, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.AppendHtmlInline(context, "", -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.AppendHtmlInline(context, "", 4), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.AppendAutolink(null!, "", "", 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.AppendAutolink(context, null!, "", 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.AppendAutolink(context, "", null!, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.AppendAutolink(context, "", "", -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.AppendAutolink(context, "", "", 3), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.TryParseReferenceImage(context, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.TryParseReferenceImage(context, 3), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.AppendImageInline(null!, 0, 0, "", "", 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.AppendImageInline(context, 0, 0, null!, "", 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.AppendImageInline(context, 0, 0, "", null!, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.AppendImageInline(context, -1, 1, "", "", 2), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.AppendImageInline(context, 1, 0, "", "", 2), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.AppendImageInline(context, 1, 2, "", "", 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.AppendImageInline(context, 1, 2, "", "", 3), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.TryParseReferenceLink(context, -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.TryParseReferenceLink(context, 3), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.AppendLinkInline(null!, 0, 0, "", "", 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.AppendLinkInline(context, 0, 0, null!, "", 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.AppendLinkInline(context, 0, 0, "", null!, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkInlineRules.AppendLinkInline(context, -1, 1, "", "", 2), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.AppendLinkInline(context, 1, 0, "", "", 2), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.AppendLinkInline(context, 1, 2, "", "", 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.AppendLinkInline(context, 1, 2, "", "", 3), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.AppendDelimitedInline(context, MarkdownNodeKind.Paragraph, 1, 2, 1), Throws.TypeOf<ArgumentException>());
        Assert.That(() => CommonMarkInlineRules.AppendDelimitedInline(context, MarkdownNodeKind.Emphasis, -1, 2, 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.AppendDelimitedInline(context, MarkdownNodeKind.Emphasis, 1, 4, 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkInlineRules.AppendDelimitedInline(context, MarkdownNodeKind.Emphasis, 1, 2, 0), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    static MarkdownInlineParseContext CreateInlineContext(string source)
    {
        MarkdownParseContext parseContext = MarkdownParser.CreateContext("", MarkdownPipeline.CommonMarkPreset());
        MarkdownNode paragraph = MarkdownNode.Create(MarkdownNodeKind.Paragraph);
        paragraph.SetLiteral(source);
        parseContext.Document.Root.AppendChild(paragraph);
        return MarkdownInlineParseContext.Create(parseContext, paragraph);
    }

    static bool ContainsInlineKind(MarkdownNode node, MarkdownNodeKind kind)
    {
        int index = 0;
        while (index < node.GetChildCount())
        {
            MarkdownNode child = node.GetChild(index);
            if (child.Kind == kind || ContainsInlineKind(child, kind))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    static string ConcatenateInlineLiterals(MarkdownNode node)
    {
        if (node.GetChildCount() == 0)
        {
            return node.Literal;
        }

        string result = "";
        int index = 0;
        while (index < node.GetChildCount())
        {
            result = result + ConcatenateInlineLiterals(node.GetChild(index));
            index = index + 1;
        }

        return result;
    }
}
