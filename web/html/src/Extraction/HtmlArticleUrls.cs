using System;
using System.Collections.Generic;
using System.Threading;

namespace html;

internal class HtmlArticleBaseUrlResult
{
    internal bool success;
    internal bool complete;
    internal string url;

    internal HtmlArticleBaseUrlResult(bool successValue,
        bool completeValue, string urlValue)
    {
        this.success = successValue;
        this.complete = completeValue;
        this.url = urlValue;
    }

    internal bool IsSuccess()
    {
        return this.success;
    }

    internal bool IsComplete()
    {
        return this.complete;
    }

    internal string GetUrl()
    {
        return this.url;
    }
}

internal class HtmlArticleUrlRewriteResult
{
    internal bool complete;
    internal int rewrittenAttributeCount;

    internal HtmlArticleUrlRewriteResult(bool completeValue,
        int rewrittenAttributeCountValue)
    {
        this.complete = completeValue;
        this.rewrittenAttributeCount = rewrittenAttributeCountValue;
    }

    internal bool IsComplete()
    {
        return this.complete;
    }

    internal int GetRewrittenAttributeCount()
    {
        return this.rewrittenAttributeCount;
    }
}

internal class HtmlArticleUrlRewriter
{
    internal HtmlLimits limits;
    internal HtmlBudget budget;
    internal CancellationToken cancellationToken;
    internal bool complete;
    internal int rewrittenAttributeCount;

    internal HtmlArticleUrlRewriter(HtmlArticleOptions options,
        HtmlBudget budgetValue, CancellationToken cancellationTokenValue)
    {
        this.limits = options.GetLimits();
        this.budget = budgetValue;
        this.cancellationToken = cancellationTokenValue;
        this.complete = true;
        this.rewrittenAttributeCount = 0;
    }

    internal HtmlArticleBaseUrlResult ResolveBaseUrl(HtmlDocument document)
    {
        HtmlNode? baseElement = this.FindBaseElement(document.GetRoot());
        if (!this.complete)
        {
            return new HtmlArticleBaseUrlResult(false, false, "");
        }

        string fallback = document.GetBaseUrlStorage();
        if (fallback.Length == 0)
        {
            fallback = document.GetUrlStorage();
        }

        if (baseElement != null)
        {
            string? rawBase = baseElement.GetAttribute("href");
            if (rawBase == null)
            {
                throw new InvalidOperationException(
                    "A selected base element must contain an href value.");
            }

            if (!this.TryWork(rawBase.Length))
            {
                return new HtmlArticleBaseUrlResult(false, false, "");
            }

            HtmlUrlResult candidate = fallback.Length == 0
                ? HtmlUrls.Normalize(rawBase, this.limits,
                    this.cancellationToken)
                : HtmlUrls.Resolve(rawBase, fallback, this.limits,
                    this.cancellationToken);
            if (candidate.WasLimitExceeded())
            {
                return new HtmlArticleBaseUrlResult(false, false, "");
            }

            if (candidate.IsSuccess()
                && candidate.GetScheme() != "data"
                && candidate.GetScheme() != "javascript")
            {
                return new HtmlArticleBaseUrlResult(true, true,
                    candidate.GetUrl());
            }
        }

        if (!this.TryWork(fallback.Length))
        {
            return new HtmlArticleBaseUrlResult(false, false, "");
        }

        HtmlUrlResult fallbackResult = HtmlUrls.Normalize(fallback,
            this.limits, this.cancellationToken);
        if (fallbackResult.WasLimitExceeded())
        {
            return new HtmlArticleBaseUrlResult(false, false, "");
        }

        return fallbackResult.IsSuccess()
            ? new HtmlArticleBaseUrlResult(true, true,
                fallbackResult.GetUrl())
            : new HtmlArticleBaseUrlResult(false, true, "");
    }

    internal HtmlArticleUrlRewriteResult Rewrite(HtmlNode root,
        HtmlArticleBaseUrlResult baseUrl)
    {
        if (!baseUrl.IsSuccess())
        {
            return new HtmlArticleUrlRewriteResult(true, 0);
        }

        List<HtmlArticleUrlTraversalEntry> pending =
            new List<HtmlArticleUrlTraversalEntry>();
        pending.Add(new HtmlArticleUrlTraversalEntry(root, 0));
        while (pending.Count > 0)
        {
            int lastIndex = pending.Count - 1;
            HtmlArticleUrlTraversalEntry entry = pending[lastIndex];
            pending.RemoveAt(lastIndex);
            if (!this.TryVisit(entry.GetDepth()))
            {
                return new HtmlArticleUrlRewriteResult(false,
                    this.rewrittenAttributeCount);
            }

            HtmlNode node = entry.GetNode();
            if (node.GetKind() == HtmlNodeKind.Element)
            {
                this.RewriteElement(node, baseUrl.GetUrl());
                if (!this.complete)
                {
                    return new HtmlArticleUrlRewriteResult(false,
                        this.rewrittenAttributeCount);
                }
            }

            HtmlNode[] children = GetTraversalChildren(node);
            int index = children.Length - 1;
            while (index >= 0)
            {
                pending.Add(new HtmlArticleUrlTraversalEntry(children[index],
                    entry.GetDepth() + 1));
                index = index - 1;
            }
        }

        return new HtmlArticleUrlRewriteResult(true,
            this.rewrittenAttributeCount);
    }

    internal HtmlNode? FindBaseElement(HtmlNode root)
    {
        List<HtmlArticleUrlTraversalEntry> pending =
            new List<HtmlArticleUrlTraversalEntry>();
        pending.Add(new HtmlArticleUrlTraversalEntry(root, 0));
        while (pending.Count > 0)
        {
            int lastIndex = pending.Count - 1;
            HtmlArticleUrlTraversalEntry entry = pending[lastIndex];
            pending.RemoveAt(lastIndex);
            if (!this.TryVisit(entry.GetDepth()))
            {
                return null;
            }

            HtmlNode node = entry.GetNode();
            if (node.GetKindStorage() == HtmlNodeKind.Element
                && node.GetNamespaceStorage() == HtmlNamespace.Html
                && node.GetLocalNameStorage() == "base"
                && node.HasAttribute("href"))
            {
                return node;
            }

            HtmlNode[] children = node.GetChildrenStorage();
            int index = children.Length - 1;
            while (index >= 0)
            {
                pending.Add(new HtmlArticleUrlTraversalEntry(children[index],
                    entry.GetDepth() + 1));
                index = index - 1;
            }
        }

        return null;
    }

    internal void RewriteElement(HtmlNode element, string baseUrl)
    {
        HtmlAttribute[] attributes = element.GetAttributesStorage();
        int index = 0;
        while (index < attributes.Length)
        {
            HtmlAttribute attribute = attributes[index];
            HtmlUrlAttributeKind kind = HtmlDomUrls.GetAttributeKind(element,
                attribute);
            string original = attribute.GetValue();
            string rewritten = original;
            if (!this.TryWork(original.Length + 1))
            {
                return;
            }

            if (kind == HtmlUrlAttributeKind.Single)
            {
                rewritten = this.RewriteSingleUrl(original, baseUrl);
            }
            else if (kind == HtmlUrlAttributeKind.SourceSet)
            {
                rewritten = this.RewriteSourceSet(original, baseUrl);
            }
            else if (kind == HtmlUrlAttributeKind.SpaceSeparated)
            {
                rewritten = this.RewriteSpaceSeparated(original, baseUrl);
            }

            if (!this.complete)
            {
                return;
            }

            if (rewritten != original)
            {
                element.SetAttribute(attribute.GetName(), rewritten);
                this.rewrittenAttributeCount =
                    this.rewrittenAttributeCount + 1;
            }

            index = index + 1;
        }
    }

    internal string RewriteSingleUrl(string value, string baseUrl)
    {
        HtmlUrlSchemeResult scheme = HtmlUrls.ExtractScheme(value,
            this.limits, this.cancellationToken);
        if (!scheme.IsComplete())
        {
            this.complete = false;
            return value;
        }

        if (scheme.HasScheme())
        {
            return value;
        }

        HtmlUrlResult resolved = HtmlUrls.Resolve(value, baseUrl,
            this.limits, this.cancellationToken);
        if (resolved.WasLimitExceeded())
        {
            this.complete = false;
            return value;
        }

        return resolved.IsSuccess() ? resolved.GetUrl() : value;
    }

    internal string RewriteSourceSet(string value, string baseUrl)
    {
        HtmlSrcsetResult parsed = HtmlSrcset.Parse(value, this.limits,
            this.cancellationToken);
        if (parsed.WasLimitExceeded())
        {
            this.complete = false;
            return value;
        }

        if (!parsed.IsValid())
        {
            return value;
        }

        HtmlSrcsetCandidate[] source = parsed.GetCandidates();
        HtmlSrcsetCandidate[] rewritten =
            new HtmlSrcsetCandidate[source.Length];
        bool changed = false;
        int index = 0;
        while (index < source.Length)
        {
            HtmlSrcsetCandidate candidate = source[index];
            string url = this.RewriteSingleUrl(candidate.GetUrl(), baseUrl);
            if (!this.complete)
            {
                return value;
            }

            rewritten[index] = CreateCandidate(candidate, url);
            changed = changed || url != candidate.GetUrl();
            index = index + 1;
        }

        return changed ? HtmlSrcset.Serialize(rewritten) : value;
    }

    internal string RewriteSpaceSeparated(string value, string baseUrl)
    {
        string[] source = HtmlTokenLists.ParseSpaceSeparated(value);
        if (source.Length == 0)
        {
            return value;
        }

        string[] rewritten = new string[source.Length];
        bool changed = false;
        int index = 0;
        while (index < source.Length)
        {
            rewritten[index] = this.RewriteSingleUrl(source[index], baseUrl);
            if (!this.complete)
            {
                return value;
            }

            changed = changed || rewritten[index] != source[index];
            index = index + 1;
        }

        return changed ? HtmlTokenLists.SerializeSpaceSeparated(rewritten)
            : value;
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

    internal static HtmlSrcsetCandidate CreateCandidate(
        HtmlSrcsetCandidate source, string url)
    {
        if (source.HasWidth())
        {
            return HtmlSrcsetCandidate.CreateWidth(url, source.GetWidth());
        }

        if (source.HasDensity())
        {
            return HtmlSrcsetCandidate.CreateDensity(url,
                source.GetDensity());
        }

        return HtmlSrcsetCandidate.Create(url);
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

internal class HtmlArticleUrlTraversalEntry
{
    internal HtmlNode node;
    internal int depth;

    internal HtmlArticleUrlTraversalEntry(HtmlNode nodeValue, int depthValue)
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
