using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailArcSealCanonicalizationOptions"/>.
/// </summary>
public interface IEmailArcSealCanonicalizationOptionsContract
{
    /// <summary>
    /// Gets the maximum header count.
    /// </summary>
    /// <returns>The current maximum header count.</returns>
    int GetMaximumHeaderCount();
    /// <summary>
    /// Sets the maximum header count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumHeaderCount(int value);
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
    EmailArcSealCanonicalizationOptions Clone();
}
