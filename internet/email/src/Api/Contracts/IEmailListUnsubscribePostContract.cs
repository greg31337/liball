using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailListUnsubscribePost"/>.
/// </summary>
public interface IEmailListUnsubscribePostContract
{
    /// <summary>
    /// Determines whether the current value or state is one click.
    /// </summary>
    /// <returns><c>true</c> when the value or state is one click; otherwise, <c>false</c>.</returns>
    bool IsOneClick();
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
