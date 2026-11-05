using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailDkimSigningAttemptResult"/>.
/// </summary>
public interface IEmailDkimSigningAttemptResultContract
{
    /// <summary>
    /// Gets the status.
    /// </summary>
    /// <returns>The current status.</returns>
    EmailDkimSigningAttemptStatus GetStatus();
    /// <summary>
    /// Gets the algorithm kind.
    /// </summary>
    /// <returns>The current algorithm kind.</returns>
    EmailDkimAlgorithmKind GetAlgorithmKind();
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
    /// Determines whether the current value or state is prohibited algorithm.
    /// </summary>
    /// <returns><c>true</c> when the value or state is prohibited algorithm; otherwise, <c>false</c>.</returns>
    bool IsProhibitedAlgorithm();
    /// <summary>
    /// Determines whether the signing result is present or available.
    /// </summary>
    /// <returns><c>true</c> when the signing result is available; otherwise, <c>false</c>.</returns>
    bool HasSigningResult();
    /// <summary>
    /// Gets the signing result.
    /// </summary>
    /// <returns>The current signing result.</returns>
    EmailDkimSigningResult? GetSigningResult();
    /// <summary>
    /// Gets the reason.
    /// </summary>
    /// <returns>The current reason.</returns>
    string GetReason();
}
