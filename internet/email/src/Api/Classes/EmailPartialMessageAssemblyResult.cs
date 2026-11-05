using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Describes the outcome of a partial message assembly operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailPartialMessageAssemblyResult : IEmailPartialMessageAssemblyResultContract
{
    string fragmentId;
    long expectedFragmentCount;
    bool hasExpectedFragmentCount;
    bool sequenceComplete;
    bool reparseRequested;
    List<EmailPartialMessageFragment> orderedFragments;
    List<long> missingFragmentNumbers;
    List<long> duplicateFragmentNumbers;
    byte[]? innerBytes;
    byte[]? reassembledBytes;
    EmailMessage? innerMessage;
    EmailMessage? message;
    EmailIssueList issues;
    /// <summary>
    /// Creates a new partial message assembly result.
    /// </summary>
    /// <param name = "fragmentId">The fragment ID used by the operation.</param>
    /// <param name = "expectedFragmentCount">The number of items or bytes to process.</param>
    /// <param name = "hasExpectedFragmentCount">The number of has expected fragment count to process.</param>
    /// <param name = "sequenceComplete">Whether to enable the sequence complete behavior.</param>
    /// <param name = "reparseRequested">Whether to enable the reparse requested behavior.</param>
    /// <param name = "orderedFragments">The ordered fragments used by the operation.</param>
    /// <param name = "missingFragmentNumbers">The missing fragment numbers used by the operation.</param>
    /// <param name = "duplicateFragmentNumbers">The duplicate fragment numbers used by the operation.</param>
    /// <param name = "innerBytes">The source bytes or data to process.</param>
    /// <param name = "reassembledBytes">The source bytes or data to process.</param>
    /// <param name = "innerMessage">The inner message used by the operation.</param>
    /// <param name = "message">The email message to process.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailPartialMessageAssemblyResult(string fragmentId, long expectedFragmentCount, bool hasExpectedFragmentCount, bool sequenceComplete, bool reparseRequested, List<EmailPartialMessageFragment> orderedFragments, List<long> missingFragmentNumbers, List<long> duplicateFragmentNumbers, byte[]? innerBytes, byte[]? reassembledBytes, EmailMessage? innerMessage, EmailMessage? message, EmailIssueList issues)
    {
        if (fragmentId == null)
            throw new ArgumentNullException("fragmentId");
        if (orderedFragments == null || missingFragmentNumbers == null || duplicateFragmentNumbers == null || issues == null)
            throw new ArgumentNullException("orderedFragments");
        this.fragmentId = fragmentId;
        this.expectedFragmentCount = expectedFragmentCount;
        this.hasExpectedFragmentCount = hasExpectedFragmentCount;
        this.sequenceComplete = sequenceComplete;
        this.reparseRequested = reparseRequested;
        this.orderedFragments = new List<EmailPartialMessageFragment>(orderedFragments);
        this.missingFragmentNumbers = new List<long>(missingFragmentNumbers);
        this.duplicateFragmentNumbers = new List<long>(duplicateFragmentNumbers);
        this.innerBytes = CopyNullable(innerBytes);
        this.reassembledBytes = CopyNullable(reassembledBytes);
        this.innerMessage = innerMessage;
        this.message = message;
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
    }

    /// <summary>
    /// Gets the fragment ID.
    /// </summary>
    /// <returns>The current fragment ID.</returns>
    public string GetFragmentId()
    {
        return fragmentId;
    }

    /// <summary>
    /// Determines whether the expected fragment count is present or available.
    /// </summary>
    /// <returns><c>true</c> when the expected fragment count is available; otherwise, <c>false</c>.</returns>
    public bool HasExpectedFragmentCount()
    {
        return hasExpectedFragmentCount;
    }

    /// <summary>
    /// Gets the expected fragment count.
    /// </summary>
    /// <returns>The current expected fragment count.</returns>
    public long GetExpectedFragmentCount()
    {
        return expectedFragmentCount;
    }

    /// <summary>
    /// Determines whether the current value or state is sequence complete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is sequence complete; otherwise, <c>false</c>.</returns>
    public bool IsSequenceComplete()
    {
        return sequenceComplete;
    }

    /// <summary>
    /// Gets the fragment count.
    /// </summary>
    /// <returns>The current fragment count.</returns>
    public int GetFragmentCount()
    {
        return orderedFragments.Count;
    }

    /// <summary>
    /// Gets the fragment at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested fragment.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailPartialMessageFragment GetFragmentAt(int index)
    {
        if (index < 0 || index >= orderedFragments.Count)
            throw new ArgumentOutOfRangeException("index");
        return orderedFragments[index];
    }

    /// <summary>
    /// Gets the fragments.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current fragments.</returns>
    public List<EmailPartialMessageFragment> GetFragments()
    {
        return new List<EmailPartialMessageFragment>(orderedFragments);
    }

    /// <summary>
    /// Gets the missing fragment count.
    /// </summary>
    /// <returns>The current missing fragment count.</returns>
    public int GetMissingFragmentCount()
    {
        return missingFragmentNumbers.Count;
    }

    /// <summary>
    /// Gets the missing fragment number at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested missing fragment number.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public long GetMissingFragmentNumberAt(int index)
    {
        if (index < 0 || index >= missingFragmentNumbers.Count)
            throw new ArgumentOutOfRangeException("index");
        return missingFragmentNumbers[index];
    }

    /// <summary>
    /// Gets the missing fragment numbers.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current missing fragment numbers.</returns>
    public List<long> GetMissingFragmentNumbers()
    {
        return new List<long>(missingFragmentNumbers);
    }

    /// <summary>
    /// Gets the duplicate fragment count.
    /// </summary>
    /// <returns>The current duplicate fragment count.</returns>
    public int GetDuplicateFragmentCount()
    {
        return duplicateFragmentNumbers.Count;
    }

    /// <summary>
    /// Gets the duplicate fragment number at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested duplicate fragment number.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public long GetDuplicateFragmentNumberAt(int index)
    {
        if (index < 0 || index >= duplicateFragmentNumbers.Count)
            throw new ArgumentOutOfRangeException("index");
        return duplicateFragmentNumbers[index];
    }

    /// <summary>
    /// Gets the duplicate fragment numbers.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current duplicate fragment numbers.</returns>
    public List<long> GetDuplicateFragmentNumbers()
    {
        return new List<long>(duplicateFragmentNumbers);
    }

    /// <summary>
    /// Determines whether the current value or state is assembled.
    /// </summary>
    /// <returns><c>true</c> when the value or state is assembled; otherwise, <c>false</c>.</returns>
    public bool IsAssembled()
    {
        return innerBytes != null;
    }

    /// <summary>
    /// Gets the inner bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current inner bytes.</returns>
    public byte[]? GetInnerBytes()
    {
        return CopyNullable(innerBytes);
    }

    /// <summary>
    /// Determines whether the operation or source value was reparse requested.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool WasReparseRequested()
    {
        return reparseRequested;
    }

    /// <summary>
    /// Determines whether the inner message is present or available.
    /// </summary>
    /// <returns><c>true</c> when the inner message is available; otherwise, <c>false</c>.</returns>
    public bool HasInnerMessage()
    {
        return innerMessage != null;
    }

    /// <summary>
    /// Gets the inner message.
    /// </summary>
    /// <returns>The current inner message.</returns>
    public EmailMessage? GetInnerMessage()
    {
        return innerMessage;
    }

    /// <summary>
    /// Determines whether the message is present or available.
    /// </summary>
    /// <returns><c>true</c> when the message is available; otherwise, <c>false</c>.</returns>
    public bool HasMessage()
    {
        return message != null;
    }

    /// <summary>
    /// Gets the message.
    /// </summary>
    /// <returns>The current message.</returns>
    public EmailMessage? GetMessage()
    {
        return message;
    }

    /// <summary>
    /// Determines whether the reassembled bytes is present or available.
    /// </summary>
    /// <returns><c>true</c> when the reassembled bytes is available; otherwise, <c>false</c>.</returns>
    public bool HasReassembledBytes()
    {
        return reassembledBytes != null;
    }

    /// <summary>
    /// Gets the reassembled bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current reassembled bytes.</returns>
    public byte[]? GetReassembledBytes()
    {
        return CopyNullable(reassembledBytes);
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
        if (!sequenceComplete || innerBytes == null || issues.HasSeverityAtLeast(EmailIssueSeverity.Error))
            return false;
        return !reparseRequested || message != null;
    }

    static byte[]? CopyNullable(byte[]? value)
    {
        if (value == null)
            return null;
        byte[] copy = new byte[value.Length];
        value.CopyTo(copy, 0);
        return copy;
    }
}
