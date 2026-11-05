namespace json;

internal class JsonLexer
{
    const int NoTokenState = 0;
    const int LiteralTokenState = 1;
    const int StringTokenState = 2;
    const int NumberTokenState = 3;

    JsonByteQueue queue;
    JsonLocationTracker locationTracker;
    Utf8Decoder locationDecoder = new Utf8Decoder();
    JsonLiteralScanner literalScanner = new JsonLiteralScanner();
    JsonStringScanner stringScanner = new JsonStringScanner();
    JsonNumberScanner numberScanner = new JsonNumberScanner();
    byte[] framingByteBuffer = new byte[1];
    JsonEscapeDecoder escapeDecoder;
    bool captureRawTokens;
    bool rejectUnicodeNoncharacters;
    JsonBomPolicy bomPolicy;
    int maximumTokenBytes;
    int maximumNumberCharacters;
    int maximumDecodedStringBytes;
    int maximumDecodedStringCodeUnits;
    int maximumDiagnosticExcerptBytes;
    int lexerState;
    int tokenByteCount;
    int locationScalarByteCount;
    JsonSourcePosition? tokenStart;
    JsonToken? currentToken;
    JsonSourceRange? currentRange;
    byte[]? currentRawBytes;
    byte currentSyntaxByte;

    internal JsonLexer(JsonReaderOptions? options)
    {
        JsonReaderOptions normalized = JsonOptionNormalizer.NormalizeReaderOptions(options);
        JsonLimits limits = normalized.Limits ?? JsonOptionNormalizer.NormalizeLimits(null);
        queue = new JsonByteQueue(limits.MaximumInputBufferBytes);
        locationTracker = new JsonLocationTracker(normalized.TrackLineInformation, normalized.TrackUnicodeScalarColumn);
        escapeDecoder = new JsonEscapeDecoder(normalized.UnicodePolicy);
        captureRawTokens = normalized.CaptureRawTokens;
        rejectUnicodeNoncharacters = normalized.ConformanceProfile == JsonConformanceProfile.IJson ||
            normalized.ConformanceProfile == JsonConformanceProfile.Jcs;
        bomPolicy = normalized.BomPolicy;
        maximumTokenBytes = limits.MaximumTokenBytes;
        maximumNumberCharacters = limits.MaximumNumberCharacters;
        maximumDecodedStringBytes = limits.MaximumDecodedStringBytes;
        maximumDecodedStringCodeUnits = limits.MaximumDecodedStringCodeUnits;
        maximumDiagnosticExcerptBytes =
            limits.MaximumDiagnosticExcerptBytes;
        literalScanner.SetMaximumDiagnosticExcerptBytes(
            maximumDiagnosticExcerptBytes);
        stringScanner.SetMaximumDiagnosticExcerptBytes(
            maximumDiagnosticExcerptBytes);
        numberScanner.SetMaximumDiagnosticExcerptBytes(
            maximumDiagnosticExcerptBytes);
        escapeDecoder.SetMaximumDiagnosticExcerptBytes(
            maximumDiagnosticExcerptBytes);
    }

    internal void FeedBytes(global::System.ReadOnlySpan<byte> bytes)
    {
        queue.EnqueueBytes(bytes);
    }

    internal JsonReadStatus ReadNextToken(bool finalInput)
    {
        ClearCurrentToken();
        while (queue.GetBufferedCount() > 0)
        {
            byte value = queue.GetByteAt(0);
            if (lexerState == NoTokenState)
            {
                if (value == 0xEF)
                {
                    int bomResult = ReadBom(finalInput);
                    if (bomResult == 1)
                    {
                        return JsonReadStatus.NeedMoreData;
                    }

                    if (bomResult == 2)
                    {
                        continue;
                    }
                }

                if (IsJsonWhitespace(value))
                {
                    ConsumeByte();
                    continue;
                }

                if (IsStructuralByte(value))
                {
                    return ReadStructuralToken(value);
                }

                tokenStart = locationTracker.GetPosition();
                tokenByteCount = 0;
                if (value == JsonSyntaxBytes.StringDelimiter)
                {
                    stringScanner.ResetScanner();
                    lexerState = StringTokenState;
                    continue;
                }

                if (value == 0x74 || value == 0x66 || value == 0x6E ||
                    value == 0x54 || value == 0x46 || value == 0x4E)
                {
                    literalScanner.ResetScanner();
                    lexerState = LiteralTokenState;
                    continue;
                }

                if (value == 0x2D || IsDigit(value))
                {
                    numberScanner.ResetScanner();
                    lexerState = NumberTokenState;
                    continue;
                }

                if (value <= 0x20)
                {
                    throw CreateLexerFailure(JsonDiagnosticCode.InvalidWhitespace, "The byte is not one of the four JSON whitespace characters.", value);
                }

                throw CreateLexerFailure(JsonDiagnosticCode.UnexpectedCharacter, "The byte cannot begin a JSON token.", value);
            }

            if (lexerState == LiteralTokenState)
            {
                if (IsTokenDelimiter(value))
                {
                    literalScanner.CompleteLiteral();
                    return FinishLiteralToken();
                }

                EnsureTokenCapacity(false);
                byte literalByte = ConsumeByte();
                tokenByteCount++;
                literalScanner.PushByte(literalByte);
                continue;
            }

            if (lexerState == NumberTokenState)
            {
                if (IsTokenDelimiter(value))
                {
                    numberScanner.CompleteNumber();
                    return FinishNumberToken();
                }

                EnsureTokenCapacity(true);
                byte numberByte = ConsumeByte();
                tokenByteCount++;
                numberScanner.PushByte(numberByte);
                continue;
            }

            EnsureTokenCapacity(false);
            byte stringByte = ConsumeByte();
            tokenByteCount++;
            if (stringScanner.PushByte(stringByte))
            {
                return FinishStringToken();
            }
        }

        if (!finalInput)
        {
            return JsonReadStatus.NeedMoreData;
        }

        if (lexerState == LiteralTokenState)
        {
            literalScanner.CompleteLiteral();
            return FinishLiteralToken();
        }

        if (lexerState == NumberTokenState)
        {
            numberScanner.CompleteNumber();
            return FinishNumberToken();
        }

        if (lexerState == StringTokenState)
        {
            stringScanner.CompleteString();
        }

        locationDecoder.CompleteDecoder();
        return JsonReadStatus.StreamComplete;
    }

    internal JsonToken? GetCurrentToken()
    {
        return currentToken;
    }

    internal byte GetCurrentSyntaxByte()
    {
        return currentSyntaxByte;
    }

    internal JsonSourceRange? GetCurrentRange()
    {
        return currentRange;
    }

    internal byte[]? GetCurrentRawBytes()
    {
        return currentRawBytes;
    }

    internal int GetBufferedByteCount()
    {
        return queue.GetBufferedCount();
    }

    internal long GetAcceptedByteCount()
    {
        return queue.GetAcceptedByteCount();
    }

    internal long GetConsumedByteCount()
    {
        return queue.GetConsumedByteCount();
    }

    internal JsonSourcePosition GetCurrentPosition()
    {
        return locationTracker.GetPosition();
    }

    internal JsonSourceRange GetCurrentFailureRange()
    {
        if (currentRange != null)
        {
            return currentRange;
        }

        if (tokenStart != null)
        {
            return locationTracker.CreateRange(tokenStart);
        }

        JsonSourcePosition position = locationTracker.GetPosition();
        JsonSourceRange range = new JsonSourceRange();
        range.Start = position;
        range.End = position;
        return range;
    }

    internal void ConsumeFramingByte(byte value)
    {
        if (lexerState != NoTokenState || queue.GetBufferedCount() != 0)
        {
            throw new global::System.InvalidOperationException("A framing byte requires an idle lexer.");
        }

        ClearCurrentToken();
        framingByteBuffer[0] = value;
        queue.EnqueueBytes(framingByteBuffer);
        ConsumeByte();
    }

    internal void ConsumeBufferedFramingByte()
    {
        if (lexerState != NoTokenState || queue.GetBufferedCount() != 1)
        {
            throw new global::System.InvalidOperationException("Exactly one buffered framing byte is required.");
        }

        ClearCurrentToken();
        ConsumeByte();
    }

    internal void ResetTokenForRecovery()
    {
        literalScanner.ResetScanner();
        stringScanner.ResetScanner();
        numberScanner.ResetScanner();
        escapeDecoder.ResetDecoder();
        locationDecoder.ResetDecoder();
        lexerState = NoTokenState;
        tokenByteCount = 0;
        locationScalarByteCount = 0;
        tokenStart = null;
        ClearCurrentToken();
    }

    internal void ResetLexer()
    {
        queue.ResetQueue();
        locationTracker.ResetTracker();
        locationDecoder.ResetDecoder();
        literalScanner.ResetScanner();
        stringScanner.ResetScanner();
        numberScanner.ResetScanner();
        escapeDecoder.ResetDecoder();
        lexerState = NoTokenState;
        tokenByteCount = 0;
        locationScalarByteCount = 0;
        tokenStart = null;
        ClearCurrentToken();
    }

    JsonReadStatus ReadStructuralToken(byte value)
    {
        JsonSourcePosition start = locationTracker.GetPosition();
        byte consumed = ConsumeByte();
        currentSyntaxByte = consumed;
        currentRange = locationTracker.CreateRange(start);
        if (captureRawTokens)
        {
            currentRawBytes = new byte[] { consumed };
        }

        JsonTokenKind kind = JsonTokenKind.None;
        if (value == JsonSyntaxBytes.BeginObject)
        {
            kind = JsonTokenKind.StartObject;
        }
        else if (value == JsonSyntaxBytes.EndObject)
        {
            kind = JsonTokenKind.EndObject;
        }
        else if (value == JsonSyntaxBytes.BeginArray)
        {
            kind = JsonTokenKind.StartArray;
        }
        else if (value == JsonSyntaxBytes.EndArray)
        {
            kind = JsonTokenKind.EndArray;
        }

        if (kind != JsonTokenKind.None)
        {
            JsonToken token = new JsonToken();
            token.Kind = kind;
            token.Range = currentRange;
            token.RawUtf8 = currentRawBytes;
            currentToken = token;
        }

        return JsonReadStatus.TokenAvailable;
    }

    JsonReadStatus FinishLiteralToken()
    {
        JsonToken token = new JsonToken();
        token.Kind = literalScanner.GetTokenKind();
        token.BooleanValue = literalScanner.GetBooleanValue();
        token.Range = CreateCurrentRange();
        byte[] rawBytes = literalScanner.CopyRawBytes();
        if (captureRawTokens)
        {
            token.RawUtf8 = rawBytes;
            currentRawBytes = rawBytes;
        }

        currentToken = token;
        currentRange = token.Range;
        currentSyntaxByte = rawBytes[0];
        literalScanner.ResetScanner();
        FinishTokenState();
        return JsonReadStatus.TokenAvailable;
    }

    JsonReadStatus FinishNumberToken()
    {
        JsonToken token = new JsonToken();
        token.Kind = JsonTokenKind.Number;
        token.Number = numberScanner.CreateNumber();
        token.Range = CreateCurrentRange();
        byte[] rawBytes = numberScanner.CopyRawBytes();
        if (captureRawTokens)
        {
            token.RawUtf8 = rawBytes;
            currentRawBytes = rawBytes;
        }

        currentToken = token;
        currentRange = token.Range;
        currentSyntaxByte = rawBytes[0];
        numberScanner.ResetScanner();
        FinishTokenState();
        return JsonReadStatus.TokenAvailable;
    }

    JsonReadStatus FinishStringToken()
    {
        string decodedValue = DecodeStringValue();
        EnforceUnicodeNoncharacterPolicy(decodedValue);
        EnforceDecodedStringLimits(decodedValue);

        JsonToken token = new JsonToken();
        token.Kind = JsonTokenKind.String;
        token.StringValue = decodedValue;
        token.Range = CreateCurrentRange();
        byte[] rawBytes = stringScanner.CopyRawBytes();
        if (captureRawTokens)
        {
            token.RawUtf8 = rawBytes;
            currentRawBytes = rawBytes;
        }

        currentToken = token;
        currentRange = token.Range;
        currentSyntaxByte = JsonSyntaxBytes.StringDelimiter;
        stringScanner.ResetScanner();
        FinishTokenState();
        return JsonReadStatus.TokenAvailable;
    }

    string DecodeStringValue()
    {
        byte[] rawBytes = stringScanner.CopyRawBytes();
        global::System.Text.StringBuilder builder = new global::System.Text.StringBuilder();
        escapeDecoder.ResetDecoder();
        int decodedEscapeIndex = 0;
        int index = 1;
        int contentEnd = rawBytes.Length - 1;
        while (index < contentEnd)
        {
            if (rawBytes[index] == JsonSyntaxBytes.Escape)
            {
                int escapeLength = rawBytes[index + 1] == 0x75 ? 6 : 2;
                int escapeEnd = index + escapeLength;
                while (index < escapeEnd)
                {
                    escapeDecoder.PushByte(rawBytes[index]);
                    index++;
                }

                decodedEscapeIndex = AppendDecodedEscapeValues(builder, decodedEscapeIndex);
                continue;
            }

            escapeDecoder.CompleteDecoder();
            decodedEscapeIndex = AppendDecodedEscapeValues(builder, decodedEscapeIndex);
            int segmentStart = index;
            while (index < contentEnd && rawBytes[index] != JsonSyntaxBytes.Escape)
            {
                index++;
            }

            builder.Append(global::System.Text.Encoding.UTF8.GetString(rawBytes, segmentStart, index - segmentStart));
        }

        escapeDecoder.CompleteDecoder();
        AppendDecodedEscapeValues(builder, decodedEscapeIndex);
        escapeDecoder.ResetDecoder();
        return builder.ToString();
    }

    int AppendDecodedEscapeValues(global::System.Text.StringBuilder builder, int startIndex)
    {
        int count = escapeDecoder.GetDecodedValueCount();
        int index = startIndex;
        while (index < count)
        {
            int value = escapeDecoder.GetDecodedValueAt(index);
            if (value <= 0xFFFF)
            {
                builder.Append((char)value);
            }
            else
            {
                int supplementary = value - 0x10000;
                builder.Append((char)(0xD800 + (supplementary >> 10)));
                builder.Append((char)(0xDC00 + (supplementary & 0x3FF)));
            }

            index++;
        }

        return count;
    }

    void EnforceUnicodeNoncharacterPolicy(string value)
    {
        if (!rejectUnicodeNoncharacters)
        {
            return;
        }

        int index = 0;
        while (index < value.Length)
        {
            int scalar = value[index];
            if (scalar >= 0xD800 && scalar <= 0xDBFF)
            {
                int lowSurrogate = value[index + 1];
                scalar = 0x10000 + ((scalar - 0xD800) << 10) + (lowSurrogate - 0xDC00);
                index++;
            }

            if ((scalar >= 0xFDD0 && scalar <= 0xFDEF) ||
                (scalar & 0xFFFF) == 0xFFFE ||
                (scalar & 0xFFFF) == 0xFFFF)
            {
                JsonDiagnostic diagnostic = new JsonDiagnostic();
                diagnostic.Code = JsonDiagnosticCode.InvalidStringCharacter;
                diagnostic.Message = "The selected conformance profile forbids Unicode noncharacters.";
                diagnostic.SourceExcerpt = BoundExcerpt(
                    stringScanner.CopyRawBytes());
                throw new JsonParseException(diagnostic);
            }

            index++;
        }
    }

    void EnforceDecodedStringLimits(string value)
    {
        if (value.Length > maximumDecodedStringCodeUnits)
        {
            throw CreateLimitFailure(JsonDiagnosticCode.DecodedStringCodeUnitLimitExceeded, "The decoded string UTF-16 code-unit limit was exceeded.");
        }

        int byteCount = global::System.Text.Encoding.UTF8.GetByteCount(value);
        if (byteCount > maximumDecodedStringBytes)
        {
            throw CreateLimitFailure(JsonDiagnosticCode.DecodedStringByteLimitExceeded, "The decoded string UTF-8 byte limit was exceeded.");
        }
    }

    void EnsureTokenCapacity(bool numberToken)
    {
        if (tokenByteCount >= maximumTokenBytes)
        {
            throw CreateLimitFailure(JsonDiagnosticCode.TokenByteLimitExceeded, "The raw token byte limit was exceeded.");
        }

        if (numberToken && tokenByteCount >= maximumNumberCharacters)
        {
            throw CreateLimitFailure(JsonDiagnosticCode.NumberCharacterLimitExceeded, "The number character limit was exceeded.");
        }
    }

    byte ConsumeByte()
    {
        byte value = queue.GetByteAt(0);
        queue.ConsumeBytes(1);
        AdvanceLocationByte(value);
        return value;
    }

    void AdvanceLocationByte(byte value)
    {
        locationScalarByteCount++;
        int scalar = locationDecoder.PushByte(value);
        if (scalar != Utf8Decoder.NeedMoreData)
        {
            locationTracker.AdvanceScalar(scalar, locationScalarByteCount);
            locationScalarByteCount = 0;
        }
    }

    JsonSourceRange CreateCurrentRange()
    {
        if (tokenStart == null)
        {
            throw new global::System.InvalidOperationException("A token start position is required.");
        }

        return locationTracker.CreateRange(tokenStart);
    }

    void FinishTokenState()
    {
        lexerState = NoTokenState;
        tokenByteCount = 0;
        tokenStart = null;
    }

    void ClearCurrentToken()
    {
        currentToken = null;
        currentRange = null;
        currentRawBytes = null;
        currentSyntaxByte = 0;
    }

    bool IsJsonWhitespace(byte value)
    {
        return value == JsonSyntaxBytes.Space || value == JsonSyntaxBytes.HorizontalTab ||
            value == JsonSyntaxBytes.LineFeed || value == JsonSyntaxBytes.CarriageReturn;
    }

    bool IsStructuralByte(byte value)
    {
        return value == JsonSyntaxBytes.BeginObject || value == JsonSyntaxBytes.EndObject ||
            value == JsonSyntaxBytes.BeginArray || value == JsonSyntaxBytes.EndArray ||
            value == JsonSyntaxBytes.NameSeparator || value == JsonSyntaxBytes.ValueSeparator;
    }

    bool IsTokenDelimiter(byte value)
    {
        return IsJsonWhitespace(value) || IsStructuralByte(value);
    }

    bool IsDigit(byte value)
    {
        return value >= 0x30 && value <= 0x39;
    }

    int ReadBom(bool finalInput)
    {
        int count = queue.GetBufferedCount();
        if (count == 1)
        {
            if (!finalInput)
            {
                return 1;
            }

            throw CreateBomFailure(JsonDiagnosticCode.InvalidUtf8, "The UTF-8 sequence is truncated.", queue.CopyBytes(0, count));
        }

        if (queue.GetByteAt(1) != 0xBB)
        {
            return 0;
        }

        if (count == 2)
        {
            if (!finalInput)
            {
                return 1;
            }

            throw CreateBomFailure(JsonDiagnosticCode.InvalidUtf8, "The UTF-8 sequence is truncated.", queue.CopyBytes(0, count));
        }

        if (queue.GetByteAt(2) != 0xBF)
        {
            return 0;
        }

        byte[] bomBytes = queue.CopyBytes(0, 3);
        if (queue.GetConsumedByteCount() != 0)
        {
            throw CreateBomFailure(JsonDiagnosticCode.UnexpectedBom, "A UTF-8 byte order mark is permitted only at byte offset zero.", bomBytes);
        }

        if (bomPolicy == JsonBomPolicy.Reject)
        {
            throw CreateBomFailure(JsonDiagnosticCode.BomNotAllowed, "The initial UTF-8 byte order mark is disallowed by policy.", bomBytes);
        }

        ConsumeByte();
        ConsumeByte();
        ConsumeByte();
        return 2;
    }

    JsonParseException CreateLexerFailure(JsonDiagnosticCode code, string message, byte value)
    {
        JsonDiagnostic diagnostic = new JsonDiagnostic();
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.SourceExcerpt = new byte[] { value };
        return new JsonParseException(diagnostic);
    }

    JsonParseException CreateBomFailure(JsonDiagnosticCode code, string message, byte[] excerpt)
    {
        JsonDiagnostic diagnostic = new JsonDiagnostic();
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.SourceExcerpt = BoundExcerpt(excerpt);
        return new JsonParseException(diagnostic);
    }

    JsonLimitException CreateLimitFailure(JsonDiagnosticCode code, string message)
    {
        JsonDiagnostic diagnostic = new JsonDiagnostic();
        diagnostic.Code = code;
        diagnostic.Message = message;
        if (lexerState == LiteralTokenState)
        {
            diagnostic.SourceExcerpt = BoundExcerpt(
                literalScanner.CopyRawBytes());
        }
        else if (lexerState == StringTokenState)
        {
            diagnostic.SourceExcerpt = BoundExcerpt(
                stringScanner.CopyRawBytes());
        }
        else if (lexerState == NumberTokenState)
        {
            diagnostic.SourceExcerpt = BoundExcerpt(
                numberScanner.CopyRawBytes());
        }

        return new JsonLimitException(diagnostic);
    }

    byte[] BoundExcerpt(byte[] source)
    {
        if (source.Length <= maximumDiagnosticExcerptBytes)
        {
            return source;
        }

        byte[] result = new byte[maximumDiagnosticExcerptBytes];
        global::System.Buffer.BlockCopy(
            source,
            source.Length - maximumDiagnosticExcerptBytes,
            result,
            0,
            maximumDiagnosticExcerptBytes);
        return result;
    }
}
