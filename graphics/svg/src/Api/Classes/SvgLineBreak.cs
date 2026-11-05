namespace svg;

/// <summary>Processes CSS white space and lays out grapheme-safe inline lines.</summary>
public class SvgInlineTextLayout : ISvgInlineTextLayout
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgInlineTextLayout()
    {
    }

    /// <summary>Processes text using wrapping horizontal defaults.</summary>
    /// <param name="text">The UTF-16 source text.</param>
    /// <returns>The normalized items and lines.</returns>
    public SvgInlineTextResult LayoutWithText(string text)
    {
        return new SvgInlineTextLayout().LayoutWithStringAndSvgInlineTextOptions(text, new SvgInlineTextOptions());
    }

    /// <summary>Processes text using explicit white-space and layout options.</summary>
    /// <param name="text">The UTF-16 source text.</param>
    /// <param name="options">The white-space, writing-mode, and sizing options.</param>
    /// <returns>The normalized items and lines.</returns>
    public SvgInlineTextResult LayoutWithStringAndSvgInlineTextOptions(string text, SvgInlineTextOptions options)
    {
        SvgInlineTextResult result = new SvgInlineTextResult();
        if (!new SvgInlineTextLayout().ValidTextOptions(text, options)) return new SvgInlineTextLayout().Fail(result, SvgInlineTextError.InvalidArgument, "Inline text arguments or options are invalid.");
        SvgUnicodeTextOptions unicodeOptions = new SvgUnicodeTextOptions();
        unicodeOptions.MaxScalars = options.MaxClusters > int.MaxValue / 16 ? int.MaxValue : options.MaxClusters * 16;
        unicodeOptions.MaxClusters = options.MaxClusters;
        unicodeOptions.MaxRuns = options.MaxClusters;
        unicodeOptions.CancellationToken = options.CancellationToken;
        SvgUnicodeTextResult unicode = new SvgUnicodeTextAnalyzer().AnalyzeWithTextAndOptions(text, unicodeOptions);
        if (!unicode.Success)
        {
            if (unicode.Error == SvgUnicodeTextError.Cancelled) return new SvgInlineTextLayout().Fail(result, SvgInlineTextError.Cancelled, unicode.Message);
            if (unicode.Error == SvgUnicodeTextError.LimitExceeded) return new SvgInlineTextLayout().Fail(result, SvgInlineTextError.LimitExceeded, unicode.Message);
            return new SvgInlineTextLayout().Fail(result, SvgInlineTextError.InvalidArgument, unicode.Message);
        }
        return new SvgInlineTextLayout().LayoutWithSvgUnicodeTextAndSvgInlineTextOptions(unicode.UnicodeText, options);
    }

    /// <summary>Processes a previously normalized Unicode text model.</summary>
    /// <param name="text">The normalized Unicode text model.</param>
    /// <param name="options">The white-space, writing-mode, and sizing options.</param>
    /// <returns>The normalized items and lines.</returns>
    public SvgInlineTextResult LayoutWithSvgUnicodeTextAndSvgInlineTextOptions(SvgUnicodeText text, SvgInlineTextOptions options)
    {
        SvgInlineTextResult result = new SvgInlineTextResult();
        if (!new SvgInlineTextLayout().ValidModelOptions(text, options)) return new SvgInlineTextLayout().Fail(result, SvgInlineTextError.InvalidArgument, "Inline Unicode model or options are invalid.");
        if (text.Clusters.Count > options.MaxClusters) return new SvgInlineTextLayout().Fail(result, SvgInlineTextError.LimitExceeded, "Inline layout exceeds MaxClusters.");
        if (options.ClusterAdvances.Count != 0 && options.ClusterAdvances.Count != text.Clusters.Count) return new SvgInlineTextLayout().Fail(result, SvgInlineTextError.InvalidArgument, "ClusterAdvances must be empty or match the source cluster count.");
        if (!new SvgInlineTextLayout().BuildItems(result, text, options)) return result;
        new SvgInlineTextLayout().AssignBreaks(result, text, options);
        new SvgInlineTextLayout().BuildProcessedText(result);
        if (!new SvgInlineTextLayout().BuildLines(result, text, options)) return result;
        new SvgInlineTextLayout().BuildRenderedText(result);
        result.Success = true;
        return result;
    }
bool BuildItems(SvgInlineTextResult result, SvgUnicodeText text, SvgInlineTextOptions options)
    {
        bool collapse = new SvgInlineTextLayout().Collapses(options.WhiteSpace);
        bool preserveBreaks = new SvgInlineTextLayout().PreservesBreaks(options.WhiteSpace);
        bool previousCollapsible = false;
        bool afterPreservedBreak = false;
        int index = 0;
        while (index < text.Clusters.Count)
        {
            if (options.CancellationToken.IsCancellationRequested)
            {
                new SvgInlineTextLayout().Fail(result, SvgInlineTextError.Cancelled, "White-space processing was cancelled.");
                return false;
            }
            SvgGraphemeCluster cluster = text.Clusters[index];
            SvgInlineTextItem item = new SvgInlineTextItem();
            item.SourceClusterIndex = index;
            item.SourceUtf16Start = cluster.Utf16Start;
            item.SourceUtf16Length = cluster.Utf16Length;
            item.Advance = options.ClusterAdvances.Count == 0 ? options.DefaultAdvance : options.ClusterAdvances[index];
            item.Orientation = new SvgInlineTextLayout().Orientation(text, cluster, options);
            bool segmentBreak = new SvgInlineTextLayout().SegmentBreak(cluster.Text);
            bool documentSpace = cluster.Text == " " || cluster.Text == "\t" || segmentBreak;
            if (new SvgInlineTextLayout().InvisibleControl(cluster))
            {
                item.Advance = 0;
                item.Text = "";
                previousCollapsible = false;
            }
            else if (segmentBreak && preserveBreaks)
            {
                if (collapse)
                {
                    int prior = result.Items.Count - 1;
                    while (prior >= 0)
                    {
                        if (result.Items[prior].CollapsibleSpace)
                        {
                            result.Items[prior].Discarded = true;
                            prior = prior - 1;
                        }
                        else if (result.Items[prior].Text.Length == 0 && !result.Items[prior].MandatoryBreak) prior = prior - 1;
                        else break;
                    }
                }
                item.MandatoryBreak = true;
                item.BreakAfter = SvgLineBreakOpportunity.Mandatory;
                item.Advance = 0;
                previousCollapsible = false;
                afterPreservedBreak = true;
            }
            else if (documentSpace && collapse)
            {
                item.CollapsibleSpace = true;
                item.Text = " ";
                if (previousCollapsible || afterPreservedBreak) item.Discarded = true;
                else previousCollapsible = true;
            }
            else
            {
                item.Text = cluster.Text;
                if (documentSpace) item.PreservedSpace = true;
                previousCollapsible = false;
                afterPreservedBreak = false;
            }
            result.Items.Add(item);
            result.ClusterToLine.Add(-1);
            index = index + 1;
        }
        return true;
    }
void AssignBreaks(SvgInlineTextResult result, SvgUnicodeText text, SvgInlineTextOptions options)
    {
        bool wraps = new SvgInlineTextLayout().Wraps(options.WhiteSpace);
        int index = 0;
        while (index < result.Items.Count)
        {
            SvgInlineTextItem item = result.Items[index];
            if (item.MandatoryBreak) item.BreakAfter = SvgLineBreakOpportunity.Mandatory;
            else if (!wraps || item.Discarded) item.BreakAfter = SvgLineBreakOpportunity.Prohibited;
            else
            {
                int next = new SvgInlineTextLayout().NextVisible(result.Items, index + 1);
                if (options.WhiteSpace == SvgWhiteSpaceMode.BreakSpaces && item.PreservedSpace) item.BreakAfter = SvgLineBreakOpportunity.Allowed;
                else if (item.CollapsibleSpace || item.PreservedSpace) item.BreakAfter = SvgLineBreakOpportunity.Allowed;
                else if (new SvgInlineTextLayout().Contains(text.Clusters[item.SourceClusterIndex], 0x200B)) item.BreakAfter = SvgLineBreakOpportunity.Allowed;
                else if (next >= 0 && new SvgInlineTextLayout().NaturalBreak(text, item.SourceClusterIndex, result.Items[next].SourceClusterIndex)) item.BreakAfter = SvgLineBreakOpportunity.Allowed;
            }
            index = index + 1;
        }
    }
bool BuildLines(SvgInlineTextResult result, SvgUnicodeText text, SvgInlineTextOptions options)
    {
        System.Collections.Generic.List<int> active = new System.Collections.Generic.List<int>();
        int itemIndex = 0;
        while (itemIndex < result.Items.Count)
        {
            if (!result.Items[itemIndex].Discarded) active.Add(itemIndex);
            itemIndex = itemIndex + 1;
        }
        if (active.Count == 0) return new SvgInlineTextLayout().AddLine(result, text, options, active, 0, 0, false, -1);
        int cursor = 0;
        bool addFinalEmpty = false;
        while (cursor < active.Count)
        {
            if (options.CancellationToken.IsCancellationRequested)
            {
                new SvgInlineTextLayout().Fail(result, SvgInlineTextError.Cancelled, "Inline line construction was cancelled.");
                return false;
            }
            while (cursor < active.Count && result.Items[active[cursor]].CollapsibleSpace)
            {
                result.Items[active[cursor]].Trimmed = true;
                cursor = cursor + 1;
            }
            if (cursor >= active.Count)
            {
                if (result.Lines.Count == 0) addFinalEmpty = true;
                break;
            }
            int start = cursor;
            int scan = cursor;
            int lastBreak = -1;
            double advance = 0;
            bool emitted = false;
            while (scan < active.Count)
            {
                if (options.CancellationToken.IsCancellationRequested)
                {
                    new SvgInlineTextLayout().Fail(result, SvgInlineTextError.Cancelled, "Inline line construction was cancelled.");
                    return false;
                }
                SvgInlineTextItem item = result.Items[active[scan]];
                if (item.MandatoryBreak)
                {
                    if (!new SvgInlineTextLayout().AddLine(result, text, options, active, start, scan, true, active[scan])) return false;
                    item.LineIndex = result.Lines.Count - 1;
                    result.ClusterToLine[item.SourceClusterIndex] = item.LineIndex;
                    cursor = scan + 1;
                    addFinalEmpty = cursor == active.Count;
                    emitted = true;
                    break;
                }
                double itemAdvance = new SvgInlineTextLayout().EffectiveAdvance(item, advance, options);
                bool overflow = options.MaxInlineSize > 0 && advance + itemAdvance > options.MaxInlineSize && scan > start;
                if (overflow && new SvgInlineTextLayout().Wraps(options.WhiteSpace))
                {
                    int end = lastBreak > start ? lastBreak : options.OverflowWrap ? scan : -1;
                    if (end > start)
                    {
                        if (!new SvgInlineTextLayout().AddLine(result, text, options, active, start, end, false, -1)) return false;
                        cursor = end;
                        emitted = true;
                        break;
                    }
                }
                advance = advance + itemAdvance;
                if (item.BreakAfter == SvgLineBreakOpportunity.Allowed) lastBreak = scan + 1;
                scan = scan + 1;
            }
            if (!emitted)
            {
                if (!new SvgInlineTextLayout().AddLine(result, text, options, active, start, active.Count, false, -1)) return false;
                cursor = active.Count;
            }
        }
        if (addFinalEmpty) return new SvgInlineTextLayout().AddLine(result, text, options, active, active.Count, active.Count, false, -1);
        return true;
    }
bool AddLine(SvgInlineTextResult result, SvgUnicodeText text, SvgInlineTextOptions options, System.Collections.Generic.List<int> active, int start, int end, bool hardBreak, int breakItem)
    {
        if (result.Lines.Count >= options.MaxLines)
        {
            new SvgInlineTextLayout().Fail(result, SvgInlineTextError.LimitExceeded, "Inline layout exceeds MaxLines.");
            return false;
        }
        if (new SvgInlineTextLayout().Collapses(options.WhiteSpace))
        {
            while (end > start && result.Items[active[end - 1]].CollapsibleSpace)
            {
                result.Items[active[end - 1]].Trimmed = true;
                end = end - 1;
            }
        }
        SvgInlineTextLine line = new SvgInlineTextLine();
        line.Index = result.Lines.Count;
        line.BlockOffset = new SvgInlineTextLayout().BlockOffset(line.Index, options);
        line.HardBreak = hardBreak;
        System.Text.StringBuilder logical = new System.Text.StringBuilder();
        System.Collections.Generic.List<int> textStarts = new System.Collections.Generic.List<int>();
        int position = start;
        double advance = 0;
        while (position < end)
        {
            int itemIndex = active[position];
            SvgInlineTextItem item = result.Items[itemIndex];
            textStarts.Add(logical.Length);
            logical.Append(item.Text);
            item.LineIndex = line.Index;
            item.InlineOffset = advance;
            item.Advance = new SvgInlineTextLayout().EffectiveAdvance(item, advance, options);
            advance = advance + item.Advance;
            line.ItemIndices.Add(itemIndex);
            result.ClusterToLine[item.SourceClusterIndex] = line.Index;
            position = position + 1;
        }
        line.Text = logical.ToString();
        line.InlineAdvance = advance;
        new SvgInlineTextLayout().SetSourceSpan(line, result, breakItem);
        if (!new SvgInlineTextLayout().ResolveVisualLine(line, result, textStarts, options)) return false;
        result.Lines.Add(line);
        return true;
    }
void SetSourceSpan(SvgInlineTextLine line, SvgInlineTextResult result, int breakItem)
    {
        if (line.ItemIndices.Count > 0)
        {
            SvgInlineTextItem first = result.Items[line.ItemIndices[0]];
            SvgInlineTextItem last = result.Items[line.ItemIndices[line.ItemIndices.Count - 1]];
            line.SourceUtf16Start = first.SourceUtf16Start;
            line.SourceUtf16Length = last.SourceUtf16Start + last.SourceUtf16Length - first.SourceUtf16Start;
            if (breakItem >= 0)
            {
                SvgInlineTextItem separator = result.Items[breakItem];
                line.SourceUtf16Length = separator.SourceUtf16Start + separator.SourceUtf16Length - first.SourceUtf16Start;
            }
        }
        else if (breakItem >= 0)
        {
            line.SourceUtf16Start = result.Items[breakItem].SourceUtf16Start;
            line.SourceUtf16Length = result.Items[breakItem].SourceUtf16Length;
        }
    }
bool ResolveVisualLine(SvgInlineTextLine line, SvgInlineTextResult result, System.Collections.Generic.List<int> textStarts, SvgInlineTextOptions options)
    {
        SvgBidiOptions bidiOptions = new SvgBidiOptions();
        bidiOptions.MaxScalars = options.MaxClusters > int.MaxValue / 16 ? int.MaxValue : options.MaxClusters * 16;
        bidiOptions.CancellationToken = options.CancellationToken;
        if (options.Direction == SvgTextDirection.LeftToRight) bidiOptions.Direction = SvgBidiDirection.LeftToRight;
        else if (options.Direction == SvgTextDirection.RightToLeft) bidiOptions.Direction = SvgBidiDirection.RightToLeft;
        else bidiOptions.Direction = SvgBidiDirection.Auto;
        SvgBidiResult bidi = new SvgBidiAnalyzer().AnalyzeWithStringAndSvgBidiOptions(line.Text, bidiOptions);
        if (!bidi.Success)
        {
            if (bidi.Error == SvgBidiError.Cancelled) new SvgInlineTextLayout().Fail(result, SvgInlineTextError.Cancelled, bidi.Message);
            else if (bidi.Error == SvgBidiError.LimitExceeded) new SvgInlineTextLayout().Fail(result, SvgInlineTextError.LimitExceeded, bidi.Message);
            else new SvgInlineTextLayout().Fail(result, SvgInlineTextError.InvalidArgument, bidi.Message);
            return false;
        }
        line.VisualText = bidi.VisualText;
        line.BaseLevel = bidi.Paragraphs.Count == 0 ? 0 : bidi.Paragraphs[0].EmbeddingLevel;
        int visual = 0;
        int previousItem = -1;
        while (visual < bidi.VisualToLogical.Count)
        {
            int logicalScalar = bidi.VisualToLogical[visual];
            int utf16 = new SvgInlineTextLayout().ScalarUtf16Start(line.Text, logicalScalar);
            int localItem = new SvgInlineTextLayout().ItemForOffset(textStarts, line, result, utf16);
            if (localItem >= 0)
            {
                int globalItem = line.ItemIndices[localItem];
                if (globalItem != previousItem)
                {
                    line.VisualItemIndices.Add(globalItem);
                    previousItem = globalItem;
                }
            }
            visual = visual + 1;
        }
        return true;
    }
int ScalarUtf16Start(string text, int scalarIndex)
    {
        int utf16 = 0;
        int scalar = 0;
        while (utf16 < text.Length && scalar < scalarIndex)
        {
            if (text[utf16] >= 0xD800 && text[utf16] <= 0xDBFF && utf16 + 1 < text.Length && text[utf16 + 1] >= 0xDC00 && text[utf16 + 1] <= 0xDFFF) utf16 = utf16 + 2;
            else utf16 = utf16 + 1;
            scalar = scalar + 1;
        }
        return utf16;
    }
int ItemForOffset(System.Collections.Generic.List<int> starts, SvgInlineTextLine line, SvgInlineTextResult result, int utf16)
    {
        int index = starts.Count - 1;
        while (index >= 0)
        {
            int itemIndex = line.ItemIndices[index];
            if (utf16 >= starts[index] && utf16 < starts[index] + result.Items[itemIndex].Text.Length) return index;
            index = index - 1;
        }
        return -1;
    }
void BuildProcessedText(SvgInlineTextResult result)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < result.Items.Count)
        {
            SvgInlineTextItem item = result.Items[index];
            if (!item.Discarded)
            {
                if (item.MandatoryBreak) builder.Append('\n');
                else builder.Append(item.Text);
            }
            index = index + 1;
        }
        result.ProcessedText = builder.ToString();
    }
void BuildRenderedText(SvgInlineTextResult result)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < result.Lines.Count)
        {
            if (index > 0) builder.Append('\n');
            builder.Append(result.Lines[index].Text);
            index = index + 1;
        }
        result.RenderedText = builder.ToString();
    }
double EffectiveAdvance(SvgInlineTextItem item, double offset, SvgInlineTextOptions options)
    {
        if (item.Text != "\t") return item.Advance;
        double tab = options.DefaultAdvance * options.TabSize;
        if (tab <= 0) return 0;
        double remainder = offset % tab;
        if (remainder == 0) return tab;
        return tab - remainder;
    }
SvgGlyphOrientation Orientation(SvgUnicodeText text, SvgGraphemeCluster cluster, SvgInlineTextOptions options)
    {
        if (options.WritingMode == SvgWritingMode.HorizontalTopToBottom) return SvgGlyphOrientation.Horizontal;
        if (options.WritingMode == SvgWritingMode.SidewaysRightToLeft || options.WritingMode == SvgWritingMode.SidewaysLeftToRight || options.VerticalOrientation == SvgVerticalOrientation.Sideways) return SvgGlyphOrientation.Sideways;
        if (options.VerticalOrientation == SvgVerticalOrientation.Upright) return SvgGlyphOrientation.Upright;
        if (cluster.IsEmojiSequence) return SvgGlyphOrientation.Upright;
        int index = cluster.ScalarStart;
        int end = cluster.ScalarStart + cluster.ScalarCount;
        while (index < end)
        {
            int value = text.Scalars[index].Value;
            if (new SvgInlineTextLayout().EastAsianUpright(value)) return SvgGlyphOrientation.Upright;
            index = index + 1;
        }
        return SvgGlyphOrientation.Sideways;
    }
double BlockOffset(int lineIndex, SvgInlineTextOptions options)
    {
        double offset = lineIndex * options.LineAdvance;
        if (options.WritingMode == SvgWritingMode.VerticalRightToLeft || options.WritingMode == SvgWritingMode.SidewaysRightToLeft) return -offset;
        return offset;
    }
bool EastAsianUpright(int value)
    {
        return new SvgInlineTextLayout().Between(value, 0x1100, 0x11FF) || new SvgInlineTextLayout().Between(value, 0x2E80, 0x303F) || new SvgInlineTextLayout().Between(value, 0x3040, 0x30FF) || new SvgInlineTextLayout().Between(value, 0x3100, 0x31FF) || new SvgInlineTextLayout().Between(value, 0x3400, 0x4DBF) || new SvgInlineTextLayout().Between(value, 0x4E00, 0x9FFF) || new SvgInlineTextLayout().Between(value, 0xA000, 0xA4CF) || new SvgInlineTextLayout().Between(value, 0xAC00, 0xD7FF) || new SvgInlineTextLayout().Between(value, 0xF900, 0xFAFF) || new SvgInlineTextLayout().Between(value, 0xFE10, 0xFE4F) || new SvgInlineTextLayout().Between(value, 0xFF01, 0xFF60) || new SvgInlineTextLayout().Between(value, 0xFFE0, 0xFFE6) || new SvgInlineTextLayout().Between(value, 0x20000, 0x323AF);
    }
bool InvisibleControl(SvgGraphemeCluster cluster)
    {
        if (cluster.ScalarCount != 1) return false;
        return new SvgInlineTextLayout().Contains(cluster, 0x00AD) || new SvgInlineTextLayout().Contains(cluster, 0x061C) || new SvgInlineTextLayout().Contains(cluster, 0x200B) || new SvgInlineTextLayout().Contains(cluster, 0x200C) || new SvgInlineTextLayout().Contains(cluster, 0x200D) || new SvgInlineTextLayout().Contains(cluster, 0x200E) || new SvgInlineTextLayout().Contains(cluster, 0x200F) || new SvgInlineTextLayout().Contains(cluster, 0x202A) || new SvgInlineTextLayout().Contains(cluster, 0x202B) || new SvgInlineTextLayout().Contains(cluster, 0x202C) || new SvgInlineTextLayout().Contains(cluster, 0x202D) || new SvgInlineTextLayout().Contains(cluster, 0x202E) || new SvgInlineTextLayout().Contains(cluster, 0x2060) || new SvgInlineTextLayout().Contains(cluster, 0x2066) || new SvgInlineTextLayout().Contains(cluster, 0x2067) || new SvgInlineTextLayout().Contains(cluster, 0x2068) || new SvgInlineTextLayout().Contains(cluster, 0x2069) || new SvgInlineTextLayout().Contains(cluster, 0xFEFF);
    }
bool NaturalBreak(SvgUnicodeText text, int beforeCluster, int afterCluster)
    {
        SvgGraphemeCluster before = text.Clusters[beforeCluster];
        SvgGraphemeCluster after = text.Clusters[afterCluster];
        if (new SvgInlineTextLayout().Contains(before, 0x2060) || new SvgInlineTextLayout().Contains(after, 0x2060) || new SvgInlineTextLayout().Contains(before, 0x00A0) || new SvgInlineTextLayout().Contains(after, 0x00A0) || new SvgInlineTextLayout().Contains(before, 0xFEFF) || new SvgInlineTextLayout().Contains(after, 0xFEFF)) return false;
        if (new SvgInlineTextLayout().Opening(before) || new SvgInlineTextLayout().Closing(after)) return false;
        if (new SvgInlineTextLayout().HyphenOrSlash(before) || new SvgInlineTextLayout().BreakPunctuation(before)) return true;
        if (new SvgInlineTextLayout().Ideographic(before) && new SvgInlineTextLayout().Ideographic(after)) return true;
        if (new SvgInlineTextLayout().Ideographic(before) && !new SvgInlineTextLayout().Closing(after)) return true;
        if (new SvgInlineTextLayout().Ideographic(after) && !new SvgInlineTextLayout().Opening(before)) return true;
        return false;
    }
bool Opening(SvgGraphemeCluster cluster)
    {
        string text = cluster.Text;
        return text == "(" || text == "[" || text == "{" || text == "<" || text == "\u3008" || text == "\u300A" || text == "\u300C" || text == "\u300E" || text == "\u3010" || text == "\u3014" || text == "\u3016" || text == "\u3018" || text == "\u301A";
    }
bool Closing(SvgGraphemeCluster cluster)
    {
        string text = cluster.Text;
        return text == ")" || text == "]" || text == "}" || text == ">" || text == "," || text == "." || text == ":" || text == ";" || text == "!" || text == "?" || text == "\u3001" || text == "\u3002" || text == "\u3009" || text == "\u300B" || text == "\u300D" || text == "\u300F" || text == "\u3011" || text == "\u3015" || text == "\u3017" || text == "\u3019" || text == "\u301B";
    }
bool HyphenOrSlash(SvgGraphemeCluster cluster)
    {
        return cluster.Text == "-" || cluster.Text == "\u058A" || cluster.Text == "\u2010" || cluster.Text == "\u2013" || cluster.Text == "/" || cluster.Text == "\u2044";
    }
bool BreakPunctuation(SvgGraphemeCluster cluster)
    {
        return cluster.Text == "," || cluster.Text == ";" || cluster.Text == ":" || cluster.Text == "\u3001" || cluster.Text == "\u3002";
    }
bool Ideographic(SvgGraphemeCluster cluster)
    {
        return cluster.Script == "Han" || cluster.Script == "Hiragana" || cluster.Script == "Katakana" || cluster.Script == "Hangul" || cluster.Script == "Bopomofo" || cluster.Script == "Yi" || cluster.IsEmojiSequence;
    }
bool Contains(SvgGraphemeCluster cluster, int value)
    {
        if (value <= 0xFFFF) return cluster.Text.IndexOf((char)value) >= 0;
        string scalar = char.ConvertFromUtf32(value);
        return cluster.Text.IndexOf(scalar, System.StringComparison.Ordinal) >= 0;
    }
bool SegmentBreak(string text)
    {
        return text == "\r" || text == "\n" || text == "\r\n" || text == "\u0085" || text == "\u2028" || text == "\u2029";
    }
bool Collapses(SvgWhiteSpaceMode mode)
    {
        return mode == SvgWhiteSpaceMode.Normal || mode == SvgWhiteSpaceMode.NoWrap || mode == SvgWhiteSpaceMode.PreLine;
    }
bool PreservesBreaks(SvgWhiteSpaceMode mode)
    {
        return mode == SvgWhiteSpaceMode.Pre || mode == SvgWhiteSpaceMode.PreWrap || mode == SvgWhiteSpaceMode.BreakSpaces || mode == SvgWhiteSpaceMode.PreLine;
    }
bool Wraps(SvgWhiteSpaceMode mode)
    {
        return mode == SvgWhiteSpaceMode.Normal || mode == SvgWhiteSpaceMode.PreWrap || mode == SvgWhiteSpaceMode.BreakSpaces || mode == SvgWhiteSpaceMode.PreLine;
    }
int NextVisible(System.Collections.Generic.List<SvgInlineTextItem> items, int index)
    {
        while (index < items.Count)
        {
            if (!items[index].Discarded) return index;
            index = index + 1;
        }
        return -1;
    }
bool Between(int value, int minimum, int maximum)
    {
        return value >= minimum && value <= maximum;
    }
bool ValidTextOptions(string text, SvgInlineTextOptions options)
    {
        return text != null && new SvgInlineTextLayout().ValidOptions(options);
    }
bool ValidModelOptions(SvgUnicodeText? text, SvgInlineTextOptions options)
    {
        if (text == null || text.Clusters == null || text.Scalars == null || text.Text == null || !new SvgInlineTextLayout().ValidOptions(options)) return false;
        int index = 0;
        while (index < text.Clusters.Count)
        {
            if (text.Clusters[index] == null || text.Clusters[index].Index != index) return false;
            index = index + 1;
        }
        return true;
    }
bool ValidOptions(SvgInlineTextOptions? options)
    {
        if (options == null || options.ClusterAdvances == null || options.MaxClusters < 1 || options.MaxLines < 1 || options.TabSize < 1) return false;
        if (double.IsNaN(options.MaxInlineSize) || double.IsInfinity(options.MaxInlineSize) || options.MaxInlineSize < 0) return false;
        if (double.IsNaN(options.DefaultAdvance) || double.IsInfinity(options.DefaultAdvance) || options.DefaultAdvance < 0) return false;
        if (double.IsNaN(options.LineAdvance) || double.IsInfinity(options.LineAdvance) || options.LineAdvance < 0) return false;
        int index = 0;
        while (index < options.ClusterAdvances.Count)
        {
            double advance = options.ClusterAdvances[index];
            if (double.IsNaN(advance) || double.IsInfinity(advance) || advance < 0) return false;
            index = index + 1;
        }
        return true;
    }
SvgInlineTextResult Fail(SvgInlineTextResult result, SvgInlineTextError error, string message)
    {
        if (result.Error == SvgInlineTextError.None) result.Error = error;
        result.Message = message;
        return result;
    }
}
