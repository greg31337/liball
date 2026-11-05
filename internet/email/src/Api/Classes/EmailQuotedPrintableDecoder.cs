using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Incrementally decodes quoted-printable.
/// </summary>
public class EmailQuotedPrintableDecoder : IEmailQuotedPrintableDecoderContract
{
    const int NormalState = 0;
    const int EqualsState = 1;
    const int FirstHexState = 2;
    const int EqualsCarriageReturnState = 3;
    const int EqualsWhitespaceState = 4;
    const int EqualsWhitespaceCarriageReturnState = 5;
    EmailQuotedPrintableDecodeMode mode;
    long maximumDecodedBytes;
    long decodedByteCount;
    int state;
    byte firstHexByte;
    int firstHexValue;
    bool firstHexLowercase;
    EmailSourcePosition? escapePosition;
    List<byte> escapeWhitespace;
    List<byte> lineWhitespace;
    bool pendingLiteralCarriageReturn;
    EmailSourcePosition? literalCarriageReturnPosition;
    bool limitReached;
    bool completed;
    EmailSourceTracker sourceTracker;
    EmailIssueList issues;
    /// <summary>
    /// Creates a new quoted-printable decoder.
    /// </summary>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <param name = "maximumDecodedBytes">The source bytes or data to process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailQuotedPrintableDecoder(EmailQuotedPrintableDecodeMode mode, long maximumDecodedBytes)
    {
        if (mode < EmailQuotedPrintableDecodeMode.Strict || mode > EmailQuotedPrintableDecodeMode.Tolerant)
        {
            throw new ArgumentOutOfRangeException("mode");
        }

        if (maximumDecodedBytes < 0)
        {
            throw new ArgumentOutOfRangeException("maximumDecodedBytes");
        }

        this.mode = mode;
        this.maximumDecodedBytes = maximumDecodedBytes;
        decodedByteCount = 0;
        state = NormalState;
        firstHexByte = 0;
        firstHexValue = 0;
        firstHexLowercase = false;
        escapePosition = null;
        escapeWhitespace = new List<byte>();
        lineWhitespace = new List<byte>();
        pendingLiteralCarriageReturn = false;
        literalCarriageReturnPosition = null;
        limitReached = false;
        completed = false;
        sourceTracker = new EmailSourceTrackerOperations().CreateWith();
        issues = new EmailIssueList();
    }

    /// <summary>
    /// Gets the mode.
    /// </summary>
    /// <returns>The current mode.</returns>
    public EmailQuotedPrintableDecodeMode GetMode()
    {
        return mode;
    }

    /// <summary>
    /// Gets the maximum decoded bytes.
    /// </summary>
    /// <returns>The current maximum decoded bytes.</returns>
    public long GetMaximumDecodedBytes()
    {
        return maximumDecodedBytes;
    }

    /// <summary>
    /// Gets the encoded byte count.
    /// </summary>
    /// <returns>The current encoded byte count.</returns>
    public long GetEncodedByteCount()
    {
        return sourceTracker.GetByteOffset();
    }

    /// <summary>
    /// Gets the decoded byte count.
    /// </summary>
    /// <returns>The current decoded byte count.</returns>
    public long GetDecodedByteCount()
    {
        return decodedByteCount;
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
    /// Determines whether the fatal error is present or available.
    /// </summary>
    /// <returns><c>true</c> when the fatal error is available; otherwise, <c>false</c>.</returns>
    public bool HasFatalError()
    {
        return limitReached;
    }

    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    public EmailIssueList GetIssues()
    {
        EmailIssueList copy = new EmailIssueList();
        copy.AddRange(issues);
        return copy;
    }

    /// <summary>
    /// Processes the next input segment while retaining state for later segments.
    /// </summary>
    /// <remarks>
    /// Call this method in input order, then call <c>Complete</c> once no more input remains.
    /// </remarks>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A new array containing the produced bytes or values.</returns>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    public byte[] Feed(ReadOnlySpan<byte> input)
    {
        if (completed)
        {
            throw new InvalidOperationException("Quoted-printable input cannot be supplied after completion.");
        }

        List<byte> output = new List<byte>();
        int index = 0;
        while (index < input.Length)
        {
            EmailSourcePosition position = sourceTracker.GetPosition();
            byte value = input[index];
            sourceTracker.Advance(input.Slice(index, 1));
            if (!limitReached)
            {
                ProcessByte(value, position, output);
            }

            index++;
        }

        return output.ToArray();
    }

    /// <summary>
    /// Finishes incremental processing and returns the final output or status.
    /// </summary>
    /// <remarks>
    /// Calling this method finalizes buffered state; use the instance's status method before attempting further input.
    /// </remarks>
    /// <returns>A new array containing the produced bytes or values.</returns>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    public byte[] Complete()
    {
        if (completed)
        {
            throw new InvalidOperationException("Quoted-printable decoding has already completed.");
        }

        completed = true;
        List<byte> output = new List<byte>();
        lineWhitespace.Clear();
        if (pendingLiteralCarriageReturn)
        {
            if (!limitReached)
            {
                AddInvalidCharacterIssue(literalCarriageReturnPosition, "A carriage return in quoted-printable data was not followed by line feed.");
            }

            pendingLiteralCarriageReturn = false;
            literalCarriageReturnPosition = null;
        }

        if (limitReached || state == NormalState)
        {
            ClearEscapeState();
            return output.ToArray();
        }

        EmailSourcePosition position = sourceTracker.GetPosition();
        AddMalformedIssue(EmailIssueCode.QuotedPrintableTruncatedEscape, "A quoted-printable escape or soft line break was incomplete at the end of input.", escapePosition, "The incomplete sequence was preserved literally.");
        PreservePendingEscape(position, output);
        ClearEscapeState();
        return output.ToArray();
    }

    void ProcessByte(byte value, EmailSourcePosition position, List<byte> output)
    {
        bool processAgain = true;
        while (processAgain && !limitReached)
        {
            processAgain = false;
            if (state == NormalState)
            {
                ProcessNormalByte(value, position, output);
            }
            else if (state == EqualsState)
            {
                processAgain = ProcessAfterEquals(value, position, output);
            }
            else if (state == FirstHexState)
            {
                processAgain = ProcessAfterFirstHex(value, position, output);
            }
            else if (state == EqualsCarriageReturnState)
            {
                processAgain = ProcessAfterEqualsCarriageReturn(value, position, output);
            }
            else if (state == EqualsWhitespaceState)
            {
                processAgain = ProcessAfterEqualsWhitespace(value, position, output);
            }
            else
            {
                processAgain = ProcessAfterEqualsWhitespaceCarriageReturn(value, position, output);
            }
        }
    }

    void ProcessNormalByte(byte value, EmailSourcePosition position, List<byte> output)
    {
        bool followsLiteralCarriageReturn = false;
        if (pendingLiteralCarriageReturn)
        {
            pendingLiteralCarriageReturn = false;
            if (value == 10)
            {
                followsLiteralCarriageReturn = true;
                literalCarriageReturnPosition = null;
            }
            else
            {
                AddInvalidCharacterIssue(literalCarriageReturnPosition, "A carriage return in quoted-printable data was not followed by line feed.");
                literalCarriageReturnPosition = null;
            }
        }

        if (value == 32 || value == 9)
        {
            lineWhitespace.Add(value);
            return;
        }

        if (value == 13 || value == 10)
        {
            if (value == 13)
            {
                pendingLiteralCarriageReturn = true;
                literalCarriageReturnPosition = position;
            }
            else if (!followsLiteralCarriageReturn)
            {
                AddInvalidCharacterIssue(position, "A line feed in quoted-printable data was not preceded by carriage return.");
            }

            lineWhitespace.Clear();
            EmitByte(value, position, output);
            return;
        }

        FlushLineWhitespace(position, output);
        if (limitReached)
        {
            return;
        }

        if (value == 61)
        {
            state = EqualsState;
            escapePosition = position;
            return;
        }

        if (value < 32 || value > 126)
        {
            AddInvalidCharacterIssue(position, "Quoted-printable data contains a raw byte that must be encoded.");
        }

        EmitByte(value, position, output);
    }

    bool ProcessAfterEquals(byte value, EmailSourcePosition position, List<byte> output)
    {
        int hexValue = DecodeHex(value);
        if (hexValue >= 0)
        {
            firstHexByte = value;
            firstHexValue = hexValue;
            firstHexLowercase = IsLowercaseHex(value);
            state = FirstHexState;
            return false;
        }

        if (value == 13)
        {
            state = EqualsCarriageReturnState;
            return false;
        }

        if (value == 10)
        {
            return RecoverLineFeedSoftBreak(position, output);
        }

        if (value == 32 || value == 9)
        {
            escapeWhitespace.Add(value);
            state = EqualsWhitespaceState;
            return false;
        }

        AddInvalidEscapeIssue();
        EmitByte(61, escapePosition, output);
        state = NormalState;
        return !limitReached;
    }

    bool ProcessAfterFirstHex(byte value, EmailSourcePosition position, List<byte> output)
    {
        int secondHexValue = DecodeHex(value);
        if (secondHexValue >= 0)
        {
            if (firstHexLowercase || IsLowercaseHex(value))
            {
                AddMalformedIssue(EmailIssueCode.QuotedPrintableLowercaseHex, "A quoted-printable hexadecimal escape used lowercase letters.", escapePosition, "The lowercase hexadecimal digits were decoded.");
            }

            EmitByte((byte)(firstHexValue * 16 + secondHexValue), position, output);
            ClearEscapeState();
            return false;
        }

        AddInvalidEscapeIssue();
        EmitByte(61, escapePosition, output);
        if (!limitReached)
        {
            EmitByte(firstHexByte, escapePosition, output);
        }

        ClearEscapeState();
        return !limitReached;
    }

    bool ProcessAfterEqualsCarriageReturn(byte value, EmailSourcePosition position, List<byte> output)
    {
        if (value == 10)
        {
            ClearEscapeState();
            return false;
        }

        AddMalformedIssue(EmailIssueCode.QuotedPrintableNonCanonicalSoftBreak, "An equals sign and carriage return were not followed by line feed.", escapePosition, "The malformed soft break was preserved literally.");
        EmitByte(61, escapePosition, output);
        if (!limitReached)
        {
            EmitByte(13, escapePosition, output);
        }

        ClearEscapeState();
        return !limitReached;
    }

    bool ProcessAfterEqualsWhitespace(byte value, EmailSourcePosition position, List<byte> output)
    {
        if (value == 32 || value == 9)
        {
            escapeWhitespace.Add(value);
            return false;
        }

        if (value == 13)
        {
            state = EqualsWhitespaceCarriageReturnState;
            return false;
        }

        if (value == 10)
        {
            return RecoverLineFeedSoftBreak(position, output);
        }

        AddInvalidEscapeIssue();
        PreserveEqualsAndWhitespace(position, output);
        ClearEscapeState();
        return !limitReached;
    }

    bool ProcessAfterEqualsWhitespaceCarriageReturn(byte value, EmailSourcePosition position, List<byte> output)
    {
        if (value == 10)
        {
            ClearEscapeState();
            return false;
        }

        AddMalformedIssue(EmailIssueCode.QuotedPrintableNonCanonicalSoftBreak, "A padded quoted-printable soft break was not terminated by line feed.", escapePosition, "The malformed soft break was preserved literally.");
        PreserveEqualsAndWhitespace(position, output);
        if (!limitReached)
        {
            EmitByte(13, escapePosition, output);
        }

        ClearEscapeState();
        return !limitReached;
    }

    bool RecoverLineFeedSoftBreak(EmailSourcePosition position, List<byte> output)
    {
        if (mode == EmailQuotedPrintableDecodeMode.Tolerant)
        {
            AddMalformedIssue(EmailIssueCode.QuotedPrintableNonCanonicalSoftBreak, "A quoted-printable soft break used line feed without carriage return.", escapePosition, "The non-canonical soft break was removed.");
            ClearEscapeState();
            return false;
        }

        AddMalformedIssue(EmailIssueCode.QuotedPrintableNonCanonicalSoftBreak, "A quoted-printable soft break used line feed without carriage return.", escapePosition, "The non-canonical sequence was preserved literally.");
        PreserveEqualsAndWhitespace(position, output);
        ClearEscapeState();
        return !limitReached;
    }

    void AddInvalidEscapeIssue()
    {
        AddMalformedIssue(EmailIssueCode.QuotedPrintableInvalidEscape, "An equals sign was not followed by two hexadecimal digits or a line break.", escapePosition, "The malformed escape prefix was preserved literally.");
    }

    void AddInvalidCharacterIssue(EmailSourcePosition? position, string description)
    {
        AddMalformedIssue(EmailIssueCode.QuotedPrintableInvalidCharacter, description, position, "The invalid raw byte was preserved literally.");
    }

    void PreservePendingEscape(EmailSourcePosition position, List<byte> output)
    {
        EmitByte(61, escapePosition, output);
        if (limitReached)
        {
            return;
        }

        if (state == FirstHexState)
        {
            EmitByte(firstHexByte, position, output);
            return;
        }

        PreserveEscapeWhitespace(position, output);
        if (!limitReached && (state == EqualsCarriageReturnState || state == EqualsWhitespaceCarriageReturnState))
        {
            EmitByte(13, position, output);
        }
    }

    void PreserveEqualsAndWhitespace(EmailSourcePosition position, List<byte> output)
    {
        EmitByte(61, escapePosition, output);
        if (!limitReached)
        {
            PreserveEscapeWhitespace(position, output);
        }
    }

    void PreserveEscapeWhitespace(EmailSourcePosition position, List<byte> output)
    {
        int index = 0;
        while (index < escapeWhitespace.Count && !limitReached)
        {
            EmitByte(escapeWhitespace[index], position, output);
            index++;
        }
    }

    void FlushLineWhitespace(EmailSourcePosition position, List<byte> output)
    {
        int index = 0;
        while (index < lineWhitespace.Count && !limitReached)
        {
            EmitByte(lineWhitespace[index], position, output);
            index++;
        }

        lineWhitespace.Clear();
    }

    void EmitByte(byte value, EmailSourcePosition? position, List<byte> output)
    {
        if (1 > maximumDecodedBytes - decodedByteCount)
        {
            AddIssue(EmailIssueSeverity.Fatal, EmailIssueCode.DecodedByteLimitExceeded, "Quoted-printable decoded output exceeds the configured byte limit.", position, "Decoding stopped before emitting the byte that crossed the limit.");
            limitReached = true;
            return;
        }

        output.Add(value);
        decodedByteCount++;
    }

    void ClearEscapeState()
    {
        state = NormalState;
        firstHexByte = 0;
        firstHexValue = 0;
        firstHexLowercase = false;
        escapePosition = null;
        escapeWhitespace.Clear();
    }

    void AddMalformedIssue(EmailIssueCode code, string description, EmailSourcePosition? position, string recovery)
    {
        EmailIssueSeverity severity = mode == EmailQuotedPrintableDecodeMode.Strict ? EmailIssueSeverity.Error : EmailIssueSeverity.Warning;
        AddIssue(severity, code, description, position, recovery);
    }

    void AddIssue(EmailIssueSeverity severity, EmailIssueCode code, string description, EmailSourcePosition? position, string recovery)
    {
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, code, description, position, null, null, recovery));
    }

    static bool IsLowercaseHex(byte value)
    {
        return value >= 97 && value <= 102;
    }

    static int DecodeHex(byte value)
    {
        if (value >= 48 && value <= 57)
        {
            return value - 48;
        }

        if (value >= 65 && value <= 70)
        {
            return value - 65 + 10;
        }

        if (value >= 97 && value <= 102)
        {
            return value - 97 + 10;
        }

        return -1;
    }
}
