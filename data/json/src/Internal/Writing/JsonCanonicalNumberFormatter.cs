namespace json;

internal class JsonCanonicalNumberFormatter
{
    int maximumNumberCharacters;
    int maximumOutputBytes;
    string significantDigits = "";
    int decimalPosition;

    internal JsonCanonicalNumberFormatter(JsonLimits? limits)
    {
        JsonLimits normalized = JsonOptionNormalizer.NormalizeLimits(limits);
        maximumNumberCharacters = normalized.MaximumNumberCharacters;
        maximumOutputBytes = normalized.MaximumOutputBytes;
    }

    internal byte[] FormatDouble(double value)
    {
        if (!global::System.Double.IsFinite(value))
        {
            throw CreateWriteFailure(
                JsonDiagnosticCode.NonFiniteNumber,
                "A canonical JSON number must be finite.");
        }

        if (value == 0)
        {
            return EncodeCanonicalText("0");
        }

        bool negative = value < 0;
        double magnitude = negative ? -value : value;
        string shortest = magnitude.ToString(
            "R",
            global::System.Globalization.CultureInfo.InvariantCulture);
        ReadShortestParts(shortest);
        string unsignedText = CreateUnsignedCanonicalText();
        string canonical = negative ? "-" + unsignedText : unsignedText;
        return EncodeCanonicalText(canonical);
    }

    void ReadShortestParts(string shortest)
    {
        int exponentIndex = shortest.IndexOf('E');
        if (exponentIndex < 0)
        {
            exponentIndex = shortest.IndexOf('e');
        }

        string mantissa = exponentIndex < 0
            ? shortest
            : shortest.Substring(0, exponentIndex);
        int exponent = exponentIndex < 0
            ? 0
            : global::System.Int32.Parse(
                shortest.Substring(exponentIndex + 1),
                global::System.Globalization.NumberStyles.Integer,
                global::System.Globalization.CultureInfo.InvariantCulture);
        int pointIndex = mantissa.IndexOf('.');
        int originalPosition = pointIndex < 0
            ? mantissa.Length
            : pointIndex;
        global::System.Text.StringBuilder builder =
            new global::System.Text.StringBuilder(mantissa.Length);
        int index = 0;
        while (index < mantissa.Length)
        {
            char character = mantissa[index];
            if (character != '.')
            {
                builder.Append(character);
            }

            index++;
        }

        string digits = builder.ToString();
        int first = 0;
        while (first < digits.Length - 1 && digits[first] == '0')
        {
            first++;
        }

        decimalPosition = originalPosition + exponent - first;
        int last = digits.Length;
        while (last > first + 1 && digits[last - 1] == '0')
        {
            last--;
        }

        significantDigits = digits.Substring(first, last - first);
    }

    string CreateUnsignedCanonicalText()
    {
        int digitCount = significantDigits.Length;
        if (digitCount <= decimalPosition && decimalPosition <= 21)
        {
            global::System.Text.StringBuilder integer =
                new global::System.Text.StringBuilder(decimalPosition);
            integer.Append(significantDigits);
            AppendZeroes(integer, decimalPosition - digitCount);
            return integer.ToString();
        }

        if (decimalPosition > 0 && decimalPosition <= 21)
        {
            return significantDigits.Substring(0, decimalPosition) + "." +
                significantDigits.Substring(decimalPosition);
        }

        if (decimalPosition > -6 && decimalPosition <= 0)
        {
            global::System.Text.StringBuilder fraction =
                new global::System.Text.StringBuilder(
                    2 - decimalPosition + digitCount);
            fraction.Append("0.");
            AppendZeroes(fraction, -decimalPosition);
            fraction.Append(significantDigits);
            return fraction.ToString();
        }

        global::System.Text.StringBuilder scientific =
            new global::System.Text.StringBuilder(digitCount + 8);
        scientific.Append(significantDigits[0]);
        if (digitCount > 1)
        {
            scientific.Append('.');
            scientific.Append(significantDigits.Substring(1));
        }

        int scientificExponent = decimalPosition - 1;
        scientific.Append('e');
        if (scientificExponent >= 0)
        {
            scientific.Append('+');
        }

        scientific.Append(scientificExponent.ToString(
            global::System.Globalization.CultureInfo.InvariantCulture));
        return scientific.ToString();
    }

    void AppendZeroes(
        global::System.Text.StringBuilder builder,
        int count)
    {
        int index = 0;
        while (index < count)
        {
            builder.Append('0');
            index++;
        }
    }

    byte[] EncodeCanonicalText(string text)
    {
        if (text.Length > maximumNumberCharacters)
        {
            throw CreateLimitFailure(
                JsonDiagnosticCode.NumberCharacterLimitExceeded,
                "The configured number character limit was exceeded.");
        }

        if (text.Length > maximumOutputBytes)
        {
            throw CreateLimitFailure(
                JsonDiagnosticCode.OutputByteLimitExceeded,
                "The configured output byte limit was exceeded.");
        }

        return global::System.Text.Encoding.ASCII.GetBytes(text);
    }

    JsonWriteException CreateWriteFailure(
        JsonDiagnosticCode code,
        string message)
    {
        return new JsonWriteException(CreateDiagnostic(code, message));
    }

    JsonLimitException CreateLimitFailure(
        JsonDiagnosticCode code,
        string message)
    {
        return new JsonLimitException(CreateDiagnostic(code, message));
    }

    JsonDiagnostic CreateDiagnostic(
        JsonDiagnosticCode code,
        string message)
    {
        JsonDiagnostic diagnostic = new JsonDiagnostic();
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.ActualToken = "Number";
        return diagnostic;
    }
}
