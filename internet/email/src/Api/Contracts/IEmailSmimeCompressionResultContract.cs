using System;
using System.IO.Compression;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailSmimeCompressionResult"/>.
/// </summary>
public interface IEmailSmimeCompressionResultContract
{
    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    bool IsSuccess();
    /// <summary>
    /// Gets the CMS bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current CMS bytes.</returns>
    byte[] GetCmsBytes();
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
    /// <summary>
    /// Gets the uncompressed bytes.
    /// </summary>
    /// <returns>The current uncompressed bytes.</returns>
    long GetUncompressedBytes();
    /// <summary>
    /// Gets the compressed bytes.
    /// </summary>
    /// <returns>The current compressed bytes.</returns>
    long GetCompressedBytes();
}
