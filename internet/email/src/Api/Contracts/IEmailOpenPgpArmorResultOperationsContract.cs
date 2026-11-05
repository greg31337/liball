using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailOpenPgpArmorResult"/>.
/// </summary>
public interface IEmailOpenPgpArmorResultOperationsContract
{
    /// <summary>
    /// Copies the value to a separate destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>A new array containing the produced bytes or values.</returns>
    byte[] Copy(byte[] value);
    /// <summary>
    /// Copies the issues to a separate destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>The issue list produced by the operation.</returns>
    EmailIssueList CopyIssues(EmailIssueList value);
}
