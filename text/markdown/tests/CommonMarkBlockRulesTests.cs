using NUnit.Framework;

namespace markdown.Tests;

[TestFixture]
public class CommonMarkBlockRulesTests
{
    [Test]
    public void BlankLineRule_MatchesEmptyAndWhitespaceOnlyLines()
    {
        MarkdownBlockParseContext context = MarkdownBlockParseContext.Create("\n \t", MarkdownPipeline.CommonMarkPreset());
        MarkdownBlockRule rule = CommonMarkBlockRules.CreateBlankLineRule();

        Assert.That(rule.TryParse(context), Is.True);
        Assert.That(context.BlankLineCount, Is.EqualTo(1));
        Assert.That(context.LastLineWasBlank, Is.True);
        Assert.That(context.LastBlankLine!.Text, Is.EqualTo(""));

        Assert.That(rule.TryParse(context), Is.True);
        Assert.That(context.BlankLineCount, Is.EqualTo(2));
        Assert.That(context.LastBlankLine!.Text, Is.EqualTo(" \t"));
    }

    [Test]
    public void BlankLineRule_DoesNotMatchContentLine()
    {
        MarkdownBlockParseContext context = MarkdownBlockParseContext.Create(" text", MarkdownPipeline.CommonMarkPreset());

        Assert.That(CommonMarkBlockRules.TryParseBlankLine(context), Is.False);
        Assert.That(context.LineIndex, Is.EqualTo(0));
        Assert.That(context.BlankLineCount, Is.EqualTo(0));
        Assert.That(context.LastLineWasBlank, Is.False);
    }

    [Test]
    public void BlockParser_TracksBlankLinesAndResetsOnContent()
    {
        MarkdownBlockParseContext context = MarkdownBlockParseContext.Create("\ntext\n  ", MarkdownPipeline.CommonMarkPreset());

        BlockParser.Run(context);

        Assert.That(context.LineIndex, Is.EqualTo(3));
        Assert.That(context.BlankLineCount, Is.EqualTo(2));
        Assert.That(context.LastLineWasBlank, Is.True);
        Assert.That(context.LastBlankLine!.LineNumber, Is.EqualTo(3));
    }

    [Test]
    public void ParagraphRule_ParsesConsecutiveNonBlankLines()
    {
        MarkdownBlockParseContext context = MarkdownBlockParseContext.Create("alpha\nbeta\n\nnext", MarkdownPipeline.CommonMarkPreset());

        Assert.That(CommonMarkBlockRules.TryParseParagraph(context), Is.True);

        MarkdownNode paragraph = context.ParseContext.Document.Root.GetChild(0);

        Assert.That(paragraph.Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(paragraph.Literal, Is.EqualTo("alpha\nbeta"));
        Assert.That(paragraph.Span.Start.Offset, Is.EqualTo(0));
        Assert.That(paragraph.Span.End.Offset, Is.EqualTo(10));
        Assert.That(paragraph.Span.End.Line, Is.EqualTo(2));
        Assert.That(paragraph.Span.End.Column, Is.EqualTo(5));
        Assert.That(context.LineIndex, Is.EqualTo(2));
    }

    [Test]
    public void ParagraphRule_DoesNotMatchBlankLine()
    {
        MarkdownBlockParseContext context = MarkdownBlockParseContext.Create("\ntext", MarkdownPipeline.CommonMarkPreset());

        Assert.That(CommonMarkBlockRules.TryParseParagraph(context), Is.False);
        Assert.That(context.ParseContext.Document.Root.GetChildCount(), Is.EqualTo(0));
        Assert.That(context.LineIndex, Is.EqualTo(0));
    }

    [Test]
    public void ParagraphRule_StopsBeforeInterruptingBlockStarts()
    {
        MarkdownDocument heading = MarkdownParser.Parse("text\n# Heading");
        MarkdownDocument thematicBreak = MarkdownParser.Parse("text\n***");
        MarkdownDocument blockQuote = MarkdownParser.Parse("text\n> quote");
        MarkdownDocument fencedCode = MarkdownParser.Parse("text\n```");
        MarkdownDocument html = MarkdownParser.Parse("text\n<div>");
        MarkdownDocument bulletList = MarkdownParser.Parse("text\n- item");
        MarkdownDocument orderedList = MarkdownParser.Parse("text\n1. item");

        Assert.That(heading.Root.GetChildCount(), Is.EqualTo(2));
        Assert.That(heading.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(heading.Root.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.Heading));
        Assert.That(thematicBreak.Root.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.ThematicBreak));
        Assert.That(blockQuote.Root.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.BlockQuote));
        Assert.That(fencedCode.Root.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.FencedCodeBlock));
        Assert.That(html.Root.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.HtmlBlock));
        Assert.That(bulletList.Root.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.List));
        Assert.That(orderedList.Root.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.List));
    }

    [Test]
    public void ParagraphRule_DoesNotStopBeforeNonInterruptingBlockForms()
    {
        MarkdownDocument orderedList = MarkdownParser.Parse("text\n2. item");
        MarkdownDocument emptyOrderedList = MarkdownParser.Parse("text\n1.");
        MarkdownDocument linkReferenceDefinition = MarkdownParser.Parse("text\n[bar]: /baz\n\n[bar]");
        MarkdownDocument htmlType7 = MarkdownParser.Parse("text\n<a>");

        Assert.That(orderedList.Root.GetChildCount(), Is.EqualTo(1));
        Assert.That(orderedList.Root.GetChild(0).Literal, Is.EqualTo("text\n2. item"));
        Assert.That(emptyOrderedList.Root.GetChildCount(), Is.EqualTo(1));
        Assert.That(emptyOrderedList.Root.GetChild(0).Literal, Is.EqualTo("text\n1."));
        Assert.That(linkReferenceDefinition.Root.GetChild(0).Literal, Is.EqualTo("text\n[bar]: /baz"));
        Assert.That(linkReferenceDefinition.Root.GetChild(1).Literal, Is.EqualTo("[bar]"));
        Assert.That(htmlType7.Root.GetChildCount(), Is.EqualTo(1));
        Assert.That(htmlType7.Root.GetChild(0).Literal, Is.EqualTo("text\n<a>"));
    }

    [Test]
    public void ThematicBreakRule_MatchesCommonMarkForms()
    {
        Assert.That(IsThematicBreak("---"), Is.True);
        Assert.That(IsThematicBreak(" - - - "), Is.True);
        Assert.That(IsThematicBreak("***"), Is.True);
        Assert.That(IsThematicBreak("___"), Is.True);
        Assert.That(IsThematicBreak("   * * *"), Is.True);
    }

    [Test]
    public void AtxHeadingRule_MatchesLevelsAndStripsClosingSequence()
    {
        MarkdownNode h1 = ParseSingleHeading("# Heading");
        MarkdownNode h3 = ParseSingleHeading("### Heading ###");
        MarkdownNode empty = ParseSingleHeading("### ###");
        MarkdownNode h6 = ParseSingleHeading("###### six");

        Assert.That(h1.GetAttributeValue("level"), Is.EqualTo("1"));
        Assert.That(h1.Literal, Is.EqualTo("Heading"));
        Assert.That(h3.GetAttributeValue("level"), Is.EqualTo("3"));
        Assert.That(h3.Literal, Is.EqualTo("Heading"));
        Assert.That(empty.GetAttributeValue("level"), Is.EqualTo("3"));
        Assert.That(empty.Literal, Is.EqualTo(""));
        Assert.That(h6.GetAttributeValue("level"), Is.EqualTo("6"));
        Assert.That(h6.Literal, Is.EqualTo("six"));
    }

    [Test]
    public void AtxHeadingRule_PreservesHashesThatAreNotClosingSequence()
    {
        MarkdownNode first = ParseSingleHeading("# foo#");
        MarkdownNode second = ParseSingleHeading("# foo ### b");

        Assert.That(first.Literal, Is.EqualTo("foo#"));
        Assert.That(second.Literal, Is.EqualTo("foo ### b"));
    }

    [Test]
    public void AtxHeadingRule_AllowsLeadingIndentUpToThreeColumns()
    {
        MarkdownNode heading = ParseSingleHeading("   ## indented");

        Assert.That(heading.Kind, Is.EqualTo(MarkdownNodeKind.Heading));
        Assert.That(heading.GetAttributeValue("level"), Is.EqualTo("2"));
        Assert.That(heading.Literal, Is.EqualTo("indented"));
    }

    [Test]
    public void AtxHeadingRule_RejectsInvalidForms()
    {
        Assert.That(IsAtxHeading("####### too many"), Is.False);
        Assert.That(IsAtxHeading("#hashtag"), Is.False);
        Assert.That(IsAtxHeading("    # code"), Is.False);
        Assert.That(IsAtxHeading("\t# code"), Is.False);
        Assert.That(IsAtxHeading("text"), Is.False);
    }

    [Test]
    public void Parser_DefaultPipelineCreatesAtxHeadingBeforeParagraph()
    {
        MarkdownDocument document = MarkdownParser.Parse("# Title\nbody");

        Assert.That(document.Root.GetChildCount(), Is.EqualTo(2));
        Assert.That(document.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Heading));
        Assert.That(document.Root.GetChild(0).GetAttributeValue("level"), Is.EqualTo("1"));
        Assert.That(document.Root.GetChild(0).Literal, Is.EqualTo("Title"));
        Assert.That(document.Root.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(document.Root.GetChild(1).Literal, Is.EqualTo("body"));
    }

    [Test]
    public void ThematicBreakRule_RejectsInvalidForms()
    {
        Assert.That(IsThematicBreak("--"), Is.False);
        Assert.That(IsThematicBreak("- -"), Is.False);
        Assert.That(IsThematicBreak("---x"), Is.False);
        Assert.That(IsThematicBreak("-*-"), Is.False);
        Assert.That(IsThematicBreak("    ---"), Is.False);
        Assert.That(IsThematicBreak("\t---"), Is.False);
        Assert.That(IsThematicBreak("text"), Is.False);
    }

    [Test]
    public void ThematicBreakRule_CreatesNodeAndAdvances()
    {
        MarkdownBlockParseContext context = MarkdownBlockParseContext.Create(" - - - \ntext", MarkdownPipeline.CommonMarkPreset());

        Assert.That(CommonMarkBlockRules.TryParseThematicBreak(context), Is.True);

        MarkdownNode thematicBreak = context.ParseContext.Document.Root.GetChild(0);

        Assert.That(thematicBreak.Kind, Is.EqualTo(MarkdownNodeKind.ThematicBreak));
        Assert.That(thematicBreak.Span.Start.Offset, Is.EqualTo(0));
        Assert.That(thematicBreak.Span.End.Offset, Is.EqualTo(7));
        Assert.That(context.LineIndex, Is.EqualTo(1));
    }

    [Test]
    public void Parser_DefaultPipelineCreatesThematicBreakBeforeParagraph()
    {
        MarkdownDocument document = MarkdownParser.Parse("---\ntext");

        Assert.That(document.Root.GetChildCount(), Is.EqualTo(2));
        Assert.That(document.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.ThematicBreak));
        Assert.That(document.Root.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(document.Root.GetChild(1).Literal, Is.EqualTo("text"));
    }

    [Test]
    public void BlockQuoteRule_ParsesParagraphChild()
    {
        MarkdownBlockParseContext context = MarkdownBlockParseContext.Create("> alpha\n> beta\nnext", MarkdownPipeline.CommonMarkPreset());

        Assert.That(CommonMarkBlockRules.TryParseBlockQuote(context), Is.True);

        MarkdownNode quote = context.ParseContext.Document.Root.GetChild(0);

        Assert.That(quote.Kind, Is.EqualTo(MarkdownNodeKind.BlockQuote));
        Assert.That(quote.GetChildCount(), Is.EqualTo(1));
        Assert.That(quote.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(quote.GetChild(0).Literal, Is.EqualTo("alpha\nbeta\nnext"));
        Assert.That(quote.GetChild(0).Parent, Is.SameAs(quote));
        Assert.That(context.LineIndex, Is.EqualTo(3));
    }

    [Test]
    public void BlockQuoteRule_ParsesNestedBlockQuote()
    {
        MarkdownDocument document = MarkdownParser.Parse("> > nested");
        MarkdownNode outer = document.Root.GetChild(0);
        MarkdownNode inner = outer.GetChild(0);

        Assert.That(outer.Kind, Is.EqualTo(MarkdownNodeKind.BlockQuote));
        Assert.That(inner.Kind, Is.EqualTo(MarkdownNodeKind.BlockQuote));
        Assert.That(inner.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(inner.GetChild(0).Literal, Is.EqualTo("nested"));
    }

    [Test]
    public void BlockQuoteRule_ParsesInnerBlocks()
    {
        MarkdownDocument document = MarkdownParser.Parse("> # Title\n> ```\n> code\n> ```");
        MarkdownNode quote = document.Root.GetChild(0);

        Assert.That(quote.Kind, Is.EqualTo(MarkdownNodeKind.BlockQuote));
        Assert.That(quote.GetChildCount(), Is.EqualTo(2));
        Assert.That(quote.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Heading));
        Assert.That(quote.GetChild(0).Literal, Is.EqualTo("Title"));
        Assert.That(quote.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.FencedCodeBlock));
        Assert.That(quote.GetChild(1).Literal, Is.EqualTo("code"));
    }

    [Test]
    public void BlockQuoteRule_StripsMarkerAndOptionalFollowingWhitespace()
    {
        Assert.That(IsBlockQuoteLine("> alpha"), Is.True);
        Assert.That(IsBlockQuoteLine("   > alpha"), Is.True);
        Assert.That(IsBlockQuoteLine("    > alpha"), Is.False);
        Assert.That(IsBlockQuoteLine("text"), Is.False);
        Assert.That(RemoveBlockQuote("> alpha"), Is.EqualTo("alpha"));
        Assert.That(RemoveBlockQuote(">alpha"), Is.EqualTo("alpha"));
        Assert.That(RemoveBlockQuote("   >\talpha"), Is.EqualTo("   alpha"));
    }

    [Test]
    public void Parser_DefaultPipelineCreatesBlockQuoteBeforeParagraph()
    {
        MarkdownDocument document = MarkdownParser.Parse("> quote\n\ntext");

        Assert.That(document.Root.GetChildCount(), Is.EqualTo(2));
        Assert.That(document.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.BlockQuote));
        Assert.That(document.Root.GetChild(0).GetChild(0).Literal, Is.EqualTo("quote"));
        Assert.That(document.Root.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(document.Root.GetChild(1).Literal, Is.EqualTo("text"));
    }

    [Test]
    public void BlockQuoteRule_ParsesLazyContinuationLines()
    {
        MarkdownDocument document = MarkdownParser.Parse("> quote\ncontinued\n\ntext");
        MarkdownNode quote = document.Root.GetChild(0);

        Assert.That(document.Root.GetChildCount(), Is.EqualTo(2));
        Assert.That(quote.Kind, Is.EqualTo(MarkdownNodeKind.BlockQuote));
        Assert.That(quote.GetChildCount(), Is.EqualTo(1));
        Assert.That(quote.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(quote.GetChild(0).Literal, Is.EqualTo("quote\ncontinued"));
        Assert.That(document.Root.GetChild(1).Literal, Is.EqualTo("text"));
    }

    [Test]
    public void BlockQuoteRule_DoesNotLazilyContinueAfterHeading()
    {
        MarkdownDocument document = MarkdownParser.Parse("> # Title\noutside");
        MarkdownNode quote = document.Root.GetChild(0);

        Assert.That(document.Root.GetChildCount(), Is.EqualTo(2));
        Assert.That(quote.Kind, Is.EqualTo(MarkdownNodeKind.BlockQuote));
        Assert.That(quote.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Heading));
        Assert.That(quote.GetChild(0).Literal, Is.EqualTo("Title"));
        Assert.That(document.Root.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(document.Root.GetChild(1).Literal, Is.EqualTo("outside"));
    }

    [Test]
    public void BlockQuoteRule_DoesNotLazilyContinueInterruptingBlocks()
    {
        MarkdownDocument thematicBreak = MarkdownParser.Parse("> foo\n---");
        MarkdownDocument list = MarkdownParser.Parse("> - foo\n- bar");

        Assert.That(thematicBreak.Root.GetChildCount(), Is.EqualTo(2));
        Assert.That(thematicBreak.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.BlockQuote));
        Assert.That(thematicBreak.Root.GetChild(0).GetChild(0).Literal, Is.EqualTo("foo"));
        Assert.That(thematicBreak.Root.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.ThematicBreak));
        Assert.That(list.Root.GetChildCount(), Is.EqualTo(2));
        Assert.That(list.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.BlockQuote));
        Assert.That(list.Root.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.List));
        Assert.That(list.Root.GetChild(1).GetChild(0).GetChild(0).Literal, Is.EqualTo("bar"));
    }

    [Test]
    public void Parser_FourSpaceBlockQuoteMarkerIsIndentedCodeBlock()
    {
        MarkdownDocument document = MarkdownParser.Parse("    > quote");

        Assert.That(document.Root.GetChildCount(), Is.EqualTo(1));
        Assert.That(document.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.IndentedCodeBlock));
        Assert.That(document.Root.GetChild(0).Literal, Is.EqualTo("> quote"));
    }

    [Test]
    public void IndentedCodeBlockRule_ParsesIndentedLinesAndTabs()
    {
        MarkdownBlockParseContext context = MarkdownBlockParseContext.Create("    alpha\n\tbeta\nnext", MarkdownPipeline.CommonMarkPreset());

        Assert.That(CommonMarkBlockRules.TryParseIndentedCodeBlock(context), Is.True);

        MarkdownNode codeBlock = context.ParseContext.Document.Root.GetChild(0);

        Assert.That(codeBlock.Kind, Is.EqualTo(MarkdownNodeKind.IndentedCodeBlock));
        Assert.That(codeBlock.Literal, Is.EqualTo("alpha\nbeta"));
        Assert.That(codeBlock.Span.Start.Offset, Is.EqualTo(0));
        Assert.That(codeBlock.Span.End.Offset, Is.EqualTo(15));
        Assert.That(codeBlock.Span.End.Line, Is.EqualTo(2));
        Assert.That(codeBlock.Span.End.Column, Is.EqualTo(6));
        Assert.That(context.LineIndex, Is.EqualTo(2));
    }

    [Test]
    public void IndentedCodeBlockRule_PreservesInternalBlankLinesAndTrimsTrailingBlankLines()
    {
        MarkdownBlockParseContext context = MarkdownBlockParseContext.Create("    alpha\n\n    beta\n   \nnext", MarkdownPipeline.CommonMarkPreset());

        Assert.That(CommonMarkBlockRules.TryParseIndentedCodeBlock(context), Is.True);

        MarkdownNode codeBlock = context.ParseContext.Document.Root.GetChild(0);

        Assert.That(codeBlock.Kind, Is.EqualTo(MarkdownNodeKind.IndentedCodeBlock));
        Assert.That(codeBlock.Literal, Is.EqualTo("alpha\n\nbeta"));
        Assert.That(context.LineIndex, Is.EqualTo(4));
    }

    [Test]
    public void IndentedCodeBlockRule_StripsOnlyOneIndentLevel()
    {
        MarkdownBlockParseContext context = MarkdownBlockParseContext.Create("        deep\n\t\tTabbed", MarkdownPipeline.CommonMarkPreset());

        Assert.That(CommonMarkBlockRules.TryParseIndentedCodeBlock(context), Is.True);

        MarkdownNode codeBlock = context.ParseContext.Document.Root.GetChild(0);

        Assert.That(codeBlock.Kind, Is.EqualTo(MarkdownNodeKind.IndentedCodeBlock));
        Assert.That(codeBlock.Literal, Is.EqualTo("    deep\n\tTabbed"));
    }

    [Test]
    public void IndentedCodeBlockRule_RejectsUnderIndentedAndBlankLines()
    {
        Assert.That(IsIndentedCodeLine("    code"), Is.True);
        Assert.That(IsIndentedCodeLine("\tcode"), Is.True);
        Assert.That(IsIndentedCodeLine("   code"), Is.False);
        Assert.That(IsIndentedCodeLine("    "), Is.False);
        Assert.That(RemoveCodePrefix("  \tcode"), Is.EqualTo("code"));
    }

    [Test]
    public void Parser_DefaultPipelineCreatesIndentedCodeBeforeParagraph()
    {
        MarkdownDocument document = MarkdownParser.Parse("    # code\ntext");

        Assert.That(document.Root.GetChildCount(), Is.EqualTo(2));
        Assert.That(document.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.IndentedCodeBlock));
        Assert.That(document.Root.GetChild(0).Literal, Is.EqualTo("# code"));
        Assert.That(document.Root.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(document.Root.GetChild(1).Literal, Is.EqualTo("text"));
    }

    [Test]
    public void Parser_IndentedCodeDoesNotInterruptParagraph()
    {
        MarkdownDocument document = MarkdownParser.Parse("paragraph\n    code");

        Assert.That(document.Root.GetChildCount(), Is.EqualTo(1));
        Assert.That(document.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(document.Root.GetChild(0).Literal, Is.EqualTo("paragraph\ncode"));
    }

    [Test]
    public void Parser_IndentedThematicBreakMarkerIsCodeBlock()
    {
        MarkdownDocument document = MarkdownParser.Parse("    ---");

        Assert.That(document.Root.GetChildCount(), Is.EqualTo(1));
        Assert.That(document.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.IndentedCodeBlock));
        Assert.That(document.Root.GetChild(0).Literal, Is.EqualTo("---"));
    }

    [Test]
    public void FencedCodeBlockRule_ParsesBacktickFence()
    {
        MarkdownBlockParseContext context = MarkdownBlockParseContext.Create("```\nalpha\n```\nnext", MarkdownPipeline.CommonMarkPreset());

        Assert.That(CommonMarkBlockRules.TryParseFencedCodeBlock(context), Is.True);

        MarkdownNode codeBlock = context.ParseContext.Document.Root.GetChild(0);

        Assert.That(codeBlock.Kind, Is.EqualTo(MarkdownNodeKind.FencedCodeBlock));
        Assert.That(codeBlock.Literal, Is.EqualTo("alpha"));
        Assert.That(codeBlock.Span.Start.Offset, Is.EqualTo(0));
        Assert.That(codeBlock.Span.End.Offset, Is.EqualTo(13));
        Assert.That(codeBlock.Span.End.Line, Is.EqualTo(3));
        Assert.That(codeBlock.Span.End.Column, Is.EqualTo(4));
        Assert.That(context.LineIndex, Is.EqualTo(3));
    }

    [Test]
    public void FencedCodeBlockRule_ParsesTildeFenceAndRemovesOpeningIndentFromContent()
    {
        MarkdownBlockParseContext context = MarkdownBlockParseContext.Create("  ~~~\n  alpha\n beta\n  ~~~\nnext", MarkdownPipeline.CommonMarkPreset());

        Assert.That(CommonMarkBlockRules.TryParseFencedCodeBlock(context), Is.True);

        MarkdownNode codeBlock = context.ParseContext.Document.Root.GetChild(0);

        Assert.That(codeBlock.Kind, Is.EqualTo(MarkdownNodeKind.FencedCodeBlock));
        Assert.That(codeBlock.Literal, Is.EqualTo("alpha\nbeta"));
        Assert.That(context.LineIndex, Is.EqualTo(4));
    }

    [Test]
    public void FencedCodeBlockRule_AllowsUnclosedFenceToEndAtDocumentEnd()
    {
        MarkdownBlockParseContext context = MarkdownBlockParseContext.Create("```\nalpha\nbeta", MarkdownPipeline.CommonMarkPreset());

        Assert.That(CommonMarkBlockRules.TryParseFencedCodeBlock(context), Is.True);

        MarkdownNode codeBlock = context.ParseContext.Document.Root.GetChild(0);

        Assert.That(codeBlock.Kind, Is.EqualTo(MarkdownNodeKind.FencedCodeBlock));
        Assert.That(codeBlock.Literal, Is.EqualTo("alpha\nbeta"));
        Assert.That(context.LineIndex, Is.EqualTo(3));
    }

    [Test]
    public void FencedCodeBlockRule_CapturesTrimmedInfoString()
    {
        MarkdownDocument document = MarkdownParser.Parse("```  csharp title=sample  \nalpha\n```");
        MarkdownNode codeBlock = document.Root.GetChild(0);

        Assert.That(document.Root.GetChildCount(), Is.EqualTo(1));
        Assert.That(codeBlock.Kind, Is.EqualTo(MarkdownNodeKind.FencedCodeBlock));
        Assert.That(codeBlock.Literal, Is.EqualTo("alpha"));
        Assert.That(codeBlock.HasAttribute("info"), Is.True);
        Assert.That(codeBlock.GetAttributeValue("info"), Is.EqualTo("csharp title=sample"));
    }

    [Test]
    public void FencedCodeBlockRule_DoesNotAddInfoAttributeWhenInfoStringIsEmpty()
    {
        MarkdownDocument document = MarkdownParser.Parse("```\nalpha\n```");
        MarkdownNode codeBlock = document.Root.GetChild(0);

        Assert.That(codeBlock.Kind, Is.EqualTo(MarkdownNodeKind.FencedCodeBlock));
        Assert.That(codeBlock.Literal, Is.EqualTo("alpha"));
        Assert.That(codeBlock.HasAttribute("info"), Is.False);
        Assert.That(codeBlock.GetAttributeValue("info"), Is.EqualTo(""));
    }

    [Test]
    public void FencedCodeBlockRule_AllowsBackticksInTildeFenceInfoString()
    {
        MarkdownDocument document = MarkdownParser.Parse("~~~ label `tick`\nalpha\n~~~");
        MarkdownNode codeBlock = document.Root.GetChild(0);

        Assert.That(codeBlock.Kind, Is.EqualTo(MarkdownNodeKind.FencedCodeBlock));
        Assert.That(codeBlock.GetAttributeValue("info"), Is.EqualTo("label `tick`"));
    }

    [Test]
    public void FencedCodeBlockRule_TreatsShorterFenceAsContent()
    {
        MarkdownDocument document = MarkdownParser.Parse("````\n```\n````");

        Assert.That(document.Root.GetChildCount(), Is.EqualTo(1));
        Assert.That(document.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.FencedCodeBlock));
        Assert.That(document.Root.GetChild(0).Literal, Is.EqualTo("```"));
    }

    [Test]
    public void FencedCodeBlockRule_MatchesCommonMarkFenceForms()
    {
        Assert.That(IsFencedCodeOpening("```"), Is.True);
        Assert.That(IsFencedCodeOpening("~~~"), Is.True);
        Assert.That(IsFencedCodeOpening("   ```` info"), Is.True);
        Assert.That(IsFencedCodeOpening("``"), Is.False);
        Assert.That(IsFencedCodeOpening("    ```"), Is.False);
        Assert.That(IsFencedCodeOpening("``` info `bad`"), Is.False);
        Assert.That(GetFenceMarker("~~~"), Is.EqualTo('~'));
        Assert.That(GetFenceLength("```` info"), Is.EqualTo(4));
        Assert.That(GetFenceIndent("   ```"), Is.EqualTo(3));
        Assert.That(GetFenceInfo("   ```` info"), Is.EqualTo("info"));
        Assert.That(GetFenceInfo("~~~  a ` b  "), Is.EqualTo("a ` b"));
        Assert.That(GetFenceInfo("``` bad `"), Is.EqualTo(""));
    }

    [Test]
    public void FencedCodeBlockRule_MatchesClosingFenceOnlyWithSameMarkerAndEnoughLength()
    {
        Assert.That(IsFencedCodeClosing("````  ", '`', 4), Is.True);
        Assert.That(IsFencedCodeClosing("```", '`', 4), Is.False);
        Assert.That(IsFencedCodeClosing("```` x", '`', 4), Is.False);
        Assert.That(IsFencedCodeClosing("~~~~", '`', 4), Is.False);
        Assert.That(IsFencedCodeClosing("    ````", '`', 4), Is.False);
    }

    [Test]
    public void Parser_DefaultPipelineCreatesFencedCodeBeforeParagraph()
    {
        MarkdownDocument document = MarkdownParser.Parse("```\nalpha\n```\nnext");

        Assert.That(document.Root.GetChildCount(), Is.EqualTo(2));
        Assert.That(document.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.FencedCodeBlock));
        Assert.That(document.Root.GetChild(0).Literal, Is.EqualTo("alpha"));
        Assert.That(document.Root.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(document.Root.GetChild(1).Literal, Is.EqualTo("next"));
    }

    [Test]
    public void Parser_FourSpaceFenceIsIndentedCodeBlock()
    {
        MarkdownDocument document = MarkdownParser.Parse("    ```");

        Assert.That(document.Root.GetChildCount(), Is.EqualTo(1));
        Assert.That(document.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.IndentedCodeBlock));
        Assert.That(document.Root.GetChild(0).Literal, Is.EqualTo("```"));
    }

    [Test]
    public void HtmlBlockRule_ParsesType1ScriptUntilClosingTag()
    {
        MarkdownDocument document = MarkdownParser.Parse("<script>\nalpha\n</SCRIPT>\nnext");

        Assert.That(document.Root.GetChildCount(), Is.EqualTo(2));
        Assert.That(document.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.HtmlBlock));
        Assert.That(document.Root.GetChild(0).Literal, Is.EqualTo("<script>\nalpha\n</SCRIPT>"));
        Assert.That(document.Root.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(document.Root.GetChild(1).Literal, Is.EqualTo("next"));
    }

    [Test]
    public void HtmlBlockRule_ParsesTypes2Through5UntilTerminators()
    {
        AssertFirstHtmlBlock("<!-- a\nb -->\nnext", "<!-- a\nb -->");
        AssertFirstHtmlBlock("<?proc\nvalue?>\nnext", "<?proc\nvalue?>");
        AssertFirstHtmlBlock("<!DOCTYPE html>\nnext", "<!DOCTYPE html>");
        AssertFirstHtmlBlock("<![CDATA[\n<g>\n]]>\nnext", "<![CDATA[\n<g>\n]]>");
    }

    [Test]
    public void HtmlBlockRule_ParsesType6BlockTagUntilBlankLine()
    {
        MarkdownDocument document = MarkdownParser.Parse("<div>\nalpha\n</div>\n\nnext");

        Assert.That(document.Root.GetChildCount(), Is.EqualTo(2));
        Assert.That(document.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.HtmlBlock));
        Assert.That(document.Root.GetChild(0).Literal, Is.EqualTo("<div>\nalpha\n</div>"));
        Assert.That(document.Root.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(document.Root.GetChild(1).Literal, Is.EqualTo("next"));
    }

    [Test]
    public void HtmlBlockRule_ParsesType7CompleteTagUntilBlankLine()
    {
        MarkdownDocument document = MarkdownParser.Parse("<custom-tag>\nalpha\n\nnext");

        Assert.That(document.Root.GetChildCount(), Is.EqualTo(2));
        Assert.That(document.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.HtmlBlock));
        Assert.That(document.Root.GetChild(0).Literal, Is.EqualTo("<custom-tag>\nalpha"));
        Assert.That(document.Root.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(document.Root.GetChild(1).Literal, Is.EqualTo("next"));
    }

    [Test]
    public void HtmlBlockRule_ClassifiesSevenCommonMarkStartForms()
    {
        Assert.That(GetHtmlType("<script>"), Is.EqualTo(1));
        Assert.That(GetHtmlType("<!-- comment"), Is.EqualTo(2));
        Assert.That(GetHtmlType("<?pi"), Is.EqualTo(3));
        Assert.That(GetHtmlType("<!DOCTYPE html>"), Is.EqualTo(4));
        Assert.That(GetHtmlType("<![CDATA["), Is.EqualTo(5));
        Assert.That(GetHtmlType("<DIV class=\"x\">body"), Is.EqualTo(6));
        Assert.That(GetHtmlType("<span>"), Is.EqualTo(7));
        Assert.That(GetHtmlType("    <div>"), Is.EqualTo(0));
        Assert.That(GetHtmlType("<span>text"), Is.EqualTo(0));
        Assert.That(CommonMarkBlockRules.GetHtmlBlockType1TagName(MakeLine("<scripted>")), Is.EqualTo(""));
        Assert.That(CommonMarkBlockRules.IsHtmlBlockTagName("section"), Is.True);
        Assert.That(CommonMarkBlockRules.IsHtmlBlockTagName("span"), Is.False);
    }

    [Test]
    public void HtmlBlockRule_DetectsTypeEndConditions()
    {
        Assert.That(IsHtmlEnd("x</script>y", 1, "script"), Is.True);
        Assert.That(IsHtmlEnd("x-->", 2, ""), Is.True);
        Assert.That(IsHtmlEnd("x?>", 3, ""), Is.True);
        Assert.That(IsHtmlEnd("<!DOCTYPE html>", 4, ""), Is.True);
        Assert.That(IsHtmlEnd("]]>", 5, ""), Is.True);
        Assert.That(IsHtmlEnd("   ", 6, ""), Is.True);
        Assert.That(IsHtmlEnd("text", 6, ""), Is.False);
    }

    [Test]
    public void Parser_DefaultPipelineCreatesHtmlBlockBeforeParagraph()
    {
        MarkdownDocument document = MarkdownParser.Parse("<div>\nhtml\n\ntext");

        Assert.That(document.Root.GetChildCount(), Is.EqualTo(2));
        Assert.That(document.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.HtmlBlock));
        Assert.That(document.Root.GetChild(0).Literal, Is.EqualTo("<div>\nhtml"));
        Assert.That(document.Root.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(document.Root.GetChild(1).Literal, Is.EqualTo("text"));
    }

    [Test]
    public void Parser_FourSpaceHtmlIsIndentedCodeBlock()
    {
        MarkdownDocument document = MarkdownParser.Parse("    <div>");

        Assert.That(document.Root.GetChildCount(), Is.EqualTo(1));
        Assert.That(document.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.IndentedCodeBlock));
        Assert.That(document.Root.GetChild(0).Literal, Is.EqualTo("<div>"));
    }

    [Test]
    public void LinkReferenceDefinitionRule_ParsesLabelDestinationAndNormalizedLabel()
    {
        MarkdownBlockParseContext context = MarkdownBlockParseContext.Create("[Foo]: https://example.com", MarkdownPipeline.CommonMarkPreset());

        Assert.That(CommonMarkBlockRules.TryParseLinkReferenceDefinition(context), Is.True);

        MarkdownNode definition = context.ParseContext.Document.Root.GetChild(0);

        Assert.That(definition.Kind, Is.EqualTo(MarkdownNodeKind.LinkReferenceDefinition));
        Assert.That(definition.Literal, Is.EqualTo("[Foo]: https://example.com"));
        Assert.That(definition.GetAttributeValue("label"), Is.EqualTo("Foo"));
        Assert.That(definition.GetAttributeValue("normalizedLabel"), Is.EqualTo("foo"));
        Assert.That(definition.GetAttributeValue("destination"), Is.EqualTo("https://example.com"));
        Assert.That(definition.HasAttribute("title"), Is.False);
        Assert.That(context.ParseContext.LinkReferences.GetCount(), Is.EqualTo(1));
        Assert.That(context.ParseContext.LinkReferences.Find("foo"), Is.SameAs(definition));
        Assert.That(context.LineIndex, Is.EqualTo(1));
    }

    [Test]
    public void LinkReferenceDefinitionRule_ParsesTitleDelimiterForms()
    {
        MarkdownNode doubleQuoted = ParseSingleLinkReference("[foo]: /url \"double title\"");
        MarkdownNode singleQuoted = ParseSingleLinkReference("[foo]: /url 'single title'");
        MarkdownNode parenthesized = ParseSingleLinkReference("[foo]: /url (paren title)");

        Assert.That(doubleQuoted.GetAttributeValue("title"), Is.EqualTo("double title"));
        Assert.That(singleQuoted.GetAttributeValue("title"), Is.EqualTo("single title"));
        Assert.That(parenthesized.GetAttributeValue("title"), Is.EqualTo("paren title"));
    }

    [Test]
    public void LinkReferenceDefinitionRule_ParsesAngleDestinationAndEmptyDestination()
    {
        MarkdownNode escaped = ParseSingleLinkReference("[foo]: <https://example.com/a b> \"title\"");
        MarkdownNode empty = ParseSingleLinkReference("[bar]: <> \"empty\"");

        Assert.That(escaped.GetAttributeValue("destination"), Is.EqualTo("https://example.com/a b"));
        Assert.That(escaped.GetAttributeValue("title"), Is.EqualTo("title"));
        Assert.That(empty.GetAttributeValue("destination"), Is.EqualTo(""));
        Assert.That(empty.GetAttributeValue("title"), Is.EqualTo("empty"));
    }

    [Test]
    public void LinkReferenceDefinitionRule_DecodesDestinationAndTitleEscapes()
    {
        MarkdownNode definition = ParseSingleLinkReference("[foo]: <a\\>b&amp;c> \"a \\\"quote\\\" &amp;\"");

        Assert.That(definition.GetAttributeValue("destination"), Is.EqualTo("a>b&c"));
        Assert.That(definition.GetAttributeValue("title"), Is.EqualTo("a \"quote\" &"));
    }

    [Test]
    public void LinkReferenceDefinitionRule_NormalizesLabelsByCollapsingWhitespaceAndCase()
    {
        MarkdownNode definition = ParseSingleLinkReference("[ A \t B ]: /url");

        Assert.That(definition.GetAttributeValue("label"), Is.EqualTo(" A \t B "));
        Assert.That(definition.GetAttributeValue("normalizedLabel"), Is.EqualTo("a b"));
        Assert.That(CommonMarkBlockRules.NormalizeLinkReferenceLabel("  A\t\tB  "), Is.EqualTo("a b"));
    }

    [Test]
    public void LinkReferenceDefinitionRule_RejectsInvalidForms()
    {
        Assert.That(IsLinkReferenceDefinition("[]: /url"), Is.False);
        Assert.That(IsLinkReferenceDefinition("[foo] /url"), Is.False);
        Assert.That(IsLinkReferenceDefinition("[foo]:"), Is.False);
        Assert.That(IsLinkReferenceDefinition("    [foo]: /url"), Is.False);
        Assert.That(IsLinkReferenceDefinition("[foo]: /url extra"), Is.False);
        Assert.That(IsLinkReferenceDefinition("[foo]: /url \"unterminated"), Is.False);
    }

    [Test]
    public void Parser_DefaultPipelineCreatesLinkReferenceBeforeParagraph()
    {
        MarkdownDocument document = MarkdownParser.Parse("[foo]: /url \"title\"\ntext");

        Assert.That(document.Root.GetChildCount(), Is.EqualTo(2));
        Assert.That(document.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.LinkReferenceDefinition));
        Assert.That(document.Root.GetChild(0).GetAttributeValue("destination"), Is.EqualTo("/url"));
        Assert.That(document.Root.GetChild(0).GetAttributeValue("title"), Is.EqualTo("title"));
        Assert.That(document.Root.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(document.Root.GetChild(1).Literal, Is.EqualTo("text"));
    }

    [Test]
    public void Parser_FourSpaceLinkReferenceIsIndentedCodeBlock()
    {
        MarkdownDocument document = MarkdownParser.Parse("    [foo]: /url");

        Assert.That(document.Root.GetChildCount(), Is.EqualTo(1));
        Assert.That(document.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.IndentedCodeBlock));
        Assert.That(document.Root.GetChild(0).Literal, Is.EqualTo("[foo]: /url"));
    }

    [Test]
    public void OrderedListRule_ParsesDotMarkerItemsWithStartNumber()
    {
        MarkdownBlockParseContext context = MarkdownBlockParseContext.Create("3. three\n4. four\nnext", MarkdownPipeline.CommonMarkPreset());

        Assert.That(CommonMarkBlockRules.TryParseOrderedList(context), Is.True);

        MarkdownNode list = context.ParseContext.Document.Root.GetChild(0);

        Assert.That(list.Kind, Is.EqualTo(MarkdownNodeKind.List));
        Assert.That(list.GetAttributeValue("type"), Is.EqualTo("ordered"));
        Assert.That(list.GetAttributeValue("start"), Is.EqualTo("3"));
        Assert.That(list.GetAttributeValue("marker"), Is.EqualTo("."));
        Assert.That(list.GetChildCount(), Is.EqualTo(2));
        Assert.That(list.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.ListItem));
        Assert.That(list.GetChild(0).GetAttributeValue("number"), Is.EqualTo("3"));
        Assert.That(list.GetChild(0).GetAttributeValue("marker"), Is.EqualTo("."));
        Assert.That(list.GetChild(0).GetChild(0).Literal, Is.EqualTo("three"));
        Assert.That(list.GetChild(1).GetAttributeValue("number"), Is.EqualTo("4"));
        Assert.That(list.GetChild(1).GetChild(0).Literal, Is.EqualTo("four\nnext"));
        Assert.That(context.LineIndex, Is.EqualTo(3));
    }

    [Test]
    public void OrderedListRule_ParsesParenMarkerItems()
    {
        MarkdownDocument document = MarkdownParser.Parse("1) one\n2) two");
        MarkdownNode list = document.Root.GetChild(0);

        Assert.That(list.Kind, Is.EqualTo(MarkdownNodeKind.List));
        Assert.That(list.GetAttributeValue("start"), Is.EqualTo("1"));
        Assert.That(list.GetAttributeValue("marker"), Is.EqualTo(")"));
        Assert.That(list.GetChildCount(), Is.EqualTo(2));
        Assert.That(list.GetChild(0).GetChild(0).Literal, Is.EqualTo("one"));
        Assert.That(list.GetChild(1).GetChild(0).Literal, Is.EqualTo("two"));
    }

    [Test]
    public void OrderedListRule_ParsesItemContentAsBlocks()
    {
        MarkdownDocument document = MarkdownParser.Parse("1. # Title\n2. ```\n3. not code");
        MarkdownNode list = document.Root.GetChild(0);

        Assert.That(list.GetChildCount(), Is.EqualTo(3));
        Assert.That(list.GetChild(0).GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Heading));
        Assert.That(list.GetChild(0).GetChild(0).Literal, Is.EqualTo("Title"));
        Assert.That(list.GetChild(1).GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.FencedCodeBlock));
        Assert.That(list.GetChild(1).GetChild(0).Literal, Is.EqualTo(""));
        Assert.That(list.GetChild(2).GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(list.GetChild(2).GetChild(0).Literal, Is.EqualTo("not code"));
    }

    [Test]
    public void OrderedListRule_AllowsEmptyItemContent()
    {
        MarkdownDocument document = MarkdownParser.Parse("1.\n2. two");
        MarkdownNode list = document.Root.GetChild(0);

        Assert.That(list.GetChildCount(), Is.EqualTo(2));
        Assert.That(list.GetChild(0).GetChildCount(), Is.EqualTo(0));
        Assert.That(list.GetChild(1).GetChild(0).Literal, Is.EqualTo("two"));
    }

    [Test]
    public void OrderedListRule_ParsesIndentedContinuationLines()
    {
        MarkdownDocument document = MarkdownParser.Parse("1. one\n   continuation\n2. two");
        MarkdownNode list = document.Root.GetChild(0);

        Assert.That(list.GetChildCount(), Is.EqualTo(2));
        Assert.That(list.GetChild(0).GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(list.GetChild(0).GetChild(0).Literal, Is.EqualTo("one\ncontinuation"));
        Assert.That(list.GetChild(1).GetChild(0).Literal, Is.EqualTo("two"));
    }

    [Test]
    public void OrderedListRule_ParsesLazyContinuationLines()
    {
        MarkdownDocument document = MarkdownParser.Parse("1. one\ncontinued\n2. two");
        MarkdownNode list = document.Root.GetChild(0);

        Assert.That(document.Root.GetChildCount(), Is.EqualTo(1));
        Assert.That(list.GetChildCount(), Is.EqualTo(2));
        Assert.That(list.GetChild(0).GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(list.GetChild(0).GetChild(0).Literal, Is.EqualTo("one\ncontinued"));
        Assert.That(list.GetChild(1).GetChild(0).Literal, Is.EqualTo("two"));
    }

    [Test]
    public void OrderedListRule_DoesNotLazilyContinueAfterHeading()
    {
        MarkdownDocument document = MarkdownParser.Parse("1. # Title\noutside");
        MarkdownNode list = document.Root.GetChild(0);

        Assert.That(document.Root.GetChildCount(), Is.EqualTo(2));
        Assert.That(list.GetChildCount(), Is.EqualTo(1));
        Assert.That(list.GetChild(0).GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Heading));
        Assert.That(list.GetChild(0).GetChild(0).Literal, Is.EqualTo("Title"));
        Assert.That(document.Root.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(document.Root.GetChild(1).Literal, Is.EqualTo("outside"));
    }

    [Test]
    public void OrderedListRule_MarksTightListWithoutBlankLines()
    {
        MarkdownDocument document = MarkdownParser.Parse("1. one\n2. two");
        MarkdownNode list = document.Root.GetChild(0);

        Assert.That(list.GetAttributeValue("tight"), Is.EqualTo("true"));
        Assert.That(list.GetAttributeValue("loose"), Is.EqualTo("false"));
        Assert.That(list.GetChildCount(), Is.EqualTo(2));
        Assert.That(list.GetChild(0).GetChild(0).Literal, Is.EqualTo("one"));
        Assert.That(list.GetChild(1).GetChild(0).Literal, Is.EqualTo("two"));
    }

    [Test]
    public void OrderedListRule_MarksLooseListWithBlankLineBetweenItems()
    {
        MarkdownDocument document = MarkdownParser.Parse("1. one\n\n2. two");
        MarkdownNode list = document.Root.GetChild(0);

        Assert.That(document.Root.GetChildCount(), Is.EqualTo(1));
        Assert.That(list.GetAttributeValue("tight"), Is.EqualTo("false"));
        Assert.That(list.GetAttributeValue("loose"), Is.EqualTo("true"));
        Assert.That(list.GetChildCount(), Is.EqualTo(2));
        Assert.That(list.GetChild(0).GetChild(0).Literal, Is.EqualTo("one"));
        Assert.That(list.GetChild(1).GetChild(0).Literal, Is.EqualTo("two"));
    }

    [Test]
    public void OrderedListRule_MarksLooseListWithBlankContinuationInsideItem()
    {
        MarkdownDocument document = MarkdownParser.Parse("1. one\n   \n   two\n2. next");
        MarkdownNode list = document.Root.GetChild(0);
        MarkdownNode firstItem = list.GetChild(0);

        Assert.That(list.GetAttributeValue("tight"), Is.EqualTo("false"));
        Assert.That(list.GetAttributeValue("loose"), Is.EqualTo("true"));
        Assert.That(list.GetChildCount(), Is.EqualTo(2));
        Assert.That(firstItem.GetChildCount(), Is.EqualTo(2));
        Assert.That(firstItem.GetChild(0).Literal, Is.EqualTo("one"));
        Assert.That(firstItem.GetChild(1).Literal, Is.EqualTo("two"));
        Assert.That(list.GetChild(1).GetChild(0).Literal, Is.EqualTo("next"));
    }

    [Test]
    public void OrderedListRule_ParsesNestedOrderedListInItem()
    {
        MarkdownDocument document = MarkdownParser.Parse("1. # Parent\n   1. child\n2. next");
        MarkdownNode list = document.Root.GetChild(0);
        MarkdownNode nested = list.GetChild(0).GetChild(1);

        Assert.That(list.GetChildCount(), Is.EqualTo(2));
        Assert.That(list.GetChild(0).GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Heading));
        Assert.That(list.GetChild(0).GetChild(0).Literal, Is.EqualTo("Parent"));
        Assert.That(nested.Kind, Is.EqualTo(MarkdownNodeKind.List));
        Assert.That(nested.GetAttributeValue("type"), Is.EqualTo("ordered"));
        Assert.That(nested.GetChild(0).GetChild(0).Literal, Is.EqualTo("child"));
        Assert.That(list.GetChild(1).GetChild(0).Literal, Is.EqualTo("next"));
    }

    [Test]
    public void OrderedListRule_ParsesNestedOrderedListAfterParagraphInterruption()
    {
        MarkdownDocument document = MarkdownParser.Parse("1. parent\n   1. child\n2. next");
        MarkdownNode list = document.Root.GetChild(0);
        MarkdownNode nested = list.GetChild(0).GetChild(1);

        Assert.That(list.GetChildCount(), Is.EqualTo(2));
        Assert.That(list.GetChild(0).GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(list.GetChild(0).GetChild(0).Literal, Is.EqualTo("parent"));
        Assert.That(nested.Kind, Is.EqualTo(MarkdownNodeKind.List));
        Assert.That(nested.GetAttributeValue("type"), Is.EqualTo("ordered"));
        Assert.That(nested.GetChild(0).GetChild(0).Literal, Is.EqualTo("child"));
        Assert.That(list.GetChild(1).GetChild(0).Literal, Is.EqualTo("next"));
    }

    [Test]
    public void OrderedListRule_RejectsInvalidMarkers()
    {
        Assert.That(IsOrderedListItem("1. item"), Is.True);
        Assert.That(IsOrderedListItem("0) item"), Is.True);
        Assert.That(IsOrderedListItem("123456789. item"), Is.True);
        Assert.That(IsOrderedListItem("1234567890. item"), Is.False);
        Assert.That(IsOrderedListItem("1.item"), Is.False);
        Assert.That(IsOrderedListItem("1- item"), Is.False);
        Assert.That(IsOrderedListItem("    1. code"), Is.False);
        Assert.That(GetOrderedNumber("42. item"), Is.EqualTo(42));
        Assert.That(GetOrderedMarker("42) item"), Is.EqualTo(")"));
        Assert.That(RemoveOrderedMarker("42. item"), Is.EqualTo("item"));
        Assert.That(GetOrderedContentIndent("42. item"), Is.EqualTo(4));
        Assert.That(GetOrderedMarkerColumn("  42. item"), Is.EqualTo(2));
    }

    [Test]
    public void Parser_DefaultPipelineCreatesOrderedListBeforeParagraph()
    {
        MarkdownDocument document = MarkdownParser.Parse("1. one\n\ntext");

        Assert.That(document.Root.GetChildCount(), Is.EqualTo(2));
        Assert.That(document.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.List));
        Assert.That(document.Root.GetChild(0).GetChild(0).GetChild(0).Literal, Is.EqualTo("one"));
        Assert.That(document.Root.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(document.Root.GetChild(1).Literal, Is.EqualTo("text"));
    }

    [Test]
    public void Parser_FourSpaceOrderedListMarkerIsIndentedCodeBlock()
    {
        MarkdownDocument document = MarkdownParser.Parse("    1. code");

        Assert.That(document.Root.GetChildCount(), Is.EqualTo(1));
        Assert.That(document.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.IndentedCodeBlock));
        Assert.That(document.Root.GetChild(0).Literal, Is.EqualTo("1. code"));
    }

    [Test]
    public void BulletListRule_ParsesDashMarkerItems()
    {
        MarkdownBlockParseContext context = MarkdownBlockParseContext.Create("- one\n- two\nnext", MarkdownPipeline.CommonMarkPreset());

        Assert.That(CommonMarkBlockRules.TryParseBulletList(context), Is.True);

        MarkdownNode list = context.ParseContext.Document.Root.GetChild(0);

        Assert.That(list.Kind, Is.EqualTo(MarkdownNodeKind.List));
        Assert.That(list.GetAttributeValue("type"), Is.EqualTo("bullet"));
        Assert.That(list.GetAttributeValue("marker"), Is.EqualTo("-"));
        Assert.That(list.GetChildCount(), Is.EqualTo(2));
        Assert.That(list.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.ListItem));
        Assert.That(list.GetChild(0).GetAttributeValue("marker"), Is.EqualTo("-"));
        Assert.That(list.GetChild(0).GetChild(0).Literal, Is.EqualTo("one"));
        Assert.That(list.GetChild(1).GetChild(0).Literal, Is.EqualTo("two\nnext"));
        Assert.That(context.LineIndex, Is.EqualTo(3));
    }

    [Test]
    public void BulletListRule_ParsesPlusAndStarMarkerItems()
    {
        MarkdownNode plus = MarkdownParser.Parse("+ one\n+ two").Root.GetChild(0);
        MarkdownNode star = MarkdownParser.Parse("* one\n* two").Root.GetChild(0);

        Assert.That(plus.Kind, Is.EqualTo(MarkdownNodeKind.List));
        Assert.That(plus.GetAttributeValue("marker"), Is.EqualTo("+"));
        Assert.That(plus.GetChild(1).GetChild(0).Literal, Is.EqualTo("two"));
        Assert.That(star.Kind, Is.EqualTo(MarkdownNodeKind.List));
        Assert.That(star.GetAttributeValue("marker"), Is.EqualTo("*"));
        Assert.That(star.GetChild(1).GetChild(0).Literal, Is.EqualTo("two"));
    }

    [Test]
    public void BulletListRule_ParsesItemContentAsBlocks()
    {
        MarkdownDocument document = MarkdownParser.Parse("- # Title\n- ```\n- not code");
        MarkdownNode list = document.Root.GetChild(0);

        Assert.That(list.GetChildCount(), Is.EqualTo(3));
        Assert.That(list.GetChild(0).GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Heading));
        Assert.That(list.GetChild(0).GetChild(0).Literal, Is.EqualTo("Title"));
        Assert.That(list.GetChild(1).GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.FencedCodeBlock));
        Assert.That(list.GetChild(1).GetChild(0).Literal, Is.EqualTo(""));
        Assert.That(list.GetChild(2).GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(list.GetChild(2).GetChild(0).Literal, Is.EqualTo("not code"));
    }

    [Test]
    public void BulletListRule_AllowsEmptyItemContent()
    {
        MarkdownDocument document = MarkdownParser.Parse("-\n- two");
        MarkdownNode list = document.Root.GetChild(0);

        Assert.That(list.GetChildCount(), Is.EqualTo(2));
        Assert.That(list.GetChild(0).GetChildCount(), Is.EqualTo(0));
        Assert.That(list.GetChild(1).GetChild(0).Literal, Is.EqualTo("two"));
    }

    [Test]
    public void BulletListRule_ParsesIndentedContinuationLines()
    {
        MarkdownDocument document = MarkdownParser.Parse("- one\n  continuation\n- two");
        MarkdownNode list = document.Root.GetChild(0);

        Assert.That(list.GetChildCount(), Is.EqualTo(2));
        Assert.That(list.GetChild(0).GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(list.GetChild(0).GetChild(0).Literal, Is.EqualTo("one\ncontinuation"));
        Assert.That(list.GetChild(1).GetChild(0).Literal, Is.EqualTo("two"));
    }

    [Test]
    public void BulletListRule_ParsesLazyContinuationLines()
    {
        MarkdownDocument document = MarkdownParser.Parse("- one\ncontinued\n- two");
        MarkdownNode list = document.Root.GetChild(0);

        Assert.That(document.Root.GetChildCount(), Is.EqualTo(1));
        Assert.That(list.GetChildCount(), Is.EqualTo(2));
        Assert.That(list.GetChild(0).GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(list.GetChild(0).GetChild(0).Literal, Is.EqualTo("one\ncontinued"));
        Assert.That(list.GetChild(1).GetChild(0).Literal, Is.EqualTo("two"));
    }

    [Test]
    public void BulletListRule_DoesNotLazilyContinueAfterHeading()
    {
        MarkdownDocument document = MarkdownParser.Parse("- # Title\noutside");
        MarkdownNode list = document.Root.GetChild(0);

        Assert.That(document.Root.GetChildCount(), Is.EqualTo(2));
        Assert.That(list.GetChildCount(), Is.EqualTo(1));
        Assert.That(list.GetChild(0).GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Heading));
        Assert.That(list.GetChild(0).GetChild(0).Literal, Is.EqualTo("Title"));
        Assert.That(document.Root.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(document.Root.GetChild(1).Literal, Is.EqualTo("outside"));
    }

    [Test]
    public void BulletListRule_MarksTightListWithoutBlankLines()
    {
        MarkdownDocument document = MarkdownParser.Parse("- one\n- two");
        MarkdownNode list = document.Root.GetChild(0);

        Assert.That(list.GetAttributeValue("tight"), Is.EqualTo("true"));
        Assert.That(list.GetAttributeValue("loose"), Is.EqualTo("false"));
        Assert.That(list.GetChildCount(), Is.EqualTo(2));
        Assert.That(list.GetChild(0).GetChild(0).Literal, Is.EqualTo("one"));
        Assert.That(list.GetChild(1).GetChild(0).Literal, Is.EqualTo("two"));
    }

    [Test]
    public void BulletListRule_MarksLooseListWithBlankLineBetweenItems()
    {
        MarkdownDocument document = MarkdownParser.Parse("- one\n\n- two");
        MarkdownNode list = document.Root.GetChild(0);

        Assert.That(document.Root.GetChildCount(), Is.EqualTo(1));
        Assert.That(list.GetAttributeValue("tight"), Is.EqualTo("false"));
        Assert.That(list.GetAttributeValue("loose"), Is.EqualTo("true"));
        Assert.That(list.GetChildCount(), Is.EqualTo(2));
        Assert.That(list.GetChild(0).GetChild(0).Literal, Is.EqualTo("one"));
        Assert.That(list.GetChild(1).GetChild(0).Literal, Is.EqualTo("two"));
    }

    [Test]
    public void BulletListRule_MarksLooseListWithBlankContinuationInsideItem()
    {
        MarkdownDocument document = MarkdownParser.Parse("- one\n  \n  two\n- next");
        MarkdownNode list = document.Root.GetChild(0);
        MarkdownNode firstItem = list.GetChild(0);

        Assert.That(list.GetAttributeValue("tight"), Is.EqualTo("false"));
        Assert.That(list.GetAttributeValue("loose"), Is.EqualTo("true"));
        Assert.That(list.GetChildCount(), Is.EqualTo(2));
        Assert.That(firstItem.GetChildCount(), Is.EqualTo(2));
        Assert.That(firstItem.GetChild(0).Literal, Is.EqualTo("one"));
        Assert.That(firstItem.GetChild(1).Literal, Is.EqualTo("two"));
        Assert.That(list.GetChild(1).GetChild(0).Literal, Is.EqualTo("next"));
    }

    [Test]
    public void BulletListRule_ParsesNestedBulletListInItem()
    {
        MarkdownDocument document = MarkdownParser.Parse("- # Parent\n  - child\n- next");
        MarkdownNode list = document.Root.GetChild(0);
        MarkdownNode nested = list.GetChild(0).GetChild(1);

        Assert.That(list.GetChildCount(), Is.EqualTo(2));
        Assert.That(list.GetChild(0).GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Heading));
        Assert.That(list.GetChild(0).GetChild(0).Literal, Is.EqualTo("Parent"));
        Assert.That(nested.Kind, Is.EqualTo(MarkdownNodeKind.List));
        Assert.That(nested.GetAttributeValue("type"), Is.EqualTo("bullet"));
        Assert.That(nested.GetChild(0).GetChild(0).Literal, Is.EqualTo("child"));
        Assert.That(list.GetChild(1).GetChild(0).Literal, Is.EqualTo("next"));
    }

    [Test]
    public void BulletListRule_ParsesNestedBulletListAfterParagraphInterruption()
    {
        MarkdownDocument document = MarkdownParser.Parse("- parent\n  - child\n- next");
        MarkdownNode list = document.Root.GetChild(0);
        MarkdownNode nested = list.GetChild(0).GetChild(1);

        Assert.That(list.GetChildCount(), Is.EqualTo(2));
        Assert.That(list.GetChild(0).GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(list.GetChild(0).GetChild(0).Literal, Is.EqualTo("parent"));
        Assert.That(nested.Kind, Is.EqualTo(MarkdownNodeKind.List));
        Assert.That(nested.GetAttributeValue("type"), Is.EqualTo("bullet"));
        Assert.That(nested.GetChild(0).GetChild(0).Literal, Is.EqualTo("child"));
        Assert.That(list.GetChild(1).GetChild(0).Literal, Is.EqualTo("next"));
    }

    [Test]
    public void BulletListRule_RejectsInvalidMarkers()
    {
        Assert.That(IsBulletListItem("- item"), Is.True);
        Assert.That(IsBulletListItem("+ item"), Is.True);
        Assert.That(IsBulletListItem("* item"), Is.True);
        Assert.That(IsBulletListItem("---"), Is.False);
        Assert.That(IsBulletListItem("- - -"), Is.False);
        Assert.That(IsBulletListItem("* * *"), Is.False);
        Assert.That(IsBulletListItem("-item"), Is.False);
        Assert.That(IsBulletListItem("x item"), Is.False);
        Assert.That(IsBulletListItem("    - code"), Is.False);
        Assert.That(GetBulletMarker("* item"), Is.EqualTo("*"));
        Assert.That(RemoveBulletMarker("- item"), Is.EqualTo("item"));
        Assert.That(GetBulletContentIndent("- item"), Is.EqualTo(2));
        Assert.That(GetBulletMarkerColumn("  - item"), Is.EqualTo(2));
    }

    [Test]
    public void Parser_DefaultPipelineCreatesBulletListBeforeParagraph()
    {
        MarkdownDocument document = MarkdownParser.Parse("- one\n\ntext");

        Assert.That(document.Root.GetChildCount(), Is.EqualTo(2));
        Assert.That(document.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.List));
        Assert.That(document.Root.GetChild(0).GetChild(0).GetChild(0).Literal, Is.EqualTo("one"));
        Assert.That(document.Root.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(document.Root.GetChild(1).Literal, Is.EqualTo("text"));
    }

    [Test]
    public void Parser_FourSpaceBulletListMarkerIsIndentedCodeBlock()
    {
        MarkdownDocument document = MarkdownParser.Parse("    - code");

        Assert.That(document.Root.GetChildCount(), Is.EqualTo(1));
        Assert.That(document.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.IndentedCodeBlock));
        Assert.That(document.Root.GetChild(0).Literal, Is.EqualTo("- code"));
    }

    [Test]
    public void SetextHeadingRule_ParsesEqualsAndDashUnderlines()
    {
        MarkdownDocument firstDocument = MarkdownParser.Parse("Title\n===");
        MarkdownDocument secondDocument = MarkdownParser.Parse("Subtitle\n---");

        MarkdownNode first = firstDocument.Root.GetChild(0);
        MarkdownNode second = secondDocument.Root.GetChild(0);

        Assert.That(firstDocument.Root.GetChildCount(), Is.EqualTo(1));
        Assert.That(first.Kind, Is.EqualTo(MarkdownNodeKind.Heading));
        Assert.That(first.GetAttributeValue("level"), Is.EqualTo("1"));
        Assert.That(first.Literal, Is.EqualTo("Title"));
        Assert.That(secondDocument.Root.GetChildCount(), Is.EqualTo(1));
        Assert.That(second.Kind, Is.EqualTo(MarkdownNodeKind.Heading));
        Assert.That(second.GetAttributeValue("level"), Is.EqualTo("2"));
        Assert.That(second.Literal, Is.EqualTo("Subtitle"));
    }

    [Test]
    public void SetextHeadingRule_ParsesMultiLineHeadingText()
    {
        MarkdownDocument document = MarkdownParser.Parse("alpha\nbeta\n---");
        MarkdownNode heading = document.Root.GetChild(0);

        Assert.That(document.Root.GetChildCount(), Is.EqualTo(1));
        Assert.That(heading.Kind, Is.EqualTo(MarkdownNodeKind.Heading));
        Assert.That(heading.GetAttributeValue("level"), Is.EqualTo("2"));
        Assert.That(heading.Literal, Is.EqualTo("alpha\nbeta"));
        Assert.That(heading.Span.Start.Offset, Is.EqualTo(0));
        Assert.That(heading.Span.End.Offset, Is.EqualTo(14));
        Assert.That(heading.Span.End.Line, Is.EqualTo(3));
        Assert.That(heading.Span.End.Column, Is.EqualTo(4));
    }

    [Test]
    public void SetextHeadingRule_RejectsInvalidUnderlineForms()
    {
        Assert.That(IsSetextUnderline("==="), Is.True);
        Assert.That(IsSetextUnderline("-"), Is.True);
        Assert.That(IsSetextUnderline("   ---   "), Is.True);
        Assert.That(IsSetextUnderline("    ---"), Is.False);
        Assert.That(IsSetextUnderline("\t---"), Is.False);
        Assert.That(IsSetextUnderline("- - -"), Is.False);
        Assert.That(IsSetextUnderline("===x"), Is.False);
        Assert.That(IsSetextUnderline("text"), Is.False);
        Assert.That(IsSetextUnderline(""), Is.False);
    }

    [Test]
    public void Parser_SetextHeadingConsumesUnderlineBeforeParagraphRule()
    {
        MarkdownDocument document = MarkdownParser.Parse("Title\n---\nnext");

        Assert.That(document.Root.GetChildCount(), Is.EqualTo(2));
        Assert.That(document.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Heading));
        Assert.That(document.Root.GetChild(0).GetAttributeValue("level"), Is.EqualTo("2"));
        Assert.That(document.Root.GetChild(0).Literal, Is.EqualTo("Title"));
        Assert.That(document.Root.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(document.Root.GetChild(1).Literal, Is.EqualTo("next"));
    }

    [Test]
    public void Parser_DefaultPipelineCreatesParagraphsSeparatedByBlankLines()
    {
        MarkdownDocument document = MarkdownParser.Parse("alpha\nbeta\n\nnext");

        Assert.That(document.Root.GetChildCount(), Is.EqualTo(2));
        Assert.That(document.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(document.Root.GetChild(0).Literal, Is.EqualTo("alpha\nbeta"));
        Assert.That(document.Root.GetChild(1).Literal, Is.EqualTo("next"));
    }

    [Test]
    public void Parser_DefaultPipelineConsumesBlankLinesWithoutNodes()
    {
        MarkdownDocument document = MarkdownParser.Parse("\n  \t\r\n");

        Assert.That(document.Root.GetChildCount(), Is.EqualTo(0));
        Assert.That(document.HasDiagnostics(), Is.False);
    }

    [Test]
    public void CommonMarkPresetsRegisterBlankLineRule()
    {
        Assert.That(MarkdownPipeline.CommonMarkPreset().GetBlockRules().Contains(CommonMarkBlockRules.BlankLineRuleName), Is.True);
        Assert.That(MarkdownPipeline.CommonMarkPreset().GetBlockRules().Contains(CommonMarkBlockRules.BlockQuoteRuleName), Is.True);
        Assert.That(MarkdownPipeline.CommonMarkPreset().GetBlockRules().Contains(CommonMarkBlockRules.BulletListRuleName), Is.True);
        Assert.That(MarkdownPipeline.CommonMarkPreset().GetBlockRules().Contains(CommonMarkBlockRules.AtxHeadingRuleName), Is.True);
        Assert.That(MarkdownPipeline.CommonMarkPreset().GetBlockRules().Contains(CommonMarkBlockRules.ThematicBreakRuleName), Is.True);
        Assert.That(MarkdownPipeline.CommonMarkPreset().GetBlockRules().Contains(CommonMarkBlockRules.FencedCodeBlockRuleName), Is.True);
        Assert.That(MarkdownPipeline.CommonMarkPreset().GetBlockRules().Contains(CommonMarkBlockRules.HtmlBlockRuleName), Is.True);
        Assert.That(MarkdownPipeline.CommonMarkPreset().GetBlockRules().Contains(CommonMarkBlockRules.IndentedCodeBlockRuleName), Is.True);
        Assert.That(MarkdownPipeline.CommonMarkPreset().GetBlockRules().Contains(CommonMarkBlockRules.LinkReferenceDefinitionRuleName), Is.True);
        Assert.That(MarkdownPipeline.CommonMarkPreset().GetBlockRules().Contains(CommonMarkBlockRules.OrderedListRuleName), Is.True);
        Assert.That(MarkdownPipeline.CommonMarkPreset().GetBlockRules().Contains(CommonMarkBlockRules.ParagraphRuleName), Is.True);
        Assert.That(MarkdownPipeline.GfmPreset().GetBlockRules().Contains(CommonMarkBlockRules.BlankLineRuleName), Is.True);
        Assert.That(MarkdownPipeline.AdvancedPreset().GetBlockRules().Contains(CommonMarkBlockRules.BlankLineRuleName), Is.True);
        Assert.That(MarkdownPipeline.ZeroPreset().GetBlockRules().Contains(CommonMarkBlockRules.BlankLineRuleName), Is.False);
        Assert.That(MarkdownPipelineBuilder.CommonMarkPreset().GetBlockRules().Contains(CommonMarkBlockRules.BlankLineRuleName), Is.True);
        Assert.That(MarkdownPipelineBuilder.GfmPreset().GetBlockRules().Contains(CommonMarkBlockRules.BlankLineRuleName), Is.True);
        Assert.That(MarkdownPipelineBuilder.AdvancedPreset().GetBlockRules().Contains(CommonMarkBlockRules.BlankLineRuleName), Is.True);
        Assert.That(MarkdownPipelineBuilder.ZeroPreset().GetBlockRules().Contains(CommonMarkBlockRules.BlankLineRuleName), Is.False);
    }

    [Test]
    public void Builder_UseCommonMarkRegistersBlankLineRuleIdempotently()
    {
        MarkdownPipelineBuilder builder = new MarkdownPipelineBuilder();

        builder.UseCommonMark();
        builder.UseCommonMark();

        Assert.That(builder.GetBlockRules().GetCount(), Is.EqualTo(11));
        Assert.That(builder.GetBlockRules().Contains(CommonMarkBlockRules.BlankLineRuleName), Is.True);
        Assert.That(builder.GetBlockRules().Contains(CommonMarkBlockRules.BlockQuoteRuleName), Is.True);
        Assert.That(builder.GetBlockRules().Contains(CommonMarkBlockRules.BulletListRuleName), Is.True);
        Assert.That(builder.GetBlockRules().Contains(CommonMarkBlockRules.AtxHeadingRuleName), Is.True);
        Assert.That(builder.GetBlockRules().Contains(CommonMarkBlockRules.ThematicBreakRuleName), Is.True);
        Assert.That(builder.GetBlockRules().Contains(CommonMarkBlockRules.FencedCodeBlockRuleName), Is.True);
        Assert.That(builder.GetBlockRules().Contains(CommonMarkBlockRules.HtmlBlockRuleName), Is.True);
        Assert.That(builder.GetBlockRules().Contains(CommonMarkBlockRules.IndentedCodeBlockRuleName), Is.True);
        Assert.That(builder.GetBlockRules().Contains(CommonMarkBlockRules.LinkReferenceDefinitionRuleName), Is.True);
        Assert.That(builder.GetBlockRules().Contains(CommonMarkBlockRules.OrderedListRuleName), Is.True);
        Assert.That(builder.GetBlockRules().Contains(CommonMarkBlockRules.ParagraphRuleName), Is.True);

        builder.UseZero();

        Assert.That(builder.GetBlockRules().Contains(CommonMarkBlockRules.BlankLineRuleName), Is.False);
        Assert.That(builder.GetBlockRules().Contains(CommonMarkBlockRules.BlockQuoteRuleName), Is.False);
        Assert.That(builder.GetBlockRules().Contains(CommonMarkBlockRules.BulletListRuleName), Is.False);
        Assert.That(builder.GetBlockRules().Contains(CommonMarkBlockRules.AtxHeadingRuleName), Is.False);
        Assert.That(builder.GetBlockRules().Contains(CommonMarkBlockRules.ThematicBreakRuleName), Is.False);
        Assert.That(builder.GetBlockRules().Contains(CommonMarkBlockRules.FencedCodeBlockRuleName), Is.False);
        Assert.That(builder.GetBlockRules().Contains(CommonMarkBlockRules.HtmlBlockRuleName), Is.False);
        Assert.That(builder.GetBlockRules().Contains(CommonMarkBlockRules.IndentedCodeBlockRuleName), Is.False);
        Assert.That(builder.GetBlockRules().Contains(CommonMarkBlockRules.LinkReferenceDefinitionRuleName), Is.False);
        Assert.That(builder.GetBlockRules().Contains(CommonMarkBlockRules.OrderedListRuleName), Is.False);
        Assert.That(builder.GetBlockRules().Contains(CommonMarkBlockRules.ParagraphRuleName), Is.False);
    }

    [Test]
    public void CommonMarkBlockRules_RejectInvalidArguments()
    {
        Assert.That(() => CommonMarkBlockRules.RegisterCommonMarkBlockRules(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.TryParseBlankLine(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.TryParseBlockQuote(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.TryParseBulletList(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.TryParseParagraph(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.TryParseAtxHeading(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.TryParseThematicBreak(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.TryParseFencedCodeBlock(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.TryParseHtmlBlock(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.TryParseIndentedCodeBlock(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.TryParseLinkReferenceDefinition(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.TryParseOrderedList(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.IsBlankLine(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.GetAtxHeadingLevel(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.GetAtxHeadingText(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.GetSetextHeadingLevel(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.IsSetextHeadingUnderline(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.IsBlockQuoteLine(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.RemoveBlockQuoteMarker(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.IsFencedCodeBlockOpeningLine(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.GetFencedCodeBlockFenceMarker(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.GetFencedCodeBlockFenceLength(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.GetFencedCodeBlockIndent(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.GetFencedCodeBlockInfoString(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.IsFencedCodeBlockClosingLine(null!, '`', 3), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.IsHtmlBlockStartLine(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.GetHtmlBlockType(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.GetHtmlBlockType1TagName(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.IsHtmlBlockEndLine(null!, 1, "script"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.IsHtmlBlockEndLine(MakeLine("</script>"), 1, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.IsHtmlBlockType6StartLine(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.IsHtmlBlockType7StartLine(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.GetHtmlStartOrClosingTagName(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.IsHtmlBlockTagName(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.IsLinkReferenceDefinitionLine(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.GetLinkReferenceDefinitionLabel(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.GetLinkReferenceDefinitionDestination(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.GetLinkReferenceDefinitionTitle(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.NormalizeLinkReferenceLabel(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.GetLinkReferenceDefinitionEndIndex(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.GetLinkReferenceDefinitionTitleStartIndex(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.GetLinkReferenceDefinitionDestinationStartIndex(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.GetLinkReferenceDefinitionDestinationEndIndex(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.GetLinkReferenceDefinitionLabelEndIndex(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.IsOrderedListItemLine(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.GetOrderedListItemNumber(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.GetOrderedListItemMarker(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.RemoveOrderedListItemMarker(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.GetOrderedListItemContentStartIndex(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.GetOrderedListItemMarkerColumn(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.GetOrderedListItemContentIndent(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.GetOrderedListItemMarkerIndex(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.IsBulletListItemLine(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.GetBulletListItemMarker(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.RemoveBulletListItemMarker(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.GetBulletListItemContentStartIndex(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.GetBulletListItemMarkerColumn(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.GetBulletListItemContentIndent(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.GetBulletListItemMarkerIndex(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.IsIndentedCodeLine(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.RemoveIndentedCodePrefix(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.RemoveOptionalIndentColumns(null!, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.RemoveOptionalIndentColumns(MakeLine("text"), -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkBlockRules.IsThematicBreakLine(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.StartsWithAt(null!, 0, "x"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.StartsWithAt("x", 0, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.StartsWithAtIgnoreCase(null!, 0, "x"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.StartsWithAtIgnoreCase("x", 0, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(CommonMarkBlockRules.StartsWithAt("x", int.MaxValue, "x"), Is.False);
        Assert.That(CommonMarkBlockRules.StartsWithAtIgnoreCase("x", int.MaxValue, "x"), Is.False);
        Assert.That(() => CommonMarkBlockRules.ContainsOrdinalIgnoreCase(null!, "x"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.ContainsOrdinalIgnoreCase("x", null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.StartsHtmlType1Tag(null!, 0, "script"), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.StartsHtmlType1Tag("<script>", 0, null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.GetCompleteHtmlTagEndIndex(null!, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.IsEscapedAt(null!, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.GetLineIndentColumn(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.GetColumnAtIndex(null!, 0), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.GetColumnAtIndex("text", -1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => CommonMarkBlockRules.JoinLines(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.CanContinueLazily(null!, MarkdownPipeline.CommonMarkPreset()), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.CanContinueLazily(new System.Collections.Generic.List<string>(), null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.IsParagraphInterruptingBlockLine(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => CommonMarkBlockRules.HasNonBlankText(null!), Throws.TypeOf<ArgumentNullException>());
    }

    static MarkdownNode ParseSingleHeading(string source)
    {
        MarkdownDocument document = MarkdownParser.Parse(source);
        return document.Root.GetChild(0);
    }

    static bool IsAtxHeading(string source)
    {
        MarkdownBlockParseContext context = MarkdownBlockParseContext.Create(source, MarkdownPipeline.CommonMarkPreset());
        MarkdownLine line = context.GetCurrentLine()!;
        return CommonMarkBlockRules.GetAtxHeadingLevel(line) > 0;
    }

    static bool IsThematicBreak(string source)
    {
        MarkdownBlockParseContext context = MarkdownBlockParseContext.Create(source, MarkdownPipeline.CommonMarkPreset());
        MarkdownLine line = context.GetCurrentLine()!;
        return CommonMarkBlockRules.IsThematicBreakLine(line);
    }

    static bool IsSetextUnderline(string source)
    {
        MarkdownBlockParseContext context = MarkdownBlockParseContext.Create(source, MarkdownPipeline.CommonMarkPreset());
        MarkdownLine? line = context.GetCurrentLine();
        if (line == null)
        {
            return false;
        }

        return CommonMarkBlockRules.IsSetextHeadingUnderline(line);
    }

    static bool IsIndentedCodeLine(string source)
    {
        MarkdownBlockParseContext context = MarkdownBlockParseContext.Create(source, MarkdownPipeline.CommonMarkPreset());
        MarkdownLine line = context.GetCurrentLine()!;
        return CommonMarkBlockRules.IsIndentedCodeLine(line);
    }

    static bool IsBlockQuoteLine(string source)
    {
        MarkdownLine line = MakeLine(source);
        return CommonMarkBlockRules.IsBlockQuoteLine(line);
    }

    static string RemoveBlockQuote(string source)
    {
        MarkdownLine line = MakeLine(source);
        return CommonMarkBlockRules.RemoveBlockQuoteMarker(line);
    }

    static string RemoveCodePrefix(string source)
    {
        MarkdownBlockParseContext context = MarkdownBlockParseContext.Create(source, MarkdownPipeline.CommonMarkPreset());
        MarkdownLine line = context.GetCurrentLine()!;
        return CommonMarkBlockRules.RemoveIndentedCodePrefix(line);
    }

    static bool IsFencedCodeOpening(string source)
    {
        MarkdownLine line = MakeLine(source);
        return CommonMarkBlockRules.IsFencedCodeBlockOpeningLine(line);
    }

    static bool IsFencedCodeClosing(string source, char marker, int length)
    {
        MarkdownLine line = MakeLine(source);
        return CommonMarkBlockRules.IsFencedCodeBlockClosingLine(line, marker, length);
    }

    static char GetFenceMarker(string source)
    {
        MarkdownLine line = MakeLine(source);
        return CommonMarkBlockRules.GetFencedCodeBlockFenceMarker(line);
    }

    static int GetFenceLength(string source)
    {
        MarkdownLine line = MakeLine(source);
        return CommonMarkBlockRules.GetFencedCodeBlockFenceLength(line);
    }

    static int GetFenceIndent(string source)
    {
        MarkdownLine line = MakeLine(source);
        return CommonMarkBlockRules.GetFencedCodeBlockIndent(line);
    }

    static string GetFenceInfo(string source)
    {
        MarkdownLine line = MakeLine(source);
        return CommonMarkBlockRules.GetFencedCodeBlockInfoString(line);
    }

    static int GetHtmlType(string source)
    {
        MarkdownLine line = MakeLine(source);
        return CommonMarkBlockRules.GetHtmlBlockType(line);
    }

    static bool IsHtmlEnd(string source, int blockType, string type1TagName)
    {
        MarkdownLine line = MakeLine(source);
        return CommonMarkBlockRules.IsHtmlBlockEndLine(line, blockType, type1TagName);
    }

    static void AssertFirstHtmlBlock(string source, string literal)
    {
        MarkdownDocument document = MarkdownParser.Parse(source);

        Assert.That(document.Root.GetChildCount(), Is.EqualTo(2));
        Assert.That(document.Root.GetChild(0).Kind, Is.EqualTo(MarkdownNodeKind.HtmlBlock));
        Assert.That(document.Root.GetChild(0).Literal, Is.EqualTo(literal));
        Assert.That(document.Root.GetChild(1).Kind, Is.EqualTo(MarkdownNodeKind.Paragraph));
        Assert.That(document.Root.GetChild(1).Literal, Is.EqualTo("next"));
    }

    static MarkdownNode ParseSingleLinkReference(string source)
    {
        MarkdownDocument document = MarkdownParser.Parse(source);
        return document.Root.GetChild(0);
    }

    static bool IsLinkReferenceDefinition(string source)
    {
        MarkdownLine line = MakeLine(source);
        return CommonMarkBlockRules.IsLinkReferenceDefinitionLine(line);
    }

    static bool IsOrderedListItem(string source)
    {
        MarkdownLine line = MakeLine(source);
        return CommonMarkBlockRules.IsOrderedListItemLine(line);
    }

    static int GetOrderedNumber(string source)
    {
        MarkdownLine line = MakeLine(source);
        return CommonMarkBlockRules.GetOrderedListItemNumber(line);
    }

    static string GetOrderedMarker(string source)
    {
        MarkdownLine line = MakeLine(source);
        return CommonMarkBlockRules.GetOrderedListItemMarker(line);
    }

    static string RemoveOrderedMarker(string source)
    {
        MarkdownLine line = MakeLine(source);
        return CommonMarkBlockRules.RemoveOrderedListItemMarker(line);
    }

    static int GetOrderedContentIndent(string source)
    {
        MarkdownLine line = MakeLine(source);
        return CommonMarkBlockRules.GetOrderedListItemContentIndent(line);
    }

    static int GetOrderedMarkerColumn(string source)
    {
        MarkdownLine line = MakeLine(source);
        return CommonMarkBlockRules.GetOrderedListItemMarkerColumn(line);
    }

    static bool IsBulletListItem(string source)
    {
        MarkdownLine line = MakeLine(source);
        return CommonMarkBlockRules.IsBulletListItemLine(line);
    }

    static string GetBulletMarker(string source)
    {
        MarkdownLine line = MakeLine(source);
        return CommonMarkBlockRules.GetBulletListItemMarker(line);
    }

    static string RemoveBulletMarker(string source)
    {
        MarkdownLine line = MakeLine(source);
        return CommonMarkBlockRules.RemoveBulletListItemMarker(line);
    }

    static int GetBulletContentIndent(string source)
    {
        MarkdownLine line = MakeLine(source);
        return CommonMarkBlockRules.GetBulletListItemContentIndent(line);
    }

    static int GetBulletMarkerColumn(string source)
    {
        MarkdownLine line = MakeLine(source);
        return CommonMarkBlockRules.GetBulletListItemMarkerColumn(line);
    }

    static MarkdownLine MakeLine(string source)
    {
        MarkdownBlockParseContext context = MarkdownBlockParseContext.Create(source, MarkdownPipeline.CommonMarkPreset());
        return context.GetCurrentLine()!;
    }
}
