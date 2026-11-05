namespace svg;

/// <summary>Defines stable failures produced by number and integer parsing.</summary>
internal enum SvgNumberParseError
{
    /// <summary>No parse failure occurred.</summary>
    None = 0,

    /// <summary>A valid SVG or CSS number was not present.</summary>
    ExpectedNumber = 1,

    /// <summary>A valid decimal integer was not present.</summary>
    ExpectedInteger = 2,

    /// <summary>An SVG exponent marker was not followed by exponent digits.</summary>
    InvalidExponent = 3,

    /// <summary>The numeric token exceeded the reader's configured bound.</summary>
    TokenTooLong = 4,

    /// <summary>The numeric value exceeded its finite or integer range.</summary>
    Overflow = 5,

    /// <summary>Non-whitespace characters followed a complete standalone value.</summary>
    TrailingCharacters = 6,

    /// <summary>An internal parser argument or reader configuration was invalid.</summary>
    InvalidArgument = 7
}

/// <summary>Contains a non-throwing finite number parse result.</summary>
internal class SvgNumberParseResult
{
    /// <summary>Indicates whether parsing succeeded.</summary>
    public bool Success;

    /// <summary>The failure kind.</summary>
    public SvgNumberParseError Error;

    /// <summary>The parsed finite value, or zero after failure.</summary>
    public double Value;

    /// <summary>The starting character offset.</summary>
    public int Start;

    /// <summary>The next character offset after success, or the original offset after failure.</summary>
    public int NextOffset;

    /// <summary>The exact failure offset, or <c>-1</c> after success.</summary>
    public int ErrorOffset;

    /// <summary>Creates a finite number parse result.</summary>
    /// <param name="success">Whether parsing succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="value">The parsed finite value, or zero.</param>
    /// <param name="start">The starting offset.</param>
    /// <param name="nextOffset">The next offset after success or original offset after failure.</param>
    /// <param name="errorOffset">The exact failure offset, or <c>-1</c>.</param>
    public SvgNumberParseResult(bool success, SvgNumberParseError error, double value, int start, int nextOffset, int errorOffset)
    {
        Success = success;
        Error = error;
        Value = value;
        Start = start;
        NextOffset = nextOffset;
        ErrorOffset = errorOffset;
    }
}

/// <summary>Contains a non-throwing signed 32-bit integer parse result.</summary>
internal class SvgIntegerParseResult
{
    /// <summary>Indicates whether parsing succeeded.</summary>
    public bool Success;

    /// <summary>The failure kind.</summary>
    public SvgNumberParseError Error;

    /// <summary>The parsed integer, or zero after failure.</summary>
    public int Value;

    /// <summary>The starting character offset.</summary>
    public int Start;

    /// <summary>The next character offset after success, or the original offset after failure.</summary>
    public int NextOffset;

    /// <summary>The exact failure offset, or <c>-1</c> after success.</summary>
    public int ErrorOffset;

    /// <summary>Creates an integer parse result.</summary>
    /// <param name="success">Whether parsing succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="value">The parsed integer, or zero.</param>
    /// <param name="start">The starting offset.</param>
    /// <param name="nextOffset">The next offset after success or original offset after failure.</param>
    /// <param name="errorOffset">The exact failure offset, or <c>-1</c>.</param>
    public SvgIntegerParseResult(bool success, SvgNumberParseError error, int value, int start, int nextOffset, int errorOffset)
    {
        Success = success;
        Error = error;
        Value = value;
        Start = start;
        NextOffset = nextOffset;
        ErrorOffset = errorOffset;
    }
}

/// <summary>Parses culture-invariant SVG numbers, CSS numbers, and decimal integers.</summary>
internal static class SvgNumberParser
{
    /// <summary>Parses one complete SVG number with optional surrounding SVG whitespace.</summary>
    /// <param name="value">The input text.</param>
    /// <param name="maxTokenCharacters">The maximum numeric token length.</param>
    /// <returns>A finite number parse result.</returns>
    public static SvgNumberParseResult ParseNumber(string? value, int maxTokenCharacters)
    {
        if (value == null)
        {
            return new SvgNumberParseResult(false, SvgNumberParseError.InvalidArgument, 0, 0, 0, 0);
        }
        SvgSpanReader reader = new SvgSpanReader(value, maxTokenCharacters);
        reader.SkipWhitespace();
        SvgNumberParseResult result = ReadNumber(reader);
        return CompleteNumber(result, reader, false);
    }

    /// <summary>Parses one complete CSS number with optional surrounding CSS whitespace.</summary>
    /// <param name="value">The input text.</param>
    /// <param name="maxTokenCharacters">The maximum numeric token length.</param>
    /// <returns>A finite number parse result.</returns>
    public static SvgNumberParseResult ParseCssNumber(string? value, int maxTokenCharacters)
    {
        if (value == null)
        {
            return new SvgNumberParseResult(false, SvgNumberParseError.InvalidArgument, 0, 0, 0, 0);
        }
        SvgSpanReader reader = new SvgSpanReader(value, maxTokenCharacters);
        reader.SkipCssWhitespace();
        SvgNumberParseResult result = ReadCssNumber(reader);
        return CompleteNumber(result, reader, true);
    }

    /// <summary>Parses one complete signed decimal integer with optional SVG whitespace.</summary>
    /// <param name="value">The input text.</param>
    /// <param name="maxTokenCharacters">The maximum integer token length.</param>
    /// <returns>An integer parse result.</returns>
    public static SvgIntegerParseResult ParseInteger(string? value, int maxTokenCharacters)
    {
        if (value == null)
        {
            return new SvgIntegerParseResult(false, SvgNumberParseError.InvalidArgument, 0, 0, 0, 0);
        }
        SvgSpanReader reader = new SvgSpanReader(value, maxTokenCharacters);
        reader.SkipWhitespace();
        SvgIntegerParseResult result = ReadInteger(reader);
        if (!result.Success)
        {
            return result;
        }
        reader.SkipWhitespace();
        if (!reader.IsEnd())
        {
            return new SvgIntegerParseResult(false, SvgNumberParseError.TrailingCharacters, 0, result.Start, result.Start, reader.GetPosition());
        }
        return new SvgIntegerParseResult(true, SvgNumberParseError.None, result.Value, result.Start, reader.GetPosition(), -1);
    }

    /// <summary>Reads one SVG number at the current reader position.</summary>
    /// <param name="reader">The bounded character reader.</param>
    /// <returns>A finite number parse result; failure restores the reader position.</returns>
    public static SvgNumberParseResult ReadNumber(SvgSpanReader reader)
    {
        return ReadNumberCore(reader, false);
    }

    /// <summary>Reads one CSS number token at the current reader position.</summary>
    /// <param name="reader">The bounded character reader.</param>
    /// <returns>A finite number parse result; failure restores the reader position.</returns>
    public static SvgNumberParseResult ReadCssNumber(SvgSpanReader reader)
    {
        return ReadNumberCore(reader, true);
    }

    /// <summary>Reads one signed decimal integer at the current reader position.</summary>
    /// <param name="reader">The bounded character reader.</param>
    /// <returns>An integer parse result; failure restores the reader position.</returns>
    public static SvgIntegerParseResult ReadInteger(SvgSpanReader? reader)
    {
        if (reader == null)
        {
            return new SvgIntegerParseResult(false, SvgNumberParseError.InvalidArgument, 0, 0, 0, 0);
        }

        int start = reader.GetPosition();
        int maximum = reader.GetMaximumTokenCharacters();
        if (maximum < 1)
        {
            return new SvgIntegerParseResult(false, SvgNumberParseError.InvalidArgument, 0, start, start, start);
        }

        System.ReadOnlySpan<char> input = reader.GetInput().Span;
        int position = start;
        bool negative = false;
        if (position < input.Length && (input[position] == '+' || input[position] == '-'))
        {
            if (position - start >= maximum)
            {
                return IntegerFailure(SvgNumberParseError.TokenTooLong, start, position);
            }
            negative = input[position] == '-';
            position = position + 1;
        }

        if (position >= input.Length || !IsDigit(input[position]))
        {
            return IntegerFailure(SvgNumberParseError.ExpectedInteger, start, position);
        }

        long maximumMagnitude = negative ? 2147483648L : 2147483647L;
        long magnitude = 0;
        while (position < input.Length && IsDigit(input[position]))
        {
            if (position - start >= maximum)
            {
                return IntegerFailure(SvgNumberParseError.TokenTooLong, start, position);
            }
            int digit = input[position] - '0';
            if (magnitude > (maximumMagnitude - digit) / 10)
            {
                return IntegerFailure(SvgNumberParseError.Overflow, start, position);
            }
            magnitude = magnitude * 10 + digit;
            position = position + 1;
        }

        int parsed;
        if (negative && magnitude == 2147483648L)
        {
            parsed = -2147483647 - 1;
        }
        else if (negative)
        {
            parsed = -(int)magnitude;
        }
        else
        {
            parsed = (int)magnitude;
        }
        reader.SetPosition(position);
        return new SvgIntegerParseResult(true, SvgNumberParseError.None, parsed, start, position, -1);
    }

    static SvgNumberParseResult ReadNumberCore(SvgSpanReader? reader, bool cssMode)
    {
        if (reader == null)
        {
            return new SvgNumberParseResult(false, SvgNumberParseError.InvalidArgument, 0, 0, 0, 0);
        }

        int start = reader.GetPosition();
        int maximum = reader.GetMaximumTokenCharacters();
        if (maximum < 1)
        {
            return NumberFailure(SvgNumberParseError.InvalidArgument, start, start);
        }

        System.ReadOnlySpan<char> input = reader.GetInput().Span;
        int position = start;
        if (position < input.Length && (input[position] == '+' || input[position] == '-'))
        {
            if (position - start >= maximum)
            {
                return NumberFailure(SvgNumberParseError.TokenTooLong, start, position);
            }
            position = position + 1;
        }

        bool hasIntegerDigits = false;
        while (position < input.Length && IsDigit(input[position]))
        {
            if (position - start >= maximum)
            {
                return NumberFailure(SvgNumberParseError.TokenTooLong, start, position);
            }
            hasIntegerDigits = true;
            position = position + 1;
        }

        bool hasFractionDigits = false;
        bool consumePoint = false;
        if (position < input.Length && input[position] == '.')
        {
            if (cssMode)
            {
                consumePoint = position + 1 < input.Length && IsDigit(input[position + 1]);
            }
            else
            {
                consumePoint = true;
            }
        }
        if (consumePoint)
        {
            if (position - start >= maximum)
            {
                return NumberFailure(SvgNumberParseError.TokenTooLong, start, position);
            }
            position = position + 1;
            while (position < input.Length && IsDigit(input[position]))
            {
                if (position - start >= maximum)
                {
                    return NumberFailure(SvgNumberParseError.TokenTooLong, start, position);
                }
                hasFractionDigits = true;
                position = position + 1;
            }
        }

        if (!hasIntegerDigits && !hasFractionDigits)
        {
            return NumberFailure(SvgNumberParseError.ExpectedNumber, start, position);
        }

        bool consumeExponent = false;
        if (position < input.Length && (input[position] == 'e' || input[position] == 'E'))
        {
            consumeExponent = !cssMode || HasCssExponent(input, position);
        }
        if (consumeExponent)
        {
            if (position - start >= maximum)
            {
                return NumberFailure(SvgNumberParseError.TokenTooLong, start, position);
            }
            position = position + 1;
            if (position < input.Length && (input[position] == '+' || input[position] == '-'))
            {
                if (position - start >= maximum)
                {
                    return NumberFailure(SvgNumberParseError.TokenTooLong, start, position);
                }
                position = position + 1;
            }
            if (position >= input.Length || !IsDigit(input[position]))
            {
                return NumberFailure(SvgNumberParseError.InvalidExponent, start, position);
            }
            while (position < input.Length && IsDigit(input[position]))
            {
                if (position - start >= maximum)
                {
                    return NumberFailure(SvgNumberParseError.TokenTooLong, start, position);
                }
                position = position + 1;
            }
        }

        double parsed = double.Parse(input.Slice(start, position - start), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);
        if (!SvgFiniteMath.IsFinite(parsed))
        {
            return NumberFailure(SvgNumberParseError.Overflow, start, position - 1);
        }
        parsed = SvgFiniteMath.NormalizeZero(parsed);
        reader.SetPosition(position);
        return new SvgNumberParseResult(true, SvgNumberParseError.None, parsed, start, position, -1);
    }

    static SvgNumberParseResult CompleteNumber(SvgNumberParseResult result, SvgSpanReader reader, bool cssMode)
    {
        if (!result.Success)
        {
            return result;
        }
        if (cssMode)
        {
            reader.SkipCssWhitespace();
        }
        else
        {
            reader.SkipWhitespace();
        }
        if (!reader.IsEnd())
        {
            return new SvgNumberParseResult(false, SvgNumberParseError.TrailingCharacters, 0, result.Start, result.Start, reader.GetPosition());
        }
        return new SvgNumberParseResult(true, SvgNumberParseError.None, result.Value, result.Start, reader.GetPosition(), -1);
    }

    static bool HasCssExponent(System.ReadOnlySpan<char> input, int position)
    {
        int next = position + 1;
        if (next < input.Length && IsDigit(input[next]))
        {
            return true;
        }
        if (next < input.Length && (input[next] == '+' || input[next] == '-'))
        {
            next = next + 1;
            return next < input.Length && IsDigit(input[next]);
        }
        return false;
    }

    static bool IsDigit(char value)
    {
        return value >= '0' && value <= '9';
    }

    static SvgNumberParseResult NumberFailure(SvgNumberParseError error, int start, int errorOffset)
    {
        return new SvgNumberParseResult(false, error, 0, start, start, errorOffset);
    }

    static SvgIntegerParseResult IntegerFailure(SvgNumberParseError error, int start, int errorOffset)
    {
        return new SvgIntegerParseResult(false, error, 0, start, start, errorOffset);
    }
}
