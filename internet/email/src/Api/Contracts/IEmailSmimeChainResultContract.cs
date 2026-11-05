using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailSmimeChainResult"/>.
/// </summary>
public interface IEmailSmimeChainResultContract
{
    /// <summary>
    /// Determines whether the current value or state is valid.
    /// </summary>
    /// <returns><c>true</c> when the value or state is valid; otherwise, <c>false</c>.</returns>
    bool IsValid();
    /// <summary>
    /// Gets the status count.
    /// </summary>
    /// <returns>The current status count.</returns>
    int GetStatusCount();
    /// <summary>
    /// Gets the status at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested status.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailSmimeChainStatus GetStatusAt(int index);
    /// <summary>
    /// Gets the element count.
    /// </summary>
    /// <returns>The current element count.</returns>
    int GetElementCount();
    /// <summary>
    /// Gets the element at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested element.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailSmimeCertificateInfo GetElementAt(int index);
}
