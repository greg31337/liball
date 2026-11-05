using System;

namespace email;
/// <summary>
/// Describes the outcome of a delivery status parse operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailDeliveryStatusParseResult : IEmailDeliveryStatusParseResultContract
{
    EmailDeliveryStatus? deliveryStatus;
    EmailIssueList issues;
    /// <summary>
    /// Creates a new delivery status parse result.
    /// </summary>
    /// <param name = "deliveryStatus">The delivery status used by the operation.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailDeliveryStatusParseResult(EmailDeliveryStatus? deliveryStatus, EmailIssueList issues)
    {
        if (issues == null)
            throw new ArgumentNullException("issues");
        this.deliveryStatus = deliveryStatus;
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
    }

    /// <summary>
    /// Determines whether the delivery status is present or available.
    /// </summary>
    /// <returns><c>true</c> when the delivery status is available; otherwise, <c>false</c>.</returns>
    public bool HasDeliveryStatus()
    {
        return deliveryStatus != null;
    }

    /// <summary>
    /// Gets the delivery status.
    /// </summary>
    /// <returns>The current delivery status.</returns>
    public EmailDeliveryStatus? GetDeliveryStatus()
    {
        return deliveryStatus;
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
        return deliveryStatus != null && !issues.HasSeverityAtLeast(EmailIssueSeverity.Error);
    }
}
