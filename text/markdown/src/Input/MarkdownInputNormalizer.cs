using System;

namespace markdown;

internal class MarkdownInputNormalizer
{
    internal const char NullReplacementCharacter = '\ufffd';

    internal static string Normalize(string source)
    {
        return ReplaceUnpairedSurrogates(ReplaceNullCharacters(source));
    }

    internal static string NormalizeMemory(ReadOnlyMemory<char> source)
    {
        ReadOnlySpan<char> span = source.Span;
        bool containsNull = false;
        int index = 0;
        while (index < span.Length)
        {
            if (span[index] == '\0')
            {
                containsNull = true;
                break;
            }

            index = index + 1;
        }

        string text = new string(span);
        if (containsNull)
        {
            text = text.Replace('\0', NullReplacementCharacter);
        }

        return ReplaceUnpairedSurrogates(text);
    }

    internal static string ReplaceNullCharacters(string source)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (!ContainsNullCharacter(source))
        {
            return source;
        }

        return source.Replace('\0', NullReplacementCharacter);
    }

    internal static bool ContainsNullCharacter(string source)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        return source.IndexOf('\0') >= 0;
    }

    internal static string ReplaceUnpairedSurrogates(string source)
    {
        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        bool containsUnpairedSurrogate = false;
        int index = 0;
        while (index < source.Length)
        {
            char character = source[index];
            if (char.IsHighSurrogate(character))
            {
                if (index + 1 < source.Length && char.IsLowSurrogate(source[index + 1]))
                {
                    index = index + 2;
                    continue;
                }

                containsUnpairedSurrogate = true;
                break;
            }

            if (char.IsLowSurrogate(character))
            {
                containsUnpairedSurrogate = true;
                break;
            }

            index = index + 1;
        }

        if (!containsUnpairedSurrogate)
        {
            return source;
        }

        char[] characters = source.ToCharArray();
        index = 0;
        while (index < characters.Length)
        {
            if (char.IsHighSurrogate(characters[index]))
            {
                if (index + 1 < characters.Length && char.IsLowSurrogate(characters[index + 1]))
                {
                    index = index + 2;
                    continue;
                }

                characters[index] = NullReplacementCharacter;
            }
            else if (char.IsLowSurrogate(characters[index]))
            {
                characters[index] = NullReplacementCharacter;
            }

            index = index + 1;
        }

        return new string(characters);
    }
}
