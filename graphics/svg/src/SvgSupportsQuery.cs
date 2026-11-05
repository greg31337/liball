namespace svg;

internal class SvgSupportsToken
{
    internal SvgCssToken Token;
    internal int OriginalIndex;

    internal SvgSupportsToken(SvgCssToken token, int originalIndex)
    {
        Token = token;
        OriginalIndex = originalIndex;
    }
}

internal class SvgSupportsParserState
{
    string text;
    System.Collections.Generic.List<SvgCssToken> allTokens;
    System.Collections.Generic.List<SvgSupportsToken> tokens;
    SvgSourceUri source;
    SvgCssLimits limits;
    int baseOffset;
    int baseLine;
    int baseColumn;
    int position;
    int conditionCount;
    bool limitExceeded;
    System.Collections.Generic.List<SvgDiagnostic> diagnostics;

    internal SvgSupportsParserState(string value, System.Collections.Generic.List<SvgCssToken> originalTokens, System.Collections.Generic.List<SvgSupportsToken> significantTokens, SvgSourceUri sourceUri, SvgCssLimits cssLimits, int offset, int line, int column)
    {
        text = value;
        allTokens = originalTokens;
        tokens = significantTokens;
        source = sourceUri;
        limits = cssLimits;
        baseOffset = offset;
        baseLine = line;
        baseColumn = column;
        position = 0;
        conditionCount = 0;
        limitExceeded = false;
        diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
    }

    internal SvgSupportsParseResult Parse()
    {
        if (tokens.Count == 0)
        {
            AddError("A supports condition cannot be empty.", null);
            SvgSupportsQuery empty = new SvgSupportsQuery(null, text, diagnostics);
            return new SvgSupportsParseResult(true, empty, diagnostics, -1);
        }
        SvgSupportsCondition? condition = ParseCondition(tokens.Count, 0);
        if (limitExceeded) return new SvgSupportsParseResult(false, null, diagnostics, CurrentOffset());
        if (position != tokens.Count) AddError("Unexpected tokens follow the supports condition.", TokenSpan(tokens[position].Token));
        SvgSupportsQuery query = new SvgSupportsQuery(condition, text, diagnostics);
        return new SvgSupportsParseResult(true, query, diagnostics, -1);
    }

    SvgSupportsCondition? ParseCondition(int end, int depth)
    {
        if (depth > limits.MaxBlockDepth)
        {
            AddLimit("The supports condition exceeds MaxBlockDepth.", CurrentSpan());
            return null;
        }
        SvgSupportsCondition? first;
        int start = position;
        bool prefixNot = IsIdent(position, end, "not");
        if (prefixNot)
        {
            position = position + 1;
            SvgSupportsCondition? child = ParseInParens(end, depth + 1);
            first = NewCondition(SvgSupportsConditionKind.Not, start, position);
            if (child != null) first.Children.Add(child);
            first.IsValid = child != null && child.IsValid;
        }
        else first = ParseInParens(end, depth + 1);
        if (first == null) return null;
        string connective = "";
        System.Collections.Generic.List<SvgSupportsCondition> combined = new System.Collections.Generic.List<SvgSupportsCondition>();
        combined.Add(first);
        while (position < end && (IsIdent(position, end, "and") || IsIdent(position, end, "or")))
        {
            if (prefixNot)
            {
                AddError("A not condition cannot be combined with an and or or operator at the same level.", TokenSpan(tokens[position].Token));
                return InvalidCondition(start, position + 1);
            }
            string current = Lower(tokens[position].Token.Value);
            if (connective.Length == 0) connective = current;
            else if (connective != current)
            {
                AddError("And and or cannot be mixed at the same supports-condition level.", TokenSpan(tokens[position].Token));
                return InvalidCondition(start, position + 1);
            }
            position = position + 1;
            SvgSupportsCondition? next = ParseInParens(end, depth + 1);
            if (next == null) return InvalidCondition(start, position);
            combined.Add(next);
        }
        if (connective.Length == 0) return first;
        SvgSupportsCondition result = NewCondition(connective == "and" ? SvgSupportsConditionKind.And : SvgSupportsConditionKind.Or, start, position);
        int index = 0;
        result.IsValid = true;
        while (index < combined.Count)
        {
            result.Children.Add(combined[index]);
            if (!combined[index].IsValid) result.IsValid = false;
            index = index + 1;
        }
        return result;
    }

    SvgSupportsCondition? ParseInParens(int end, int depth)
    {
        if (position >= end)
        {
            AddError("A supports feature or nested condition is required.", CurrentSpan());
            return null;
        }
        int start = position;
        SvgCssToken token = tokens[position].Token;
        if (token.Kind == SvgCssTokenKind.Function)
        {
            int close = MatchingClose(position, end);
            if (close < 0)
            {
                AddError("A supports function has an unclosed parenthesis.", TokenSpan(token));
                position = end;
                return InvalidCondition(start, position);
            }
            position = close + 1;
            if (AsciiEquals(token.Value, "selector")) return SelectorCondition(start, close);
            SvgSupportsCondition unknownFunction = NewCondition(SvgSupportsConditionKind.Unknown, start, position);
            unknownFunction.IsValid = true;
            return unknownFunction;
        }
        if (token.Kind != SvgCssTokenKind.OpenParenthesis)
        {
            AddError("A supports condition must be enclosed in parentheses.", TokenSpan(token));
            position = position + 1;
            return InvalidCondition(start, position);
        }
        int closing = MatchingClose(position, end);
        if (closing < 0)
        {
            AddError("A supports condition has an unclosed parenthesis.", TokenSpan(token));
            position = end;
            return InvalidCondition(start, position);
        }
        int innerStart = position + 1;
        int innerEnd = closing;
        if (innerStart >= innerEnd)
        {
            position = closing + 1;
            AddError("A supports condition cannot be empty.", Span(tokens[start].Token, tokens[closing].Token));
            return InvalidCondition(start, position);
        }
        int colon = TopLevelColon(innerStart, innerEnd);
        if (colon >= 0)
        {
            position = closing + 1;
            return DeclarationCondition(start, closing, innerStart, innerEnd, colon);
        }
        bool nested = tokens[innerStart].Token.Kind == SvgCssTokenKind.OpenParenthesis || tokens[innerStart].Token.Kind == SvgCssTokenKind.Function || IsIdent(innerStart, innerEnd, "not");
        if (!nested)
        {
            position = closing + 1;
            SvgSupportsCondition unknown = NewCondition(SvgSupportsConditionKind.Unknown, start, position);
            unknown.IsValid = true;
            return unknown;
        }
        position = innerStart;
        SvgSupportsCondition? child = ParseCondition(innerEnd, depth + 1);
        if (limitExceeded)
        {
            position = closing + 1;
            return null;
        }
        if (position != innerEnd)
        {
            AddError("Unexpected tokens occur inside the nested supports condition.", CurrentSpan());
            position = closing + 1;
            return InvalidCondition(start, position);
        }
        position = closing + 1;
        if (child != null)
        {
            child.RawText = Slice(tokens[start].Token, tokens[closing].Token);
            child.Source = Span(tokens[start].Token, tokens[closing].Token);
        }
        return child;
    }

    SvgSupportsCondition DeclarationCondition(int outerStart, int close, int innerStart, int innerEnd, int colon)
    {
        if (colon != innerStart + 1 || tokens[innerStart].Token.Kind != SvgCssTokenKind.Ident || colon + 1 >= innerEnd)
        {
            AddError("A supports declaration requires one property name and a nonempty value.", Span(tokens[outerStart].Token, tokens[close].Token));
            return InvalidCondition(outerStart, close + 1);
        }
        SvgSupportsCondition condition = NewCondition(SvgSupportsConditionKind.Declaration, outerStart, close + 1);
        string name = tokens[innerStart].Token.Value;
        condition.PropertyName = name.StartsWith("--", System.StringComparison.Ordinal) ? name : Lower(name);
        condition.ValueText = text.Substring(tokens[colon + 1].Token.StartOffset, tokens[innerEnd - 1].Token.EndOffset - tokens[colon + 1].Token.StartOffset);
        condition.IsValid = true;
        return condition;
    }

    SvgSupportsCondition SelectorCondition(int start, int close)
    {
        SvgSupportsCondition condition = NewCondition(SvgSupportsConditionKind.Selector, start, close + 1);
        SvgCssToken function = tokens[start].Token;
        SvgCssToken closing = tokens[close].Token;
        condition.SelectorText = text.Substring(function.EndOffset, closing.StartOffset - function.EndOffset);
        if (tokens[start].OriginalIndex + 1 <= tokens[close].OriginalIndex)
        {
            SvgSelectorParseResult parsed = new SvgSelectorParser().ParseTokens(text, allTokens, tokens[start].OriginalIndex + 1, tokens[close].OriginalIndex, limits, source);
            condition.Selector = parsed.Selector;
        }
        condition.IsValid = true;
        return condition;
    }

    SvgSupportsCondition NewCondition(SvgSupportsConditionKind kind, int start, int end)
    {
        if (conditionCount >= limits.MaxDeclarations)
        {
            AddLimit("The supports condition count exceeds MaxDeclarations.", CurrentSpan());
            return new SvgSupportsCondition();
        }
        conditionCount = conditionCount + 1;
        SvgSupportsCondition condition = new SvgSupportsCondition();
        condition.Kind = kind;
        if (start < end && start >= 0 && end <= tokens.Count)
        {
            condition.RawText = Slice(tokens[start].Token, tokens[end - 1].Token);
            condition.Source = Span(tokens[start].Token, tokens[end - 1].Token);
        }
        return condition;
    }

    SvgSupportsCondition InvalidCondition(int start, int end)
    {
        SvgSupportsCondition condition = NewCondition(SvgSupportsConditionKind.Unknown, start, end);
        condition.IsValid = false;
        return condition;
    }

    int MatchingClose(int open, int end)
    {
        int depth = 0;
        int index = open;
        while (index < end)
        {
            SvgCssTokenKind kind = tokens[index].Token.Kind;
            if (kind == SvgCssTokenKind.OpenParenthesis || kind == SvgCssTokenKind.Function) depth = depth + 1;
            else if (kind == SvgCssTokenKind.CloseParenthesis)
            {
                depth = depth - 1;
                if (depth == 0) return index;
            }
            index = index + 1;
        }
        return -1;
    }

    int TopLevelColon(int start, int end)
    {
        int depth = 0;
        int index = start;
        while (index < end)
        {
            SvgCssTokenKind kind = tokens[index].Token.Kind;
            if (kind == SvgCssTokenKind.OpenParenthesis || kind == SvgCssTokenKind.Function || kind == SvgCssTokenKind.OpenSquare || kind == SvgCssTokenKind.OpenCurly) depth = depth + 1;
            else if (kind == SvgCssTokenKind.CloseParenthesis || kind == SvgCssTokenKind.CloseSquare || kind == SvgCssTokenKind.CloseCurly) depth = depth - 1;
            else if (kind == SvgCssTokenKind.Colon && depth == 0) return index;
            index = index + 1;
        }
        return -1;
    }

    void AddError(string message, SvgSourceSpan? span)
    {
        if (limitExceeded) return;
        diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InvalidCss, message, span));
    }

    void AddLimit(string message, SvgSourceSpan? span)
    {
        if (limitExceeded) return;
        limitExceeded = true;
        diagnostics.Add(new SvgDiagnostic(SvgDiagnosticSeverity.Error, SvgDiagnosticCode.InputLimitExceeded, message, span));
    }

    bool IsIdent(int index, int end, string value)
    {
        return index < end && tokens[index].Token.Kind == SvgCssTokenKind.Ident && AsciiEquals(tokens[index].Token.Value, value);
    }

    SvgSourceSpan? CurrentSpan()
    {
        return position < tokens.Count ? TokenSpan(tokens[position].Token) : null;
    }

    int CurrentOffset()
    {
        return position < tokens.Count ? tokens[position].Token.StartOffset : text.Length;
    }

    SvgSourceSpan TokenSpan(SvgCssToken token)
    {
        return Span(token, token);
    }

    SvgSourceSpan Span(SvgCssToken first, SvgCssToken last)
    {
        int startLine = baseLine + first.StartLine - 1;
        int startColumn = first.StartLine == 1 ? baseColumn + first.StartColumn - 1 : first.StartColumn;
        int endLine = baseLine + last.EndLine - 1;
        int endColumn = last.EndLine == 1 ? baseColumn + last.EndColumn - 1 : last.EndColumn;
        return new SvgSourceSpan(source, baseOffset + first.StartOffset, startLine, startColumn, baseOffset + last.EndOffset, endLine, endColumn);
    }

    string Slice(SvgCssToken first, SvgCssToken last)
    {
        return text.Substring(first.StartOffset, last.EndOffset - first.StartOffset);
    }

    static string Lower(string value)
    {
        return value.ToLowerInvariant();
    }

    static bool AsciiEquals(string left, string right)
    {
        return string.Equals(left, right, System.StringComparison.OrdinalIgnoreCase);
    }
}
