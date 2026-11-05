using System;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace token;

/// <summary>
/// Converts JWT NumericDate values to and from UTC instants.
/// </summary>
/// <remarks>
/// NumericDate counts SI seconds from the Unix epoch and may contain a fractional part.
/// Conversion retains fractions at the 100-nanosecond resolution of <see cref="DateTimeOffset"/>.
/// </remarks>
internal static class JwtNumericDate
{
    const long TicksPerSecond = 10000000;
    const long MaximumPositiveTicks = 2534023007999999999;
    const long MaximumNegativeTicks = 621355968000000000;

    /// <summary>
    /// Converts strict JSON number text to a UTC instant.
    /// </summary>
    /// <param name="numberText">The NumericDate JSON number text.</param>
    /// <returns>A result containing the UTC instant or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>numberText</c> is null.</exception>
    internal static JwtNumericDateResult ToDateTimeOffset(string numberText)
    {
        if (numberText == null)
        {
            throw new ArgumentNullException("numberText");
        }

        JsonReadResult parsed = JsonReader.Read(numberText, 1);
        if (!parsed.Success)
        {
            return Failure(
                "invalid_numeric_date",
                "A JWT NumericDate must be a strict JSON number.");
        }

        string raw = (parsed.Document ?? throw new InvalidOperationException("A required value is null.")).RootElement.GetRawText();
        bool number = parsed.Document.RootElement.ValueKind == JsonValueKind.Number
            && string.Equals(raw, numberText, StringComparison.Ordinal);
        parsed.Document.Dispose();
        if (!number)
        {
            return Failure(
                "invalid_numeric_date",
                "A JWT NumericDate must be a strict JSON number.");
        }

        JwtNumericDateTickResult ticks = ReadTicks(numberText);
        if (!ticks.Success)
        {
            return Failure(
                "numeric_date_out_of_range",
                "The JWT NumericDate is outside the supported date range.");
        }

        return Success(numberText, ticks.Ticks);
    }

    /// <summary>
    /// Converts a JSON number value to a UTC instant.
    /// </summary>
    /// <param name="value">The NumericDate JSON value.</param>
    /// <returns>A result containing the UTC instant or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    internal static JwtNumericDateResult ToDateTimeOffset(JsonValue value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (value.Kind != TokenJsonValueKind.Number || value.NumberText == null)
        {
            return Failure(
                "invalid_numeric_date",
                "A JWT NumericDate must be a JSON number.");
        }

        return ToDateTimeOffset(value.NumberText);
    }

    /// <summary>
    /// Converts seconds since the Unix epoch to a UTC instant.
    /// </summary>
    /// <param name="seconds">The NumericDate value in seconds.</param>
    /// <returns>A result containing the UTC instant or stable failure details.</returns>
    internal static JwtNumericDateResult ToDateTimeOffset(decimal seconds)
    {
        string numberText = seconds.ToString("0.############################", CultureInfo.InvariantCulture);
        return ToDateTimeOffset(numberText);
    }

    /// <summary>
    /// Converts an instant to a canonical JWT NumericDate.
    /// </summary>
    /// <param name="value">The instant to convert.</param>
    /// <returns>A successful result containing canonical JSON number text.</returns>
    internal static JwtNumericDateResult FromDateTimeOffset(DateTimeOffset value)
    {
        DateTimeOffset utc = value.ToUniversalTime();
        long ticks = utc.Ticks - DateTimeOffset.UnixEpoch.Ticks;
        string numberText = FormatTicks(ticks);
        return Success(numberText, ticks);
    }

    /// <summary>
    /// Converts the current instant from a clock to a canonical JWT NumericDate.
    /// </summary>
    /// <param name="clock">The clock that supplies the current instant.</param>
    /// <returns>A successful result containing canonical JSON number text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>clock</c> is null.</exception>
    internal static JwtNumericDateResult FromClock(ITokenClock clock)
    {
        if (clock == null)
        {
            throw new ArgumentNullException("clock");
        }

        return FromDateTimeOffset(clock.GetUtcNow());
    }

    /// <summary>
    /// Creates a JSON number value for an instant.
    /// </summary>
    /// <param name="value">The instant to convert.</param>
    /// <returns>A JSON number containing the canonical NumericDate text.</returns>
    internal static JsonValue ToJsonValue(DateTimeOffset value)
    {
        JwtNumericDateResult converted = FromDateTimeOffset(value);
        JsonValue result = new JsonValue();
        result.Kind = TokenJsonValueKind.Number;
        result.NumberText = converted.NumberText;
        return result;
    }

    static JwtNumericDateResult Success(string numberText, long ticks)
    {
        JwtNumericDateResult result = new JwtNumericDateResult();
        result.DateTime = DateTimeOffset.UnixEpoch.AddTicks(ticks);
        result.Seconds = (decimal)ticks / TicksPerSecond;
        result.NumberText = numberText;
        result.Success = true;
        return result;
    }

    static JwtNumericDateResult Failure(string code, string message)
    {
        JwtNumericDateResult result = new JwtNumericDateResult();
        result.Error = new TokenError(code, message, -1);
        return result;
    }

    static string FormatTicks(long ticks)
    {
        decimal seconds = (decimal)ticks / TicksPerSecond;
        return seconds.ToString("0.#######", CultureInfo.InvariantCulture);
    }

    static JwtNumericDateTickResult ReadTicks(string numberText)
    {
        bool negative = numberText[0] == '-';
        int position = negative ? 1 : 0;
        int exponentPosition = FindExponent(numberText, position);
        int significandEnd = exponentPosition < 0
            ? numberText.Length
            : exponentPosition;
        StringBuilder digits = new StringBuilder(significandEnd - position);
        int fractionDigits = 0;
        bool fraction = false;
        while (position < significandEnd)
        {
            char character = numberText[position];
            if (character == '.')
            {
                fraction = true;
            }
            else
            {
                digits.Append(character);
                if (fraction)
                {
                    fractionDigits += 1;
                }
            }

            position += 1;
        }

        int exponent = ReadExponent(numberText, exponentPosition);
        string significant = TrimLeadingZeroes(digits.ToString());
        if (significant.Length == 0)
        {
            return TickSuccess(0);
        }

        int scale = exponent - fractionDigits + 7;
        int retainedDigits = significant.Length + scale;
        if (retainedDigits <= 0)
        {
            return TickSuccess(0);
        }

        string limit = negative
            ? MaximumNegativeTicks.ToString(CultureInfo.InvariantCulture)
            : MaximumPositiveTicks.ToString(CultureInfo.InvariantCulture);
        int comparison = CompareMagnitude(
            significant,
            retainedDigits,
            limit);
        if (comparison > 0
            || comparison == 0 && HasDiscardedNonzeroDigits(
                significant,
                retainedDigits))
        {
            return TickFailure();
        }

        long magnitude = 0;
        int index = 0;
        while (index < retainedDigits)
        {
            int digit = index < significant.Length
                ? significant[index] - '0'
                : 0;
            magnitude = magnitude * 10 + digit;
            index += 1;
        }

        return TickSuccess(negative ? -magnitude : magnitude);
    }

    static int FindExponent(string numberText, int start)
    {
        int position = start;
        while (position < numberText.Length)
        {
            char character = numberText[position];
            if (character == 'e' || character == 'E')
            {
                return position;
            }

            position += 1;
        }

        return -1;
    }

    static int ReadExponent(string numberText, int exponentPosition)
    {
        if (exponentPosition < 0)
        {
            return 0;
        }

        int position = exponentPosition + 1;
        bool negative = false;
        if (numberText[position] == '+' || numberText[position] == '-')
        {
            negative = numberText[position] == '-';
            position += 1;
        }

        int exponent = 0;
        while (position < numberText.Length && exponent < 100000)
        {
            exponent = exponent * 10 + numberText[position] - '0';
            position += 1;
        }

        if (exponent > 100000)
        {
            exponent = 100000;
        }

        return negative ? -exponent : exponent;
    }

    static string TrimLeadingZeroes(string digits)
    {
        int position = 0;
        while (position < digits.Length && digits[position] == '0')
        {
            position += 1;
        }

        return position == digits.Length ? string.Empty : digits.Substring(position);
    }

    static int CompareMagnitude(
        string significant,
        int retainedDigits,
        string limit)
    {
        if (retainedDigits < limit.Length)
        {
            return -1;
        }

        if (retainedDigits > limit.Length)
        {
            return 1;
        }

        int position = 0;
        while (position < retainedDigits)
        {
            char digit = position < significant.Length
                ? significant[position]
                : '0';
            if (digit < limit[position])
            {
                return -1;
            }

            if (digit > limit[position])
            {
                return 1;
            }

            position += 1;
        }

        return 0;
    }

    static bool HasDiscardedNonzeroDigits(
        string significant,
        int retainedDigits)
    {
        int position = retainedDigits;
        while (position < significant.Length)
        {
            if (significant[position] != '0')
            {
                return true;
            }

            position += 1;
        }

        return false;
    }

    static JwtNumericDateTickResult TickSuccess(long ticks)
    {
        JwtNumericDateTickResult result = new JwtNumericDateTickResult();
        result.Success = true;
        result.Ticks = ticks;
        return result;
    }

    static JwtNumericDateTickResult TickFailure()
    {
        return new JwtNumericDateTickResult();
    }
}
