using System;
using System.Globalization;
using System.Threading;

namespace html;

/// <summary>Parses and serializes the delay and optional URL in an HTML refresh value.</summary>
internal static class HtmlRefresh
{
    /// <summary>Parses an HTML refresh value into a delay and optional URL.</summary>
    /// <param name="value">The complete refresh-content value.</param>
    /// <returns>The parsed delay, optional URL, diagnostics, and completion state.</returns>
    public static HtmlRefreshResult Parse(string value)
    {
        return Parse(value, HtmlLimits.CreateDefault(),
            CancellationToken.None);
    }

    /// <summary>Parses an HTML refresh value while enforcing resource limits and cancellation.</summary>
    /// <param name="value">The complete refresh-content value.</param>
    /// <param name="limits">The limits copied and enforced for this operation.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The parsed delay, optional URL, diagnostics, and completion state.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static HtmlRefreshResult Parse(string value, HtmlLimits limits,
        CancellationToken cancellationToken)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (limits == null)
        {
            throw new ArgumentNullException("limits");
        }

        HtmlBudget budget = new HtmlBudget(limits, cancellationToken);
        HtmlDiagnosticCollector diagnostics =
            new HtmlDiagnosticCollector(budget);
        if (!budget.Allows(HtmlLimitKind.AttributeValueCharacters,
            value.Length))
        {
            diagnostics.Add(HtmlMicrosyntaxDiagnostics.Create(value, 0,
                value.Length, HtmlDiagnosticCode.MicrosyntaxLimitExceeded,
                "The refresh value exceeds the attribute-value character limit."));
            return CreateResult(false, false, true, 0, "", diagnostics);
        }

        int position = SkipWhitespace(value, 0, budget);
        int digitsStart = position;
        long delay = 0;
        bool overflow = false;
        while (position < value.Length && Ascii.IsDigit(value[position]))
        {
            budget.CheckCancellation();
            int digit = value[position] - '0';
            if (delay > (long.MaxValue - digit) / 10)
            {
                overflow = true;
            }
            else if (!overflow)
            {
                delay = delay * 10 + digit;
            }

            position = position + 1;
        }

        if (digitsStart == position
            && (position == value.Length || value[position] != '.'))
        {
            return CreateInvalidResult(value, digitsStart,
                position < value.Length ? position + 1 : position,
                "A refresh value must begin with a nonnegative delay.",
                diagnostics);
        }

        if (overflow)
        {
            return CreateInvalidResult(value, digitsStart, position,
                "The refresh delay exceeds the supported integer range.",
                diagnostics);
        }

        while (position < value.Length
            && (Ascii.IsDigit(value[position]) || value[position] == '.'))
        {
            budget.CheckCancellation();
            position = position + 1;
        }

        if (position == value.Length)
        {
            return CreateResult(true, false, false, delay, "",
                diagnostics);
        }

        char delimiter = value[position];
        if (delimiter != ';' && delimiter != ','
            && !Ascii.IsHtmlWhitespace(delimiter))
        {
            return CreateInvalidResult(value, position, position + 1,
                "The refresh delay must be followed by whitespace, a comma, or a semicolon.",
                diagnostics);
        }

        position = SkipWhitespace(value, position, budget);
        if (position < value.Length
            && (value[position] == ';' || value[position] == ','))
        {
            position = position + 1;
        }

        position = SkipWhitespace(value, position, budget);
        if (position == value.Length)
        {
            return CreateResult(true, false, false, delay, "",
                diagnostics);
        }

        int originalUrlStart = position;
        bool skipQuotes = true;
        if (IsAsciiLetter(value[position], 'u'))
        {
            int keywordPosition = position + 1;
            if (keywordPosition >= value.Length
                || !IsAsciiLetter(value[keywordPosition], 'r'))
            {
                skipQuotes = false;
            }
            else
            {
                keywordPosition = keywordPosition + 1;
                if (keywordPosition >= value.Length
                    || !IsAsciiLetter(value[keywordPosition], 'l'))
                {
                    skipQuotes = false;
                }
                else
                {
                    keywordPosition = SkipWhitespace(value,
                        keywordPosition + 1, budget);
                    if (keywordPosition >= value.Length
                        || value[keywordPosition] != '=')
                    {
                        skipQuotes = false;
                    }
                    else
                    {
                        position = SkipWhitespace(value,
                            keywordPosition + 1, budget);
                    }
                }
            }
        }

        if (!skipQuotes)
        {
            position = originalUrlStart;
            return CreateUrlResult(value, position, '\0', delay, budget,
                diagnostics);
        }

        char quote = '\0';
        if (position < value.Length
            && (value[position] == '\'' || value[position] == '"'))
        {
            quote = value[position];
            position = position + 1;
        }

        return CreateUrlResult(value, position, quote, delay, budget,
            diagnostics);
    }

    /// <summary>Serializes a refresh delay and optional URL with limits and cancellation.</summary>
    /// <param name="delaySeconds">The nonnegative whole-second delay.</param>
    /// <param name="url">The optional URL, or an empty string.</param>
    /// <param name="limits">The output limits enforced for this operation.</param>
    /// <param name="cancellationToken">The token checked before serialization.</param>
    /// <returns>The normalized refresh value.</returns>
    public static string Serialize(long delaySeconds, string url,
        HtmlLimits limits, CancellationToken cancellationToken)
    {
        if (delaySeconds < 0)
        {
            throw new ArgumentOutOfRangeException("delaySeconds");
        }

        if (url == null)
        {
            throw new ArgumentNullException("url");
        }

        if (limits == null)
        {
            throw new ArgumentNullException("limits");
        }

        limits.Validate();
        cancellationToken.ThrowIfCancellationRequested();
        string delay = delaySeconds.ToString(CultureInfo.InvariantCulture);
        long length = delay.Length;
        if (url.Length != 0)
        {
            length = length + 6L + url.Length;
        }

        if (length > limits.GetMaximumOutputCharacters())
        {
            throw new HtmlSerializationLimitException(
                limits.GetMaximumOutputCharacters());
        }

        return url.Length == 0 ? delay : delay + "; url=" + url;
    }

    internal static HtmlRefreshResult CreateUrlResult(string value, int start,
        char quote, long delay, HtmlBudget budget,
        HtmlDiagnosticCollector diagnostics)
    {
        int end = value.Length;
        if (quote != '\0')
        {
            int index = start;
            while (index < value.Length)
            {
                budget.CheckCancellation();
                if (value[index] == quote)
                {
                    end = index;
                    break;
                }

                index = index + 1;
            }
        }

        string url = value.Substring(start, end - start);
        return CreateResult(true, true, false, delay, url, diagnostics);
    }

    internal static HtmlRefreshResult CreateInvalidResult(string source,
        int start, int end, string message,
        HtmlDiagnosticCollector diagnostics)
    {
        diagnostics.Add(HtmlMicrosyntaxDiagnostics.Create(source, start,
            end, HtmlDiagnosticCode.MicrosyntaxInvalid, message));
        return CreateResult(false, false, false, 0, "", diagnostics);
    }

    internal static HtmlRefreshResult CreateResult(bool success, bool hasUrl,
        bool limitExceeded, long delay, string url,
        HtmlDiagnosticCollector diagnostics)
    {
        return new HtmlRefreshResult(success, hasUrl, limitExceeded, delay,
            url, diagnostics.GetDiagnostics(), diagnostics.GetTotalCount(),
            diagnostics.GetDroppedCount(), diagnostics.WasTruncated());
    }

    internal static int SkipWhitespace(string value, int position, HtmlBudget budget)
    {
        while (position < value.Length
            && Ascii.IsHtmlWhitespace(value[position]))
        {
            budget.CheckCancellation();
            position = position + 1;
        }

        return position;
    }

    internal static bool IsAsciiLetter(char value, char lower)
    {
        return Ascii.ToLower(value) == lower;
    }
}
