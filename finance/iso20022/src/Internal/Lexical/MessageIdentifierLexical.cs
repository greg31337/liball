using System;

namespace iso20022;

internal static class MessageIdentifierLexical
{
    internal static MessageIdentifier? ParseMessageIdentifier(ReadOnlySpan<char> text)
    {
        if (text.Length != 15)
        {
            return null;
        }

        if (text[4] != '.' || text[8] != '.' || text[12] != '.')
        {
            return null;
        }

        ReadOnlySpan<char> businessArea = text.Slice(0, 4);
        ReadOnlySpan<char> functionality = text.Slice(5, 3);
        ReadOnlySpan<char> variant = text.Slice(9, 3);
        ReadOnlySpan<char> version = text.Slice(13, 2);
        if (!IsLowerAsciiLetters(businessArea))
        {
            return null;
        }

        if (!IsAsciiDigits(functionality) || !IsAsciiDigits(variant) || !IsAsciiDigits(version))
        {
            return null;
        }

        MessageIdentifier result = new MessageIdentifier();
        result.BusinessArea = businessArea.ToString();
        result.MessageFunctionality = functionality.ToString();
        result.Variant = variant.ToString();
        result.Version = version.ToString();
        result.Value = text.ToString();
        return result;
    }

    internal static string? FormatMessageIdentifier(MessageIdentifier identifier)
    {
        if (identifier == null)
        {
            return null;
        }

        string value = $"{identifier.BusinessArea}.{identifier.MessageFunctionality}.{identifier.Variant}.{identifier.Version}";
        MessageIdentifier? parsed = ParseMessageIdentifier(value.AsSpan());
        if (parsed == null)
        {
            return null;
        }

        return value;
    }

    internal static bool IsLowerAsciiLetters(ReadOnlySpan<char> text)
    {
        int index = 0;
        while (index < text.Length)
        {
            char character = text[index];
            if (character < 'a' || character > 'z')
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal static bool IsAsciiDigits(ReadOnlySpan<char> text)
    {
        int index = 0;
        while (index < text.Length)
        {
            char character = text[index];
            if (character < '0' || character > '9')
            {
                return false;
            }

            index++;
        }

        return true;
    }
}
