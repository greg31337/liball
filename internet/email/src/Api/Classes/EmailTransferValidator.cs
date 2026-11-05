using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Validates transfer and reports policy violations.
/// </summary>
public class EmailTransferValidator : IEmailTransferValidatorContract
{
    EmailTransferEncodingKind kind;
    long maximumBytes;
    int maximumLineBytes;
    long copiedByteCount;
    long currentLineByteCount;
    bool lineTooLongReported;
    bool pendingCarriageReturn;
    EmailSourcePosition? carriageReturnPosition;
    bool limitReached;
    bool cancelled;
    bool completed;
    EmailSourceTracker sourceTracker;
    EmailIssueList issues;
    /// <summary>
    /// Creates a new transfer validator.
    /// </summary>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <param name = "maximumBytes">The source bytes or data to process.</param>
    /// <param name = "maximumLineBytes">The source bytes or data to process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailTransferValidator(EmailTransferEncodingKind kind, long maximumBytes, int maximumLineBytes)
    {
        new EmailTransferEncoding().ValidateIdentityKind(kind);
        if (maximumBytes < 0)
        {
            throw new ArgumentOutOfRangeException("maximumBytes");
        }

        if (maximumLineBytes <= 0)
        {
            throw new ArgumentOutOfRangeException("maximumLineBytes");
        }

        this.kind = kind;
        this.maximumBytes = maximumBytes;
        this.maximumLineBytes = maximumLineBytes;
        copiedByteCount = 0;
        currentLineByteCount = 0;
        lineTooLongReported = false;
        pendingCarriageReturn = false;
        carriageReturnPosition = null;
        limitReached = false;
        cancelled = false;
        completed = false;
        sourceTracker = new EmailSourceTrackerOperations().CreateWith();
        issues = new EmailIssueList();
    }

    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    public EmailTransferEncodingKind GetKind()
    {
        return kind;
    }

    /// <summary>
    /// Gets the maximum bytes.
    /// </summary>
    /// <returns>The current maximum bytes.</returns>
    public long GetMaximumBytes()
    {
        return maximumBytes;
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
    /// Gets the copied byte count.
    /// </summary>
    /// <returns>The current copied byte count.</returns>
    public long GetCopiedByteCount()
    {
        return copiedByteCount;
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
    /// Determines whether the current value or state is cancelled.
    /// </summary>
    /// <returns><c>true</c> when the value or state is cancelled; otherwise, <c>false</c>.</returns>
    public bool IsCancelled()
    {
        return cancelled;
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
            throw new InvalidOperationException("Identity transfer input cannot be supplied after completion.");
        }

        if (cancelled)
        {
            throw new InvalidOperationException("Identity transfer input cannot be supplied after cancellation.");
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
                if (copiedByteCount >= maximumBytes)
                {
                    AddIssue(EmailIssueSeverity.Fatal, EmailIssueCode.TransferEncodingByteLimitExceeded, "Identity transfer output exceeds the configured byte limit.", position, "Copying stopped before the byte that crossed the limit.");
                    limitReached = true;
                }
                else
                {
                    output.Add(value);
                    copiedByteCount++;
                    if (kind != EmailTransferEncodingKind.Binary)
                    {
                        ValidateTransportByte(value, position);
                    }
                }
            }

            index++;
        }

        return output.ToArray();
    }

    /// <summary>
    /// Determines whether the instance can cel.
    /// </summary>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    public void Cancel()
    {
        if (completed)
        {
            throw new InvalidOperationException("Identity transfer validation has already completed.");
        }

        if (cancelled || limitReached)
        {
            return;
        }

        cancelled = true;
        pendingCarriageReturn = false;
        carriageReturnPosition = null;
        AddIssue(EmailIssueSeverity.Error, EmailIssueCode.OperationCancelled, "Identity transfer was cancelled.", sourceTracker.GetPosition(), "Copying stopped at the current source position.");
    }

    /// <summary>
    /// Finishes incremental processing and returns the final output or status.
    /// </summary>
    /// <remarks>
    /// Calling this method finalizes buffered state; use the instance's status method before attempting further input.
    /// </remarks>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    public void Complete()
    {
        if (completed)
        {
            throw new InvalidOperationException("Identity transfer validation has already completed.");
        }

        completed = true;
        if (!cancelled && !limitReached && kind != EmailTransferEncodingKind.Binary && pendingCarriageReturn)
        {
            AddLineBreakIssue(carriageReturnPosition, "A carriage return at end of input was not followed by line feed.");
        }

        pendingCarriageReturn = false;
        carriageReturnPosition = null;
    }

    /// <summary>
    /// Gets the result.
    /// </summary>
    /// <returns>The current result.</returns>
    /// <exception cref = "InvalidOperationException">Thrown when the instance is not in a state that permits this operation.</exception>
    public EmailTransferValidationResult GetResult()
    {
        if (!completed)
        {
            throw new InvalidOperationException("Identity transfer validation must complete before a result is requested.");
        }

        return new EmailTransferValidationResult(kind, sourceTracker.GetByteOffset(), copiedByteCount, cancelled, issues);
    }

    void ValidateTransportByte(byte value, EmailSourcePosition position)
    {
        if (pendingCarriageReturn)
        {
            pendingCarriageReturn = false;
            if (value == 10)
            {
                ResetLine();
                carriageReturnPosition = null;
                return;
            }

            AddLineBreakIssue(carriageReturnPosition, "A carriage return was not followed by line feed.");
            ResetLine();
            carriageReturnPosition = null;
        }

        if (value == 13)
        {
            pendingCarriageReturn = true;
            carriageReturnPosition = position;
            return;
        }

        if (value == 10)
        {
            AddLineBreakIssue(position, "A line feed appeared without a preceding carriage return.");
            ResetLine();
            return;
        }

        if (value == 0)
        {
            AddIssue(EmailIssueSeverity.Error, EmailIssueCode.TransferEncodingNullByte, "A NUL byte is not permitted by 7bit or 8bit transport encoding.", position, "The byte was copied unchanged for deterministic identity output.");
        }

        if (kind == EmailTransferEncodingKind.SevenBit && value > 127)
        {
            AddIssue(EmailIssueSeverity.Error, EmailIssueCode.TransferEncodingHighBitByte, "A byte with its high bit set is not permitted by 7bit transport encoding.", position, "The byte was copied unchanged for deterministic identity output.");
        }

        currentLineByteCount++;
        if (!lineTooLongReported && currentLineByteCount > maximumLineBytes)
        {
            lineTooLongReported = true;
            AddIssue(EmailIssueSeverity.Error, EmailIssueCode.TransferEncodingLineTooLong, "A transport line exceeds the configured byte limit.", position, "The overlong line was copied unchanged.");
        }
    }

    void AddLineBreakIssue(EmailSourcePosition? position, string description)
    {
        AddIssue(EmailIssueSeverity.Error, EmailIssueCode.TransferEncodingNonCanonicalLineBreak, description, position, "The non-canonical line break was copied unchanged.");
    }

    void ResetLine()
    {
        currentLineByteCount = 0;
        lineTooLongReported = false;
    }

    void AddIssue(EmailIssueSeverity severity, EmailIssueCode code, string description, EmailSourcePosition? position, string recovery)
    {
        issues.Add(new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, code, description, position, null, null, recovery));
    }
}
