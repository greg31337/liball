using System;
using System.Collections.Generic;
using System.Threading;

namespace html;

/// <summary>Extracts HTML article data from an already parsed HTML document.</summary>
internal static class HtmlArticleExtractor
{
    /// <summary>Extracts an article without sanitizing its content.</summary>
    /// <remarks>Use <see cref="HtmlProcessor"/> for renderable content.</remarks>
    /// <param name="document">The parsed HTML document to inspect or process.</param>
    /// <param name="options">The settings copied for use by this operation.</param>
    /// <returns>The extracted article, completeness status, and diagnostics.</returns>
    public static HtmlArticleResult Extract(HtmlDocument document,
        HtmlArticleOptions options)
    {
        return Extract(document, options, CancellationToken.None);
    }

    /// <summary>Extracts an article without sanitizing its content.</summary>
    /// <remarks>Use <see cref="HtmlProcessor"/> for renderable content.</remarks>
    /// <param name="document">The parsed HTML document to inspect or process.</param>
    /// <param name="options">The settings copied for use by this operation.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The extracted article, completeness status, and diagnostics.</returns>
    public static HtmlArticleResult Extract(HtmlDocument document,
        HtmlArticleOptions options, CancellationToken cancellationToken)
    {
        return ExtractParsed(document, options, cancellationToken,
            true, true, false);
    }

    internal static HtmlArticleResult ExtractParsed(HtmlDocument document,
        HtmlArticleOptions options, CancellationToken cancellationToken,
        bool inputSuccess, bool inputComplete, bool inputTruncated)
    {
        return ExtractParsed(document, options, cancellationToken,
            inputSuccess, inputComplete, inputTruncated, null, null);
    }

    internal static HtmlArticleResult ExtractParsed(HtmlDocument document,
        HtmlArticleOptions options, CancellationToken cancellationToken,
        bool inputSuccess, bool inputComplete, bool inputTruncated,
        HtmlMetadata? metadata, HtmlBudget? budget)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        options.Validate();
        HtmlArticleExtractionContext context =
            new HtmlArticleExtractionContext(options, cancellationToken,
                budget);
        return context.Extract(document, inputSuccess, inputComplete,
            inputTruncated, metadata);
    }

    /// <summary>
    /// Performs a fast, conservative preflight for readable article content.
    /// </summary>
    /// <remarks>
    /// This heuristic can report false positives for comment threads,
    /// product listings, and repeated snippets placed in semantic article
    /// elements. It can report false negatives for short articles, poetry,
    /// image-led stories, and content stored mainly in custom or unusual
    /// elements. Call article extraction for a definitive extraction attempt.
    /// </remarks>
    /// <param name="document">The parsed HTML document to inspect or process.</param>
    /// <param name="options">The settings copied for use by this operation.</param>
    /// <returns><c>true</c> when the document passes the readability heuristic; otherwise, <c>false</c>.</returns>
    public static bool IsProbablyReadable(HtmlDocument document,
        HtmlArticleOptions options)
    {
        return IsProbablyReadable(document, options,
            CancellationToken.None);
    }

    internal static bool IsProbablyReadable(HtmlDocument document,
        HtmlArticleOptions options, CancellationToken cancellationToken)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        options.Validate();
        HtmlReadabilityPreflightContext context =
            new HtmlReadabilityPreflightContext(options,
                cancellationToken);
        return context.Evaluate(document.GetRoot());
    }

    internal static HtmlArticleCandidateSelection SelectCandidate(
        HtmlDocument document, HtmlArticleOptions options,
        CancellationToken cancellationToken)
    {
        return SelectCandidate(document, options, cancellationToken, null);
    }

    internal static HtmlArticleCandidateSelection SelectCandidate(
        HtmlDocument document, HtmlArticleOptions options,
        CancellationToken cancellationToken, HtmlBudget? budget)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        options.Validate();
        HtmlArticleCandidateSelector selector =
            new HtmlArticleCandidateSelector(options, cancellationToken,
                budget);
        return selector.Choose(document.GetRoot());
    }

    internal static HtmlArticleCleanupResult CleanCandidate(
        HtmlArticleCandidateSelection selection,
        HtmlArticleOptions options,
        CancellationToken cancellationToken)
    {
        return CleanCandidate(selection, options, cancellationToken, null);
    }

    internal static HtmlArticleCleanupResult CleanCandidate(
        HtmlArticleCandidateSelection selection,
        HtmlArticleOptions options,
        CancellationToken cancellationToken, HtmlBudget? budget)
    {
        if (selection == null)
        {
            throw new ArgumentNullException("selection");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        options.Validate();
        HtmlArticleCleaner cleaner = new HtmlArticleCleaner(options,
            cancellationToken, budget);
        return cleaner.Clean(selection);
    }
}

internal class HtmlReadabilityPreflightContext
{
    internal HtmlArticleOptions options;
    internal HtmlBudget budget;
    internal List<HtmlReadabilityFrame> frames;
    internal double score;
    internal long contentCharacters;
    internal bool complete;

    internal HtmlReadabilityPreflightContext(HtmlArticleOptions optionsValue,
        CancellationToken cancellationToken)
    {
        this.options = optionsValue.Copy();
        this.budget = new HtmlBudget(this.options.GetLimits(),
            cancellationToken);
        this.frames = new List<HtmlReadabilityFrame>();
        this.score = 0;
        this.contentCharacters = 0;
        this.complete = true;
    }

    internal bool Evaluate(HtmlNode root)
    {
        HtmlNode? current = root;
        int depth = 0;
        bool entering = true;
        while (current != null)
        {
            if (entering)
            {
                if (!this.TryVisit(depth))
                {
                    this.complete = false;
                    break;
                }

                HtmlNodeKind kind = current.GetKind();
                if (IsContainer(kind))
                {
                    this.PushFrame(current);
                }
                else if (kind == HtmlNodeKind.Text)
                {
                    this.AddText(current.GetData());
                }

                HtmlNode? child = current.GetFirstChild();
                if (child != null)
                {
                    current = child;
                    depth = depth + 1;
                    entering = true;
                    continue;
                }
            }

            if (IsContainer(current.GetKind()))
            {
                this.PopFrame();
            }

            if (current == root)
            {
                current = null;
                continue;
            }

            HtmlNode? sibling = current.GetNextSibling();
            if (sibling != null)
            {
                current = sibling;
                entering = true;
            }
            else
            {
                current = current.GetParent();
                depth = depth - 1;
                entering = false;
            }
        }

        if (!this.complete || this.contentCharacters <= 0)
        {
            return false;
        }

        return this.contentCharacters
                >= this.options.GetMinimumContentCharacters()
            && this.score >= this.options.GetScoreThreshold();
    }

    internal void PushFrame(HtmlNode node)
    {
        bool parentExcluded = this.frames.Count > 0
            && this.frames[this.frames.Count - 1].IsExcluded();
        bool parentLinked = this.frames.Count > 0
            && this.frames[this.frames.Count - 1].IsInsideLink();
        bool htmlElement = node.GetKind() == HtmlNodeKind.Element
            && node.GetNamespaceStorage() == HtmlNamespace.Html;
        string name = htmlElement ? node.GetLocalName() : "";
        bool excluded = parentExcluded
            || htmlElement && IsExcludedElement(name);
        bool insideLink = parentLinked || htmlElement && name == "a";
        this.frames.Add(new HtmlReadabilityFrame(name, htmlElement,
            excluded, insideLink));
    }

    internal void PopFrame()
    {
        int lastIndex = this.frames.Count - 1;
        HtmlReadabilityFrame frame = this.frames[lastIndex];
        this.frames.RemoveAt(lastIndex);
        if (!frame.IsExcluded())
        {
            this.ScoreFrame(frame);
        }

        if (this.frames.Count == 0)
        {
            this.contentCharacters = frame.IsExcluded() ? 0
                : frame.GetTextCharacters() - frame.GetLinkCharacters();
            return;
        }

        HtmlReadabilityFrame parent =
            this.frames[this.frames.Count - 1];
        if (!frame.IsExcluded() && !parent.IsExcluded())
        {
            parent.Add(frame.GetTextCharacters(),
                frame.GetLinkCharacters(), frame.GetPunctuationCount());
        }
    }

    internal void AddText(string value)
    {
        if (this.frames.Count == 0)
        {
            return;
        }

        HtmlReadabilityFrame frame =
            this.frames[this.frames.Count - 1];
        if (frame.IsExcluded())
        {
            return;
        }

        long characters = 0;
        long punctuation = 0;
        bool pendingSpace = false;
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (UnicodeTools.IsUnicodeWhitespace(current))
            {
                if (characters > 0)
                {
                    pendingSpace = true;
                }
            }
            else
            {
                if (pendingSpace)
                {
                    characters = AddSaturated(characters, 1);
                    pendingSpace = false;
                }

                characters = AddSaturated(characters, 1);
                if (IsContentPunctuation(current))
                {
                    punctuation = AddSaturated(punctuation, 1);
                }
            }

            index = index + 1;
        }

        long linkCharacters = frame.IsInsideLink() ? characters : 0;
        frame.Add(characters, linkCharacters, punctuation);
    }

    internal void ScoreFrame(HtmlReadabilityFrame frame)
    {
        double weight = GetCandidateWeight(frame.GetName(),
            frame.IsHtmlElement());
        if (weight <= 0 || frame.GetTextCharacters() <= 0)
        {
            return;
        }

        long nonLinkCharacters = frame.GetTextCharacters()
            - frame.GetLinkCharacters();
        if (nonLinkCharacters
            < this.options.GetMinimumCandidateCharacters())
        {
            return;
        }

        double linkDensity = (double)frame.GetLinkCharacters()
            / (double)frame.GetTextCharacters();
        if (linkDensity >= 0.8)
        {
            return;
        }

        double lengthScore = Math.Sqrt((double)nonLinkCharacters
            - this.options.GetMinimumCandidateCharacters() + 1.0);
        double punctuationScore = Math.Min(
            (double)frame.GetPunctuationCount(), 12.0) * 0.35;
        double candidateScore = (lengthScore + punctuationScore)
            * weight * (1.0 - linkDensity);
        if (frame.GetName() == "article" || frame.GetName() == "main")
        {
            candidateScore = candidateScore + 5.0;
        }

        this.score = this.score + candidateScore;
    }

    internal bool TryVisit(int depth)
    {
        return this.budget.Allows(HtmlLimitKind.Depth, depth)
            && this.budget.TryConsume(HtmlLimitKind.Visits, 1);
    }

    internal static bool IsContainer(HtmlNodeKind kind)
    {
        return kind == HtmlNodeKind.Document
            || kind == HtmlNodeKind.Fragment
            || kind == HtmlNodeKind.Element;
    }

    internal static bool IsExcludedElement(string name)
    {
        switch (name)
        {
            case "script":
            case "style":
            case "noscript":
            case "template":
            case "nav":
            case "aside":
            case "header":
            case "footer":
            case "form":
            case "button":
            case "select":
            case "textarea":
                return true;
            default:
                return false;
        }
    }

    internal static double GetCandidateWeight(string name, bool htmlElement)
    {
        if (!htmlElement)
        {
            return 0;
        }

        switch (name)
        {
            case "article":
                return 1.5;
            case "main":
                return 1.3;
            case "p":
                return 1.0;
            case "blockquote":
                return 0.9;
            case "pre":
                return 0.8;
            case "section":
                return 0.6;
            case "td":
                return 0.5;
            default:
                return 0;
        }
    }

    internal static bool IsContentPunctuation(char value)
    {
        switch (value)
        {
            case ',':
            case '.':
            case ';':
            case ':':
            case '!':
            case '?':
            case '\u060C':
            case '\u061B':
            case '\u061F':
            case '\u3001':
            case '\u3002':
            case '\uFF01':
            case '\uFF0C':
            case '\uFF1F':
                return true;
            default:
                return false;
        }
    }

    internal static long AddSaturated(long value, long amount)
    {
        return value > long.MaxValue - amount
            ? long.MaxValue : value + amount;
    }
}

internal class HtmlReadabilityFrame
{
    internal string name;
    internal bool htmlElement;
    internal bool excluded;
    internal bool insideLink;
    internal long textCharacters;
    internal long linkCharacters;
    internal long punctuationCount;

    internal HtmlReadabilityFrame(string nameValue, bool htmlElementValue,
        bool excludedValue, bool insideLinkValue)
    {
        this.name = nameValue;
        this.htmlElement = htmlElementValue;
        this.excluded = excludedValue;
        this.insideLink = insideLinkValue;
        this.textCharacters = 0;
        this.linkCharacters = 0;
        this.punctuationCount = 0;
    }

    internal string GetName()
    {
        return this.name;
    }

    internal bool IsHtmlElement()
    {
        return this.htmlElement;
    }

    internal bool IsExcluded()
    {
        return this.excluded;
    }

    internal bool IsInsideLink()
    {
        return this.insideLink;
    }

    internal long GetTextCharacters()
    {
        return this.textCharacters;
    }

    internal long GetLinkCharacters()
    {
        return this.linkCharacters;
    }

    internal long GetPunctuationCount()
    {
        return this.punctuationCount;
    }

    internal void Add(long textCharactersValue, long linkCharactersValue,
        long punctuationCountValue)
    {
        this.textCharacters = AddSaturated(this.textCharacters,
            textCharactersValue);
        this.linkCharacters = AddSaturated(this.linkCharacters,
            linkCharactersValue);
        this.punctuationCount = AddSaturated(this.punctuationCount,
            punctuationCountValue);
    }

    internal static long AddSaturated(long value, long amount)
    {
        return value > long.MaxValue - amount
            ? long.MaxValue : value + amount;
    }
}
