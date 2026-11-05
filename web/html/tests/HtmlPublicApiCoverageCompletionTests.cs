using System.Text;

namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlPublicApiCoverageCompletionTests
{
    [Test]
    public void FactoryCreatesEveryPresetAndValueKind()
    {
        IHtmlFactory factory = new HtmlFactory();
        HtmlSourcePosition start = factory.CreateMappedSourcePosition(
            2, 3, 1, 4);
        HtmlSourcePosition end = factory.CreateMappedSourcePosition(
            7, 9, 1, 10);
        HtmlSourcePosition bytes = factory.CreateByteSourcePosition(
            3, 1, 4, 6);
        HtmlSourcePosition unknown = factory.CreateUnknownSourcePosition();
        HtmlSourceSpan span = factory.CreateSourceSpan(start, end);
        HtmlSourceSpan unknownSpan = factory.CreateUnknownSourceSpan();
        HtmlDocument document = factory.CreateDocument(
            "https://example.test/page", "https://example.test/", "en",
            unknownSpan);
        HtmlAttribute plain = factory.CreateAttribute("id", "main", span);
        HtmlAttribute namespaced = factory.CreateNamespacedAttribute(
            "xlink:href", "href", "xlink",
            "http://www.w3.org/1999/xlink", "next", span);
        HtmlLimits defaults = factory.CreateDefaultLimits();
        HtmlLimits untrusted = factory.CreateUntrustedLimits();
        HtmlParseOptions parse = factory.CreateParseOptions();
        HtmlSerializerOptions serializer = factory.CreateSerializerOptions();
        HtmlSerializerOptions preserving =
            factory.CreatePreservingSerializerOptions();
        HtmlSerializerOptions compact =
            factory.CreateCompactSerializerOptions();
        HtmlSerializerOptions pretty =
            factory.CreatePrettySerializerOptions();
        HtmlTokenizerOptions tokenizer = factory.CreateTokenizerOptions();
        HtmlTokenizerOptions untrustedTokenizer =
            factory.CreateUntrustedTokenizerOptions();
        HtmlProcessingOptions rich =
            factory.CreateRichTextProcessingOptions();
        HtmlProcessingOptions article =
            factory.CreateArticleProcessingOptions();
        HtmlProcessingOptions llm = factory.CreateLlmProcessingOptions();
        HtmlSrcsetCandidate url = factory.CreateSrcsetCandidate("a.png");
        HtmlSrcsetCandidate width =
            factory.CreateWidthSrcsetCandidate("b.png", 640);
        HtmlSrcsetCandidate density =
            factory.CreateDensitySrcsetCandidate("c.png", 2.0);

        Assert.Multiple(() =>
        {
            Assert.That(start.GetNormalizedCharacterOffset(), Is.EqualTo(2));
            Assert.That(start.GetCharacterOffset(), Is.EqualTo(3));
            Assert.That(bytes.HasByteOffset(), Is.True);
            Assert.That(bytes.GetByteOffset(), Is.EqualTo(6));
            Assert.That(unknown.IsKnown(), Is.False);
            Assert.That(unknown.GetCharacterOffset(), Is.EqualTo(-1));
            Assert.That(span.GetNormalizedCharacterLength(), Is.EqualTo(5));
            Assert.That(span.GetCharacterLength(), Is.EqualTo(6));
            Assert.That(span.GetByteLength(), Is.EqualTo(-1));
            Assert.That(unknownSpan.IsKnown(), Is.False);
            Assert.That(unknownSpan.GetStart().IsKnown(), Is.False);
            Assert.That(unknownSpan.GetEnd().IsKnown(), Is.False);
            Assert.That(unknownSpan.GetNormalizedCharacterLength(),
                Is.EqualTo(-1));
            Assert.That(unknownSpan.GetCharacterLength(), Is.EqualTo(-1));
            Assert.That(unknownSpan.GetByteLength(), Is.EqualTo(-1));
            Assert.That(document.GetResponseUrl(),
                Is.EqualTo("https://example.test/page"));
            Assert.That(document.GetBaseUrl(),
                Is.EqualTo("https://example.test/"));
            Assert.That(document.GetContentLanguage(), Is.EqualTo("en"));
            Assert.That(plain.GetName(), Is.EqualTo("id"));
            Assert.That(plain.GetLocalName(), Is.EqualTo("id"));
            Assert.That(plain.GetPrefix(), Is.Empty);
            Assert.That(plain.GetNamespaceUri(), Is.Empty);
            Assert.That(plain.GetValue(), Is.EqualTo("main"));
            Assert.That(plain.GetSourceSpan(), Is.SameAs(span));
            Assert.That(namespaced.GetName(), Is.EqualTo("xlink:href"));
            Assert.That(namespaced.GetLocalName(), Is.EqualTo("href"));
            Assert.That(namespaced.GetPrefix(), Is.EqualTo("xlink"));
            Assert.That(namespaced.GetNamespaceUri(),
                Is.EqualTo("http://www.w3.org/1999/xlink"));
            Assert.That(defaults.GetMaximumInputCharacters(),
                Is.GreaterThan(untrusted.GetMaximumInputCharacters()));
            Assert.That(parse.GetTrackSourceLocations(), Is.True);
            Assert.That(serializer.GetNameMode(),
                Is.EqualTo(HtmlSerializerNameMode.Canonical));
            Assert.That(preserving.GetNameMode(),
                Is.EqualTo(HtmlSerializerNameMode.Preserve));
            Assert.That(compact.GetOmitOptionalTags(), Is.True);
            Assert.That(pretty.GetPretty(), Is.True);
            Assert.That(tokenizer.GetTrackSourceLocations(), Is.True);
            Assert.That(tokenizer.GetCoalesceCharacterTokens(), Is.True);
            Assert.That(untrustedTokenizer.GetLimits()
                .GetMaximumInputCharacters(), Is.EqualTo(
                    untrusted.GetMaximumInputCharacters()));
            Assert.That(rich.GetSanitize(), Is.True);
            Assert.That(rich.GetSerializeHtml(), Is.True);
            Assert.That(article.GetExtractMetadata(), Is.True);
            Assert.That(article.GetExtractArticle(), Is.True);
            Assert.That(article.GetConvertText(), Is.True);
            Assert.That(article.GetConvertMarkdown(), Is.True);
            Assert.That(llm.GetSerializeHtml(), Is.False);
            Assert.That(llm.GetConvertText(), Is.True);
            Assert.That(llm.GetConvertMarkdown(), Is.True);
            Assert.That(url.GetUrl(), Is.EqualTo("a.png"));
            Assert.That(url.HasWidth(), Is.False);
            Assert.That(url.HasDensity(), Is.False);
            Assert.That(width.HasWidth(), Is.True);
            Assert.That(width.GetWidth(), Is.EqualTo(640));
            Assert.That(width.HasDensity(), Is.False);
            Assert.That(density.HasDensity(), Is.True);
            Assert.That(density.GetDensity(), Is.EqualTo(2.0));
            Assert.That(density.HasWidth(), Is.False);
        });
    }

    [Test]
    public void FactoryRejectsInvalidOwnersAndValueArguments()
    {
        IHtmlFactory factory = new HtmlFactory();
        HtmlSourcePosition position = factory.CreateSourcePosition(0, 1, 1);
        HtmlSourcePosition next = factory.CreateSourcePosition(1, 1, 2);
        HtmlSourceSpan span = factory.CreateSourceSpan(position, next);
        HtmlDocument document = factory.CreateDocument("", "", "", span);

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentNullException>(() =>
                factory.CreateDocumentTypeNode(null, "html", null, null,
                    false, span))!.ParamName, Is.EqualTo("ownerDocument"));
            Assert.That(Assert.Throws<ArgumentNullException>(() =>
                factory.CreateElementNode(null, "", "p", span))!.ParamName,
                Is.EqualTo("ownerDocument"));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                factory.CreateMappedSourcePosition(-1, 0, 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                factory.CreateMappedSourcePosition(0, -1, 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                factory.CreateMappedSourcePosition(0, 0, 0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                factory.CreateMappedSourcePosition(0, 0, 1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                factory.CreateByteSourcePosition(0, 1, 1, -1));
            Assert.Throws<ArgumentNullException>(() =>
                factory.CreateSourceSpan(null, next));
            Assert.Throws<ArgumentNullException>(() =>
                factory.CreateSourceSpan(position, null));
            Assert.Throws<ArgumentException>(() => factory.CreateSourceSpan(
                factory.CreateUnknownSourcePosition(), next));
            Assert.Throws<ArgumentException>(() => factory.CreateSourceSpan(
                next, position));
            Assert.Throws<ArgumentException>(() => factory.CreateSourceSpan(
                factory.CreateByteSourcePosition(0, 1, 1, 0), next));
            Assert.Throws<ArgumentException>(() => factory.CreateSourceSpan(
                factory.CreateByteSourcePosition(2, 1, 1, 2),
                factory.CreateByteSourcePosition(3, 1, 2, 1)));
            Assert.Throws<ArgumentNullException>(() =>
                factory.CreateAttribute(null, "value", span));
            Assert.Throws<ArgumentException>(() =>
                factory.CreateAttribute("", "value", span));
            Assert.Throws<ArgumentNullException>(() =>
                factory.CreateNamespacedAttribute("x:y", "y", null, "x",
                    "value", span));
            Assert.Throws<ArgumentNullException>(() =>
                factory.CreateNamespacedAttribute("x:y", "y", "x", null,
                    "value", span));
            Assert.Throws<ArgumentNullException>(() =>
                factory.CreateNamespacedAttribute("x:y", "y", "x", "x",
                    null, span));
            Assert.Throws<ArgumentNullException>(() =>
                factory.CreateNamespacedAttribute("x:y", "y", "x", "x",
                    "value", null));
            Assert.Throws<ArgumentNullException>(() =>
                factory.CreateSrcsetCandidate(null));
            Assert.Throws<ArgumentException>(() =>
                factory.CreateSrcsetCandidate(""));
            Assert.Throws<ArgumentException>(() =>
                factory.CreateSrcsetCandidate(",a.png"));
            Assert.Throws<ArgumentException>(() =>
                factory.CreateSrcsetCandidate("a b.png"));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                factory.CreateWidthSrcsetCandidate("a.png", 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                factory.CreateDensitySrcsetCandidate("a.png", 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                factory.CreateDensitySrcsetCandidate("a.png",
                    double.PositiveInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                factory.CreateDensitySrcsetCandidate("a.png", double.NaN));
        });

        Assert.That(document.GetRoot(), Is.Not.Null);
    }

    [Test]
    public void CompiledConfigurationsExposeImmutableDefensiveSnapshots()
    {
        HtmlFactory factory = new HtmlFactory();
        HtmlLimits limits = factory.CreateDefaultLimits();
        limits.SetMaximumVisits(3456);
        HtmlParseOptions parse = factory.CreateParseOptions();
        parse.SetResponseUrl("https://example.test/page");
        parse.SetBaseUrl("https://example.test/base/");
        parse.SetContentLanguage("fr");
        parse.SetTransportEncodingLabel("utf-8");
        parse.SetScriptingEnabled(false);
        parse.SetTrackSourceLocations(false);
        parse.SetTrackByteOffsets(true);
        parse.SetLeaveOpen(true);
        parse.SetLimits(limits);
        HtmlCompiledParseOptions compiledParse = parse.Compile();
        HtmlParseOptions explicitlyEncoded = factory.CreateParseOptions();
        explicitlyEncoded.SetEncoding(Encoding.UTF8);
        HtmlCompiledParseOptions compiledEncoding =
            explicitlyEncoded.Compile();

        HtmlSerializerOptions serializer = factory.CreateSerializerOptions();
        serializer.SetNameMode(HtmlSerializerNameMode.Preserve);
        serializer.SetQuoteMode(HtmlSerializerQuoteMode.Shortest);
        serializer.SetAsciiOnly(true);
        serializer.SetSortAttributes(true);
        serializer.SetSelfCloseForeignElements(true);
        serializer.SetPretty(true);
        serializer.SetOmitOptionalTags(true);
        serializer.SetIndentation("\t");
        serializer.SetNewLine("\r\n");
        serializer.SetLimits(limits);
        HtmlCompiledSerializerOptions compiledSerializer =
            serializer.Compile();

        HtmlTextOptions text = factory.CreateTextOptions();
        text.SetLinkStyle(HtmlTextLinkStyle.TextAndUrl);
        text.SetImageStyle(HtmlTextImageStyle.AltTextAndUrl);
        text.SetTableStyle(HtmlTextTableStyle.ReadingOrder);
        text.SetWhitespaceMode(HtmlTextWhitespaceMode.Preserve);
        text.SetNonBreakingSpaceMode(HtmlTextNonBreakingSpaceMode.Preserve);
        text.SetWrapColumn(72);
        text.SetTruncationMarker("[cut]");
        text.SetNewLine("\r\n");
        text.SetTrim(false);
        text.SetMaximumBlankLines(3);
        text.SetLineBoundaryBreaks(1);
        text.SetBlockBoundaryBreaks(2);
        text.SetUnorderedListBullets(["-", "+"]);
        text.SetMaximumListDepth(4);
        text.SetIncludeFormControls(true);
        text.SetIncludeHiddenContent(true);
        text.SetBaseUrl("https://example.test/base/");
        text.AddRule(factory.CreateReplaceTextRule("em", "replacement"));
        text.SetLimits(limits);
        HtmlCompiledTextOptions compiledText = text.Compile();

        HtmlMarkdownOptions markdown = factory.CreateCommonMarkOptions();
        markdown.SetHeadingStyle(HtmlMarkdownHeadingStyle.Setext);
        markdown.SetEmphasisMarker("_");
        markdown.SetStrongMarker("__");
        markdown.SetHardBreakStyle(HtmlMarkdownHardBreakStyle.Backslash);
        markdown.SetCodeFenceStyle(HtmlMarkdownCodeFenceStyle.Tildes);
        markdown.SetThematicBreakMarker("___");
        markdown.SetLinkStyle(HtmlMarkdownLinkStyle.Reference);
        markdown.SetUnsafeLinkFallback(
            HtmlMarkdownUnsafeLinkFallback.Text);
        markdown.SetBaseUrl("https://example.test/base/");
        markdown.SetUnknownElementAction(
            HtmlMarkdownUnknownElementAction.Unwrap);
        markdown.SetOrderedListNumbering(
            HtmlMarkdownOrderedListNumbering.Preserve);
        markdown.SetUnorderedListMarker("+");
        markdown.SetEnableTables(true);
        markdown.SetEnableStrikethrough(true);
        markdown.SetEnableTaskLists(true);
        markdown.SetEnableAutolinks(true);
        markdown.SetEnableDefinitionLists(true);
        markdown.SetEnableFootnotes(true);
        markdown.SetEnableSubscript(true);
        markdown.SetEnableSuperscript(true);
        markdown.SetEnableTrustedRawHtml(true);
        markdown.SetNewLine("\r\n");
        markdown.SetTruncationMarker("[cut]");
        markdown.AddRule(factory.CreateKeepMarkdownRule("strong"));
        markdown.SetLimits(limits);
        HtmlCompiledMarkdownOptions compiledMarkdown = markdown.Compile();

        HtmlArticleOptions article = factory.CreateArticleOptions();
        article.SetMinimumContentCharacters(100);
        article.SetMinimumCandidateCharacters(25);
        article.SetScoreThreshold(7);
        article.SetMaximumTopCandidates(3);
        article.SetRemoveUnlikelyCandidates(false);
        article.SetWeightClassesAndIds(false);
        article.SetCleanConditionally(false);
        article.SetProduceSanitizedHtml(false);
        article.SetProduceText(true);
        article.SetProduceMarkdown(true);
        article.SetSanitizerPolicy(
            factory.CreateBasicFormattingSanitizerPolicy().Compile());
        article.SetTextOptions(text);
        article.SetMarkdownOptions(markdown);
        article.SetLimits(limits);
        HtmlCompiledArticleOptions compiledArticle = article.Compile();

        HtmlDocument document = Html.Parse(
            "<p><em>text</em><strong>bold</strong></p>");
        HtmlNode scope = document.GetRoot();
        HtmlNode emphasis = document.QuerySelector("em");
        HtmlNode strong = document.QuerySelector("strong");
        HtmlCompiledTextRule[] textRules = compiledText.GetRules();
        HtmlCompiledMarkdownRule[] markdownRules =
            compiledMarkdown.GetRules();
        textRules[0] = null;
        markdownRules[0] = null;
        HtmlLimits exposed = compiledArticle.GetLimits();
        exposed.SetMaximumVisits(1);

        Assert.Multiple(() =>
        {
            Assert.That(compiledParse.Compile(), Is.SameAs(compiledParse));
            Assert.That(compiledParse.GetResponseUrl(),
                Is.EqualTo("https://example.test/page"));
            Assert.That(compiledParse.GetBaseUrl(),
                Is.EqualTo("https://example.test/base/"));
            Assert.That(compiledParse.GetContentLanguage(), Is.EqualTo("fr"));
            Assert.That(compiledParse.HasEncoding(), Is.False);
            Assert.That(compiledParse.GetEncoding(), Is.Null);
            Assert.That(compiledParse.GetTransportEncodingLabel(),
                Is.EqualTo("utf-8"));
            Assert.That(compiledParse.GetScriptingEnabled(), Is.False);
            Assert.That(compiledParse.GetTrackSourceLocations(), Is.False);
            Assert.That(compiledParse.GetTrackByteOffsets(), Is.True);
            Assert.That(compiledParse.GetLeaveOpen(), Is.True);
            Assert.That(compiledParse.GetLimits().GetMaximumVisits(),
                Is.EqualTo(3456));
            Assert.That(compiledEncoding.HasEncoding(), Is.True);
            Assert.That(compiledEncoding.GetEncoding()?.WebName,
                Is.EqualTo(Encoding.UTF8.WebName));
            Assert.That(compiledSerializer.Compile(),
                Is.SameAs(compiledSerializer));
            Assert.That(compiledSerializer.GetNameMode(),
                Is.EqualTo(HtmlSerializerNameMode.Preserve));
            Assert.That(compiledSerializer.GetQuoteMode(),
                Is.EqualTo(HtmlSerializerQuoteMode.Shortest));
            Assert.That(compiledSerializer.GetAsciiOnly(), Is.True);
            Assert.That(compiledSerializer.GetSortAttributes(), Is.True);
            Assert.That(compiledSerializer.GetSelfCloseForeignElements(),
                Is.True);
            Assert.That(compiledSerializer.GetPretty(), Is.True);
            Assert.That(compiledSerializer.GetOmitOptionalTags(), Is.True);
            Assert.That(compiledSerializer.GetIndentation(), Is.EqualTo("\t"));
            Assert.That(compiledSerializer.GetNewLine(), Is.EqualTo("\r\n"));
            Assert.That(compiledSerializer.GetLimits().GetMaximumVisits(),
                Is.EqualTo(3456));
            Assert.That(compiledText.Compile(), Is.SameAs(compiledText));
            Assert.That(compiledText.GetLinkStyle(),
                Is.EqualTo(HtmlTextLinkStyle.TextAndUrl));
            Assert.That(compiledText.GetImageStyle(),
                Is.EqualTo(HtmlTextImageStyle.AltTextAndUrl));
            Assert.That(compiledText.GetTableStyle(),
                Is.EqualTo(HtmlTextTableStyle.ReadingOrder));
            Assert.That(compiledText.GetWhitespaceMode(),
                Is.EqualTo(HtmlTextWhitespaceMode.Preserve));
            Assert.That(compiledText.GetNonBreakingSpaceMode(),
                Is.EqualTo(HtmlTextNonBreakingSpaceMode.Preserve));
            Assert.That(compiledText.GetWrapColumn(), Is.EqualTo(72));
            Assert.That(compiledText.GetTruncationMarker(), Is.EqualTo("[cut]"));
            Assert.That(compiledText.GetNewLine(), Is.EqualTo("\r\n"));
            Assert.That(compiledText.GetTrim(), Is.False);
            Assert.That(compiledText.GetMaximumBlankLines(), Is.EqualTo(3));
            Assert.That(compiledText.GetLineBoundaryBreaks(), Is.EqualTo(1));
            Assert.That(compiledText.GetBlockBoundaryBreaks(), Is.EqualTo(2));
            Assert.That(compiledText.GetUnorderedListBullets(),
                Is.EqualTo(new[] { "-", "+" }));
            Assert.That(compiledText.GetMaximumListDepth(), Is.EqualTo(4));
            Assert.That(compiledText.GetIncludeFormControls(), Is.True);
            Assert.That(compiledText.GetIncludeHiddenContent(), Is.True);
            Assert.That(compiledText.GetBaseUrl(),
                Is.EqualTo("https://example.test/base/"));
            Assert.That(compiledText.GetRules()[^1].GetSelectorSource(),
                Is.EqualTo("em"));
            Assert.That(compiledText.GetRules()[^1].GetSelector(), Is.Not.Null);
            Assert.That(compiledText.GetRules()[^1].GetAction(),
                Is.EqualTo(HtmlTextRuleAction.Replace));
            Assert.That(compiledText.GetRules()[^1].GetReplacement(),
                Is.EqualTo("replacement"));
            Assert.That(compiledText.GetRules()[^1].Matches(emphasis, scope,
                CancellationToken.None), Is.True);
            Assert.That(compiledText.GetLimits().GetMaximumVisits(),
                Is.EqualTo(3456));
            Assert.That(compiledMarkdown.Compile(),
                Is.SameAs(compiledMarkdown));
            Assert.That(compiledMarkdown.GetHeadingStyle(),
                Is.EqualTo(HtmlMarkdownHeadingStyle.Setext));
            Assert.That(compiledMarkdown.GetEmphasisMarker(), Is.EqualTo("_"));
            Assert.That(compiledMarkdown.GetStrongMarker(), Is.EqualTo("__"));
            Assert.That(compiledMarkdown.GetHardBreakStyle(),
                Is.EqualTo(HtmlMarkdownHardBreakStyle.Backslash));
            Assert.That(compiledMarkdown.GetCodeFenceStyle(),
                Is.EqualTo(HtmlMarkdownCodeFenceStyle.Tildes));
            Assert.That(compiledMarkdown.GetThematicBreakMarker(),
                Is.EqualTo("___"));
            Assert.That(compiledMarkdown.GetLinkStyle(),
                Is.EqualTo(HtmlMarkdownLinkStyle.Reference));
            Assert.That(compiledMarkdown.GetUnsafeLinkFallback(),
                Is.EqualTo(HtmlMarkdownUnsafeLinkFallback.Text));
            Assert.That(compiledMarkdown.GetBaseUrl(),
                Is.EqualTo("https://example.test/base/"));
            Assert.That(compiledMarkdown.GetUnknownElementAction(),
                Is.EqualTo(HtmlMarkdownUnknownElementAction.Unwrap));
            Assert.That(compiledMarkdown.GetOrderedListNumbering(),
                Is.EqualTo(HtmlMarkdownOrderedListNumbering.Preserve));
            Assert.That(compiledMarkdown.GetUnorderedListMarker(),
                Is.EqualTo("+"));
            Assert.That(compiledMarkdown.GetEnableTables(), Is.True);
            Assert.That(compiledMarkdown.GetEnableStrikethrough(), Is.True);
            Assert.That(compiledMarkdown.GetEnableTaskLists(), Is.True);
            Assert.That(compiledMarkdown.GetEnableAutolinks(), Is.True);
            Assert.That(compiledMarkdown.GetEnableDefinitionLists(), Is.True);
            Assert.That(compiledMarkdown.GetEnableFootnotes(), Is.True);
            Assert.That(compiledMarkdown.GetEnableSubscript(), Is.True);
            Assert.That(compiledMarkdown.GetEnableSuperscript(), Is.True);
            Assert.That(compiledMarkdown.GetEnableTrustedRawHtml(), Is.True);
            Assert.That(compiledMarkdown.GetNewLine(), Is.EqualTo("\r\n"));
            Assert.That(compiledMarkdown.GetTruncationMarker(),
                Is.EqualTo("[cut]"));
            Assert.That(compiledMarkdown.GetRules()[^1].GetSelectorSource(),
                Is.EqualTo("strong"));
            Assert.That(compiledMarkdown.GetRules()[^1].GetSelector(),
                Is.Not.Null);
            Assert.That(compiledMarkdown.GetRules()[^1].GetAction(),
                Is.EqualTo(HtmlMarkdownRuleAction.Keep));
            Assert.That(compiledMarkdown.GetRules()[^1].Matches(strong, scope,
                CancellationToken.None), Is.True);
            Assert.That(compiledMarkdown.FindRule(strong, scope,
                CancellationToken.None), Is.Not.Null);
            Assert.That(compiledMarkdown.FindRule(emphasis, scope,
                CancellationToken.None), Is.Null);
            Assert.That(compiledMarkdown.GetLimits().GetMaximumVisits(),
                Is.EqualTo(3456));
            Assert.That(compiledArticle.Compile(), Is.SameAs(compiledArticle));
            Assert.That(compiledArticle.GetMinimumContentCharacters(),
                Is.EqualTo(100));
            Assert.That(compiledArticle.GetMinimumCandidateCharacters(),
                Is.EqualTo(25));
            Assert.That(compiledArticle.GetScoreThreshold(), Is.EqualTo(7));
            Assert.That(compiledArticle.GetMaximumTopCandidates(), Is.EqualTo(3));
            Assert.That(compiledArticle.GetRemoveUnlikelyCandidates(), Is.False);
            Assert.That(compiledArticle.GetWeightClassesAndIds(), Is.False);
            Assert.That(compiledArticle.GetCleanConditionally(), Is.False);
            Assert.That(compiledArticle.GetProduceSanitizedHtml(), Is.False);
            Assert.That(compiledArticle.GetProduceText(), Is.True);
            Assert.That(compiledArticle.GetProduceMarkdown(), Is.True);
            Assert.That(compiledArticle.GetSanitizerPolicy(), Is.Not.Null);
            Assert.That(compiledArticle.GetTextOptions().GetWrapColumn(),
                Is.EqualTo(72));
            Assert.That(compiledArticle.GetMarkdownOptions().GetHeadingStyle(),
                Is.EqualTo(HtmlMarkdownHeadingStyle.Setext));
            Assert.That(compiledArticle.GetLimits().GetMaximumVisits(),
                Is.EqualTo(3456));
        });
    }

}
