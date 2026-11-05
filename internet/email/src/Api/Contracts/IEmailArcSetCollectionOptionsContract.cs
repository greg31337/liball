using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailArcSetCollectionOptions"/>.
/// </summary>
public interface IEmailArcSetCollectionOptionsContract
{
    /// <summary>
    /// Gets the maximum set count.
    /// </summary>
    /// <returns>The current maximum set count.</returns>
    int GetMaximumSetCount();
    /// <summary>
    /// Sets the maximum set count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    void SetMaximumSetCount(int value);
    /// <summary>
    /// Gets the maximum ARC header count.
    /// </summary>
    /// <returns>The current maximum ARC header count.</returns>
    int GetMaximumArcHeaderCount();
    /// <summary>
    /// Sets the maximum ARC header count used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumArcHeaderCount(int value);
    /// <summary>
    /// Gets the maximum header bytes.
    /// </summary>
    /// <returns>The current maximum header bytes.</returns>
    int GetMaximumHeaderBytes();
    /// <summary>
    /// Sets the maximum header bytes used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumHeaderBytes(int value);
    /// <summary>
    /// Gets the maximum tags.
    /// </summary>
    /// <returns>The current maximum tags.</returns>
    int GetMaximumTags();
    /// <summary>
    /// Sets the maximum tags used by this instance.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    void SetMaximumTags(int value);
    /// <summary>
    /// Creates an independent copy of this instance and its option state.
    /// </summary>
    /// <returns>A separate copy with the same current settings and values.</returns>
    EmailArcSetCollectionOptions Clone();
}
