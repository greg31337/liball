using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailRfc2231EncodeResult"/>.
/// </summary>
public interface IEmailRfc2231EncodeResultContract
{
    /// <summary>
    /// Gets the bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current bytes.</returns>
    byte[] GetBytes();
    /// <summary>
    /// Gets the byte count.
    /// </summary>
    /// <returns>The current byte count.</returns>
    long GetByteCount();
    /// <summary>
    /// Gets the parameter count.
    /// </summary>
    /// <returns>The current parameter count.</returns>
    int GetParameterCount();
    /// <summary>
    /// Gets the parameter.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current parameter.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailRfc2231Parameter GetParameter(int index);
    /// <summary>
    /// Gets the parameters.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current parameters.</returns>
    List<EmailRfc2231Parameter> GetParameters();
    /// <summary>
    /// Determines whether the current value or state uses continuations.
    /// </summary>
    /// <returns><c>true</c> when the value or state uses continuations; otherwise, <c>false</c>.</returns>
    bool UsesContinuations();
    /// <summary>
    /// Gets the fold count.
    /// </summary>
    /// <returns>The current fold count.</returns>
    int GetFoldCount();
    /// <summary>
    /// Gets the final line column.
    /// </summary>
    /// <returns>The current final line column.</returns>
    int GetFinalLineColumn();
}
