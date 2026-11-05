using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Provides the public factory and type-level operations associated with <see cref = "EmailMediaType"/>.
/// </summary>
public class EmailMediaTypeOperations : IEmailMediaTypeOperationsContract
{
    /// <summary>
    /// Creates a default configured for the requested behavior.
    /// </summary>
    /// <returns>The media type produced by the operation.</returns>
    public EmailMediaType CreateDefaultWith()
    {
        return EmailMediaType.CreateDefaultImplementationWith();
    }

    /// <summary>
    /// Creates a default configured for the requested behavior.
    /// </summary>
    /// <param name = "rawBytes">The source bytes or data to process.</param>
    /// <param name = "position">The position used by the operation.</param>
    /// <returns>The media type produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailMediaType CreateDefaultWithRawBytesAndPosition(byte[] rawBytes, EmailSourcePosition position)
    {
        return EmailMediaType.CreateDefaultImplementationWithRawBytesAndPosition(rawBytes, position);
    }
}
