using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;

namespace html;

/// <summary>Parses and serializes HTML <c>srcset</c> candidate lists.</summary>
internal static class HtmlSrcset
{
    /// <summary>Parses an HTML <c>srcset</c> value into ordered URL candidates and descriptors.</summary>
    /// <param name="value">The complete <c>srcset</c> attribute value.</param>
    /// <returns>The parsed candidates, diagnostics, and completion state.</returns>
    public static HtmlSrcsetResult Parse(string value)
    {
        return Parse(value, HtmlLimits.CreateDefault(),
            CancellationToken.None);
    }

    /// <summary>Parses an HTML <c>srcset</c> value while enforcing resource limits and cancellation.</summary>
    /// <param name="value">The complete <c>srcset</c> attribute value.</param>
    /// <param name="limits">The limits copied and enforced for this operation.</param>
    /// <param name="cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The parsed candidates, diagnostics, and completion state.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static HtmlSrcsetResult Parse(string value, HtmlLimits limits,
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
                "The srcset value exceeds the attribute-value character limit."));
            return CreateResult(new HtmlSrcsetCandidate[0], 0, true,
                diagnostics);
        }

        List<HtmlSrcsetCandidate> candidates =
            new List<HtmlSrcsetCandidate>();
        int errorCount = 0;
        int position = 0;
        while (position < value.Length)
        {
            int separatorStart = position;
            bool sawComma = false;
            while (position < value.Length
                && (Ascii.IsHtmlWhitespace(value[position])
                    || value[position] == ','))
            {
                budget.CheckCancellation();
                if (value[position] == ',')
                {
                    sawComma = true;
                }

                position = position + 1;
            }

            if (sawComma)
            {
                errorCount = IncrementError(errorCount);
                diagnostics.Add(HtmlMicrosyntaxDiagnostics.Create(value,
                    separatorStart, position,
                    HtmlDiagnosticCode.MicrosyntaxInvalid,
                    "The srcset contains an empty candidate."));
            }

            if (position == value.Length)
            {
                break;
            }

            int urlStart = position;
            while (position < value.Length
                && !Ascii.IsHtmlWhitespace(value[position]))
            {
                budget.CheckCancellation();
                position = position + 1;
            }

            string url = value.Substring(urlStart, position - urlStart);
            int trailingCommas = CountTrailingCommas(url);
            List<string> descriptors = new List<string>();
            if (trailingCommas > 0)
            {
                url = url.Substring(0, url.Length - trailingCommas);
                if (trailingCommas > 1)
                {
                    errorCount = IncrementError(errorCount);
                    diagnostics.Add(HtmlMicrosyntaxDiagnostics.Create(value,
                        position - trailingCommas, position,
                        HtmlDiagnosticCode.MicrosyntaxInvalid,
                        "A srcset candidate has more than one trailing comma."));
                }
            }
            else
            {
                position = TokenizeDescriptors(value, position,
                    descriptors, budget);
            }

            HtmlSrcsetCandidate? candidate = ParseCandidate(url, descriptors);
            if (candidate == null)
            {
                errorCount = IncrementError(errorCount);
                diagnostics.Add(HtmlMicrosyntaxDiagnostics.Create(value,
                    urlStart, position,
                    HtmlDiagnosticCode.MicrosyntaxInvalid,
                    GetCandidateErrorMessage(url, descriptors)));
            }
            else
            {
                if (!budget.TryConsume(HtmlLimitKind.Tokens, 1))
                {
                    diagnostics.Add(HtmlMicrosyntaxDiagnostics.Create(value,
                        urlStart, position,
                        HtmlDiagnosticCode.MicrosyntaxLimitExceeded,
                        "The srcset candidate limit was exceeded."));
                    return CreateResult(candidates.ToArray(), errorCount,
                        true, diagnostics);
                }

                candidates.Add(candidate);
            }

        }

        return CreateResult(candidates.ToArray(), errorCount, false,
            diagnostics);
    }

    /// <summary>Serializes the supplied candidate list as a normalized <c>srcset</c> value.</summary>
    /// <param name="candidates">The <c>srcset</c> candidates to serialize.</param>
    /// <returns>A normalized comma-separated <c>srcset</c> value preserving candidate order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is inconsistent or malformed.</exception>
    public static string Serialize(HtmlSrcsetCandidate[] candidates)
    {
        if (candidates == null)
        {
            throw new ArgumentNullException("candidates");
        }

        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < candidates.Length)
        {
            HtmlSrcsetCandidate candidate = candidates[index];
            if (candidate == null)
            {
                throw new ArgumentException(
                    "A srcset candidate cannot be null.", "candidates");
            }

            if (index > 0)
            {
                result.Append(", ");
            }

            result.Append(candidate.GetUrl());
            if (candidate.HasWidth())
            {
                result.Append(' ');
                result.Append(candidate.GetWidth().ToString(
                    CultureInfo.InvariantCulture));
                result.Append('w');
            }
            else if (candidate.HasDensity())
            {
                result.Append(' ');
                result.Append(candidate.GetDensity().ToString("R",
                    CultureInfo.InvariantCulture));
                result.Append('x');
            }

            index = index + 1;
        }

        return result.ToString();
    }

    /// <summary>Serializes candidates while enforcing output limits and cancellation.</summary>
    /// <param name="candidates">The candidates to serialize.</param>
    /// <param name="limits">The limits enforced for this operation.</param>
    /// <param name="cancellationToken">The token checked while serializing.</param>
    /// <returns>The normalized candidate list.</returns>
    public static string SerializeWithLimits(HtmlSrcsetCandidate[] candidates,
        HtmlLimits limits, CancellationToken cancellationToken)
    {
        if (limits == null)
        {
            throw new ArgumentNullException("limits");
        }

        limits.Validate();
        if (candidates == null)
        {
            throw new ArgumentNullException("candidates");
        }

        long length = candidates.Length == 0
            ? 0 : (long)(candidates.Length - 1) * 2L;
        int index = 0;
        while (index < candidates.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            HtmlSrcsetCandidate candidate = candidates[index];
            if (candidate == null)
            {
                throw new ArgumentException(
                    "A srcset candidate cannot be null.", "candidates");
            }

            length = length + candidate.GetUrl().Length;
            if (candidate.HasWidth())
            {
                length = length + 2L + candidate.GetWidth().ToString(
                    CultureInfo.InvariantCulture).Length;
            }
            else if (candidate.HasDensity())
            {
                length = length + 2L + candidate.GetDensity().ToString("R",
                    CultureInfo.InvariantCulture).Length;
            }

            if (length > limits.GetMaximumOutputCharacters())
            {
                throw new HtmlSerializationLimitException(
                    limits.GetMaximumOutputCharacters());
            }

            index = index + 1;
        }

        return Serialize(candidates);
    }

    internal static HtmlSrcsetResult CreateResult(
        HtmlSrcsetCandidate[] candidates, int errorCount,
        bool limitExceeded, HtmlDiagnosticCollector diagnostics)
    {
        return new HtmlSrcsetResult(candidates, errorCount, limitExceeded,
            diagnostics.GetDiagnostics(), diagnostics.GetTotalCount(),
            diagnostics.GetDroppedCount(), diagnostics.WasTruncated());
    }

    internal static string GetCandidateErrorMessage(string url,
        List<string> descriptors)
    {
        if (!IsValidUrl(url))
        {
            return "A srcset candidate has an empty or malformed URL.";
        }

        if (descriptors.Count > 1)
        {
            return "A srcset candidate has duplicate or conflicting descriptors.";
        }

        if (descriptors.Count == 0)
        {
            return "The srcset candidate is malformed.";
        }

        string descriptor = descriptors[0];
        if (descriptor.Length != 0
            && descriptor[descriptor.Length - 1] == 'w')
        {
            return "A srcset width descriptor must be a positive integer followed by 'w'.";
        }

        if (descriptor.Length != 0
            && descriptor[descriptor.Length - 1] == 'x')
        {
            return "A srcset density descriptor must be finite and positive and end with 'x'.";
        }

        return "The srcset candidate uses an unsupported descriptor.";
    }

    internal static int TokenizeDescriptors(string value, int position,
        List<string> descriptors, HtmlBudget budget)
    {
        while (position < value.Length
            && Ascii.IsHtmlWhitespace(value[position]))
        {
            budget.CheckCancellation();
            position = position + 1;
        }

        StringBuilder current = new StringBuilder();
        bool inParentheses = false;
        bool afterDescriptor = false;
        while (position < value.Length)
        {
            budget.CheckCancellation();
            char character = value[position];
            if (inParentheses)
            {
                current.Append(character);
                position = position + 1;
                if (character == ')')
                {
                    inParentheses = false;
                }

                continue;
            }

            if (character == ',')
            {
                position = position + 1;
                AddDescriptor(descriptors, current);
                return position;
            }

            if (Ascii.IsHtmlWhitespace(character))
            {
                AddDescriptor(descriptors, current);
                afterDescriptor = true;
                position = position + 1;
                continue;
            }

            if (afterDescriptor)
            {
                afterDescriptor = false;
            }

            current.Append(character);
            if (character == '(')
            {
                inParentheses = true;
            }

            position = position + 1;
        }

        AddDescriptor(descriptors, current);
        return position;
    }

    internal static void AddDescriptor(List<string> descriptors,
        StringBuilder current)
    {
        if (current.Length == 0)
        {
            return;
        }

        descriptors.Add(current.ToString());
        current.Clear();
    }

    internal static HtmlSrcsetCandidate? ParseCandidate(string url,
        List<string> descriptors)
    {
        if (!IsValidUrl(url))
        {
            return null;
        }

        if (descriptors.Count == 0)
        {
            return HtmlSrcsetCandidate.Create(url);
        }

        if (descriptors.Count != 1)
        {
            return null;
        }

        string descriptor = descriptors[0];
        if (descriptor.Length < 2)
        {
            return null;
        }

        char suffix = descriptor[descriptor.Length - 1];
        if (suffix == 'w')
        {
            IntegerScanResult width = NumberParser.ParseNonNegativeInteger(
                descriptor.AsSpan(0, descriptor.Length - 1));
            return width.IsSuccess() && width.GetValue() > 0
                ? HtmlSrcsetCandidate.CreateWidth(url, width.GetValue())
                : null;
        }

        if (suffix == 'x')
        {
            FloatingPointScanResult density =
                NumberParser.ParseFloatingPointDescriptor(
                    descriptor.AsSpan());
            return density.IsSuccess()
                ? HtmlSrcsetCandidate.CreateDensity(url, density.GetValue())
                : null;
        }

        return null;
    }

    internal static bool IsValidUrl(string url)
    {
        if (url.Length == 0 || url[0] == ',' || url[url.Length - 1] == ',')
        {
            return false;
        }

        int index = 0;
        while (index < url.Length)
        {
            if (Ascii.IsHtmlWhitespace(url[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static int CountTrailingCommas(string value)
    {
        int index = value.Length;
        while (index > 0 && value[index - 1] == ',')
        {
            index = index - 1;
        }

        return value.Length - index;
    }

    internal static int IncrementError(int value)
    {
        return value == int.MaxValue ? value : value + 1;
    }
}
