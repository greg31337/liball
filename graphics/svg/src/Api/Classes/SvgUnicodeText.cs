namespace svg;

/// <summary>Performs bounded Unicode scalar decoding, extended grapheme segmentation, and script-language itemization.</summary>
public class SvgUnicodeTextAnalyzer : ISvgUnicodeTextAnalyzer
{

    /// <summary>Creates a stateless service instance.</summary>
    public SvgUnicodeTextAnalyzer()
    {
    }

    /// <summary>Analyzes text using secure bounds and the undetermined language.</summary>
    /// <param name="text">The UTF-16 source text.</param>
    /// <returns>The normalized text and logical mappings.</returns>
    public SvgUnicodeTextResult AnalyzeWithText(string text)
    {
        return new SvgUnicodeTextAnalyzer().AnalyzeWithTextAndOptions(text, new SvgUnicodeTextOptions());
    }

    /// <summary>Analyzes text using explicit bounds and language spans.</summary>
    /// <param name="text">The UTF-16 source text.</param>
    /// <param name="options">The bounds, languages, and cancellation token.</param>
    /// <returns>The normalized text and logical mappings.</returns>
    public SvgUnicodeTextResult AnalyzeWithTextAndOptions(string text, SvgUnicodeTextOptions options)
    {
        SvgUnicodeTextResult result = new SvgUnicodeTextResult();
        if (!new SvgUnicodeTextAnalyzer().Valid(text, options)) return new SvgUnicodeTextAnalyzer().Fail(result, SvgUnicodeTextError.InvalidArgument, "Unicode text arguments or options are invalid.");
        result.UnicodeText.SourceText = text;
        System.Text.StringBuilder normalized = new System.Text.StringBuilder(text.Length);
        int offset = 0;
        while (offset < text.Length)
        {
            if (options.CancellationToken.IsCancellationRequested) return new SvgUnicodeTextAnalyzer().Fail(result, SvgUnicodeTextError.Cancelled, "Unicode scalar decoding was cancelled.");
            if (result.UnicodeText.Scalars.Count >= options.MaxScalars) return new SvgUnicodeTextAnalyzer().Fail(result, SvgUnicodeTextError.LimitExceeded, "Unicode scalar decoding exceeds MaxScalars.");
            SvgUnicodeScalar scalar = new SvgUnicodeScalar();
            scalar.Index = result.UnicodeText.Scalars.Count;
            scalar.Utf16Start = offset;
            int first = text[offset];
            if (first >= 0xD800 && first <= 0xDBFF && offset + 1 < text.Length && text[offset + 1] >= 0xDC00 && text[offset + 1] <= 0xDFFF)
            {
                int second = text[offset + 1];
                scalar.Value = 0x10000 + ((first - 0xD800) << 10) + second - 0xDC00;
                scalar.Utf16Length = 2;
                normalized.Append(text[offset]);
                normalized.Append(text[offset + 1]);
            }
            else if (first >= 0xD800 && first <= 0xDFFF)
            {
                scalar.Value = 0xFFFD;
                scalar.IsReplacement = true;
                result.UnicodeText.ReplacementCount = result.UnicodeText.ReplacementCount + 1;
                normalized.Append('\uFFFD');
            }
            else
            {
                scalar.Value = first;
                normalized.Append(text[offset]);
            }
            scalar.IsVariationSelector = new SvgUnicodeTextAnalyzer().VariationSelector(scalar.Value);
            scalar.IsJoinControl = scalar.Value == 0x200C || scalar.Value == 0x200D;
            result.UnicodeText.Scalars.Add(scalar);
            offset = offset + scalar.Utf16Length;
        }
        result.UnicodeText.Text = normalized.ToString();
        int scalarIndex = 0;
        while (scalarIndex < result.UnicodeText.Scalars.Count)
        {
            SvgUnicodeScalar scalar = result.UnicodeText.Scalars[scalarIndex];
            scalar.Category = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(result.UnicodeText.Text, scalar.Utf16Start);
            scalar.IsCombiningMark = !scalar.IsVariationSelector && new SvgUnicodeTextAnalyzer().MarkCategory(scalar.Category);
            scalar.Script = new SvgUnicodeTextAnalyzer().Script(scalar.Value, scalar.Category);
            scalarIndex = scalarIndex + 1;
        }
        if (!new SvgUnicodeTextAnalyzer().BuildClusters(result, options)) return result;
        new SvgUnicodeTextAnalyzer().ResolveScripts(result.UnicodeText.Clusters);
        new SvgUnicodeTextAnalyzer().AssignLanguages(result.UnicodeText.Clusters, options);
        if (!new SvgUnicodeTextAnalyzer().BuildRuns(result, options)) return result;
        result.Success = true;
        return result;
    }
bool BuildClusters(SvgUnicodeTextResult result, SvgUnicodeTextOptions options)
    {
        System.Globalization.TextElementEnumerator enumerator = System.Globalization.StringInfo.GetTextElementEnumerator(result.UnicodeText.Text);
        int scalarCursor = 0;
        while (enumerator.MoveNext())
        {
            if (options.CancellationToken.IsCancellationRequested)
            {
                new SvgUnicodeTextAnalyzer().Fail(result, SvgUnicodeTextError.Cancelled, "Grapheme segmentation was cancelled.");
                return false;
            }
            if (result.UnicodeText.Clusters.Count >= options.MaxClusters)
            {
                new SvgUnicodeTextAnalyzer().Fail(result, SvgUnicodeTextError.LimitExceeded, "Grapheme segmentation exceeds MaxClusters.");
                return false;
            }
            SvgGraphemeCluster cluster = new SvgGraphemeCluster();
            cluster.Index = result.UnicodeText.Clusters.Count;
            cluster.Utf16Start = enumerator.ElementIndex;
            cluster.Text = enumerator.GetTextElement();
            cluster.Utf16Length = cluster.Text.Length;
            cluster.ScalarStart = scalarCursor;
            int end = cluster.Utf16Start + cluster.Utf16Length;
            int lastVariationBase = -1;
            while (scalarCursor < result.UnicodeText.Scalars.Count && result.UnicodeText.Scalars[scalarCursor].Utf16Start < end)
            {
                SvgUnicodeScalar scalar = result.UnicodeText.Scalars[scalarCursor];
                scalar.ClusterIndex = cluster.Index;
                if (scalar.IsVariationSelector)
                {
                    scalar.VariationBaseIndex = lastVariationBase;
                    cluster.VariationSelectorIndices.Add(scalar.Index);
                }
                else
                {
                    lastVariationBase = scalar.Index;
                    if (!scalar.IsCombiningMark && !scalar.IsJoinControl && cluster.BaseScalarIndex < 0) cluster.BaseScalarIndex = scalar.Index;
                }
                if (scalar.IsCombiningMark) cluster.HasCombiningMarks = true;
                if (new SvgUnicodeTextAnalyzer().Emoji(scalar.Value) || scalar.Value == 0x200D || scalar.Value == 0xFE0F) cluster.IsEmojiSequence = true;
                scalarCursor = scalarCursor + 1;
            }
            cluster.ScalarCount = scalarCursor - cluster.ScalarStart;
            cluster.Script = new SvgUnicodeTextAnalyzer().ClusterScript(result.UnicodeText.Scalars, cluster);
            result.UnicodeText.Clusters.Add(cluster);
        }
        return true;
    }
string ClusterScript(System.Collections.Generic.List<SvgUnicodeScalar> scalars, SvgGraphemeCluster cluster)
    {
        int index = cluster.ScalarStart;
        int end = cluster.ScalarStart + cluster.ScalarCount;
        while (index < end)
        {
            string script = scalars[index].Script;
            if (script != "Common" && script != "Inherited" && script != "Unknown") return script;
            index = index + 1;
        }
        index = cluster.ScalarStart;
        while (index < end)
        {
            if (scalars[index].Script == "Unknown") return "Unknown";
            index = index + 1;
        }
        return "Common";
    }
void ResolveScripts(System.Collections.Generic.List<SvgGraphemeCluster> clusters)
    {
        string previous = "";
        int index = 0;
        while (index < clusters.Count)
        {
            SvgGraphemeCluster cluster = clusters[index];
            if (new SvgUnicodeTextAnalyzer().StrongScript(cluster.Script)) previous = cluster.Script;
            else if (!cluster.IsEmojiSequence && previous.Length != 0) cluster.Script = previous;
            index = index + 1;
        }
        string following = "";
        index = clusters.Count - 1;
        while (index >= 0)
        {
            SvgGraphemeCluster cluster = clusters[index];
            if (new SvgUnicodeTextAnalyzer().StrongScript(cluster.Script)) following = cluster.Script;
            else if (!cluster.IsEmojiSequence && following.Length != 0) cluster.Script = following;
            index = index - 1;
        }
    }
void AssignLanguages(System.Collections.Generic.List<SvgGraphemeCluster> clusters, SvgUnicodeTextOptions options)
    {
        int clusterIndex = 0;
        while (clusterIndex < clusters.Count)
        {
            SvgGraphemeCluster cluster = clusters[clusterIndex];
            cluster.Language = options.DefaultLanguage.Trim();
            int spanIndex = 0;
            while (spanIndex < options.LanguageSpans.Count)
            {
                SvgLanguageSpan span = options.LanguageSpans[spanIndex];
                if (span.Length > 0 && cluster.Utf16Start >= span.Start && cluster.Utf16Start < span.Start + span.Length) cluster.Language = span.Language;
                spanIndex = spanIndex + 1;
            }
            clusterIndex = clusterIndex + 1;
        }
    }
bool BuildRuns(SvgUnicodeTextResult result, SvgUnicodeTextOptions options)
    {
        int mode = 0;
        while (mode < 3)
        {
            System.Collections.Generic.List<SvgTextItemRun> target = mode == 0 ? result.UnicodeText.ScriptRuns : mode == 1 ? result.UnicodeText.LanguageRuns : result.UnicodeText.ItemRuns;
            int clusterIndex = 0;
            while (clusterIndex < result.UnicodeText.Clusters.Count)
            {
                SvgGraphemeCluster cluster = result.UnicodeText.Clusters[clusterIndex];
                if (target.Count == 0 || !new SvgUnicodeTextAnalyzer().SameRun(target[target.Count - 1], cluster, mode))
                {
                    if (target.Count >= options.MaxRuns)
                    {
                        new SvgUnicodeTextAnalyzer().Fail(result, SvgUnicodeTextError.LimitExceeded, "Unicode itemization exceeds MaxRuns.");
                        return false;
                    }
                    SvgTextItemRun run = new SvgTextItemRun();
                    run.Index = target.Count;
                    run.Utf16Start = cluster.Utf16Start;
                    run.Utf16Length = cluster.Utf16Length;
                    run.ScalarStart = cluster.ScalarStart;
                    run.ScalarCount = cluster.ScalarCount;
                    run.ClusterStart = cluster.Index;
                    run.ClusterCount = 1;
                    run.Script = cluster.Script;
                    run.Language = cluster.Language;
                    target.Add(run);
                }
                else
                {
                    SvgTextItemRun run = target[target.Count - 1];
                    run.Utf16Length = cluster.Utf16Start + cluster.Utf16Length - run.Utf16Start;
                    run.ScalarCount = cluster.ScalarStart + cluster.ScalarCount - run.ScalarStart;
                    run.ClusterCount = cluster.Index + 1 - run.ClusterStart;
                }
                clusterIndex = clusterIndex + 1;
            }
            mode = mode + 1;
        }
        return true;
    }
bool SameRun(SvgTextItemRun run, SvgGraphemeCluster cluster, int mode)
    {
        if (mode == 0) return run.Script == cluster.Script;
        if (mode == 1) return string.Equals(run.Language, cluster.Language, System.StringComparison.OrdinalIgnoreCase);
        return run.Script == cluster.Script && string.Equals(run.Language, cluster.Language, System.StringComparison.OrdinalIgnoreCase);
    }
string Script(int scalar, System.Globalization.UnicodeCategory category)
    {
        if (new SvgUnicodeTextAnalyzer().Inherited(scalar)) return "Inherited";
        if (new SvgUnicodeTextAnalyzer().Common(scalar)) return "Common";
        if (new SvgUnicodeTextAnalyzer().Between(scalar, 0x0041, 0x005A) || new SvgUnicodeTextAnalyzer().Between(scalar, 0x0061, 0x007A) || new SvgUnicodeTextAnalyzer().Between(scalar, 0x00C0, 0x02E4) || new SvgUnicodeTextAnalyzer().Between(scalar, 0x1D00, 0x1DBE) || new SvgUnicodeTextAnalyzer().Between(scalar, 0x1E00, 0x1EFF) || new SvgUnicodeTextAnalyzer().Between(scalar, 0x2C60, 0x2C7F) || new SvgUnicodeTextAnalyzer().Between(scalar, 0xA720, 0xA7FF) || new SvgUnicodeTextAnalyzer().Between(scalar, 0xAB30, 0xAB6F) || new SvgUnicodeTextAnalyzer().Between(scalar, 0xFB00, 0xFB06) || new SvgUnicodeTextAnalyzer().Between(scalar, 0xFF21, 0xFF3A) || new SvgUnicodeTextAnalyzer().Between(scalar, 0xFF41, 0xFF5A) || new SvgUnicodeTextAnalyzer().Between(scalar, 0x10780, 0x107BF) || new SvgUnicodeTextAnalyzer().Between(scalar, 0x1DF00, 0x1DFFF)) return "Latin";
        if (new SvgUnicodeTextAnalyzer().Between(scalar, 0x0370, 0x03FF) || new SvgUnicodeTextAnalyzer().Between(scalar, 0x1F00, 0x1FFF) || new SvgUnicodeTextAnalyzer().Between(scalar, 0x10140, 0x1018E) || new SvgUnicodeTextAnalyzer().Between(scalar, 0x1D200, 0x1D245)) return "Greek";
        if (new SvgUnicodeTextAnalyzer().Between(scalar, 0x0400, 0x052F) || new SvgUnicodeTextAnalyzer().Between(scalar, 0x1C80, 0x1C8F) || new SvgUnicodeTextAnalyzer().Between(scalar, 0x2DE0, 0x2DFF) || new SvgUnicodeTextAnalyzer().Between(scalar, 0xA640, 0xA69F) || new SvgUnicodeTextAnalyzer().Between(scalar, 0xFE2E, 0xFE2F)) return "Cyrillic";
        if (new SvgUnicodeTextAnalyzer().Between(scalar, 0x0530, 0x058F) || new SvgUnicodeTextAnalyzer().Between(scalar, 0xFB13, 0xFB17)) return "Armenian";
        if (new SvgUnicodeTextAnalyzer().Between(scalar, 0x0590, 0x05FF) || new SvgUnicodeTextAnalyzer().Between(scalar, 0xFB1D, 0xFB4F)) return "Hebrew";
        if (new SvgUnicodeTextAnalyzer().Between(scalar, 0x0600, 0x06FF) || new SvgUnicodeTextAnalyzer().Between(scalar, 0x0750, 0x077F) || new SvgUnicodeTextAnalyzer().Between(scalar, 0x0870, 0x089F) || new SvgUnicodeTextAnalyzer().Between(scalar, 0x08A0, 0x08FF) || new SvgUnicodeTextAnalyzer().Between(scalar, 0xFB50, 0xFDFF) || new SvgUnicodeTextAnalyzer().Between(scalar, 0xFE70, 0xFEFF) || new SvgUnicodeTextAnalyzer().Between(scalar, 0x10E60, 0x10E7E) || new SvgUnicodeTextAnalyzer().Between(scalar, 0x1EE00, 0x1EEFF)) return "Arabic";
        if (new SvgUnicodeTextAnalyzer().Between(scalar, 0x0700, 0x074F) || new SvgUnicodeTextAnalyzer().Between(scalar, 0x0860, 0x086F)) return "Syriac";
        if (new SvgUnicodeTextAnalyzer().Between(scalar, 0x0780, 0x07BF)) return "Thaana";
        if (new SvgUnicodeTextAnalyzer().Between(scalar, 0x07C0, 0x07FF)) return "Nko";
        if (new SvgUnicodeTextAnalyzer().Between(scalar, 0x0800, 0x083F)) return "Samaritan";
        if (new SvgUnicodeTextAnalyzer().Between(scalar, 0x0840, 0x085F)) return "Mandaic";
        if (new SvgUnicodeTextAnalyzer().Between(scalar, 0x0900, 0x097F) || new SvgUnicodeTextAnalyzer().Between(scalar, 0xA8E0, 0xA8FF) || new SvgUnicodeTextAnalyzer().Between(scalar, 0x11B00, 0x11B5F)) return "Devanagari";
        if (new SvgUnicodeTextAnalyzer().Between(scalar, 0x0980, 0x09FF)) return "Bengali";
        if (new SvgUnicodeTextAnalyzer().Between(scalar, 0x0A00, 0x0A7F)) return "Gurmukhi";
        if (new SvgUnicodeTextAnalyzer().Between(scalar, 0x0A80, 0x0AFF)) return "Gujarati";
        if (new SvgUnicodeTextAnalyzer().Between(scalar, 0x0B00, 0x0B7F)) return "Oriya";
        if (new SvgUnicodeTextAnalyzer().Between(scalar, 0x0B80, 0x0BFF)) return "Tamil";
        if (new SvgUnicodeTextAnalyzer().Between(scalar, 0x0C00, 0x0C7F)) return "Telugu";
        if (new SvgUnicodeTextAnalyzer().Between(scalar, 0x0C80, 0x0CFF)) return "Kannada";
        if (new SvgUnicodeTextAnalyzer().Between(scalar, 0x0D00, 0x0D7F)) return "Malayalam";
        if (new SvgUnicodeTextAnalyzer().Between(scalar, 0x0D80, 0x0DFF)) return "Sinhala";
        if (new SvgUnicodeTextAnalyzer().Between(scalar, 0x0E00, 0x0E7F)) return "Thai";
        if (new SvgUnicodeTextAnalyzer().Between(scalar, 0x0E80, 0x0EFF)) return "Lao";
        if (new SvgUnicodeTextAnalyzer().Between(scalar, 0x0F00, 0x0FFF)) return "Tibetan";
        if (new SvgUnicodeTextAnalyzer().Between(scalar, 0x1000, 0x109F) || new SvgUnicodeTextAnalyzer().Between(scalar, 0xA9E0, 0xA9FF) || new SvgUnicodeTextAnalyzer().Between(scalar, 0xAA60, 0xAA7F)) return "Myanmar";
        if (new SvgUnicodeTextAnalyzer().Between(scalar, 0x10A0, 0x10FF) || new SvgUnicodeTextAnalyzer().Between(scalar, 0x1C90, 0x1CBF) || new SvgUnicodeTextAnalyzer().Between(scalar, 0x2D00, 0x2D2F)) return "Georgian";
        if (new SvgUnicodeTextAnalyzer().Between(scalar, 0x1100, 0x11FF) || new SvgUnicodeTextAnalyzer().Between(scalar, 0x3130, 0x318F) || new SvgUnicodeTextAnalyzer().Between(scalar, 0xA960, 0xA97F) || new SvgUnicodeTextAnalyzer().Between(scalar, 0xAC00, 0xD7AF) || new SvgUnicodeTextAnalyzer().Between(scalar, 0xD7B0, 0xD7FF) || new SvgUnicodeTextAnalyzer().Between(scalar, 0xFFA0, 0xFFDC)) return "Hangul";
        if (new SvgUnicodeTextAnalyzer().Between(scalar, 0x1200, 0x139F) || new SvgUnicodeTextAnalyzer().Between(scalar, 0x2D80, 0x2DDF) || new SvgUnicodeTextAnalyzer().Between(scalar, 0xAB00, 0xAB2F)) return "Ethiopic";
        if (new SvgUnicodeTextAnalyzer().Between(scalar, 0x13A0, 0x13FF) || new SvgUnicodeTextAnalyzer().Between(scalar, 0xAB70, 0xABBF)) return "Cherokee";
        if (new SvgUnicodeTextAnalyzer().Between(scalar, 0x1400, 0x167F) || new SvgUnicodeTextAnalyzer().Between(scalar, 0x18B0, 0x18FF)) return "Canadian_Aboriginal";
        if (new SvgUnicodeTextAnalyzer().Between(scalar, 0x1680, 0x169F)) return "Ogham";
        if (new SvgUnicodeTextAnalyzer().Between(scalar, 0x16A0, 0x16FF)) return "Runic";
        if (new SvgUnicodeTextAnalyzer().Between(scalar, 0x1700, 0x171F)) return "Tagalog";
        if (new SvgUnicodeTextAnalyzer().Between(scalar, 0x1720, 0x173F)) return "Hanunoo";
        if (new SvgUnicodeTextAnalyzer().Between(scalar, 0x1740, 0x175F)) return "Buhid";
        if (new SvgUnicodeTextAnalyzer().Between(scalar, 0x1760, 0x177F)) return "Tagbanwa";
        if (new SvgUnicodeTextAnalyzer().Between(scalar, 0x1780, 0x17FF) || new SvgUnicodeTextAnalyzer().Between(scalar, 0x19E0, 0x19FF)) return "Khmer";
        if (new SvgUnicodeTextAnalyzer().Between(scalar, 0x1800, 0x18AF)) return "Mongolian";
        if (new SvgUnicodeTextAnalyzer().Between(scalar, 0x3040, 0x309F) || new SvgUnicodeTextAnalyzer().Between(scalar, 0x1B001, 0x1B11F)) return "Hiragana";
        if (new SvgUnicodeTextAnalyzer().Between(scalar, 0x30A1, 0x30FA) || new SvgUnicodeTextAnalyzer().Between(scalar, 0x30FD, 0x30FF) || new SvgUnicodeTextAnalyzer().Between(scalar, 0x31F0, 0x31FF) || new SvgUnicodeTextAnalyzer().Between(scalar, 0x32D0, 0x32FE) || new SvgUnicodeTextAnalyzer().Between(scalar, 0x3300, 0x3357) || new SvgUnicodeTextAnalyzer().Between(scalar, 0xFF66, 0xFF9D) || scalar == 0x1B000 || new SvgUnicodeTextAnalyzer().Between(scalar, 0x1B120, 0x1B16F)) return "Katakana";
        if (new SvgUnicodeTextAnalyzer().Between(scalar, 0x3100, 0x312F) || new SvgUnicodeTextAnalyzer().Between(scalar, 0x31A0, 0x31BF)) return "Bopomofo";
        if (new SvgUnicodeTextAnalyzer().Between(scalar, 0x2E80, 0x2FFF) || new SvgUnicodeTextAnalyzer().Between(scalar, 0x3190, 0x319F) || new SvgUnicodeTextAnalyzer().Between(scalar, 0x31C0, 0x31EF) || new SvgUnicodeTextAnalyzer().Between(scalar, 0x3400, 0x4DBF) || new SvgUnicodeTextAnalyzer().Between(scalar, 0x4E00, 0x9FFF) || new SvgUnicodeTextAnalyzer().Between(scalar, 0xF900, 0xFAFF) || new SvgUnicodeTextAnalyzer().Between(scalar, 0x20000, 0x323AF)) return "Han";
        if (new SvgUnicodeTextAnalyzer().Between(scalar, 0xA000, 0xA4CF)) return "Yi";
        if (new SvgUnicodeTextAnalyzer().MarkCategory(category)) return "Inherited";
        if (new SvgUnicodeTextAnalyzer().CommonCategory(category)) return "Common";
        return "Unknown";
    }
bool Inherited(int scalar)
    {
        return new SvgUnicodeTextAnalyzer().Between(scalar, 0x0300, 0x036F) || new SvgUnicodeTextAnalyzer().Between(scalar, 0x0485, 0x0486) || new SvgUnicodeTextAnalyzer().Between(scalar, 0x1AB0, 0x1AFF) || new SvgUnicodeTextAnalyzer().Between(scalar, 0x1DC0, 0x1DFF) || scalar == 0x200C || scalar == 0x200D || new SvgUnicodeTextAnalyzer().Between(scalar, 0x20D0, 0x20FF) || new SvgUnicodeTextAnalyzer().Between(scalar, 0xFE00, 0xFE0F) || new SvgUnicodeTextAnalyzer().Between(scalar, 0xFE20, 0xFE2D) || new SvgUnicodeTextAnalyzer().Between(scalar, 0xE0100, 0xE01EF);
    }
bool Common(int scalar)
    {
        if (scalar <= 0x0040 || new SvgUnicodeTextAnalyzer().Between(scalar, 0x005B, 0x0060) || new SvgUnicodeTextAnalyzer().Between(scalar, 0x007B, 0x00A9) || scalar == 0x00AB || scalar == 0x00B5 || scalar == 0x00B7 || scalar == 0x00BB || new SvgUnicodeTextAnalyzer().Between(scalar, 0x2000, 0x206F) || new SvgUnicodeTextAnalyzer().Between(scalar, 0x20A0, 0x20CF) || new SvgUnicodeTextAnalyzer().Between(scalar, 0x2100, 0x214F) || new SvgUnicodeTextAnalyzer().Between(scalar, 0x2190, 0x2BFF) || new SvgUnicodeTextAnalyzer().Between(scalar, 0x3000, 0x303F) || scalar == 0x30A0 || scalar == 0x30FB || scalar == 0x30FC) return true;
        return new SvgUnicodeTextAnalyzer().Emoji(scalar);
    }
bool Emoji(int scalar)
    {
        return new SvgUnicodeTextAnalyzer().Between(scalar, 0x1F000, 0x1FAFF) || new SvgUnicodeTextAnalyzer().Between(scalar, 0x2600, 0x27BF) || new SvgUnicodeTextAnalyzer().Between(scalar, 0x2300, 0x23FF);
    }
bool VariationSelector(int scalar)
    {
        return new SvgUnicodeTextAnalyzer().Between(scalar, 0xFE00, 0xFE0F) || new SvgUnicodeTextAnalyzer().Between(scalar, 0xE0100, 0xE01EF);
    }
bool MarkCategory(System.Globalization.UnicodeCategory category)
    {
        return category == System.Globalization.UnicodeCategory.NonSpacingMark || category == System.Globalization.UnicodeCategory.SpacingCombiningMark || category == System.Globalization.UnicodeCategory.EnclosingMark;
    }
bool CommonCategory(System.Globalization.UnicodeCategory category)
    {
        return category == System.Globalization.UnicodeCategory.Control || category == System.Globalization.UnicodeCategory.Format || category == System.Globalization.UnicodeCategory.SpaceSeparator || category == System.Globalization.UnicodeCategory.LineSeparator || category == System.Globalization.UnicodeCategory.ParagraphSeparator || category == System.Globalization.UnicodeCategory.DecimalDigitNumber || category == System.Globalization.UnicodeCategory.LetterNumber || category == System.Globalization.UnicodeCategory.OtherNumber || category == System.Globalization.UnicodeCategory.ConnectorPunctuation || category == System.Globalization.UnicodeCategory.DashPunctuation || category == System.Globalization.UnicodeCategory.OpenPunctuation || category == System.Globalization.UnicodeCategory.ClosePunctuation || category == System.Globalization.UnicodeCategory.InitialQuotePunctuation || category == System.Globalization.UnicodeCategory.FinalQuotePunctuation || category == System.Globalization.UnicodeCategory.OtherPunctuation || category == System.Globalization.UnicodeCategory.MathSymbol || category == System.Globalization.UnicodeCategory.CurrencySymbol || category == System.Globalization.UnicodeCategory.ModifierSymbol || category == System.Globalization.UnicodeCategory.OtherSymbol;
    }
bool StrongScript(string script)
    {
        return script != "Common" && script != "Inherited";
    }
bool Between(int value, int minimum, int maximum)
    {
        return value >= minimum && value <= maximum;
    }
bool Valid(string? text, SvgUnicodeTextOptions? options)
    {
        if (text == null || options == null || options.DefaultLanguage == null || options.DefaultLanguage.Trim().Length == 0 || options.LanguageSpans == null || options.MaxScalars < 1 || options.MaxClusters < 1 || options.MaxRuns < 1) return false;
        int index = 0;
        while (index < options.LanguageSpans.Count)
        {
            SvgLanguageSpan span = options.LanguageSpans[index];
            if (span == null || span.Language == null || span.Language.Trim().Length == 0 || span.Start < 0 || span.Length < 0 || (long)span.Start + span.Length > text.Length) return false;
            index = index + 1;
        }
        return true;
    }
SvgUnicodeTextResult Fail(SvgUnicodeTextResult result, SvgUnicodeTextError error, string message)
    {
        if (result.Error == SvgUnicodeTextError.None) result.Error = error;
        result.Message = message;
        return result;
    }
}
