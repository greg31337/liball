using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailTnefHeader"/>.
/// </summary>
public interface IEmailTnefHeaderContract
{
    /// <summary>
    /// Gets the signature.
    /// </summary>
    /// <returns>The current signature.</returns>
    uint GetSignature();
    /// <summary>
    /// Determines whether the current value or state is signature valid.
    /// </summary>
    /// <returns><c>true</c> when the value or state is signature valid; otherwise, <c>false</c>.</returns>
    bool IsSignatureValid();
    /// <summary>
    /// Gets the legacy key.
    /// </summary>
    /// <returns>The current legacy key.</returns>
    int GetLegacyKey();
    /// <summary>
    /// Gets the source range.
    /// </summary>
    /// <returns>The current source range.</returns>
    EmailRange GetSourceRange();
}
