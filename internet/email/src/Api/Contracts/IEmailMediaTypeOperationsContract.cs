using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailMediaType"/>.
/// </summary>
public interface IEmailMediaTypeOperationsContract
{
    /// <summary>
    /// Creates a default configured for the requested behavior.
    /// </summary>
    /// <returns>The media type produced by the operation.</returns>
    EmailMediaType CreateDefaultWith();
    /// <summary>
    /// Creates a default configured for the requested behavior.
    /// </summary>
    /// <param name = "rawBytes">The source bytes or data to process.</param>
    /// <param name = "position">The position used by the operation.</param>
    /// <returns>The media type produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailMediaType CreateDefaultWithRawBytesAndPosition(byte[] rawBytes, EmailSourcePosition position);
}
