namespace svg;

/// <summary>Defines the supported invariant number formatting modes.</summary>
internal enum SvgNumberFormatMode
{
    /// <summary>Uses the runtime's shortest round-trip digits with normalized exponent text.</summary>
    Shortest = 0,

    /// <summary>Uses stable plain-decimal and scientific thresholds.</summary>
    Canonical = 1,

    /// <summary>Uses an exact configured count of fractional digits.</summary>
    Fixed = 2,

    /// <summary>Uses the shortest valid plain or exponent SVG token.</summary>
    Compact = 3
}

/// <summary>Defines stable failures produced by number formatting.</summary>
internal enum SvgNumberFormatError
{
    /// <summary>No formatting failure occurred.</summary>
    None = 0,

    /// <summary>The input value was NaN or infinity.</summary>
    NonFinite = 1,

    /// <summary>The requested fixed precision was outside the supported range.</summary>
    InvalidPrecision = 2,

    /// <summary>The requested formatting mode was not recognized.</summary>
    InvalidMode = 3
}

/// <summary>Contains a non-throwing invariant number formatting result.</summary>
internal class SvgNumberFormatResult
{
    /// <summary>Indicates whether formatting succeeded.</summary>
    public bool Success;

    /// <summary>The failure kind.</summary>
    public SvgNumberFormatError Error;

    /// <summary>The formatted number, or an empty string after failure.</summary>
    public string Text;

    /// <summary>Creates a number formatting result.</summary>
    /// <param name="success">Whether formatting succeeded.</param>
    /// <param name="error">The failure kind.</param>
    /// <param name="text">The formatted number, or an empty string.</param>
    public SvgNumberFormatResult(bool success, SvgNumberFormatError error, string text)
    {
        Success = success;
        Error = error;
        Text = text;
    }
}

/// <summary>Formats finite SVG and CSS numbers deterministically and without current-culture data.</summary>
internal static class SvgNumberFormatter
{
    /// <summary>The largest supported fixed fractional precision.</summary>
    public const int MaximumFixedPrecision = 99;

    /// <summary>Formats a value using the requested mode.</summary>
    /// <param name="value">The finite value to format.</param>
    /// <param name="mode">The formatting mode.</param>
    /// <param name="precision">The fractional precision used by fixed mode.</param>
    /// <returns>The formatted number or a stable failure.</returns>
    public static SvgNumberFormatResult Format(double value, SvgNumberFormatMode mode, int precision)
    {
        switch (mode)
        {
            case SvgNumberFormatMode.Shortest:
                return FormatShortest(value);
            case SvgNumberFormatMode.Canonical:
                return FormatCanonical(value);
            case SvgNumberFormatMode.Fixed:
                return FormatFixed(value, precision);
            case SvgNumberFormatMode.Compact:
                return FormatCompact(value);
            default:
                return new SvgNumberFormatResult(false, SvgNumberFormatError.InvalidMode, "");
        }
    }

    /// <summary>Formats the shortest runtime round-trip decimal digits with a normalized exponent.</summary>
    /// <param name="value">The finite value to format.</param>
    /// <returns>The shortest round-trip result.</returns>
    public static SvgNumberFormatResult FormatShortest(double value)
    {
        if (!SvgFiniteMath.IsFinite(value))
        {
            return NonFiniteFailure();
        }
        if (value == 0)
        {
            return Success("0");
        }
        string text = value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        return Success(NormalizeExponent(text));
    }

    /// <summary>Formats a canonical decimal using plain notation from exponent minus six through twenty.</summary>
    /// <remarks>Values outside that range use one leading significant digit and a lowercase exponent without a plus sign or leading zeros.</remarks>
    /// <param name="value">The finite value to format.</param>
    /// <returns>The canonical result.</returns>
    public static SvgNumberFormatResult FormatCanonical(double value)
    {
        SvgNumberFormatResult shortest = FormatShortest(value);
        if (!shortest.Success || shortest.Text == "0")
        {
            return shortest;
        }
        string plain = ExpandExponent(shortest.Text);
        string scientific = CreateScientific(plain);
        int exponent = ReadExponent(scientific);
        if (exponent >= -6 && exponent <= 20)
        {
            return Success(plain);
        }
        return Success(scientific);
    }

    /// <summary>Formats a finite value with an exact count of fractional digits.</summary>
    /// <param name="value">The finite value to format.</param>
    /// <param name="fractionalDigits">The fractional digit count from zero through the supported maximum.</param>
    /// <returns>The fixed-precision result.</returns>
    public static SvgNumberFormatResult FormatFixed(double value, int fractionalDigits)
    {
        if (fractionalDigits < 0 || fractionalDigits > MaximumFixedPrecision)
        {
            return new SvgNumberFormatResult(false, SvgNumberFormatError.InvalidPrecision, "");
        }
        if (!SvgFiniteMath.IsFinite(value))
        {
            return NonFiniteFailure();
        }
        if (value == 0)
        {
            value = 0;
        }
        string precision = fractionalDigits.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string text = value.ToString("F" + precision, System.Globalization.CultureInfo.InvariantCulture);
        return Success(NormalizeFormattedZero(text));
    }

    /// <summary>Formats to a maximum fractional precision and removes insignificant fractional zeros.</summary>
    /// <param name="value">The finite value to format.</param>
    /// <param name="fractionalDigits">The maximum fractional digit count.</param>
    /// <returns>The trimmed fixed-precision result.</returns>
    public static SvgNumberFormatResult FormatFixedTrimmed(double value, int fractionalDigits)
    {
        SvgNumberFormatResult fixedResult = FormatFixed(value, fractionalDigits);
        if (!fixedResult.Success)
        {
            return fixedResult;
        }
        return Success(TrimFractionZeros(fixedResult.Text));
    }

    /// <summary>Formats the shortest valid plain or exponent SVG number token.</summary>
    /// <param name="value">The finite value to format.</param>
    /// <returns>The compact result.</returns>
    public static SvgNumberFormatResult FormatCompact(double value)
    {
        SvgNumberFormatResult shortest = FormatShortest(value);
        if (!shortest.Success || shortest.Text == "0")
        {
            return shortest;
        }
        string plain = ExpandExponent(shortest.Text);
        string compactPlain = RemoveFractionLeadingZero(plain);
        string compactScientific = CreateCompactScientific(plain);
        if (compactScientific.Length < compactPlain.Length)
        {
            return Success(compactScientific);
        }
        return Success(compactPlain);
    }

    static string NormalizeExponent(string text)
    {
        int marker = text.IndexOf('E');
        if (marker < 0)
        {
            marker = text.IndexOf('e');
        }
        if (marker < 0)
        {
            return text;
        }
        string mantissa = text.Substring(0, marker);
        int exponent = int.Parse(text.Substring(marker + 1), System.Globalization.NumberStyles.AllowLeadingSign, System.Globalization.CultureInfo.InvariantCulture);
        if (exponent == 0)
        {
            return mantissa;
        }
        return mantissa + "e" + exponent.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    static string ExpandExponent(string text)
    {
        int marker = text.IndexOf('e');
        if (marker < 0)
        {
            return text;
        }

        string mantissa = text.Substring(0, marker);
        int exponent = int.Parse(text.Substring(marker + 1), System.Globalization.NumberStyles.AllowLeadingSign, System.Globalization.CultureInfo.InvariantCulture);
        bool negative = mantissa.Length > 0 && mantissa[0] == '-';
        int signCharacters = negative ? 1 : 0;
        int point = mantissa.IndexOf('.');
        int decimalPosition = point < 0 ? mantissa.Length - signCharacters : point - signCharacters;
        string unsigned = negative ? mantissa.Substring(1) : mantissa;
        string digits = unsigned.Replace(".", "");
        decimalPosition = decimalPosition + exponent;

        string expanded;
        if (decimalPosition <= 0)
        {
            expanded = "0." + new string('0', -decimalPosition) + digits;
        }
        else if (decimalPosition >= digits.Length)
        {
            expanded = digits + new string('0', decimalPosition - digits.Length);
        }
        else
        {
            expanded = digits.Insert(decimalPosition, ".");
        }
        return negative ? "-" + expanded : expanded;
    }

    static string CreateScientific(string plain)
    {
        bool negative = plain.Length > 0 && plain[0] == '-';
        string unsigned = negative ? plain.Substring(1) : plain;
        int point = unsigned.IndexOf('.');
        int decimalPosition = point < 0 ? unsigned.Length : point;
        string digits = unsigned.Replace(".", "");
        int first = FirstNonzero(digits);
        if (first < 0)
        {
            return "0";
        }
        int last = LastNonzero(digits);
        string significant = digits.Substring(first, last - first + 1);
        int exponent = decimalPosition - first - 1;
        string coefficient;
        if (significant.Length == 1)
        {
            coefficient = significant;
        }
        else
        {
            coefficient = significant.Substring(0, 1) + "." + significant.Substring(1);
        }
        if (negative)
        {
            coefficient = "-" + coefficient;
        }
        if (exponent == 0)
        {
            return coefficient;
        }
        return coefficient + "e" + exponent.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    static string CreateCompactScientific(string plain)
    {
        bool negative = plain.Length > 0 && plain[0] == '-';
        string unsigned = negative ? plain.Substring(1) : plain;
        int point = unsigned.IndexOf('.');
        int decimalPosition = point < 0 ? unsigned.Length : point;
        string digits = unsigned.Replace(".", "");
        int first = FirstNonzero(digits);
        if (first < 0)
        {
            return "0";
        }
        int last = LastNonzero(digits);
        string significant = digits.Substring(first, last - first + 1);
        int scientificExponent = decimalPosition - first - 1;
        string best = "";
        int coefficientDigits = 1;
        while (coefficientDigits <= significant.Length)
        {
            string coefficient;
            if (coefficientDigits == significant.Length)
            {
                coefficient = significant;
            }
            else
            {
                coefficient = significant.Substring(0, coefficientDigits) + "." + significant.Substring(coefficientDigits);
            }
            if (negative)
            {
                coefficient = "-" + coefficient;
            }
            int exponent = scientificExponent - coefficientDigits + 1;
            string candidate = exponent == 0 ? coefficient : coefficient + "e" + exponent.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (best.Length == 0 || candidate.Length < best.Length)
            {
                best = candidate;
            }
            coefficientDigits = coefficientDigits + 1;
        }
        return best;
    }

    static int ReadExponent(string scientific)
    {
        int marker = scientific.IndexOf('e');
        if (marker < 0)
        {
            return 0;
        }
        return int.Parse(scientific.Substring(marker + 1), System.Globalization.NumberStyles.AllowLeadingSign, System.Globalization.CultureInfo.InvariantCulture);
    }

    static int FirstNonzero(string digits)
    {
        int index = 0;
        while (index < digits.Length)
        {
            if (digits[index] != '0')
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    static int LastNonzero(string digits)
    {
        int index = digits.Length - 1;
        while (index >= 0)
        {
            if (digits[index] != '0')
            {
                return index;
            }
            index = index - 1;
        }
        return -1;
    }

    static string RemoveFractionLeadingZero(string text)
    {
        if (text.Length >= 2 && text[0] == '0' && text[1] == '.')
        {
            return text.Substring(1);
        }
        if (text.Length >= 3 && text[0] == '-' && text[1] == '0' && text[2] == '.')
        {
            return "-" + text.Substring(2);
        }
        return text;
    }

    static string NormalizeFormattedZero(string text)
    {
        if (text.Length == 0 || text[0] != '-')
        {
            return text;
        }
        int index = 1;
        while (index < text.Length)
        {
            if (text[index] != '0' && text[index] != '.')
            {
                return text;
            }
            index = index + 1;
        }
        return text.Substring(1);
    }

    static string TrimFractionZeros(string text)
    {
        int point = text.IndexOf('.');
        if (point < 0)
        {
            return text;
        }
        int end = text.Length;
        while (end > point + 1 && text[end - 1] == '0')
        {
            end = end - 1;
        }
        if (end == point + 1)
        {
            end = point;
        }
        return text.Substring(0, end);
    }

    static SvgNumberFormatResult Success(string text)
    {
        return new SvgNumberFormatResult(true, SvgNumberFormatError.None, text);
    }

    static SvgNumberFormatResult NonFiniteFailure()
    {
        return new SvgNumberFormatResult(false, SvgNumberFormatError.NonFinite, "");
    }
}
