using System;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailByteCounter"/>.
/// </summary>
public interface IEmailByteCounterOperationsContract
{
    /// <summary>Creates a byte counter initialized to zero.</summary>
    /// <returns>The configured byte counter.</returns>
    EmailByteCounter CreateWith();
    /// <summary>
    /// Creates a new byte counter.
    /// </summary>
    /// <param name = "initialByteCount">The number of items or bytes to process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    /// <returns>The configured byte counter.</returns>
    EmailByteCounter CreateWithInitialByteCount(long initialByteCount);
}
