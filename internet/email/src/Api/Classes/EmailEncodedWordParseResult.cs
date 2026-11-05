using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Describes the outcome of a encoded word parse operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailEncodedWordParseResult : IEmailEncodedWordParseResultContract
{
    List<EmailEncodedWordPart> parts;
    EmailIssueList issues;
    int encodedWordCount;
    long decodedByteCount;
    /// <summary>
    /// Creates a new encoded word parse result.
    /// </summary>
    /// <param name = "parts">The parts used by the operation.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <param name = "encodedWordCount">The number of items or bytes to process.</param>
    /// <param name = "decodedByteCount">The number of items or bytes to process.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailEncodedWordParseResult(List<EmailEncodedWordPart> parts, EmailIssueList issues, int encodedWordCount, long decodedByteCount)
    {
        if (parts == null)
        {
            throw new ArgumentNullException("parts");
        }

        if (issues == null)
        {
            throw new ArgumentNullException("issues");
        }

        if (encodedWordCount < 0)
        {
            throw new ArgumentOutOfRangeException("encodedWordCount");
        }

        if (decodedByteCount < 0)
        {
            throw new ArgumentOutOfRangeException("decodedByteCount");
        }

        this.parts = new List<EmailEncodedWordPart>();
        int index = 0;
        while (index < parts.Count)
        {
            if (parts[index] == null)
            {
                throw new ArgumentException("Encoded-word result parts cannot contain null.", "parts");
            }

            this.parts.Add(parts[index]);
            index++;
        }

        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
        this.encodedWordCount = encodedWordCount;
        this.decodedByteCount = decodedByteCount;
    }

    /// <summary>
    /// Gets the part count.
    /// </summary>
    /// <returns>The current part count.</returns>
    public int GetPartCount()
    {
        return parts.Count;
    }

    /// <summary>
    /// Gets the part.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current part.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailEncodedWordPart GetPart(int index)
    {
        if (index < 0 || index >= parts.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return parts[index];
    }

    /// <summary>
    /// Gets the parts.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current parts.</returns>
    public List<EmailEncodedWordPart> GetParts()
    {
        return new List<EmailEncodedWordPart>(parts);
    }

    /// <summary>
    /// Gets the encoded word count.
    /// </summary>
    /// <returns>The current encoded word count.</returns>
    public int GetEncodedWordCount()
    {
        return encodedWordCount;
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
    /// Gets the combined bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current combined bytes.</returns>
    public byte[] GetCombinedBytes()
    {
        long length = 0;
        int index = 0;
        while (index < parts.Count)
        {
            length = new EmailSafeArithmetic().AddNonNegative(length, parts[index].GetByteCount());
            index++;
        }

        byte[] combined = new byte[new EmailSafeArithmetic().ToInt32NonNegative(length)];
        int offset = 0;
        index = 0;
        while (index < parts.Count)
        {
            byte[] value = parts[index].GetBytes();
            value.CopyTo(combined, offset);
            offset += value.Length;
            index++;
        }

        return combined;
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
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return !issues.HasSeverityAtLeast(EmailIssueSeverity.Error);
    }

    /// <summary>
    /// Determines whether the fatal error is present or available.
    /// </summary>
    /// <returns><c>true</c> when the fatal error is available; otherwise, <c>false</c>.</returns>
    public bool HasFatalError()
    {
        return issues.HasSeverityAtLeast(EmailIssueSeverity.Fatal);
    }
}
