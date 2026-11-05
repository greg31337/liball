using System;

namespace uri;

internal class TextOutput
{
    internal char[] Characters;
    internal int Length;
    internal int MaximumCharacters;
    internal bool Failed;
    internal int ErrorOffset;
    internal int ErrorLength;

    internal TextOutput(int maximumCharacters)
    {
        MaximumCharacters = maximumCharacters;
        Length = 0;
        Failed = maximumCharacters < 0;
        ErrorOffset = 0;
        ErrorLength = 0;

        int capacity = 64;
        if (maximumCharacters < capacity)
        {
            capacity = maximumCharacters;
        }

        if (capacity < 0)
        {
            capacity = 0;
        }

        Characters = new char[capacity];
    }

    internal TextOutputResult AppendCharacter(char value)
    {
        TextOutputResult available = CheckAvailable(1);
        if (!available.Success)
        {
            return available;
        }

        EnsureCapacity(1);
        int writtenOffset = Length;
        Characters[Length] = value;
        Length = Length + 1;
        return new TextOutputResult(true, writtenOffset, 1, 0, 0);
    }

    internal TextOutputResult AppendText(string text)
    {
        return AppendSpan(text.AsSpan());
    }

    internal TextOutputResult AppendSpan(ReadOnlySpan<char> text)
    {
        TextOutputResult available = CheckAvailable(text.Length);
        if (!available.Success)
        {
            return available;
        }

        EnsureCapacity(text.Length);
        int writtenOffset = Length;
        int index = 0;
        while (index < text.Length)
        {
            Characters[Length + index] = text[index];
            index = index + 1;
        }

        Length = Length + text.Length;
        return new TextOutputResult(true, writtenOffset, text.Length, 0, 0);
    }

    internal TextOutputResult AppendRepeated(char value, int count)
    {
        if (count < 0)
        {
            return SetFailure(Length, 0);
        }

        TextOutputResult available = CheckAvailable(count);
        if (!available.Success)
        {
            return available;
        }

        EnsureCapacity(count);
        int writtenOffset = Length;
        int index = 0;
        while (index < count)
        {
            Characters[Length + index] = value;
            index = index + 1;
        }

        Length = Length + count;
        return new TextOutputResult(true, writtenOffset, count, 0, 0);
    }

    internal int GetLength()
    {
        return Length;
    }

    internal string GetText()
    {
        return new string(Characters, 0, Length);
    }

    internal TextOutputResult GetStatus()
    {
        if (Failed)
        {
            return new TextOutputResult(false, Length, 0, ErrorOffset, ErrorLength);
        }

        return new TextOutputResult(true, Length, 0, 0, 0);
    }

    internal TextOutputResult CheckAvailable(int requestedCharacters)
    {
        if (Failed)
        {
            return new TextOutputResult(false, Length, 0, ErrorOffset, ErrorLength);
        }

        if (requestedCharacters < 0)
        {
            return SetFailure(Length, 0);
        }

        int availableCharacters = MaximumCharacters - Length;
        if (requestedCharacters > availableCharacters)
        {
            return SetFailure(MaximumCharacters, requestedCharacters - availableCharacters);
        }

        return new TextOutputResult(true, Length, requestedCharacters, 0, 0);
    }

    internal TextOutputResult SetFailure(int errorOffset, int errorLength)
    {
        Failed = true;
        ErrorOffset = errorOffset;
        ErrorLength = errorLength;
        return new TextOutputResult(false, Length, 0, ErrorOffset, ErrorLength);
    }

    internal void EnsureCapacity(int additionalCharacters)
    {
        int requiredCapacity = Length + additionalCharacters;
        if (requiredCapacity <= Characters.Length)
        {
            return;
        }

        int capacity = Characters.Length;
        if (capacity == 0)
        {
            capacity = 1;
        }

        while (capacity < requiredCapacity)
        {
            if (capacity > MaximumCharacters / 2)
            {
                capacity = MaximumCharacters;
            }
            else
            {
                capacity = capacity * 2;
            }
        }

        char[] expanded = new char[capacity];
        Array.Copy(Characters, expanded, Length);
        Characters = expanded;
    }
}
