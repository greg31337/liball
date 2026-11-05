namespace geo;

internal class GeoJsonSyntaxValidator
{
    internal byte[] Source;
    internal int MaximumDepth;
    internal int Position;
    internal ParseError? Failure;

    internal GeoJsonSyntaxValidator(byte[] source, int maximumDepth)
    {
        Source = source;
        MaximumDepth = maximumDepth;
    }

    internal bool ValidateDocument()
    {
        SkipWhitespace();
        if (!ParseValue(0)) return false;
        SkipWhitespace();
        if (Position != Source.Length)
        {
            SetFailure(ParseErrorKind.TrailingContent, Position,
                $"Unexpected content follows the JSON value at byte offset {Position}.");
            return false;
        }
        return true;
    }

    internal bool ParseValue(int depth)
    {
        if (Position >= Source.Length)
        {
            SetFailure(ParseErrorKind.UnexpectedEnd, Position,
                $"JSON input ended before a value at byte offset {Position}.");
            return false;
        }

        byte current = Source[Position];
        if (current == (byte)'{') return ParseObject(depth + 1);
        if (current == (byte)'[') return ParseArray(depth + 1);
        if (current == (byte)'"') return ParseString();
        if (current == (byte)'t') return ParseLiteral("true");
        if (current == (byte)'f') return ParseLiteral("false");
        if (current == (byte)'n') return ParseLiteral("null");
        if (current == (byte)'-' || IsDigit(current)) return ParseNumber();

        SetFailure(ParseErrorKind.UnexpectedToken, Position,
            $"JSON value token is invalid at byte offset {Position}.");
        return false;
    }

    internal bool ParseObject(int depth)
    {
        int start = Position;
        if (!CheckDepth(depth, start)) return false;
        Position++;
        SkipWhitespace();
        if (ConsumeByte((byte)'}')) return true;

        while (Failure == null)
        {
            if (Position >= Source.Length)
            {
                SetFailure(ParseErrorKind.UnexpectedEnd, Position,
                    $"JSON object is not closed at byte offset {Position}.");
                return false;
            }
            if (Source[Position] != (byte)'"')
            {
                SetFailure(ParseErrorKind.UnexpectedToken, Position,
                    $"JSON object member name must be a string at byte offset {Position}.");
                return false;
            }
            if (!ParseString()) return false;
            SkipWhitespace();
            if (!RequireByte((byte)':', "a colon after the JSON member name")) return false;
            SkipWhitespace();
            if (!ParseValue(depth)) return false;
            SkipWhitespace();
            if (ConsumeByte((byte)'}')) return true;
            if (!RequireByte((byte)',', "a comma or closing brace in the JSON object")) return false;
            SkipWhitespace();
        }
        return false;
    }

    internal bool ParseArray(int depth)
    {
        int start = Position;
        if (!CheckDepth(depth, start)) return false;
        Position++;
        SkipWhitespace();
        if (ConsumeByte((byte)']')) return true;

        while (Failure == null)
        {
            if (!ParseValue(depth)) return false;
            SkipWhitespace();
            if (ConsumeByte((byte)']')) return true;
            if (!RequireByte((byte)',', "a comma or closing bracket in the JSON array")) return false;
            SkipWhitespace();
        }
        return false;
    }

    internal bool ParseString()
    {
        int start = Position;
        Position++;
        while (Position < Source.Length)
        {
            byte current = Source[Position];
            if (current == (byte)'"')
            {
                Position++;
                return true;
            }
            if (current == (byte)'\\')
            {
                if (!ParseEscapeSequence()) return false;
                continue;
            }
            if (current < 0x20)
            {
                SetFailure(ParseErrorKind.UnexpectedToken, Position,
                    $"JSON string contains an unescaped control character at byte offset {Position}.");
                return false;
            }
            if (current < 0x80)
            {
                Position++;
                continue;
            }
            if (!ParseUtf8Scalar()) return false;
        }

        SetFailure(ParseErrorKind.UnexpectedEnd, Source.Length,
            $"JSON string beginning at byte offset {start} is not closed at byte offset {Source.Length}.");
        return false;
    }

    internal bool ParseEscapeSequence()
    {
        int escapeOffset = Position;
        Position++;
        if (Position >= Source.Length)
        {
            SetFailure(ParseErrorKind.UnexpectedEnd, Position,
                $"JSON escape sequence is incomplete at byte offset {Position}.");
            return false;
        }

        byte escaped = Source[Position];
        if (escaped == (byte)'"' || escaped == (byte)'\\' || escaped == (byte)'/' ||
            escaped == (byte)'b' || escaped == (byte)'f' || escaped == (byte)'n' ||
            escaped == (byte)'r' || escaped == (byte)'t')
        {
            Position++;
            return true;
        }
        if (escaped != (byte)'u')
        {
            SetFailure(ParseErrorKind.UnexpectedToken, escapeOffset,
                $"JSON escape sequence is invalid at byte offset {escapeOffset}.");
            return false;
        }

        Position++;
        int codeUnit = ReadUnicodeCodeUnit();
        if (Failure != null) return false;
        if (codeUnit >= 0xdc00 && codeUnit <= 0xdfff)
        {
            SetFailure(ParseErrorKind.UnexpectedToken, escapeOffset,
                $"JSON string contains an unpaired low surrogate at byte offset {escapeOffset}.");
            return false;
        }
        if (codeUnit >= 0xd800 && codeUnit <= 0xdbff)
        {
            int lowOffset = Position;
            if (Position + 2 > Source.Length || Source[Position] != (byte)'\\' ||
                Source[Position + 1] != (byte)'u')
            {
                SetFailure(ParseErrorKind.UnexpectedToken, lowOffset,
                    $"JSON high surrogate is not followed by a low surrogate at byte offset {lowOffset}.");
                return false;
            }
            Position += 2;
            int low = ReadUnicodeCodeUnit();
            if (Failure != null) return false;
            if (low < 0xdc00 || low > 0xdfff)
            {
                SetFailure(ParseErrorKind.UnexpectedToken, lowOffset,
                    $"JSON high surrogate is not followed by a low surrogate at byte offset {lowOffset}.");
                return false;
            }
        }
        return true;
    }

    internal int ReadUnicodeCodeUnit()
    {
        if (Position > Source.Length - 4)
        {
            SetFailure(ParseErrorKind.UnexpectedEnd, Source.Length,
                $"JSON Unicode escape is incomplete at byte offset {Source.Length}.");
            return 0;
        }

        int value = 0;
        int count = 0;
        while (count < 4)
        {
            int digit = GetHexDigit(Source[Position]);
            if (digit < 0)
            {
                SetFailure(ParseErrorKind.UnexpectedToken, Position,
                    $"JSON Unicode escape digit is invalid at byte offset {Position}.");
                return 0;
            }
            value = value * 16 + digit;
            Position++;
            count++;
        }
        return value;
    }

    internal bool ParseUtf8Scalar()
    {
        int start = Position;
        byte first = Source[Position];
        int length;
        uint value;
        if (first >= 0xc2 && first <= 0xdf)
        {
            length = 2;
            value = (uint)(first & 0x1f);
        }
        else if (first >= 0xe0 && first <= 0xef)
        {
            length = 3;
            value = (uint)(first & 0x0f);
        }
        else if (first >= 0xf0 && first <= 0xf4)
        {
            length = 4;
            value = (uint)(first & 0x07);
        }
        else
        {
            SetFailure(ParseErrorKind.UnexpectedToken, start,
                $"JSON string contains invalid UTF-8 at byte offset {start}.");
            return false;
        }
        if (start > Source.Length - length)
        {
            SetFailure(ParseErrorKind.UnexpectedEnd, Source.Length,
                $"JSON UTF-8 sequence is incomplete at byte offset {Source.Length}.");
            return false;
        }

        int index = 1;
        while (index < length)
        {
            byte continuation = Source[start + index];
            if ((continuation & 0xc0) != 0x80)
            {
                SetFailure(ParseErrorKind.UnexpectedToken, start + index,
                    $"JSON string contains invalid UTF-8 at byte offset {start + index}.");
                return false;
            }
            value = value * 64u + (uint)(continuation & 0x3f);
            index++;
        }

        if ((length == 3 && value < 0x800u) || (length == 4 && value < 0x10000u) ||
            (value >= 0xd800u && value <= 0xdfffu) || value > 0x10ffffu)
        {
            SetFailure(ParseErrorKind.UnexpectedToken, start,
                $"JSON string contains invalid UTF-8 at byte offset {start}.");
            return false;
        }
        Position += length;
        return true;
    }

    internal bool ParseLiteral(string literal)
    {
        int start = Position;
        int index = 0;
        while (index < literal.Length)
        {
            if (Position >= Source.Length)
            {
                SetFailure(ParseErrorKind.UnexpectedEnd, Position,
                    $"JSON literal is incomplete at byte offset {Position}.");
                return false;
            }
            if (Source[Position] != (byte)literal[index])
            {
                SetFailure(ParseErrorKind.UnexpectedToken, Position,
                    $"JSON literal is invalid at byte offset {Position}.");
                return false;
            }
            Position++;
            index++;
        }
        if (Position < Source.Length && !IsValueTerminator(Source[Position]))
        {
            SetFailure(ParseErrorKind.UnexpectedToken, start,
                $"JSON literal is invalid at byte offset {start}.");
            return false;
        }
        return true;
    }

    internal bool ParseNumber()
    {
        int start = Position;
        if (Source[Position] == (byte)'-')
        {
            Position++;
            if (Position >= Source.Length)
            {
                SetFailure(ParseErrorKind.UnexpectedEnd, Position,
                    $"JSON number is incomplete at byte offset {Position}.");
                return false;
            }
        }

        if (Source[Position] == (byte)'0')
        {
            Position++;
            if (Position < Source.Length && IsDigit(Source[Position]))
            {
                SetFailure(ParseErrorKind.InvalidNumber, Position,
                    $"JSON number has a leading zero at byte offset {Position}.");
                return false;
            }
        }
        else
        {
            if (!IsNonzeroDigit(Source[Position]))
            {
                SetFailure(ParseErrorKind.InvalidNumber, Position,
                    $"JSON number requires an integer digit at byte offset {Position}.");
                return false;
            }
            while (Position < Source.Length && IsDigit(Source[Position])) Position++;
        }

        if (Position < Source.Length && Source[Position] == (byte)'.')
        {
            Position++;
            if (Position >= Source.Length || !IsDigit(Source[Position]))
            {
                SetFailure(ParseErrorKind.InvalidNumber, Position,
                    $"JSON fraction requires a digit at byte offset {Position}.");
                return false;
            }
            while (Position < Source.Length && IsDigit(Source[Position])) Position++;
        }
        if (Position < Source.Length && (Source[Position] == (byte)'e' || Source[Position] == (byte)'E'))
        {
            Position++;
            if (Position < Source.Length &&
                (Source[Position] == (byte)'+' || Source[Position] == (byte)'-')) Position++;
            if (Position >= Source.Length || !IsDigit(Source[Position]))
            {
                SetFailure(ParseErrorKind.InvalidNumber, Position,
                    $"JSON exponent requires a digit at byte offset {Position}.");
                return false;
            }
            while (Position < Source.Length && IsDigit(Source[Position])) Position++;
        }
        if (Position < Source.Length && !IsValueTerminator(Source[Position]))
        {
            SetFailure(ParseErrorKind.InvalidNumber, start,
                $"JSON number is invalid at byte offset {start}.");
            return false;
        }
        return true;
    }

    internal bool CheckDepth(int depth, int offset)
    {
        if (depth <= MaximumDepth) return true;
        SetFailure(ParseErrorKind.LimitExceeded, offset,
            $"Maximum JSON nesting depth of {MaximumDepth} was exceeded at byte offset {offset}.");
        return false;
    }

    internal bool RequireByte(byte expected, string description)
    {
        if (Position >= Source.Length)
        {
            SetFailure(ParseErrorKind.UnexpectedEnd, Position,
                $"JSON input ended before {description} at byte offset {Position}.");
            return false;
        }
        if (Source[Position] != expected)
        {
            SetFailure(ParseErrorKind.UnexpectedToken, Position,
                $"JSON input requires {description} at byte offset {Position}.");
            return false;
        }
        Position++;
        return true;
    }

    internal bool ConsumeByte(byte expected)
    {
        if (Position >= Source.Length || Source[Position] != expected) return false;
        Position++;
        return true;
    }

    internal void SkipWhitespace()
    {
        while (Position < Source.Length)
        {
            byte current = Source[Position];
            if (current != 0x20 && current != 0x09 && current != 0x0a && current != 0x0d) return;
            Position++;
        }
    }

    internal void SetFailure(ParseErrorKind kind, int offset, string message)
    {
        if (Failure != null) return;
        ParseError error = new ParseError();
        error.Kind = kind;
        error.Offset = offset;
        error.JsonPath = "$";
        error.Message = message + " JSON path $.";
        Failure = error;
    }

    internal static bool IsDigit(byte value)
    {
        return value >= (byte)'0' && value <= (byte)'9';
    }

    internal static bool IsNonzeroDigit(byte value)
    {
        return value >= (byte)'1' && value <= (byte)'9';
    }

    internal static bool IsValueTerminator(byte value)
    {
        return value == (byte)',' || value == (byte)']' || value == (byte)'}' ||
            value == 0x20 || value == 0x09 || value == 0x0a || value == 0x0d;
    }

    internal static int GetHexDigit(byte value)
    {
        if (value >= (byte)'0' && value <= (byte)'9') return value - (byte)'0';
        if (value >= (byte)'A' && value <= (byte)'F') return value - (byte)'A' + 10;
        if (value >= (byte)'a' && value <= (byte)'f') return value - (byte)'a' + 10;
        return -1;
    }
}
