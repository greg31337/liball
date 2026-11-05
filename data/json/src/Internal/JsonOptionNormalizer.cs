namespace json;

internal static class JsonOptionNormalizer
{
    internal static JsonLimits NormalizeLimits(JsonLimits? limits)
    {
        JsonLimits source = limits ?? new JsonLimits();
        JsonLimits normalized = new JsonLimits();

        RejectNegativeLimit(source.MaximumDocumentBytes, "MaximumDocumentBytes");
        RejectNegativeLimit(source.MaximumInputBufferBytes, "MaximumInputBufferBytes");
        RejectNegativeLimit(source.MaximumTokenBytes, "MaximumTokenBytes");
        RejectNegativeLimit(source.MaximumDecodedStringBytes, "MaximumDecodedStringBytes");
        RejectNegativeLimit(source.MaximumDecodedStringCodeUnits, "MaximumDecodedStringCodeUnits");
        RejectNegativeLimit(source.MaximumNumberCharacters, "MaximumNumberCharacters");
        RejectNegativeLimit(source.MaximumNestingDepth, "MaximumNestingDepth");
        RejectNegativeLimit(source.MaximumArrayItems, "MaximumArrayItems");
        RejectNegativeLimit(source.MaximumObjectMembers, "MaximumObjectMembers");
        RejectNegativeLimit(source.MaximumDomNodes, "MaximumDomNodes");
        RejectNegativeLimit(source.MaximumOutputBytes, "MaximumOutputBytes");
        RejectNegativeLimit(source.MaximumDiagnosticExcerptBytes, "MaximumDiagnosticExcerptBytes");

        normalized.MaximumDocumentBytes = source.MaximumDocumentBytes == 0 ? JsonDefaults.MaximumDocumentBytes : source.MaximumDocumentBytes;
        normalized.MaximumInputBufferBytes = source.MaximumInputBufferBytes == 0 ? JsonDefaults.MaximumInputBufferBytes : source.MaximumInputBufferBytes;
        normalized.MaximumTokenBytes = source.MaximumTokenBytes == 0 ? JsonDefaults.MaximumTokenBytes : source.MaximumTokenBytes;
        normalized.MaximumDecodedStringBytes = source.MaximumDecodedStringBytes == 0 ? JsonDefaults.MaximumDecodedStringBytes : source.MaximumDecodedStringBytes;
        normalized.MaximumDecodedStringCodeUnits = source.MaximumDecodedStringCodeUnits == 0 ? JsonDefaults.MaximumDecodedStringCodeUnits : source.MaximumDecodedStringCodeUnits;
        normalized.MaximumNumberCharacters = source.MaximumNumberCharacters == 0 ? JsonDefaults.MaximumNumberCharacters : source.MaximumNumberCharacters;
        normalized.MaximumNestingDepth = source.MaximumNestingDepth == 0 ? JsonDefaults.MaximumNestingDepth : source.MaximumNestingDepth;
        normalized.MaximumArrayItems = source.MaximumArrayItems == 0 ? JsonDefaults.MaximumArrayItems : source.MaximumArrayItems;
        normalized.MaximumObjectMembers = source.MaximumObjectMembers == 0 ? JsonDefaults.MaximumObjectMembers : source.MaximumObjectMembers;
        normalized.MaximumDomNodes = source.MaximumDomNodes == 0 ? JsonDefaults.MaximumDomNodes : source.MaximumDomNodes;
        normalized.MaximumOutputBytes = source.MaximumOutputBytes == 0 ? JsonDefaults.MaximumOutputBytes : source.MaximumOutputBytes;
        normalized.MaximumDiagnosticExcerptBytes = source.MaximumDiagnosticExcerptBytes == 0 ? JsonDefaults.MaximumDiagnosticExcerptBytes : source.MaximumDiagnosticExcerptBytes;
        return normalized;
    }

    internal static JsonReaderOptions NormalizeReaderOptions(JsonReaderOptions? options)
    {
        JsonReaderOptions source = options ?? new JsonReaderOptions();

        if (source.ConformanceProfile != JsonConformanceProfile.SyntaxCompatible &&
            source.ConformanceProfile != JsonConformanceProfile.Portable &&
            source.ConformanceProfile != JsonConformanceProfile.IJson &&
            source.ConformanceProfile != JsonConformanceProfile.Jcs)
        {
            throw new JsonParseException(CreateReaderDiagnostic("ConformanceProfile has an undefined value."));
        }

        if (source.DuplicateKeyPolicy != JsonDuplicateKeyPolicy.Preserve &&
            source.DuplicateKeyPolicy != JsonDuplicateKeyPolicy.Reject &&
            source.DuplicateKeyPolicy != JsonDuplicateKeyPolicy.KeepFirst &&
            source.DuplicateKeyPolicy != JsonDuplicateKeyPolicy.KeepLast)
        {
            throw new JsonParseException(CreateReaderDiagnostic("DuplicateKeyPolicy has an undefined value."));
        }

        if (source.InputMode != JsonInputMode.SingleValue &&
            source.InputMode != JsonInputMode.WhitespaceSeparated &&
            source.InputMode != JsonInputMode.JsonTextSequence)
        {
            throw new JsonParseException(CreateReaderDiagnostic("InputMode has an undefined value."));
        }

        if (source.SequenceErrorPolicy != JsonSequenceErrorPolicy.Stop &&
            source.SequenceErrorPolicy != JsonSequenceErrorPolicy.Resynchronize)
        {
            throw new JsonParseException(CreateReaderDiagnostic("SequenceErrorPolicy has an undefined value."));
        }

        if (source.UnicodePolicy != JsonUnicodePolicy.SyntaxCompatible &&
            source.UnicodePolicy != JsonUnicodePolicy.UnicodeScalar)
        {
            throw new JsonParseException(CreateReaderDiagnostic("UnicodePolicy has an undefined value."));
        }

        if (source.BomPolicy != JsonBomPolicy.Reject && source.BomPolicy != JsonBomPolicy.AllowInitial)
        {
            throw new JsonParseException(CreateReaderDiagnostic("BomPolicy has an undefined value."));
        }

        if (source.StreamBufferBytes < 0)
        {
            throw new JsonLimitException(CreateLimitDiagnostic("StreamBufferBytes cannot be negative."));
        }

        if (source.TrackUnicodeScalarColumn && !source.TrackLineInformation)
        {
            throw new JsonParseException(CreateReaderDiagnostic("TrackUnicodeScalarColumn requires TrackLineInformation."));
        }

        if (source.SequenceErrorPolicy == JsonSequenceErrorPolicy.Resynchronize && source.InputMode != JsonInputMode.JsonTextSequence)
        {
            throw new JsonParseException(CreateReaderDiagnostic("Resynchronize is valid only for JSON text sequence input."));
        }

        if (source.ConformanceProfile == JsonConformanceProfile.Portable ||
            source.ConformanceProfile == JsonConformanceProfile.IJson ||
            source.ConformanceProfile == JsonConformanceProfile.Jcs)
        {
            if (source.DuplicateKeyPolicy != JsonDuplicateKeyPolicy.Reject ||
                source.UnicodePolicy != JsonUnicodePolicy.UnicodeScalar ||
                source.BomPolicy != JsonBomPolicy.Reject)
            {
                throw new JsonParseException(CreateReaderDiagnostic("The selected conformance profile requires rejected duplicates, Unicode scalar strings, and BOM rejection."));
            }
        }

        if (source.ConformanceProfile == JsonConformanceProfile.Jcs && source.InputMode != JsonInputMode.SingleValue)
        {
            throw new JsonParseException(CreateReaderDiagnostic("The JCS profile requires single-value input."));
        }

        JsonReaderOptions normalized = new JsonReaderOptions();
        normalized.ConformanceProfile = source.ConformanceProfile;
        normalized.DuplicateKeyPolicy = source.DuplicateKeyPolicy;
        normalized.InputMode = source.InputMode;
        normalized.SequenceErrorPolicy = source.SequenceErrorPolicy;
        normalized.UnicodePolicy = source.UnicodePolicy;
        normalized.BomPolicy = source.BomPolicy;
        normalized.TrackLineInformation = source.TrackLineInformation;
        normalized.TrackUnicodeScalarColumn = source.TrackUnicodeScalarColumn;
        normalized.CaptureRawTokens = source.CaptureRawTokens;
        normalized.CaptureSourceRanges = source.CaptureSourceRanges;
        normalized.StreamBufferBytes = source.StreamBufferBytes == 0 ? JsonDefaults.StreamBufferBytes : source.StreamBufferBytes;
        normalized.Limits = NormalizeLimits(source.Limits);
        return normalized;
    }

    internal static JsonWriterOptions NormalizeWriterOptions(JsonWriterOptions? options)
    {
        JsonWriterOptions source = options ?? new JsonWriterOptions();
        string indentation = source.Indentation ?? JsonDefaults.Indentation;
        string newLine = source.NewLine ?? JsonDefaults.NewLine;

        if (source.Formatting != JsonFormatting.Compact && source.Formatting != JsonFormatting.Pretty)
        {
            throw new JsonWriteException(CreateWriterDiagnostic("Formatting has an undefined value."));
        }

        if (source.StringEscapeMode != JsonStringEscapeMode.Minimal &&
            source.StringEscapeMode != JsonStringEscapeMode.AsciiOnly &&
            source.StringEscapeMode != JsonStringEscapeMode.HtmlSafe)
        {
            throw new JsonWriteException(CreateWriterDiagnostic("StringEscapeMode has an undefined value."));
        }

        if (source.PropertyOrder != JsonPropertyOrder.Preserve &&
            source.PropertyOrder != JsonPropertyOrder.OrdinalUtf16 &&
            source.PropertyOrder != JsonPropertyOrder.OrdinalUnicodeScalar &&
            source.PropertyOrder != JsonPropertyOrder.Jcs)
        {
            throw new JsonWriteException(CreateWriterDiagnostic("PropertyOrder has an undefined value."));
        }

        if (source.DuplicateKeyPolicy != JsonDuplicateKeyPolicy.Preserve &&
            source.DuplicateKeyPolicy != JsonDuplicateKeyPolicy.Reject &&
            source.DuplicateKeyPolicy != JsonDuplicateKeyPolicy.KeepFirst &&
            source.DuplicateKeyPolicy != JsonDuplicateKeyPolicy.KeepLast)
        {
            throw new JsonWriteException(CreateWriterDiagnostic("DuplicateKeyPolicy has an undefined value."));
        }

        if (source.UnicodePolicy != JsonUnicodePolicy.SyntaxCompatible &&
            source.UnicodePolicy != JsonUnicodePolicy.UnicodeScalar)
        {
            throw new JsonWriteException(CreateWriterDiagnostic("UnicodePolicy has an undefined value."));
        }

        if (source.OutputMode != JsonInputMode.SingleValue &&
            source.OutputMode != JsonInputMode.WhitespaceSeparated &&
            source.OutputMode != JsonInputMode.JsonTextSequence)
        {
            throw new JsonWriteException(CreateWriterDiagnostic("OutputMode has an undefined value."));
        }

        if (!IsWriterIndentationValid(indentation))
        {
            throw new JsonWriteException(CreateWriterDiagnostic(
                "Indentation can contain only ASCII space and horizontal tab characters."));
        }

        if (!IsWriterNewLineValid(newLine))
        {
            throw new JsonWriteException(CreateWriterDiagnostic(
                "NewLine must be LF or CRLF."));
        }

        JsonWriterOptions normalized = new JsonWriterOptions();
        normalized.Formatting = source.Formatting;
        normalized.StringEscapeMode = source.StringEscapeMode;
        normalized.PropertyOrder = source.PropertyOrder;
        normalized.DuplicateKeyPolicy = source.DuplicateKeyPolicy;
        normalized.UnicodePolicy = source.UnicodePolicy;
        normalized.OutputMode = source.OutputMode;
        normalized.EscapeSolidus = source.EscapeSolidus;
        normalized.UseUppercaseHexadecimalEscapes = source.UseUppercaseHexadecimalEscapes;
        normalized.SkipModelValidation = source.SkipModelValidation;
        normalized.OmitSpaceAfterColon = source.OmitSpaceAfterColon;
        normalized.Indentation = indentation;
        normalized.NewLine = newLine;
        normalized.Limits = NormalizeLimits(source.Limits);
        return normalized;
    }

    static bool IsWriterIndentationValid(string indentation)
    {
        int index = 0;
        while (index < indentation.Length)
        {
            char value = indentation[index];
            if (value != ' ' && value != '\t')
            {
                return false;
            }

            index++;
        }

        return true;
    }

    static bool IsWriterNewLineValid(string newLine)
    {
        return newLine == "\n" || newLine == "\r\n";
    }

    internal static JsonWriterTarget NormalizeWriterTarget(JsonWriterTarget? target)
    {
        if (target == null)
        {
            throw new JsonWriteException(CreateWriterDiagnostic("A writer target is required."));
        }

        if (target.Stream == null && target.BufferWriter == null)
        {
            throw new JsonWriteException(CreateWriterDiagnostic("A writer target must contain one destination."));
        }

        if (target.Stream != null && target.BufferWriter != null)
        {
            throw new JsonWriteException(CreateWriterDiagnostic("A writer target cannot contain both destinations."));
        }

        return target;
    }

    internal static void RejectNegativeLimit(int value, string name)
    {
        if (value < 0)
        {
            throw new JsonLimitException(CreateLimitDiagnostic($"{name} cannot be negative."));
        }
    }

    internal static JsonDiagnostic CreateLimitDiagnostic(string message)
    {
        JsonDiagnostic diagnostic = new JsonDiagnostic();
        diagnostic.Code = JsonDiagnosticCode.InvalidOptions;
        diagnostic.Message = message;
        return diagnostic;
    }

    internal static JsonDiagnostic CreateReaderDiagnostic(string message)
    {
        JsonDiagnostic diagnostic = new JsonDiagnostic();
        diagnostic.Code = JsonDiagnosticCode.InvalidOptions;
        diagnostic.Message = message;
        return diagnostic;
    }

    internal static JsonDiagnostic CreateWriterDiagnostic(string message)
    {
        JsonDiagnostic diagnostic = new JsonDiagnostic();
        diagnostic.Code = JsonDiagnosticCode.InvalidOptions;
        diagnostic.Message = message;
        return diagnostic;
    }
}
