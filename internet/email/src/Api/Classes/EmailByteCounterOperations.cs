using System;

namespace email;
/// <summary>
/// Provides the public factory and type-level operations associated with <see cref = "EmailByteCounter"/>.
/// </summary>
public class EmailByteCounterOperations : IEmailByteCounterOperationsContract
{
    /// <summary>Creates a byte counter initialized to zero.</summary>
    /// <returns>The configured byte counter.</returns>
    public EmailByteCounter CreateWith()
    {
        return EmailByteCounter.CreateImplementationWith();
    }

    /// <summary>
    /// Creates a new byte counter.
    /// </summary>
    /// <param name = "initialByteCount">The number of items or bytes to process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    /// <returns>The configured byte counter.</returns>
    public EmailByteCounter CreateWithInitialByteCount(long initialByteCount)
    {
        return EmailByteCounter.CreateImplementationWithInitialByteCount(initialByteCount);
    }
}
