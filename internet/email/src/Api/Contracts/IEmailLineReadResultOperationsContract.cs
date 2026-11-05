using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailLineReadResult"/>.
/// </summary>
public interface IEmailLineReadResultOperationsContract
{
    /// <summary>
    /// Creates a need more data configured for the requested behavior.
    /// </summary>
    /// <param name = "consumedByteCount">The number of items or bytes to process.</param>
    /// <returns>The line read result produced by the operation.</returns>
    EmailLineReadResult CreateNeedMoreData(int consumedByteCount);
    /// <summary>
    /// Creates a line configured for the requested behavior.
    /// </summary>
    /// <param name = "line">The line used by the operation.</param>
    /// <param name = "consumedByteCount">The number of items or bytes to process.</param>
    /// <returns>The line read result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    EmailLineReadResult CreateLine(EmailLine line, int consumedByteCount);
    /// <summary>
    /// Creates an end of input configured for the requested behavior.
    /// </summary>
    /// <returns>The line read result produced by the operation.</returns>
    EmailLineReadResult CreateEndOfInput();
}
