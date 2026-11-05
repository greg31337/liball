using System;

namespace email;
/// <summary>
/// Describes the outcome of a canonical comparison operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailCanonicalComparisonResult : IEmailCanonicalComparisonResultContract
{
    bool equal;
    EmailHashResult leftFingerprint;
    EmailHashResult rightFingerprint;
    /// <summary>
    /// Creates a new canonical comparison result.
    /// </summary>
    /// <param name = "equal">Whether to enable the equal behavior.</param>
    /// <param name = "leftFingerprint">The left fingerprint used by the operation.</param>
    /// <param name = "rightFingerprint">The right fingerprint used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailCanonicalComparisonResult(bool equal, EmailHashResult leftFingerprint, EmailHashResult rightFingerprint)
    {
        if (leftFingerprint == null)
        {
            throw new ArgumentNullException("leftFingerprint");
        }

        if (rightFingerprint == null)
        {
            throw new ArgumentNullException("rightFingerprint");
        }

        if (leftFingerprint.GetAlgorithm() != rightFingerprint.GetAlgorithm())
        {
            throw new ArgumentException("Canonical fingerprints must use the same algorithm.", "rightFingerprint");
        }

        this.equal = equal;
        this.leftFingerprint = leftFingerprint;
        this.rightFingerprint = rightFingerprint;
    }

    /// <summary>
    /// Determines whether the compared values are equal.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool AreEqual()
    {
        return equal;
    }

    /// <summary>
    /// Gets the left fingerprint.
    /// </summary>
    /// <returns>The current left fingerprint.</returns>
    public EmailHashResult GetLeftFingerprint()
    {
        return leftFingerprint;
    }

    /// <summary>
    /// Gets the right fingerprint.
    /// </summary>
    /// <returns>The current right fingerprint.</returns>
    public EmailHashResult GetRightFingerprint()
    {
        return rightFingerprint;
    }
}
