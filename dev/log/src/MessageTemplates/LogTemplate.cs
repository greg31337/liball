namespace log;

internal class LogTemplate
{
    internal string OriginalText;
    internal LogTemplateToken[] Tokens;
    internal int PlaceholderCount;
    internal int InvalidTokenCount;
    internal bool IsValid;
    internal string ErrorMessage;

    internal LogTemplate(string originalText, LogTemplateToken[] tokens)
    {
        OriginalText = LogResult.NormalizeText(originalText);
        Tokens = LogTemplate.CopyTokens(tokens);
        PlaceholderCount = LogTemplate.CountPlaceholders(Tokens);
        InvalidTokenCount = LogTemplate.CountInvalidTokens(tokens);
        IsValid = InvalidTokenCount == 0;
        ErrorMessage = "";

        if (!IsValid)
        {
            ErrorMessage = "template contains invalid tokens";
        }
    }

    internal static LogTemplate Empty()
    {
        return new LogTemplate("", new LogTemplateToken[0]);
    }

    internal static LogTemplate FromTokens(string originalText, LogTemplateToken[] tokens)
    {
        return new LogTemplate(originalText, tokens);
    }

    internal LogTemplateToken[] GetPlaceholders()
    {
        int count = LogTemplate.CountPlaceholders(Tokens);
        LogTemplateToken[] placeholders = new LogTemplateToken[count];
        int index = 0;
        foreach (LogTemplateToken token in Tokens)
        {
            if (token.IsPlaceholder)
            {
                placeholders[index] = LogTemplateToken.Copy(token);
                index = index + 1;
            }
        }

        return placeholders;
    }

    internal static LogTemplateToken[] CopyTokens(LogTemplateToken[] tokens)
    {
        if (tokens == null)
        {
            return new LogTemplateToken[0];
        }

        int count = 0;
        foreach (LogTemplateToken token in tokens)
        {
            if (token != null)
            {
                count = count + 1;
            }
        }

        LogTemplateToken[] copy = new LogTemplateToken[count];
        int index = 0;
        foreach (LogTemplateToken token in tokens)
        {
            if (token != null)
            {
                copy[index] = LogTemplateToken.Copy(token);
                index = index + 1;
            }
        }

        return copy;
    }

    internal static int CountPlaceholders(LogTemplateToken[] tokens)
    {
        if (tokens == null)
        {
            return 0;
        }

        int count = 0;
        foreach (LogTemplateToken token in tokens)
        {
            if (token != null && token.IsPlaceholder)
            {
                count = count + 1;
            }
        }

        return count;
    }

    internal static int CountInvalidTokens(LogTemplateToken[] tokens)
    {
        if (tokens == null)
        {
            return 0;
        }

        int count = 0;
        foreach (LogTemplateToken token in tokens)
        {
            if (token == null || !token.IsValid)
            {
                count = count + 1;
            }
        }

        return count;
    }
}
