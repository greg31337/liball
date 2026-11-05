using System.Collections.Generic;
using System.Text;

namespace log;

internal class LogTemplateParser
{
    internal LogTemplate Parse(string? template)
    {
        string text = LogResult.NormalizeText(template);
        List<LogTemplateToken> tokens = new List<LogTemplateToken>();
        StringBuilder literal = new StringBuilder();
        int literalStart = 0;
        int literalSourceLength = 0;
        int placeholderPosition = 0;
        int index = 0;

        while (index < text.Length)
        {
            char current = text[index];

            if (current == '{')
            {
                if (LogTemplateParser.HasNext(text, index, '{'))
                {
                    if (literal.Length == 0)
                    {
                        literalStart = index;
                    }

                    literal.Append('{');
                    literalSourceLength = literalSourceLength + 2;
                    index = index + 2;
                    continue;
                }

                LogTemplateParser.FlushLiteral(tokens, literal, literalStart, literalSourceLength);
                literalSourceLength = 0;

                int endIndex = LogTemplateParser.FindClosingBrace(text, index + 1);
                if (endIndex < 0)
                {
                    tokens.Add(LogTemplateToken.Placeholder("", placeholderPosition, index, text.Length - index));
                    placeholderPosition = placeholderPosition + 1;
                    index = text.Length;
                    continue;
                }

                string content = text.Substring(index + 1, endIndex - index - 1);
                tokens.Add(LogTemplateParser.ParsePlaceholderContent(content, placeholderPosition, index, endIndex - index + 1));
                placeholderPosition = placeholderPosition + 1;
                index = endIndex + 1;
                continue;
            }

            if (current == '}')
            {
                if (LogTemplateParser.HasNext(text, index, '}'))
                {
                    if (literal.Length == 0)
                    {
                        literalStart = index;
                    }

                    literal.Append('}');
                    literalSourceLength = literalSourceLength + 2;
                    index = index + 2;
                    continue;
                }

                LogTemplateParser.FlushLiteral(tokens, literal, literalStart, literalSourceLength);
                literalSourceLength = 0;
                tokens.Add(new LogTemplateToken(false, false, "", "", "", false, false, -1, index, 1));
                index = index + 1;
                continue;
            }

            if (literal.Length == 0)
            {
                literalStart = index;
            }

            literal.Append(current);
            literalSourceLength = literalSourceLength + 1;
            index = index + 1;
        }

        LogTemplateParser.FlushLiteral(tokens, literal, literalStart, literalSourceLength);

        return new LogTemplate(text, tokens.ToArray());
    }

    internal static LogTemplate ParseTemplate(string? template)
    {
        LogTemplateParser parser = new LogTemplateParser();
        return parser.Parse(template);
    }

    internal static bool HasNext(string text, int index, char expected)
    {
        int nextIndex = index + 1;
        if (nextIndex >= text.Length)
        {
            return false;
        }

        return text[nextIndex] == expected;
    }

    internal static int FindClosingBrace(string text, int startIndex)
    {
        int index = startIndex;
        while (index < text.Length)
        {
            char current = text[index];
            if (current == '{')
            {
                return -1;
            }

            if (current == '}')
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static LogTemplateToken ParsePlaceholderContent(string content, int position, int startIndex, int length)
    {
        string text = LogResult.NormalizeText(content);
        bool isDestructured = false;
        bool isStringified = false;
        int nameStart = 0;

        if (text.Length > 0)
        {
            if (text[0] == '@')
            {
                isDestructured = true;
                nameStart = 1;
            }

            if (text[0] == '$')
            {
                isStringified = true;
                nameStart = 1;
            }
        }

        int colonIndex = text.IndexOf(':', nameStart);
        string name = "";
        string format = "";

        if (colonIndex >= 0)
        {
            name = text.Substring(nameStart, colonIndex - nameStart);
            format = text.Substring(colonIndex + 1);
        }
        else
        {
            name = text.Substring(nameStart);
        }

        return LogTemplateToken.PlaceholderFormat(name, format, isDestructured, isStringified, position, startIndex, length);
    }

    internal static void FlushLiteral(List<LogTemplateToken> tokens, StringBuilder literal, int literalStart, int literalSourceLength)
    {
        if (literal.Length == 0)
        {
            return;
        }

        tokens.Add(LogTemplateToken.LiteralSpan(literal.ToString(), literalStart, literalSourceLength));
        literal.Clear();
    }
}
