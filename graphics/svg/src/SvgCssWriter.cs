namespace svg;

internal static class SvgCssWriter
{
    internal static string FormatStyleSheet(string value, SvgWriteMode mode, SvgValueFormatOptions numbers, SvgValueFormatOptions colors, string indentText, string newLine, int maximumTokens, long maximumCharacters)
    {
        SvgCssLimits limits = Limits(maximumTokens, maximumCharacters);
        SvgCssParseResult parsed = new SvgCssParser().ParseStyleSheetWithValueAndLimits(value, limits);
        if (!parsed.Success || parsed.StyleSheet == null || parsed.UnknownSyntax.Count != 0) return Format(value, mode, numbers, indentText, newLine, maximumTokens, maximumCharacters, mode == SvgWriteMode.Pretty);
        System.Text.StringBuilder generated = new System.Text.StringBuilder();
        int index = 0;
        while (index < parsed.StyleSheet.Rules.Count)
        {
            SvgCssRule rule = parsed.StyleSheet.Rules[index];
            if (rule.Kind == SvgCssRuleKind.Style && rule.IsValid)
            {
                int preludeIndex = 0;
                while (preludeIndex < rule.Prelude.Count)
                {
                    generated.Append(rule.Prelude[preludeIndex].RawText);
                    preludeIndex = preludeIndex + 1;
                }
                generated.Append('{');
                generated.Append(FormatDeclarations(rule.Declarations, mode, numbers, colors, maximumTokens, maximumCharacters));
                generated.Append('}');
            }
            else generated.Append(rule.RawText);
            index = index + 1;
        }
        return Format(generated.ToString(), mode, numbers, indentText, newLine, maximumTokens, maximumCharacters, mode == SvgWriteMode.Pretty);
    }

    internal static string FormatDeclarationList(string value, SvgWriteMode mode, SvgValueFormatOptions numbers, SvgValueFormatOptions colors, int maximumTokens, long maximumCharacters)
    {
        SvgCssParseResult parsed = new SvgCssParser().ParseStyleAttributeWithValueAndLimits(value, Limits(maximumTokens, maximumCharacters));
        if (!parsed.Success || parsed.UnknownSyntax.Count != 0) return Format(value, mode, numbers, "", "", maximumTokens, maximumCharacters, false);
        return FormatDeclarations(parsed.Declarations, mode, numbers, colors, maximumTokens, maximumCharacters);
    }

    static string FormatDeclarations(System.Collections.Generic.List<SvgCssDeclaration> declarations, SvgWriteMode mode, SvgValueFormatOptions numbers, SvgValueFormatOptions colors, int maximumTokens, long maximumCharacters)
    {
        SvgPropertyCatalog properties = SvgPropertyCatalog.CreateDefault();
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        int index = 0;
        while (index < declarations.Count)
        {
            SvgCssDeclaration declaration = declarations[index];
            if (index != 0) result.Append(';');
            result.Append(declaration.Name);
            result.Append(':');
            string value = declaration.ValueText.Trim();
            SvgPropertySupportEntry? property = properties.FindPresentationAttribute(declaration.Name);
            string? color = null;
            if (property != null && (property.ValueKind == SvgPropertyValueSupportKind.Color || property.ValueKind == SvgPropertyValueSupportKind.Paint)) color = SvgValueCanonicalizer.Color(value, colors);
            result.Append(color == null ? Format(value, mode, numbers, "", "", maximumTokens, maximumCharacters, false) : color);
            if (declaration.Important) result.Append("!important");
            index = index + 1;
        }
        if (declarations.Count != 0) result.Append(';');
        return result.ToString();
    }

    static SvgCssLimits Limits(int maximumTokens, long maximumCharacters)
    {
        SvgCssLimits limits = new SvgCssLimits();
        limits.MaxTokens = maximumTokens < limits.MaxTokens ? maximumTokens : limits.MaxTokens;
        limits.MaxCharacters = maximumCharacters < limits.MaxCharacters ? maximumCharacters : limits.MaxCharacters;
        limits.MaxDeclarations = maximumTokens < limits.MaxDeclarations ? maximumTokens : limits.MaxDeclarations;
        limits.MaxRules = maximumTokens < limits.MaxRules ? maximumTokens : limits.MaxRules;
        return limits;
    }

    static string Format(string value, SvgWriteMode mode, SvgValueFormatOptions numbers, string indentText, string newLine, int maximumTokens, long maximumCharacters, bool pretty)
    {
        SvgCssTokenizeResult tokenized = new SvgCssTokenizer().TokenizeWithValueAndMaxTokensAndMaxCharacters(value, maximumTokens, maximumCharacters);
        if (!tokenized.Success || tokenized.ParseErrorCount != 0) return value;
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        SvgCssToken? previous = null;
        bool pendingWhitespace = false;
        bool lineStart = true;
        int depth = 0;
        int index = 0;
        while (index < tokenized.Tokens.Count)
        {
            SvgCssToken token = tokenized.Tokens[index];
            if (token.Kind == SvgCssTokenKind.Whitespace || token.Kind == SvgCssTokenKind.Comment)
            {
                pendingWhitespace = true;
                index = index + 1;
                continue;
            }
            if (pretty && token.Kind == SvgCssTokenKind.CloseCurly)
            {
                if (!lineStart)
                {
                    TrimTrailingWhitespace(result);
                    AppendNewLine(result, newLine);
                }
                depth = depth > 0 ? depth - 1 : 0;
                AppendIndent(result, indentText, depth);
                result.Append('}');
                lineStart = false;
                pendingWhitespace = false;
                previous = token;
                if (NextSignificantKind(tokenized.Tokens, index + 1) != SvgCssTokenKind.Semicolon && NextSignificantKind(tokenized.Tokens, index + 1) != SvgCssTokenKind.CloseCurly)
                {
                    AppendNewLine(result, newLine);
                    lineStart = true;
                }
                index = index + 1;
                continue;
            }
            if (pretty && token.Kind == SvgCssTokenKind.OpenCurly)
            {
                TrimTrailingWhitespace(result);
                if (result.Length != 0 && !lineStart) result.Append(' ');
                result.Append('{');
                AppendNewLine(result, newLine);
                depth = depth + 1;
                lineStart = true;
                pendingWhitespace = false;
                previous = token;
                index = index + 1;
                continue;
            }
            if (pretty && token.Kind == SvgCssTokenKind.Semicolon)
            {
                TrimTrailingWhitespace(result);
                result.Append(';');
                AppendNewLine(result, newLine);
                lineStart = true;
                pendingWhitespace = false;
                previous = token;
                index = index + 1;
                continue;
            }
            if (lineStart && pretty)
            {
                AppendIndent(result, indentText, depth);
                lineStart = false;
            }
            if (pendingWhitespace && NeedsWhitespace(previous, token)) result.Append(' ');
            AppendToken(result, token, numbers);
            pendingWhitespace = false;
            previous = token;
            index = index + 1;
        }
        TrimTrailingWhitespace(result);
        return result.ToString();
    }

    static void AppendToken(System.Text.StringBuilder result, SvgCssToken token, SvgValueFormatOptions numbers)
    {
        if (token.Kind == SvgCssTokenKind.Number || token.Kind == SvgCssTokenKind.Percentage || token.Kind == SvgCssTokenKind.Dimension)
        {
            string? formatted = SvgValueCanonicalizer.Number(token.NumberValue, numbers);
            if (formatted != null)
            {
                result.Append(formatted);
                if (token.Kind != SvgCssTokenKind.Number) result.Append(token.RawText.Substring(token.Value.Length));
                return;
            }
        }
        result.Append(token.RawText);
    }

    static bool NeedsWhitespace(SvgCssToken? previous, SvgCssToken current)
    {
        if (previous == null) return false;
        if (current.Kind == SvgCssTokenKind.Colon || current.Kind == SvgCssTokenKind.Semicolon || current.Kind == SvgCssTokenKind.Comma || current.Kind == SvgCssTokenKind.CloseParenthesis || current.Kind == SvgCssTokenKind.CloseSquare || current.Kind == SvgCssTokenKind.OpenCurly || current.Kind == SvgCssTokenKind.CloseCurly) return false;
        if (previous.Kind == SvgCssTokenKind.Colon || previous.Kind == SvgCssTokenKind.Semicolon || previous.Kind == SvgCssTokenKind.Comma || previous.Kind == SvgCssTokenKind.Function || previous.Kind == SvgCssTokenKind.OpenParenthesis || previous.Kind == SvgCssTokenKind.OpenSquare || previous.Kind == SvgCssTokenKind.OpenCurly || previous.Kind == SvgCssTokenKind.CloseCurly) return false;
        return true;
    }

    static SvgCssTokenKind NextSignificantKind(System.Collections.Generic.List<SvgCssToken> tokens, int start)
    {
        int index = start;
        while (index < tokens.Count)
        {
            SvgCssTokenKind kind = tokens[index].Kind;
            if (kind != SvgCssTokenKind.Whitespace && kind != SvgCssTokenKind.Comment) return kind;
            index = index + 1;
        }
        return (SvgCssTokenKind)(-1);
    }

    static void AppendIndent(System.Text.StringBuilder result, string indentText, int depth)
    {
        int index = 0;
        while (index < depth)
        {
            result.Append(indentText);
            index = index + 1;
        }
    }

    static void AppendNewLine(System.Text.StringBuilder result, string newLine)
    {
        if (newLine.Length == 0) return;
        result.Append(newLine);
    }

    static void TrimTrailingWhitespace(System.Text.StringBuilder result)
    {
        while (result.Length != 0)
        {
            char value = result[result.Length - 1];
            if (value != ' ' && value != '\t' && value != '\r' && value != '\n') return;
            result.Length = result.Length - 1;
        }
    }
}
