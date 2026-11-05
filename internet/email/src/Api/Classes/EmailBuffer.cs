using System;
using System.Buffers;

namespace email;
/// <summary>
/// Represents a buffer.
/// </summary>
public class EmailBuffer : IDisposable, IEmailBufferContract
{
    ArrayPool<byte>? pool;
    byte[]? buffer;
    int length;
    int maximumCapacity;
    bool clearSensitiveBytes;
    bool closed;
    /// <summary>
    /// Creates a buffer backed by the shared array pool.
    /// </summary>
    /// <param name = "initialCapacity">The initial capacity used by the operation.</param>
    /// <param name = "maximumCapacity">The maximum capacity used by the operation.</param>
    /// <param name = "clearSensitiveBytes">Whether to enable the clear sensitive bytes behavior.</param>
    public EmailBuffer(int initialCapacity, int maximumCapacity, bool clearSensitiveBytes)
    {
        Initialize(ArrayPool<byte>.Shared, initialCapacity, maximumCapacity, clearSensitiveBytes);
    }

    internal static EmailBuffer CreateImplementationWithInitialCapacityAndMaximumCapacityAndClearSensitiveBytes(int initialCapacity, int maximumCapacity, bool clearSensitiveBytes)
    {
        return new EmailBuffer(initialCapacity, maximumCapacity, clearSensitiveBytes);
    }

    internal static EmailBuffer CreateImplementationWithPoolAndInitialCapacityAndMaximumCapacityAndClearSensitiveBytes(ArrayPool<byte> pool, int initialCapacity, int maximumCapacity, bool clearSensitiveBytes)
    {
        EmailBuffer value = new EmailBuffer(0, 1, false);
        value.Initialize(pool, initialCapacity, maximumCapacity, clearSensitiveBytes);
        return value;
    }

    void Initialize(ArrayPool<byte> pool, int initialCapacity, int maximumCapacity, bool clearSensitiveBytes)
    {
        if (pool == null)
        {
            throw new ArgumentNullException("pool");
        }

        if (initialCapacity < 0)
        {
            throw new ArgumentOutOfRangeException("initialCapacity");
        }

        if (maximumCapacity < 1)
        {
            throw new ArgumentOutOfRangeException("maximumCapacity");
        }

        if (initialCapacity > maximumCapacity)
        {
            throw new ArgumentOutOfRangeException("initialCapacity");
        }

        this.pool = pool;
        this.maximumCapacity = maximumCapacity;
        this.clearSensitiveBytes = clearSensitiveBytes;
        buffer = null;
        length = 0;
        closed = false;
        if (initialCapacity > 0)
        {
            buffer = RentBuffer(initialCapacity);
        }
    }

    /// <summary>
    /// Gets the length.
    /// </summary>
    /// <returns>The current length.</returns>
    public int GetLength()
    {
        EnsureOpen();
        return length;
    }

    /// <summary>
    /// Gets the capacity.
    /// </summary>
    /// <returns>The current capacity.</returns>
    public int GetCapacity()
    {
        EnsureOpen();
        if (buffer == null)
        {
            return 0;
        }

        return buffer.Length < maximumCapacity ? buffer.Length : maximumCapacity;
    }

    /// <summary>
    /// Gets the maximum capacity.
    /// </summary>
    /// <returns>The current maximum capacity.</returns>
    public int GetMaximumCapacity()
    {
        return maximumCapacity;
    }

    /// <summary>
    /// Gets the clears sensitive bytes.
    /// </summary>
    /// <returns><c>true</c> when the clears sensitive bytes setting is enabled; otherwise, <c>false</c>.</returns>
    public bool GetClearsSensitiveBytes()
    {
        return clearSensitiveBytes;
    }

    /// <summary>
    /// Determines whether the current value or state is closed.
    /// </summary>
    /// <returns><c>true</c> when the value or state is closed; otherwise, <c>false</c>.</returns>
    public bool IsClosed()
    {
        return closed;
    }

    /// <summary>
    /// Ensures the capacity.
    /// </summary>
    /// <param name = "requiredCapacity">The required capacity used by the operation.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    public void EnsureCapacity(int requiredCapacity)
    {
        EnsureOpen();
        if (requiredCapacity < 0)
        {
            throw new ArgumentOutOfRangeException("requiredCapacity");
        }

        if (requiredCapacity > maximumCapacity)
        {
            throw new EmailLimitException("MaxBufferBytes", maximumCapacity, requiredCapacity);
        }

        int currentCapacity = GetCapacity();
        if (requiredCapacity <= currentCapacity)
        {
            return;
        }

        long doubledCapacity = currentCapacity == 0 ? 1 : (long)currentCapacity * 2;
        int newCapacity = doubledCapacity > maximumCapacity ? maximumCapacity : (int)doubledCapacity;
        if (newCapacity < requiredCapacity)
        {
            newCapacity = requiredCapacity;
        }

        byte[] replacement = RentBuffer(newCapacity);
        if (length > 0)
        {
            Array.Copy((buffer ?? throw new InvalidOperationException()), 0, replacement, 0, length);
        }

        byte[]? previous = buffer;
        buffer = replacement;
        if (previous != null)
        {
            ReturnBuffer(previous);
        }
    }

    /// <summary>
    /// Appends the supplied value.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    public void Append(ReadOnlySpan<byte> value)
    {
        EnsureOpen();
        if (value.Length > maximumCapacity - length)
        {
            throw new EmailLimitException("MaxBufferBytes", maximumCapacity, (long)length + value.Length);
        }

        int newLength = length + value.Length;
        EnsureCapacity(newLength);
        if (value.Length > 0)
        {
            value.CopyTo(new Span<byte>(buffer, length, value.Length));
        }

        length = newLength;
    }

    /// <summary>
    /// Appends the byte.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    public void AppendByte(byte value)
    {
        EnsureOpen();
        if (length == maximumCapacity)
        {
            throw new EmailLimitException("MaxBufferBytes", maximumCapacity, (long)length + 1);
        }

        EnsureCapacity(length + 1);
        (buffer ?? throw new InvalidOperationException())[length] = value;
        length++;
    }

    /// <summary>
    /// Gets the byte.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current byte.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public byte GetByte(int index)
    {
        EnsureOpen();
        if (index < 0 || index >= length)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return (buffer ?? throw new InvalidOperationException())[index];
    }

    /// <summary>
    /// Copies the current output or content into a new byte array.
    /// </summary>
    /// <returns>A new array containing the produced bytes or values.</returns>
    public byte[] ToArray()
    {
        EnsureOpen();
        byte[] copy = new byte[length];
        if (length > 0)
        {
            Array.Copy((buffer ?? throw new InvalidOperationException()), 0, copy, 0, length);
        }

        return copy;
    }

    /// <summary>
    /// Copies the to to a separate destination.
    /// </summary>
    /// <param name = "destination">The destination that receives the generated data.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    public void CopyTo(Span<byte> destination)
    {
        EnsureOpen();
        if (destination.Length < length)
        {
            throw new ArgumentException("The destination is smaller than the buffered content.", "destination");
        }

        if (length > 0)
        {
            new ReadOnlySpan<byte>(buffer, 0, length).CopyTo(destination);
        }
    }

    /// <summary>
    /// Discards the prefix.
    /// </summary>
    /// <param name = "byteCount">The number of items or bytes to process.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public void DiscardPrefix(int byteCount)
    {
        EnsureOpen();
        if (byteCount < 0 || byteCount > length)
        {
            throw new ArgumentOutOfRangeException("byteCount");
        }

        if (byteCount == 0)
        {
            return;
        }

        int remaining = length - byteCount;
        if (remaining > 0)
        {
            Array.Copy((buffer ?? throw new InvalidOperationException()), byteCount, buffer, 0, remaining);
        }

        if (clearSensitiveBytes)
        {
            Array.Clear((buffer ?? throw new InvalidOperationException()), remaining, byteCount);
        }

        length = remaining;
    }

    /// <summary>
    /// Clears all stored items or state.
    /// </summary>
    public void Clear()
    {
        EnsureOpen();
        if (clearSensitiveBytes && length > 0)
        {
            Array.Clear((buffer ?? throw new InvalidOperationException()), 0, length);
        }

        length = 0;
    }

    /// <summary>
    /// Closes the supplied value.
    /// </summary>
    public void Close()
    {
        if (closed)
        {
            return;
        }

        byte[]? released = buffer;
        buffer = null;
        length = 0;
        closed = true;
        if (released != null)
        {
            ReturnBuffer(released);
        }
    }

    /// <summary>
    /// Releases streams, temporary storage, and other resources retained by this instance.
    /// </summary>
    public void Dispose()
    {
        Close();
    }

    byte[] RentBuffer(int minimumCapacity)
    {
        byte[] rented = (pool ?? throw new InvalidOperationException()).Rent(minimumCapacity);
        if (rented == null)
        {
            throw new InvalidOperationException("The byte pool returned null.");
        }

        if (rented.Length < minimumCapacity)
        {
            pool.Return(rented, false);
            throw new InvalidOperationException("The byte pool returned a buffer smaller than requested.");
        }

        return rented;
    }

    void ReturnBuffer(byte[] returned)
    {
        if (clearSensitiveBytes)
        {
            Array.Clear(returned, 0, returned.Length);
        }

        (pool ?? throw new InvalidOperationException()).Return(returned, false);
    }

    void EnsureOpen()
    {
        if (closed)
        {
            throw new ObjectDisposedException("EmailBuffer");
        }
    }
}
