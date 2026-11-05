using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace pdf;

/// <summary>
/// Performs grapheme-safe Unicode measurement, line breaking, fallback selection, bidirectional ordering, and drawing.
/// </summary>
public class PdfUnicodeTextLayout : IPdfUnicodeTextLayout
{
    /// <summary>
    /// Initializes a Unicode text layout engine.
    /// </summary>
    public PdfUnicodeTextLayout()
    {
    }

    /// <summary>
    /// Lays out one normal Unicode text span.
    /// </summary>
    /// <param name="text">The logical Unicode text.</param>
    /// <param name="fonts">The ordered fallback-font chain.</param>
    /// <param name="options">The sizing and direction options.</param>
    /// <returns>The laid-out lines and aggregate size.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    public PdfUnicodeTextResult Layout(string text, PdfFontFallbackChain fonts, PdfUnicodeTextOptions options)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        PdfUnicodeTextSpan span = new PdfUnicodeTextSpan(text);
        return LayoutSpans(new PdfUnicodeTextSpan[] { span }, fonts, options);
    }

    /// <summary>
    /// Lays out Unicode spans with script, footnote, and optional hyphenation metadata.
    /// </summary>
    /// <param name="spans">The logical spans to lay out in order.</param>
    /// <param name="fonts">The ordered fallback-font chain.</param>
    /// <param name="options">The sizing and direction options.</param>
    /// <returns>The laid-out lines and aggregate size.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    public PdfUnicodeTextResult LayoutSpans(PdfUnicodeTextSpan[] spans, PdfFontFallbackChain fonts, PdfUnicodeTextOptions options)
    {
        ValidateArguments(spans, fonts, options);
        List<PdfUnicodeCluster> clusters = CreateClusters(spans, fonts, options);
        List<PdfUnicodeTextLine> lines = BreakLines(clusters, fonts, options);
        double width = 0;
        double height = 0;
        foreach (PdfUnicodeTextLine line in lines)
        {
            if (line.Width > width)
            {
                width = line.Width;
            }

            height += line.Height;
        }

        return new PdfUnicodeTextResult(lines.ToArray(), width, height);
    }

    /// <summary>
    /// Draws a completed Unicode layout with its first line on the supplied baseline.
    /// </summary>
    /// <param name="canvas">The target PDF canvas.</param>
    /// <param name="result">The completed Unicode layout.</param>
    /// <param name="x">The horizontal coordinate of the visual line start.</param>
    /// <param name="baseline">The baseline of the first line.</param>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    public void Draw(PdfCanvas canvas, PdfUnicodeTextResult result, double x, double baseline)
    {
        if (canvas == null)
        {
            throw new ArgumentNullException("canvas");
        }

        if (result == null)
        {
            throw new ArgumentNullException("result");
        }

        PdfCanvas.ValidateFinite(x, "x");
        PdfCanvas.ValidateFinite(baseline, "baseline");

        double lineBaseline = baseline;
        foreach (PdfUnicodeTextLine line in result.Lines)
        {
            double currentX = x;
            foreach (PdfUnicodeGlyphRun run in line.Runs)
            {
                canvas.DrawTextWithTrueTypeFont(run.Text, currentX, lineBaseline + run.BaselineShift, run.FontSize, run.Font);
                currentX += run.Width;
            }

            lineBaseline -= line.Height;
        }
    }

    internal static void ValidateArguments(PdfUnicodeTextSpan[] spans, PdfFontFallbackChain fonts, PdfUnicodeTextOptions options)
    {
        if (spans == null)
        {
            throw new ArgumentNullException("spans");
        }

        if (fonts == null)
        {
            throw new ArgumentNullException("fonts");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (fonts.GetFontCount() == 0)
        {
            throw new ArgumentException("A Unicode text layout requires at least one fallback font.", "fonts");
        }

        ValidatePositive(options.MaxWidth, "MaxWidth");
        ValidatePositive(options.FontSize, "FontSize");
        if (double.IsNaN(options.LineHeight) || double.IsInfinity(options.LineHeight) || options.LineHeight < 0)
        {
            throw new ArgumentOutOfRangeException("LineHeight", "Line height must be finite and nonnegative.");
        }

        ValidatePositive(options.ScriptScale, "ScriptScale");
        ValidateNonnegative(options.SuperscriptRise, "SuperscriptRise");
        ValidateNonnegative(options.SubscriptDrop, "SubscriptDrop");
        if ((int)options.Direction < 0 || (int)options.Direction > 2)
        {
            throw new ArgumentException("Text direction is not supported.", "Direction");
        }
    }

    internal static List<PdfUnicodeCluster> CreateClusters(PdfUnicodeTextSpan[] spans, PdfFontFallbackChain fonts, PdfUnicodeTextOptions options)
    {
        List<PdfUnicodeCluster> clusters = new List<PdfUnicodeCluster>();
        foreach (PdfUnicodeTextSpan span in spans)
        {
            if (span == null)
            {
                throw new ArgumentNullException("spans");
            }

            if (span.Text == null)
            {
                throw new ArgumentNullException("Text");
            }

            if ((int)span.Script < 0 || (int)span.Script > 2)
            {
                throw new ArgumentException("Text script is not supported.", "Script");
            }

            ValidateHyphenationPoints(span);
            TextElementEnumerator enumerator = StringInfo.GetTextElementEnumerator(span.Text);
            PdfUnicodeCluster? previousInSpan = null;
            while (enumerator.MoveNext())
            {
                int elementIndex = enumerator.ElementIndex;
                string element = enumerator.GetTextElement();
                if (elementIndex == 0 && StartsWithCombiningMark(element) && clusters.Count > 0 && !clusters[clusters.Count - 1].MandatoryBreak)
                {
                    PdfUnicodeCluster previousCluster = clusters[clusters.Count - 1];
                    string combinedText = previousCluster.Text + RemoveLayoutControls(element);
                    previousCluster.Font = FindFontForCluster(combinedText, fonts);
                    previousCluster.Text = combinedText;
                    previousCluster.Width = MeasureCluster(combinedText, previousCluster.Font, previousCluster.FontSize);
                    previousInSpan = previousCluster;
                    continue;
                }

                if (HasHyphenationPoint(span.HyphenationPoints, elementIndex) && previousInSpan != null)
                {
                    previousInSpan.BreakAfter = true;
                    previousInSpan.AddHyphenAtBreak = true;
                }

                PdfUnicodeCluster cluster = CreateCluster(element, span, fonts, options);
                if (previousInSpan != null && CanBreakBetween(previousInSpan.Text, cluster.Text))
                {
                    previousInSpan.BreakAfter = true;
                }

                clusters.Add(cluster);
                previousInSpan = cluster;
            }
        }

        return clusters;
    }

    internal static PdfUnicodeCluster CreateCluster(string element, PdfUnicodeTextSpan span, PdfFontFallbackChain fonts, PdfUnicodeTextOptions options)
    {
        PdfUnicodeCluster cluster = new PdfUnicodeCluster(element);
        cluster.FootnoteMarker = span.FootnoteMarker;
        PdfTextScript script = span.FootnoteMarker ? PdfTextScript.Superscript : span.Script;
        cluster.FontSize = script == PdfTextScript.Normal ? options.FontSize : options.FontSize * options.ScriptScale;
        if (script == PdfTextScript.Superscript)
        {
            cluster.BaselineShift = options.FontSize * options.SuperscriptRise;
        }
        else if (script == PdfTextScript.Subscript)
        {
            cluster.BaselineShift = 0 - options.FontSize * options.SubscriptDrop;
        }

        if (element == "\r" || element == "\n" || element == "\r\n")
        {
            cluster.MandatoryBreak = true;
            cluster.TrimAtLineEdge = true;
            return cluster;
        }

        bool hasSoftHyphen = element.IndexOf('\u00AD', StringComparison.Ordinal) >= 0;
        bool hasZeroWidthSpace = element.IndexOf('\u200B', StringComparison.Ordinal) >= 0;
        string renderableText = RemoveLayoutControls(element);
        cluster.Text = renderableText;
        if (renderableText.Length == 0)
        {
            cluster.TrimAtLineEdge = true;
            cluster.BreakAfter = hasSoftHyphen || hasZeroWidthSpace;
            cluster.AddHyphenAtBreak = hasSoftHyphen;
            return cluster;
        }

        cluster.Font = FindFontForCluster(renderableText, fonts);
        cluster.Width = MeasureCluster(renderableText, cluster.Font, cluster.FontSize);
        cluster.Direction = GetClusterDirection(renderableText);
        cluster.TrimAtLineEdge = IsBreakSpace(renderableText);
        if (IsBreakSpace(renderableText) || IsBreakAfterCharacter(renderableText))
        {
            cluster.BreakAfter = true;
        }

        if (hasSoftHyphen || hasZeroWidthSpace)
        {
            cluster.BreakAfter = true;
            cluster.AddHyphenAtBreak = hasSoftHyphen;
        }

        return cluster;
    }

    internal static List<PdfUnicodeTextLine> BreakLines(List<PdfUnicodeCluster> clusters, PdfFontFallbackChain fonts, PdfUnicodeTextOptions options)
    {
        List<PdfUnicodeTextLine> lines = new List<PdfUnicodeTextLine>();
        List<PdfUnicodeCluster> pending = new List<PdfUnicodeCluster>();
        foreach (PdfUnicodeCluster cluster in clusters)
        {
            if (cluster.MandatoryBreak)
            {
                lines.Add(CreateLine(pending, options));
                pending = new List<PdfUnicodeCluster>();
            }
            else
            {
                pending.Add(cluster);
                TrimLeadingClusters(pending);
                WrapPendingLines(lines, pending, fonts, options);
            }
        }

        if (pending.Count > 0)
        {
            lines.Add(CreateLine(pending, options));
        }

        return lines;
    }

    internal static void WrapPendingLines(List<PdfUnicodeTextLine> lines, List<PdfUnicodeCluster> pending, PdfFontFallbackChain fonts, PdfUnicodeTextOptions options)
    {
        while (MeasureClusters(pending) > options.MaxWidth && pending.Count > 0)
        {
            int breakPosition = FindLastBreakPosition(pending);
            bool addHyphen = false;
            if (breakPosition <= 0)
            {
                if (pending.Count == 1)
                {
                    lines.Add(CreateLine(pending, options));
                    pending.Clear();
                    return;
                }

                breakPosition = pending.Count - 1;
            }
            else
            {
                addHyphen = pending[breakPosition - 1].AddHyphenAtBreak;
            }

            List<PdfUnicodeCluster> lineClusters = TakeLeadingClusters(pending, breakPosition);
            if (addHyphen)
            {
                lineClusters.Add(CreateInsertedHyphen(lineClusters, fonts, options));
            }

            lines.Add(CreateLine(lineClusters, options));
            TrimLeadingClusters(pending);
        }
    }

    internal static PdfUnicodeCluster CreateInsertedHyphen(List<PdfUnicodeCluster> lineClusters, PdfFontFallbackChain fonts, PdfUnicodeTextOptions options)
    {
        PdfUnicodeCluster hyphen = new PdfUnicodeCluster("-");
        PdfUnicodeCluster? source = null;
        if (lineClusters.Count > 0)
        {
            source = lineClusters[lineClusters.Count - 1];
        }

        hyphen.FontSize = source == null ? options.FontSize : source.FontSize;
        hyphen.BaselineShift = source == null ? 0 : source.BaselineShift;
        hyphen.FootnoteMarker = source != null && source.FootnoteMarker;
        hyphen.Font = FindFontForCluster("-", fonts);
        hyphen.Width = MeasureCluster("-", hyphen.Font, hyphen.FontSize);
        hyphen.Direction = 1;
        return hyphen;
    }

    internal static PdfUnicodeTextLine CreateLine(List<PdfUnicodeCluster> source, PdfUnicodeTextOptions options)
    {
        List<PdfUnicodeCluster> logical = new List<PdfUnicodeCluster>();
        foreach (PdfUnicodeCluster cluster in source)
        {
            if (cluster.Text.Length > 0)
            {
                logical.Add(cluster);
            }
        }

        TrimTrailingClusters(logical);
        int baseLevel = GetBaseLevel(logical, options.Direction);
        AssignEmbeddingLevels(logical, baseLevel);
        List<PdfUnicodeCluster> visual = ReorderByEmbeddingLevel(logical);
        List<PdfUnicodeGlyphRun> runs = CreateRuns(visual);
        StringBuilder text = new StringBuilder();
        double width = 0;
        double maximumAscent = 0;
        double maximumDescent = 0;
        foreach (PdfUnicodeGlyphRun run in runs)
        {
            text.Append(run.Text);
            width += run.Width;
            double ascent = NormalizeMetric(run.Font.Ascent, run.Font.UnitsPerEm, run.FontSize) + run.BaselineShift;
            double descent = 0 - NormalizeMetric(run.Font.Descent, run.Font.UnitsPerEm, run.FontSize) - run.BaselineShift;
            if (ascent > maximumAscent)
            {
                maximumAscent = ascent;
            }

            if (descent > maximumDescent)
            {
                maximumDescent = descent;
            }
        }

        double height = maximumAscent + maximumDescent;

        if (height < options.LineHeight)
        {
            height = options.LineHeight;
        }

        if (height == 0)
        {
            height = options.LineHeight > 0 ? options.LineHeight : options.FontSize * 1.2;
        }

        return new PdfUnicodeTextLine(text.ToString(), runs.ToArray(), width, height);
    }

    internal static List<PdfUnicodeGlyphRun> CreateRuns(List<PdfUnicodeCluster> visual)
    {
        List<PdfUnicodeGlyphRun> runs = new List<PdfUnicodeGlyphRun>();
        StringBuilder text = new StringBuilder();
        PdfUnicodeCluster? first = null;
        double width = 0;
        foreach (PdfUnicodeCluster cluster in visual)
        {
            if (cluster.Font == null)
            {
                continue;
            }

            bool matches = first != null && first.Font != null && first.Font.Identity == cluster.Font.Identity && first.FontSize == cluster.FontSize && first.BaselineShift == cluster.BaselineShift && (first.Level & 1) == (cluster.Level & 1) && first.FootnoteMarker == cluster.FootnoteMarker;
            if (!matches && first != null && first.Font != null)
            {
                runs.Add(new PdfUnicodeGlyphRun(first.Font, text.ToString(), first.FontSize, first.BaselineShift, width, (first.Level & 1) != 0, first.FootnoteMarker));
                text.Clear();
                width = 0;
            }

            if (!matches)
            {
                first = cluster;
            }

            text.Append(cluster.Text);
            width += cluster.Width;
        }

        if (first != null && first.Font != null)
        {
            runs.Add(new PdfUnicodeGlyphRun(first.Font, text.ToString(), first.FontSize, first.BaselineShift, width, (first.Level & 1) != 0, first.FootnoteMarker));
        }

        return runs;
    }

    internal static int GetBaseLevel(List<PdfUnicodeCluster> clusters, PdfTextDirection direction)
    {
        if (direction == PdfTextDirection.LeftToRight)
        {
            return 0;
        }

        if (direction == PdfTextDirection.RightToLeft)
        {
            return 1;
        }

        foreach (PdfUnicodeCluster cluster in clusters)
        {
            if (cluster.Direction < 0)
            {
                return 1;
            }

            if (cluster.Direction > 0)
            {
                return 0;
            }
        }

        return 0;
    }

    internal static void AssignEmbeddingLevels(List<PdfUnicodeCluster> clusters, int baseLevel)
    {
        int previousDirection = baseLevel == 0 ? 1 : -1;
        for (int index = 0; index < clusters.Count; index++)
        {
            PdfUnicodeCluster cluster = clusters[index];
            int direction = cluster.Direction;
            if (direction == 0)
            {
                int nextDirection = FindNextStrongDirection(clusters, index + 1, baseLevel);
                if (nextDirection == previousDirection)
                {
                    direction = previousDirection;
                }
                else
                {
                    direction = baseLevel == 0 ? 1 : -1;
                }
            }
            else
            {
                previousDirection = direction;
            }

            if (baseLevel == 0)
            {
                cluster.Level = direction < 0 ? 1 : 0;
            }
            else
            {
                cluster.Level = direction > 0 ? 2 : 1;
            }
        }
    }

    internal static int FindNextStrongDirection(List<PdfUnicodeCluster> clusters, int start, int baseLevel)
    {
        for (int index = start; index < clusters.Count; index++)
        {
            if (clusters[index].Direction != 0)
            {
                return clusters[index].Direction;
            }
        }

        return baseLevel == 0 ? 1 : -1;
    }

    internal static List<PdfUnicodeCluster> ReorderByEmbeddingLevel(List<PdfUnicodeCluster> logical)
    {
        List<PdfUnicodeCluster> visual = new List<PdfUnicodeCluster>();
        int maximumLevel = 0;
        foreach (PdfUnicodeCluster cluster in logical)
        {
            visual.Add(cluster);
            if (cluster.Level > maximumLevel)
            {
                maximumLevel = cluster.Level;
            }
        }

        for (int level = maximumLevel; level >= 1; level--)
        {
            int index = 0;
            while (index < visual.Count)
            {
                while (index < visual.Count && visual[index].Level < level)
                {
                    index++;
                }

                int start = index;
                while (index < visual.Count && visual[index].Level >= level)
                {
                    index++;
                }

                ReverseClusters(visual, start, index - 1);
            }
        }

        return visual;
    }

    internal static void ReverseClusters(List<PdfUnicodeCluster> clusters, int start, int end)
    {
        while (start < end)
        {
            PdfUnicodeCluster temporary = clusters[start];
            clusters[start] = clusters[end];
            clusters[end] = temporary;
            start++;
            end--;
        }
    }

    internal static PdfTrueTypeFont FindFontForCluster(string text, PdfFontFallbackChain fonts)
    {
        for (int fontIndex = 0; fontIndex < fonts.GetFontCount(); fontIndex++)
        {
            PdfTrueTypeFont font = fonts.GetFont(fontIndex);
            bool containsAll = true;
            int index = 0;
            while (index < text.Length)
            {
                int unicodeValue = PdfTrueTypeFontUsage.ReadUnicodeValue(text, index);
                if (font.GetGlyphIndex(unicodeValue) < 0)
                {
                    containsAll = false;
                }

                if (unicodeValue > 0xFFFF)
                {
                    index++;
                }

                index++;
            }

            if (containsAll)
            {
                return font;
            }
        }

        throw new ArgumentException("No single font in the fallback chain contains an entire grapheme cluster.", "text");
    }

    internal static double MeasureCluster(string text, PdfTrueTypeFont font, double fontSize)
    {
        double width = 0;
        int index = 0;
        while (index < text.Length)
        {
            int unicodeValue = PdfTrueTypeFontUsage.ReadUnicodeValue(text, index);
            int glyphIndex = font.GetGlyphIndex(unicodeValue);
            width += font.AdvanceWidths[glyphIndex] * fontSize / font.UnitsPerEm;
            if (unicodeValue > 0xFFFF)
            {
                index++;
            }

            index++;
        }

        return width;
    }

    internal static string RemoveLayoutControls(string text)
    {
        StringBuilder output = new StringBuilder();
        int index = 0;
        while (index < text.Length)
        {
            int unicodeValue = PdfTrueTypeFontUsage.ReadUnicodeValue(text, index);
            if (unicodeValue != 0x00AD && unicodeValue != 0x200B && unicodeValue != 0x200D && unicodeValue != 0x2060 && !IsVariationSelector(unicodeValue))
            {
                output.Append(char.ConvertFromUtf32(unicodeValue));
            }

            if (unicodeValue > 0xFFFF)
            {
                index++;
            }

            index++;
        }

        return output.ToString();
    }

    internal static int GetClusterDirection(string text)
    {
        int index = 0;
        while (index < text.Length)
        {
            int unicodeValue = PdfTrueTypeFontUsage.ReadUnicodeValue(text, index);
            if (IsRightToLeft(unicodeValue))
            {
                return -1;
            }

            string scalar = char.ConvertFromUtf32(unicodeValue);
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(scalar, 0);
            if (category == UnicodeCategory.UppercaseLetter || category == UnicodeCategory.LowercaseLetter || category == UnicodeCategory.TitlecaseLetter || category == UnicodeCategory.OtherLetter || category == UnicodeCategory.DecimalDigitNumber)
            {
                return 1;
            }

            if (unicodeValue > 0xFFFF)
            {
                index++;
            }

            index++;
        }

        return 0;
    }

    internal static bool IsRightToLeft(int value)
    {
        if ((value >= 0x0590 && value <= 0x08FF) || (value >= 0xFB1D && value <= 0xFDFF) || (value >= 0xFE70 && value <= 0xFEFF) || (value >= 0x10800 && value <= 0x10FFF) || (value >= 0x1E800 && value <= 0x1EEFF))
        {
            if ((value >= 0x0660 && value <= 0x0669) || (value >= 0x06F0 && value <= 0x06F9))
            {
                return false;
            }

            return true;
        }

        return false;
    }

    internal static bool CanBreakBetween(string previous, string current)
    {
        if (previous.Length == 0 || current.Length == 0)
        {
            return false;
        }

        int previousValue = PdfTrueTypeFontUsage.ReadUnicodeValue(previous, 0);
        int currentValue = PdfTrueTypeFontUsage.ReadUnicodeValue(current, 0);
        if (previousValue == 0x00A0 || previousValue == 0x2060 || currentValue == 0x00A0 || currentValue == 0x2060)
        {
            return false;
        }

        if (IsOpeningPunctuation(previousValue) || IsClosingPunctuation(currentValue))
        {
            return false;
        }

        bool previousIdeographic = IsCjk(previousValue) || IsEmoji(previousValue);
        bool currentIdeographic = IsCjk(currentValue) || IsEmoji(currentValue);
        return previousIdeographic || currentIdeographic;
    }

    internal static bool IsOpeningPunctuation(int value)
    {
        if (value == 0x3008 || value == 0x300A || value == 0x300C || value == 0x300E || value == 0x3010 || value == 0x3014 || value == 0x3016 || value == 0x3018 || value == 0x301A || value == 0xFF08)
        {
            return true;
        }

        string text = char.ConvertFromUtf32(value);
        UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(text, 0);
        return category == UnicodeCategory.OpenPunctuation || category == UnicodeCategory.InitialQuotePunctuation;
    }

    internal static bool IsClosingPunctuation(int value)
    {
        if (value == 0x3001 || value == 0x3002 || value == 0x3009 || value == 0x300B || value == 0x300D || value == 0x300F || value == 0x3011 || value == 0x3015 || value == 0x3017 || value == 0x3019 || value == 0x301B || value == 0xFF01 || value == 0xFF09 || value == 0xFF0C || value == 0xFF0E || value == 0xFF1F)
        {
            return true;
        }

        string text = char.ConvertFromUtf32(value);
        UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(text, 0);
        return category == UnicodeCategory.ClosePunctuation || category == UnicodeCategory.FinalQuotePunctuation;
    }

    internal static bool IsCjk(int value)
    {
        return (value >= 0x2E80 && value <= 0xA4CF) || (value >= 0xAC00 && value <= 0xD7A3) || (value >= 0xF900 && value <= 0xFAFF) || (value >= 0x20000 && value <= 0x3134F);
    }

    internal static bool IsEmoji(int value)
    {
        return (value >= 0x1F000 && value <= 0x1FAFF) || (value >= 0x2600 && value <= 0x27BF);
    }

    internal static bool StartsWithCombiningMark(string text)
    {
        int unicodeValue = PdfTrueTypeFontUsage.ReadUnicodeValue(text, 0);
        string scalar = char.ConvertFromUtf32(unicodeValue);
        UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(scalar, 0);
        return category == UnicodeCategory.NonSpacingMark || category == UnicodeCategory.SpacingCombiningMark || category == UnicodeCategory.EnclosingMark;
    }

    internal static bool IsBreakSpace(string text)
    {
        return text == " " || text == "\t" || text == "\f" || text == "\v" || text == "\u3000";
    }

    internal static bool IsBreakAfterCharacter(string text)
    {
        return text == "-" || text == "\u2010" || text == "\u2013" || text == "\u2014" || text == "/";
    }

    internal static bool IsVariationSelector(int value)
    {
        return (value >= 0xFE00 && value <= 0xFE0F) || (value >= 0xE0100 && value <= 0xE01EF);
    }

    internal static int FindLastBreakPosition(List<PdfUnicodeCluster> clusters)
    {
        for (int index = clusters.Count - 2; index >= 0; index--)
        {
            if (clusters[index].BreakAfter)
            {
                return index + 1;
            }
        }

        return -1;
    }

    internal static List<PdfUnicodeCluster> TakeLeadingClusters(List<PdfUnicodeCluster> clusters, int count)
    {
        List<PdfUnicodeCluster> leading = new List<PdfUnicodeCluster>();
        for (int index = 0; index < count; index++)
        {
            leading.Add(clusters[0]);
            clusters.RemoveAt(0);
        }

        TrimTrailingClusters(leading);
        return leading;
    }

    internal static void TrimLeadingClusters(List<PdfUnicodeCluster> clusters)
    {
        while (clusters.Count > 0 && clusters[0].TrimAtLineEdge)
        {
            clusters.RemoveAt(0);
        }
    }

    internal static void TrimTrailingClusters(List<PdfUnicodeCluster> clusters)
    {
        while (clusters.Count > 0 && clusters[clusters.Count - 1].TrimAtLineEdge)
        {
            clusters.RemoveAt(clusters.Count - 1);
        }
    }

    internal static double MeasureClusters(List<PdfUnicodeCluster> clusters)
    {
        double width = 0;
        foreach (PdfUnicodeCluster cluster in clusters)
        {
            width += cluster.Width;
        }

        return width;
    }

    internal static bool HasHyphenationPoint(int[] points, int value)
    {
        foreach (int point in points)
        {
            if (point == value)
            {
                return true;
            }
        }

        return false;
    }

    internal static void ValidateHyphenationPoints(PdfUnicodeTextSpan span)
    {
        if (span.HyphenationPoints == null)
        {
            throw new ArgumentNullException("HyphenationPoints");
        }

        int previous = 0;
        foreach (int point in span.HyphenationPoints)
        {
            if (point <= 0 || point >= span.Text.Length || point <= previous || (char.IsLowSurrogate(span.Text[point]) && char.IsHighSurrogate(span.Text[point - 1])) || !IsTextElementBoundary(span.Text, point))
            {
                throw new ArgumentException("Hyphenation points must be ordered UTF-16 boundaries inside the span.", "HyphenationPoints");
            }

            previous = point;
        }
    }

    internal static bool IsTextElementBoundary(string text, int point)
    {
        TextElementEnumerator enumerator = StringInfo.GetTextElementEnumerator(text);
        while (enumerator.MoveNext())
        {
            if (enumerator.ElementIndex == point)
            {
                return true;
            }
        }

        return false;
    }

    internal static double NormalizeMetric(int value, int unitsPerEm, double fontSize)
    {
        return value * fontSize / unitsPerEm;
    }

    internal static void ValidatePositive(double value, string name)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
        {
            throw new ArgumentOutOfRangeException(name, "Value must be finite and positive.");
        }
    }

    internal static void ValidateNonnegative(double value, string name)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
        {
            throw new ArgumentOutOfRangeException(name, "Value must be finite and nonnegative.");
        }
    }
}
