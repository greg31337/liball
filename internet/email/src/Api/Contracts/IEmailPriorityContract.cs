using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailPriority"/>.
/// </summary>
public interface IEmailPriorityContract
{
    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    EmailPriorityKind GetKind();
    /// <summary>
    /// Gets the original value.
    /// </summary>
    /// <returns>The current original value.</returns>
    string GetOriginalValue();
    /// <summary>
    /// Determines whether the current value or state is x priority.
    /// </summary>
    /// <returns><c>true</c> when the value or state is x priority; otherwise, <c>false</c>.</returns>
    bool IsXPriority();
    /// <summary>
    /// Gets the numeric value.
    /// </summary>
    /// <returns>The current numeric value.</returns>
    int GetNumericValue();
    /// <summary>
    /// Gets the raw bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current raw bytes.</returns>
    byte[] GetRawBytes();
    /// <summary>
    /// Gets the source position.
    /// </summary>
    /// <returns>The current source position.</returns>
    EmailSourcePosition GetSourcePosition();
}
