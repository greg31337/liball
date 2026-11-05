using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailRfc2231Parameter"/>.
/// </summary>
public interface IEmailRfc2231ParameterContract
{
    /// <summary>
    /// Gets the name.
    /// </summary>
    /// <returns>The current name.</returns>
    string? GetName();
    /// <summary>
    /// Gets the value bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current value bytes.</returns>
    byte[] GetValueBytes();
    /// <summary>
    /// Determines whether the operation or source value was quoted.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool WasQuoted();
    /// <summary>
    /// Gets the source position.
    /// </summary>
    /// <returns>The current source position.</returns>
    EmailSourcePosition? GetSourcePosition();
}
