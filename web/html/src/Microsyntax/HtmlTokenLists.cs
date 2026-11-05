using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace html;

/// <summary>Parses, normalizes, and searches whitespace-separated HTML token lists.</summary>
internal static class HtmlTokenLists
{
    /// <summary>Splits an HTML space-separated token list using ASCII whitespace rules.</summary>
    /// <param name="value">The complete token-list value.</param>
    /// <returns>A newly allocated array containing the results in the documented order; the array is empty when no values are produced.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static string[] ParseSpaceSeparated(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        List<string> result = new List<string>();
        int index = 0;
        while (index < value.Length)
        {
            while (index < value.Length
                && Ascii.IsHtmlWhitespace(value[index]))
            {
                index = index + 1;
            }

            int start = index;
            while (index < value.Length
                && !Ascii.IsHtmlWhitespace(value[index]))
            {
                index = index + 1;
            }

            if (start < index)
            {
                result.Add(value.Substring(start, index - start));
            }
        }

        return result.ToArray();
    }

    /// <summary>Splits a comma-separated token list, trimming HTML whitespace around each token.</summary>
    /// <param name="value">The complete token-list value.</param>
    /// <returns>A newly allocated array containing the results in the documented order; the array is empty when no values are produced.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static string[] ParseCommaSeparated(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        List<string> result = new List<string>();
        int start = 0;
        while (start <= value.Length)
        {
            int end = start;
            while (end < value.Length && value[end] != ',')
            {
                end = end + 1;
            }

            int tokenStart = start;
            int tokenEnd = end;
            while (tokenStart < tokenEnd
                && Ascii.IsHtmlWhitespace(value[tokenStart]))
            {
                tokenStart = tokenStart + 1;
            }

            while (tokenEnd > tokenStart
                && Ascii.IsHtmlWhitespace(value[tokenEnd - 1]))
            {
                tokenEnd = tokenEnd - 1;
            }

            result.Add(value.Substring(tokenStart, tokenEnd - tokenStart));
            if (end == value.Length)
            {
                break;
            }

            start = end + 1;
        }

        return result.ToArray();
    }

    /// <summary>Parses a space-separated token list with limits and cancellation.</summary>
    /// <param name="value">The token-list value.</param>
    /// <param name="limits">The limits enforced for this operation.</param>
    /// <param name="cancellationToken">The token checked while parsing.</param>
    /// <returns>The retained tokens and completion state.</returns>
    public static HtmlTokenListResult ParseSpaceSeparatedWithLimits(
        string value, HtmlLimits limits,
        CancellationToken cancellationToken)
    {
        ValidateBoundedParseArguments(value, limits);
        HtmlBudget budget = new HtmlBudget(limits, cancellationToken);
        if (!budget.Allows(HtmlLimitKind.AttributeValueCharacters,
            value.Length))
        {
            return CreateLimitResult(value, 0, value.Length,
                new string[0],
                "The token-list value exceeds the attribute-value character limit.");
        }

        List<string> result = new List<string>();
        int index = 0;
        while (index < value.Length)
        {
            while (index < value.Length
                && Ascii.IsHtmlWhitespace(value[index]))
            {
                budget.CheckCancellation();
                index = index + 1;
            }

            int start = index;
            while (index < value.Length
                && !Ascii.IsHtmlWhitespace(value[index]))
            {
                budget.CheckCancellation();
                index = index + 1;
            }

            if (start < index)
            {
                if (!budget.TryConsume(HtmlLimitKind.Tokens, 1))
                {
                    return CreateLimitResult(value, start, index,
                        result.ToArray(),
                        "The token-list token limit was exceeded.");
                }

                result.Add(value.Substring(start, index - start));
            }
        }

        return new HtmlTokenListResult(result.ToArray(), false,
            new HtmlDiagnostic[0]);
    }

    /// <summary>Parses a comma-separated token list with limits and cancellation.</summary>
    /// <param name="value">The token-list value.</param>
    /// <param name="limits">The limits enforced for this operation.</param>
    /// <param name="cancellationToken">The token checked while parsing.</param>
    /// <returns>The retained tokens and completion state.</returns>
    public static HtmlTokenListResult ParseCommaSeparatedWithLimits(
        string value, HtmlLimits limits,
        CancellationToken cancellationToken)
    {
        ValidateBoundedParseArguments(value, limits);
        HtmlBudget budget = new HtmlBudget(limits, cancellationToken);
        if (!budget.Allows(HtmlLimitKind.AttributeValueCharacters,
            value.Length))
        {
            return CreateLimitResult(value, 0, value.Length,
                new string[0],
                "The token-list value exceeds the attribute-value character limit.");
        }

        List<string> result = new List<string>();
        int start = 0;
        while (start <= value.Length)
        {
            int end = start;
            while (end < value.Length && value[end] != ',')
            {
                budget.CheckCancellation();
                end = end + 1;
            }

            int tokenStart = start;
            int tokenEnd = end;
            while (tokenStart < tokenEnd
                && Ascii.IsHtmlWhitespace(value[tokenStart]))
            {
                tokenStart = tokenStart + 1;
            }

            while (tokenEnd > tokenStart
                && Ascii.IsHtmlWhitespace(value[tokenEnd - 1]))
            {
                tokenEnd = tokenEnd - 1;
            }

            if (!budget.TryConsume(HtmlLimitKind.Tokens, 1))
            {
                return CreateLimitResult(value, tokenStart, tokenEnd,
                    result.ToArray(),
                    "The token-list token limit was exceeded.");
            }

            result.Add(value.Substring(tokenStart,
                tokenEnd - tokenStart));
            if (end == value.Length)
            {
                break;
            }

            start = end + 1;
        }

        return new HtmlTokenListResult(result.ToArray(), false,
            new HtmlDiagnostic[0]);
    }

    internal static void ValidateBoundedParseArguments(string value,
        HtmlLimits limits)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (limits == null)
        {
            throw new ArgumentNullException("limits");
        }

        limits.Validate();
    }

    internal static HtmlTokenListResult CreateLimitResult(string value,
        int start, int end, string[] tokens, string message)
    {
        HtmlDiagnostic diagnostic = HtmlMicrosyntaxDiagnostics.Create(value,
            start, end, HtmlDiagnosticCode.MicrosyntaxLimitExceeded,
            message);
        return new HtmlTokenListResult(tokens, true,
            new[] { diagnostic });
    }

    /// <summary>Joins validated tokens with a single ASCII space.</summary>
    /// <param name="tokens">The token values to inspect or serialize.</param>
    /// <returns>The tokens joined by one ASCII space, or an empty string when the array is empty.</returns>
    public static string SerializeSpaceSeparated(string[] tokens)
    {
        ValidateTokens(tokens, false);
        return Join(tokens, " ");
    }

    /// <summary>Joins validated tokens with a comma and a single following space.</summary>
    /// <param name="tokens">The token values to inspect or serialize.</param>
    /// <returns>The tokens joined by a comma and one ASCII space, or an empty string when the array is empty.</returns>
    public static string SerializeCommaSeparated(string[] tokens)
    {
        ValidateTokens(tokens, true);
        return Join(tokens, ", ");
    }

    /// <summary>Serializes a space-separated list with output limits and cancellation.</summary>
    /// <param name="tokens">The tokens to serialize.</param>
    /// <param name="limits">The limits enforced for this operation.</param>
    /// <param name="cancellationToken">The token checked while serializing.</param>
    /// <returns>The normalized token list.</returns>
    public static string SerializeSpaceSeparatedWithLimits(string[] tokens,
        HtmlLimits limits, CancellationToken cancellationToken)
    {
        return SerializeWithLimits(tokens, false, limits,
            cancellationToken);
    }

    /// <summary>Serializes a comma-separated list with output limits and cancellation.</summary>
    /// <param name="tokens">The tokens to serialize.</param>
    /// <param name="limits">The limits enforced for this operation.</param>
    /// <param name="cancellationToken">The token checked while serializing.</param>
    /// <returns>The normalized token list.</returns>
    public static string SerializeCommaSeparatedWithLimits(string[] tokens,
        HtmlLimits limits, CancellationToken cancellationToken)
    {
        return SerializeWithLimits(tokens, true, limits,
            cancellationToken);
    }

    internal static string SerializeWithLimits(string[] tokens,
        bool commaSeparated, HtmlLimits limits,
        CancellationToken cancellationToken)
    {
        if (limits == null)
        {
            throw new ArgumentNullException("limits");
        }

        limits.Validate();
        ValidateTokens(tokens, commaSeparated);
        long length = tokens.Length == 0 ? 0 : tokens.Length - 1;
        if (commaSeparated && tokens.Length > 1)
        {
            length = length + tokens.Length - 1;
        }

        int index = 0;
        while (index < tokens.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            length = length + tokens[index].Length;
            if (length > limits.GetMaximumOutputCharacters())
            {
                throw new HtmlSerializationLimitException(
                    limits.GetMaximumOutputCharacters());
            }

            index = index + 1;
        }

        return Join(tokens, commaSeparated ? ", " : " ");
    }

    /// <summary>Determines whether the token array contains an exact match.</summary>
    /// <param name="tokens">The tokens to search.</param>
    /// <param name="expected">The token to match.</param>
    /// <returns><c>true</c> when a token matches; otherwise, <c>false</c>.</returns>
    public static bool Contains(string[] tokens, string expected)
    {
        ValidateContainsArguments(tokens, expected);
        int index = 0;
        while (index < tokens.Length)
        {
            if (tokens[index] == expected)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    /// <summary>Determines whether the token array contains an ASCII case-insensitive match.</summary>
    /// <param name="tokens">The tokens to search.</param>
    /// <param name="expected">The token to match.</param>
    /// <returns><c>true</c> when a token matches; otherwise, <c>false</c>.</returns>
    public static bool ContainsAsciiInsensitive(string[] tokens,
        string expected)
    {
        ValidateContainsArguments(tokens, expected);
        int index = 0;
        while (index < tokens.Length)
        {
            if (Ascii.EqualsIgnoreCase(tokens[index].AsSpan(),
                expected.AsSpan()))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    /// <summary>Searches the requested prefix of a token array using ASCII case-insensitive matching.</summary>
    /// <param name="tokens">The tokens to search.</param>
    /// <param name="count">The number of leading entries to search.</param>
    /// <param name="expected">The token to match.</param>
    /// <returns><c>true</c> when a token matches; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an argument is outside its supported range.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is inconsistent or malformed.</exception>
    public static bool ContainsAsciiInsensitive(string[] tokens, int count,
        string expected)
    {
        if (tokens == null)
        {
            throw new ArgumentNullException("tokens");
        }

        if (expected == null)
        {
            throw new ArgumentNullException("expected");
        }

        if (count < 0 || count > tokens.Length)
        {
            throw new ArgumentOutOfRangeException("count");
        }

        int index = 0;
        while (index < count)
        {
            if (tokens[index] == null)
            {
                throw new ArgumentException("A token cannot be null.",
                    "tokens");
            }

            if (Ascii.EqualsIgnoreCase(tokens[index].AsSpan(),
                expected.AsSpan()))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static string Join(string[] tokens, string separator)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < tokens.Length)
        {
            if (index > 0)
            {
                result.Append(separator);
            }

            result.Append(tokens[index]);
            index = index + 1;
        }

        return result.ToString();
    }

    internal static void ValidateTokens(string[] tokens, bool commaSeparated)
    {
        if (tokens == null)
        {
            throw new ArgumentNullException("tokens");
        }

        int index = 0;
        while (index < tokens.Length)
        {
            string token = tokens[index];
            if (token == null)
            {
                throw new ArgumentException("A token cannot be null.",
                    "tokens");
            }

            if (!commaSeparated && token.Length == 0)
            {
                throw new ArgumentException(
                    "A space-separated token cannot be empty.", "tokens");
            }

            int characterIndex = 0;
            while (characterIndex < token.Length)
            {
                char current = token[characterIndex];
                if (!commaSeparated && Ascii.IsHtmlWhitespace(current)
                    || commaSeparated && current == ',')
                {
                    throw new ArgumentException(
                        "A token contains its list delimiter.", "tokens");
                }

                characterIndex = characterIndex + 1;
            }

            if (commaSeparated && token.Length > 0
                && (Ascii.IsHtmlWhitespace(token[0])
                    || Ascii.IsHtmlWhitespace(token[token.Length - 1])))
            {
                throw new ArgumentException(
                    "A comma-separated token cannot begin or end with HTML whitespace.",
                    "tokens");
            }

            index = index + 1;
        }
    }

    internal static void ValidateContainsArguments(string[] tokens, string expected)
    {
        if (tokens == null)
        {
            throw new ArgumentNullException("tokens");
        }

        if (expected == null)
        {
            throw new ArgumentNullException("expected");
        }

        int index = 0;
        while (index < tokens.Length)
        {
            if (tokens[index] == null)
            {
                throw new ArgumentException("A token cannot be null.",
                    "tokens");
            }

            index = index + 1;
        }
    }
}
