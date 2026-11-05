using System;
using System.Collections.Generic;
using System.Threading;

namespace html;

internal class HtmlArticleExtractionContext
{
    internal HtmlArticleOptions options;
    internal CancellationToken cancellationToken;
    internal HtmlBudget budget;
    internal bool shareBudget;
    internal HtmlDiagnosticCollector diagnostics;
    internal long upstreamDroppedDiagnostics;

    internal HtmlArticleExtractionContext(HtmlArticleOptions optionsValue,
        CancellationToken cancellationTokenValue, HtmlBudget? budgetValue)
    {
        this.options = optionsValue.Copy();
        this.cancellationToken = cancellationTokenValue;
        this.shareBudget = budgetValue != null;
        this.budget = budgetValue == null
            ? new HtmlBudget(this.options.GetLimits(),
                cancellationTokenValue)
            : budgetValue;
        this.diagnostics = new HtmlDiagnosticCollector(this.budget);
        this.upstreamDroppedDiagnostics = 0;
    }

    internal HtmlArticleResult Extract(HtmlDocument document,
        bool inputSuccess, bool inputComplete, bool inputTruncated,
        HtmlMetadata? suppliedMetadata)
    {
        this.cancellationToken.ThrowIfCancellationRequested();
        HtmlDiagnostic[] documentDiagnostics = document.GetDiagnostics();
        this.AddDiagnostics(documentDiagnostics);
        bool documentIncomplete = HasIncompleteDiagnostic(
            documentDiagnostics);
        if (!inputSuccess)
        {
            this.AddFailureDiagnostic(
                "Article extraction did not run because HTML parsing failed.");
            return this.CreateResult(null, false,
                inputComplete && !documentIncomplete,
                inputTruncated || !inputComplete || documentIncomplete);
        }

        HtmlMetadata? metadata = suppliedMetadata;
        if (metadata == null)
        {
            metadata = HtmlMetadataExtractor.Extract(document,
                this.options.GetLimits(), this.cancellationToken,
                this.shareBudget ? this.budget : null);
        }
        HtmlDiagnostic[] metadataDiagnostics = metadata.GetDiagnostics();
        this.AddDiagnostics(metadataDiagnostics);
        this.upstreamDroppedDiagnostics = AddSaturated(
            this.upstreamDroppedDiagnostics,
            metadata.GetDroppedDiagnosticCount());
        bool metadataIncomplete = metadata.WereCandidatesTruncated()
            || metadata.WereDiagnosticsTruncated()
            || HasIncompleteDiagnostic(metadataDiagnostics);

        HtmlArticleCandidateSelection selection =
            HtmlArticleExtractor.SelectCandidate(document, this.options,
                this.cancellationToken,
                this.shareBudget ? this.budget : null);
        if (!selection.IsComplete())
        {
            this.AddLimitDiagnostic(
                "Article candidate selection exceeded its resource limits.");
            return this.CreateResult(null, false, false, true);
        }

        if (!selection.IsSuccess())
        {
            this.AddFailureDiagnostic(
                "No readable article candidate met the configured thresholds.");
            bool complete = inputComplete && !documentIncomplete
                && !metadataIncomplete;
            return this.CreateResult(null, false, complete, !complete);
        }

        HtmlArticleCleanupResult cleanup =
            HtmlArticleExtractor.CleanCandidate(selection, this.options,
                this.cancellationToken,
                this.shareBudget ? this.budget : null);
        if (!cleanup.IsComplete())
        {
            this.AddLimitDiagnostic(
                "Article cleanup exceeded its resource limits.");
            return this.CreateResult(null, false, false, true);
        }

        if (!cleanup.IsSuccess())
        {
            this.AddFailureDiagnostic(
                "The selected article contained no content after cleanup.");
            bool complete = inputComplete && !documentIncomplete
                && !metadataIncomplete;
            return this.CreateResult(null, false, complete, !complete);
        }

        HtmlArticleAssembler assembler = new HtmlArticleAssembler(
            metadata, this.budget, this.options.GetLimits(),
            this.cancellationToken);
        HtmlNode? cleanedContent = cleanup.GetContent();
        if (cleanedContent == null)
        {
            throw new InvalidOperationException(
                "Successful article cleanup must contain content.");
        }

        HtmlArticleAssemblyResult assembly =
            assembler.Assemble(cleanedContent);
        if (!assembly.IsComplete())
        {
            this.AddLimitDiagnostic(
                "Article result assembly exceeded its resource limits.");
            return this.CreateResult(null, false, false, true);
        }

        HtmlArticle? assembledArticle = assembly.GetArticle();
        if (assembledArticle == null)
        {
            throw new InvalidOperationException(
                "Successful article assembly must contain an article.");
        }

        HtmlArticleOutputResult output = this.ProduceOutputs(
            assembledArticle);
        if (!output.IsSuccess())
        {
            return this.CreateResult(null, false, false,
                output.WasTruncated());
        }

        bool resultComplete = inputComplete && !inputTruncated
            && !documentIncomplete && !metadataIncomplete;
        resultComplete = resultComplete && output.IsComplete();
        return this.CreateResult(output.GetArticle(), true,
            resultComplete, !resultComplete || output.WasTruncated());
    }

    internal HtmlArticleOutputResult ProduceOutputs(HtmlArticle article)
    {
        if (!this.options.GetProduceSanitizedHtml()
            && !this.options.GetProduceText()
            && !this.options.GetProduceMarkdown())
        {
            return new HtmlArticleOutputResult(true, true, false, article);
        }

        HtmlLimits limits = this.options.GetLimits();
        HtmlCompiledSanitizerPolicy policy =
            this.options.GetSanitizerPolicy();
        HtmlSanitizeResult sanitized;
        if (this.options.GetProduceSanitizedHtml())
        {
            sanitized = HtmlSanitizer.Sanitize(article.GetContent(), policy,
                limits, this.cancellationToken);
        }
        else
        {
            HtmlBudget outputBudget = new HtmlBudget(limits,
                this.cancellationToken);
            sanitized = HtmlSanitizer.SanitizeForPipeline(
                article.GetContent(), policy, limits,
                this.cancellationToken, outputBudget);
        }

        this.AddOutputDiagnostics(sanitized.GetDiagnostics(),
            sanitized.GetDroppedDiagnosticCount());
        if (!sanitized.IsSuccess())
        {
            if (sanitized.WasTruncated()
                || sanitized.GetReport().WasLimitExceeded())
            {
                this.AddLimitDiagnostic(
                    "Article output sanitization exceeded its resource limits.");
            }
            else
            {
                this.AddFailureDiagnostic(
                    "Article output sanitization did not complete.");
            }

            return new HtmlArticleOutputResult(false, false,
                sanitized.WasTruncated(), null);
        }

        HtmlNode? content = sanitized.GetNode();
        if (content == null)
        {
            throw new InvalidOperationException(
                "Successful article sanitization must contain a node.");
        }

        bool complete = !sanitized.WasTruncated();
        bool truncated = sanitized.WasTruncated();
        string? html = this.options.GetProduceSanitizedHtml()
            ? sanitized.GetHtml() : null;
        string? text = null;
        if (this.options.GetProduceText())
        {
            HtmlTextResult textResult = HtmlTextConverter.Convert(content,
                this.options.GetTextOptions(), this.cancellationToken);
            this.AddOutputDiagnostics(textResult.GetDiagnostics(),
                textResult.GetDroppedDiagnosticCount());
            if (!textResult.IsSuccess())
            {
                this.AddFailureDiagnostic(
                    "Article plain-text conversion did not complete.");
                return new HtmlArticleOutputResult(false, false,
                    textResult.WasTruncated(), null);
            }

            text = textResult.GetText();
            complete = complete && textResult.IsComplete()
                && !textResult.WasTruncated();
            truncated = truncated || textResult.WasTruncated();
        }

        string? markdown = null;
        if (this.options.GetProduceMarkdown())
        {
            HtmlMarkdownResult markdownResult =
                HtmlMarkdownConverter.Convert(content,
                    this.options.GetMarkdownOptions(),
                    this.cancellationToken);
            this.AddOutputDiagnostics(markdownResult.GetDiagnostics(),
                markdownResult.GetDroppedDiagnosticCount());
            if (!markdownResult.IsSuccess())
            {
                this.AddFailureDiagnostic(
                    "Article Markdown conversion did not complete.");
                return new HtmlArticleOutputResult(false, false,
                    markdownResult.WasTruncated(), null);
            }

            markdown = markdownResult.GetMarkdown();
            complete = complete && markdownResult.IsComplete()
                && !markdownResult.WasTruncated();
            truncated = truncated || markdownResult.WasTruncated();
        }

        HtmlArticle processed = article.CreateProcessed(content, html,
            text, markdown);
        return new HtmlArticleOutputResult(true, complete, truncated,
            processed);
    }

    internal void AddOutputDiagnostics(HtmlDiagnostic[] values,
        long droppedCount)
    {
        this.AddDiagnostics(values);
        this.upstreamDroppedDiagnostics = AddSaturated(
            this.upstreamDroppedDiagnostics, droppedCount);
    }

    internal void AddDiagnostics(HtmlDiagnostic[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            this.diagnostics.Add(values[index]);
            index = index + 1;
        }
    }

    internal void AddFailureDiagnostic(string message)
    {
        this.diagnostics.AddDetails(HtmlDiagnosticCode.ArticleExtractionFailed,
            HtmlDiagnosticSeverity.Information, message,
            HtmlSourceSpan.CreateUnknown());
    }

    internal void AddLimitDiagnostic(string message)
    {
        this.diagnostics.AddDetails(HtmlDiagnosticCode.ResourceLimitExceeded,
            HtmlDiagnosticSeverity.Warning, message,
            HtmlSourceSpan.CreateUnknown());
    }

    internal HtmlArticleResult CreateResult(HtmlArticle? article, bool success,
        bool complete, bool truncated)
    {
        if (this.diagnostics.WasTruncated()
            || this.upstreamDroppedDiagnostics > 0)
        {
            complete = false;
            truncated = true;
        }

        long diagnosticCount = AddSaturated(
            this.diagnostics.GetTotalCount(),
            this.upstreamDroppedDiagnostics);
        long droppedCount = AddSaturated(
            this.diagnostics.GetDroppedCount(),
            this.upstreamDroppedDiagnostics);
        return new HtmlArticleResult(success, complete, truncated, article,
            this.diagnostics.GetDiagnostics(), diagnosticCount,
            droppedCount);
    }

    internal static bool HasIncompleteDiagnostic(HtmlDiagnostic[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            HtmlDiagnosticCode code = values[index].GetCode();
            if (code == HtmlDiagnosticCode.InputLimitExceeded
                || code == HtmlDiagnosticCode.TreeBuilderDepthExceeded
                || code == HtmlDiagnosticCode.ResourceLimitExceeded
                || code == HtmlDiagnosticCode.OperationIncomplete)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static long AddSaturated(long left, long right)
    {
        return right > long.MaxValue - left
            ? long.MaxValue : left + right;
    }
}

internal class HtmlArticleOutputResult
{
    internal bool success;
    internal bool complete;
    internal bool truncated;
    internal HtmlArticle? article;

    internal HtmlArticleOutputResult(bool successValue, bool completeValue,
        bool truncatedValue, HtmlArticle? articleValue)
    {
        this.success = successValue;
        this.complete = completeValue;
        this.truncated = truncatedValue;
        this.article = articleValue;
    }

    internal bool IsSuccess()
    {
        return this.success;
    }

    internal bool IsComplete()
    {
        return this.complete;
    }

    internal bool WasTruncated()
    {
        return this.truncated;
    }

    internal HtmlArticle? GetArticle()
    {
        return this.article;
    }
}

internal class HtmlArticleAssemblyResult
{
    internal bool complete;
    internal HtmlArticle? article;

    internal HtmlArticleAssemblyResult(bool completeValue,
        HtmlArticle? articleValue)
    {
        this.complete = completeValue;
        this.article = articleValue;
    }

    internal bool IsComplete()
    {
        return this.complete;
    }

    internal HtmlArticle? GetArticle()
    {
        return this.article;
    }
}

internal class HtmlArticleAssembler
{
    internal HtmlMetadata metadata;
    internal HtmlBudget budget;
    internal HtmlLimits limits;
    internal CancellationToken cancellationToken;
    internal bool complete;

    internal HtmlArticleAssembler(HtmlMetadata metadataValue,
        HtmlBudget budgetValue, HtmlLimits limitsValue,
        CancellationToken cancellationTokenValue)
    {
        this.metadata = metadataValue;
        this.budget = budgetValue;
        this.limits = limitsValue.Copy();
        this.cancellationToken = cancellationTokenValue;
        this.complete = true;
    }

    internal HtmlArticleAssemblyResult Assemble(HtmlNode content)
    {
        HtmlArticleAssemblyScan scan = new HtmlArticleAssemblyScan(
            this.budget, this.limits, this.cancellationToken);
        scan.Scan(content);
        if (!scan.IsComplete())
        {
            return new HtmlArticleAssemblyResult(false, null);
        }

        string? title = this.metadata.GetTitle();
        if (title == null)
        {
            title = this.GetNodeText(scan.GetHeading());
        }

        string? byline = this.metadata.GetAuthor();
        if (byline == null)
        {
            byline = this.GetNodeText(scan.GetByline());
        }

        string? excerpt = this.metadata.GetDescription();
        if (excerpt == null)
        {
            excerpt = this.GetNodeText(scan.GetParagraph());
        }

        string? language = this.metadata.GetLanguage();
        if (language == null && scan.GetLanguage() != null)
        {
            language = HtmlMetadataResolver.NormalizeLanguageValue(
                scan.GetLanguage());
        }

        string? direction = this.metadata.GetDirection();
        if (direction == null)
        {
            direction = NormalizeDirection(scan.GetDirection());
        }

        string? publishedTime = this.metadata.GetPublishedTime();
        if (publishedTime == null)
        {
            publishedTime = this.GetTimeText(scan.GetTime());
        }

        if (!this.complete)
        {
            return new HtmlArticleAssemblyResult(false, null);
        }

        string? imageUrl = this.metadata.GetImageUrl();
        if (imageUrl == null)
        {
            imageUrl = scan.GetImageUrl();
        }

        HtmlArticle? article = new HtmlArticle(title, byline, excerpt,
            this.metadata.GetSiteName(), language, direction, publishedTime,
            this.metadata.GetCanonicalUrl(), imageUrl, content, null, null,
            null, scan.GetLength());
        return new HtmlArticleAssemblyResult(true, article);
    }

    internal string? GetTimeText(HtmlNode? node)
    {
        if (node == null)
        {
            return null;
        }

        string? value = node.GetAttribute("datetime");
        if (string.IsNullOrWhiteSpace(value))
        {
            return this.GetNodeText(node);
        }

        return this.NormalizeValue(value);
    }

    internal string? GetNodeText(HtmlNode? node)
    {
        if (node == null)
        {
            return null;
        }

        return this.NormalizeValue(node.GetTextContent());
    }

    internal string? NormalizeValue(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (!this.budget.TryConsume(HtmlLimitKind.HasWork, 1))
            {
                this.complete = false;
                return null;
            }

            index = index + 1;
        }

        string normalized = HtmlMetadataResolver.NormalizeTextValue(value);
        return normalized.Length == 0 ? null : normalized;
    }

    internal static string? NormalizeDirection(string? value)
    {
        if (value == null)
        {
            return null;
        }

        string normalized = value.Trim();
        if (Ascii.EqualsIgnoreCase(normalized.AsSpan(), "ltr".AsSpan()))
        {
            return "ltr";
        }

        if (Ascii.EqualsIgnoreCase(normalized.AsSpan(), "rtl".AsSpan()))
        {
            return "rtl";
        }

        if (Ascii.EqualsIgnoreCase(normalized.AsSpan(), "auto".AsSpan()))
        {
            return "auto";
        }

        return null;
    }
}

internal class HtmlArticleAssemblyScan
{
    internal HtmlBudget budget;
    internal HtmlLimits limits;
    internal CancellationToken cancellationToken;
    internal bool complete;
    internal HtmlNode? heading;
    internal HtmlNode? byline;
    internal HtmlNode? paragraph;
    internal HtmlNode? time;
    internal string? language;
    internal string? direction;
    internal string? imageUrl;
    internal int length;
    internal bool pendingSpace;

    internal HtmlArticleAssemblyScan(HtmlBudget budgetValue,
        HtmlLimits limitsValue,
        CancellationToken cancellationTokenValue)
    {
        this.budget = budgetValue;
        this.limits = limitsValue.Copy();
        this.cancellationToken = cancellationTokenValue;
        this.complete = true;
        this.heading = null;
        this.byline = null;
        this.paragraph = null;
        this.time = null;
        this.language = null;
        this.direction = null;
        this.imageUrl = null;
        this.length = 0;
        this.pendingSpace = false;
    }

    internal void Scan(HtmlNode root)
    {
        List<HtmlArticleAssemblyEntry> pending =
            new List<HtmlArticleAssemblyEntry>();
        pending.Add(new HtmlArticleAssemblyEntry(root, 0));
        while (pending.Count > 0)
        {
            int lastIndex = pending.Count - 1;
            HtmlArticleAssemblyEntry entry = pending[lastIndex];
            pending.RemoveAt(lastIndex);
            if (!this.TryVisit(entry.GetDepth()))
            {
                return;
            }

            HtmlNode node = entry.GetNode();
            if (node.GetKind() == HtmlNodeKind.Text)
            {
                this.AddText(node.GetData());
                if (!this.complete)
                {
                    return;
                }
            }
            else if (node.GetKind() == HtmlNodeKind.Element)
            {
                this.InspectElement(node);
                if (!this.complete)
                {
                    return;
                }
            }

            HtmlNode[] children = GetTraversalChildren(node);
            int index = children.Length - 1;
            while (index >= 0)
            {
                pending.Add(new HtmlArticleAssemblyEntry(children[index],
                    entry.GetDepth() + 1));
                index = index - 1;
            }
        }
    }

    internal bool IsComplete()
    {
        return this.complete;
    }

    internal HtmlNode? GetHeading()
    {
        return this.heading;
    }

    internal HtmlNode? GetByline()
    {
        return this.byline;
    }

    internal HtmlNode? GetParagraph()
    {
        return this.paragraph;
    }

    internal HtmlNode? GetTime()
    {
        return this.time;
    }

    internal string? GetLanguage()
    {
        return this.language;
    }

    internal string? GetDirection()
    {
        return this.direction;
    }

    internal string? GetImageUrl()
    {
        return this.imageUrl;
    }

    internal int GetLength()
    {
        return this.length;
    }

    internal void InspectElement(HtmlNode element)
    {
        if (this.language == null)
        {
            string? foundLanguage = element.GetAttribute("lang");
            if (string.IsNullOrWhiteSpace(foundLanguage))
            {
                foundLanguage = element.GetAttributeInNamespace(
                    HtmlNamespaceData.GetUri(HtmlNamespace.Xml), "lang");
            }

            if (!string.IsNullOrWhiteSpace(foundLanguage))
            {
                this.language = foundLanguage;
            }
        }

        if (this.direction == null)
        {
            string? foundDirection = element.GetAttribute("dir");
            if (IsDirection(foundDirection))
            {
                this.direction = foundDirection;
            }
        }

        if (element.GetNamespaceStorage() != HtmlNamespace.Html)
        {
            return;
        }

        string name = element.GetLocalName();
        bool bylineElement = IsBylineElement(element);
        if (this.heading == null && IsHeading(name))
        {
            this.heading = element;
        }

        if (this.byline == null && bylineElement)
        {
            this.byline = element;
        }

        if (this.paragraph == null && name == "p" && !bylineElement)
        {
            this.paragraph = element;
        }

        if (this.time == null && name == "time")
        {
            this.time = element;
        }

        if (this.imageUrl == null && name == "img")
        {
            this.imageUrl = this.FindImageUrl(element);
        }
    }

    internal void AddText(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (!this.budget.TryConsume(HtmlLimitKind.HasWork, 1))
            {
                this.complete = false;
                return;
            }

            char current = value[index];
            if (UnicodeTools.IsUnicodeWhitespace(current))
            {
                this.pendingSpace = this.length > 0;
            }
            else
            {
                if (this.pendingSpace && this.length < int.MaxValue)
                {
                    this.length = this.length + 1;
                }

                this.pendingSpace = false;
                if (this.length < int.MaxValue)
                {
                    this.length = this.length + 1;
                }
            }

            index = index + 1;
        }
    }

    internal bool TryVisit(int depth)
    {
        if (!this.budget.Allows(HtmlLimitKind.Depth, depth)
            || !this.budget.TryConsume(HtmlLimitKind.Visits, 1))
        {
            this.complete = false;
            return false;
        }

        return true;
    }

    internal static bool IsBylineElement(HtmlNode element)
    {
        string? relation = element.GetAttribute("rel");
        string? itemProperty = element.GetAttribute("itemprop");
        string hints = element.GetAttribute("class") + " "
            + element.GetAttribute("id");
        return ContainsToken(relation, "author")
            || ContainsToken(itemProperty, "author")
            || ContainsHint(hints, "author")
            || ContainsHint(hints, "byline");
    }

    internal static bool IsHeading(string name)
    {
        return name == "h1" || name == "h2" || name == "h3"
            || name == "h4" || name == "h5" || name == "h6";
    }

    internal string? FindImageUrl(HtmlNode image)
    {
        string? source = image.GetAttribute("src");
        if (!string.IsNullOrWhiteSpace(source))
        {
            return source;
        }

        string? sourceSet = image.GetAttribute("srcset");
        if (string.IsNullOrWhiteSpace(sourceSet))
        {
            return null;
        }

        if (!this.budget.TryConsume(HtmlLimitKind.HasWork,
            sourceSet.Length))
        {
            this.complete = false;
            return null;
        }

        HtmlSrcsetResult parsed = HtmlSrcset.Parse(sourceSet, this.limits,
            this.cancellationToken);
        if (parsed.WasLimitExceeded())
        {
            this.complete = false;
            return null;
        }

        HtmlSrcsetCandidate[] candidates = parsed.GetCandidates();
        return parsed.IsValid() && candidates.Length > 0
            ? candidates[0].GetUrl() : null;
    }

    internal static bool IsDirection(string? value)
    {
        if (value == null)
        {
            return false;
        }

        string normalized = value.Trim();
        return Ascii.EqualsIgnoreCase(normalized.AsSpan(), "ltr".AsSpan())
            || Ascii.EqualsIgnoreCase(normalized.AsSpan(), "rtl".AsSpan())
            || Ascii.EqualsIgnoreCase(normalized.AsSpan(), "auto".AsSpan());
    }

    internal static bool ContainsToken(string? value, string expected)
    {
        if (value == null)
        {
            return false;
        }

        string[] tokens = HtmlTokenLists.ParseSpaceSeparated(value);
        return HtmlTokenLists.ContainsAsciiInsensitive(tokens, expected);
    }

    internal static bool ContainsHint(string value, string expected)
    {
        int start = 0;
        int index = 0;
        while (index <= value.Length)
        {
            char current = index < value.Length ? value[index] : ' ';
            bool tokenCharacter = current >= 'A' && current <= 'Z'
                || current >= 'a' && current <= 'z'
                || current >= '0' && current <= '9';
            if (!tokenCharacter)
            {
                int length = index - start;
                if (length == expected.Length
                    && Ascii.EqualsIgnoreCase(
                        value.AsSpan(start, length), expected.AsSpan()))
                {
                    return true;
                }

                start = index + 1;
            }

            index = index + 1;
        }

        return false;
    }

    internal static HtmlNode[] GetTraversalChildren(HtmlNode node)
    {
        HtmlNodeKind kind = node.GetKindStorage();
        if (kind != HtmlNodeKind.Document
            && kind != HtmlNodeKind.Fragment
            && kind != HtmlNodeKind.Element)
        {
            return new HtmlNode[0];
        }

        return HtmlTreeMutation.GetMutationTarget(node).GetChildrenStorage();
    }
}

internal class HtmlArticleAssemblyEntry
{
    internal HtmlNode node;
    internal int depth;

    internal HtmlArticleAssemblyEntry(HtmlNode nodeValue, int depthValue)
    {
        this.node = nodeValue;
        this.depth = depthValue;
    }

    internal HtmlNode GetNode()
    {
        return this.node;
    }

    internal int GetDepth()
    {
        return this.depth;
    }
}
