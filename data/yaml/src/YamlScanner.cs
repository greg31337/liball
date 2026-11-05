using System.Collections.Generic;
using System.Text;

namespace yaml;

/// <summary>
/// Converts YAML text into low-level tokens and scanner diagnostics.
/// </summary>
internal static class YamlScanner
{
    /// <summary>
    /// Scans normalized YAML text and returns tokens together with all scanner diagnostics.
    /// </summary>
    /// <param name="text">The YAML text to process.</param>
    /// <param name="options">The settings that control the operation; pass null to use the applicable defaults.</param>
    /// <returns>The resulting YAML scanner result.</returns>
    internal static YamlScannerResult Scan(string? text, YamlParserOptions? options)
    {
        YamlScannerResult result = new YamlScannerResult();
        YamlReader reader = YamlReader.FromText(text);
        result.SourceText = reader.Text;
        YamlParserOptions effective = options == null ? new YamlParserOptions() : options;
        result.ConfigureDiagnosticLimit(effective.MaxDiagnostics);
        List<int> indents = new List<int>();
        List<char> flows = new List<char>();
        bool atLineStart = true;

        indents.Add(0);
        ValidateUnicode(reader.Text, result);
        AddToken(result, YamlTokenKind.StreamStart, "", reader.Mark());

        while (!reader.End() && !result.DiagnosticLimitReached)
        {
            if (atLineStart)
            {
                atLineStart = ScanLineStart(reader, result, indents, effective, flows.Count);
                if (atLineStart)
                {
                    continue;
                }
            }

            if (reader.End())
            {
                break;
            }

            if (IsInvalidYamlCharacterAt(reader, 0))
            {
                ScanInvalidCharacter(reader, result);
                continue;
            }

            if (reader.Current() == '\n')
            {
                reader.Advance();
                atLineStart = true;
                continue;
            }

            if (reader.Current() == ' ' || reader.Current() == '\t')
            {
                ScanWhitespace(reader, result);
                continue;
            }

            if (reader.Current() == '#')
            {
                ValidateCommentSeparation(reader, result);
                ScanComment(reader, result, effective);
                continue;
            }

            if (reader.Current() == '%' && reader.Column == 1)
            {
                ScanDirective(reader, result, effective);
                continue;
            }

            if (StartsDocumentMarker(reader, "---"))
            {
                ScanFixedToken(reader, result, YamlTokenKind.DocumentStart, "---");
                continue;
            }

            if (StartsDocumentMarker(reader, "..."))
            {
                ValidateDocumentEndTail(reader, result);
                ScanFixedToken(reader, result, YamlTokenKind.DocumentEnd, "...");
                continue;
            }

            if (reader.Current() == '[')
            {
                ScanFixedToken(reader, result, YamlTokenKind.FlowSequenceStart, "[");
                flows.Add(']');
                continue;
            }

            if (reader.Current() == ']')
            {
                ValidateFlowCollectionEndSeparation(reader, result);
                ScanFixedToken(reader, result, YamlTokenKind.FlowSequenceEnd, "]");
                if (flows.Count > 0 && flows[flows.Count - 1] == ']')
                {
                    flows.RemoveAt(flows.Count - 1);
                }
                continue;
            }

            if (reader.Current() == '{')
            {
                ScanFixedToken(reader, result, YamlTokenKind.FlowMappingStart, "{");
                flows.Add('}');
                continue;
            }

            if (reader.Current() == '}')
            {
                ValidateFlowCollectionEndSeparation(reader, result);
                ScanFixedToken(reader, result, YamlTokenKind.FlowMappingEnd, "}");
                if (flows.Count > 0 && flows[flows.Count - 1] == '}')
                {
                    flows.RemoveAt(flows.Count - 1);
                }
                continue;
            }

            if (reader.Current() == ',')
            {
                if (flows.Count == 0)
                {
                    result.AddError("YAML015", "A flow entry separator is only valid inside a flow collection.", reader.Mark());
                }

                ScanFixedToken(reader, result, YamlTokenKind.FlowEntry, ",");
                continue;
            }

            if (flows.Count == 0 && StartsBlockLineIndicator(reader, '-'))
            {
                int compactMappingIndent = DetectCompactMappingIndent(reader);
                ScanFixedToken(reader, result, YamlTokenKind.BlockSequenceEntry, "-");
                AddImplicitIndent(reader, result, indents, compactMappingIndent);
                continue;
            }

            if (StartsMappingKeyIndicator(reader, flows.Count > 0))
            {
                ScanFixedToken(reader, result, YamlTokenKind.BlockMappingKey, "?");
                continue;
            }

            if (StartsMappingValueIndicator(reader, result, flows.Count > 0))
            {
                if (flows.Count == 0 && HasMappingValueOnLine(result, reader.Line))
                {
                    result.AddError("YAML015", "A block mapping entry cannot contain more than one value indicator.", reader.Mark());
                }

                ValidateSimpleKey(result, reader.Mark(), FlowContext(flows));
                ScanFixedToken(reader, result, YamlTokenKind.BlockMappingValue, ":");
                continue;
            }

            if (reader.Current() == '&')
            {
                ScanAnchor(reader, result);
                continue;
            }

            if (reader.Current() == '*')
            {
                ScanAlias(reader, result);
                continue;
            }

            if (reader.Current() == '!')
            {
                ScanTag(reader, result);
                continue;
            }

            if (reader.Current() == '\'')
            {
                ScanSingleQuotedScalar(reader, result);
                continue;
            }

            if (reader.Current() == '"')
            {
                ScanDoubleQuotedScalar(reader, result);
                continue;
            }

            if (reader.Current() == '|')
            {
                ScanBlockScalarHeader(reader, result, YamlTokenKind.LiteralScalar, indents);
                atLineStart = ReaderIsAtLineStart(reader);
                continue;
            }

            if (reader.Current() == '>')
            {
                ScanBlockScalarHeader(reader, result, YamlTokenKind.FoldedScalar, indents);
                atLineStart = ReaderIsAtLineStart(reader);
                continue;
            }

            if (reader.Current() == '@' || reader.Current() == '`' || reader.Current() == '%')
            {
                ScanReservedIndicator(reader, result);
                continue;
            }

            if (reader.Current() == '-'
                && IsIndicatorBoundary(reader.Peek(1))
                && InvalidSequenceIndicatorPosition(result, flows.Count > 0, reader.Line))
            {
                ScanInvalidSequenceIndicator(reader, result);
                continue;
            }

            if (!IsPlainScalarTerminator(reader, flows.Count > 0))
            {
                ScanPlainScalar(reader, result, flows.Count > 0, PlainScalarContinuationIndent(reader, indents));
                continue;
            }

            reader.Advance();
        }

        CloseIndents(reader, result, indents);
        AddToken(result, YamlTokenKind.StreamEnd, "", reader.Mark());
        return result;
    }

    /// <summary>
    /// Scans YAML text and returns only the token sequence; inspect Scan when diagnostics are needed.
    /// </summary>
    /// <param name="text">The YAML text to process.</param>
    /// <param name="options">The settings that control the operation; pass null to use the applicable defaults.</param>
    /// <returns>The resulting values in document or sequence order.</returns>
    internal static List<YamlToken> ScanTokens(string? text, YamlParserOptions? options)
    {
        return Scan(text, options).Tokens;
    }

    internal static bool ScanLineStart(YamlReader reader, YamlScannerResult result, List<int> indents, YamlParserOptions options, int flowDepth)
    {
        YamlSourceSpan start = reader.Mark();
        int spaces = 0;
        bool sawTab = false;

        while (reader.Current() == ' ' || reader.Current() == '\t')
        {
            if (reader.Current() == '\t')
            {
                sawTab = true;
            }
            else if (!sawTab)
            {
                spaces = spaces + 1;
            }
            reader.Advance();
        }

        if (sawTab && !TabCanBeSeparation(reader, result, spaces))
        {
            result.AddError("YAML001", "Tabs are not allowed in indentation.", reader.SpanFrom(start));
        }

        if (reader.Current() == '\n')
        {
            if (options == null || options.IncludeComments)
            {
                AddToken(result, YamlTokenKind.BlankLine, "", reader.SpanFrom(start));
            }

            reader.Advance();
            return true;
        }

        if (reader.Current() == '#')
        {
            if (reader.Index > start.StartIndex)
            {
                AddToken(result, YamlTokenKind.Whitespace, reader.Text.Substring(start.StartIndex, reader.Index - start.StartIndex), reader.SpanFrom(start));
            }

            ScanComment(reader, result, options);
            return false;
        }

        if (flowDepth == 0)
        {
            UpdateIndent(reader, result, indents, spaces, start);
        }
        return false;
    }

    internal static bool TabCanBeSeparation(YamlReader reader, YamlScannerResult result, int spaces)
    {
        char current = reader.Current();
        if (current == '\0' || current == '\n' || current == '#')
        {
            return true;
        }

        if (spaces > 0 && !LineContainsMappingValueIndicator(reader))
        {
            return true;
        }

        if (current == '[' || current == ']' || current == '{' || current == '}')
        {
            return true;
        }

        int previous = PreviousStructuralTokenIndex(result, result.Tokens.Count - 1);
        if (previous >= 0
            && result.Tokens[previous].Kind == YamlTokenKind.PlainScalar
            && result.Tokens[previous].Span.StartColumn == 1)
        {
            return true;
        }

        return false;
    }

    internal static bool LineContainsMappingValueIndicator(YamlReader reader)
    {
        int offset = 0;
        while (reader.Peek(offset) != '\0' && reader.Peek(offset) != '\n')
        {
            if (reader.Peek(offset) == ':' && IsIndicatorBoundary(reader.Peek(offset + 1)))
            {
                return true;
            }

            offset = offset + 1;
        }

        return false;
    }

    internal static bool HasMappingValueOnLine(YamlScannerResult result, int line)
    {
        int depth = 0;
        bool contentBeforeValue = false;
        int index = 0;
        while (index < result.Tokens.Count)
        {
            YamlToken token = result.Tokens[index];
            if (token.Span.StartLine != line)
            {
                index = index + 1;
                continue;
            }

            if (token.Kind == YamlTokenKind.FlowSequenceStart || token.Kind == YamlTokenKind.FlowMappingStart)
            {
                if (depth == 0)
                {
                    contentBeforeValue = true;
                }

                depth = depth + 1;
            }
            else if (token.Kind == YamlTokenKind.FlowSequenceEnd || token.Kind == YamlTokenKind.FlowMappingEnd)
            {
                if (depth > 0)
                {
                    depth = depth - 1;
                }
            }
            else if (depth == 0 && token.Kind == YamlTokenKind.BlockMappingValue && contentBeforeValue)
            {
                return true;
            }
            else if (depth == 0
                && token.Kind != YamlTokenKind.Whitespace
                && token.Kind != YamlTokenKind.Indent
                && token.Kind != YamlTokenKind.Dedent
                && token.Kind != YamlTokenKind.BlockMappingValue)
            {
                contentBeforeValue = true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static void ValidateDocumentEndTail(YamlReader reader, YamlScannerResult result)
    {
        int offset = 3;
        while (reader.Peek(offset) == ' ' || reader.Peek(offset) == '\t')
        {
            offset = offset + 1;
        }

        if (reader.Peek(offset) == '\0' || reader.Peek(offset) == '\n' || reader.Peek(offset) == '#')
        {
            return;
        }

        result.AddError("YAML015", "A document end marker cannot have trailing content.", reader.Mark());
    }

    internal static void UpdateIndent(YamlReader reader, YamlScannerResult result, List<int> indents, int spaces, YamlSourceSpan start)
    {
        int current = indents[indents.Count - 1];
        if (spaces > current)
        {
            indents.Add(spaces);
            result.AddToken(YamlToken.CreateIndent(YamlTokenKind.Indent, spaces, reader.SpanFrom(start)));
            return;
        }

        while (spaces < current && indents.Count > 1)
        {
            indents.RemoveAt(indents.Count - 1);
            current = indents[indents.Count - 1];
            result.AddToken(YamlToken.CreateIndent(YamlTokenKind.Dedent, current, reader.SpanFrom(start)));
        }

        if (spaces != current)
        {
            if (LastStructuralTokenIsTag(result))
            {
                indents.Add(spaces);
                result.AddToken(YamlToken.CreateIndent(YamlTokenKind.Indent, spaces, reader.SpanFrom(start)));
                return;
            }

            result.AddError("YAML002", "Indentation does not match any previous indentation level.", reader.SpanFrom(start));
        }
    }

    internal static bool LastStructuralTokenIsTag(YamlScannerResult result)
    {
        int index = result.Tokens.Count - 1;
        while (index >= 0)
        {
            YamlTokenKind kind = result.Tokens[index].Kind;
            if (kind != YamlTokenKind.Whitespace
                && kind != YamlTokenKind.Comment
                && kind != YamlTokenKind.BlankLine
                && kind != YamlTokenKind.Indent
                && kind != YamlTokenKind.Dedent)
            {
                return kind == YamlTokenKind.Tag;
            }

            index = index - 1;
        }

        return false;
    }

    internal static bool StartsMappingKeyIndicator(YamlReader reader, bool inFlow)
    {
        if (!StartsBlockIndicator(reader, '?'))
        {
            return false;
        }

        if (inFlow)
        {
            return true;
        }

        return IsFirstNonWhitespaceOnLine(reader) || FollowsBlockLineIndicator(reader);
    }

    internal static bool StartsMappingValueIndicator(YamlReader reader, YamlScannerResult result, bool inFlow)
    {
        if (reader.Current() != ':')
        {
            return false;
        }

        if (!inFlow)
        {
            return StartsBlockIndicator(reader, ':');
        }

        if (IsIndicatorBoundary(reader.Peek(1)))
        {
            return true;
        }

        int previous = PreviousStructuralTokenIndex(result, result.Tokens.Count - 1);
        if (previous < 0)
        {
            return false;
        }

        YamlTokenKind kind = result.Tokens[previous].Kind;
        return kind == YamlTokenKind.SingleQuotedScalar
            || kind == YamlTokenKind.DoubleQuotedScalar
            || kind == YamlTokenKind.FlowSequenceEnd
            || kind == YamlTokenKind.FlowMappingEnd;
    }

    internal static char FlowContext(List<char> flows)
    {
        if (flows.Count == 0)
        {
            return '\0';
        }

        return flows[flows.Count - 1] == '}' ? '{' : '[';
    }

    internal static void ValidateSimpleKey(YamlScannerResult result, YamlSourceSpan colon, char flowContext)
    {
        if (IsFirstNonWhitespaceAt(result.SourceText, colon.StartIndex))
        {
            return;
        }

        int candidate = SimpleKeyCandidateIndex(result);
        if (candidate < 0)
        {
            return;
        }

        int beforeCandidate = PreviousStructuralTokenIndex(result, candidate - 1);
        if (beforeCandidate >= 0
            && (result.Tokens[beforeCandidate].Kind == YamlTokenKind.Anchor || result.Tokens[beforeCandidate].Kind == YamlTokenKind.Tag))
        {
            int beforeProperty = PreviousStructuralTokenIndex(result, beforeCandidate - 1);
            if (beforeProperty >= 0
                && result.Tokens[beforeProperty].Kind == YamlTokenKind.DocumentStart
                && result.Tokens[beforeProperty].Span.StartLine == colon.StartLine)
            {
                result.AddError("YAML015", "A block mapping cannot follow a node property on the document marker line.", colon);
                return;
            }
        }

        if (beforeCandidate >= 0 && result.Tokens[beforeCandidate].Kind == YamlTokenKind.BlockMappingKey)
        {
            return;
        }

        YamlSourceSpan start = result.Tokens[candidate].Span;
        if (flowContext != '{' && start.StartLine != colon.StartLine)
        {
            result.AddError("YAML014", "A simple mapping key must be on one line.", YamlSourceSpan.FromBounds(start.StartIndex, start.StartLine, start.StartColumn, colon.EndIndex, colon.EndLine, colon.EndColumn));
            return;
        }

        if (UnicodeScalarCount(result.SourceText, start.StartIndex, colon.StartIndex) > 1024)
        {
            result.AddError("YAML014", "A simple mapping key must not exceed 1024 Unicode characters.", YamlSourceSpan.FromBounds(start.StartIndex, start.StartLine, start.StartColumn, colon.EndIndex, colon.EndLine, colon.EndColumn));
        }
    }

    internal static int SimpleKeyCandidateIndex(YamlScannerResult result)
    {
        int candidate = PreviousStructuralTokenIndex(result, result.Tokens.Count - 1);
        if (candidate < 0)
        {
            return -1;
        }

        YamlTokenKind kind = result.Tokens[candidate].Kind;
        if (kind == YamlTokenKind.FlowSequenceEnd || kind == YamlTokenKind.FlowMappingEnd)
        {
            candidate = MatchingFlowStartTokenIndex(result, candidate);
        }

        if (candidate < 0 || !CanEndSimpleKey(result.Tokens[candidate].Kind))
        {
            return -1;
        }

        int previous = PreviousStructuralTokenIndex(result, candidate - 1);
        while (previous >= 0
            && (result.Tokens[previous].Kind == YamlTokenKind.Tag || result.Tokens[previous].Kind == YamlTokenKind.Anchor)
            && result.Tokens[previous].Span.StartLine == result.Tokens[candidate].Span.StartLine)
        {
            candidate = previous;
            previous = PreviousStructuralTokenIndex(result, candidate - 1);
        }

        return candidate;
    }

    internal static bool IsFirstNonWhitespaceAt(string text, int sourceIndex)
    {
        int index = sourceIndex - 1;
        while (index >= 0 && text[index] != '\n')
        {
            if (text[index] != ' ' && text[index] != '\t')
            {
                return false;
            }

            index = index - 1;
        }

        return true;
    }

    internal static bool CanEndSimpleKey(YamlTokenKind kind)
    {
        return kind == YamlTokenKind.PlainScalar
            || kind == YamlTokenKind.SingleQuotedScalar
            || kind == YamlTokenKind.DoubleQuotedScalar
            || kind == YamlTokenKind.LiteralScalar
            || kind == YamlTokenKind.FoldedScalar
            || kind == YamlTokenKind.Alias
            || kind == YamlTokenKind.Tag
            || kind == YamlTokenKind.Anchor
            || kind == YamlTokenKind.FlowSequenceStart
            || kind == YamlTokenKind.FlowMappingStart;
    }

    internal static int MatchingFlowStartTokenIndex(YamlScannerResult result, int endIndex)
    {
        YamlTokenKind endKind = result.Tokens[endIndex].Kind;
        YamlTokenKind startKind = endKind == YamlTokenKind.FlowSequenceEnd ? YamlTokenKind.FlowSequenceStart : YamlTokenKind.FlowMappingStart;
        int depth = 0;
        int index = endIndex;
        while (index >= 0)
        {
            YamlTokenKind kind = result.Tokens[index].Kind;
            if (kind == endKind)
            {
                depth = depth + 1;
            }
            else if (kind == startKind)
            {
                depth = depth - 1;
                if (depth == 0)
                {
                    return index;
                }
            }

            index = index - 1;
        }

        return -1;
    }

    internal static int PreviousStructuralTokenIndex(YamlScannerResult result, int startIndex)
    {
        int index = startIndex;
        while (index >= 0)
        {
            YamlTokenKind kind = result.Tokens[index].Kind;
            if (kind != YamlTokenKind.Whitespace
                && kind != YamlTokenKind.Comment
                && kind != YamlTokenKind.BlankLine
                && kind != YamlTokenKind.Indent
                && kind != YamlTokenKind.Dedent)
            {
                return index;
            }

            index = index - 1;
        }

        return -1;
    }

    internal static int UnicodeScalarCount(string text, int startIndex, int endIndex)
    {
        int count = 0;
        int index = startIndex;
        while (index < endIndex && index < text.Length)
        {
            char current = text[index];
            if (char.IsHighSurrogate(current)
                && index + 1 < endIndex
                && index + 1 < text.Length
                && char.IsLowSurrogate(text[index + 1]))
            {
                index = index + 2;
            }
            else
            {
                index = index + 1;
            }

            count = count + 1;
        }

        return count;
    }

    internal static void AddImplicitIndent(YamlReader reader, YamlScannerResult result, List<int> indents, int indent)
    {
        if (indent <= indents[indents.Count - 1])
        {
            return;
        }

        indents.Add(indent);
        result.AddToken(YamlToken.CreateIndent(YamlTokenKind.Indent, indent, reader.Mark()));
    }

    internal static void CloseIndents(YamlReader reader, YamlScannerResult result, List<int> indents)
    {
        while (indents.Count > 1)
        {
            indents.RemoveAt(indents.Count - 1);
            result.AddToken(YamlToken.CreateIndent(YamlTokenKind.Dedent, indents[indents.Count - 1], reader.Mark()));
        }
    }

    internal static void ScanWhitespace(YamlReader reader, YamlScannerResult result)
    {
        YamlSourceSpan start = reader.Mark();
        bool sawTab = false;
        while (reader.Current() == ' ' || reader.Current() == '\t')
        {
            if (reader.Current() == '\t')
            {
                sawTab = true;
            }

            reader.Advance();
        }

        YamlSourceSpan span = reader.SpanFrom(start);
        AddToken(result, YamlTokenKind.Whitespace, reader.Text.Substring(start.StartIndex, reader.Index - start.StartIndex), span);
        if (sawTab && TabSeparatesCompactBlockCollection(reader, start))
        {
            result.AddError("YAML001", "Tabs are not allowed as indentation between compact block indicators.", span);
        }
    }

    internal static bool TabSeparatesCompactBlockCollection(YamlReader reader, YamlSourceSpan whitespaceStart)
    {
        int indicatorIndex = whitespaceStart.StartIndex - 1;
        if (indicatorIndex < 0 || !IsLineLeadingBlockIndicator(reader.Text, indicatorIndex))
        {
            return false;
        }

        char current = reader.Current();
        if ((current == '-' || current == '?' || current == ':') && IsIndicatorBoundary(reader.Peek(1)))
        {
            return true;
        }

        if (current == '\0' || current == '\n' || current == '#' || current == '\'' || current == '"' || current == '[' || current == '{')
        {
            return false;
        }

        int offset = 0;
        while (reader.Peek(offset) != '\0' && reader.Peek(offset) != '\n' && reader.Peek(offset) != '#')
        {
            if (reader.Peek(offset) == ':' && IsIndicatorBoundary(reader.Peek(offset + 1)))
            {
                return true;
            }

            offset = offset + 1;
        }

        return false;
    }

    internal static bool IsLineLeadingBlockIndicator(string text, int indicatorIndex)
    {
        if (indicatorIndex < 0 || indicatorIndex >= text.Length)
        {
            return false;
        }

        char indicator = text[indicatorIndex];
        if (indicator != '-' && indicator != '?' && indicator != ':')
        {
            return false;
        }

        int index = indicatorIndex - 1;
        while (index >= 0 && text[index] != '\n')
        {
            if (text[index] != ' ' && text[index] != '\t')
            {
                return false;
            }

            index = index - 1;
        }

        return true;
    }

    internal static void ScanComment(YamlReader reader, YamlScannerResult result, YamlParserOptions options)
    {
        YamlSourceSpan start = reader.Mark();
        reader.Advance();
        while (!reader.End() && reader.Current() != '\n')
        {
            reader.Advance();
        }

        if (options == null || options.IncludeComments)
        {
            AddToken(result, YamlTokenKind.Comment, reader.Text.Substring(start.StartIndex, reader.Index - start.StartIndex), reader.SpanFrom(start));
        }
    }

    internal static void ValidateCommentSeparation(YamlReader reader, YamlScannerResult result)
    {
        if (reader.Index == 0)
        {
            return;
        }

        char previous = reader.Text[reader.Index - 1];
        if (previous == '\n' || previous == ' ' || previous == '\t')
        {
            return;
        }

        YamlSourceSpan start = reader.Mark();
        result.AddError(
            "YAML015",
            "A comment must be separated from preceding content by white space.",
            YamlSourceSpan.FromBounds(start.StartIndex, start.StartLine, start.StartColumn, start.StartIndex + 1, start.StartLine, start.StartColumn + 1));
    }

    internal static void ScanDirective(YamlReader reader, YamlScannerResult result, YamlParserOptions options)
    {
        YamlSourceSpan start = reader.Mark();
        while (!reader.End() && reader.Current() != '\n')
        {
            reader.Advance();
        }

        YamlSourceSpan span = reader.SpanFrom(start);
        AddToken(result, YamlTokenKind.Directive, reader.Text.Substring(start.StartIndex, reader.Index - start.StartIndex), span);
        if (options != null && !options.AllowDirectives)
        {
            result.AddError("YAML004", "Directives are disabled by parser options.", span);
        }
    }

    internal static void ScanAnchor(YamlReader reader, YamlScannerResult result)
    {
        ScanAnchorLike(reader, result, YamlTokenKind.Anchor, "YAML005", "Anchor names must not be empty.");
    }

    internal static void ScanAlias(YamlReader reader, YamlScannerResult result)
    {
        ScanAnchorLike(reader, result, YamlTokenKind.Alias, "YAML006", "Alias names must not be empty.");
    }

    internal static void ScanAnchorLike(YamlReader reader, YamlScannerResult result, YamlTokenKind kind, string code, string message)
    {
        YamlSourceSpan start = reader.Mark();
        reader.Advance();
        int nameStart = reader.Index;
        while (IsAnchorNameChar(reader.Current()))
        {
            reader.Advance();
        }

        YamlSourceSpan span = reader.SpanFrom(start);
        if (reader.Index == nameStart)
        {
            AddToken(result, YamlTokenKind.Error, reader.Text.Substring(start.StartIndex, reader.Index - start.StartIndex), span);
            result.AddError(code, message, span);
            return;
        }

        AddToken(result, kind, reader.Text.Substring(start.StartIndex, reader.Index - start.StartIndex), span);
    }

    internal static void ScanTag(YamlReader reader, YamlScannerResult result)
    {
        YamlSourceSpan start = reader.Mark();
        reader.Advance();

        if (reader.Current() == '<')
        {
            reader.Advance();
            while (!reader.End() && reader.Current() != '\n' && reader.Current() != '>')
            {
                reader.Advance();
            }

            if (reader.Current() == '>')
            {
                reader.Advance();
                AddToken(result, YamlTokenKind.Tag, reader.Text.Substring(start.StartIndex, reader.Index - start.StartIndex), reader.SpanFrom(start));
                return;
            }

            YamlSourceSpan span = reader.SpanFrom(start);
            AddToken(result, YamlTokenKind.Error, reader.Text.Substring(start.StartIndex, reader.Index - start.StartIndex), span);
            result.AddError("YAML007", "Verbatim tags must end with '>'.", span);
            return;
        }

        while (!IsTagTerminator(reader.Current()))
        {
            reader.Advance();
        }

        AddToken(result, YamlTokenKind.Tag, reader.Text.Substring(start.StartIndex, reader.Index - start.StartIndex), reader.SpanFrom(start));
    }

    internal static void ScanPlainScalar(YamlReader reader, YamlScannerResult result, bool inFlow, int blockIndent)
    {
        YamlSourceSpan start = reader.Mark();
        StringBuilder builder = new StringBuilder();
        bool rootScalar = (reader.Column == 1 && IsFirstNonWhitespaceOnLine(reader)) || PreviousStructuralTokenIsDocumentStart(result);
        int scalarIndent = reader.Column - 1;
        bool scanning = true;
        while (!reader.End() && scanning)
        {
            char current = reader.Current();
            if (current == '\n')
            {
                int continuation = PlainScalarContinuationLength(reader, inFlow, blockIndent, rootScalar, scalarIndent);
                if (continuation == 0)
                {
                    scanning = false;
                }
                else
                {
                    int lineBreaks = CountLineBreaks(reader, continuation);
                    reader.AdvanceBy(continuation);
                    if (lineBreaks == 1)
                    {
                        builder.Append(' ');
                    }
                    else
                    {
                        builder.Append('\n', lineBreaks - 1);
                    }
                }
            }
            else if (current == ' ' || current == '\t')
            {
                int whitespace = 0;
                while (reader.Peek(whitespace) == ' ' || reader.Peek(whitespace) == '\t')
                {
                    whitespace = whitespace + 1;
                }

                char next = reader.Peek(whitespace);
                if (next == '\n')
                {
                    reader.AdvanceBy(whitespace);
                }
                else if (next == '\0' || next == '#' || PlainScalarCharacterTerminates(reader, whitespace, inFlow))
                {
                    scanning = false;
                }
                else
                {
                    int copied = 0;
                    while (copied < whitespace)
                    {
                        builder.Append(reader.Read());
                        copied = copied + 1;
                    }
                }
            }
            else if (PlainScalarCharacterTerminates(reader, 0, inFlow))
            {
                scanning = false;
            }
            else
            {
                builder.Append(reader.Read());
            }
        }

        if (builder.Length > 0)
        {
            AddToken(result, YamlTokenKind.PlainScalar, builder.ToString(), reader.SpanFrom(start));
        }
    }

    internal static int PlainScalarContinuationLength(YamlReader reader, bool inFlow, int blockIndent, bool rootScalar, int scalarIndent)
    {
        int offset = 0;
        int indentation = 0;
        bool countingIndentation = true;
        bool foundLineBreak = false;
        while (reader.Peek(offset) == '\n' || reader.Peek(offset) == ' ' || reader.Peek(offset) == '\t')
        {
            char current = reader.Peek(offset);
            if (current == '\n')
            {
                foundLineBreak = true;
                indentation = 0;
                countingIndentation = true;
            }
            else if (countingIndentation && current == ' ')
            {
                indentation = indentation + 1;
            }
            else if (current == '\t')
            {
                countingIndentation = false;
            }

            offset = offset + 1;
        }

        if (!foundLineBreak || reader.Peek(offset) == '\0' || reader.Peek(offset) == '#')
        {
            return 0;
        }

        if (StartsDocumentMarkerAtOffset(reader, offset))
        {
            return 0;
        }

        if (inFlow
            && (reader.Peek(offset) == ']'
                || reader.Peek(offset) == '}'
                || reader.Peek(offset) == ','
                || (reader.Peek(offset) == ':' && IsIndicatorBoundary(reader.Peek(offset + 1)))))
        {
            return 0;
        }

        if (!inFlow
            && (reader.Peek(offset) == '-' || reader.Peek(offset) == '?')
            && IsIndicatorBoundary(reader.Peek(offset + 1))
            && (indentation <= blockIndent || indentation >= scalarIndent))
        {
            return 0;
        }

        if (!inFlow && !rootScalar && indentation <= blockIndent)
        {
            return 0;
        }

        return offset;
    }

    internal static int PlainScalarContinuationIndent(YamlReader reader, List<int> indents)
    {
        int last = indents.Count - 1;
        if (IsFirstNonWhitespaceOnLine(reader) && last > 0)
        {
            return indents[last - 1];
        }

        return indents[last];
    }

    internal static int CountLineBreaks(YamlReader reader, int length)
    {
        int count = 0;
        int offset = 0;
        while (offset < length)
        {
            if (reader.Peek(offset) == '\n')
            {
                count = count + 1;
            }

            offset = offset + 1;
        }

        return count;
    }

    internal static bool PlainScalarCharacterTerminates(YamlReader reader, int offset, bool inFlow)
    {
        char current = reader.Peek(offset);
        if (IsInvalidYamlCharacterAt(reader, offset) || current == '\0' || current == '\n')
        {
            return true;
        }

        if (inFlow && (current == '[' || current == ']' || current == '{' || current == '}' || current == ','))
        {
            return true;
        }

        if (current == ':'
            && ((inFlow && IsIndicatorBoundary(reader.Peek(offset + 1)))
                || (!inFlow && IsBlockIndicatorBoundary(reader.Peek(offset + 1)))))
        {
            return true;
        }

        return false;
    }

    internal static bool StartsDocumentMarkerAtOffset(YamlReader reader, int offset)
    {
        bool startMarker = reader.Peek(offset) == '-' && reader.Peek(offset + 1) == '-' && reader.Peek(offset + 2) == '-';
        bool endMarker = reader.Peek(offset) == '.' && reader.Peek(offset + 1) == '.' && reader.Peek(offset + 2) == '.';
        if (!startMarker && !endMarker)
        {
            return false;
        }

        int before = offset - 1;
        if (before < 0 || reader.Peek(before) != '\n')
        {
            return false;
        }

        return IsIndicatorBoundary(reader.Peek(offset + 3));
    }

    internal static void ScanSingleQuotedScalar(YamlReader reader, YamlScannerResult result)
    {
        YamlSourceSpan start = reader.Mark();
        StringBuilder builder = new StringBuilder();
        bool closed = false;

        reader.Advance();
        while (!reader.End())
        {
            if (reader.Current() == '\'')
            {
                if (reader.Peek(1) == '\'')
                {
                    builder.Append('\'');
                    reader.AdvanceBy(2);
                }
                else
                {
                    reader.Advance();
                    closed = true;
                    break;
                }
            }
            else if (reader.Current() == '\n')
            {
                TrimTrailingFlowWhitespace(reader, builder);
                if (StartsDocumentMarkerAfterLineBreak(reader))
                {
                    break;
                }

                ScanFlowScalarLineBreak(reader, builder);
            }
            else
            {
                builder.Append(reader.Read());
            }
        }

        YamlSourceSpan span = reader.SpanFrom(start);
        if (closed)
        {
            AddToken(result, YamlTokenKind.SingleQuotedScalar, builder.ToString(), span);
            return;
        }

        AddToken(result, YamlTokenKind.Error, reader.Text.Substring(start.StartIndex, reader.Index - start.StartIndex), span);
        result.AddError("YAML008", "Single-quoted scalar must end with a closing quote.", span);
    }

    internal static void ScanDoubleQuotedScalar(YamlReader reader, YamlScannerResult result)
    {
        YamlSourceSpan start = reader.Mark();
        StringBuilder builder = new StringBuilder();
        List<YamlSourceSpan> continuationIndentErrors = new List<YamlSourceSpan>();
        bool closed = false;
        int previous = PreviousStructuralTokenIndex(result, result.Tokens.Count - 1);
        bool mappingValue = previous >= 0 && result.Tokens[previous].Kind == YamlTokenKind.BlockMappingValue;

        reader.Advance();
        while (!reader.End())
        {
            if (reader.Current() == '"')
            {
                reader.Advance();
                closed = true;
                break;
            }

            if (reader.Current() == '\\')
            {
                YamlSourceSpan escapeStart = reader.Mark();
                reader.Advance();
                ScanDoubleQuotedEscape(reader, result, builder, escapeStart);
            }
            else if (reader.Current() == '\n')
            {
                TrimTrailingFlowWhitespace(reader, builder);
                if (StartsDocumentMarkerAfterLineBreak(reader))
                {
                    break;
                }

                if (mappingValue)
                {
                    ValidateQuotedScalarIndentationTab(reader, result);
                    CollectQuotedScalarContinuationIndentError(reader, continuationIndentErrors);
                }

                ScanFlowScalarLineBreak(reader, builder);
            }
            else
            {
                builder.Append(reader.Read());
            }
        }

        YamlSourceSpan span = reader.SpanFrom(start);
        if (closed)
        {
            foreach (YamlSourceSpan continuationIndentError in continuationIndentErrors)
            {
                result.AddError(
                    "YAML015",
                    "A multiline quoted mapping value must be indented on continuation lines.",
                    continuationIndentError);
            }

            AddToken(result, YamlTokenKind.DoubleQuotedScalar, builder.ToString(), span);
            return;
        }

        AddToken(result, YamlTokenKind.Error, reader.Text.Substring(start.StartIndex, reader.Index - start.StartIndex), span);
        result.AddError("YAML009", "Double-quoted scalar must end with a closing quote.", span);
    }

    internal static void ValidateQuotedScalarIndentationTab(YamlReader reader, YamlScannerResult result)
    {
        if (reader.Peek(1) != '\t')
        {
            return;
        }

        result.AddError(
            "YAML001",
            "A tab cannot be used as quoted scalar indentation.",
            YamlSourceSpan.FromBounds(reader.Index + 1, reader.Line + 1, 1, reader.Index + 2, reader.Line + 1, 2));
    }

    internal static void CollectQuotedScalarContinuationIndentError(YamlReader reader, List<YamlSourceSpan> errors)
    {
        char next = reader.Peek(1);
        if (next == '\0' || next == '\n' || next == ' ' || next == '\t')
        {
            return;
        }

        errors.Add(YamlSourceSpan.FromBounds(reader.Index + 1, reader.Line + 1, 1, reader.Index + 2, reader.Line + 1, 2));
    }

    internal static bool StartsDocumentMarkerAfterLineBreak(YamlReader reader)
    {
        bool start = reader.Peek(1) == '-' && reader.Peek(2) == '-' && reader.Peek(3) == '-';
        bool end = reader.Peek(1) == '.' && reader.Peek(2) == '.' && reader.Peek(3) == '.';
        return (start || end) && IsIndicatorBoundary(reader.Peek(4));
    }

    internal static bool InvalidSequenceIndicatorPosition(YamlScannerResult result, bool inFlow, int line)
    {
        if (inFlow)
        {
            return true;
        }

        int previous = PreviousStructuralTokenIndex(result, result.Tokens.Count - 1);
        if (previous < 0 || result.Tokens[previous].Span.StartLine != line)
        {
            return false;
        }

        return result.Tokens[previous].Kind == YamlTokenKind.BlockMappingValue
            || result.Tokens[previous].Kind == YamlTokenKind.Anchor
            || result.Tokens[previous].Kind == YamlTokenKind.Tag;
    }

    internal static void ScanInvalidSequenceIndicator(YamlReader reader, YamlScannerResult result)
    {
        YamlSourceSpan start = reader.Mark();
        reader.Advance();
        YamlSourceSpan span = reader.SpanFrom(start);
        AddToken(result, YamlTokenKind.Error, "-", span);
        result.AddError("YAML015", "A block sequence indicator is not valid in this position.", span);
    }

    internal static void ValidateFlowCollectionEndSeparation(YamlReader reader, YamlScannerResult result)
    {
        char next = reader.Peek(1);
        if (IsIndicatorBoundary(next) || next == ',' || next == ']' || next == '}' || next == ':')
        {
            return;
        }

        result.AddError("YAML015", "A flow collection end must be separated from following content.", reader.Mark());
    }

    internal static bool PreviousStructuralTokenIsDocumentStart(YamlScannerResult result)
    {
        int previous = PreviousStructuralTokenIndex(result, result.Tokens.Count - 1);
        return previous >= 0 && result.Tokens[previous].Kind == YamlTokenKind.DocumentStart;
    }

    internal static void ScanDoubleQuotedEscape(YamlReader reader, YamlScannerResult result, StringBuilder builder, YamlSourceSpan escapeStart)
    {
        char escaped = reader.Current();
        if (escaped == '\0')
        {
            result.AddError("YAML010", "Double-quoted escape sequence is incomplete.", reader.SpanFrom(escapeStart));
            return;
        }

        if (escaped == '\n')
        {
            ScanEscapedFlowScalarLineBreak(reader, builder);
            return;
        }

        if (escaped == ' ')
        {
            builder.Append(' ');
            reader.Advance();
            return;
        }

        if (escaped == '0')
        {
            builder.Append((char)0);
            reader.Advance();
            return;
        }

        if (escaped == 'a')
        {
            builder.Append((char)7);
            reader.Advance();
            return;
        }

        if (escaped == 'b')
        {
            builder.Append((char)8);
            reader.Advance();
            return;
        }

        if (escaped == 't' || escaped == '\t')
        {
            builder.Append('\t');
            reader.Advance();
            return;
        }

        if (escaped == 'n')
        {
            builder.Append('\n');
            reader.Advance();
            return;
        }

        if (escaped == 'v')
        {
            builder.Append((char)11);
            reader.Advance();
            return;
        }

        if (escaped == 'f')
        {
            builder.Append((char)12);
            reader.Advance();
            return;
        }

        if (escaped == 'r')
        {
            builder.Append('\r');
            reader.Advance();
            return;
        }

        if (escaped == 'e')
        {
            builder.Append((char)27);
            reader.Advance();
            return;
        }

        if (escaped == '"' || escaped == '/' || escaped == '\\')
        {
            builder.Append(escaped);
            reader.Advance();
            return;
        }

        if (escaped == '_')
        {
            builder.Append((char)160);
            reader.Advance();
            return;
        }

        if (escaped == 'N')
        {
            builder.Append((char)133);
            reader.Advance();
            return;
        }

        if (escaped == 'L')
        {
            builder.Append((char)8232);
            reader.Advance();
            return;
        }

        if (escaped == 'P')
        {
            builder.Append((char)8233);
            reader.Advance();
            return;
        }

        if (escaped == 'x')
        {
            reader.Advance();
            ScanHexEscape(reader, result, builder, escapeStart, 2);
            return;
        }

        if (escaped == 'u')
        {
            reader.Advance();
            ScanHexEscape(reader, result, builder, escapeStart, 4);
            return;
        }

        if (escaped == 'U')
        {
            reader.Advance();
            ScanHexEscape(reader, result, builder, escapeStart, 8);
            return;
        }

        reader.Advance();
        result.AddError("YAML010", "Invalid double-quoted escape sequence '\\" + escaped.ToString() + "'.", reader.SpanFrom(escapeStart));
    }

    internal static void ScanFlowScalarLineBreak(YamlReader reader, StringBuilder builder)
    {
        int lineBreaks = 0;
        while (reader.Current() == '\n')
        {
            reader.Advance();
            lineBreaks = lineBreaks + 1;
            while (reader.Current() == ' ' || reader.Current() == '\t')
            {
                reader.Advance();
            }
        }

        if (lineBreaks == 1)
        {
            builder.Append(' ');
            return;
        }

        int index = 1;
        while (index < lineBreaks)
        {
            builder.Append('\n');
            index = index + 1;
        }
    }

    internal static void TrimTrailingFlowWhitespace(YamlReader reader, StringBuilder builder)
    {
        while (builder.Length > 0 && builder[builder.Length - 1] == ' ')
        {
            builder.Length = builder.Length - 1;
        }

        int sourceIndex = reader.Index - 1;
        while (sourceIndex >= 0 && reader.Text[sourceIndex] == ' ')
        {
            sourceIndex = sourceIndex - 1;
        }

        if (sourceIndex >= 0 && reader.Text[sourceIndex] == '\t')
        {
            bool escapedTab = sourceIndex > 0 && reader.Text[sourceIndex - 1] == '\\';
            while (!escapedTab && builder.Length > 0 && builder[builder.Length - 1] == '\t')
            {
                builder.Length = builder.Length - 1;
            }
        }
    }

    internal static void ScanEscapedFlowScalarLineBreak(YamlReader reader, StringBuilder builder)
    {
        reader.Advance();
        while (reader.Current() == ' ' || reader.Current() == '\t')
        {
            reader.Advance();
        }

        while (reader.Current() == '\n')
        {
            builder.Append('\n');
            reader.Advance();
            while (reader.Current() == ' ' || reader.Current() == '\t')
            {
                reader.Advance();
            }
        }
    }

    internal static void ScanHexEscape(YamlReader reader, YamlScannerResult result, StringBuilder builder, YamlSourceSpan escapeStart, int digitCount)
    {
        long value = 0;
        int index = 0;
        while (index < digitCount)
        {
            int digit = HexValue(reader.Current());
            if (digit < 0)
            {
                result.AddError("YAML011", "Double-quoted hexadecimal escape has invalid digits.", reader.SpanFrom(escapeStart));
                return;
            }

            value = value * 16 + digit;
            reader.Advance();
            index = index + 1;
        }

        AppendCodePoint(builder, result, value, reader.SpanFrom(escapeStart));
    }

    internal static int HexValue(char value)
    {
        if (value >= '0' && value <= '9')
        {
            return value - '0';
        }

        if (value >= 'a' && value <= 'f')
        {
            return value - 'a' + 10;
        }

        if (value >= 'A' && value <= 'F')
        {
            return value - 'A' + 10;
        }

        return -1;
    }

    internal static void AppendCodePoint(StringBuilder builder, YamlScannerResult result, long value, YamlSourceSpan span)
    {
        if (value < 0 || value > 1114111 || (value >= 55296 && value <= 57343))
        {
            result.AddError("YAML011", "Double-quoted hexadecimal escape is not a valid Unicode scalar value.", span);
            return;
        }

        if (value <= 65535)
        {
            builder.Append((char)value);
            return;
        }

        long scalar = value - 65536;
        builder.Append((char)(55296 + scalar / 1024));
        builder.Append((char)(56320 + scalar % 1024));
    }

    internal static void ScanBlockScalarHeader(YamlReader reader, YamlScannerResult result, YamlTokenKind kind, List<int> indents)
    {
        YamlSourceSpan start = reader.Mark();
        int baseIndent = BlockScalarBaseIndent(reader, result, indents);
        bool allowsZeroIndent = BlockScalarAllowsZeroIndent(reader, result);
        bool sawChomping = false;
        bool sawIndent = false;
        bool invalid = false;
        int indent = 0;

        reader.Advance();
        while (IsBlockScalarHeaderIndicator(reader.Current()))
        {
            if (reader.Current() == '+' || reader.Current() == '-')
            {
                if (sawChomping)
                {
                    invalid = true;
                }

                sawChomping = true;
            }
            else
            {
                if (sawIndent)
                {
                    invalid = true;
                }

                sawIndent = true;
                if (reader.Current() == '0')
                {
                    invalid = true;
                }
                else
                {
                    indent = reader.Current() - '0';
                }
            }

            reader.Advance();
        }

        if (!IsBlockScalarHeaderBoundary(reader.Current()))
        {
            invalid = true;
            while (!reader.End() && !IsBlockScalarHeaderBoundary(reader.Current()))
            {
                reader.Advance();
            }
        }

        if (!invalid && (reader.Current() == ' ' || reader.Current() == '\t'))
        {
            int tail = 0;
            while (reader.Peek(tail) == ' ' || reader.Peek(tail) == '\t')
            {
                tail = tail + 1;
            }

            char afterTail = reader.Peek(tail);
            if (afterTail != '\0' && afterTail != '\n' && afterTail != '#')
            {
                invalid = true;
                while (!reader.End() && reader.Current() != '\n')
                {
                    reader.Advance();
                }
            }
        }

        YamlSourceSpan span = reader.SpanFrom(start);
        string value = reader.Text.Substring(start.StartIndex, reader.Index - start.StartIndex);
        if (invalid)
        {
            AddToken(result, YamlTokenKind.Error, value, span);
            result.AddError("YAML012", "Block scalar header has invalid indentation or chomping indicators.", span);
            return;
        }

        int minimumIndent = baseIndent + 1;
        if (allowsZeroIndent)
        {
            minimumIndent = 0;
        }

        int contentIndent = 0;
        if (indent > 0)
        {
            contentIndent = baseIndent + indent;
        }

        if (kind == YamlTokenKind.LiteralScalar)
        {
            ScanLiteralBlockScalarContent(reader, result, start, value, contentIndent, minimumIndent);
            return;
        }

        ScanFoldedBlockScalarContent(reader, result, start, value, contentIndent, minimumIndent);
    }

    internal static int BlockScalarBaseIndent(YamlReader reader, YamlScannerResult result, List<int> indents)
    {
        int baseIndent = indents[indents.Count - 1];
        if (IsFirstNonWhitespaceOnLine(reader)
            && LastStructuralTokenIsTag(result)
            && indents.Count > 1)
        {
            baseIndent = indents[indents.Count - 2];
        }

        return baseIndent;
    }

    internal static bool BlockScalarAllowsZeroIndent(YamlReader reader, YamlScannerResult result)
    {
        if (reader.Column == 1)
        {
            return true;
        }

        bool sawContent = false;
        int index = result.Tokens.Count - 1;
        while (index >= 0 && result.Tokens[index].Span.StartLine == reader.Line)
        {
            YamlTokenKind kind = result.Tokens[index].Kind;
            if (kind == YamlTokenKind.DocumentStart)
            {
                return !sawContent;
            }

            if (kind != YamlTokenKind.Whitespace
                && kind != YamlTokenKind.Comment
                && kind != YamlTokenKind.Tag
                && kind != YamlTokenKind.Anchor)
            {
                sawContent = true;
            }

            index = index - 1;
        }

        return false;
    }

    internal static void ScanLiteralBlockScalarContent(YamlReader reader, YamlScannerResult result, YamlSourceSpan start, string header, int contentIndent, int minimumIndent)
    {
        SkipBlockScalarHeaderLine(reader);
        int contentStart = reader.Index;
        ValidateBlockScalarIndentationTab(reader, result, contentIndent == 0 ? minimumIndent : contentIndent);

        if (contentIndent == 0)
        {
            contentIndent = DetectBlockScalarIndent(reader.Text, reader.Index, minimumIndent);
        }

        if (HasOverIndentedLeadingBlockBlank(reader.Text, contentStart, contentIndent))
        {
            result.AddError("YAML012", "A leading block scalar blank line is more indented than the first content line.", reader.SpanFrom(start));
        }

        string raw = ScanBlockScalarRawContent(reader, contentIndent);
        string content = ApplyBlockScalarChomping(raw, BlockScalarChomping(header));
        result.AddToken(YamlToken.CreateBlockScalar(YamlTokenKind.LiteralScalar, header, content, contentIndent, reader.SpanFrom(start)));
    }

    internal static void ScanFoldedBlockScalarContent(YamlReader reader, YamlScannerResult result, YamlSourceSpan start, string header, int contentIndent, int minimumIndent)
    {
        SkipBlockScalarHeaderLine(reader);
        int contentStart = reader.Index;
        ValidateBlockScalarIndentationTab(reader, result, contentIndent == 0 ? minimumIndent : contentIndent);

        if (contentIndent == 0)
        {
            contentIndent = DetectBlockScalarIndent(reader.Text, reader.Index, minimumIndent);
        }

        if (HasOverIndentedLeadingBlockBlank(reader.Text, contentStart, contentIndent))
        {
            result.AddError("YAML012", "A leading block scalar blank line is more indented than the first content line.", reader.SpanFrom(start));
        }

        string raw = ScanBlockScalarRawContent(reader, contentIndent);
        string content = ApplyBlockScalarChomping(FoldBlockScalar(raw), BlockScalarChomping(header));
        result.AddToken(YamlToken.CreateBlockScalar(YamlTokenKind.FoldedScalar, header, content, contentIndent, reader.SpanFrom(start)));
    }

    internal static void ValidateBlockScalarIndentationTab(YamlReader reader, YamlScannerResult result, int requiredIndent)
    {
        int spaces = CountLeadingSpaces(reader);
        if (spaces >= requiredIndent || reader.Peek(spaces) != '\t')
        {
            return;
        }

        YamlSourceSpan start = reader.Mark();
        result.AddError(
            "YAML012",
            "A tab cannot be used as block scalar indentation.",
            YamlSourceSpan.FromBounds(
                start.StartIndex + spaces,
                start.StartLine,
                start.StartColumn + spaces,
                start.StartIndex + spaces + 1,
                start.StartLine,
                start.StartColumn + spaces + 1));
    }

    internal static string ScanBlockScalarRawContent(YamlReader reader, int contentIndent)
    {
        StringBuilder builder = new StringBuilder();
        while (!reader.End())
        {
            int leading = CountLeadingSpaces(reader);
            char afterIndent = reader.Peek(leading);
            if (afterIndent == '\n')
            {
                int extra = leading - contentIndent;
                while (extra > 0)
                {
                    builder.Append(' ');
                    extra = extra - 1;
                }

                reader.AdvanceBy(leading);
                builder.Append(reader.Read());
                continue;
            }

            if (afterIndent == '\0')
            {
                if (leading > 0)
                {
                    int extra = leading - contentIndent;
                    while (extra > 0)
                    {
                        builder.Append(' ');
                        extra = extra - 1;
                    }

                    reader.AdvanceBy(leading);
                    builder.Append('\n');
                }

                break;
            }

            if (leading == 0
                && (StartsDocumentMarker(reader, "---") || StartsDocumentMarker(reader, "...")))
            {
                break;
            }

            if (leading < contentIndent)
            {
                break;
            }

            reader.AdvanceBy(contentIndent);
            while (!reader.End() && reader.Current() != '\n')
            {
                builder.Append(reader.Read());
            }

            if (reader.Current() == '\n')
            {
                builder.Append(reader.Read());
            }
        }

        return builder.ToString();
    }

    internal static string FoldBlockScalar(string raw)
    {
        StringBuilder builder = new StringBuilder();
        int lineStart = 0;
        while (lineStart < raw.Length)
        {
            int lineEnd = raw.IndexOf('\n', lineStart);
            if (lineEnd < 0)
            {
                builder.Append(raw.Substring(lineStart));
                break;
            }

            string line = raw.Substring(lineStart, lineEnd - lineStart);
            builder.Append(line);

            int nextStart = lineEnd + 1;
            if (nextStart >= raw.Length)
            {
                builder.Append('\n');
                break;
            }

            if (!HasNonEmptyBlockLineAfter(raw, nextStart))
            {
                builder.Append(raw.Substring(lineEnd));
                break;
            }

            int nextEnd = raw.IndexOf('\n', nextStart);
            if (nextEnd < 0)
            {
                nextEnd = raw.Length;
            }

            string nextLine = raw.Substring(nextStart, nextEnd - nextStart);
            bool lineEmpty = line.Length == 0;
            bool lineMoreIndented = line.Length > 0 && (line[0] == ' ' || line[0] == '\t');
            bool nextEmpty = nextLine.Length == 0;
            bool nextMoreIndented = nextLine.Length > 0 && (nextLine[0] == ' ' || nextLine[0] == '\t');

            if (lineEmpty || lineMoreIndented || nextMoreIndented)
            {
                builder.Append('\n');
            }
            else if (nextEmpty && NextNonEmptyBlockLineIsMoreIndented(raw, nextStart))
            {
                builder.Append('\n');
            }
            else if (!nextEmpty)
            {
                builder.Append(' ');
            }

            lineStart = nextStart;
        }

        return builder.ToString();
    }

    internal static bool HasNonEmptyBlockLineAfter(string raw, int start)
    {
        int index = start;
        while (index < raw.Length)
        {
            int lineEnd = raw.IndexOf('\n', index);
            if (lineEnd < 0)
            {
                lineEnd = raw.Length;
            }

            if (lineEnd > index)
            {
                return true;
            }

            if (lineEnd >= raw.Length)
            {
                break;
            }

            index = lineEnd + 1;
        }

        return false;
    }

    internal static bool NextNonEmptyBlockLineIsMoreIndented(string raw, int start)
    {
        int index = start;
        while (index < raw.Length)
        {
            int lineEnd = raw.IndexOf('\n', index);
            if (lineEnd < 0)
            {
                lineEnd = raw.Length;
            }

            if (lineEnd > index)
            {
                return raw[index] == ' ' || raw[index] == '\t';
            }

            if (lineEnd >= raw.Length)
            {
                break;
            }

            index = lineEnd + 1;
        }

        return false;
    }

    internal static void SkipBlockScalarHeaderLine(YamlReader reader)
    {
        while (reader.Current() == ' ' || reader.Current() == '\t')
        {
            reader.Advance();
        }

        if (reader.Current() == '#')
        {
            while (!reader.End() && reader.Current() != '\n')
            {
                reader.Advance();
            }
        }

        if (reader.Current() == '\n')
        {
            reader.Advance();
        }
    }

    internal static int DetectBlockScalarIndent(string text, int startIndex, int minimumIndent)
    {
        int index = startIndex;
        int blankIndent = minimumIndent;
        while (index < text.Length)
        {
            int spaces = 0;
            while (index < text.Length && text[index] == ' ')
            {
                spaces = spaces + 1;
                index = index + 1;
            }

            if (index >= text.Length)
            {
                if (spaces > blankIndent)
                {
                    blankIndent = spaces;
                }

                return blankIndent;
            }

            if (text[index] == '\n')
            {
                if (spaces > blankIndent)
                {
                    blankIndent = spaces;
                }

                index = index + 1;
                continue;
            }

            if (spaces < minimumIndent)
            {
                return minimumIndent;
            }

            if (spaces == 0
                && ((text[index] == '-' && index + 2 < text.Length && text[index + 1] == '-' && text[index + 2] == '-')
                    || (text[index] == '.' && index + 2 < text.Length && text[index + 1] == '.' && text[index + 2] == '.')))
            {
                return minimumIndent;
            }

            return spaces;
        }

        return blankIndent;
    }

    internal static bool HasOverIndentedLeadingBlockBlank(string text, int startIndex, int contentIndent)
    {
        int index = startIndex;
        while (index < text.Length)
        {
            int spaces = 0;
            while (index < text.Length && text[index] == ' ')
            {
                spaces = spaces + 1;
                index = index + 1;
            }

            if (index >= text.Length)
            {
                return false;
            }

            if (text[index] != '\n')
            {
                return false;
            }

            if (spaces > contentIndent)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static int CountLeadingSpaces(YamlReader reader)
    {
        int offset = 0;
        while (reader.Peek(offset) == ' ')
        {
            offset = offset + 1;
        }

        return offset;
    }

    internal static int BlockScalarChomping(string header)
    {
        int index = 0;
        while (index < header.Length)
        {
            if (header[index] == '-')
            {
                return -1;
            }

            if (header[index] == '+')
            {
                return 1;
            }

            index = index + 1;
        }

        return 0;
    }

    internal static string ApplyBlockScalarChomping(string content, int chomping)
    {
        if (chomping > 0)
        {
            return content;
        }

        int end = content.Length;
        while (end > 0 && content[end - 1] == '\n')
        {
            end = end - 1;
        }

        if (chomping < 0)
        {
            return content.Substring(0, end);
        }

        if (content.Length == 0)
        {
            return "";
        }

        if (end == 0)
        {
            return "";
        }

        return content.Substring(0, end) + "\n";
    }

    internal static bool IsBlockScalarHeaderIndicator(char current)
    {
        return current == '+'
            || current == '-'
            || (current >= '0' && current <= '9');
    }

    internal static bool IsBlockScalarHeaderBoundary(char current)
    {
        return current == '\0'
            || current == '\n'
            || current == ' '
            || current == '\t';
    }

    internal static bool IsIndicatorBoundary(char current)
    {
        return current == '\0'
            || current == '\n'
            || current == ' '
            || current == '\t'
            || current == '['
            || current == ']'
            || current == '{'
            || current == '}'
            || current == ',';
    }

    internal static void ScanFixedToken(YamlReader reader, YamlScannerResult result, YamlTokenKind kind, string value)
    {
        YamlSourceSpan start = reader.Mark();
        reader.AdvanceBy(value.Length);
        AddToken(result, kind, value, reader.SpanFrom(start));
    }

    internal static void ScanReservedIndicator(YamlReader reader, YamlScannerResult result)
    {
        YamlSourceSpan start = reader.Mark();
        string value = reader.Read().ToString();
        YamlSourceSpan span = reader.SpanFrom(start);
        AddToken(result, YamlTokenKind.Error, value, span);
        result.AddError("YAML003", "Reserved indicator '" + value + "' cannot start a token.", span);
    }

    internal static void ScanInvalidCharacter(YamlReader reader, YamlScannerResult result)
    {
        YamlSourceSpan start = reader.Mark();
        char value = reader.Current();
        reader.Advance();
        YamlSourceSpan span = reader.SpanFrom(start);
        AddToken(result, YamlTokenKind.Error, value.ToString(), span);
    }

    internal static bool StartsDocumentMarker(YamlReader reader, string marker)
    {
        if (reader.Column != 1)
        {
            return false;
        }

        int index = 0;
        while (index < marker.Length)
        {
            if (reader.Peek(index) != marker[index])
            {
                return false;
            }

            index = index + 1;
        }

        char boundary = reader.Peek(marker.Length);
        return boundary == '\0' || boundary == '\n' || boundary == ' ' || boundary == '\t';
    }

    internal static bool ReaderIsAtLineStart(YamlReader reader)
    {
        if (reader.Index == 0)
        {
            return true;
        }

        return reader.Index <= reader.Text.Length && reader.Text[reader.Index - 1] == '\n';
    }

    internal static bool IsAnchorNameChar(char current)
    {
        if (current == '\0' || current == '\n' || current == ' ' || current == '\t')
        {
            return false;
        }

        if (current == '[' || current == ']' || current == '{' || current == '}' || current == ',')
        {
            return false;
        }

        return true;
    }

    internal static bool IsTagTerminator(char current)
    {
        if (current == '\0' || current == '\n' || current == ' ' || current == '\t')
        {
            return true;
        }

        if (current == '[' || current == ']' || current == '{' || current == '}' || current == ',')
        {
            return true;
        }

        return current == '#';
    }

    internal static bool IsPlainScalarTerminator(YamlReader reader, bool inFlow)
    {
        char current = reader.Current();
        if (IsInvalidYamlCharacterAt(reader, 0))
        {
            return true;
        }

        if (current == '\0' || current == '\n' || current == ' ' || current == '\t')
        {
            return true;
        }

        if (inFlow && (current == '[' || current == ']' || current == '{' || current == '}' || current == ','))
        {
            return true;
        }

        if (reader.Current() == '%' && reader.Column == 1)
        {
            return true;
        }

        if (StartsDocumentMarker(reader, "---") || StartsDocumentMarker(reader, "..."))
        {
            return true;
        }

        if (!inFlow && (StartsBlockLineIndicator(reader, '-') || StartsBlockLineIndicator(reader, '?')))
        {
            return true;
        }

        if (current == ':' && inFlow)
        {
            return IsIndicatorBoundary(reader.Peek(1));
        }

        if (current == ':')
        {
            return StartsBlockIndicator(reader, ':');
        }

        return false;
    }

    internal static bool IsInvalidYamlCharacter(char current)
    {
        if (current == '\n' || current == '\t')
        {
            return false;
        }

        if (current < ' ')
        {
            return true;
        }

        if (current >= (char)127 && current <= (char)132)
        {
            return true;
        }

        if (current >= (char)134 && current <= (char)159)
        {
            return true;
        }

        return char.IsSurrogate(current) || current == (char)65534 || current == (char)65535;
    }

    internal static bool IsInvalidYamlCharacterAt(YamlReader reader, int offset)
    {
        char current = reader.Peek(offset);
        if (char.IsHighSurrogate(current) && char.IsLowSurrogate(reader.Peek(offset + 1)))
        {
            return false;
        }

        int absoluteIndex = reader.Index + offset;
        if (char.IsLowSurrogate(current)
            && absoluteIndex > 0
            && absoluteIndex <= reader.Text.Length
            && char.IsHighSurrogate(reader.Text[absoluteIndex - 1]))
        {
            return false;
        }

        return IsInvalidYamlCharacter(current);
    }

    internal static void ValidateUnicode(string text, YamlScannerResult result)
    {
        int index = 0;
        int line = 1;
        int column = 1;
        while (index < text.Length)
        {
            char current = text[index];
            if (char.IsHighSurrogate(current)
                && index + 1 < text.Length
                && char.IsLowSurrogate(text[index + 1]))
            {
                index = index + 2;
                column = column + 2;
                continue;
            }

            if (IsInvalidYamlCharacter(current))
            {
                YamlSourceSpan span = YamlSourceSpan.FromBounds(index, line, column, index + 1, line, column + 1);
                result.AddError("YAML013", "Invalid character U+" + ((int)current).ToString("X4") + ".", span);
            }

            index = index + 1;
            if (current == '\n')
            {
                line = line + 1;
                column = 1;
            }
            else
            {
                column = column + 1;
            }
        }
    }

    internal static bool StartsBlockIndicator(YamlReader reader, char indicator)
    {
        if (reader.Current() != indicator)
        {
            return false;
        }

        return IsBlockIndicatorBoundary(reader.Peek(1));
    }

    internal static bool IsBlockIndicatorBoundary(char current)
    {
        return current == '\0' || current == '\n' || current == ' ' || current == '\t';
    }

    internal static int DetectCompactMappingIndent(YamlReader reader)
    {
        if (reader.Current() != '-')
        {
            return 0;
        }

        int offset = 1;
        if (reader.Peek(offset) != ' ' && reader.Peek(offset) != '\t')
        {
            return 0;
        }

        while (reader.Peek(offset) == ' ' || reader.Peek(offset) == '\t')
        {
            offset = offset + 1;
        }

        if (reader.Peek(offset) == '\0' || reader.Peek(offset) == '\n' || reader.Peek(offset) == '#')
        {
            return 0;
        }

        int indent = reader.Column + offset - 1;
        int scan = offset;
        bool sawContent = false;
        while (reader.Peek(scan) != '\0' && reader.Peek(scan) != '\n')
        {
            if (reader.Peek(scan) != ' ' && reader.Peek(scan) != '\t')
            {
                sawContent = true;
            }

            if (reader.Peek(scan) == ':' && sawContent)
            {
                char boundary = reader.Peek(scan + 1);
                if (boundary == '\0' || boundary == '\n' || boundary == ' ' || boundary == '\t')
                {
                    return indent;
                }
            }

            scan = scan + 1;
        }

        return reader.Column - 1;
    }

    internal static bool StartsBlockLineIndicator(YamlReader reader, char indicator)
    {
        if (!StartsBlockIndicator(reader, indicator))
        {
            return false;
        }

        return IsFirstNonWhitespaceOnLine(reader) || FollowsBlockLineIndicator(reader);
    }

    internal static bool FollowsBlockLineIndicator(YamlReader reader)
    {
        int index = reader.Index - 1;
        bool sawIndicator = false;
        while (index >= 0 && reader.Text[index] != '\n')
        {
            if (reader.Text[index] == ' ' || reader.Text[index] == '\t')
            {
                index = index - 1;
                continue;
            }

            if (reader.Text[index] != '-' && reader.Text[index] != '?' && reader.Text[index] != ':')
            {
                return false;
            }

            sawIndicator = true;
            index = index - 1;
        }

        return sawIndicator;
    }

    internal static bool IsFirstNonWhitespaceOnLine(YamlReader reader)
    {
        int index = reader.Index - 1;
        while (index >= 0 && reader.Text[index] != '\n')
        {
            if (reader.Text[index] != ' ' && reader.Text[index] != '\t')
            {
                return false;
            }

            index = index - 1;
        }

        return true;
    }

    internal static void AddToken(YamlScannerResult result, YamlTokenKind kind, string value, YamlSourceSpan span)
    {
        result.AddToken(YamlToken.Create(kind, value, span));
    }
}
