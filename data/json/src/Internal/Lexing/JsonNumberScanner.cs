namespace json;

internal class JsonNumberScanner
{
    const int StartState = 0;
    const int MinusState = 1;
    const int ZeroState = 2;
    const int IntegerState = 3;
    const int FractionMarkerState = 4;
    const int FractionState = 5;
    const int ExponentMarkerState = 6;
    const int ExponentSignState = 7;
    const int ExponentState = 8;

    byte[] rawBytes = new byte[32];
    int maximumDiagnosticExcerptBytes =
        JsonDefaults.MaximumDiagnosticExcerptBytes;
    int rawByteCount;
    int state;
    bool complete;
    bool negative;
    bool fraction;
    bool exponent;
    bool nonZeroSignificandDigit;

    internal bool PushByte(byte value)
    {
        if (complete)
        {
            throw CreateNumberFailure("The JSON number is already complete.");
        }

        AppendRawByte(value);
        if (state == StartState)
        {
            if (value == 0x2D)
            {
                negative = true;
                state = MinusState;
                return false;
            }

            if (value == 0x30)
            {
                state = ZeroState;
                return true;
            }

            if (IsNonZeroDigit(value))
            {
                nonZeroSignificandDigit = true;
                state = IntegerState;
                return true;
            }

            throw CreateNumberFailure("A JSON number must begin with a minus sign or decimal digit.");
        }

        if (state == MinusState)
        {
            if (value == 0x30)
            {
                state = ZeroState;
                return true;
            }

            if (IsNonZeroDigit(value))
            {
                nonZeroSignificandDigit = true;
                state = IntegerState;
                return true;
            }

            throw CreateNumberFailure("A minus sign must be followed by an integer digit.");
        }

        if (state == ZeroState)
        {
            if (value == 0x2E)
            {
                fraction = true;
                state = FractionMarkerState;
                return false;
            }

            if (value == 0x65 || value == 0x45)
            {
                exponent = true;
                state = ExponentMarkerState;
                return false;
            }

            throw CreateNumberFailure("A leading zero cannot be followed by another integer digit or number character.");
        }

        if (state == IntegerState)
        {
            if (IsDigit(value))
            {
                if (value != 0x30)
                {
                    nonZeroSignificandDigit = true;
                }

                return true;
            }

            if (value == 0x2E)
            {
                fraction = true;
                state = FractionMarkerState;
                return false;
            }

            if (value == 0x65 || value == 0x45)
            {
                exponent = true;
                state = ExponentMarkerState;
                return false;
            }

            throw CreateNumberFailure("The byte is not valid after the integer portion of a JSON number.");
        }

        if (state == FractionMarkerState)
        {
            if (!IsDigit(value))
            {
                throw CreateNumberFailure("A decimal point must be followed by a fraction digit.");
            }

            if (value != 0x30)
            {
                nonZeroSignificandDigit = true;
            }

            state = FractionState;
            return true;
        }

        if (state == FractionState)
        {
            if (IsDigit(value))
            {
                if (value != 0x30)
                {
                    nonZeroSignificandDigit = true;
                }

                return true;
            }

            if (value == 0x65 || value == 0x45)
            {
                exponent = true;
                state = ExponentMarkerState;
                return false;
            }

            throw CreateNumberFailure("The byte is not valid after the fraction portion of a JSON number.");
        }

        if (state == ExponentMarkerState)
        {
            if (value == 0x2B || value == 0x2D)
            {
                state = ExponentSignState;
                return false;
            }

            if (IsDigit(value))
            {
                state = ExponentState;
                return true;
            }

            throw CreateNumberFailure("An exponent marker must be followed by a sign or decimal digit.");
        }

        if (state == ExponentSignState)
        {
            if (!IsDigit(value))
            {
                throw CreateNumberFailure("An exponent sign must be followed by a decimal digit.");
            }

            state = ExponentState;
            return true;
        }

        if (state == ExponentState)
        {
            if (IsDigit(value))
            {
                return true;
            }

            throw CreateNumberFailure("The byte is not valid after the exponent portion of a JSON number.");
        }

        throw CreateNumberFailure("The JSON number scanner is in an invalid state.");
    }

    internal void SetMaximumDiagnosticExcerptBytes(int maximumBytes)
    {
        maximumDiagnosticExcerptBytes = maximumBytes;
    }

    internal void CompleteNumber()
    {
        if (complete)
        {
            return;
        }

        if (!CanCompleteNumber())
        {
            throw CreateNumberFailure("The JSON number is incomplete.");
        }

        complete = true;
    }

    internal bool CanCompleteNumber()
    {
        return state == ZeroState || state == IntegerState || state == FractionState || state == ExponentState;
    }

    internal bool HasPendingBytes()
    {
        return rawByteCount > 0 && !complete;
    }

    internal bool IsComplete()
    {
        return complete;
    }

    internal int GetRawByteCount()
    {
        return rawByteCount;
    }

    internal byte[] CopyRawBytes()
    {
        byte[] result = new byte[rawByteCount];
        if (rawByteCount > 0)
        {
            global::System.Buffer.BlockCopy(rawBytes, 0, result, 0, rawByteCount);
        }

        return result;
    }

    internal string CopyLexeme()
    {
        return global::System.Text.Encoding.ASCII.GetString(rawBytes, 0, rawByteCount);
    }

    internal JsonNumberKind GetNumberKind()
    {
        if (fraction && exponent)
        {
            return JsonNumberKind.FractionWithExponent;
        }

        if (fraction)
        {
            return JsonNumberKind.Fraction;
        }

        if (exponent)
        {
            return JsonNumberKind.Exponent;
        }

        return JsonNumberKind.Integer;
    }

    internal bool IsNegative()
    {
        return negative;
    }

    internal bool HasFraction()
    {
        return fraction;
    }

    internal bool HasExponent()
    {
        return exponent;
    }

    internal bool IsNegativeZero()
    {
        return complete && negative && !nonZeroSignificandDigit;
    }

    internal JsonNumber CreateNumber()
    {
        CompleteNumber();
        JsonNumber number = new JsonNumber();
        number.Lexeme = CopyLexeme();
        number.Kind = GetNumberKind();
        number.IsNegative = negative;
        number.HasFraction = fraction;
        number.HasExponent = exponent;
        number.IsNegativeZero = IsNegativeZero();
        return number;
    }

    internal void ResetScanner()
    {
        rawByteCount = 0;
        state = StartState;
        complete = false;
        negative = false;
        fraction = false;
        exponent = false;
        nonZeroSignificandDigit = false;
    }

    bool IsDigit(byte value)
    {
        return value >= 0x30 && value <= 0x39;
    }

    bool IsNonZeroDigit(byte value)
    {
        return value >= 0x31 && value <= 0x39;
    }

    void AppendRawByte(byte value)
    {
        if (rawByteCount == rawBytes.Length)
        {
            int newCapacity = rawBytes.Length * 2;
            byte[] replacement = new byte[newCapacity];
            global::System.Buffer.BlockCopy(rawBytes, 0, replacement, 0, rawByteCount);
            rawBytes = replacement;
        }

        rawBytes[rawByteCount] = value;
        rawByteCount++;
    }

    JsonParseException CreateNumberFailure(string message)
    {
        JsonDiagnostic diagnostic = new JsonDiagnostic();
        diagnostic.Code = JsonDiagnosticCode.InvalidNumber;
        diagnostic.Message = message;
        diagnostic.SourceExcerpt = CopyDiagnosticBytes();
        ResetScanner();
        return new JsonParseException(diagnostic);
    }

    byte[] CopyDiagnosticBytes()
    {
        int length = rawByteCount < maximumDiagnosticExcerptBytes
            ? rawByteCount
            : maximumDiagnosticExcerptBytes;
        byte[] result = new byte[length];
        if (length > 0)
        {
            global::System.Buffer.BlockCopy(
                rawBytes,
                rawByteCount - length,
                result,
                0,
                length);
        }

        return result;
    }
}
