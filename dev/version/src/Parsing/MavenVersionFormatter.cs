namespace version;

internal static class MavenVersionFormatter
{
    internal static string FormatNormalized(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        return FormatTokens(MavenVersionParser.ParseTokens(text));
    }

    internal static string FormatCanonical(string text)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        return FormatTokens(MavenVersionParser.ParseTokens(text));
    }

    internal static string FormatTokens(MavenVersionToken[] tokens)
    {
        System.ArgumentNullException.ThrowIfNull(tokens);

        int length = TrimTrailingNullTokens(tokens);
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;

        while (index < length)
        {
            string tokenText = FormatToken(tokens[index]);
            if (tokenText.Length > 0)
            {
                if (builder.Length > 0 && tokens[index].HasSeparatorBefore())
                {
                    builder.Append(NormalizeSeparator(tokens[index].GetSeparatorBefore()));
                }

                builder.Append(tokenText);
            }

            index = index + 1;
        }

        if (builder.Length == 0)
        {
            return "0";
        }

        return builder.ToString();
    }

    internal static int TrimTrailingNullTokens(MavenVersionToken[] tokens)
    {
        int length = tokens.Length;

        while (length > 1 && IsNullToken(tokens[length - 1]))
        {
            length = length - 1;
        }

        return length;
    }

    internal static bool IsNullToken(MavenVersionToken token)
    {
        if (token.IsNumeric())
        {
            return VersionFormatter.NormalizeNumericText(token.GetText()) == "0";
        }

        return MavenQualifierComparer.NormalizeQualifier(token.GetText()) == "";
    }

    internal static string FormatToken(MavenVersionToken token)
    {
        if (token.IsNumeric())
        {
            return VersionFormatter.NormalizeNumericText(token.GetText());
        }

        return MavenQualifierComparer.NormalizeQualifier(token.GetText());
    }

    internal static char NormalizeSeparator(char separator)
    {
        if (separator == '_')
        {
            return '-';
        }

        return separator;
    }
}
