using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace html;

internal class HtmlArticleCandidate
{
    internal HtmlNode node;
    internal double rawScore;
    internal double score;
    internal int documentOrder;
    internal long textCharacters;
    internal long linkCharacters;
    internal long punctuationCount;

    internal HtmlArticleCandidate(HtmlNode nodeValue, double rawScoreValue,
        int documentOrderValue)
    {
        this.node = nodeValue;
        this.rawScore = rawScoreValue;
        this.score = rawScoreValue;
        this.documentOrder = documentOrderValue;
        this.textCharacters = 0;
        this.linkCharacters = 0;
        this.punctuationCount = 0;
    }

    internal HtmlNode GetNode()
    {
        return this.node;
    }

    internal double GetRawScore()
    {
        return this.rawScore;
    }

    internal double GetScore()
    {
        return this.score;
    }

    internal int GetDocumentOrder()
    {
        return this.documentOrder;
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

    internal double GetLinkDensity()
    {
        return this.textCharacters <= 0 ? 0
            : (double)this.linkCharacters / (double)this.textCharacters;
    }

    internal void AddScore(double value)
    {
        this.rawScore = this.rawScore + value;
    }

    internal void SetMetrics(long textCharactersValue,
        long linkCharactersValue, long punctuationCountValue)
    {
        this.textCharacters = textCharactersValue;
        this.linkCharacters = linkCharactersValue;
        this.punctuationCount = punctuationCountValue;
    }

    internal void ApplyLinkDensity()
    {
        this.score = this.rawScore * (1.0 - this.GetLinkDensity());
    }
}

internal class HtmlArticleCandidateSelection
{
    internal bool success;
    internal bool complete;
    internal HtmlNode? candidate;
    internal HtmlNode[] siblings;
    internal HtmlArticleCandidate[] topCandidates;

    internal HtmlArticleCandidateSelection(bool successValue,
        bool completeValue, HtmlNode? candidateValue,
        HtmlNode[] siblingsValue,
        HtmlArticleCandidate[] topCandidatesValue)
    {
        this.success = successValue;
        this.complete = completeValue;
        this.candidate = candidateValue;
        this.siblings = CopyNodes(siblingsValue);
        this.topCandidates = CopyCandidates(topCandidatesValue);
    }

    internal bool IsSuccess()
    {
        return this.success;
    }

    internal bool IsComplete()
    {
        return this.complete;
    }

    internal HtmlNode? GetCandidate()
    {
        return this.candidate;
    }

    internal HtmlNode[] GetSiblings()
    {
        return CopyNodes(this.siblings);
    }

    internal HtmlArticleCandidate[] GetTopCandidates()
    {
        return CopyCandidates(this.topCandidates);
    }

    internal static HtmlNode[] CopyNodes(HtmlNode[] values)
    {
        HtmlNode[] result = new HtmlNode[values.Length];
        Array.Copy(values, result, values.Length);
        return result;
    }

    internal static HtmlArticleCandidate[] CopyCandidates(
        HtmlArticleCandidate[] values)
    {
        HtmlArticleCandidate[] result =
            new HtmlArticleCandidate[values.Length];
        Array.Copy(values, result, values.Length);
        return result;
    }
}

internal class HtmlArticleCandidateSelector
{
    internal static readonly string[] PositiveHints = new string[]
    {
        "article", "blog", "body", "content", "entry", "hentry", "main",
        "page", "post", "story", "text"
    };

    internal static readonly string[] NegativeHints = new string[]
    {
        "ad", "advert", "advertisement", "agegate", "banner", "breadcrumb",
        "comment", "community", "contact", "footer", "foot", "masthead",
        "media", "meta", "modal", "nav", "navbar", "outbrain", "pagination",
        "promo", "related", "remark", "rss", "share", "shopping", "sidebar",
        "sponsor"
    };

    internal HtmlArticleOptions options;
    internal HtmlBudget budget;
    internal List<HtmlArticleScoringFrame> frames;
    internal Dictionary<HtmlNode, HtmlArticleCandidate> candidates;
    internal bool complete;
    internal int nextDocumentOrder;

    internal HtmlArticleCandidateSelector(HtmlArticleOptions optionsValue,
        CancellationToken cancellationToken, HtmlBudget? budgetValue)
    {
        this.options = optionsValue.Copy();
        this.budget = budgetValue == null
            ? new HtmlBudget(this.options.GetLimits(), cancellationToken)
            : budgetValue;
        this.frames = new List<HtmlArticleScoringFrame>();
        this.candidates = new Dictionary<HtmlNode, HtmlArticleCandidate>();
        this.complete = true;
        this.nextDocumentOrder = 0;
    }

    internal HtmlArticleCandidateSelection Choose(HtmlNode root)
    {
        this.Traverse(root);
        if (!this.complete)
        {
            return this.CreateIncompleteSelection();
        }

        HtmlArticleCandidate[] ranked = this.RankCandidates();
        if (!this.complete)
        {
            return this.CreateIncompleteSelection();
        }

        int retainedCount = ranked.Length;
        if (retainedCount > this.options.GetMaximumTopCandidates())
        {
            retainedCount = this.options.GetMaximumTopCandidates();
        }

        HtmlArticleCandidate[] top =
            new HtmlArticleCandidate[retainedCount];
        Array.Copy(ranked, top, retainedCount);
        if (top.Length == 0
            || top[0].GetScore() < this.options.GetScoreThreshold())
        {
            return new HtmlArticleCandidateSelection(false, true, null,
                new HtmlNode[0], top);
        }

        HtmlNode[] siblings = this.AggregateSiblings(top[0]);
        if (!this.complete)
        {
            return this.CreateIncompleteSelection();
        }

        return new HtmlArticleCandidateSelection(true, true,
            top[0].GetNode(), siblings, top);
    }

    internal void Traverse(HtmlNode root)
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
                    return;
                }

                HtmlNodeKind kind = current.GetKind();
                if (IsContainer(kind))
                {
                    this.PushFrame(current);
                }
                else if (kind == HtmlNodeKind.Text)
                {
                    this.AddText(current.GetData());
                    if (!this.complete)
                    {
                        return;
                    }
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
                if (!this.complete)
                {
                    return;
                }
            }

            if (current == root)
            {
                return;
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
        bool linked = parentLinked || htmlElement && name == "a";
        this.frames.Add(new HtmlArticleScoringFrame(node, name, htmlElement,
            excluded, linked, this.nextDocumentOrder));
        this.nextDocumentOrder = this.nextDocumentOrder + 1;
    }

    internal void PopFrame()
    {
        int frameIndex = this.frames.Count - 1;
        HtmlArticleScoringFrame frame = this.frames[frameIndex];
        if (!frame.IsExcluded())
        {
            this.DiscoverFromFrame(frame, frameIndex);
            HtmlArticleCandidate? candidate;
            if (this.candidates.TryGetValue(frame.GetNode(), out candidate)
                && candidate != null)
            {
                candidate.SetMetrics(frame.GetTextCharacters(),
                    frame.GetLinkCharacters(),
                    frame.GetPunctuationCount());
            }
        }

        this.frames.RemoveAt(frameIndex);
        if (this.frames.Count == 0)
        {
            return;
        }

        HtmlArticleScoringFrame parent =
            this.frames[this.frames.Count - 1];
        if (!frame.IsExcluded() && !parent.IsExcluded())
        {
            parent.Add(frame.GetTextCharacters(),
                frame.GetLinkCharacters(), frame.GetPunctuationCount());
            if (frame.IsContentBlock()
                || frame.HasContentBlockDescendant())
            {
                parent.MarkContentBlockDescendant();
            }
        }
    }

    internal void DiscoverFromFrame(HtmlArticleScoringFrame frame, int frameIndex)
    {
        bool contentBlock = IsContentBlock(frame.GetName(),
            frame.IsHtmlElement());
        bool textOnlyDiv = frame.IsHtmlElement()
            && frame.GetName() == "div"
            && !frame.HasContentBlockDescendant();
        if (contentBlock || textOnlyDiv)
        {
            frame.MarkContentBlock();
            double contentScore = this.GetContentScore(frame);
            if (contentScore > 0)
            {
                this.AddScore(frameIndex, contentScore);
                this.AddScore(frameIndex - 1, contentScore);
                this.AddScore(frameIndex - 2, contentScore / 2.0);
            }
        }

        if (IsSemanticCandidate(frame.GetName(), frame.IsHtmlElement())
            && frame.GetTextCharacters()
                >= this.options.GetMinimumCandidateCharacters())
        {
            double semanticScore = 1.0 + Math.Min(
                (double)frame.GetTextCharacters() / 200.0, 3.0);
            this.AddScore(frameIndex, semanticScore);
        }
    }

    internal void AddScore(int frameIndex, double value)
    {
        if (frameIndex < 0 || value <= 0 || !this.complete)
        {
            return;
        }

        if (!this.budget.TryConsume(HtmlLimitKind.HasWork, 1))
        {
            this.complete = false;
            return;
        }

        HtmlArticleScoringFrame frame = this.frames[frameIndex];
        if (frame.IsExcluded() || !frame.IsHtmlElement())
        {
            return;
        }

        HtmlArticleCandidate? candidate;
        if (!this.candidates.TryGetValue(frame.GetNode(), out candidate)
            || candidate == null)
        {
            double initialScore = GetTagScore(frame.GetName());
            if (this.options.GetWeightClassesAndIds())
            {
                initialScore = initialScore
                    + this.GetClassAndIdWeight(frame.GetNode());
                if (!this.complete)
                {
                    return;
                }
            }

            candidate = new HtmlArticleCandidate(frame.GetNode(),
                initialScore, frame.GetDocumentOrder());
            this.candidates.Add(frame.GetNode(), candidate);
        }

        candidate.AddScore(value);
    }

    internal double GetContentScore(HtmlArticleScoringFrame frame)
    {
        long nonLinkCharacters = frame.GetTextCharacters()
            - frame.GetLinkCharacters();
        if (nonLinkCharacters
            < this.options.GetMinimumCandidateCharacters())
        {
            return 0;
        }

        double punctuation = Math.Min(
            (double)frame.GetPunctuationCount(), 10.0);
        double length = Math.Min(
            (double)nonLinkCharacters / 100.0, 3.0);
        return 1.0 + punctuation + length;
    }

    internal void AddText(string value)
    {
        if (this.frames.Count == 0)
        {
            return;
        }

        HtmlArticleScoringFrame frame =
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
            if (!this.budget.TryConsume(HtmlLimitKind.HasWork, 1))
            {
                this.complete = false;
                return;
            }

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

        frame.Add(characters,
            frame.IsInsideLink() ? characters : 0, punctuation);
    }

    internal HtmlArticleCandidate[] RankCandidates()
    {
        HtmlArticleCandidate[] values =
            new HtmlArticleCandidate[this.candidates.Count];
        this.candidates.Values.CopyTo(values, 0);
        int index = 0;
        while (index < values.Length)
        {
            if (!this.budget.TryConsume(HtmlLimitKind.HasWork, 1))
            {
                this.complete = false;
                return new HtmlArticleCandidate[0];
            }

            values[index].ApplyLinkDensity();
            index = index + 1;
        }

        Array.Sort(values, HtmlArticleCandidateComparer.Compare);
        return values;
    }

    internal HtmlNode[] AggregateSiblings(HtmlArticleCandidate top)
    {
        HtmlNode? parent = top.GetNode().GetParent();
        if (parent == null)
        {
            return new HtmlNode[] { top.GetNode() };
        }

        HtmlNode[] children = parent.GetChildElements();
        List<HtmlNode> result = new List<HtmlNode>();
        double threshold = Math.Max(10.0, top.GetScore() * 0.2);
        string? topClass = top.GetNode().GetAttribute("class");
        int index = 0;
        while (index < children.Length)
        {
            if (!this.budget.TryConsume(HtmlLimitKind.HasWork, 1))
            {
                this.complete = false;
                return new HtmlNode[0];
            }

            HtmlNode sibling = children[index];
            bool include = object.ReferenceEquals(sibling, top.GetNode());
            HtmlArticleCandidate? candidate;
            if (!include
                && this.candidates.TryGetValue(sibling, out candidate)
                && candidate != null)
            {
                double siblingScore = candidate.GetScore();
                string? siblingClass = sibling.GetAttribute("class");
                if (topClass != null && topClass.Length > 0
                    && siblingClass == topClass)
                {
                    siblingScore = siblingScore + top.GetScore() * 0.2;
                }

                include = siblingScore >= threshold
                    || IsSiblingParagraph(candidate);
            }

            if (include)
            {
                result.Add(sibling);
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    internal int GetClassAndIdWeight(HtmlNode node)
    {
        return this.GetHintWeight(node.GetAttribute("class"))
            + this.GetHintWeight(node.GetAttribute("id"));
    }

    internal int GetHintWeight(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        bool positive = false;
        bool negative = false;
        StringBuilder token = new StringBuilder();
        int index = 0;
        while (index <= value.Length)
        {
            if (!this.budget.TryConsume(HtmlLimitKind.HasWork, 1))
            {
                this.complete = false;
                return 0;
            }

            char current = index < value.Length ? value[index] : ' ';
            if (current >= 'A' && current <= 'Z'
                || current >= 'a' && current <= 'z'
                || current >= '0' && current <= '9')
            {
                token.Append(Ascii.ToLower(current));
            }
            else if (token.Length > 0)
            {
                string found = token.ToString();
                positive = positive || ContainsHint(PositiveHints, found);
                negative = negative || ContainsHint(NegativeHints, found);
                token.Clear();
            }

            index = index + 1;
        }

        int result = positive ? 25 : 0;
        return negative ? result - 25 : result;
    }

    internal HtmlArticleCandidateSelection CreateIncompleteSelection()
    {
        return new HtmlArticleCandidateSelection(false, false, null,
            new HtmlNode[0], new HtmlArticleCandidate[0]);
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
        return name == "script" || name == "style"
            || name == "noscript" || name == "template";
    }

    internal static bool IsContentBlock(string name, bool htmlElement)
    {
        return htmlElement && (name == "p" || name == "pre"
            || name == "td" || name == "blockquote");
    }

    internal static bool IsSemanticCandidate(string name, bool htmlElement)
    {
        return htmlElement && (name == "article" || name == "main"
            || name == "section");
    }

    internal static double GetTagScore(string name)
    {
        switch (name)
        {
            case "article":
            case "main":
                return 10;
            case "section":
            case "div":
                return 5;
            case "pre":
            case "td":
            case "blockquote":
                return 3;
            case "address":
            case "ol":
            case "ul":
            case "dl":
            case "dd":
            case "dt":
            case "li":
            case "form":
            case "nav":
            case "aside":
            case "header":
            case "footer":
                return -3;
            case "h1":
            case "h2":
            case "h3":
            case "h4":
            case "h5":
            case "h6":
            case "th":
                return -5;
            default:
                return 0;
        }
    }

    internal static bool IsSiblingParagraph(HtmlArticleCandidate candidate)
    {
        HtmlNode node = candidate.GetNode();
        if (node.GetNamespaceStorage() != HtmlNamespace.Html
            || node.GetLocalName() != "p")
        {
            return false;
        }

        double density = candidate.GetLinkDensity();
        return candidate.GetTextCharacters() >= 80 && density < 0.25
            || candidate.GetTextCharacters() >= 20
                && candidate.GetPunctuationCount() > 0
                && density == 0;
    }

    internal static bool ContainsHint(string[] values, string value)
    {
        int index = 0;
        while (index < values.Length)
        {
            string hint = values[index];
            if (hint == value || hint.Length >= 5
                && value.IndexOf(hint, StringComparison.Ordinal) >= 0)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
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

internal class HtmlArticleScoringFrame
{
    internal HtmlNode node;
    internal string name;
    internal bool htmlElement;
    internal bool excluded;
    internal bool insideLink;
    internal int documentOrder;
    internal long textCharacters;
    internal long linkCharacters;
    internal long punctuationCount;
    internal bool contentBlock;
    internal bool contentBlockDescendant;

    internal HtmlArticleScoringFrame(HtmlNode nodeValue, string nameValue,
        bool htmlElementValue, bool excludedValue, bool insideLinkValue,
        int documentOrderValue)
    {
        this.node = nodeValue;
        this.name = nameValue;
        this.htmlElement = htmlElementValue;
        this.excluded = excludedValue;
        this.insideLink = insideLinkValue;
        this.documentOrder = documentOrderValue;
        this.textCharacters = 0;
        this.linkCharacters = 0;
        this.punctuationCount = 0;
        this.contentBlock = false;
        this.contentBlockDescendant = false;
    }

    internal HtmlNode GetNode()
    {
        return this.node;
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

    internal int GetDocumentOrder()
    {
        return this.documentOrder;
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

    internal bool IsContentBlock()
    {
        return this.contentBlock;
    }

    internal bool HasContentBlockDescendant()
    {
        return this.contentBlockDescendant;
    }

    internal void MarkContentBlock()
    {
        this.contentBlock = true;
    }

    internal void MarkContentBlockDescendant()
    {
        this.contentBlockDescendant = true;
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

internal static class HtmlArticleCandidateComparer
{
    internal static int Compare(HtmlArticleCandidate left,
        HtmlArticleCandidate right)
    {
        if (object.ReferenceEquals(left, right))
        {
            return 0;
        }

        if (left == null)
        {
            return 1;
        }

        if (right == null)
        {
            return -1;
        }

        int result = right.GetScore().CompareTo(left.GetScore());
        if (result != 0)
        {
            return result;
        }

        return left.GetDocumentOrder().CompareTo(
            right.GetDocumentOrder());
    }
}
