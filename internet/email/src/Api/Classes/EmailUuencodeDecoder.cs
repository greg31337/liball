using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Incrementally decodes uuencode.
/// </summary>
public class EmailUuencodeDecoder : IEmailUuencodeDecoderContract
{
    const int SearchingState = 0;
    const int DataState = 1;
    const int ZeroLineState = 2;
    const int EndedState = 3;
    long maximumDecodedBytes;
    int maximumLineBytes;
    long decodedByteCount;
    int state;
    EmailUuencodeDetectionResult detection;
    List<byte> lineBuffer;
    EmailSourcePosition linePosition;
    bool lineOverflow;
    EmailSourcePosition? lineOverflowPosition;
    bool previousWasCarriageReturn;
    bool limitReached;
    bool completed;
    EmailSourceTracker sourceTracker;
    EmailIssueList issues;
    /// <summary>
    /// Creates a new uuencode decoder.
    /// </summary>
    /// <param name = "maximumDecodedBytes">The source bytes or data to process.</param>
    /// <param name = "maximumLineBytes">The source bytes or data to process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailUuencodeDecoder(long maximumDecodedBytes, int maximumLineBytes)
    {
        if (maximumDecodedBytes < 0)
        {
            throw new ArgumentOutOfRangeException("maximumDecodedBytes");
        }

        if (maximumLineBytes < 11)
        {
            throw new ArgumentOutOfRangeException("maximumLineBytes");
        }

        this.maximumDecodedBytes = maximumDecodedBytes;
        this.maximumLineBytes = maximumLineBytes;
        decodedByteCount = 0;
        state = SearchingState;
        detection = new EmailUuencodeDetectionResultOperations().CreateNotDetected();
        lineBuffer = new List<byte>();
        sourceTracker = new EmailSourceTrackerOperations().CreateWith();
        linePosition = sourceTracker.GetPosition();
        lineOverflow = false;
        lineOverflowPosition = null;
        previousWasCarriageReturn = false;
        limitReached = false;
        completed = false;
        issues = new EmailIssueList();
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
    /// Gets the maximum line bytes.
    /// </summary>
    /// <returns>The current maximum line bytes.</returns>
    public int GetMaximumLineBytes()
    {
        return maximumLineBytes;
    }

    /// <summary>
    /// Gets the source byte count.
    /// </summary>
    /// <returns>The current source byte count.</returns>
    public long GetSourceByteCount()
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
    /// Gets the detection.
    /// </summary>
    /// <returns>The current detection.</returns>
    public EmailUuencodeDetectionResult GetDetection()
    {
        return detection;
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
    /// Determines whether the current value or state is complete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is complete; otherwise, <c>false</c>.</returns>
    public bool IsComplete()
    {
        return completed;
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
            throw new InvalidOperationException("Uuencoded input cannot be supplied after completion.");
        }

        List<byte> output = new List<byte>();
        int index = 0;
        while (index < input.Length)
        {
            EmailSourcePosition position = sourceTracker.GetPosition();
            byte value = input[index];
            sourceTracker.Advance(input.Slice(index, 1));
            if (value == 13)
            {
                ProcessLine(output);
                previousWasCarriageReturn = true;
                linePosition = sourceTracker.GetPosition();
            }
            else if (value == 10)
            {
                if (!previousWasCarriageReturn)
                {
                    ProcessLine(output);
                }

                previousWasCarriageReturn = false;
                linePosition = sourceTracker.GetPosition();
            }
            else
            {
                previousWasCarriageReturn = false;
                if (lineBuffer.Count < maximumLineBytes)
                {
                    lineBuffer.Add(value);
                }
                else if (!lineOverflow)
                {
                    lineOverflow = true;
                    lineOverflowPosition = position;
                }
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
            throw new InvalidOperationException("Uuencode decoding has already completed.");
        }

        completed = true;
        List<byte> output = new List<byte>();
        if (lineBuffer.Count > 0 || lineOverflow)
        {
            ProcessLine(output);
        }

        if (!limitReached)
        {
            if (state == SearchingState)
            {
                AddIssue(EmailIssueSeverity.Error, EmailIssueCode.UuencodeHeaderNotFound, "No traditional uuencode begin header was found.", sourceTracker.GetPosition(), "No decoded bytes were produced.");
            }
            else if (state == DataState)
            {
                AddIssue(EmailIssueSeverity.Warning, EmailIssueCode.UuencodeMissingTermination, "The uuencoded block ended without a zero-length line and end marker.", sourceTracker.GetPosition(), "All recoverable data lines were decoded.");
            }
            else if (state == ZeroLineState)
            {
                AddIssue(EmailIssueSeverity.Warning, EmailIssueCode.UuencodeMissingTermination, "The uuencoded block ended without its end marker.", sourceTracker.GetPosition(), "All recoverable data lines were decoded.");
            }
        }

        return output.ToArray();
    }

    void ProcessLine(List<byte> output)
    {
        byte[] line = lineBuffer.ToArray();
        if (state == SearchingState)
        {
            EmailUuencodeDetectionResult found = new EmailUuencode().ParseHeader(line, linePosition);
            if (found.IsDetected())
            {
                detection = found;
                state = DataState;
            }
        }
        else if (!limitReached && state != EndedState)
        {
            if (lineOverflow)
            {
                AddIssue(EmailIssueSeverity.Warning, EmailIssueCode.UuencodeInvalidLine, "A uuencoded line exceeds the configured encoded-line limit.", lineOverflowPosition, "The retained line prefix was processed tolerantly.");
            }

            if (state == DataState)
            {
                ProcessDataLine(line, output);
            }
            else if (state == ZeroLineState)
            {
                ProcessEndLine(line);
            }
        }

        lineBuffer.Clear();
        lineOverflow = false;
        lineOverflowPosition = null;
    }

    void ProcessDataLine(byte[] line, List<byte> output)
    {
        if (IsEndLine(line))
        {
            AddIssue(EmailIssueSeverity.Warning, EmailIssueCode.UuencodeMissingTermination, "The uuencoded block omitted its zero-length data line.", linePosition, "The end marker was accepted.");
            state = EndedState;
            return;
        }

        if (line.Length == 0)
        {
            AddIssue(EmailIssueSeverity.Warning, EmailIssueCode.UuencodeInvalidLine, "An empty line was used as a stripped zero-length uuencode line.", linePosition, "The empty line was accepted as the data terminator.");
            state = ZeroLineState;
            return;
        }

        int declaredCount = DecodeCharacter(line[0], CreateLinePosition(0));
        if (declaredCount == 0)
        {
            if (HasNonWhitespaceAfter(line, 1))
            {
                AddIssue(EmailIssueSeverity.Warning, EmailIssueCode.UuencodeInvalidLine, "A zero-length uuencode line contained trailing data.", linePosition, "The trailing data was ignored.");
            }

            state = ZeroLineState;
            return;
        }

        int decodedCount = declaredCount;
        if (decodedCount > 45)
        {
            decodedCount = 45;
            AddIssue(EmailIssueSeverity.Warning, EmailIssueCode.UuencodeInvalidLine, "A uuencode data line declared more than 45 decoded bytes.", linePosition, "The declared count was clamped to 45 bytes.");
        }

        int expectedCharacters = (decodedCount + 2) / 3 * 4;
        int availableCharacters = line.Length - 1;
        if (availableCharacters < expectedCharacters)
        {
            AddIssue(EmailIssueSeverity.Warning, EmailIssueCode.UuencodeTruncatedLine, "A uuencode data line ended before its declared payload was complete.", CreateLinePosition(line.Length), "All bytes recoverable from complete character pairs were decoded.");
        }

        DecodePayload(line, decodedCount, expectedCharacters, output);
        if (line.Length > expectedCharacters + 1 && HasNonWhitespaceAfter(line, expectedCharacters + 1))
        {
            AddIssue(EmailIssueSeverity.Warning, EmailIssueCode.UuencodeInvalidLine, "A uuencode data line contained characters after its declared payload.", CreateLinePosition(expectedCharacters + 1), "The trailing characters were ignored.");
        }
    }

    void DecodePayload(byte[] line, int decodedCount, int expectedCharacters, List<byte> output)
    {
        int payloadIndex = 0;
        int remaining = decodedCount;
        while (remaining > 0 && payloadIndex < expectedCharacters && payloadIndex + 1 < line.Length && !limitReached)
        {
            int available = line.Length - payloadIndex - 1;
            if (available > 4)
            {
                available = 4;
            }

            int first = DecodeCharacter(line[payloadIndex + 1], CreateLinePosition(payloadIndex + 1));
            if (available < 2)
            {
                break;
            }

            int second = DecodeCharacter(line[payloadIndex + 2], CreateLinePosition(payloadIndex + 2));
            EmitByte((byte)(first << 2 | second >> 4), CreateLinePosition(payloadIndex + 2), output);
            remaining--;
            if (remaining > 0 && available >= 3 && !limitReached)
            {
                int third = DecodeCharacter(line[payloadIndex + 3], CreateLinePosition(payloadIndex + 3));
                EmitByte((byte)((second & 15) << 4 | third >> 2), CreateLinePosition(payloadIndex + 3), output);
                remaining--;
                if (remaining > 0 && available >= 4 && !limitReached)
                {
                    int fourth = DecodeCharacter(line[payloadIndex + 4], CreateLinePosition(payloadIndex + 4));
                    EmitByte((byte)((third & 3) << 6 | fourth), CreateLinePosition(payloadIndex + 4), output);
                    remaining--;
                }
            }

            payloadIndex += 4;
        }
    }

    void ProcessEndLine(byte[] line)
    {
        if (IsEndLine(line))
        {
            state = EndedState;
            return;
        }

        if (line.Length > 0)
        {
            AddIssue(EmailIssueSeverity.Warning, EmailIssueCode.UuencodeInvalidLine, "Unexpected content appeared between the zero-length line and end marker.", linePosition, "The unexpected line was ignored while searching for the end marker.");
        }
    }

    int DecodeCharacter(byte value, EmailSourcePosition position)
    {
        if (value == 96)
        {
            return 0;
        }

        if (value >= 32 && value <= 95)
        {
            return value - 32;
        }

        AddIssue(EmailIssueSeverity.Warning, EmailIssueCode.UuencodeInvalidCharacter, "A byte outside the traditional uuencode character range was encountered.", position, "The low six bits of the adjusted byte were used.");
        return value - 32 & 63;
    }

    void EmitByte(byte value, EmailSourcePosition position, List<byte> output)
    {
        if (decodedByteCount >= maximumDecodedBytes)
        {
            AddIssue(EmailIssueSeverity.Fatal, EmailIssueCode.UuencodeDecodedByteLimitExceeded, "Uuencode decoded output exceeds the configured byte limit.", position, "Decoding stopped before emitting the byte that crossed the limit.");
            limitReached = true;
            return;
        }

        output.Add(value);
        decodedByteCount++;
    }

    EmailSourcePosition CreateLinePosition(int columnOffset)
    {
        long byteOffset = new EmailSafeArithmetic().AddNonNegative(linePosition.GetByteOffset(), columnOffset);
        long columnNumber = new EmailSafeArithmetic().AddNonNegative(linePosition.GetColumnNumber(), columnOffset);
        return new EmailSourcePosition(byteOffset, linePosition.GetLineNumber(), columnNumber);
    }

    void AddIssue(EmailIssueSeverity severity, EmailIssueCode code, string description, EmailSourcePosition? position, string recovery)
    {
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, code, description, position, null, null, recovery));
    }

    static bool IsEndLine(byte[] line)
    {
        if (line.Length < 3 || line[0] != 101 || line[1] != 110 || line[2] != 100)
        {
            return false;
        }

        int index = 3;
        while (index < line.Length)
        {
            if (line[index] != 32 && line[index] != 9)
            {
                return false;
            }

            index++;
        }

        return true;
    }

    static bool HasNonWhitespaceAfter(byte[] line, int start)
    {
        int index = start;
        while (index < line.Length)
        {
            if (line[index] != 32 && line[index] != 9)
            {
                return true;
            }

            index++;
        }

        return false;
    }
}
