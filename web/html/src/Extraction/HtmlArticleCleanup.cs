using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace html;

internal class HtmlArticleCleanupResult
{
    internal bool success;
    internal bool complete;
    internal HtmlNode? content;
    internal int removedNodeCount;
    internal int rewrittenUrlCount;

    internal HtmlArticleCleanupResult(bool successValue, bool completeValue,
        HtmlNode? contentValue, int removedNodeCountValue,
        int rewrittenUrlCountValue)
    {
        this.success = successValue;
        this.complete = completeValue;
        this.content = contentValue;
        this.removedNodeCount = removedNodeCountValue;
        this.rewrittenUrlCount = rewrittenUrlCountValue;
    }

    internal bool IsSuccess()
    {
        return this.success;
    }

    internal bool IsComplete()
    {
        return this.complete;
    }

    internal HtmlNode? GetContent()
    {
        return this.content;
    }

    internal int GetRemovedNodeCount()
    {
        return this.removedNodeCount;
    }

    internal int GetRewrittenUrlCount()
    {
        return this.rewrittenUrlCount;
    }
}

internal class HtmlArticleCleaner
{
    internal static readonly string[] PositiveHints = new string[]
    {
        "article", "blog", "body", "content", "entry", "hentry", "main",
        "page", "post", "story", "text"
    };

    internal static readonly string[] NegativeHints = new string[]
    {
        "advert", "advertisement", "agegate", "banner", "breadcrumb",
        "comment", "community", "contact", "footer", "foot", "header",
        "masthead", "media", "meta", "modal", "nav", "navbar", "outbrain",
        "pagination", "popup", "promo", "related", "remark", "rss", "share",
        "shopping", "sidebar", "sponsor", "subscription", "toolbar", "widget"
    };

    internal static readonly string[] RemovalHints = new string[]
    {
        " ad ", " ad-", " ad_", " ads ", " ads-", " ads_", "advert",
        " nav ", " nav-", " nav_", "navbar", "navigation", "outbrain",
        "promo", "sidebar", "sponsor"
    };

    internal HtmlArticleOptions options;
    internal HtmlBudget budget;
    internal CancellationToken cancellationToken;
    internal List<HtmlArticleCleanupEntry> entries;
    internal int removedNodeCount;
    internal int rewrittenUrlCount;
    internal bool complete;

    internal HtmlArticleCleaner(HtmlArticleOptions optionsValue,
        CancellationToken cancellationToken, HtmlBudget? budgetValue)
    {
        this.options = optionsValue.Copy();
        this.budget = budgetValue == null
            ? new HtmlBudget(this.options.GetLimits(), cancellationToken)
            : budgetValue;
        this.cancellationToken = cancellationToken;
        this.entries = new List<HtmlArticleCleanupEntry>();
        this.removedNodeCount = 0;
        this.rewrittenUrlCount = 0;
        this.complete = true;
    }

    internal HtmlArticleCleanupResult Clean(
        HtmlArticleCandidateSelection selection)
    {
        if (!selection.IsSuccess())
        {
            return new HtmlArticleCleanupResult(false,
                selection.IsComplete(), null, 0, 0);
        }

        HtmlNode? candidate = selection.GetCandidate();
        if (candidate == null)
        {
            throw new InvalidOperationException(
                "A successful candidate selection must contain a node.");
        }

        HtmlNode[] siblings = selection.GetSiblings();
        if (!this.CanClone(siblings))
        {
            return this.CreateIncompleteResult();
        }

        HtmlDocument? sourceDocument = candidate.GetOwnerDocument();
        if (sourceDocument == null)
        {
            throw new InvalidOperationException(
                "An article candidate must belong to a document.");
        }

        HtmlArticleUrlRewriter urlRewriter = new HtmlArticleUrlRewriter(
            this.options, this.budget, this.cancellationToken);
        HtmlArticleBaseUrlResult baseUrl =
            urlRewriter.ResolveBaseUrl(sourceDocument);
        if (!baseUrl.IsComplete())
        {
            return this.CreateIncompleteResult();
        }

        HtmlNode fragment = this.CloneSiblings(candidate, siblings, baseUrl);
        this.CollectEntries(fragment);
        if (!this.complete)
        {
            return this.CreateIncompleteResult();
        }

        this.RemoveStructuralNoise(fragment);
        this.RemoveDuplicateHeadings();
        this.RemoveEmptyAndLinkHeavyNodes(fragment);
        if (!this.complete)
        {
            return this.CreateIncompleteResult();
        }

        HtmlArticleUrlRewriteResult rewriteResult =
            urlRewriter.Rewrite(fragment, baseUrl);
        this.rewrittenUrlCount = rewriteResult.GetRewrittenAttributeCount();
        if (!rewriteResult.IsComplete())
        {
            return this.CreateIncompleteResult();
        }

        HtmlArticleCleanupMetrics metrics = this.MeasureFragment(fragment);
        if (!this.complete)
        {
            return this.CreateIncompleteResult();
        }

        bool hasContent = metrics.GetTextCharacters() > 0
            || metrics.GetMeaningfulElements() > 0;
        if (!hasContent)
        {
            return new HtmlArticleCleanupResult(false, true, null,
                this.removedNodeCount, this.rewrittenUrlCount);
        }

        return new HtmlArticleCleanupResult(true, true, fragment,
            this.removedNodeCount, this.rewrittenUrlCount);
    }

    internal bool CanClone(HtmlNode[] siblings)
    {
        int visits = 0;
        int index = siblings.Length - 1;
        List<HtmlArticleCloneEntry> pending =
            new List<HtmlArticleCloneEntry>();
        while (index >= 0)
        {
            pending.Add(new HtmlArticleCloneEntry(siblings[index], 0));
            index = index - 1;
        }

        HtmlLimits limits = this.options.GetLimits();
        while (pending.Count > 0)
        {
            this.budget.CheckCancellation();
            int lastIndex = pending.Count - 1;
            HtmlArticleCloneEntry entry = pending[lastIndex];
            pending.RemoveAt(lastIndex);
            visits = visits + 1;
            if (visits > limits.GetMaximumVisits()
                || entry.GetDepth() > limits.GetMaximumDepth())
            {
                this.complete = false;
                return false;
            }

            HtmlNode[] children = GetTraversalChildren(entry.GetNode());
            index = children.Length - 1;
            while (index >= 0)
            {
                pending.Add(new HtmlArticleCloneEntry(children[index],
                    entry.GetDepth() + 1));
                index = index - 1;
            }
        }

        return true;
    }

    internal HtmlNode CloneSiblings(HtmlNode candidate, HtmlNode[] siblings,
        HtmlArticleBaseUrlResult baseUrl)
    {
        HtmlDocument? source = candidate.GetOwnerDocument();
        if (source == null)
        {
            throw new InvalidOperationException(
                "An article candidate must belong to a document.");
        }

        string cloneBaseUrl = baseUrl.IsSuccess()
            ? baseUrl.GetUrl() : source.GetBaseUrlStorage();
        HtmlDocument cloneDocument = new HtmlDocument(
            source.GetUrlStorage(), cloneBaseUrl,
            source.GetModeStorage(), source.GetDiagnosticsStorage(),
            HtmlSourceSpan.CreateUnknown());
        HtmlNode fragment = cloneDocument.CreateFragment();
        int index = 0;
        while (index < siblings.Length)
        {
            fragment.Append(cloneDocument.Import(siblings[index], true));
            index = index + 1;
        }

        return fragment;
    }

    internal void CollectEntries(HtmlNode root)
    {
        List<HtmlArticleCloneEntry> pending =
            new List<HtmlArticleCloneEntry>();
        pending.Add(new HtmlArticleCloneEntry(root, 0));
        while (pending.Count > 0)
        {
            int lastIndex = pending.Count - 1;
            HtmlArticleCloneEntry pendingEntry = pending[lastIndex];
            pending.RemoveAt(lastIndex);
            if (!this.TryVisit(pendingEntry.GetDepth()))
            {
                this.complete = false;
                return;
            }

            this.entries.Add(new HtmlArticleCleanupEntry(
                pendingEntry.GetNode(), pendingEntry.GetDepth()));
            HtmlNode[] children = GetTraversalChildren(
                pendingEntry.GetNode());
            int index = children.Length - 1;
            while (index >= 0)
            {
                pending.Add(new HtmlArticleCloneEntry(children[index],
                    pendingEntry.GetDepth() + 1));
                index = index - 1;
            }
        }
    }

    internal void RemoveStructuralNoise(HtmlNode fragment)
    {
        int skippedDepth = -1;
        int index = 1;
        while (index < this.entries.Count)
        {
            HtmlArticleCleanupEntry entry = this.entries[index];
            if (skippedDepth >= 0 && entry.GetDepth() > skippedDepth)
            {
                entry.SetActive(false);
                index = index + 1;
                continue;
            }

            skippedDepth = -1;
            if (!this.TryWork(1))
            {
                return;
            }

            HtmlNode node = entry.GetNode();
            if (node.GetKind() == HtmlNodeKind.Comment)
            {
                this.Remove(entry);
                skippedDepth = entry.GetDepth();
            }
            else if (node.GetKind() == HtmlNodeKind.Element
                && this.ShouldRemoveStructurally(node, fragment))
            {
                this.Remove(entry);
                skippedDepth = entry.GetDepth();
            }

            index = index + 1;
        }
    }

    internal bool ShouldRemoveStructurally(HtmlNode node, HtmlNode fragment)
    {
        bool htmlElement = node.GetNamespaceStorage() == HtmlNamespace.Html;
        string name = htmlElement ? node.GetLocalName() : "";
        if (htmlElement && IsAlwaysRemovedElement(name))
        {
            return true;
        }

        if (node.HasAttribute("hidden") || node.HasAttribute("inert")
            || IsTrueAttribute(node.GetAttribute("aria-hidden")))
        {
            return true;
        }

        if (htmlElement && name == "input"
            && EqualsAsciiIgnoringCase(node.GetAttribute("type"), "hidden"))
        {
            return true;
        }

        if (this.HasHiddenStyle(node.GetAttribute("style")))
        {
            return true;
        }

        string? role = node.GetAttribute("role");
        if (role != null)
        {
            role = role.Trim();
        }
        if (EqualsAsciiIgnoringCase(role, "navigation")
            || EqualsAsciiIgnoringCase(role, "complementary"))
        {
            return true;
        }

        if (!this.options.GetRemoveUnlikelyCandidates()
            || node.GetParent() == fragment)
        {
            return false;
        }

        if (IsPreservedContentContext(node))
        {
            return false;
        }

        string hints = " " + node.GetAttribute("class") + " "
            + node.GetAttribute("id") + " ";
        return this.ContainsHint(hints, RemovalHints)
            || this.ContainsHint(hints, NegativeHints)
            && !this.ContainsHint(hints, PositiveHints);
    }

    internal void RemoveDuplicateHeadings()
    {
        Dictionary<string, bool> headings =
            new Dictionary<string, bool>(StringComparer.Ordinal);
        int index = 0;
        while (index < this.entries.Count)
        {
            HtmlArticleCleanupEntry entry = this.entries[index];
            HtmlNode node = entry.GetNode();
            if (entry.IsActive() && IsHeading(node))
            {
                string text = HtmlTextContent.Normalize(
                    node.GetTextContent()).Normalize(NormalizationForm.FormC);
                if (!this.TryWork(text.Length))
                {
                    return;
                }

                string key = FoldAscii(text);
                if (key.Length > 0 && headings.ContainsKey(key))
                {
                    this.Remove(entry);
                }
                else if (key.Length > 0)
                {
                    headings.Add(key, true);
                }
            }

            index = index + 1;
        }
    }

    internal void RemoveEmptyAndLinkHeavyNodes(HtmlNode fragment)
    {
        Dictionary<HtmlNode, HtmlArticleCleanupMetrics> metrics =
            new Dictionary<HtmlNode, HtmlArticleCleanupMetrics>();
        int index = this.entries.Count - 1;
        while (index >= 0)
        {
            HtmlArticleCleanupEntry entry = this.entries[index];
            if (entry.IsActive())
            {
                if (!this.TryWork(1))
                {
                    return;
                }

                HtmlNode node = entry.GetNode();
                HtmlArticleCleanupMetrics measured =
                    this.MeasureNode(node, metrics);
                metrics.Add(node, measured);
                if (node != fragment && node.GetKind() == HtmlNodeKind.Element
                    && this.ShouldRemoveByContent(node, measured))
                {
                    this.Remove(entry);
                }
            }

            index = index - 1;
        }
    }

    internal HtmlArticleCleanupMetrics MeasureNode(HtmlNode node,
        Dictionary<HtmlNode, HtmlArticleCleanupMetrics> metrics)
    {
        HtmlArticleCleanupMetrics result =
            new HtmlArticleCleanupMetrics();
        if (node.GetKind() == HtmlNodeKind.Text)
        {
            result.AddText(this.CountNormalizedCharacters(node.GetData()));
            return result;
        }

        HtmlNode[] children = GetTraversalChildren(node);
        int index = 0;
        while (index < children.Length)
        {
            if (metrics.ContainsKey(children[index]))
            {
                result.Add(metrics[children[index]]);
            }

            index = index + 1;
        }

        if (node.GetKind() == HtmlNodeKind.Element)
        {
            if (IsLink(node))
            {
                result.SetLinkCharacters(result.GetTextCharacters());
            }

            if (IsMeaningfulElement(node))
            {
                result.AddMeaningfulElement();
            }
        }

        return result;
    }

    internal bool ShouldRemoveByContent(HtmlNode node,
        HtmlArticleCleanupMetrics metrics)
    {
        if (IsMeaningfulElement(node))
        {
            return false;
        }

        if (metrics.GetTextCharacters() == 0
            && metrics.GetMeaningfulElements() == 0)
        {
            return true;
        }

        if (!this.options.GetCleanConditionally()
            || !IsConditionalElement(node))
        {
            return false;
        }

        long text = metrics.GetTextCharacters();
        long linked = metrics.GetLinkCharacters();
        if (text <= 0 || linked <= 0)
        {
            return false;
        }

        double density = (double)linked / (double)text;
        long nonLinkText = text - linked;
        return density > 0.8
            || density > 0.5
                && nonLinkText
                    < this.options.GetMinimumCandidateCharacters();
    }

    internal HtmlArticleCleanupMetrics MeasureFragment(HtmlNode fragment)
    {
        HtmlArticleCleanupMetrics result =
            new HtmlArticleCleanupMetrics();
        List<HtmlNode> pending = new List<HtmlNode>();
        pending.Add(fragment);
        while (pending.Count > 0)
        {
            this.budget.CheckCancellation();
            int lastIndex = pending.Count - 1;
            HtmlNode current = pending[lastIndex];
            pending.RemoveAt(lastIndex);
            if (current.GetKind() == HtmlNodeKind.Text)
            {
                result.AddText(this.CountNormalizedCharacters(
                    current.GetData()));
            }
            else if (current.GetKind() == HtmlNodeKind.Element
                && IsMeaningfulElement(current))
            {
                result.AddMeaningfulElement();
            }

            HtmlNode[] children = GetTraversalChildren(current);
            int index = children.Length - 1;
            while (index >= 0)
            {
                pending.Add(children[index]);
                index = index - 1;
            }
        }

        return result;
    }

    internal int CountNormalizedCharacters(string value)
    {
        int characters = 0;
        bool pendingSpace = false;
        int index = 0;
        while (index < value.Length)
        {
            if (!this.TryWork(1))
            {
                return 0;
            }

            char current = value[index];
            if (char.IsWhiteSpace(current))
            {
                pendingSpace = characters > 0;
            }
            else
            {
                if (pendingSpace)
                {
                    characters = characters + 1;
                    pendingSpace = false;
                }

                characters = characters + 1;
            }

            index = index + 1;
        }

        return characters;
    }

    internal bool HasHiddenStyle(string? style)
    {
        if (style == null)
        {
            return false;
        }

        int start = 0;
        while (start < style.Length)
        {
            int end = style.IndexOf(';', start);
            if (end < 0)
            {
                end = style.Length;
            }

            int colon = style.IndexOf(':', start, end - start);
            if (colon >= 0)
            {
                string property = style.Substring(start,
                    colon - start).Trim();
                string value = style.Substring(colon + 1,
                    end - colon - 1).Trim();
                if (!this.TryWork(property.Length + value.Length))
                {
                    return false;
                }

                if (EqualsAsciiIgnoringCase(property, "display")
                    && StartsWithCssKeyword(value, "none")
                    || EqualsAsciiIgnoringCase(property, "visibility")
                        && (StartsWithCssKeyword(value, "hidden")
                            || StartsWithCssKeyword(value, "collapse"))
                    || EqualsAsciiIgnoringCase(property,
                            "content-visibility")
                        && StartsWithCssKeyword(value, "hidden")
                    || EqualsAsciiIgnoringCase(property, "opacity")
                        && IsZeroCssValue(value))
                {
                    return true;
                }
            }

            start = end + 1;
        }

        return false;
    }

    internal bool ContainsHint(string value, string[] hints)
    {
        if (!this.TryWork(value.Length))
        {
            return false;
        }

        int index = 0;
        while (index < hints.Length)
        {
            if (value.IndexOf(hints[index],
                StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void Remove(HtmlArticleCleanupEntry entry)
    {
        HtmlNode node = entry.GetNode();
        if (node.GetParent() != null)
        {
            node.Remove();
            this.removedNodeCount = this.removedNodeCount + 1;
        }

        entry.SetActive(false);
    }

    internal bool TryVisit(int depth)
    {
        if (!this.budget.Allows(HtmlLimitKind.Depth, depth)
            || !this.budget.TryConsume(HtmlLimitKind.Visits, 1))
        {
            return false;
        }

        return true;
    }

    internal bool TryWork(int amount)
    {
        if (this.complete
            && this.budget.TryConsume(HtmlLimitKind.HasWork, amount))
        {
            return true;
        }

        this.complete = false;
        return false;
    }

    internal HtmlArticleCleanupResult CreateIncompleteResult()
    {
        return new HtmlArticleCleanupResult(false, false, null,
            this.removedNodeCount, this.rewrittenUrlCount);
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

    internal static bool IsAlwaysRemovedElement(string name)
    {
        return name == "script" || name == "style" || name == "noscript"
            || name == "template" || name == "nav" || name == "aside"
            || name == "form" || name == "button" || name == "input"
            || name == "select" || name == "textarea" || name == "option"
            || name == "menu" || name == "dialog" || name == "meta"
            || name == "link" || name == "base" || name == "title";
    }

    internal static bool IsHeading(HtmlNode node)
    {
        if (node.GetKind() != HtmlNodeKind.Element
            || node.GetNamespaceStorage() != HtmlNamespace.Html)
        {
            return false;
        }

        string name = node.GetLocalName();
        return name == "h1" || name == "h2" || name == "h3"
            || name == "h4" || name == "h5" || name == "h6";
    }

    internal static bool IsLink(HtmlNode node)
    {
        return node.GetKind() == HtmlNodeKind.Element
            && node.GetNamespaceStorage() == HtmlNamespace.Html
            && node.GetLocalName() == "a";
    }

    internal static bool IsMeaningfulElement(HtmlNode node)
    {
        if (node.GetKind() != HtmlNodeKind.Element)
        {
            return false;
        }

        if (node.GetNamespaceStorage() != HtmlNamespace.Html)
        {
            return true;
        }

        string name = node.GetLocalName();
        return name == "img" || name == "picture" || name == "video"
            || name == "audio" || name == "source" || name == "track"
            || name == "iframe" || name == "embed" || name == "object"
            || name == "canvas" || name == "svg" || name == "math"
            || name == "br" || name == "hr";
    }

    internal static bool IsPreservedContentContext(HtmlNode node)
    {
        HtmlNode? current = node;
        while (current != null)
        {
            if (IsPreservedContentRoot(current))
            {
                return true;
            }

            current = current.GetParent();
        }

        return false;
    }

    internal static bool IsPreservedContentRoot(HtmlNode node)
    {
        if (node.GetKind() != HtmlNodeKind.Element)
        {
            return false;
        }

        HtmlNamespace namespaceKind = node.GetNamespaceStorage();
        if (namespaceKind == HtmlNamespace.MathMl
            || namespaceKind == HtmlNamespace.Svg)
        {
            return true;
        }

        if (namespaceKind != HtmlNamespace.Html)
        {
            return false;
        }

        string name = node.GetLocalName();
        return name == "figure" || name == "figcaption"
            || name == "table" || name == "caption" || name == "thead"
            || name == "tbody" || name == "tfoot" || name == "tr"
            || name == "th" || name == "td" || name == "pre"
            || name == "code" || name == "kbd" || name == "samp"
            || name == "picture" || name == "img" || name == "video"
            || name == "audio" || name == "source" || name == "track"
            || name == "iframe" || name == "embed" || name == "object"
            || name == "canvas";
    }

    internal static bool IsConditionalElement(HtmlNode node)
    {
        if (node.GetNamespaceStorage() != HtmlNamespace.Html)
        {
            return false;
        }

        string name = node.GetLocalName();
        return name == "div" || name == "section" || name == "ul"
            || name == "ol" || name == "dl";
    }

    internal static bool IsTrueAttribute(string? value)
    {
        return value != null
            && EqualsAsciiIgnoringCase(value.Trim(), "true");
    }

    internal static bool StartsWithCssKeyword(string value, string keyword)
    {
        if (!value.StartsWith(keyword,
            StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return value.Length == keyword.Length
            || char.IsWhiteSpace(value[keyword.Length])
            || value[keyword.Length] == '!';
    }

    internal static bool IsZeroCssValue(string value)
    {
        int important = value.IndexOf('!');
        if (important >= 0)
        {
            value = value.Substring(0, important).Trim();
        }

        if (value.Length == 0)
        {
            return false;
        }

        bool foundZero = false;
        bool foundDecimalPoint = false;
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current == '0')
            {
                foundZero = true;
            }
            else if (current == '.' && !foundDecimalPoint)
            {
                foundDecimalPoint = true;
            }
            else
            {
                return false;
            }

            index = index + 1;
        }

        return foundZero;
    }

    internal static bool EqualsAsciiIgnoringCase(string? left, string right)
    {
        return string.Equals(left, right,
            StringComparison.OrdinalIgnoreCase);
    }

    internal static string FoldAscii(string value)
    {
        StringBuilder builder = new StringBuilder(value.Length);
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (current >= 'A' && current <= 'Z')
            {
                current = (char)(current + ('a' - 'A'));
            }

            builder.Append(current);
            index = index + 1;
        }

        return builder.ToString();
    }
}

internal class HtmlArticleCloneEntry
{
    internal HtmlNode node;
    internal int depth;

    internal HtmlArticleCloneEntry(HtmlNode nodeValue, int depthValue)
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

internal class HtmlArticleCleanupEntry
{
    internal HtmlNode node;
    internal int depth;
    internal bool active;

    internal HtmlArticleCleanupEntry(HtmlNode nodeValue, int depthValue)
    {
        this.node = nodeValue;
        this.depth = depthValue;
        this.active = true;
    }

    internal HtmlNode GetNode()
    {
        return this.node;
    }

    internal int GetDepth()
    {
        return this.depth;
    }

    internal bool IsActive()
    {
        return this.active;
    }

    internal void SetActive(bool value)
    {
        this.active = value;
    }
}

internal class HtmlArticleCleanupMetrics
{
    internal long textCharacters;
    internal long linkCharacters;
    internal int meaningfulElements;

    internal HtmlArticleCleanupMetrics()
    {
        this.textCharacters = 0;
        this.linkCharacters = 0;
        this.meaningfulElements = 0;
    }

    internal long GetTextCharacters()
    {
        return this.textCharacters;
    }

    internal long GetLinkCharacters()
    {
        return this.linkCharacters;
    }

    internal int GetMeaningfulElements()
    {
        return this.meaningfulElements;
    }

    internal void AddText(long value)
    {
        this.textCharacters = this.textCharacters + value;
    }

    internal void SetLinkCharacters(long value)
    {
        this.linkCharacters = value;
    }

    internal void AddMeaningfulElement()
    {
        this.meaningfulElements = this.meaningfulElements + 1;
    }

    internal void Add(HtmlArticleCleanupMetrics value)
    {
        this.textCharacters = this.textCharacters
            + value.textCharacters;
        this.linkCharacters = this.linkCharacters
            + value.linkCharacters;
        this.meaningfulElements = this.meaningfulElements
            + value.meaningfulElements;
    }
}
