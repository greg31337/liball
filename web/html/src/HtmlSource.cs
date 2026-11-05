using System;

namespace html;

internal class HtmlLineMap
{
    internal int length;
    internal int[] lineStarts;
    internal int lineCount;
    internal int[] characterOffsets;
    internal bool trackByteOffsets;
    internal long[] byteOffsets;

    internal HtmlLineMap(int initialCharacterOffset, bool trackByteOffsets, long initialByteOffset)
    {
        if (initialCharacterOffset < 0)
        {
            throw new ArgumentOutOfRangeException("initialCharacterOffset", initialCharacterOffset, "An initial character offset cannot be negative.");
        }

        if (trackByteOffsets && initialByteOffset < 0)
        {
            throw new ArgumentOutOfRangeException("initialByteOffset", initialByteOffset, "An initial byte offset cannot be negative.");
        }

        this.length = 0;
        this.lineStarts = new int[8];
        this.lineStarts[0] = 0;
        this.lineCount = 1;
        this.characterOffsets = new int[16];
        this.characterOffsets[0] = initialCharacterOffset;
        this.trackByteOffsets = trackByteOffsets;
        this.byteOffsets = trackByteOffsets ? new long[16] : new long[0];
        if (trackByteOffsets)
        {
            this.byteOffsets[0] = initialByteOffset;
        }
    }

    internal static HtmlLineMap Create()
    {
        return new HtmlLineMap(0, false, -1);
    }

    internal static HtmlLineMap Create(int initialCharacterOffset)
    {
        return new HtmlLineMap(initialCharacterOffset, false, -1);
    }

    internal static HtmlLineMap CreateWithByteOffsets(long initialByteOffset)
    {
        return new HtmlLineMap(0, true, initialByteOffset);
    }

    internal static HtmlLineMap CreateWithByteOffsets(int initialCharacterOffset, long initialByteOffset)
    {
        return new HtmlLineMap(initialCharacterOffset, true, initialByteOffset);
    }

    internal int GetLength()
    {
        return this.length;
    }

    internal int GetLineCount()
    {
        return this.lineCount;
    }

    internal bool TracksByteOffsets()
    {
        return this.trackByteOffsets;
    }

    internal void Append(char value)
    {
        if (this.trackByteOffsets)
        {
            throw new InvalidOperationException("A byte-tracking line map requires byte offsets for every appended character.");
        }

        int characterOffset = this.characterOffsets[this.length];
        this.AppendCore(value, characterOffset, AddOne(characterOffset), -1, -1);
    }

    internal void AppendWithCharacterOffsets(char value, int characterOffset, int nextCharacterOffset)
    {
        if (this.trackByteOffsets)
        {
            throw new InvalidOperationException("A byte-tracking line map requires byte offsets for every appended character.");
        }

        this.AppendCore(value, characterOffset, nextCharacterOffset, -1, -1);
    }

    internal void AppendWithByteOffsets(char value, int characterOffset, int nextCharacterOffset, long byteOffset, long nextByteOffset)
    {
        if (!this.trackByteOffsets)
        {
            throw new InvalidOperationException("This line map was created without byte-offset tracking.");
        }

        if (byteOffset != this.byteOffsets[this.length])
        {
            throw new ArgumentException("The byte offset must continue from the preceding normalized character.", "byteOffset");
        }

        if (nextByteOffset < byteOffset)
        {
            throw new ArgumentOutOfRangeException("nextByteOffset", nextByteOffset, "A next byte offset cannot precede the current byte offset.");
        }

        this.AppendCore(value, characterOffset, nextCharacterOffset, byteOffset, nextByteOffset);
    }

    internal HtmlSourcePosition GetPosition(int normalizedCharacterOffset)
    {
        if (normalizedCharacterOffset < 0 || normalizedCharacterOffset > this.length)
        {
            throw new ArgumentOutOfRangeException("normalizedCharacterOffset", normalizedCharacterOffset, "The normalized character offset is outside this source map.");
        }

        int low = 0;
        int high = this.lineCount - 1;
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            if (this.lineStarts[middle] <= normalizedCharacterOffset)
            {
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        int lineIndex = high;
        int line = lineIndex + 1;
        int column = normalizedCharacterOffset - this.lineStarts[lineIndex] + 1;
        int characterOffset = this.characterOffsets[normalizedCharacterOffset];
        if (this.trackByteOffsets)
        {
            return HtmlSourcePosition.CreateMappedWithByteOffset(normalizedCharacterOffset, characterOffset, line, column, this.byteOffsets[normalizedCharacterOffset]);
        }

        return HtmlSourcePosition.CreateMapped(normalizedCharacterOffset, characterOffset, line, column);
    }

    internal HtmlSourceSpan GetSpan(int normalizedStart, int normalizedEnd)
    {
        if (normalizedEnd < normalizedStart)
        {
            throw new ArgumentException("The normalized end offset cannot precede the normalized start offset.");
        }

        return HtmlSourceSpan.Create(this.GetPosition(normalizedStart), this.GetPosition(normalizedEnd));
    }

    internal void AppendCore(char value, int characterOffset, int nextCharacterOffset, long byteOffset, long nextByteOffset)
    {
        if (value == '\r')
        {
            throw new ArgumentException("HtmlLineMap accepts normalized newlines only.", "value");
        }

        if (this.length >= int.MaxValue - 1)
        {
            throw new InsufficientMemoryException("The source map cannot represent another character.");
        }

        if (characterOffset != this.characterOffsets[this.length])
        {
            throw new ArgumentException("The character offset must continue from the preceding normalized character.", "characterOffset");
        }

        if (nextCharacterOffset < characterOffset)
        {
            throw new ArgumentOutOfRangeException("nextCharacterOffset", nextCharacterOffset, "A next character offset cannot precede the current character offset.");
        }

        int nextLength = this.length + 1;
        this.EnsurePositionCapacity(nextLength + 1);
        this.characterOffsets[nextLength] = nextCharacterOffset;
        if (this.trackByteOffsets)
        {
            this.byteOffsets[nextLength] = nextByteOffset;
        }

        this.length = nextLength;
        if (value == '\n')
        {
            this.EnsureLineCapacity(this.lineCount + 1);
            this.lineStarts[this.lineCount] = this.length;
            this.lineCount = this.lineCount + 1;
        }
    }

    internal void EnsurePositionCapacity(int required)
    {
        if (required <= this.characterOffsets.Length)
        {
            return;
        }

        int capacity = GrowCapacity(this.characterOffsets.Length, required);
        int[] characters = new int[capacity];
        Array.Copy(this.characterOffsets, characters, this.length + 1);
        this.characterOffsets = characters;
        if (this.trackByteOffsets)
        {
            long[] bytes = new long[capacity];
            Array.Copy(this.byteOffsets, bytes, this.length + 1);
            this.byteOffsets = bytes;
        }
    }

    internal void EnsureLineCapacity(int required)
    {
        if (required <= this.lineStarts.Length)
        {
            return;
        }

        int capacity = GrowCapacity(this.lineStarts.Length, required);
        int[] starts = new int[capacity];
        Array.Copy(this.lineStarts, starts, this.lineCount);
        this.lineStarts = starts;
    }

    internal static int AddOne(int value)
    {
        if (value == int.MaxValue)
        {
            throw new ArgumentOutOfRangeException("characterOffset", value, "The next character offset cannot be represented.");
        }

        return value + 1;
    }

    internal static int GrowCapacity(int current, int required)
    {
        int doubled;
        if (current > int.MaxValue / 2)
        {
            doubled = int.MaxValue;
        }
        else
        {
            doubled = current * 2;
        }

        if (doubled < required)
        {
            doubled = required;
        }

        return doubled;
    }
}
