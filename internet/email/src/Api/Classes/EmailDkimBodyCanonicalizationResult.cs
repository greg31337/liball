using System;

namespace email;
/// <summary>
/// Describes the outcome of a DKIM body canonicalization operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailDkimBodyCanonicalizationResult : IEmailDkimBodyCanonicalizationResultContract
{
    EmailDkimCanonicalizationKind kind;
    EmailHashResult hash;
    long sourceByteCount;
    long canonicalizedByteCount;
    bool hasBodyLengthLimit;
    long bodyLengthLimit;
    /// <summary>
    /// Creates a new DKIM body canonicalization result.
    /// </summary>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <param name = "hash">The hash used by the operation.</param>
    /// <param name = "sourceByteCount">The number of items or bytes to process.</param>
    /// <param name = "canonicalizedByteCount">The number of items or bytes to process.</param>
    /// <param name = "hasBodyLengthLimit">Whether to enable the has body length limit behavior.</param>
    /// <param name = "bodyLengthLimit">The body length limit used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailDkimBodyCanonicalizationResult(EmailDkimCanonicalizationKind kind, EmailHashResult hash, long sourceByteCount, long canonicalizedByteCount, bool hasBodyLengthLimit, long bodyLengthLimit)
    {
        if (kind != EmailDkimCanonicalizationKind.Simple && kind != EmailDkimCanonicalizationKind.Relaxed)
            throw new ArgumentOutOfRangeException("kind");
        if (hash == null)
            throw new ArgumentNullException("hash");
        if (sourceByteCount < 0 || canonicalizedByteCount < 0 || hash.GetHashedByteCount() > canonicalizedByteCount)
            throw new ArgumentOutOfRangeException("sourceByteCount");
        if (bodyLengthLimit < 0)
            throw new ArgumentOutOfRangeException("bodyLengthLimit");
        if (!hasBodyLengthLimit && bodyLengthLimit != 0)
            throw new ArgumentException("A body-length value requires an enabled body-length limit.", "bodyLengthLimit");
        long expectedHashedByteCount = canonicalizedByteCount;
        if (hasBodyLengthLimit && bodyLengthLimit < expectedHashedByteCount)
            expectedHashedByteCount = bodyLengthLimit;
        if (hash.GetHashedByteCount() != expectedHashedByteCount)
            throw new ArgumentException("The hash byte count does not match the canonicalized body prefix.", "hash");
        this.kind = kind;
        this.hash = hash;
        this.sourceByteCount = sourceByteCount;
        this.canonicalizedByteCount = canonicalizedByteCount;
        this.hasBodyLengthLimit = hasBodyLengthLimit;
        this.bodyLengthLimit = bodyLengthLimit;
    }

    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    public EmailDkimCanonicalizationKind GetKind()
    {
        return kind;
    }

    /// <summary>
    /// Gets the hash.
    /// </summary>
    /// <returns>The current hash.</returns>
    public EmailHashResult GetHash()
    {
        return hash;
    }

    /// <summary>
    /// Gets the source byte count.
    /// </summary>
    /// <returns>The current source byte count.</returns>
    public long GetSourceByteCount()
    {
        return sourceByteCount;
    }

    /// <summary>
    /// Gets the canonicalized byte count.
    /// </summary>
    /// <returns>The current canonicalized byte count.</returns>
    public long GetCanonicalizedByteCount()
    {
        return canonicalizedByteCount;
    }

    /// <summary>
    /// Gets the hashed byte count.
    /// </summary>
    /// <returns>The current hashed byte count.</returns>
    public long GetHashedByteCount()
    {
        return hash.GetHashedByteCount();
    }

    /// <summary>
    /// Determines whether the body length limit is present or available.
    /// </summary>
    /// <returns><c>true</c> when the body length limit is available; otherwise, <c>false</c>.</returns>
    public bool HasBodyLengthLimit()
    {
        return hasBodyLengthLimit;
    }

    /// <summary>
    /// Gets the body length limit.
    /// </summary>
    /// <returns>The current body length limit.</returns>
    public long GetBodyLengthLimit()
    {
        return bodyLengthLimit;
    }

    /// <summary>
    /// Determines whether the current value or state is body length limit satisfied.
    /// </summary>
    /// <returns><c>true</c> when the value or state is body length limit satisfied; otherwise, <c>false</c>.</returns>
    public bool IsBodyLengthLimitSatisfied()
    {
        return !hasBodyLengthLimit || bodyLengthLimit <= canonicalizedByteCount;
    }

    /// <summary>
    /// Determines whether the operation or source value was body limited for hash.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool WasBodyLimitedForHash()
    {
        return hasBodyLengthLimit && bodyLengthLimit < canonicalizedByteCount;
    }

    /// <summary>
    /// Gets the unsigned canonicalized byte count.
    /// </summary>
    /// <returns>The current unsigned canonicalized byte count.</returns>
    public long GetUnsignedCanonicalizedByteCount()
    {
        return canonicalizedByteCount - hash.GetHashedByteCount();
    }
}
