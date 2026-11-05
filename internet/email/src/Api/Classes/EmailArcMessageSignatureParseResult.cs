using System;

namespace email;
/// <summary>
/// Describes the outcome of a ARC message signature parse operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailArcMessageSignatureParseResult : IEmailArcMessageSignatureParseResultContract
{
    EmailDkimSignature? value;
    EmailSignatureParseState state;
    /// <summary>
    /// Creates a new ARC message signature parse result.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <param name = "consumedByteCount">The number of items or bytes to process.</param>
    /// <param name = "sourceByteCount">The number of items or bytes to process.</param>
    public EmailArcMessageSignatureParseResult(EmailDkimSignature? value, EmailIssueList issues, int consumedByteCount, int sourceByteCount)
    {
        this.value = value;
        state = new EmailSignatureParseState(issues, consumedByteCount, sourceByteCount);
    }

    /// <summary>
    /// Determines whether the value is present or available.
    /// </summary>
    /// <returns><c>true</c> when the value is available; otherwise, <c>false</c>.</returns>
    public bool HasValue()
    {
        return value != null;
    }

    /// <summary>
    /// Gets the value produced by the operation, or null when no value is available.
    /// </summary>
    /// <returns>The current value.</returns>
    public EmailDkimSignature? GetValue()
    {
        return value;
    }

    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    public EmailIssueList GetIssues()
    {
        return state.GetIssues();
    }

    /// <summary>
    /// Gets the consumed byte count.
    /// </summary>
    /// <returns>The current consumed byte count.</returns>
    public int GetConsumedByteCount()
    {
        return state.GetConsumedByteCount();
    }

    /// <summary>
    /// Gets the source byte count.
    /// </summary>
    /// <returns>The current source byte count.</returns>
    public int GetSourceByteCount()
    {
        return state.GetSourceByteCount();
    }

    /// <summary>
    /// Determines whether the current value or state is complete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is complete; otherwise, <c>false</c>.</returns>
    public bool IsComplete()
    {
        return state.IsComplete();
    }

    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    public bool IsSuccess()
    {
        return state.IsSuccess();
    }

    /// <summary>
    /// Determines whether the fatal error is present or available.
    /// </summary>
    /// <returns><c>true</c> when the fatal error is available; otherwise, <c>false</c>.</returns>
    public bool HasFatalError()
    {
        return state.HasFatalError();
    }
}
