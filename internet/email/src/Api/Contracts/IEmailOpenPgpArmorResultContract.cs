using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailOpenPgpArmorResult"/>.
/// </summary>
public interface IEmailOpenPgpArmorResultContract
{
    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    bool IsSuccess();
    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    EmailOpenPgpArmorKind GetKind();
    /// <summary>
    /// Gets the data.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current data.</returns>
    byte[] GetData();
    /// <summary>
    /// Gets the headers.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current headers.</returns>
    List<EmailOpenPgpArmorHeader> GetHeaders();
    /// <summary>
    /// Determines whether the checksum is present or available.
    /// </summary>
    /// <returns><c>true</c> when the checksum is available; otherwise, <c>false</c>.</returns>
    bool HasChecksum();
    /// <summary>
    /// Determines whether the current value or state is checksum valid.
    /// </summary>
    /// <returns><c>true</c> when the value or state is checksum valid; otherwise, <c>false</c>.</returns>
    bool IsChecksumValid();
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
}
