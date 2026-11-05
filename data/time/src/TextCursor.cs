using System;

namespace time;

internal class TextCursor
{
    string text;
    int position;

    internal TextCursor(string text)
    {
        TimeValidation.RequireNotNull(text, "text");

        this.text = text;
        position = 0;
    }

    internal static TextCursor Create(string text)
    {
        return new TextCursor(text);
    }

    internal string GetText()
    {
        return text;
    }

    internal int GetPosition()
    {
        return position;
    }

    internal int GetLength()
    {
        return text.Length;
    }

    internal bool IsEnd()
    {
        return position >= text.Length;
    }

    internal bool CanRead(int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException("count", "Read count must not be negative.");
        }

        return count <= text.Length - position;
    }

    internal char Peek()
    {
        if (IsEnd())
        {
            throw new FormatException("Cannot peek past the end of text.");
        }

        return text[position];
    }

    internal char Read()
    {
        char value = Peek();
        position = position + 1;
        return value;
    }

    internal bool Match(char expected)
    {
        if (!IsEnd() && text[position] == expected)
        {
            position = position + 1;
            return true;
        }

        return false;
    }

    internal void Expect(char expected)
    {
        if (!Match(expected))
        {
            throw new FormatException("Expected character was not found.");
        }
    }

    internal void Advance(int count)
    {
        if (!CanRead(count))
        {
            throw new FormatException("Cannot advance past the end of text.");
        }

        position = position + count;
    }

    internal int ReadFixedDigits(int length)
    {
        if (length <= 0)
        {
            throw new ArgumentOutOfRangeException("length", "Digit length must be positive.");
        }

        if (!CanRead(length))
        {
            throw new FormatException("Not enough characters are available for the requested digits.");
        }

        int value = 0;
        int index = 0;
        while (index < length)
        {
            char character = text[position + index];
            if (!IsDigit(character))
            {
                throw new FormatException("Expected digit was not found.");
            }

            value = value * 10 + (character - '0');
            index = index + 1;
        }

        position = position + length;
        return value;
    }

    internal int ReadDigits()
    {
        if (IsEnd() || !IsDigit(text[position]))
        {
            throw new FormatException("Expected at least one digit.");
        }

        int value = 0;
        while (!IsEnd() && IsDigit(text[position]))
        {
            value = value * 10 + (text[position] - '0');
            position = position + 1;
        }

        return value;
    }

    internal string ReadUntil(char delimiter)
    {
        int start = position;
        while (!IsEnd() && text[position] != delimiter)
        {
            position = position + 1;
        }

        return text.Substring(start, position - start);
    }

    internal string ReadRemaining()
    {
        string result = text.Substring(position);
        position = text.Length;
        return result;
    }

    internal void RequireEnd()
    {
        if (!IsEnd())
        {
            throw new FormatException("Unexpected trailing text.");
        }
    }

    internal static bool IsDigit(char character)
    {
        return character >= '0' && character <= '9';
    }
}
