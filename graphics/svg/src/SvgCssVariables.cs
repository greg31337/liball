namespace svg;

/// <summary>Tokenizes balanced custom-property component values.</summary>
internal static class SvgCssVariableTokenizer
{
    /// <summary>Tokenizes one exact custom-property value.</summary>
    /// <param name="name">The validated property name.</param>
    /// <param name="rawText">The raw value.</param>
    /// <param name="maxTokens">The maximum token count.</param>
    /// <param name="maxTokenCharacters">The maximum token length.</param>
    /// <returns>A stored value or stable failure.</returns>
    public static SvgCustomPropertyStoreResult Tokenize(string name, string? rawText, int maxTokens, int maxTokenCharacters)
    {
        if (rawText == null || maxTokens < 1 || maxTokenCharacters < 1) return Failure(SvgCssVariableError.InvalidArgument, 0);
        System.Collections.Generic.List<SvgCssStoredToken> tokens = new System.Collections.Generic.List<SvgCssStoredToken>();
        System.Collections.Generic.List<char> brackets = new System.Collections.Generic.List<char>();
        int position = 0;
        while (position < rawText.Length)
        {
            if (tokens.Count >= maxTokens) return Failure(SvgCssVariableError.TokenLimitExceeded, position);
            int start = position;
            SvgCssStoredTokenKind kind;
            char current = rawText[position];
            if (SvgSpanCharacters.IsCssWhitespace(current))
            {
                kind = SvgCssStoredTokenKind.Whitespace;
                while (position < rawText.Length && SvgSpanCharacters.IsCssWhitespace(rawText[position])) position = position + 1;
            }
            else if (current == '\'' || current == '"')
            {
                kind = SvgCssStoredTokenKind.String;
                position = ScanString(rawText, position);
                if (position < 0) return Failure(SvgCssVariableError.InvalidTokens, start);
            }
            else if (current == '/' && position + 1 < rawText.Length && rawText[position + 1] == '*')
            {
                kind = SvgCssStoredTokenKind.Comment;
                position = ScanComment(rawText, position);
                if (position < 0) return Failure(SvgCssVariableError.InvalidTokens, start);
            }
            else if (SvgCssVariableRules.IsNameCharacter(current))
            {
                kind = current >= '0' && current <= '9' || current == '.' ? SvgCssStoredTokenKind.Numeric : SvgCssStoredTokenKind.Identifier;
                while (position < rawText.Length && (SvgCssVariableRules.IsNameCharacter(rawText[position]) || rawText[position] == '%' || rawText[position] == '.')) position = position + 1;
            }
            else
            {
                kind = SvgCssStoredTokenKind.Delimiter;
                position = position + 1;
                if (current == '(' || current == '[' || current == '{') brackets.Add(current);
                else if (current == ')' || current == ']' || current == '}')
                {
                    if (brackets.Count == 0 || !Matches(brackets[brackets.Count - 1], current)) return Failure(SvgCssVariableError.InvalidTokens, start);
                    brackets.RemoveAt(brackets.Count - 1);
                }
            }
            if (position - start > maxTokenCharacters) return Failure(SvgCssVariableError.TokenTooLong, start + maxTokenCharacters);
            tokens.Add(new SvgCssStoredToken(kind, rawText.Substring(start, position - start), start, position));
        }
        if (brackets.Count != 0) return Failure(SvgCssVariableError.InvalidTokens, rawText.Length);
        return new SvgCustomPropertyStoreResult(true, SvgCssVariableError.None, new SvgCustomPropertyValue(name, rawText, tokens), -1);
    }

    static int ScanString(string text, int start)
    {
        char quote = text[start];
        int position = start + 1;
        while (position < text.Length)
        {
            char current = text[position];
            if (current == quote) return position + 1;
            if (current == '\n' || current == '\r' || current == '\f') return -1;
            if (current == '\\')
            {
                position = position + 1;
                if (position >= text.Length || text[position] == '\n' || text[position] == '\r' || text[position] == '\f') return -1;
            }
            position = position + 1;
        }
        return -1;
    }

    static int ScanComment(string text, int start)
    {
        int position = start + 2;
        while (position + 1 < text.Length)
        {
            if (text[position] == '*' && text[position + 1] == '/') return position + 2;
            position = position + 1;
        }
        return -1;
    }

    static bool Matches(char open, char close)
    {
        return open == '(' && close == ')' || open == '[' && close == ']' || open == '{' && close == '}';
    }

    static SvgCustomPropertyStoreResult Failure(SvgCssVariableError error, int offset)
    {
        return new SvgCustomPropertyStoreResult(false, error, null, offset);
    }
}

/// <summary>Stores one internal recursive resolution result.</summary>
internal class SvgCssVariableResolveWork
{
    /// <summary>Whether resolution succeeded.</summary>
    public bool Success;
    /// <summary>The failure kind.</summary>
    public SvgCssVariableError Error;
    /// <summary>The resolved text.</summary>
    public string Text;
    /// <summary>The failure offset.</summary>
    public int ErrorOffset;
    /// <summary>The first repeated custom-property name for a cycle failure.</summary>
    public string CycleName;

    /// <summary>Creates a recursive resolution result.</summary>
    /// <param name="success">Whether resolution succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="text">The resolved text.</param>
    /// <param name="errorOffset">The failure offset.</param>
    /// <param name="cycleName">The first repeated name for a cycle failure.</param>
    internal SvgCssVariableResolveWork(bool success, SvgCssVariableError error, string text, int errorOffset, string cycleName)
    {
        Success = success;
        Error = error;
        Text = text;
        ErrorOffset = errorOffset;
        CycleName = cycleName;
    }
}

/// <summary>Provides CSS custom-property name and token character rules.</summary>
internal static class SvgCssVariableRules
{
    /// <summary>Tests a custom-property name beginning with two hyphens.</summary>
    /// <param name="name">The candidate name.</param>
    /// <returns><c>true</c> for a supported custom-property identifier.</returns>
    public static bool IsCustomName(string? name)
    {
        if (name == null || name.Length < 3 || name[0] != '-' || name[1] != '-') return false;
        int position = 2;
        while (position < name.Length)
        {
            if (!IsNameCharacter(name[position])) return false;
            position = position + 1;
        }
        return true;
    }

    /// <summary>Tests a supported CSS identifier character.</summary>
    /// <param name="value">The character.</param>
    /// <returns><c>true</c> for a name character.</returns>
    public static bool IsNameCharacter(char value)
    {
        return value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z' || value >= '0' && value <= '9' || value == '-' || value == '_' || value >= 128;
    }
}
