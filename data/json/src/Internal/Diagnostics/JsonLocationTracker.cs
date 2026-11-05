namespace json;

internal class JsonLocationTracker
{
    bool trackLineInformation;
    bool trackScalarColumn;
    long byteOffset;
    int line;
    int byteColumn;
    int scalarColumn;
    bool pendingCarriageReturn;

    internal JsonLocationTracker(bool trackLineInformation, bool trackScalarColumn)
    {
        if (trackScalarColumn && !trackLineInformation)
        {
            throw new JsonParseException(JsonOptionNormalizer.CreateReaderDiagnostic("Scalar-column tracking requires line tracking."));
        }

        this.trackLineInformation = trackLineInformation;
        this.trackScalarColumn = trackScalarColumn;
        ResetTracker();
    }

    internal void AdvanceScalar(int scalar, int utf8ByteCount)
    {
        int requiredByteCount = GetRequiredByteCount(scalar);
        if (utf8ByteCount != requiredByteCount)
        {
            throw new global::System.ArgumentOutOfRangeException("utf8ByteCount");
        }

        if (byteOffset > long.MaxValue - utf8ByteCount)
        {
            throw new JsonLimitException(CreateLocationLimitDiagnostic("The source byte offset would overflow."));
        }

        if (!trackLineInformation)
        {
            byteOffset += utf8ByteCount;
            return;
        }

        if (scalar == 0x0D)
        {
            if (line == int.MaxValue)
            {
                throw new JsonLimitException(CreateLocationLimitDiagnostic("The source line counter would overflow."));
            }

            byteOffset += utf8ByteCount;
            line++;
            byteColumn = 1;
            if (trackScalarColumn)
            {
                scalarColumn = 1;
            }

            pendingCarriageReturn = true;
            return;
        }

        if (scalar == 0x0A)
        {
            if (pendingCarriageReturn)
            {
                byteOffset += utf8ByteCount;
                pendingCarriageReturn = false;
                return;
            }

            if (line == int.MaxValue)
            {
                throw new JsonLimitException(CreateLocationLimitDiagnostic("The source line counter would overflow."));
            }

            byteOffset += utf8ByteCount;
            line++;
            byteColumn = 1;
            if (trackScalarColumn)
            {
                scalarColumn = 1;
            }

            return;
        }

        if (byteColumn > int.MaxValue - utf8ByteCount)
        {
            throw new JsonLimitException(CreateLocationLimitDiagnostic("The source byte column would overflow."));
        }

        if (trackScalarColumn && scalarColumn == int.MaxValue)
        {
            throw new JsonLimitException(CreateLocationLimitDiagnostic("The source scalar column would overflow."));
        }

        byteOffset += utf8ByteCount;
        byteColumn += utf8ByteCount;
        if (trackScalarColumn)
        {
            scalarColumn++;
        }

        pendingCarriageReturn = false;
    }

    internal JsonSourcePosition GetPosition()
    {
        JsonSourcePosition position = new JsonSourcePosition();
        position.ByteOffset = byteOffset;
        if (trackLineInformation)
        {
            position.Line = line;
            position.ByteColumn = byteColumn;
        }

        if (trackScalarColumn)
        {
            position.ScalarColumn = scalarColumn;
        }

        return position;
    }

    internal JsonSourceRange CreateRange(JsonSourcePosition startPosition)
    {
        JsonSourceRange range = new JsonSourceRange();
        range.Start = startPosition;
        range.End = GetPosition();
        return range;
    }

    internal bool HasPendingCarriageReturn()
    {
        return pendingCarriageReturn;
    }

    internal void ResetTracker()
    {
        byteOffset = 0;
        pendingCarriageReturn = false;
        if (trackLineInformation)
        {
            line = 1;
            byteColumn = 1;
        }
        else
        {
            line = 0;
            byteColumn = 0;
        }

        scalarColumn = trackScalarColumn ? 1 : 0;
    }

    internal static int GetRequiredByteCount(int scalar)
    {
        if (scalar < 0 || scalar > 0x10FFFF || scalar >= 0xD800 && scalar <= 0xDFFF)
        {
            throw new global::System.ArgumentOutOfRangeException("scalar");
        }

        if (scalar <= 0x7F)
        {
            return 1;
        }

        if (scalar <= 0x7FF)
        {
            return 2;
        }

        if (scalar <= 0xFFFF)
        {
            return 3;
        }

        return 4;
    }

    internal static JsonDiagnostic CreateLocationLimitDiagnostic(string message)
    {
        JsonDiagnostic diagnostic = new JsonDiagnostic();
        diagnostic.Code = JsonDiagnosticCode.DocumentByteLimitExceeded;
        diagnostic.Message = message;
        return diagnostic;
    }
}
