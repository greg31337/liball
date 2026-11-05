using System;

namespace email;
/// <summary>
/// Describes the outcome of a security multipart parse operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailSecurityMultipartParseResult : IEmailSecurityMultipartParseResultContract
{
    EmailSecurityMultipart? securityMultipart;
    EmailIssueList issues;
    /// <summary>
    /// Creates a new security multipart parse result.
    /// </summary>
    /// <param name = "securityMultipart">The security multipart used by the operation.</param>
    /// <param name = "issues">The ordered diagnostics associated with the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailSecurityMultipartParseResult(EmailSecurityMultipart? securityMultipart, EmailIssueList issues)
    {
        if (issues == null)
            throw new ArgumentNullException("issues");
        this.securityMultipart = securityMultipart;
        this.issues = new EmailIssueList();
        this.issues.AddRange(issues);
    }

    /// <summary>
    /// Determines whether the current value or state is recognized.
    /// </summary>
    /// <returns><c>true</c> when the value or state is recognized; otherwise, <c>false</c>.</returns>
    public bool IsRecognized()
    {
        return securityMultipart != null;
    }

    /// <summary>
    /// Determines whether the security multipart is present or available.
    /// </summary>
    /// <returns><c>true</c> when the security multipart is available; otherwise, <c>false</c>.</returns>
    public bool HasSecurityMultipart()
    {
        return securityMultipart != null;
    }

    /// <summary>
    /// Gets the security multipart.
    /// </summary>
    /// <returns>The current security multipart.</returns>
    public EmailSecurityMultipart? GetSecurityMultipart()
    {
        return securityMultipart;
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
        return securityMultipart != null && securityMultipart.AreInvariantsSatisfied();
    }
}
