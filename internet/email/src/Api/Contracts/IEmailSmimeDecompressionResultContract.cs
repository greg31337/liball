using System;
using System.IO.Compression;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailSmimeDecompressionResult"/>.
/// </summary>
public interface IEmailSmimeDecompressionResultContract
{
    /// <summary>
    /// Determines whether the current value or state is success.
    /// </summary>
    /// <returns><c>true</c> when the value or state is success; otherwise, <c>false</c>.</returns>
    bool IsSuccess();
    /// <summary>
    /// Determines whether the current value or state is unsupported algorithm.
    /// </summary>
    /// <returns><c>true</c> when the value or state is unsupported algorithm; otherwise, <c>false</c>.</returns>
    bool IsUnsupportedAlgorithm();
    /// <summary>
    /// Determines whether the current value or state is losslessly preserved.
    /// </summary>
    /// <returns><c>true</c> when the value or state is losslessly preserved; otherwise, <c>false</c>.</returns>
    bool IsLosslesslyPreserved();
    /// <summary>
    /// Gets the status.
    /// </summary>
    /// <returns>The current status.</returns>
    EmailSmimeDecompressionStatus GetStatus();
    /// <summary>
    /// Determines whether the content is present or available.
    /// </summary>
    /// <returns><c>true</c> when the content is available; otherwise, <c>false</c>.</returns>
    bool HasContent();
    /// <summary>
    /// Gets the content.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current content.</returns>
    byte[]? GetContent();
    /// <summary>
    /// Gets the compressed content.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current compressed content.</returns>
    byte[] GetCompressedContent();
    /// <summary>
    /// Gets the original CMS bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current original CMS bytes.</returns>
    byte[] GetOriginalCmsBytes();
    /// <summary>
    /// Gets the algorithm object identifier.
    /// </summary>
    /// <returns>The current algorithm object identifier.</returns>
    string GetAlgorithmObjectIdentifier();
    /// <summary>
    /// Gets the content type object identifier.
    /// </summary>
    /// <returns>The current content type object identifier.</returns>
    string GetContentTypeObjectIdentifier();
    /// <summary>
    /// Gets the issues.
    /// </summary>
    /// <returns>The current issues.</returns>
    EmailIssueList GetIssues();
}
