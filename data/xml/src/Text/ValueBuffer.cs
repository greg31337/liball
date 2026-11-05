using System;

namespace xml;

internal class ValueBuffer
{
    internal char[] Characters;
    internal int Count;

    internal ValueBuffer(int initialCapacity)
    {
        Guard.NonNegativeInt(initialCapacity, "initialCapacity");
        Characters = new char[initialCapacity];
        Count = 0;
    }

    internal int CharacterCount()
    {
        return Count;
    }

    internal int CharacterCapacity()
    {
        return Characters.Length;
    }

    internal void AppendCharacter(char value)
    {
        EnsureCharacterCapacity(1);
        Characters[Count] = value;
        Count = Count + 1;
    }

    internal void AppendText(string value)
    {
        Guard.NotNull(value, "value");
        EnsureCharacterCapacity(value.Length);
        value.AsSpan().CopyTo(new Span<char>(Characters, Count, value.Length));
        Count = Count + value.Length;
    }

    internal void AppendCharacters(ReadOnlySpan<char> value)
    {
        EnsureCharacterCapacity(value.Length);
        value.CopyTo(new Span<char>(Characters, Count, value.Length));
        Count = Count + value.Length;
    }

    internal ReadOnlySpan<char> WrittenCharacters()
    {
        return new ReadOnlySpan<char>(Characters, 0, Count);
    }

    internal void CopyCharacters(Span<char> destination)
    {
        if (destination.Length < Count)
        {
            throw new ArgumentException("The destination is too small.", "destination");
        }

        WrittenCharacters().CopyTo(destination);
    }

    internal string GetText()
    {
        return new string(Characters, 0, Count);
    }

    internal void ClearCharacters()
    {
        Array.Clear(Characters, 0, Count);
        Count = 0;
    }

    internal void EnsureCharacterCapacity(int additional)
    {
        Guard.NonNegativeInt(additional, "additional");
        if (additional <= Characters.Length - Count)
        {
            return;
        }

        if (additional > int.MaxValue - Count)
        {
            throw new OutOfMemoryException("The character buffer is too large.");
        }

        int required = Count + additional;
        int capacity = Characters.Length;
        if (capacity < 4)
        {
            capacity = 4;
        }

        while (capacity < required)
        {
            if (capacity > int.MaxValue / 2)
            {
                capacity = required;
            }
            else
            {
                capacity = capacity * 2;
            }
        }

        char[] expanded = new char[capacity];
        Array.Copy(Characters, expanded, Count);
        Characters = expanded;
    }
}

internal class ByteBuffer
{
    internal byte[] Bytes;
    internal int Count;

    internal ByteBuffer(int initialCapacity)
    {
        Guard.NonNegativeInt(initialCapacity, "initialCapacity");
        Bytes = new byte[initialCapacity];
        Count = 0;
    }

    internal int ByteCount()
    {
        return Count;
    }

    internal int ByteCapacity()
    {
        return Bytes.Length;
    }

    internal void AppendByte(byte value)
    {
        EnsureByteCapacity(1);
        Bytes[Count] = value;
        Count = Count + 1;
    }

    internal void AppendBytes(ReadOnlySpan<byte> value)
    {
        EnsureByteCapacity(value.Length);
        value.CopyTo(new Span<byte>(Bytes, Count, value.Length));
        Count = Count + value.Length;
    }

    internal ReadOnlySpan<byte> WrittenBytes()
    {
        return new ReadOnlySpan<byte>(Bytes, 0, Count);
    }

    internal void CopyBytes(Span<byte> destination)
    {
        if (destination.Length < Count)
        {
            throw new ArgumentException("The destination is too small.", "destination");
        }

        WrittenBytes().CopyTo(destination);
    }

    internal byte[] GetByteArray()
    {
        byte[] result = new byte[Count];
        Array.Copy(Bytes, result, Count);
        return result;
    }

    internal void ClearBytes()
    {
        Array.Clear(Bytes, 0, Count);
        Count = 0;
    }

    internal void EnsureByteCapacity(int additional)
    {
        Guard.NonNegativeInt(additional, "additional");
        if (additional <= Bytes.Length - Count)
        {
            return;
        }

        if (additional > int.MaxValue - Count)
        {
            throw new OutOfMemoryException("The byte buffer is too large.");
        }

        int required = Count + additional;
        int capacity = Bytes.Length;
        if (capacity < 4)
        {
            capacity = 4;
        }

        while (capacity < required)
        {
            if (capacity > int.MaxValue / 2)
            {
                capacity = required;
            }
            else
            {
                capacity = capacity * 2;
            }
        }

        byte[] expanded = new byte[capacity];
        Array.Copy(Bytes, expanded, Count);
        Bytes = expanded;
    }
}
