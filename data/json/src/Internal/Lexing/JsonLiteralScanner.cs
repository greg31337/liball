namespace json;

internal class JsonLiteralScanner
{
    byte[] expectedBytes = new byte[5];
    byte[] rawBytes = new byte[5];
    int maximumDiagnosticExcerptBytes =
        JsonDefaults.MaximumDiagnosticExcerptBytes;
    int expectedByteCount;
    int rawByteCount;
    bool complete;
    bool booleanValue;
    JsonTokenKind tokenKind;

    internal bool PushByte(byte value)
    {
        if (complete)
        {
            throw new JsonParseException(CreateLiteralDiagnostic("The JSON literal is already complete."));
        }

        if (expectedByteCount == 0)
        {
            BeginLiteral(value);
            return false;
        }

        rawBytes[rawByteCount] = value;
        rawByteCount++;
        if (value != expectedBytes[rawByteCount - 1])
        {
            JsonDiagnostic diagnostic = CreateLiteralDiagnostic("The JSON literal is misspelled.");
            ResetScanner();
            throw new JsonParseException(diagnostic);
        }

        if (rawByteCount == expectedByteCount)
        {
            complete = true;
            return true;
        }

        return false;
    }

    internal void SetMaximumDiagnosticExcerptBytes(int maximumBytes)
    {
        maximumDiagnosticExcerptBytes = maximumBytes;
    }

    internal void CompleteLiteral()
    {
        if (expectedByteCount != 0 && !complete)
        {
            JsonDiagnostic diagnostic = CreateLiteralDiagnostic("The JSON literal is incomplete.");
            ResetScanner();
            throw new JsonParseException(diagnostic);
        }
    }

    internal bool HasPendingBytes()
    {
        return expectedByteCount != 0 && !complete;
    }

    internal bool IsComplete()
    {
        return complete;
    }

    internal JsonTokenKind GetTokenKind()
    {
        return tokenKind;
    }

    internal bool GetBooleanValue()
    {
        return booleanValue;
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

    internal void ResetScanner()
    {
        expectedByteCount = 0;
        rawByteCount = 0;
        complete = false;
        booleanValue = false;
        tokenKind = JsonTokenKind.None;
    }

    void BeginLiteral(byte value)
    {
        rawBytes[0] = value;
        rawByteCount = 1;

        if (value == 0x74)
        {
            expectedBytes[0] = 0x74;
            expectedBytes[1] = 0x72;
            expectedBytes[2] = 0x75;
            expectedBytes[3] = 0x65;
            expectedByteCount = 4;
            booleanValue = true;
            tokenKind = JsonTokenKind.Boolean;
            return;
        }

        if (value == 0x66)
        {
            expectedBytes[0] = 0x66;
            expectedBytes[1] = 0x61;
            expectedBytes[2] = 0x6C;
            expectedBytes[3] = 0x73;
            expectedBytes[4] = 0x65;
            expectedByteCount = 5;
            booleanValue = false;
            tokenKind = JsonTokenKind.Boolean;
            return;
        }

        if (value == 0x6E)
        {
            expectedBytes[0] = 0x6E;
            expectedBytes[1] = 0x75;
            expectedBytes[2] = 0x6C;
            expectedBytes[3] = 0x6C;
            expectedByteCount = 4;
            booleanValue = false;
            tokenKind = JsonTokenKind.Null;
            return;
        }

        JsonDiagnostic diagnostic = CreateLiteralDiagnostic("A JSON literal must begin with lowercase 't', 'f', or 'n'.");
        ResetScanner();
        throw new JsonParseException(diagnostic);
    }

    JsonDiagnostic CreateLiteralDiagnostic(string message)
    {
        JsonDiagnostic diagnostic = new JsonDiagnostic();
        diagnostic.Code = JsonDiagnosticCode.InvalidLiteral;
        diagnostic.Message = message;
        diagnostic.SourceExcerpt = CopyDiagnosticBytes();
        return diagnostic;
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
