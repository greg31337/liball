using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailDeliveryStatusParseResult"/>.
/// </summary>
public interface IEmailDeliveryStatusParseResultContract
{
    /// <summary>
    /// Determines whether the delivery status is present or available.
    /// </summary>
    /// <returns><c>true</c> when the delivery status is available; otherwise, <c>false</c>.</returns>
    bool HasDeliveryStatus();
    /// <summary>
    /// Gets the delivery status.
    /// </summary>
    /// <returns>The current delivery status.</returns>
    EmailDeliveryStatus? GetDeliveryStatus();
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    bool IsSuccess();
}
