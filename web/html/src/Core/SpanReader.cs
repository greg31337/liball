using System;

namespace html;

internal class SpanReader
{
    internal char[] input;
    internal int position;

    internal SpanReader(ReadOnlySpan<char> input)
    {
        this.input = new char[input.Length];
        input.CopyTo(this.input);
        this.position = 0;
    }

    internal static SpanReader Create(ReadOnlySpan<char> input)
    {
        return new SpanReader(input);
    }

    internal int GetLength()
    {
        return this.input.Length;
    }

    internal int GetPosition()
    {
        return this.position;
    }

    internal int GetRemainingLength()
    {
        return this.input.Length - this.position;
    }

    internal bool IsEnd()
    {
        return this.position == this.input.Length;
    }

    internal int Peek()
    {
        return this.PeekAhead(0);
    }

    internal int PeekAhead(int lookahead)
    {
        if (lookahead < 0)
        {
            throw new ArgumentOutOfRangeException("lookahead", lookahead, "Lookahead cannot be negative.");
        }

        if (lookahead >= this.input.Length - this.position)
        {
            return -1;
        }

        return this.input[this.position + lookahead];
    }

    internal int Read()
    {
        if (this.position == this.input.Length)
        {
            return -1;
        }

        char value = this.input[this.position];
        this.position = this.position + 1;
        return value;
    }

    internal void Advance(int count)
    {
        if (count < 0 || count > this.input.Length - this.position)
        {
            throw new ArgumentOutOfRangeException("count", count, "The advance exceeds the remaining input.");
        }

        this.position = this.position + count;
    }

    internal void SetPosition(int position)
    {
        if (position < 0 || position > this.input.Length)
        {
            throw new ArgumentOutOfRangeException("position", position, "The reader position is outside the input.");
        }

        this.position = position;
    }

    internal void Reconsume()
    {
        if (this.position == 0)
        {
            throw new InvalidOperationException("No consumed character is available to reconsume.");
        }

        this.position = this.position - 1;
    }

    internal bool ConsumeIf(char value)
    {
        if (this.Peek() != value)
        {
            return false;
        }

        this.position = this.position + 1;
        return true;
    }

    internal bool ConsumeIfSequence(ReadOnlySpan<char> value, bool asciiIgnoreCase)
    {
        if (!this.StartsWith(value, asciiIgnoreCase))
        {
            return false;
        }

        this.position = this.position + value.Length;
        return true;
    }

    internal bool StartsWith(ReadOnlySpan<char> value, bool asciiIgnoreCase)
    {
        if (value.Length > this.input.Length - this.position)
        {
            return false;
        }

        int index = 0;
        while (index < value.Length)
        {
            char actual = this.input[this.position + index];
            char expected = value[index];
            if (asciiIgnoreCase)
            {
                actual = Ascii.ToLower(actual);
                expected = Ascii.ToLower(expected);
            }

            if (actual != expected)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal string GetString(int start, int length)
    {
        if (start < 0 || start > this.input.Length)
        {
            throw new ArgumentOutOfRangeException("start", start, "The string start is outside the input.");
        }

        if (length < 0 || length > this.input.Length - start)
        {
            throw new ArgumentOutOfRangeException("length", length, "The string length is outside the input.");
        }

        return new string(this.input, start, length);
    }

    internal bool TryConsumeRemainingTo(CharacterBuffer destination)
    {
        if (destination == null)
        {
            throw new ArgumentNullException("destination");
        }

        int remaining = this.input.Length - this.position;
        if (!destination.TryAppendSpan(new ReadOnlySpan<char>(this.input, this.position, remaining)))
        {
            return false;
        }

        this.position = this.input.Length;
        return true;
    }

    internal void Reset(ReadOnlySpan<char> input)
    {
        char[] replacement = new char[input.Length];
        input.CopyTo(replacement);
        this.input = replacement;
        this.position = 0;
    }
}
