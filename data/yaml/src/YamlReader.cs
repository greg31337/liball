using System.Text;

namespace yaml;

/// <summary>
/// Reads normalized YAML text while tracking indexes, lines, columns, and an input BOM.
/// </summary>
internal class YamlReader
{
    /// <summary>
    /// The normalized input text read by this instance.
    /// </summary>
    internal string Text = "";
    /// <summary>
    /// The number of UTF-16 characters in the normalized input text.
    /// </summary>
    internal int Length;
    /// <summary>
    /// The zero-based index of the current character in normalized input.
    /// </summary>
    internal int Index;
    /// <summary>
    /// The current one-based source line.
    /// </summary>
    internal int Line;
    /// <summary>
    /// The current one-based source column.
    /// </summary>
    internal int Column;
    /// <summary>
    /// Whether the original input began with a recognized byte-order mark.
    /// </summary>
    internal bool HadBom;

    /// <summary>
    /// Initializes a reader over normalized YAML text at line 1, column 1.
    /// </summary>
    internal YamlReader()
    {
        Reset("");
    }

    /// <summary>
    /// Creates a reader over normalized YAML text at line 1, column 1.
    /// </summary>
    /// <param name="text">The YAML text to process.</param>
    /// <returns>The resulting YAML reader.</returns>
    internal static YamlReader FromText(string? text)
    {
        YamlReader reader = new YamlReader();
        reader.Reset(text);
        return reader;
    }

    /// <summary>
    /// Creates a reader over normalized YAML text and records the caller-supplied byte-order-mark state.
    /// </summary>
    /// <param name="text">The YAML text to process.</param>
    /// <param name="hadBom">Whether the supplied text originally had a byte-order mark.</param>
    /// <returns>The resulting YAML reader.</returns>
    internal static YamlReader FromText(string? text, bool hadBom)
    {
        YamlReader reader = new YamlReader();
        reader.ResetWithBom(text, hadBom);
        return reader;
    }

    /// <summary>
    /// Decodes UTF-8 or UTF-16 input, recognizes a byte-order mark, normalizes line endings, and returns a positioned reader.
    /// </summary>
    /// <param name="bytes">The encoded input bytes; UTF-8 and UTF-16 byte-order marks are recognized.</param>
    /// <returns>The resulting YAML reader.</returns>
    internal static YamlReader FromBytes(byte[]? bytes)
    {
        if (bytes == null)
        {
            return FromText("");
        }

        if (bytes.Length >= 3 && bytes[0] == 239 && bytes[1] == 187 && bytes[2] == 191)
        {
            return FromText(Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3), true);
        }

        if (bytes.Length >= 2 && bytes[0] == 255 && bytes[1] == 254)
        {
            return FromText(Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2), true);
        }

        if (bytes.Length >= 2 && bytes[0] == 254 && bytes[1] == 255)
        {
            return FromText(Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2), true);
        }

        return FromText(Encoding.UTF8.GetString(bytes));
    }

    /// <summary>
    /// Resets this instance to the library's default state.
    /// </summary>
    /// <param name="text">The YAML text to process.</param>
    internal void Reset(string? text)
    {
        ResetWithBom(text, false);
    }

    /// <summary>
    /// Replaces the input, normalizes its line endings, and records whether it originally had a byte-order mark.
    /// </summary>
    /// <param name="text">The YAML text to process.</param>
    /// <param name="hadBom">Whether the supplied text originally had a byte-order mark.</param>
    internal void ResetWithBom(string? text, bool hadBom)
    {
        HadBom = HasStringBom(text) || hadBom;
        Text = NormalizeText(text);
        Length = Text.Length;
        Index = 0;
        Line = 1;
        Column = 1;
    }

    /// <summary>
    /// Determines whether a string begins with the Unicode byte-order-mark character.
    /// </summary>
    /// <param name="text">The YAML text to process.</param>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal static bool HasStringBom(string? text)
    {
        if (text == null)
        {
            return false;
        }

        return text.Length > 0 && text[0] == '\uFEFF';
    }

    /// <summary>
    /// Removes an initial Unicode byte-order mark and normalizes CRLF and CR line endings to LF.
    /// </summary>
    /// <param name="text">The YAML text to process.</param>
    /// <returns>The emitted or formatted text.</returns>
    internal static string NormalizeText(string? text)
    {
        StringBuilder builder = new StringBuilder();
        int index = 0;

        if (text == null)
        {
            return "";
        }

        if (text.Length > 0 && text[0] == '\uFEFF')
        {
            index = 1;
        }

        while (index < text.Length)
        {
            char current = text[index];
            if (current == '\r')
            {
                builder.Append('\n');
                if (index + 1 < text.Length && text[index + 1] == '\n')
                {
                    index = index + 1;
                }
            }
            else
            {
                builder.Append(current);
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    /// <summary>
    /// Determines whether the reader has reached the end of normalized input.
    /// </summary>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool End()
    {
        return Index >= Length;
    }

    /// <summary>
    /// Returns the current character, or the null character at end of input.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    internal char Current()
    {
        return Peek(0);
    }

    /// <summary>
    /// Returns a character relative to the current position, or the null character outside the input.
    /// </summary>
    /// <param name="offset">The non-negative offset from the current reader position.</param>
    /// <returns>The result of the operation.</returns>
    internal char Peek(int offset)
    {
        if (offset < 0 || Index < 0 || Index >= Length || offset >= Length - Index)
        {
            return '\0';
        }

        return Text[Index + offset];
    }

    /// <summary>
    /// Reads and advances over the requested input, returning the consumed value.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    internal char Read()
    {
        char current = Current();
        if (current == '\0')
        {
            return '\0';
        }

        Advance();
        return current;
    }

    /// <summary>
    /// Advances one character only when the current character matches the supplied value.
    /// </summary>
    /// <param name="expected">The character to consume when it is current.</param>
    /// <returns>True when the condition is met; otherwise false.</returns>
    internal bool Consume(char expected)
    {
        if (Current() != expected)
        {
            return false;
        }

        Advance();
        return true;
    }

    /// <summary>
    /// Advances one normalized character and updates the one-based line and column.
    /// </summary>
    internal void Advance()
    {
        if (End())
        {
            return;
        }

        char current = Text[Index];
        Index = Index + 1;
        if (current == '\n')
        {
            if (Line < int.MaxValue)
            {
                Line = Line + 1;
            }

            Column = 1;
        }
        else if (Column < int.MaxValue)
        {
            Column = Column + 1;
        }
    }

    /// <summary>
    /// Advances by up to the requested number of normalized characters.
    /// </summary>
    /// <param name="count">The number of characters or items to process.</param>
    internal void AdvanceBy(int count)
    {
        int remaining = count;
        while (remaining > 0 && !End())
        {
            Advance();
            remaining = remaining - 1;
        }
    }

    /// <summary>
    /// Captures an empty source range at the current reader position.
    /// </summary>
    /// <returns>The resulting YAML source span.</returns>
    internal YamlSourceSpan Mark()
    {
        return YamlSourceSpan.FromBounds(Index, Line, Column, Index, Line, Column);
    }

    /// <summary>
    /// Extends a previously captured mark through the current reader position.
    /// </summary>
    /// <param name="start">The start used by the operation.</param>
    /// <returns>The resulting YAML source span.</returns>
    internal YamlSourceSpan SpanFrom(YamlSourceSpan? start)
    {
        if (start == null)
        {
            return YamlSourceSpan.FromBounds(Index, Line, Column, Index, Line, Column);
        }

        return YamlSourceSpan.FromBounds(start.StartIndex, start.StartLine, start.StartColumn, Index, Line, Column);
    }

    /// <summary>
    /// Reads and advances over the requested input, returning the consumed value.
    /// </summary>
    /// <param name="count">The number of characters or items to process.</param>
    /// <returns>The emitted or formatted text.</returns>
    internal string ReadText(int count)
    {
        StringBuilder builder = new StringBuilder();
        int remaining = count;
        while (remaining > 0 && !End())
        {
            builder.Append(Read());
            remaining = remaining - 1;
        }

        return builder.ToString();
    }
}
