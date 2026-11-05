using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Represents a boundary scanner.
/// </summary>
public class EmailBoundaryScanner : IEmailBoundaryScannerContract
{
    byte[]? boundary;
    EmailParseOptions? options;
    List<byte>? line;
    List<EmailBoundaryDelimiter>? delimiters;
    EmailIssueList? issues;
    long sourceByteCount;
    long lineStartOffset;
    int precedingLineEndingByteCount;
    bool pendingCarriageReturn;
    bool closingBoundaryFound;
    bool completed;
    bool configurationRecovered;
    /// <summary>
    /// Creates a boundary scanner using the supplied policy.
    /// </summary>
    /// <param name = "boundary">The boundary used by the operation.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    public EmailBoundaryScanner(string? boundary, EmailParseOptions options)
    {
        Initialize(boundary, options);
    }

    internal static EmailBoundaryScanner CreateImplementationWithBoundary(string boundary)
    {
        return new EmailBoundaryScanner(boundary, new EmailParseOptions());
    }

    internal static EmailBoundaryScanner CreateImplementationWithBoundaryAndOptions(string? boundary, EmailParseOptions options)
    {
        return new EmailBoundaryScanner(boundary, options);
    }

    void Initialize(string? suppliedBoundary, EmailParseOptions suppliedOptions)
    {
        if (suppliedBoundary == null)
        {
            throw new ArgumentNullException("suppliedBoundary");
        }

        if (suppliedOptions == null)
        {
            throw new ArgumentNullException("suppliedOptions");
        }

        options = suppliedOptions.Clone();
        EmailBoundaryValue normalized = NormalizeBoundary(suppliedBoundary, options.GetCompatibilityOptions().GetAllowBrokenMimeBoundaries());
        boundary = normalized.GetBytes();
        configurationRecovered = normalized.WasRecovered();
        line = new List<byte>();
        delimiters = new List<EmailBoundaryDelimiter>();
        issues = new EmailIssueList();
        sourceByteCount = 0;
        lineStartOffset = 0;
        precedingLineEndingByteCount = 0;
        pendingCarriageReturn = false;
        closingBoundaryFound = false;
        completed = false;
        if (configurationRecovered)
        {
            issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Warning, EmailIssueCode.RecoveryApplied, "A mismatched quote around the MIME boundary value was ignored.", new EmailSourcePosition(0, 1, 1), null, "Content-Type", "Boundary scanning used the unquoted boundary value."));
        }
    }

    /// <summary>
    /// Processes the next input segment while retaining state for later segments.
    /// </summary>
    /// <remarks>
    /// Call this method in input order, then call <c>Complete</c> once no more input remains.
    /// </remarks>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A list snapshot containing the matching or produced values.</returns>
    public List<EmailBoundaryDelimiter> Feed(ReadOnlySpan<byte> input)
    {
        EnsureActive();
        int before = (delimiters ?? throw new InvalidOperationException()).Count;
        int index = 0;
        while (index < input.Length)
        {
            ProcessByte(input[index]);
            index++;
        }

        return CopyNewDelimiters(before);
    }

    /// <summary>
    /// Finishes incremental processing and returns the final output or status.
    /// </summary>
    /// <remarks>
    /// Calling this method finalizes buffered state; use the instance's status method before attempting further input.
    /// </remarks>
    /// <returns>The boundary scan result produced by the operation.</returns>
    public EmailBoundaryScanResult Complete()
    {
        EnsureActive();
        if (pendingCarriageReturn)
        {
            pendingCarriageReturn = false;
            FinishLine(EmailLineEnding.Cr, sourceByteCount);
        }
        else if ((line ?? throw new InvalidOperationException()).Count > 0)
        {
            FinishLine(EmailLineEnding.None, sourceByteCount);
        }

        if (!closingBoundaryFound)
        {
            EmailIssueSeverity severity = (options ?? throw new InvalidOperationException()).GetMode() == EmailParseMode.Strict ? EmailIssueSeverity.Error : EmailIssueSeverity.Warning;
            (issues ?? throw new InvalidOperationException()).Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, EmailIssueCode.MimeStructureMalformed, "The multipart body ended without a closing boundary delimiter.", new EmailSourcePosition(sourceByteCount, 1, 1), null, null, "All complete opening delimiters and body bytes were retained."));
        }

        completed = true;
        return new EmailBoundaryScanResult(delimiters, issues, sourceByteCount, closingBoundaryFound);
    }

    /// <summary>
    /// Determines whether the current value or state is complete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is complete; otherwise, <c>false</c>.</returns>
    public bool IsComplete()
    {
        return completed;
    }

    /// <summary>
    /// Determines whether the closing boundary is present or available.
    /// </summary>
    /// <returns><c>true</c> when the closing boundary is available; otherwise, <c>false</c>.</returns>
    public bool HasClosingBoundary()
    {
        return closingBoundaryFound;
    }

    /// <summary>
    /// Gets the source byte count.
    /// </summary>
    /// <returns>The current source byte count.</returns>
    public long GetSourceByteCount()
    {
        return sourceByteCount;
    }

    void ProcessByte(byte value)
    {
        long offset = sourceByteCount;
        sourceByteCount++;
        if (pendingCarriageReturn)
        {
            pendingCarriageReturn = false;
            if (value == 10)
            {
                FinishLine(EmailLineEnding.CrLf, sourceByteCount);
                return;
            }

            FinishLine(EmailLineEnding.Cr, offset);
        }

        if (value == 13)
        {
            pendingCarriageReturn = true;
        }
        else if (value == 10)
        {
            FinishLine(EmailLineEnding.Lf, sourceByteCount);
        }
        else
        {
            (line ?? throw new InvalidOperationException()).Add(value);
            int maximum = (options ?? throw new InvalidOperationException()).GetLimits().GetParsingLimits().GetMaxLineBytes();
            if (line.Count > maximum)
            {
                throw new EmailLimitException("MaxLineBytes", maximum, line.Count);
            }
        }
    }

    void FinishLine(EmailLineEnding ending, long endOffset)
    {
        if (!closingBoundaryFound)
        {
            RecognizeLine(ending, endOffset);
        }
        else
        {
            RecognizeUnexpectedLine();
        }

        (line ?? throw new InvalidOperationException()).Clear();
        lineStartOffset = endOffset;
        precedingLineEndingByteCount = GetLineEndingLength(ending);
    }

    void RecognizeUnexpectedLine()
    {
        EmailBoundaryMatch match = MatchLine();
        if (!match.IsMatch())
        {
            return;
        }

        EmailIssueSeverity severity = (options ?? throw new InvalidOperationException()).GetMode() == EmailParseMode.Strict ? EmailIssueSeverity.Error : EmailIssueSeverity.Warning;
        (issues ?? throw new InvalidOperationException()).Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, EmailIssueCode.MimeUnexpectedBoundary, "A MIME boundary delimiter appeared after the closing delimiter.", new EmailSourcePosition(lineStartOffset, 1, 1), null, null, "The unexpected delimiter was retained as epilogue content."));
    }

    void RecognizeLine(EmailLineEnding ending, long endOffset)
    {
        if (ending != EmailLineEnding.CrLf && ending != EmailLineEnding.None && !(options ?? throw new InvalidOperationException()).GetCompatibilityOptions().GetAllowBareLineEndings())
        {
            return;
        }

        EmailBoundaryMatch match = MatchLine();
        if (!match.IsMatch())
        {
            return;
        }

        bool recovered = configurationRecovered || match.WasRecovered() || ending == EmailLineEnding.Lf || ending == EmailLineEnding.Cr;
        EmailRange range = new EmailRange(lineStartOffset, endOffset - lineStartOffset);
        EmailBoundaryDelimiter delimiter = new EmailBoundaryDelimiter(range, match.IsClosing(), ending, precedingLineEndingByteCount, match.GetTransportPaddingByteCount(), recovered);
        (delimiters ?? throw new InvalidOperationException()).Add(delimiter);
        if (recovered)
        {
            (issues ?? throw new InvalidOperationException()).Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(EmailIssueSeverity.Warning, EmailIssueCode.MimeStructureMalformed, "A noncanonical MIME boundary delimiter was accepted.", new EmailSourcePosition(lineStartOffset, 1, 1), null, null, "The delimiter range was retained and marked as recovered."));
        }

        if (match.IsClosing())
        {
            closingBoundaryFound = true;
        }
    }

    EmailBoundaryMatch MatchLine()
    {
        if ((line ?? throw new InvalidOperationException()).Count < (boundary ?? throw new InvalidOperationException()).Length + 2 || line[0] != 45 || line[1] != 45)
        {
            return EmailBoundaryMatch.CreateNoMatch();
        }

        int index = 2;
        bool recovered = false;
        bool leadingQuote = index < line.Count && line[index] == 34;
        if (leadingQuote)
        {
            if (!(options ?? throw new InvalidOperationException()).GetCompatibilityOptions().GetAllowBrokenMimeBoundaries())
            {
                return EmailBoundaryMatch.CreateNoMatch();
            }

            index++;
            recovered = true;
        }

        if (!MatchesBoundary(index))
        {
            return EmailBoundaryMatch.CreateNoMatch();
        }

        index += boundary.Length;
        if (index < line.Count && line[index] == 34)
        {
            if (!(options ?? throw new InvalidOperationException()).GetCompatibilityOptions().GetAllowBrokenMimeBoundaries())
            {
                return EmailBoundaryMatch.CreateNoMatch();
            }

            index++;
            recovered = true;
        }
        else if (leadingQuote)
        {
            recovered = true;
        }

        bool closing = false;
        if (index + 1 < line.Count && line[index] == 45 && line[index + 1] == 45)
        {
            closing = true;
            index += 2;
        }

        int padding = 0;
        while (index < line.Count && (line[index] == 32 || line[index] == 9))
        {
            padding++;
            index++;
        }

        if (index != line.Count)
        {
            return EmailBoundaryMatch.CreateNoMatch();
        }

        return EmailBoundaryMatch.CreateMatch(closing, padding, recovered);
    }

    bool MatchesBoundary(int start)
    {
        if (start > (line ?? throw new InvalidOperationException()).Count - (boundary ?? throw new InvalidOperationException()).Length)
        {
            return false;
        }

        int index = 0;
        while (index < boundary.Length)
        {
            if (line[start + index] != boundary[index])
            {
                return false;
            }

            index++;
        }

        return true;
    }

    List<EmailBoundaryDelimiter> CopyNewDelimiters(int start)
    {
        List<EmailBoundaryDelimiter> result = new List<EmailBoundaryDelimiter>();
        int index = start;
        while (index < (delimiters ?? throw new InvalidOperationException()).Count)
        {
            result.Add(delimiters[index]);
            index++;
        }

        return result;
    }

    void EnsureActive()
    {
        if (completed)
        {
            throw new InvalidOperationException("The boundary scanner is already complete.");
        }
    }

    static EmailBoundaryValue NormalizeBoundary(string value, bool allowBroken)
    {
        bool leading = value.Length > 0 && value[0] == '"';
        bool trailing = value.Length > 0 && value[value.Length - 1] == '"';
        bool recovered = false;
        int start = 0;
        int length = value.Length;
        if (leading && trailing && value.Length >= 2)
        {
            start = 1;
            length -= 2;
        }
        else if (leading || trailing)
        {
            if (!allowBroken)
            {
                throw new ArgumentException("A MIME boundary value has mismatched quotes.", "value");
            }

            recovered = true;
            if (leading)
            {
                start = 1;
                length--;
            }
            else
            {
                length--;
            }
        }

        if (length < 1 || length > 70)
        {
            throw new ArgumentException("A MIME boundary must contain between 1 and 70 bytes.", "value");
        }

        string normalized = value.Substring(start, length);
        byte[] bytes = Encoding.ASCII.GetBytes(normalized);
        int index = 0;
        while (index < normalized.Length)
        {
            char current = normalized[index];
            if (!IsBoundaryCharacter(current) || index + 1 == normalized.Length && current == ' ')
            {
                throw new ArgumentException("A MIME boundary contains an invalid character.", "value");
            }

            index++;
        }

        return new EmailBoundaryValue(bytes, recovered);
    }

    static bool IsBoundaryCharacter(char value)
    {
        if (value >= '0' && value <= '9' || value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z')
        {
            return true;
        }

        return value == '\'' || value == '(' || value == ')' || value == '+' || value == '_' || value == ',' || value == '-' || value == '.' || value == '/' || value == ':' || value == '=' || value == '?' || value == ' ';
    }

    static int GetLineEndingLength(EmailLineEnding ending)
    {
        if (ending == EmailLineEnding.CrLf)
        {
            return 2;
        }

        return ending == EmailLineEnding.None ? 0 : 1;
    }
}
