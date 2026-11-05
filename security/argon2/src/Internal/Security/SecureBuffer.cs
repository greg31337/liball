using System;
using System.Security.Cryptography;

namespace argon2;

internal class SecureBuffer : System.IDisposable
{
    byte[] bytes;
    bool disposed;

    internal SecureBuffer(int length)
    {
        int managedLength = Argon2ParameterValidator.GetManagedArrayLength(length);
        bytes = new byte[managedLength];
    }

    internal Span<byte> GetSpan()
    {
        if (disposed)
        {
            throw new System.InvalidOperationException("The secure buffer has been disposed.");
        }

        return bytes;
    }

    internal byte[] GetArray()
    {
        if (disposed)
        {
            throw new System.InvalidOperationException("The secure buffer has been disposed.");
        }

        return bytes;
    }

    internal int GetLength()
    {
        return bytes.Length;
    }

    internal bool IsDisposed()
    {
        return disposed;
    }

    internal bool IsCleared()
    {
        int index = 0;
        while (index < bytes.Length)
        {
            if (bytes[index] != 0)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    /// <summary>
    /// Clears the complete buffer and marks it as disposed.
    /// </summary>
    public void Dispose()
    {
        if (!disposed)
        {
            CryptographicOperations.ZeroMemory(bytes);
            disposed = true;
        }
    }
}
