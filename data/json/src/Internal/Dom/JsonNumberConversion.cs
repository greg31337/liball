namespace json;

internal static class JsonNumberConversion
{
    static readonly JsonValueFactory NumberFactory = new JsonValueFactory();

    internal static long ConvertToInt64(JsonNumber number)
    {
        string lexeme = GetValidatedLexeme(number);
        if (number.Kind != JsonNumberKind.Integer)
        {
            throw CreateConversionFailure(
                "The JSON number is not in an integer lexical form for Int64 conversion.");
        }

        long value = 0;
        JsonExceptionCapture capture = new JsonExceptionCapture(() =>
        {
            value = global::System.Int64.Parse(
                lexeme,
                global::System.Globalization.NumberStyles.Integer,
                global::System.Globalization.CultureInfo.InvariantCulture);
        });
        capture.ExecuteOperation();
        global::System.Exception? failure = capture.GetFailure();
        if (failure != null)
        {
            throw CreateConversionFailure(
                "The JSON number is outside the Int64 range.");
        }

        return value;
    }

    internal static JsonInt64Result TryConvertToInt64(JsonNumber number)
    {
        long value = 0;
        JsonExceptionCapture capture = new JsonExceptionCapture(() =>
        {
            value = ConvertToInt64(number);
        });
        capture.ExecuteOperation();
        JsonInt64Result result = new JsonInt64Result();
        global::System.Exception? failure = capture.GetFailure();
        if (failure == null)
        {
            result.Success = true;
            result.Value = value;
            result.Diagnostic = null;
            return result;
        }

        JsonParseException? parseFailure = failure as JsonParseException;
        if (parseFailure == null)
        {
            throw failure;
        }

        result.Success = false;
        result.Value = 0;
        result.Diagnostic = parseFailure.Diagnostic;
        return result;
    }

    internal static ulong ConvertToUInt64(JsonNumber number)
    {
        string lexeme = GetValidatedLexeme(number);
        if (number.Kind != JsonNumberKind.Integer)
        {
            throw CreateConversionFailure(
                "The JSON number is not in an integer lexical form for UInt64 conversion.");
        }

        ulong value = 0;
        JsonExceptionCapture capture = new JsonExceptionCapture(() =>
        {
            value = global::System.UInt64.Parse(
                lexeme,
                global::System.Globalization.NumberStyles.Integer,
                global::System.Globalization.CultureInfo.InvariantCulture);
        });
        capture.ExecuteOperation();
        global::System.Exception? failure = capture.GetFailure();
        if (failure != null)
        {
            throw CreateConversionFailure(
                "The JSON number is outside the UInt64 range.");
        }

        return value;
    }

    internal static JsonUInt64Result TryConvertToUInt64(JsonNumber number)
    {
        ulong value = 0;
        JsonExceptionCapture capture = new JsonExceptionCapture(() =>
        {
            value = ConvertToUInt64(number);
        });
        capture.ExecuteOperation();
        JsonUInt64Result result = new JsonUInt64Result();
        global::System.Exception? failure = capture.GetFailure();
        if (failure == null)
        {
            result.Success = true;
            result.Value = value;
            result.Diagnostic = null;
            return result;
        }

        JsonParseException? parseFailure = failure as JsonParseException;
        if (parseFailure == null)
        {
            throw failure;
        }

        result.Success = false;
        result.Value = 0;
        result.Diagnostic = parseFailure.Diagnostic;
        return result;
    }

    internal static decimal ConvertToDecimal(JsonNumber number)
    {
        string lexeme = GetValidatedLexeme(number);
        decimal value = 0;
        JsonExceptionCapture capture = new JsonExceptionCapture(() =>
        {
            value = global::System.Decimal.Parse(
                lexeme,
                global::System.Globalization.NumberStyles.Float,
                global::System.Globalization.CultureInfo.InvariantCulture);
        });
        capture.ExecuteOperation();
        global::System.Exception? failure = capture.GetFailure();
        if (failure != null)
        {
            throw CreateConversionFailure(
                "The JSON number is outside the Decimal range.");
        }

        return value;
    }

    internal static JsonDecimalResult TryConvertToDecimal(JsonNumber number)
    {
        decimal value = 0;
        JsonExceptionCapture capture = new JsonExceptionCapture(() =>
        {
            value = ConvertToDecimal(number);
        });
        capture.ExecuteOperation();
        JsonDecimalResult result = new JsonDecimalResult();
        global::System.Exception? failure = capture.GetFailure();
        if (failure == null)
        {
            result.Success = true;
            result.Value = value;
            result.Diagnostic = null;
            return result;
        }

        JsonParseException? parseFailure = failure as JsonParseException;
        if (parseFailure == null)
        {
            throw failure;
        }

        result.Success = false;
        result.Value = 0;
        result.Diagnostic = parseFailure.Diagnostic;
        return result;
    }

    internal static double ConvertToDouble(JsonNumber number)
    {
        string lexeme = GetValidatedLexeme(number);
        double value = 0;
        JsonExceptionCapture capture = new JsonExceptionCapture(() =>
        {
            value = global::System.Double.Parse(
                lexeme,
                global::System.Globalization.NumberStyles.Float,
                global::System.Globalization.CultureInfo.InvariantCulture);
        });
        capture.ExecuteOperation();
        global::System.Exception? failure = capture.GetFailure();
        if (failure != null || !global::System.Double.IsFinite(value))
        {
            throw CreateConversionFailure(
                "The JSON number is outside the finite Double range.");
        }

        return value;
    }

    internal static JsonDoubleResult TryConvertToDouble(JsonNumber number)
    {
        double value = 0;
        JsonExceptionCapture capture = new JsonExceptionCapture(() =>
        {
            value = ConvertToDouble(number);
        });
        capture.ExecuteOperation();
        JsonDoubleResult result = new JsonDoubleResult();
        global::System.Exception? failure = capture.GetFailure();
        if (failure == null)
        {
            result.Success = true;
            result.Value = value;
            result.Diagnostic = null;
            return result;
        }

        JsonParseException? parseFailure = failure as JsonParseException;
        if (parseFailure == null)
        {
            throw failure;
        }

        result.Success = false;
        result.Value = 0;
        result.Diagnostic = parseFailure.Diagnostic;
        return result;
    }

    static string GetValidatedLexeme(JsonNumber? number)
    {
        if (number == null || number.Lexeme == null)
        {
            throw CreateInvalidModelFailure(
                "A JSON number with an exact lexeme is required for conversion.");
        }

        string lexeme = number.Lexeme;
        JsonValueResult validation = NumberFactory.TryCreateNumber(lexeme);
        JsonNumber? expected = validation.Value?.Number;
        if (!validation.Success || expected == null)
        {
            throw CreateInvalidModelFailure(
                "The JSON number lexeme is invalid.");
        }

        if (number.Kind != expected.Kind ||
            number.IsNegative != expected.IsNegative ||
            number.HasFraction != expected.HasFraction ||
            number.HasExponent != expected.HasExponent ||
            number.IsNegativeZero != expected.IsNegativeZero)
        {
            throw CreateInvalidModelFailure(
                "The JSON number classification does not match its lexeme.");
        }

        return lexeme;
    }

    static JsonParseException CreateInvalidModelFailure(string message)
    {
        return new JsonParseException(CreateConversionDiagnostic(
            JsonDiagnosticCode.InvalidModel,
            message));
    }

    static JsonParseException CreateConversionFailure(string message)
    {
        return new JsonParseException(CreateConversionDiagnostic(
            JsonDiagnosticCode.NumericConversionFailed,
            message));
    }

    static JsonDiagnostic CreateConversionDiagnostic(
        JsonDiagnosticCode code,
        string message)
    {
        JsonDiagnostic diagnostic = new JsonDiagnostic();
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.ActualToken = JsonValueKind.Number.ToString();
        return diagnostic;
    }
}
