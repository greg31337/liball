using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailRfc2231DecodeResult"/>.
/// </summary>
public interface IEmailRfc2231DecodeResultContract
{
    /// <summary>
    /// Gets the parameter count.
    /// </summary>
    /// <returns>The current parameter count.</returns>
    int GetParameterCount();
    /// <summary>
    /// Gets the parameter.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current parameter.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailRfc2231DecodedParameter GetParameter(int index);
    /// <summary>
    /// Gets the parameters.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current parameters.</returns>
    List<EmailRfc2231DecodedParameter> GetParameters();
    /// <summary>
    /// Gets the source parameter count.
    /// </summary>
    /// <returns>The current source parameter count.</returns>
    int GetSourceParameterCount();
    /// <summary>
    /// Gets the source parameter.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current source parameter.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailRfc2231Parameter GetSourceParameter(int index);
    /// <summary>
    /// Gets the decoded byte count.
    /// </summary>
    /// <returns>The current decoded byte count.</returns>
    long GetDecodedByteCount();
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
