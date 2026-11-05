using System;

namespace sdp;

internal static class SdpParserCore
{
    internal static SdpParseResult ParseTextInput(string input, SdpParseOptions options, bool validateDescriptionStructure)
    {
        return ParseCharacterInput(input.AsSpan(), options, validateDescriptionStructure);
    }

    internal static SdpParseResult ParseCharacterInput(ReadOnlySpan<char> input, SdpParseOptions options, bool validateDescriptionStructure)
    {
        ValidateOptions(options);
        SdpParseContext context = new SdpParseContext(CopyOptions(options), false, validateDescriptionStructure);
        if (input.Length > context.Options.MaximumInputCharacters)
        {
            SdpSourceLocation location = CreateLocation(0, -1, 1, 1);
            context.AddDiagnostic(SdpDiagnosticCodes.ParseInputCharacterLimitExceeded, SdpValidationSeverity.Fatal, SdpScope.Session, '\0', -1, -1, "The SDP text exceeds the configured character limit.", location);
            context.Stop = true;
            context.Complete();
            return context.Result;
        }
        if (!ValidateCharacterContent(context, input))
        {
            context.Complete();
            return context.Result;
        }

        int offset = 0;
        int lineNumber = 1;
        while (offset < input.Length && !context.Stop)
        {
            SdpLineReadResult lineResult = SdpLineReader.ReadCharacterLine(input, offset, lineNumber, context.Options.MaximumLineLength, context.Options.MaximumLineCount);
            if (lineResult.LineCountExceeded)
            {
                SdpSourceLocation location = CreateLocation(offset, -1, lineNumber, 1);
                context.AddDiagnostic(SdpDiagnosticCodes.ParseLineCountLimitExceeded, SdpValidationSeverity.Fatal, context.Scope, '\0', context.MediaIndex, -1, "The SDP input exceeds the configured line-count limit.", location);
                context.Stop = true;
                continue;
            }
            if (!lineResult.HasLine)
            {
                continue;
            }

            offset = lineResult.NextOffset;
            context.Result.CharactersConsumed = offset;
            context.Result.LinesConsumed = context.Result.LinesConsumed + 1;
            context.RecordLineEnd(lineResult);
            ProcessLine(context, lineResult);
            lineNumber = lineNumber + 1;
        }
        context.Complete();
        return context.Result;
    }

    internal static SdpParseResult ParseUtf8Input(ReadOnlySpan<byte> input, SdpParseOptions options, bool validateDescriptionStructure)
    {
        ValidateOptions(options);
        SdpParseContext context = new SdpParseContext(CopyOptions(options), true, validateDescriptionStructure);
        if (input.Length > context.Options.MaximumInputBytes)
        {
            SdpSourceLocation location = CreateLocation(0, 0, 1, 1);
            context.AddDiagnostic(SdpDiagnosticCodes.ParseInputByteLimitExceeded, SdpValidationSeverity.Fatal, SdpScope.Session, '\0', -1, -1, "The UTF-8 SDP input exceeds the configured byte limit.", location);
            context.Stop = true;
            context.Complete();
            return context.Result;
        }

        SdpUtf8DecodeResult validation = SdpUtf8Decoder.ValidateUtf8(input, context.Options.AllowByteOrderMark);
        if (!validation.Success)
        {
            SdpSourceLocation location = CreateUtf8FailureLocation(input, validation);
            context.AddDiagnostic(validation.DiagnosticCode, SdpValidationSeverity.Fatal, SdpScope.Session, '\0', -1, -1, "The UTF-8 SDP input could not be safely decoded.", location);
            context.Result.CharactersConsumed = validation.CharacterCount;
            context.Result.BytesConsumed = validation.BytesConsumed;
            context.Stop = true;
            context.Complete();
            return context.Result;
        }

        int byteOffset = validation.HasByteOrderMark ? 3 : 0;
        int characterOffset = 0;
        int lineNumber = 1;
        context.Result.BytesConsumed = byteOffset;
        while (byteOffset < input.Length && !context.Stop)
        {
            SdpLineReadResult lineResult = SdpLineReader.ReadUtf8Line(input, byteOffset, characterOffset, lineNumber, context.Options.MaximumLineLength, context.Options.MaximumLineCount);
            if (lineResult.LineCountExceeded)
            {
                SdpSourceLocation location = CreateLocation(characterOffset, byteOffset, lineNumber, 1);
                context.AddDiagnostic(SdpDiagnosticCodes.ParseLineCountLimitExceeded, SdpValidationSeverity.Fatal, context.Scope, '\0', context.MediaIndex, -1, "The SDP input exceeds the configured line-count limit.", location);
                context.Stop = true;
                continue;
            }
            if (!lineResult.HasLine)
            {
                continue;
            }

            byteOffset = lineResult.NextOffset;
            characterOffset = characterOffset + lineResult.CharactersConsumed;
            context.Result.BytesConsumed = byteOffset;
            context.Result.CharactersConsumed = characterOffset;
            context.Result.LinesConsumed = context.Result.LinesConsumed + 1;
            context.RecordLineEnd(lineResult);
            ProcessLine(context, lineResult);
            lineNumber = lineNumber + 1;
        }
        context.Complete();
        return context.Result;
    }

    internal static void ProcessLine(SdpParseContext context, SdpLineReadResult lineResult)
    {
        context.CurrentLineMalformed = false;
        if (lineResult.LineLengthExceeded)
        {
            context.CanAttachRepeat = false;
            context.AddDiagnostic(SdpDiagnosticCodes.ParseLineLengthLimitExceeded, SdpValidationSeverity.Error, context.Scope, '\0', context.MediaIndex, -1, "An SDP input line exceeds the configured length limit.", lineResult.Line.Location);
            return;
        }

        bool validShape = lineResult.Line.Text.Length >= 2 && lineResult.Line.Text[1] == '=' && lineResult.Line.Text[0] >= 'a' && lineResult.Line.Text[0] <= 'z';
        char fieldName = '\0';
        int occurrenceIndex = -1;
        if (validShape)
        {
            fieldName = lineResult.Line.Text[0];
            if (fieldName == 'm')
            {
                if (!context.EnterMedia(lineResult.Line))
                {
                    return;
                }
            }
            if (fieldName == 'a')
            {
                if (!context.CountAttribute(lineResult.Line))
                {
                    return;
                }
            }
            occurrenceIndex = SdpFieldOrderValidator.ReadOccurrenceIndex(context, fieldName);
        }

        bool malformed = context.ValidateLineEnding(lineResult.Line, fieldName);
        if (lineResult.Line.Text.Length == 0)
        {
            context.AddDiagnostic(SdpDiagnosticCodes.ParseEmptyLine, SdpValidationSeverity.Error, context.Scope, '\0', context.MediaIndex, -1, "The SDP input contains an empty line.", lineResult.Line.Location);
            malformed = true;
        }
        else if (!validShape)
        {
            context.AddDiagnostic(SdpDiagnosticCodes.ParseMalformedLine, SdpValidationSeverity.Error, context.Scope, '\0', context.MediaIndex, -1, "An SDP line does not begin with a lowercase field name and equals sign.", lineResult.Line.Location);
            malformed = true;
        }
        if (validShape && context.ValidateDescriptionStructure)
        {
            if (SdpFieldOrderValidator.IsKnownField(fieldName))
            {
                SdpFieldOrderValidator.ValidateFieldOrder(context, lineResult.Line, fieldName);
            }
            else
            {
                SdpFieldOrderValidator.AddUnknownFieldDiagnostic(context, lineResult.Line, fieldName);
            }
        }
        if (!validShape || fieldName != 't' && fieldName != 'r' && fieldName != 'z')
        {
            context.CanAttachRepeat = false;
        }
        if (validShape && fieldName == 'v')
        {
            SdpSessionLineParser.ParseProtocolVersion(context, lineResult.Line);
        }
        if (validShape && fieldName == 'o')
        {
            SdpSessionLineParser.ParseOrigin(context, lineResult.Line);
        }
        if (validShape && fieldName == 's')
        {
            SdpSessionLineParser.ParseSessionName(context, lineResult.Line);
        }
        if (validShape && fieldName == 'i')
        {
            SdpSessionLineParser.ParseInformation(context, lineResult.Line);
        }
        if (validShape && fieldName == 'u')
        {
            SdpSessionLineParser.ParseUri(context, lineResult.Line);
        }
        if (validShape && fieldName == 'e')
        {
            SdpSessionLineParser.ParseEmail(context, lineResult.Line);
        }
        if (validShape && fieldName == 'p')
        {
            SdpSessionLineParser.ParsePhone(context, lineResult.Line);
        }
        if (validShape && fieldName == 'c')
        {
            SdpConnectionLineParser.ParseConnection(context, lineResult.Line);
        }
        if (validShape && fieldName == 'b')
        {
            SdpBandwidthLineParser.ParseBandwidth(context, lineResult.Line);
        }
        if (validShape && fieldName == 'm')
        {
            SdpMediaLineParser.ParseMedia(context, lineResult.Line);
        }
        if (validShape && fieldName == 't')
        {
            SdpTimingLineParser.ParseTiming(context, lineResult.Line);
        }
        if (validShape && fieldName == 'r')
        {
            SdpTimingLineParser.ParseRepeat(context, lineResult.Line);
        }
        if (validShape && fieldName == 'z')
        {
            SdpTimingLineParser.ParseZone(context, lineResult.Line);
        }
        if (validShape && fieldName == 'k')
        {
            SdpEncryptionKeyLineParser.ParseEncryptionKey(context, lineResult.Line);
        }
        if (validShape && fieldName == 'a')
        {
            SdpAttributeLineParser.ParseAttribute(context, lineResult.Line);
        }
        malformed = malformed || context.CurrentLineMalformed;
        if (malformed)
        {
            context.AddRecoveryDiagnostic(lineResult.Line, fieldName, occurrenceIndex);
        }
        context.RetainLine(lineResult.Line, malformed);
    }

    internal static bool ValidateCharacterContent(SdpParseContext context, ReadOnlySpan<char> input)
    {
        int offset = 0;
        int lineNumber = 1;
        int columnNumber = 1;
        while (offset < input.Length)
        {
            char value = input[offset];
            if (value == '\0')
            {
                SdpSourceLocation location = CreateLocation(offset, -1, lineNumber, columnNumber);
                context.AddDiagnostic(SdpDiagnosticCodes.ParseNulCharacterNotAllowed, SdpValidationSeverity.Fatal, SdpScope.Session, '\0', -1, -1, "The SDP text contains a NUL character.", location);
                context.Result.CharactersConsumed = offset;
                context.Stop = true;
                return false;
            }
            if (char.IsControl(value) && value != '\r' && value != '\n')
            {
                SdpSourceLocation location = CreateLocation(offset, -1, lineNumber, columnNumber);
                context.AddDiagnostic(SdpDiagnosticCodes.ParseControlCharacterNotAllowed, SdpValidationSeverity.Fatal, SdpScope.Session, '\0', -1, -1, "The SDP text contains a disallowed control character.", location);
                context.Result.CharactersConsumed = offset;
                context.Stop = true;
                return false;
            }
            if (value == '\r')
            {
                if (offset + 1 < input.Length && input[offset + 1] == '\n')
                {
                    offset = offset + 1;
                }
                lineNumber = lineNumber + 1;
                columnNumber = 1;
            }
            else if (value == '\n')
            {
                lineNumber = lineNumber + 1;
                columnNumber = 1;
            }
            else
            {
                columnNumber = columnNumber + 1;
            }
            offset = offset + 1;
        }
        return true;
    }

    internal static SdpSourceLocation CreateUtf8FailureLocation(ReadOnlySpan<byte> input, SdpUtf8DecodeResult validation)
    {
        int byteOffset = validation.HasByteOrderMark && !validation.ByteOrderMarkNotAllowed ? 3 : 0;
        int lineNumber = 1;
        int columnNumber = 1;
        while (byteOffset < validation.ByteOffset)
        {
            byte value = input[byteOffset];
            if (value == 13)
            {
                if (byteOffset + 1 < validation.ByteOffset && input[byteOffset + 1] == 10)
                {
                    byteOffset = byteOffset + 1;
                }
                lineNumber = lineNumber + 1;
                columnNumber = 1;
            }
            else if (value == 10)
            {
                lineNumber = lineNumber + 1;
                columnNumber = 1;
            }
            else if (value >= 240)
            {
                byteOffset = byteOffset + 3;
                columnNumber = columnNumber + 2;
            }
            else if (value >= 224)
            {
                byteOffset = byteOffset + 2;
                columnNumber = columnNumber + 1;
            }
            else if (value >= 194)
            {
                byteOffset = byteOffset + 1;
                columnNumber = columnNumber + 1;
            }
            else
            {
                columnNumber = columnNumber + 1;
            }
            byteOffset = byteOffset + 1;
        }
        return CreateLocation(validation.CharacterOffset, validation.ByteOffset, lineNumber, columnNumber);
    }

    internal static SdpSourceLocation CreateLocation(int characterOffset, int byteOffset, int lineNumber, int columnNumber)
    {
        SdpSourceLocation result = new SdpSourceLocation();
        result.CharacterOffset = characterOffset;
        result.ByteOffset = byteOffset;
        result.LineNumber = lineNumber;
        result.ColumnNumber = columnNumber;
        return result;
    }

    internal static void ValidateOptions(SdpParseOptions options)
    {
        if (options.Mode != SdpParseMode.Strict && options.Mode != SdpParseMode.Interoperable && options.Mode != SdpParseMode.Lossless)
        {
            throw new ArgumentOutOfRangeException("options");
        }
        if (options.MaximumInputCharacters < 0 || options.MaximumInputBytes < 0 || options.MaximumLineLength < 0 || options.MaximumLineCount < 0 || options.MaximumMediaCount < 0 || options.MaximumAttributesPerScope < 0 || options.MaximumTokenLength < 0 || options.MaximumCandidateCount < 0 || options.MaximumDiagnosticCount < 0)
        {
            throw new ArgumentOutOfRangeException("options");
        }
    }

    internal static SdpParseOptions CopyOptions(SdpParseOptions options)
    {
        SdpParseOptions result = new SdpParseOptions();
        result.Mode = options.Mode;
        result.AllowByteOrderMark = options.AllowByteOrderMark;
        result.AllowLfLineEndings = options.AllowLfLineEndings;
        result.AllowCrLineEndings = options.AllowCrLineEndings;
        result.PreserveRawLines = options.PreserveRawLines;
        result.PreserveMalformedLines = options.PreserveMalformedLines;
        result.MaximumInputCharacters = options.MaximumInputCharacters;
        result.MaximumInputBytes = options.MaximumInputBytes;
        result.MaximumLineLength = options.MaximumLineLength;
        result.MaximumLineCount = options.MaximumLineCount;
        result.MaximumMediaCount = options.MaximumMediaCount;
        result.MaximumAttributesPerScope = options.MaximumAttributesPerScope;
        result.MaximumTokenLength = options.MaximumTokenLength;
        result.MaximumCandidateCount = options.MaximumCandidateCount;
        result.MaximumDiagnosticCount = options.MaximumDiagnosticCount;
        return result;
    }
}
