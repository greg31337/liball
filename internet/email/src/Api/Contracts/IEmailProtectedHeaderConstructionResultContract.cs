using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailProtectedHeaderConstructionResult"/>.
/// </summary>
public interface IEmailProtectedHeaderConstructionResultContract
{
    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    bool IsSuccess();
    /// <summary>
    /// Gets the payload.
    /// </summary>
    /// <returns>The current payload.</returns>
    MimeEntity GetPayload();
    /// <summary>
    /// Gets the outer headers.
    /// </summary>
    /// <returns>The current outer headers.</returns>
    EmailHeaders GetOuterHeaders();
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
}
