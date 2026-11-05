using System;
using System.Buffers;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailBuffer"/>.
/// </summary>
public interface IEmailBufferOperationsContract
{
    /// <summary>Creates a buffer backed by the shared array pool.</summary>
    /// <param name = "initialCapacity">The initial capacity.</param>
    /// <param name = "maximumCapacity">The maximum capacity.</param>
    /// <param name = "clearSensitiveBytes">Whether returned storage is cleared.</param>
    /// <returns>The configured buffer.</returns>
    EmailBuffer CreateWithInitialCapacityAndMaximumCapacityAndClearSensitiveBytes(int initialCapacity, int maximumCapacity, bool clearSensitiveBytes);
    /// <summary>
    /// Creates a new buffer.
    /// </summary>
    /// <param name = "pool">The pool used by the operation.</param>
    /// <param name = "initialCapacity">The initial capacity used by the operation.</param>
    /// <param name = "maximumCapacity">The maximum capacity used by the operation.</param>
    /// <param name = "clearSensitiveBytes">Whether to enable the clear sensitive bytes behavior.</param>
    /// <returns>The configured buffer.</returns>
    EmailBuffer CreateWithPoolAndInitialCapacityAndMaximumCapacityAndClearSensitiveBytes(ArrayPool<byte> pool, int initialCapacity, int maximumCapacity, bool clearSensitiveBytes);
}
