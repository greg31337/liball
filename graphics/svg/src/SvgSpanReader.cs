using System;

namespace svg;

/// <summary>Defines stable failures produced by the internal character reader.</summary>
internal enum SvgSpanReadError
{
    /// <summary>No read failure occurred.</summary>
    None = 0,

    /// <summary>The reader reached the end of its input.</summary>
    EndOfInput = 1,

    /// <summary>The current character did not match the requested grammar.</summary>
    UnexpectedCharacter = 2,

    /// <summary>An escape ended early or escaped a line break.</summary>
    InvalidEscape = 3,

    /// <summary>A token exceeded the configured raw character bound.</summary>
    TokenTooLong = 4,

    /// <summary>An internal reader argument was invalid.</summary>
    InvalidArgument = 5
}

/// <summary>Contains a non-throwing single-character read result.</summary>
internal class SvgSpanCharacterResult
{
    /// <summary>Indicates whether a character was read.</summary>
    public bool Success;

    /// <summary>The failure kind.</summary>
    public SvgSpanReadError Error;

    /// <summary>The character offset before the read.</summary>
    public int Start;

    /// <summary>The character offset after the read.</summary>
    public int End;

    /// <summary>The exact failure offset, or <c>-1</c> after success.</summary>
    public int ErrorOffset;

    /// <summary>The consumed character, or the null character after failure.</summary>
    public char Value;

    /// <summary>Creates a character read result.</summary>
    /// <param name="success">Whether the read succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="start">The starting character offset.</param>
    /// <param name="end">The ending character offset.</param>
    /// <param name="errorOffset">The exact failure offset, or <c>-1</c>.</param>
    /// <param name="value">The consumed character, or the null character.</param>
    public SvgSpanCharacterResult(bool success, SvgSpanReadError error, int start, int end, int errorOffset, char value)
    {
        Success = success;
        Error = error;
        Start = start;
        End = end;
        ErrorOffset = errorOffset;
        Value = value;
    }
}

/// <summary>Contains a non-throwing comma-and-whitespace read result.</summary>
internal class SvgSpanSeparatorResult
{
    /// <summary>Indicates whether the requested separator was present.</summary>
    public bool Success;

    /// <summary>The failure kind.</summary>
    public SvgSpanReadError Error;

    /// <summary>The character offset before separator scanning.</summary>
    public int Start;

    /// <summary>The character offset after separator scanning.</summary>
    public int End;

    /// <summary>The exact failure offset, or <c>-1</c> after success.</summary>
    public int ErrorOffset;

    /// <summary>Indicates whether a comma was consumed.</summary>
    public bool HasComma;

    /// <summary>The total SVG whitespace characters consumed around the comma.</summary>
    public int WhitespaceCharacters;

    /// <summary>Creates a separator read result.</summary>
    /// <param name="success">Whether the requested separator was present.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="start">The starting character offset.</param>
    /// <param name="end">The ending character offset.</param>
    /// <param name="errorOffset">The exact failure offset, or <c>-1</c>.</param>
    /// <param name="hasComma">Whether a comma was consumed.</param>
    /// <param name="whitespaceCharacters">The whitespace character count.</param>
    public SvgSpanSeparatorResult(bool success, SvgSpanReadError error, int start, int end, int errorOffset, bool hasComma, int whitespaceCharacters)
    {
        Success = success;
        Error = error;
        Start = start;
        End = end;
        ErrorOffset = errorOffset;
        HasComma = hasComma;
        WhitespaceCharacters = whitespaceCharacters;
    }
}

/// <summary>Contains a non-throwing CSS escape read result.</summary>
internal class SvgSpanEscapeResult
{
    /// <summary>Indicates whether an escape was decoded.</summary>
    public bool Success;

    /// <summary>The failure kind.</summary>
    public SvgSpanReadError Error;

    /// <summary>The offset of the backslash.</summary>
    public int Start;

    /// <summary>The exclusive ending offset inspected by the read.</summary>
    public int End;

    /// <summary>The exact failure offset, or <c>-1</c> after success.</summary>
    public int ErrorOffset;

    /// <summary>The decoded Unicode scalar, or zero after failure.</summary>
    public int CodePoint;

    /// <summary>The decoded UTF-16 text, or an empty string after failure.</summary>
    public string Value;

    /// <summary>Creates an escape read result.</summary>
    /// <param name="success">Whether the escape was decoded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="start">The offset of the backslash.</param>
    /// <param name="end">The exclusive ending offset inspected by the read.</param>
    /// <param name="errorOffset">The exact failure offset, or <c>-1</c>.</param>
    /// <param name="codePoint">The decoded Unicode scalar, or zero.</param>
    /// <param name="value">The decoded UTF-16 text, or an empty string.</param>
    public SvgSpanEscapeResult(bool success, SvgSpanReadError error, int start, int end, int errorOffset, int codePoint, string value)
    {
        Success = success;
        Error = error;
        Start = start;
        End = end;
        ErrorOffset = errorOffset;
        CodePoint = codePoint;
        Value = value;
    }
}

/// <summary>Contains a bounded token read without copying its raw input.</summary>
internal class SvgSpanTokenResult
{
    /// <summary>Indicates whether token scanning succeeded.</summary>
    public bool Success;

    /// <summary>The failure kind.</summary>
    public SvgSpanReadError Error;

    /// <summary>The token's starting character offset.</summary>
    public int Start;

    /// <summary>The exclusive ending offset inspected by the read.</summary>
    public int End;

    /// <summary>The exact failure offset, or <c>-1</c> after success.</summary>
    public int ErrorOffset;

    /// <summary>The raw token input or inspected failure prefix.</summary>
    public System.ReadOnlyMemory<char> RawText;

    /// <summary>The decoded value when escapes occurred, or <c>null</c> otherwise.</summary>
    public string? DecodedValue;

    /// <summary>Indicates whether the successful token contained an escape.</summary>
    public bool HadEscape;

    /// <summary>Creates a bounded token read result.</summary>
    /// <param name="success">Whether token scanning succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="start">The token's starting offset.</param>
    /// <param name="end">The exclusive ending offset inspected by the read.</param>
    /// <param name="errorOffset">The exact failure offset, or <c>-1</c>.</param>
    /// <param name="rawText">The raw token input or inspected prefix.</param>
    /// <param name="decodedValue">The decoded value, or <c>null</c>.</param>
    /// <param name="hadEscape">Whether the successful token contained an escape.</param>
    public SvgSpanTokenResult(bool success, SvgSpanReadError error, int start, int end, int errorOffset, System.ReadOnlyMemory<char> rawText, string? decodedValue, bool hadEscape)
    {
        Success = success;
        Error = error;
        Start = start;
        End = end;
        ErrorOffset = errorOffset;
        RawText = rawText;
        DecodedValue = decodedValue;
        HadEscape = hadEscape;
    }

    /// <summary>Returns decoded token text, allocating only when the raw slice must become a string.</summary>
    /// <returns>The decoded token text.</returns>
    public string GetValue()
    {
        if (DecodedValue != null)
        {
            return DecodedValue;
        }
        return RawText.Span.ToString();
    }
}

/// <summary>Scans bounded SVG and CSS character microsyntax over an input memory slice.</summary>
internal class SvgSpanReader
{
    System.ReadOnlyMemory<char> text;
    int position;
    int maximumTokenCharacters;

    /// <summary>Creates a reader over a string without copying its characters.</summary>
    /// <param name="value">The input string, or <c>null</c> for empty input.</param>
    /// <param name="maxTokenCharacters">The maximum raw characters in one token.</param>
    public SvgSpanReader(string? value, int maxTokenCharacters)
    {
        text = value == null ? System.ReadOnlyMemory<char>.Empty : value.AsMemory();
        position = 0;
        maximumTokenCharacters = maxTokenCharacters;
    }

    /// <summary>Creates a reader over an existing character memory slice.</summary>
    /// <param name="value">The input memory slice.</param>
    /// <param name="maxTokenCharacters">The maximum raw characters in one token.</param>
    /// <returns>A new reader over the supplied memory.</returns>
    public static SvgSpanReader FromMemory(System.ReadOnlyMemory<char> value, int maxTokenCharacters)
    {
        SvgSpanReader result = new SvgSpanReader(null, maxTokenCharacters);
        result.text = value;
        return result;
    }

    /// <summary>Returns the current zero-based character offset.</summary>
    /// <returns>The current offset.</returns>
    public int GetPosition()
    {
        return position;
    }

    /// <summary>Returns the input character count.</summary>
    /// <returns>The input length.</returns>
    public int GetLength()
    {
        return text.Length;
    }

    /// <summary>Returns the configured raw token character bound.</summary>
    /// <returns>The raw token character bound.</returns>
    public int GetMaximumTokenCharacters()
    {
        return maximumTokenCharacters;
    }

    /// <summary>Returns the complete input memory without copying its characters.</summary>
    /// <returns>The input memory slice.</returns>
    public System.ReadOnlyMemory<char> GetInput()
    {
        return text;
    }

    /// <summary>Tests whether the reader is at the end of input.</summary>
    /// <returns><c>true</c> at the end of input.</returns>
    public bool IsEnd()
    {
        return position >= text.Length;
    }

    /// <summary>Moves to a valid absolute offset without throwing.</summary>
    /// <param name="value">The requested offset from zero through the input length.</param>
    /// <returns><c>true</c> when the position was changed.</returns>
    public bool SetPosition(int value)
    {
        if (value < 0 || value > text.Length)
        {
            return false;
        }
        position = value;
        return true;
    }

    /// <summary>Reads the current character and advances one code unit.</summary>
    /// <returns>A successful character or an end-of-input result.</returns>
    public SvgSpanCharacterResult ReadCharacter()
    {
        int start = position;
        if (position >= text.Length)
        {
            return new SvgSpanCharacterResult(false, SvgSpanReadError.EndOfInput, start, start, start, '\0');
        }
        char value = text.Span[position];
        position = position + 1;
        return new SvgSpanCharacterResult(true, SvgSpanReadError.None, start, position, -1, value);
    }

    /// <summary>Reads the current character without advancing.</summary>
    /// <returns>A successful character or an end-of-input result.</returns>
    public SvgSpanCharacterResult PeekCharacter()
    {
        int start = position;
        if (position >= text.Length)
        {
            return new SvgSpanCharacterResult(false, SvgSpanReadError.EndOfInput, start, start, start, '\0');
        }
        return new SvgSpanCharacterResult(true, SvgSpanReadError.None, start, start, -1, text.Span[position]);
    }

    /// <summary>Consumes an expected character when present.</summary>
    /// <param name="value">The expected character.</param>
    /// <returns><c>true</c> when the character was consumed.</returns>
    public bool Consume(char value)
    {
        if (position >= text.Length || text.Span[position] != value)
        {
            return false;
        }
        position = position + 1;
        return true;
    }

    /// <summary>Skips SVG whitespace characters: space, tab, carriage return, and line feed.</summary>
    /// <returns>The number of characters consumed.</returns>
    public int SkipWhitespace()
    {
        int start = position;
        while (position < text.Length && SvgSpanCharacters.IsSvgWhitespace(text.Span[position]))
        {
            position = position + 1;
        }
        return position - start;
    }

    /// <summary>Skips CSS whitespace, including form feed.</summary>
    /// <returns>The number of characters consumed.</returns>
    public int SkipCssWhitespace()
    {
        int start = position;
        while (position < text.Length && SvgSpanCharacters.IsCssWhitespace(text.Span[position]))
        {
            position = position + 1;
        }
        return position - start;
    }

    /// <summary>Consumes optional SVG whitespace, one optional comma, and trailing SVG whitespace.</summary>
    /// <returns>An always-successful separator result.</returns>
    public SvgSpanSeparatorResult SkipOptionalCommaWhitespace()
    {
        int start = position;
        int whitespace = SkipWhitespace();
        bool comma = Consume(',');
        whitespace = whitespace + SkipWhitespace();
        return new SvgSpanSeparatorResult(true, SvgSpanReadError.None, start, position, -1, comma, whitespace);
    }

    /// <summary>Requires SVG whitespace or a comma with optional surrounding SVG whitespace.</summary>
    /// <returns>A separator result; failure leaves the reader at its original position.</returns>
    public SvgSpanSeparatorResult ReadCommaWhitespace()
    {
        int start = position;
        int whitespace = SkipWhitespace();
        bool comma = Consume(',');
        whitespace = whitespace + SkipWhitespace();
        if (!comma && whitespace == 0)
        {
            position = start;
            SvgSpanReadError error = start >= text.Length ? SvgSpanReadError.EndOfInput : SvgSpanReadError.UnexpectedCharacter;
            return new SvgSpanSeparatorResult(false, error, start, start, start, false, 0);
        }
        return new SvgSpanSeparatorResult(true, SvgSpanReadError.None, start, position, -1, comma, whitespace);
    }

    /// <summary>Decodes one CSS escape beginning at the current backslash.</summary>
    /// <returns>An escape result; failure leaves the reader at its original position.</returns>
    public SvgSpanEscapeResult ReadEscape()
    {
        int start = position;
        if (position >= text.Length)
        {
            return new SvgSpanEscapeResult(false, SvgSpanReadError.EndOfInput, start, start, start, 0, "");
        }
        if (text.Span[position] != '\\')
        {
            return new SvgSpanEscapeResult(false, SvgSpanReadError.UnexpectedCharacter, start, start, start, 0, "");
        }

        position = position + 1;
        if (position >= text.Length)
        {
            int endOffset = position;
            position = start;
            return new SvgSpanEscapeResult(false, SvgSpanReadError.InvalidEscape, start, endOffset, endOffset, 0, "");
        }

        char current = text.Span[position];
        if (current == '\n' || current == '\r' || current == '\f')
        {
            int errorOffset = position;
            int endOffset = position + 1;
            position = start;
            return new SvgSpanEscapeResult(false, SvgSpanReadError.InvalidEscape, start, endOffset, errorOffset, 0, "");
        }

        int firstHex = SvgSpanCharacters.HexValue(current);
        if (firstHex < 0)
        {
            position = position + 1;
            return new SvgSpanEscapeResult(true, SvgSpanReadError.None, start, position, -1, current, current.ToString());
        }

        int codePoint = 0;
        int digits = 0;
        while (position < text.Length && digits < 6)
        {
            int hex = SvgSpanCharacters.HexValue(text.Span[position]);
            if (hex < 0)
            {
                break;
            }
            codePoint = codePoint * 16 + hex;
            digits = digits + 1;
            position = position + 1;
        }

        if (position < text.Length && SvgSpanCharacters.IsCssWhitespace(text.Span[position]))
        {
            char whitespace = text.Span[position];
            position = position + 1;
            if (whitespace == '\r' && position < text.Length && text.Span[position] == '\n')
            {
                position = position + 1;
            }
        }

        if (codePoint == 0 || codePoint > 1114111 || codePoint >= 55296 && codePoint <= 57343)
        {
            codePoint = 65533;
        }
        return new SvgSpanEscapeResult(true, SvgSpanReadError.None, start, position, -1, codePoint, char.ConvertFromUtf32(codePoint));
    }

    /// <summary>Reads a bounded token accepted by a caller predicate and optionally decodes CSS escapes.</summary>
    /// <param name="accept">The predicate for unescaped characters.</param>
    /// <param name="allowEscapes">Whether a backslash starts a CSS escape.</param>
    /// <returns>A token result; failure leaves the reader at its original position.</returns>
    public SvgSpanTokenResult ReadToken(System.Func<char, bool> accept, bool allowEscapes)
    {
        int start = position;
        if (accept == null || maximumTokenCharacters < 1)
        {
            return new SvgSpanTokenResult(false, SvgSpanReadError.InvalidArgument, start, start, start, text.Slice(start, 0), null, false);
        }

        System.Text.StringBuilder? decoded = null;
        int copyStart = start;
        bool hadEscape = false;
        while (position < text.Length)
        {
            char current = text.Span[position];
            if (accept(current))
            {
                if (position - start >= maximumTokenCharacters)
                {
                    int errorOffset = position;
                    System.ReadOnlyMemory<char> prefix = text.Slice(start, position + 1 - start);
                    position = start;
                    return new SvgSpanTokenResult(false, SvgSpanReadError.TokenTooLong, start, errorOffset + 1, errorOffset, prefix, null, false);
                }
                position = position + 1;
                continue;
            }

            if (current != '\\' || !allowEscapes)
            {
                break;
            }

            int escapeStart = position;
            SvgSpanEscapeResult escape = ReadEscape();
            if (!escape.Success)
            {
                int inspectedEnd = escape.End;
                System.ReadOnlyMemory<char> prefix = text.Slice(start, inspectedEnd - start);
                position = start;
                return new SvgSpanTokenResult(false, escape.Error, start, inspectedEnd, escape.ErrorOffset, prefix, null, false);
            }
            if (position - start > maximumTokenCharacters)
            {
                int errorOffset = start + maximumTokenCharacters;
                System.ReadOnlyMemory<char> prefix = text.Slice(start, position - start);
                int inspectedEnd = position;
                position = start;
                return new SvgSpanTokenResult(false, SvgSpanReadError.TokenTooLong, start, inspectedEnd, errorOffset, prefix, null, false);
            }

            if (decoded == null)
            {
                decoded = new System.Text.StringBuilder();
            }
            decoded.Append(text.Span.Slice(copyStart, escapeStart - copyStart));
            decoded.Append(escape.Value);
            copyStart = position;
            hadEscape = true;
        }

        int end = position;
        string? decodedValue = null;
        if (decoded != null)
        {
            decoded.Append(text.Span.Slice(copyStart, end - copyStart));
            decodedValue = decoded.ToString();
        }
        return new SvgSpanTokenResult(true, SvgSpanReadError.None, start, end, -1, text.Slice(start, end - start), decodedValue, hadEscape);
    }
}

/// <summary>Provides shared character classification for SVG and CSS scanners.</summary>
internal static class SvgSpanCharacters
{
    /// <summary>Tests for SVG space, tab, carriage return, or line feed.</summary>
    /// <param name="value">The character to test.</param>
    /// <returns><c>true</c> for SVG whitespace.</returns>
    public static bool IsSvgWhitespace(char value)
    {
        return value == ' ' || value == '\t' || value == '\r' || value == '\n';
    }

    /// <summary>Tests for CSS whitespace, including form feed.</summary>
    /// <param name="value">The character to test.</param>
    /// <returns><c>true</c> for CSS whitespace.</returns>
    public static bool IsCssWhitespace(char value)
    {
        return IsSvgWhitespace(value) || value == '\f';
    }

    /// <summary>Returns an ASCII hexadecimal digit value.</summary>
    /// <param name="value">The character to inspect.</param>
    /// <returns>A value from zero through fifteen, or <c>-1</c>.</returns>
    public static int HexValue(char value)
    {
        if (value >= '0' && value <= '9')
        {
            return value - '0';
        }
        if (value >= 'A' && value <= 'F')
        {
            return value - 'A' + 10;
        }
        if (value >= 'a' && value <= 'f')
        {
            return value - 'a' + 10;
        }
        return -1;
    }
}
