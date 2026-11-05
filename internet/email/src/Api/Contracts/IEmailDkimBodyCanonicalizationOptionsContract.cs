using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailDkimBodyCanonicalizationOptions"/>.
/// </summary>
public interface IEmailDkimBodyCanonicalizationOptionsContract
{
    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    EmailDkimCanonicalizationKind GetKind();
    /// <summary>
    /// Sets the kind used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetKind(EmailDkimCanonicalizationKind value);
    /// <summary>
    /// Gets the hash algorithm.
    /// </summary>
    /// <returns>The current hash algorithm.</returns>
    EmailHashAlgorithm GetHashAlgorithm();
    /// <summary>
    /// Sets the hash algorithm used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetHashAlgorithm(EmailHashAlgorithm value);
    /// <summary>
    /// Gets the maximum input bytes.
    /// </summary>
    /// <returns>The current maximum input bytes.</returns>
    long GetMaximumInputBytes();
    /// <summary>
    /// Sets the maximum input bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumInputBytes(long value);
    /// <summary>
    /// Gets the maximum canonicalized bytes.
    /// </summary>
    /// <returns>The current maximum canonicalized bytes.</returns>
    long GetMaximumCanonicalizedBytes();
    /// <summary>
    /// Sets the maximum canonicalized bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumCanonicalizedBytes(long value);
    /// <summary>
    /// Gets the maximum hashed bytes.
    /// </summary>
    /// <returns>The current maximum hashed bytes.</returns>
    long GetMaximumHashedBytes();
    /// <summary>
    /// Sets the maximum hashed bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumHashedBytes(long value);
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
    /// Sets the body length limit used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetBodyLengthLimit(long value);
    /// <summary>
    /// Clears the stored body length limit value or values.
    /// </summary>
    void ClearBodyLengthLimit();
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailDkimBodyCanonicalizationOptions Clone();
}
