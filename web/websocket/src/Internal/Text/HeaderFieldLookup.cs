using System;

namespace websocket;

internal class HeaderFieldLookupResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.ProtocolFailure;
    internal WebSocketHeaderField[] Matches = Array.Empty<WebSocketHeaderField>();
    internal string CombinedValue = "";
}

internal class HeaderTokenValuesResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.ProtocolFailure;
    internal int FieldCount;
    internal string[] Tokens = Array.Empty<string>();
}

internal class HeaderTokenSearchResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.ProtocolFailure;
    internal int FieldCount;
    internal int TokenCount;
    internal bool Found;
}

internal static class HeaderFieldLookup
{
    internal static HeaderFieldLookupResult FindFields(
        WebSocketHeaderField[]? fields,
        ReadOnlySpan<char> fieldName)
    {
        HeaderFieldLookupResult result = new HeaderFieldLookupResult();
        if (!AsciiToken.IsValid(fieldName))
        {
            return result;
        }

        if (fields == null)
        {
            result.Status = WebSocketParseStatus.Complete;
            return result;
        }

        int matchCount = 0;
        int fieldIndex = 0;
        while (fieldIndex < fields.Length)
        {
            WebSocketHeaderField field = fields[fieldIndex];
            if (field == null ||
                field.Name == null ||
                field.Value == null ||
                !AsciiToken.IsValid(field.Name) ||
                !HttpFieldValue.IsSafe(field.Value))
            {
                return result;
            }

            if (AsciiToken.EqualsIgnoreCase(field.Name, fieldName))
            {
                matchCount = matchCount + 1;
            }

            fieldIndex = fieldIndex + 1;
        }

        WebSocketHeaderField[] matches = new WebSocketHeaderField[matchCount];
        fieldIndex = 0;
        int matchIndex = 0;
        while (fieldIndex < fields.Length)
        {
            WebSocketHeaderField field = fields[fieldIndex];
            if (AsciiToken.EqualsIgnoreCase(field.Name, fieldName))
            {
                matches[matchIndex] = field;
                matchIndex = matchIndex + 1;
            }

            fieldIndex = fieldIndex + 1;
        }

        result.Status = WebSocketParseStatus.Complete;
        result.Matches = matches;
        return result;
    }

    internal static HeaderFieldLookupResult CombineListFields(
        WebSocketHeaderField[]? fields,
        ReadOnlySpan<char> fieldName)
    {
        HeaderFieldLookupResult result = FindFields(fields, fieldName);
        if (result.Status != WebSocketParseStatus.Complete || result.Matches.Length == 0)
        {
            return result;
        }

        int combinedLength = 0;
        int matchIndex = 0;
        while (matchIndex < result.Matches.Length)
        {
            string value = result.Matches[matchIndex].Value;
            int separatorLength = 0;
            if (matchIndex != 0)
            {
                separatorLength = 2;
            }

            if (combinedLength > int.MaxValue - separatorLength ||
                combinedLength + separatorLength > int.MaxValue - value.Length)
            {
                result.Status = WebSocketParseStatus.LimitRejected;
                result.CombinedValue = "";
                return result;
            }

            combinedLength = combinedLength + separatorLength + value.Length;
            matchIndex = matchIndex + 1;
        }

        char[] combined = new char[combinedLength];
        int destinationIndex = 0;
        matchIndex = 0;
        while (matchIndex < result.Matches.Length)
        {
            if (matchIndex != 0)
            {
                combined[destinationIndex] = ',';
                combined[destinationIndex + 1] = ' ';
                destinationIndex = destinationIndex + 2;
            }

            string value = result.Matches[matchIndex].Value;
            value.AsSpan().CopyTo(combined.AsSpan(destinationIndex));
            destinationIndex = destinationIndex + value.Length;
            matchIndex = matchIndex + 1;
        }

        result.CombinedValue = new string(combined);
        return result;
    }

    internal static HeaderTokenValuesResult ReadTokenValues(
        WebSocketHeaderField[]? fields,
        ReadOnlySpan<char> fieldName,
        int maximumTokenCount)
    {
        HeaderTokenValuesResult result = new HeaderTokenValuesResult();
        HeaderFieldLookupResult lookup = CombineListFields(fields, fieldName);
        result.FieldCount = lookup.Matches.Length;
        if (lookup.Status != WebSocketParseStatus.Complete)
        {
            result.Status = lookup.Status;
            return result;
        }

        HeaderTokenListResult tokens = HeaderListParser.ParseTokenList(
            lookup.CombinedValue,
            maximumTokenCount);
        result.Status = tokens.Status;
        if (tokens.Status == WebSocketParseStatus.Complete)
        {
            result.Tokens = tokens.Elements;
        }

        return result;
    }

    internal static HeaderTokenSearchResult ContainsHttpToken(
        WebSocketHeaderField[]? fields,
        ReadOnlySpan<char> fieldName,
        ReadOnlySpan<char> candidate,
        int maximumTokenCount)
    {
        return SearchTokens(fields, fieldName, candidate, maximumTokenCount, false);
    }

    internal static HeaderTokenSearchResult ContainsSubprotocol(
        WebSocketHeaderField[]? fields,
        ReadOnlySpan<char> fieldName,
        ReadOnlySpan<char> candidate,
        int maximumTokenCount)
    {
        return SearchTokens(fields, fieldName, candidate, maximumTokenCount, true);
    }

    static HeaderTokenSearchResult SearchTokens(
        WebSocketHeaderField[]? fields,
        ReadOnlySpan<char> fieldName,
        ReadOnlySpan<char> candidate,
        int maximumTokenCount,
        bool caseSensitive)
    {
        HeaderTokenSearchResult result = new HeaderTokenSearchResult();
        if (!AsciiToken.IsValid(candidate))
        {
            return result;
        }

        HeaderTokenValuesResult values = ReadTokenValues(fields, fieldName, maximumTokenCount);
        result.Status = values.Status;
        result.FieldCount = values.FieldCount;
        result.TokenCount = values.Tokens.Length;
        if (values.Status != WebSocketParseStatus.Complete)
        {
            return result;
        }

        int tokenIndex = 0;
        while (tokenIndex < values.Tokens.Length)
        {
            string token = values.Tokens[tokenIndex];
            bool equal;
            if (caseSensitive)
            {
                equal = EqualsCaseSensitive(token, candidate);
            }
            else
            {
                equal = AsciiToken.EqualsIgnoreCase(token, candidate);
            }

            if (equal)
            {
                result.Found = true;
                return result;
            }

            tokenIndex = tokenIndex + 1;
        }

        return result;
    }

    static bool EqualsCaseSensitive(ReadOnlySpan<char> left, ReadOnlySpan<char> right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (left[index] != right[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }
}
