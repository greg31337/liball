using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailDkimBodyCanonicalizationResult"/>.
/// </summary>
public interface IEmailDkimBodyCanonicalizationResultContract
{
    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    EmailDkimCanonicalizationKind GetKind();
    /// <summary>
    /// Gets the hash.
    /// </summary>
    /// <returns>The current hash.</returns>
    EmailHashResult GetHash();
    /// <summary>
    /// Gets the source byte count.
    /// </summary>
    /// <returns>The current source byte count.</returns>
    long GetSourceByteCount();
    /// <summary>
    /// Gets the canonicalized byte count.
    /// </summary>
    /// <returns>The current canonicalized byte count.</returns>
    long GetCanonicalizedByteCount();
    /// <summary>
    /// Gets the hashed byte count.
    /// </summary>
    /// <returns>The current hashed byte count.</returns>
    long GetHashedByteCount();
    /// <summary>
    /// Determines whether the body length limit is present or available.
    /// </summary>
    /// <returns><c>true</c> when the body length limit is available; otherwise, <c>false</c>.</returns>
    bool HasBodyLengthLimit();
    /// <summary>
    /// Gets the body length limit.
    /// </summary>
    /// <returns>The current body length limit.</returns>
    long GetBodyLengthLimit();
    /// <summary>
    /// Determines whether the current value or state is body length limit satisfied.
    /// </summary>
    /// <returns><c>true</c> when the value or state is body length limit satisfied; otherwise, <c>false</c>.</returns>
    bool IsBodyLengthLimitSatisfied();
    /// <summary>
    /// Determines whether the operation or source value was body limited for hash.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool WasBodyLimitedForHash();
    /// <summary>
    /// Gets the unsigned canonicalized byte count.
    /// </summary>
    /// <returns>The current unsigned canonicalized byte count.</returns>
    long GetUnsignedCanonicalizedByteCount();
}
