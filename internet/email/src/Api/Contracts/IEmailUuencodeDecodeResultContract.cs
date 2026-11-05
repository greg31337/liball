using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailUuencodeDecodeResult"/>.
/// </summary>
public interface IEmailUuencodeDecodeResultContract
{
    /// <summary>
    /// Gets the decoded bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current decoded bytes.</returns>
    byte[] GetDecodedBytes();
    /// <summary>
    /// Gets the decoded byte count.
    /// </summary>
    /// <returns>The current decoded byte count.</returns>
    long GetDecodedByteCount();
    /// <summary>
    /// Gets the detection.
    /// </summary>
    /// <returns>The current detection.</returns>
    EmailUuencodeDetectionResult GetDetection();
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
    /// <summary>
    /// Determines whether the fatal error is present or available.
    /// </summary>
    /// <returns><c>true</c> when the fatal error is available; otherwise, <c>false</c>.</returns>
    bool HasFatalError();
}
