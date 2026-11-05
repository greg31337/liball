using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailDkimHeaderCanonicalizationOptions"/>.
/// </summary>
public interface IEmailDkimHeaderCanonicalizationOptionsContract
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
    /// Gets the maximum selected header count.
    /// </summary>
    /// <returns>The current maximum selected header count.</returns>
    int GetMaximumSelectedHeaderCount();
    /// <summary>
    /// Sets the maximum selected header count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumSelectedHeaderCount(int value);
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
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailDkimHeaderCanonicalizationOptions Clone();
}
