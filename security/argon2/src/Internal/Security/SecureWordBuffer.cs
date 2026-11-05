using System;

namespace argon2;

internal class SecureWordBuffer : System.IDisposable
{
    ulong[] words;
    bool disposed;

    internal SecureWordBuffer(ulong[] words)
    {
        if (words == null)
        {
            throw new Argon2ParameterException("A secure word buffer is required.");
        }

        this.words = words;
    }

    internal Span<ulong> GetSpan()
    {
        if (disposed)
        {
            throw new System.InvalidOperationException("The secure word buffer has been disposed.");
        }

        return words;
    }

    internal bool IsDisposed()
    {
        return disposed;
    }

    internal bool IsCleared()
    {
        int index = 0;
        while (index < words.Length)
        {
            if (words[index] != 0UL)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    /// <summary>
    /// Clears the complete word buffer and marks the guard as disposed.
    /// </summary>
    public void Dispose()
    {
        if (!disposed)
        {
            Array.Clear(words);
            disposed = true;
        }
    }
}
