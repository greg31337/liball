using System;
using System.Text;

namespace markdown;

internal class HtmlEscaper
{
    internal static string EscapeText(string text)
    {
        return Escape(text, true, false);
    }

    internal static string EscapeAttribute(string text)
    {
        return Escape(text, true, false);
    }

    internal static string EscapeAttributeWithApostrophes(string text)
    {
        return Escape(text, true, true);
    }

    internal static string Escape(string text, bool escapeDoubleQuote, bool escapeApostrophe)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        StringBuilder builder = new StringBuilder(text.Length);
        int index = 0;

        while (index < text.Length)
        {
            char character = text[index];
            if (character == '&')
            {
                builder.Append("&amp;");
            }
            else if (character == '<')
            {
                builder.Append("&lt;");
            }
            else if (character == '>')
            {
                builder.Append("&gt;");
            }
            else if (character == '"' && escapeDoubleQuote)
            {
                builder.Append("&quot;");
            }
            else if (character == '\'' && escapeApostrophe)
            {
                builder.Append("&#39;");
            }
            else
            {
                builder.Append(character);
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    internal static string EscapeUrl(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        StringBuilder builder = new StringBuilder(text.Length);
        int index = 0;
        while (index < text.Length)
        {
            char character = text[index];
            if (ShouldPercentEncodeUrlCharacter(character))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(character.ToString());
                int byteIndex = 0;
                while (byteIndex < bytes.Length)
                {
                    builder.Append('%');
                    builder.Append(bytes[byteIndex].ToString("X2"));
                    byteIndex = byteIndex + 1;
                }
            }
            else
            {
                builder.Append(character);
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    internal static bool ShouldPercentEncodeUrlCharacter(char character)
    {
        return character <= ' '
            || character >= '\u007f'
            || character == '"'
            || character == '<'
            || character == '>'
            || character == '\\'
            || character == '['
            || character == ']'
            || character == '`'
            || character == '^'
            || character == '{'
            || character == '|'
            || character == '}';
    }
}
