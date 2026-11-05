using System;

namespace email;
/// <summary>
/// Provides the public factory and type-level operations associated with <see cref = "EmailLineReadResult"/>.
/// </summary>
public class EmailLineReadResultOperations : IEmailLineReadResultOperationsContract
{
    /// <summary>
    /// Creates a need more data configured for the requested behavior.
    /// </summary>
    /// <param name = "consumedByteCount">The number of items or bytes to process.</param>
    /// <returns>The line read result produced by the operation.</returns>
    public EmailLineReadResult CreateNeedMoreData(int consumedByteCount)
    {
        return EmailLineReadResult.CreateNeedMoreDataImplementation(consumedByteCount);
    }

    /// <summary>
    /// Creates a line configured for the requested behavior.
    /// </summary>
    /// <param name = "line">The line used by the operation.</param>
    /// <param name = "consumedByteCount">The number of items or bytes to process.</param>
    /// <returns>The line read result produced by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailLineReadResult CreateLine(EmailLine line, int consumedByteCount)
    {
        return EmailLineReadResult.CreateLineImplementation(line, consumedByteCount);
    }

    /// <summary>
    /// Creates an end of input configured for the requested behavior.
    /// </summary>
    /// <returns>The line read result produced by the operation.</returns>
    public EmailLineReadResult CreateEndOfInput()
    {
        return EmailLineReadResult.CreateEndOfInputImplementation();
    }
}
