using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailDkimVerificationKey"/>.
/// </summary>
public interface IEmailDkimVerificationKeyContract
{
    /// <summary>
    /// Gets the domain.
    /// </summary>
    /// <returns>The current domain.</returns>
    string GetDomain();
    /// <summary>
    /// Gets the selector.
    /// </summary>
    /// <returns>The current selector.</returns>
    string GetSelector();
    /// <summary>
    /// Gets the key.
    /// </summary>
    /// <returns>The current key.</returns>
    EmailDkimKey GetKey();
}
