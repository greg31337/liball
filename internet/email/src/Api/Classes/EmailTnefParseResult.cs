using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Describes the outcome of a TNEF parse operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailTnefParseResult : IEmailTnefParseResultContract
{
    EmailTnefContainer? container;
    EmailTnefMessage? message;
    EmailIssueList? issues;
    bool complete;
    /// <summary>
    /// Creates a TNEF parse result with a decoded message model.
    /// </summary>
    /// <param name = "container">The container used by the operation.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <param name = "complete">Whether to enable the complete behavior.</param>
    public EmailTnefParseResult(EmailTnefContainer? container, EmailIssueList issues, bool complete)
    {
        Initialize(container, null, issues, complete);
    }

    internal static EmailTnefParseResult CreateImplementationWithContainerAndIssuesAndComplete(EmailTnefContainer? container, EmailIssueList issues, bool complete)
    {
        return new EmailTnefParseResult(container, issues, complete);
    }

    internal static EmailTnefParseResult CreateImplementationWithContainerAndMessageAndIssuesAndComplete(EmailTnefContainer container, EmailTnefMessage message, EmailIssueList issues, bool complete)
    {
        EmailTnefParseResult value = new EmailTnefParseResult(container, issues, complete);
        value.Initialize(container, message, issues, complete);
        return value;
    }

    void Initialize(EmailTnefContainer? container, EmailTnefMessage? message, EmailIssueList issues, bool complete)
    {
        if (issues == null)
            throw new ArgumentNullException("issues");
        this.container = container;
        this.message = message;
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
        this.complete = complete;
    }

    /// <summary>
    /// Determines whether the container is present or available.
    /// </summary>
    /// <returns><c>true</c> when the container is available; otherwise, <c>false</c>.</returns>
    public bool HasContainer()
    {
        return container != null;
    }

    /// <summary>
    /// Gets the container.
    /// </summary>
    /// <returns>The current container.</returns>
    public EmailTnefContainer? GetContainer()
    {
        return container;
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
    public EmailTnefMessage? GetMessage()
    {
        return message;
    }

    /// <summary>
    /// Determines whether the current value or state is complete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is complete; otherwise, <c>false</c>.</returns>
    public bool IsComplete()
    {
        return complete;
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
        return container != null && complete && !(issues ?? throw new InvalidOperationException()).HasSeverityAtLeast(EmailIssueSeverity.Error);
    }
}
