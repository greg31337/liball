using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace odf;

internal class TextSearchHit
{
    internal OdfSearchMatch? Match;
    internal OdfTextBlock? Block;
    internal OdfTextRun? Run;
    internal int Offset;
    internal int Length;
    internal string Replacement = "";
}

internal static class TextSearchModel
{
    internal static OdfSearchMatch[] SearchLiteral(
        OdfDocument document,
        string value,
        bool matchCase)
    {
        return MatchesFromHits(FindLiteralHits(document, value, matchCase));
    }

    internal static OdfSearchMatch[] SearchRegularExpression(
        OdfDocument document,
        string pattern,
        bool matchCase)
    {
        return MatchesFromHits(
            FindRegularExpressionHits(
                document,
                pattern,
                matchCase,
                null));
    }

    internal static OdfSearchMatch[] SearchStyle(
        OdfDocument document,
        string styleName)
    {
        return MatchesFromHits(FindStyleHits(document, styleName));
    }

    internal static OdfSearchMatch[] SearchElement(
        OdfDocument document,
        OdfQualifiedName name)
    {
        return MatchesFromHits(FindElementHits(document, name));
    }

    internal static OdfEditResult ReplaceLiteral(
        OdfDocument document,
        string value,
        string replacement,
        bool matchCase)
    {
        RequireReplacement(replacement);
        List<TextSearchHit> hits = FindLiteralHits(document, value, matchCase);
        int index = 0;
        while (index < hits.Count)
        {
            hits[index].Replacement = replacement;
            index = index + 1;
        }
        return ApplyReplacements(document, hits);
    }

    internal static OdfEditResult ReplaceRegularExpression(
        OdfDocument document,
        string pattern,
        string replacement,
        bool matchCase)
    {
        RequireReplacement(replacement);
        return ApplyReplacements(
            document,
            FindRegularExpressionHits(
                document,
                pattern,
                matchCase,
                replacement));
    }

    internal static OdfEditResult ReplaceStyle(
        OdfDocument document,
        string styleName,
        string replacement)
    {
        RequireReplacement(replacement);
        List<TextSearchHit> hits = FindStyleHits(document, styleName);
        SetReplacementValues(hits, replacement);
        return ApplyReplacements(document, hits);
    }

    internal static OdfEditResult ReplaceElement(
        OdfDocument document,
        OdfQualifiedName name,
        string replacement)
    {
        RequireReplacement(replacement);
        List<TextSearchHit> hits = FindElementHits(document, name);
        SetReplacementValues(hits, replacement);
        return ApplyReplacements(document, hits);
    }

    static List<TextSearchHit> FindLiteralHits(
        OdfDocument document,
        string value,
        bool matchCase)
    {
        RequireDocument(document);
        if (value == null || value.Length == 0)
        {
            throw new OdfEditException(
                "A literal text query cannot be empty.");
        }
        StringComparison comparison = matchCase
            ? StringComparison.Ordinal
            : StringComparison.OrdinalIgnoreCase;
        List<TextSearchHit> hits = new List<TextSearchHit>();
        int blockIndex = 0;
        while (blockIndex < document.TextBlocks.Length)
        {
            OdfTextBlock block = document.TextBlocks[blockIndex];
            int offset = 0;
            while (offset <= block.Text.Length - value.Length)
            {
                int found = block.Text.IndexOf(value, offset, comparison);
                if (found < 0)
                {
                    offset = block.Text.Length + 1;
                }
                else
                {
                    string matched = block.Text.Substring(found, value.Length);
                    hits.Add(CreateHit(
                        block,
                        null,
                        block.Node,
                        found,
                        value.Length,
                        matched,
                        matched == value));
                    offset = found + value.Length;
                }
            }
            blockIndex = blockIndex + 1;
        }
        return hits;
    }

    static List<TextSearchHit> FindRegularExpressionHits(
        OdfDocument document,
        string pattern,
        bool matchCase,
        string? replacement)
    {
        RequireDocument(document);
        if (pattern == null || pattern.Length == 0)
        {
            throw new OdfEditException(
                "A regular-expression query cannot be empty.");
        }
        RegexOptions options = RegexOptions.CultureInvariant;
        if (!matchCase)
        {
            options = options | RegexOptions.IgnoreCase;
        }
        List<TextSearchHit> hits = new List<TextSearchHit>();
        int blockIndex = 0;
        while (blockIndex < document.TextBlocks.Length)
        {
            OdfTextBlock block = document.TextBlocks[blockIndex];
            MatchCollection matches = Regex.Matches(
                block.Text,
                pattern,
                options,
                TimeSpan.FromSeconds(2));
            foreach (Match match in matches)
            {
                TextSearchHit hit = CreateHit(
                    block,
                    null,
                    block.Node,
                    match.Index,
                    match.Length,
                    match.Value,
                    false);
                if (replacement != null)
                {
                    hit.Replacement = match.Result(replacement);
                }
                hits.Add(hit);
            }
            blockIndex = blockIndex + 1;
        }
        return hits;
    }

    static List<TextSearchHit> FindStyleHits(
        OdfDocument document,
        string styleName)
    {
        RequireDocument(document);
        if (styleName == null || styleName.Length == 0)
        {
            throw new OdfEditException("A style-name query cannot be empty.");
        }
        List<TextSearchHit> hits = new List<TextSearchHit>();
        int blockIndex = 0;
        while (blockIndex < document.TextBlocks.Length)
        {
            OdfTextBlock block = document.TextBlocks[blockIndex];
            if (block.StyleName == styleName)
            {
                hits.Add(CreateHit(
                    block,
                    null,
                    block.Node,
                    0,
                    block.Text.Length,
                    block.Text,
                    true));
            }
            AddStyleRunHits(block.Runs, block, 0, styleName, hits);
            blockIndex = blockIndex + 1;
        }
        return hits;
    }

    static int AddStyleRunHits(
        OdfTextRun[] runs,
        OdfTextBlock block,
        int baseOffset,
        string styleName,
        List<TextSearchHit> hits)
    {
        int offset = baseOffset;
        int index = 0;
        while (index < runs.Length)
        {
            OdfTextRun run = runs[index];
            if (run.StyleName == styleName)
            {
                hits.Add(CreateHit(
                    block,
                    run,
                    run.Node,
                    offset,
                    run.Text.Length,
                    run.Text,
                    true));
            }
            if (run.Runs.Length > 0)
            {
                AddStyleRunHits(
                    run.Runs,
                    block,
                    offset,
                    styleName,
                    hits);
            }
            offset = offset + run.Text.Length;
            index = index + 1;
        }
        return offset;
    }

    static List<TextSearchHit> FindElementHits(
        OdfDocument document,
        OdfQualifiedName name)
    {
        RequireDocument(document);
        if (name == null || !OdfNameRules.IsQualifiedNameValue(name))
        {
            throw new OdfEditException(
                "A valid qualified element name is required.");
        }
        List<TextSearchHit> hits = new List<TextSearchHit>();
        int blockIndex = 0;
        while (blockIndex < document.TextBlocks.Length)
        {
            OdfTextBlock block = document.TextBlocks[blockIndex];
            if (NamesMatch(block.Node, name))
            {
                hits.Add(CreateHit(
                    block,
                    null,
                    block.Node,
                    0,
                    block.Text.Length,
                    block.Text,
                    true));
            }
            AddElementRunHits(block.Runs, block, 0, name, hits);
            blockIndex = blockIndex + 1;
        }
        return hits;
    }

    static int AddElementRunHits(
        OdfTextRun[] runs,
        OdfTextBlock block,
        int baseOffset,
        OdfQualifiedName name,
        List<TextSearchHit> hits)
    {
        int offset = baseOffset;
        int index = 0;
        while (index < runs.Length)
        {
            OdfTextRun run = runs[index];
            if (NamesMatch(run.Node, name))
            {
                hits.Add(CreateHit(
                    block,
                    run,
                    run.Node,
                    offset,
                    run.Text.Length,
                    run.Text,
                    true));
            }
            if (run.Runs.Length > 0)
            {
                AddElementRunHits(run.Runs, block, offset, name, hits);
            }
            offset = offset + run.Text.Length;
            index = index + 1;
        }
        return offset;
    }

    static bool NamesMatch(OdfNode? node, OdfQualifiedName name)
    {
        return node != null
            && node.Name != null
            && node.Name.NamespaceName == name.NamespaceName
            && node.Name.LocalName == name.LocalName;
    }

    static TextSearchHit CreateHit(
        OdfTextBlock block,
        OdfTextRun? run,
        OdfNode? node,
        int offset,
        int length,
        string matched,
        bool exact)
    {
        TextSearchHit hit = new TextSearchHit();
        hit.Block = block;
        hit.Run = run;
        hit.Offset = offset;
        hit.Length = length;
        OdfSearchMatch result = new OdfSearchMatch();
        result.Location = CreateLocation(block, node, offset, length);
        result.MatchedValue = matched;
        result.Context = CreateContext(block.Text, offset, length);
        result.Node = node;
        result.IsExact = exact;
        hit.Match = result;
        return hit;
    }

    static OdfLocation CreateLocation(
        OdfTextBlock block,
        OdfNode? node,
        int offset,
        int length)
    {
        OdfLocation source = node == null
            ? new OdfLocation()
            : node.Location;
        OdfLocation result = new OdfLocation();
        result.PartPath = block.PartPath;
        result.NodePath = source.NodePath;
        result.XmlId = source.XmlId;
        result.Line = source.Line;
        result.Column = source.Column;
        result.ByteOffset = source.ByteOffset;
        result.TextOffset = offset;
        result.TextLength = length;
        return result;
    }

    static string CreateContext(string text, int offset, int length)
    {
        int start = offset > 20 ? offset - 20 : 0;
        int end = offset + length + 20;
        if (end > text.Length)
        {
            end = text.Length;
        }
        return text.Substring(start, end - start);
    }

    static void SetReplacementValues(
        List<TextSearchHit> hits,
        string replacement)
    {
        int index = 0;
        while (index < hits.Count)
        {
            hits[index].Replacement = replacement;
            index = index + 1;
        }
    }

    static OdfEditResult ApplyReplacements(
        OdfDocument document,
        List<TextSearchHit> hits)
    {
        List<TextSearchHit> accepted = NonOverlappingHits(document, hits);
        bool changed = false;
        int changedCount = 0;
        int index = accepted.Count - 1;
        while (index >= 0)
        {
            TextSearchHit hit = accepted[index];
            OdfTextBlock block = hit.Block ?? new OdfTextBlock();
            OdfSearchMatch match = hit.Match ?? new OdfSearchMatch();
            if (match.MatchedValue != hit.Replacement)
            {
                if (hit.Run == null)
                {
                    TextEditingModel.ReplaceTextRange(
                        document,
                        block,
                        hit.Offset,
                        hit.Length,
                        hit.Replacement);
                }
                else
                {
                    ReplaceRunText(document, hit);
                }
                changed = true;
                changedCount = changedCount + 1;
            }
            index = index - 1;
        }
        OdfEditResult result = new OdfEditResult();
        result.Status = changed
            ? OdfEditStatus.Success
            : OdfEditStatus.NoChange;
        result.Changed = changed;
        result.AffectedCount = changedCount;
        result.Message = changed
            ? $"Replaced {changedCount} text match(es)."
            : "No text replacements were required.";
        result.SearchMatches = MatchesFromHits(accepted);
        if (result.SearchMatches.Length > 0)
        {
            result.Location = result.SearchMatches[0].Location;
        }
        return result;
    }

    static List<TextSearchHit> NonOverlappingHits(
        OdfDocument document,
        List<TextSearchHit> hits)
    {
        List<TextSearchHit> result = new List<TextSearchHit>();
        int blockIndex = 0;
        while (blockIndex < document.TextBlocks.Length)
        {
            OdfTextBlock block = document.TextBlocks[blockIndex];
            int coveredEnd = -1;
            int hitIndex = 0;
            while (hitIndex < hits.Count)
            {
                TextSearchHit hit = hits[hitIndex];
                if (hit.Block == block && hit.Offset >= coveredEnd)
                {
                    result.Add(hit);
                    coveredEnd = hit.Offset + hit.Length;
                }
                hitIndex = hitIndex + 1;
            }
            blockIndex = blockIndex + 1;
        }
        return result;
    }

    static void ReplaceRunText(OdfDocument document, TextSearchHit hit)
    {
        OdfTextBlock block = hit.Block ?? new OdfTextBlock();
        OdfTextRun run = hit.Run ?? new OdfTextRun();
        TextEditingModel.TrackTextReplacement(
            document,
            block,
            hit.Offset,
            hit.Length,
            hit.Replacement.Length);
        OdfTextRun text = new OdfTextRun();
        text.Kind = OdfTextRunKind.Text;
        text.Text = hit.Replacement;
        run.Runs = hit.Replacement.Length == 0 ? [] : [text];
        run.Text = hit.Replacement;
        block.Text = RefreshBlockText(block.Runs);
        block.OriginalText = block.Text;
    }

    static string RefreshBlockText(OdfTextRun[] runs)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < runs.Length)
        {
            OdfTextRun run = runs[index];
            if (run.Runs.Length > 0)
            {
                run.Text = RefreshBlockText(run.Runs);
            }
            result.Append(run.Text);
            index = index + 1;
        }
        return result.ToString();
    }

    static OdfSearchMatch[] MatchesFromHits(List<TextSearchHit> hits)
    {
        OdfSearchMatch[] result = new OdfSearchMatch[hits.Count];
        int index = 0;
        while (index < hits.Count)
        {
            result[index] = hits[index].Match ?? new OdfSearchMatch();
            index = index + 1;
        }
        return result;
    }

    static void RequireDocument(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for text search.");
        }
    }

    static void RequireReplacement(string replacement)
    {
        if (replacement == null)
        {
            throw new OdfEditException("Replacement text cannot be null.");
        }
    }
}
