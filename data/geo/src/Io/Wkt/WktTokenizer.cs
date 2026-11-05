using System.Globalization;

namespace geo;

internal enum WktTokenKind
{
    Invalid = 0,
    End = 1,
    Keyword = 2,
    Number = 3,
    LeftParenthesis = 4,
    RightParenthesis = 5,
    Comma = 6,
    Semicolon = 7,
    Equals = 8
}

internal class WktToken
{
    internal WktTokenKind Kind;
    internal string Text = "";
    internal double Number;
    internal int Offset;
    internal int Length;
    internal ParseError? Error;
}

internal class WktTokenizer
{
    internal const int MaximumKeywordLength = 64;
    internal const int MaximumNumberLength = 128;

    internal string Source;
    internal int Position;
    internal int Depth;
    internal int MaximumDepth;
    internal ParseError? Failure;

    internal WktTokenizer(string source, WktReadOptions options)
    {
        Source = source;
        if (source == null)
        {
            Source = "";
            Failure = CreateParseError(ParseErrorKind.UnexpectedEnd, 0, "WKT input is null at character offset 0.");
        }

        if (options == null)
        {
            Failure = CreateParseError(ParseErrorKind.LimitExceeded, 0, "WKT read options are null at character offset 0.");
            return;
        }

        MaximumDepth = options.MaximumNestingDepth;
        if (options.MaximumCoordinates < 1)
        {
            Failure = CreateParseError(ParseErrorKind.LimitExceeded, 0, "Maximum WKT coordinate count must be positive at character offset 0.");
            return;
        }
        if (options.MaximumComponents < 1)
        {
            Failure = CreateParseError(ParseErrorKind.LimitExceeded, 0, "Maximum WKT component count must be positive at character offset 0.");
            return;
        }
        if (MaximumDepth < 0)
        {
            Failure = CreateParseError(ParseErrorKind.LimitExceeded, 0, "Maximum WKT nesting depth cannot be negative at character offset 0.");
        }
    }

    internal WktToken ReadToken()
    {
        if (Failure != null)
        {
            return CreateFailureToken();
        }

        SkipWhitespace();
        if (Position >= Source.Length)
        {
            if (Depth != 0)
            {
                SetFailure(ParseErrorKind.UnexpectedEnd, Position, $"WKT input ended before all parentheses were closed at character offset {Position}.");
                return CreateFailureToken();
            }

            return CreateEndToken();
        }

        char current = Source[Position];
        if (IsAsciiLetter(current))
        {
            return ReadKeywordToken();
        }
        if (IsNumberStart(current))
        {
            return ReadNumberToken();
        }
        if (current == '(')
        {
            int offset = Position;
            Position++;
            if (Depth >= MaximumDepth)
            {
                SetFailure(ParseErrorKind.LimitExceeded, offset, $"Maximum WKT nesting depth of {MaximumDepth} was exceeded at character offset {offset}.");
                return CreateFailureToken();
            }
            Depth++;
            return CreateSimpleToken(WktTokenKind.LeftParenthesis, offset);
        }
        if (current == ')')
        {
            int offset = Position;
            Position++;
            if (Depth == 0)
            {
                SetFailure(ParseErrorKind.UnexpectedToken, offset, $"Unexpected closing parenthesis at character offset {offset}.");
                return CreateFailureToken();
            }
            Depth--;
            return CreateSimpleToken(WktTokenKind.RightParenthesis, offset);
        }
        if (current == ',')
        {
            int offset = Position;
            Position++;
            return CreateSimpleToken(WktTokenKind.Comma, offset);
        }
        if (current == ';')
        {
            int offset = Position;
            Position++;
            return CreateSimpleToken(WktTokenKind.Semicolon, offset);
        }
        if (current == '=')
        {
            int offset = Position;
            Position++;
            return CreateSimpleToken(WktTokenKind.Equals, offset);
        }

        int unexpectedOffset = Position;
        Position++;
        SetFailure(ParseErrorKind.UnexpectedToken, unexpectedOffset, $"Unexpected character '{current}' at character offset {unexpectedOffset}.");
        return CreateFailureToken();
    }

    internal WktToken ReadKeywordToken()
    {
        int start = Position;
        while (Position < Source.Length && IsAsciiLetter(Source[Position]))
        {
            Position++;
            if (Position - start > MaximumKeywordLength)
            {
                int offset = start + MaximumKeywordLength;
                SetFailure(ParseErrorKind.LimitExceeded, offset, $"WKT keyword exceeds {MaximumKeywordLength} characters at character offset {offset}.");
                return CreateFailureToken();
            }
        }

        string text = Source.Substring(start, Position - start).ToUpperInvariant();
        WktToken token = new WktToken();
        token.Kind = WktTokenKind.Keyword;
        token.Text = text;
        token.Offset = start;
        token.Length = Position - start;
        return token;
    }

    internal WktToken ReadNumberToken()
    {
        int start = Position;
        if (Source[Position] == '+' || Source[Position] == '-')
        {
            Position++;
        }

        int integerDigits = 0;
        while (Position < Source.Length && IsAsciiDigit(Source[Position]))
        {
            Position++;
            integerDigits++;
            if (CheckNumberLengthLimit(start)) return CreateFailureToken();
        }

        int fractionalDigits = 0;
        if (Position < Source.Length && Source[Position] == '.')
        {
            Position++;
            if (CheckNumberLengthLimit(start)) return CreateFailureToken();
            while (Position < Source.Length && IsAsciiDigit(Source[Position]))
            {
                Position++;
                fractionalDigits++;
                if (CheckNumberLengthLimit(start)) return CreateFailureToken();
            }
        }

        if (integerDigits == 0 && fractionalDigits == 0)
        {
            SetFailure(ParseErrorKind.InvalidNumber, start, $"Numeric token requires at least one digit at character offset {start}.");
            return CreateFailureToken();
        }

        if (Position < Source.Length && (Source[Position] == 'e' || Source[Position] == 'E'))
        {
            Position++;
            if (CheckNumberLengthLimit(start)) return CreateFailureToken();
            if (Position < Source.Length && (Source[Position] == '+' || Source[Position] == '-'))
            {
                Position++;
                if (CheckNumberLengthLimit(start)) return CreateFailureToken();
            }

            int exponentDigits = 0;
            while (Position < Source.Length && IsAsciiDigit(Source[Position]))
            {
                Position++;
                exponentDigits++;
                if (CheckNumberLengthLimit(start)) return CreateFailureToken();
            }
            if (exponentDigits == 0)
            {
                SetFailure(ParseErrorKind.InvalidNumber, Position, $"Numeric exponent requires at least one digit at character offset {Position}.");
                return CreateFailureToken();
            }
        }

        if (Position < Source.Length && !IsTokenSeparator(Source[Position]))
        {
            int offset = Position;
            SetFailure(ParseErrorKind.InvalidNumber, offset, $"Unexpected character in numeric token at character offset {offset}.");
            return CreateFailureToken();
        }

        string text = Source.Substring(start, Position - start);
        double value = double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);
        if (!double.IsFinite(value))
        {
            SetFailure(ParseErrorKind.InvalidNumber, start, $"Numeric token is outside the finite double range at character offset {start}.");
            return CreateFailureToken();
        }

        WktToken token = new WktToken();
        token.Kind = WktTokenKind.Number;
        token.Text = text;
        token.Number = value;
        token.Offset = start;
        token.Length = Position - start;
        return token;
    }

    internal bool CheckNumberLengthLimit(int start)
    {
        if (Position - start <= MaximumNumberLength)
        {
            return false;
        }

        int offset = start + MaximumNumberLength;
        SetFailure(ParseErrorKind.LimitExceeded, offset, $"WKT numeric token exceeds {MaximumNumberLength} characters at character offset {offset}.");
        return true;
    }

    internal void SkipWhitespace()
    {
        while (Position < Source.Length && char.IsWhiteSpace(Source[Position]))
        {
            Position++;
        }
    }

    internal void SetFailure(ParseErrorKind kind, int offset, string message)
    {
        Failure = CreateParseError(kind, offset, message);
    }

    internal ParseError CreateParseError(ParseErrorKind kind, int offset, string message)
    {
        ParseError error = new ParseError();
        error.Kind = kind;
        error.Message = message;
        error.Offset = offset;
        return error;
    }

    internal WktToken CreateFailureToken()
    {
        WktToken token = new WktToken();
        token.Kind = WktTokenKind.Invalid;
        token.Offset = Failure == null ? Position : (int)Failure.Offset;
        token.Error = Failure;
        return token;
    }

    internal WktToken CreateSimpleToken(WktTokenKind kind, int offset)
    {
        WktToken token = new WktToken();
        token.Kind = kind;
        token.Text = Source.Substring(offset, 1);
        token.Offset = offset;
        token.Length = 1;
        return token;
    }

    internal WktToken CreateEndToken()
    {
        WktToken token = new WktToken();
        token.Kind = WktTokenKind.End;
        token.Offset = Position;
        return token;
    }

    internal static bool IsAsciiLetter(char value)
    {
        return (value >= 'A' && value <= 'Z') || (value >= 'a' && value <= 'z');
    }

    internal static bool IsAsciiDigit(char value)
    {
        return value >= '0' && value <= '9';
    }

    internal static bool IsNumberStart(char value)
    {
        return IsAsciiDigit(value) || value == '+' || value == '-' || value == '.';
    }

    internal static bool IsTokenSeparator(char value)
    {
        return char.IsWhiteSpace(value) || value == '(' || value == ')' || value == ',' || value == ';' || value == '=';
    }
}
