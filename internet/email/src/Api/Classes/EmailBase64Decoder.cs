using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Incrementally decodes base64.
/// </summary>
public class EmailBase64Decoder : IEmailBase64DecoderContract
{
    EmailBase64DecodeMode mode;
    long maximumDecodedBytes;
    long decodedByteCount;
    int[] quantum;
    int quantumCount;
    bool terminalPaddingSeen;
    bool limitReached;
    bool completed;
    EmailSourceTracker sourceTracker;
    EmailIssueList issues;
    /// <summary>
    /// Creates a new base64 decoder.
    /// </summary>
    /// <param name = "mode">The mode used by the operation.</param>
    /// <param name = "maximumDecodedBytes">The source bytes or data to process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailBase64Decoder(EmailBase64DecodeMode mode, long maximumDecodedBytes)
    {
        if (mode < EmailBase64DecodeMode.Strict || mode > EmailBase64DecodeMode.Tolerant)
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
        quantum = new int[4];
        quantumCount = 0;
        terminalPaddingSeen = false;
        limitReached = false;
        completed = false;
        sourceTracker = new EmailSourceTrackerOperations().CreateWith();
        issues = new EmailIssueList();
    }

    /// <summary>
    /// Gets the mode.
    /// </summary>
    /// <returns>The current mode.</returns>
    public EmailBase64DecodeMode GetMode()
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
            throw new InvalidOperationException("Base64 input cannot be supplied after completion.");
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
            throw new InvalidOperationException("Base64 decoding has already completed.");
        }

        completed = true;
        List<byte> output = new List<byte>();
        if (limitReached || quantumCount == 0)
        {
            return output.ToArray();
        }

        EmailSourcePosition position = sourceTracker.GetPosition();
        if (quantumCount == 1)
        {
            AddMalformedIssue(EmailIssueCode.Base64TruncatedQuantum, "The final Base64 quantum contains only one alphabet character.", position, "The incomplete quantum was ignored.");
            quantumCount = 0;
            return output.ToArray();
        }

        if (quantumCount == 2)
        {
            string recovery = "One recoverable decoded byte was produced.";
            if (1 > maximumDecodedBytes - decodedByteCount)
            {
                recovery = "The recoverable byte was not emitted because it would exceed the decoded-byte limit.";
            }

            AddMalformedIssue(EmailIssueCode.Base64TruncatedQuantum, "The final Base64 quantum is missing two padding characters.", position, recovery);
            DecodeOneByte(position, output);
            quantumCount = 0;
            return output.ToArray();
        }

        if (quantum[2] < 0)
        {
            string recovery = "One recoverable decoded byte was produced.";
            if (1 > maximumDecodedBytes - decodedByteCount)
            {
                recovery = "The recoverable byte was not emitted because it would exceed the decoded-byte limit.";
            }

            AddMalformedIssue(EmailIssueCode.Base64TruncatedQuantum, "The final Base64 quantum is missing its second padding character.", position, recovery);
            DecodeOneByte(position, output);
        }
        else
        {
            string recovery = "Two recoverable decoded bytes were produced.";
            if (2 > maximumDecodedBytes - decodedByteCount)
            {
                recovery = "The recoverable bytes were not emitted because they would exceed the decoded-byte limit.";
            }

            AddMalformedIssue(EmailIssueCode.Base64TruncatedQuantum, "The final Base64 quantum is missing one padding character.", position, recovery);
            DecodeTwoBytes(position, output);
        }

        quantumCount = 0;
        return output.ToArray();
    }

    void ProcessByte(byte value, EmailSourcePosition position, List<byte> output)
    {
        if (IsStandardWhitespace(value))
        {
            return;
        }

        if (value == 11 || value == 12)
        {
            if (mode == EmailBase64DecodeMode.Tolerant)
            {
                AddIssue(EmailIssueSeverity.Warning, EmailIssueCode.Base64NonCanonicalWhitespace, "A non-canonical ASCII whitespace byte appeared in Base64 content.", position, "The whitespace byte was ignored.");
                return;
            }

            AddMalformedIssue(EmailIssueCode.Base64InvalidCharacter, "A non-canonical ASCII whitespace byte appeared in strict Base64 content.", position, "The invalid byte was ignored while retaining a partial result.");
            return;
        }

        int decodedValue = DecodeAlphabet(value);
        if (decodedValue >= 0)
        {
            ProcessAlphabet(decodedValue, position, output);
            return;
        }

        if (value == 61)
        {
            ProcessPadding(position, output);
            return;
        }

        AddMalformedIssue(EmailIssueCode.Base64InvalidCharacter, "An invalid byte appeared in Base64 content.", position, "The invalid byte was ignored while retaining a partial result.");
    }

    void ProcessAlphabet(int value, EmailSourcePosition position, List<byte> output)
    {
        if (terminalPaddingSeen)
        {
            AddMalformedIssue(EmailIssueCode.Base64DataAfterPadding, "Base64 alphabet data appeared after terminal padding.", position, "The trailing alphabet byte was ignored.");
            return;
        }

        if (quantumCount == 3 && quantum[2] < 0)
        {
            AddMalformedIssue(EmailIssueCode.Base64InvalidPadding, "A Base64 alphabet byte appeared after padding inside a quantum.", position, "The misplaced padding was discarded and decoding continued.");
            quantum[2] = value;
            return;
        }

        quantum[quantumCount] = value;
        quantumCount++;
        if (quantumCount == 4)
        {
            DecodeThreeBytes(position, output);
            quantumCount = 0;
        }
    }

    void ProcessPadding(EmailSourcePosition position, List<byte> output)
    {
        if (terminalPaddingSeen)
        {
            AddMalformedIssue(EmailIssueCode.Base64InvalidPadding, "Additional Base64 padding appeared after a terminal padded quantum.", position, "The extra padding was ignored.");
            return;
        }

        if (quantumCount < 2)
        {
            AddMalformedIssue(EmailIssueCode.Base64InvalidPadding, "Base64 padding appeared before two alphabet characters.", position, "The misplaced padding was ignored.");
            return;
        }

        quantum[quantumCount] = -1;
        quantumCount++;
        if (quantumCount < 4)
        {
            return;
        }

        if (quantum[2] < 0)
        {
            DecodeOneByte(position, output);
        }
        else
        {
            DecodeTwoBytes(position, output);
        }

        quantumCount = 0;
        terminalPaddingSeen = true;
    }

    void DecodeOneByte(EmailSourcePosition position, List<byte> output)
    {
        if (!ReserveDecodedBytes(1, position))
        {
            return;
        }

        if ((quantum[1] & 15) != 0)
        {
            AddMalformedIssue(EmailIssueCode.Base64NonZeroPaddingBits, "The unused bits before Base64 double padding are not zero.", position, "The recoverable high-order byte was decoded.");
        }

        output.Add((byte)(quantum[0] << 2 | quantum[1] >> 4));
    }

    void DecodeTwoBytes(EmailSourcePosition position, List<byte> output)
    {
        if (!ReserveDecodedBytes(2, position))
        {
            return;
        }

        if ((quantum[2] & 3) != 0)
        {
            AddMalformedIssue(EmailIssueCode.Base64NonZeroPaddingBits, "The unused bits before Base64 padding are not zero.", position, "The two recoverable high-order bytes were decoded.");
        }

        output.Add((byte)(quantum[0] << 2 | quantum[1] >> 4));
        output.Add((byte)((quantum[1] & 15) << 4 | quantum[2] >> 2));
    }

    void DecodeThreeBytes(EmailSourcePosition position, List<byte> output)
    {
        if (!ReserveDecodedBytes(3, position))
        {
            return;
        }

        output.Add((byte)(quantum[0] << 2 | quantum[1] >> 4));
        output.Add((byte)((quantum[1] & 15) << 4 | quantum[2] >> 2));
        output.Add((byte)((quantum[2] & 3) << 6 | quantum[3]));
    }

    bool ReserveDecodedBytes(int count, EmailSourcePosition position)
    {
        if (count > maximumDecodedBytes - decodedByteCount)
        {
            AddIssue(EmailIssueSeverity.Fatal, EmailIssueCode.DecodedByteLimitExceeded, "Base64 decoded output exceeds the configured byte limit.", position, "Decoding stopped before emitting the quantum that crossed the limit.");
            limitReached = true;
            return false;
        }

        decodedByteCount += count;
        return true;
    }

    void AddMalformedIssue(EmailIssueCode code, string description, EmailSourcePosition position, string recovery)
    {
        EmailIssueSeverity severity = mode == EmailBase64DecodeMode.Strict ? EmailIssueSeverity.Error : EmailIssueSeverity.Warning;
        AddIssue(severity, code, description, position, recovery);
    }

    void AddIssue(EmailIssueSeverity severity, EmailIssueCode code, string description, EmailSourcePosition position, string recovery)
    {
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, code, description, position, null, null, recovery));
    }

    static bool IsStandardWhitespace(byte value)
    {
        return value == 32 || value == 9 || value == 13 || value == 10;
    }

    static int DecodeAlphabet(byte value)
    {
        if (value >= 65 && value <= 90)
        {
            return value - 65;
        }

        if (value >= 97 && value <= 122)
        {
            return value - 97 + 26;
        }

        if (value >= 48 && value <= 57)
        {
            return value - 48 + 52;
        }

        if (value == 43)
        {
            return 62;
        }

        if (value == 47)
        {
            return 63;
        }

        return -1;
    }
}
