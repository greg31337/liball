using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Provides the public factory and type-level operations associated with <see cref = "EmailHeaders"/>.
/// </summary>
public class EmailHeadersOperations : IEmailHeadersOperationsContract
{
    /// <summary>Creates an empty header collection.</summary>
    /// <returns>The configured header collection.</returns>
    public EmailHeaders CreateWith()
    {
        return EmailHeaders.CreateImplementationWith();
    }

    /// <summary>
    /// Creates a new headers.
    /// </summary>
    /// <param name = "preservedHeaders">The preserved headers used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <returns>The configured header collection.</returns>
    public EmailHeaders CreateWithPreservedHeaders(List<EmailHeader> preservedHeaders)
    {
        return EmailHeaders.CreateImplementationWithPreservedHeaders(preservedHeaders);
    }
}
