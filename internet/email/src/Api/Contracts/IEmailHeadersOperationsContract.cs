using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailHeaders"/>.
/// </summary>
public interface IEmailHeadersOperationsContract
{
    /// <summary>Creates an empty header collection.</summary>
    /// <returns>The configured header collection.</returns>
    EmailHeaders CreateWith();
    /// <summary>
    /// Creates a new headers.
    /// </summary>
    /// <param name = "preservedHeaders">The preserved headers used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <returns>The configured header collection.</returns>
    EmailHeaders CreateWithPreservedHeaders(List<EmailHeader> preservedHeaders);
}
