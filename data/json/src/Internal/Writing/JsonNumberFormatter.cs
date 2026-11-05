namespace json;

internal class JsonNumberFormatter
{
    JsonValueFactory numberFactory = new JsonValueFactory();
    int maximumNumberCharacters;
    int maximumOutputBytes;

    internal JsonNumberFormatter(JsonWriterOptions options)
    {
        JsonWriterOptions normalized =
            JsonOptionNormalizer.NormalizeWriterOptions(options);
        JsonLimits limits = normalized.Limits
            ?? throw new global::System.InvalidOperationException(
                "Normalized writer limits are required.");
        maximumNumberCharacters = limits.MaximumNumberCharacters;
        maximumOutputBytes = limits.MaximumOutputBytes;
    }

    internal byte[] FormatInt64(long value)
    {
        string lexeme = value.ToString(
            global::System.Globalization.CultureInfo.InvariantCulture);
        return EncodeFormattedLexeme(lexeme);
    }

    internal byte[] FormatUInt64(ulong value)
    {
        string lexeme = value.ToString(
            global::System.Globalization.CultureInfo.InvariantCulture);
        return EncodeFormattedLexeme(lexeme);
    }

    internal byte[] FormatDecimal(decimal value)
    {
        string lexeme = value.ToString(
            global::System.Globalization.CultureInfo.InvariantCulture);
        int[] bits = global::System.Decimal.GetBits(value);
        if (value == 0 && bits[3] < 0 && lexeme[0] != '-')
        {
            lexeme = "-" + lexeme;
        }

        return EncodeFormattedLexeme(lexeme);
    }

    internal byte[] FormatDouble(double value)
    {
        if (!global::System.Double.IsFinite(value))
        {
            throw CreateWriteFailure(
                JsonDiagnosticCode.NonFiniteNumber,
                "A JSON number must be finite.");
        }

        string lexeme;
        if (value == 0 &&
            global::System.BitConverter.DoubleToInt64Bits(value) < 0)
        {
            lexeme = "-0";
        }
        else
        {
            lexeme = value.ToString(
                "R",
                global::System.Globalization.CultureInfo.InvariantCulture);
        }

        return EncodeFormattedLexeme(lexeme);
    }

    internal byte[] FormatExactLexeme(string lexeme)
    {
        if (lexeme == null)
        {
            throw CreateWriteFailure(
                JsonDiagnosticCode.InvalidNumber,
                "An exact JSON number lexeme is required.");
        }

        EnforceLengthLimits(lexeme.Length);
        JsonValueResult validation = numberFactory.TryCreateNumber(lexeme);
        if (!validation.Success)
        {
            JsonDiagnostic diagnostic = validation.Diagnostic
                ?? CreateDiagnostic(
                    JsonDiagnosticCode.InvalidNumber,
                    "The exact JSON number lexeme is invalid.");
            throw new JsonWriteException(diagnostic);
        }

        return global::System.Text.Encoding.ASCII.GetBytes(lexeme);
    }

    byte[] EncodeFormattedLexeme(string lexeme)
    {
        EnforceLengthLimits(lexeme.Length);
        return global::System.Text.Encoding.ASCII.GetBytes(lexeme);
    }

    void EnforceLengthLimits(int length)
    {
        if (length > maximumNumberCharacters)
        {
            throw CreateLimitFailure(
                JsonDiagnosticCode.NumberCharacterLimitExceeded,
                "The configured number character limit was exceeded.");
        }

        if (length > maximumOutputBytes)
        {
            throw CreateLimitFailure(
                JsonDiagnosticCode.OutputByteLimitExceeded,
                "The configured output byte limit was exceeded.");
        }
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
