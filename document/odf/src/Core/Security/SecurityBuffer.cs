using System;
using System.Security.Cryptography;
using System.Text;

namespace odf;

internal class SecurityBuffer : IDisposable
{
    byte[] secretBytes = [];
    bool isCleared;

    internal SecurityBuffer(byte[] source)
    {
        secretBytes = new byte[source.Length];
        Array.Copy(source, secretBytes, source.Length);
    }

    internal static SecurityBuffer CreateUtf8(string secret)
    {
        byte[] encoded = Encoding.UTF8.GetBytes(secret);
        SecurityBuffer result = new SecurityBuffer(encoded);
        ClearBytes(encoded);
        return result;
    }

    internal static void ClearBytes(byte[] bytes)
    {
        CryptographicOperations.ZeroMemory(bytes);
    }

    internal byte[] GetOperationBytes()
    {
        EnsureAvailable();
        return secretBytes;
    }

    internal byte[] CopyBytes()
    {
        EnsureAvailable();
        byte[] result = new byte[secretBytes.Length];
        Array.Copy(secretBytes, result, secretBytes.Length);
        return result;
    }

    internal int GetLength()
    {
        EnsureAvailable();
        return secretBytes.Length;
    }

    internal bool IsCleared()
    {
        return isCleared;
    }

    internal void Clear()
    {
        if (isCleared)
        {
            return;
        }
        ClearBytes(secretBytes);
        secretBytes = [];
        isCleared = true;
    }

    /// <summary>
    /// Clears the owned secret bytes and ends their operation-scoped lifetime.
    /// </summary>
    public void Dispose()
    {
        Clear();
    }

    internal void EnsureAvailable()
    {
        if (isCleared)
        {
            throw new ObjectDisposedException("SecurityBuffer");
        }
    }
}
