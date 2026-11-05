using System;

namespace uuid;

internal class UuidParser
{
    internal static UuidParseResult Parse(string? text)
    {
        if (text == null)
        {
            return UuidParseResult.Failure(IdentifierErrorCode.NullInput, "UUID text is null");
        }

        if (IsUrn(text))
        {
            string urnValue = text.Substring(9);
            if (urnValue.Length != 36)
            {
                return UuidParseResult.Failure(IdentifierErrorCode.InvalidLength, "UUID URN must contain canonical UUID text");
            }

            return ParseCanonical(urnValue);
        }

        if (text.Length == 32)
        {
            return ParseCompact(text);
        }

        if (text.Length == 36)
        {
            return ParseCanonical(text);
        }

        return UuidParseResult.Failure(IdentifierErrorCode.InvalidLength, "UUID text length must be 32 compact hex characters, 36 canonical characters, or urn:uuid: plus canonical UUID");
    }

    internal static UuidParseResult ParseCompact(string? text)
    {
        if (text == null)
        {
            return UuidParseResult.Failure(IdentifierErrorCode.NullInput, "UUID text is null");
        }

        if (text.Length != 32)
        {
            return UuidParseResult.Failure(IdentifierErrorCode.InvalidLength, "compact UUID text must be 32 hex characters");
        }

        int index = 0;
        while (index < text.Length)
        {
            if (!HexTools.IsHex(text[index]))
            {
                return UuidParseResult.Failure(IdentifierErrorCode.InvalidCharacter, "invalid UUID hex character at index " + index.ToString());
            }

            index = index + 1;
        }

        byte[] bytes = HexTools.ParseUuidCompact(text);
        return UuidParseResult.Success(new UuidValue(bytes));
    }

    internal static UuidParseResult ParseCanonical(string? text)
    {
        if (text == null)
        {
            return UuidParseResult.Failure(IdentifierErrorCode.NullInput, "UUID text is null");
        }

        if (text.Length != 36)
        {
            return UuidParseResult.Failure(IdentifierErrorCode.InvalidLength, "canonical UUID text must be 36 characters");
        }

        if (!HasCanonicalHyphens(text))
        {
            return UuidParseResult.Failure(IdentifierErrorCode.InvalidCharacter, "canonical UUID hyphens are not in expected positions");
        }

        char[] compact = new char[32];
        int textIndex = 0;
        int compactIndex = 0;
        while (textIndex < text.Length)
        {
            if (text[textIndex] != '-')
            {
                if (!HexTools.IsHex(text[textIndex]))
                {
                    return UuidParseResult.Failure(IdentifierErrorCode.InvalidCharacter, "invalid UUID hex character at index " + textIndex.ToString());
                }

                compact[compactIndex] = text[textIndex];
                compactIndex = compactIndex + 1;
            }

            textIndex = textIndex + 1;
        }

        byte[] bytes = HexTools.ParseUuidCompact(new string(compact));
        return UuidParseResult.Success(new UuidValue(bytes));
    }

    internal static UuidValue ParseValue(string? text)
    {
        UuidParseResult result = Parse(text);
        if (!result.Ok || result.Value == null)
        {
            throw new IdentifierException(result.Code, result.Error);
        }

        return result.Value;
    }

    internal static bool IsUrn(string? text)
    {
        if (text == null)
        {
            return false;
        }

        return text.StartsWith("urn:uuid:", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool HasCanonicalHyphens(string text)
    {
        if (text.Length != 36)
        {
            return false;
        }

        int index = 0;
        while (index < text.Length)
        {
            bool shouldBeHyphen = index == 8 || index == 13 || index == 18 || index == 23;
            if (shouldBeHyphen)
            {
                if (text[index] != '-')
                {
                    return false;
                }
            }
            else if (text[index] == '-')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }
}
