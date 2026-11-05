using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailCanonicalComparisonResult"/>.
/// </summary>
public interface IEmailCanonicalComparisonResultContract
{
    /// <summary>
    /// Determines whether the compared values are equal.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool AreEqual();
    /// <summary>
    /// Gets the left fingerprint.
    /// </summary>
    /// <returns>The current left fingerprint.</returns>
    EmailHashResult GetLeftFingerprint();
    /// <summary>
    /// Gets the right fingerprint.
    /// </summary>
    /// <returns>The current right fingerprint.</returns>
    EmailHashResult GetRightFingerprint();
}
