namespace json.Tests;

public class JsonOptionNormalizationTests
{
    [Test]
    public void NullOptionsAndLimitsNormalizeAsDefaultInstances()
    {
        JsonLimits limits = JsonOptionNormalizer.NormalizeLimits(null);
        JsonReaderOptions reader = JsonOptionNormalizer.NormalizeReaderOptions(null);
        JsonWriterOptions writer = JsonOptionNormalizer.NormalizeWriterOptions(null);

        Assert.Multiple(() =>
        {
            Assert.That(limits.MaximumDocumentBytes, Is.EqualTo(JsonDefaults.MaximumDocumentBytes));
            Assert.That(reader.StreamBufferBytes, Is.EqualTo(JsonDefaults.StreamBufferBytes));
            Assert.That(reader.Limits, Is.Not.Null);
            Assert.That(writer.Indentation, Is.EqualTo(JsonDefaults.Indentation));
            Assert.That(writer.NewLine, Is.EqualTo(JsonDefaults.NewLine));
            Assert.That(writer.Limits, Is.Not.Null);
        });
    }

    [Test]
    public void ZeroLimitsExpandToSafeDefaultsWithoutMutatingInput()
    {
        JsonLimits source = new JsonLimits();
        JsonLimits normalized = JsonOptionNormalizer.NormalizeLimits(source);

        Assert.Multiple(() =>
        {
            Assert.That(normalized, Is.Not.SameAs(source));
            Assert.That(source.MaximumDocumentBytes, Is.Zero);
            Assert.That(normalized.MaximumDocumentBytes, Is.EqualTo(JsonDefaults.MaximumDocumentBytes));
            Assert.That(normalized.MaximumInputBufferBytes, Is.EqualTo(JsonDefaults.MaximumInputBufferBytes));
            Assert.That(normalized.MaximumTokenBytes, Is.EqualTo(JsonDefaults.MaximumTokenBytes));
            Assert.That(normalized.MaximumDecodedStringBytes, Is.EqualTo(JsonDefaults.MaximumDecodedStringBytes));
            Assert.That(normalized.MaximumDecodedStringCodeUnits, Is.EqualTo(JsonDefaults.MaximumDecodedStringCodeUnits));
            Assert.That(normalized.MaximumNumberCharacters, Is.EqualTo(JsonDefaults.MaximumNumberCharacters));
            Assert.That(normalized.MaximumNestingDepth, Is.EqualTo(JsonDefaults.MaximumNestingDepth));
            Assert.That(normalized.MaximumArrayItems, Is.EqualTo(JsonDefaults.MaximumArrayItems));
            Assert.That(normalized.MaximumObjectMembers, Is.EqualTo(JsonDefaults.MaximumObjectMembers));
            Assert.That(normalized.MaximumDomNodes, Is.EqualTo(JsonDefaults.MaximumDomNodes));
            Assert.That(normalized.MaximumOutputBytes, Is.EqualTo(JsonDefaults.MaximumOutputBytes));
            Assert.That(normalized.MaximumDiagnosticExcerptBytes, Is.EqualTo(JsonDefaults.MaximumDiagnosticExcerptBytes));
        });
    }

    [Test]
    public void PositiveLimitsArePreserved()
    {
        JsonLimits source = new JsonLimits();
        source.MaximumDocumentBytes = 1;
        source.MaximumInputBufferBytes = 2;
        source.MaximumTokenBytes = 3;
        source.MaximumDecodedStringBytes = 4;
        source.MaximumDecodedStringCodeUnits = 5;
        source.MaximumNumberCharacters = 6;
        source.MaximumNestingDepth = 7;
        source.MaximumArrayItems = 8;
        source.MaximumObjectMembers = 9;
        source.MaximumDomNodes = 10;
        source.MaximumOutputBytes = 11;
        source.MaximumDiagnosticExcerptBytes = 12;

        JsonLimits normalized = JsonOptionNormalizer.NormalizeLimits(source);

        Assert.That(
            new[]
            {
                normalized.MaximumDocumentBytes,
                normalized.MaximumInputBufferBytes,
                normalized.MaximumTokenBytes,
                normalized.MaximumDecodedStringBytes,
                normalized.MaximumDecodedStringCodeUnits,
                normalized.MaximumNumberCharacters,
                normalized.MaximumNestingDepth,
                normalized.MaximumArrayItems,
                normalized.MaximumObjectMembers,
                normalized.MaximumDomNodes,
                normalized.MaximumOutputBytes,
                normalized.MaximumDiagnosticExcerptBytes
            },
            Is.EqualTo(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }));
    }

    [TestCase(nameof(JsonLimits.MaximumDocumentBytes))]
    [TestCase(nameof(JsonLimits.MaximumInputBufferBytes))]
    [TestCase(nameof(JsonLimits.MaximumTokenBytes))]
    [TestCase(nameof(JsonLimits.MaximumDecodedStringBytes))]
    [TestCase(nameof(JsonLimits.MaximumDecodedStringCodeUnits))]
    [TestCase(nameof(JsonLimits.MaximumNumberCharacters))]
    [TestCase(nameof(JsonLimits.MaximumNestingDepth))]
    [TestCase(nameof(JsonLimits.MaximumArrayItems))]
    [TestCase(nameof(JsonLimits.MaximumObjectMembers))]
    [TestCase(nameof(JsonLimits.MaximumDomNodes))]
    [TestCase(nameof(JsonLimits.MaximumOutputBytes))]
    [TestCase(nameof(JsonLimits.MaximumDiagnosticExcerptBytes))]
    public void EveryNegativeLimitIsRejected(string fieldName)
    {
        JsonLimits limits = new JsonLimits();
        SetLimit(limits, fieldName, -1);

        JsonLimitException exception = Assert.Throws<JsonLimitException>(() => JsonOptionNormalizer.NormalizeLimits(limits))
            ?? throw new InvalidOperationException("Expected JsonLimitException.");

        Assert.Multiple(() =>
        {
            Assert.That(exception.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InvalidOptions));
            Assert.That(exception.Message, Does.Contain(fieldName));
        });
    }

    [Test]
    public void ReaderDefaultsAreExpandedIntoAnIndependentCopy()
    {
        JsonReaderOptions source = new JsonReaderOptions();
        JsonReaderOptions normalized = JsonOptionNormalizer.NormalizeReaderOptions(source);

        Assert.Multiple(() =>
        {
            Assert.That(normalized, Is.Not.SameAs(source));
            Assert.That(source.StreamBufferBytes, Is.Zero);
            Assert.That(source.Limits, Is.Null);
            Assert.That(normalized.StreamBufferBytes, Is.EqualTo(JsonDefaults.StreamBufferBytes));
            Assert.That(normalized.Limits, Is.Not.Null);
            Assert.That(normalized.Limits?.MaximumDocumentBytes, Is.EqualTo(JsonDefaults.MaximumDocumentBytes));
            Assert.That(normalized.ConformanceProfile, Is.EqualTo(JsonConformanceProfile.SyntaxCompatible));
            Assert.That(normalized.BomPolicy, Is.EqualTo(JsonBomPolicy.Reject));
        });
    }

    [Test]
    public void CompatiblePortableReaderProfileIsAccepted()
    {
        JsonReaderOptions source = new JsonReaderOptions();
        source.ConformanceProfile = JsonConformanceProfile.Portable;
        source.DuplicateKeyPolicy = JsonDuplicateKeyPolicy.Reject;
        source.UnicodePolicy = JsonUnicodePolicy.UnicodeScalar;
        source.BomPolicy = JsonBomPolicy.Reject;
        source.TrackLineInformation = true;
        source.TrackUnicodeScalarColumn = true;

        JsonReaderOptions normalized = JsonOptionNormalizer.NormalizeReaderOptions(source);

        Assert.Multiple(() =>
        {
            Assert.That(normalized.ConformanceProfile, Is.EqualTo(JsonConformanceProfile.Portable));
            Assert.That(normalized.DuplicateKeyPolicy, Is.EqualTo(JsonDuplicateKeyPolicy.Reject));
            Assert.That(normalized.UnicodePolicy, Is.EqualTo(JsonUnicodePolicy.UnicodeScalar));
            Assert.That(normalized.TrackUnicodeScalarColumn, Is.True);
        });
    }

    [TestCase(JsonConformanceProfile.Portable)]
    [TestCase(JsonConformanceProfile.IJson)]
    [TestCase(JsonConformanceProfile.Jcs)]
    public void StrictReaderProfilesAcceptTheirRequiredPolicies(JsonConformanceProfile profile)
    {
        JsonReaderOptions source = CreateStrictReaderOptions(profile);

        JsonReaderOptions normalized = JsonOptionNormalizer.NormalizeReaderOptions(source);

        Assert.Multiple(() =>
        {
            Assert.That(normalized.ConformanceProfile, Is.EqualTo(profile));
            Assert.That(normalized.DuplicateKeyPolicy, Is.EqualTo(JsonDuplicateKeyPolicy.Reject));
            Assert.That(normalized.UnicodePolicy, Is.EqualTo(JsonUnicodePolicy.UnicodeScalar));
            Assert.That(normalized.BomPolicy, Is.EqualTo(JsonBomPolicy.Reject));
        });
    }

    [TestCase(JsonConformanceProfile.Portable, nameof(JsonReaderOptions.DuplicateKeyPolicy))]
    [TestCase(JsonConformanceProfile.Portable, nameof(JsonReaderOptions.UnicodePolicy))]
    [TestCase(JsonConformanceProfile.Portable, nameof(JsonReaderOptions.BomPolicy))]
    [TestCase(JsonConformanceProfile.IJson, nameof(JsonReaderOptions.DuplicateKeyPolicy))]
    [TestCase(JsonConformanceProfile.IJson, nameof(JsonReaderOptions.UnicodePolicy))]
    [TestCase(JsonConformanceProfile.IJson, nameof(JsonReaderOptions.BomPolicy))]
    [TestCase(JsonConformanceProfile.Jcs, nameof(JsonReaderOptions.DuplicateKeyPolicy))]
    [TestCase(JsonConformanceProfile.Jcs, nameof(JsonReaderOptions.UnicodePolicy))]
    [TestCase(JsonConformanceProfile.Jcs, nameof(JsonReaderOptions.BomPolicy))]
    public void StrictReaderProfilesRejectEachPolicyMismatch(JsonConformanceProfile profile, string fieldName)
    {
        JsonReaderOptions source = CreateStrictReaderOptions(profile);

        switch (fieldName)
        {
            case nameof(JsonReaderOptions.DuplicateKeyPolicy):
                source.DuplicateKeyPolicy = JsonDuplicateKeyPolicy.Preserve;
                break;
            case nameof(JsonReaderOptions.UnicodePolicy):
                source.UnicodePolicy = JsonUnicodePolicy.SyntaxCompatible;
                break;
            case nameof(JsonReaderOptions.BomPolicy):
                source.BomPolicy = JsonBomPolicy.AllowInitial;
                break;
            default:
                throw new InvalidOperationException(fieldName);
        }

        JsonParseException exception = ExpectReaderFailure(source);

        Assert.That(exception.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InvalidOptions));
    }

    [TestCase(JsonInputMode.SingleValue)]
    [TestCase(JsonInputMode.WhitespaceSeparated)]
    public void SequenceRecoveryRejectsEveryNonSequenceInputMode(JsonInputMode inputMode)
    {
        JsonReaderOptions source = new JsonReaderOptions();
        source.InputMode = inputMode;
        source.SequenceErrorPolicy = JsonSequenceErrorPolicy.Resynchronize;

        JsonParseException exception = ExpectReaderFailure(source);

        Assert.That(exception.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InvalidOptions));
    }

    [TestCase(JsonInputMode.WhitespaceSeparated)]
    [TestCase(JsonInputMode.JsonTextSequence)]
    public void JcsRejectsEveryMultipleValueInputMode(JsonInputMode inputMode)
    {
        JsonReaderOptions source = CreateStrictReaderOptions(JsonConformanceProfile.Jcs);
        source.InputMode = inputMode;

        JsonParseException exception = ExpectReaderFailure(source);

        Assert.That(exception.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InvalidOptions));
    }

    [TestCase(nameof(JsonReaderOptions.ConformanceProfile))]
    [TestCase(nameof(JsonReaderOptions.DuplicateKeyPolicy))]
    [TestCase(nameof(JsonReaderOptions.InputMode))]
    [TestCase(nameof(JsonReaderOptions.SequenceErrorPolicy))]
    [TestCase(nameof(JsonReaderOptions.UnicodePolicy))]
    [TestCase(nameof(JsonReaderOptions.BomPolicy))]
    public void EveryUndefinedReaderEnumValueIsRejected(string fieldName)
    {
        JsonReaderOptions source = new JsonReaderOptions();
        SetUndefinedReaderEnum(source, fieldName);

        JsonParseException exception = ExpectReaderFailure(source);

        Assert.That(exception.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InvalidOptions));
    }

    [Test]
    public void IncompatibleReaderOptionsAreRejected()
    {
        JsonReaderOptions scalarWithoutLines = new JsonReaderOptions();
        scalarWithoutLines.TrackUnicodeScalarColumn = true;
        JsonReaderOptions recoveryWithoutSequence = new JsonReaderOptions();
        recoveryWithoutSequence.SequenceErrorPolicy = JsonSequenceErrorPolicy.Resynchronize;
        JsonReaderOptions incompatibleProfile = new JsonReaderOptions();
        incompatibleProfile.ConformanceProfile = JsonConformanceProfile.IJson;
        JsonReaderOptions jcsSequence = new JsonReaderOptions();
        jcsSequence.ConformanceProfile = JsonConformanceProfile.Jcs;
        jcsSequence.DuplicateKeyPolicy = JsonDuplicateKeyPolicy.Reject;
        jcsSequence.UnicodePolicy = JsonUnicodePolicy.UnicodeScalar;
        jcsSequence.InputMode = JsonInputMode.WhitespaceSeparated;
        JsonReaderOptions undefined = new JsonReaderOptions();
        undefined.InputMode = (JsonInputMode)99;

        JsonParseException scalarException = ExpectReaderFailure(scalarWithoutLines);
        JsonParseException recoveryException = ExpectReaderFailure(recoveryWithoutSequence);
        JsonParseException profileException = ExpectReaderFailure(incompatibleProfile);
        JsonParseException jcsException = ExpectReaderFailure(jcsSequence);
        JsonParseException undefinedException = ExpectReaderFailure(undefined);

        Assert.Multiple(() =>
        {
            Assert.That(scalarException.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InvalidOptions));
            Assert.That(recoveryException.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InvalidOptions));
            Assert.That(profileException.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InvalidOptions));
            Assert.That(jcsException.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InvalidOptions));
            Assert.That(undefinedException.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InvalidOptions));
        });
    }

    [Test]
    public void NegativeReaderBufferSizeIsRejectedAsALimitFailure()
    {
        JsonReaderOptions source = new JsonReaderOptions();
        source.StreamBufferBytes = -1;

        JsonLimitException exception = Assert.Throws<JsonLimitException>(() => JsonOptionNormalizer.NormalizeReaderOptions(source))
            ?? throw new InvalidOperationException("Expected JsonLimitException.");

        Assert.That(exception.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InvalidOptions));
    }

    [Test]
    public void WriterDefaultsAndCustomFieldsAreNormalizedWithoutMutatingInput()
    {
        JsonWriterOptions defaults = new JsonWriterOptions();
        JsonWriterOptions normalizedDefaults = JsonOptionNormalizer.NormalizeWriterOptions(defaults);
        JsonWriterOptions custom = new JsonWriterOptions();
        custom.Formatting = JsonFormatting.Pretty;
        custom.StringEscapeMode = JsonStringEscapeMode.AsciiOnly;
        custom.PropertyOrder = JsonPropertyOrder.OrdinalUtf16;
        custom.DuplicateKeyPolicy = JsonDuplicateKeyPolicy.KeepFirst;
        custom.UnicodePolicy = JsonUnicodePolicy.UnicodeScalar;
        custom.OutputMode = JsonInputMode.JsonTextSequence;
        custom.EscapeSolidus = true;
        custom.UseUppercaseHexadecimalEscapes = true;
        custom.SkipModelValidation = true;
        custom.OmitSpaceAfterColon = true;
        custom.Indentation = "\t";
        custom.NewLine = "\r\n";
        JsonWriterOptions normalizedCustom = JsonOptionNormalizer.NormalizeWriterOptions(custom);

        Assert.Multiple(() =>
        {
            Assert.That(defaults.Indentation, Is.Null);
            Assert.That(defaults.NewLine, Is.Null);
            Assert.That(defaults.Limits, Is.Null);
            Assert.That(normalizedDefaults.Indentation, Is.EqualTo(JsonDefaults.Indentation));
            Assert.That(normalizedDefaults.NewLine, Is.EqualTo(JsonDefaults.NewLine));
            Assert.That(normalizedDefaults.Limits, Is.Not.Null);
            Assert.That(normalizedCustom.Formatting, Is.EqualTo(JsonFormatting.Pretty));
            Assert.That(normalizedCustom.StringEscapeMode, Is.EqualTo(JsonStringEscapeMode.AsciiOnly));
            Assert.That(normalizedCustom.PropertyOrder, Is.EqualTo(JsonPropertyOrder.OrdinalUtf16));
            Assert.That(normalizedCustom.DuplicateKeyPolicy, Is.EqualTo(JsonDuplicateKeyPolicy.KeepFirst));
            Assert.That(normalizedCustom.UnicodePolicy, Is.EqualTo(JsonUnicodePolicy.UnicodeScalar));
            Assert.That(normalizedCustom.OutputMode, Is.EqualTo(JsonInputMode.JsonTextSequence));
            Assert.That(normalizedCustom.EscapeSolidus, Is.True);
            Assert.That(normalizedCustom.UseUppercaseHexadecimalEscapes, Is.True);
            Assert.That(normalizedCustom.SkipModelValidation, Is.True);
            Assert.That(normalizedCustom.OmitSpaceAfterColon, Is.True);
            Assert.That(normalizedCustom.Indentation, Is.EqualTo("\t"));
            Assert.That(normalizedCustom.NewLine, Is.EqualTo("\r\n"));
        });
    }

    [Test]
    public void UndefinedWriterEnumIsRejected()
    {
        JsonWriterOptions source = new JsonWriterOptions();
        source.PropertyOrder = (JsonPropertyOrder)99;

        JsonWriteException exception = Assert.Throws<JsonWriteException>(() => JsonOptionNormalizer.NormalizeWriterOptions(source))
            ?? throw new InvalidOperationException("Expected JsonWriteException.");

        Assert.That(exception.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InvalidOptions));
    }

    [TestCase(nameof(JsonWriterOptions.Formatting))]
    [TestCase(nameof(JsonWriterOptions.StringEscapeMode))]
    [TestCase(nameof(JsonWriterOptions.PropertyOrder))]
    [TestCase(nameof(JsonWriterOptions.DuplicateKeyPolicy))]
    [TestCase(nameof(JsonWriterOptions.UnicodePolicy))]
    [TestCase(nameof(JsonWriterOptions.OutputMode))]
    public void EveryUndefinedWriterEnumValueIsRejected(string fieldName)
    {
        JsonWriterOptions source = new JsonWriterOptions();
        SetUndefinedWriterEnum(source, fieldName);

        JsonWriteException exception = Assert.Throws<JsonWriteException>(() => JsonOptionNormalizer.NormalizeWriterOptions(source))
            ?? throw new InvalidOperationException("Expected JsonWriteException.");

        Assert.That(exception.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InvalidOptions));
    }

    [Test]
    public void WriterTargetRequiresExactlyOneDestination()
    {
        JsonWriterTarget empty = new JsonWriterTarget();
        JsonWriterTarget both = new JsonWriterTarget();
        using MemoryStream bothStream = new MemoryStream();
        both.Stream = bothStream;
        both.BufferWriter = new global::System.Buffers.ArrayBufferWriter<byte>();

        JsonWriteException missingTarget = Assert.Throws<JsonWriteException>(() => JsonOptionNormalizer.NormalizeWriterTarget(null))
            ?? throw new InvalidOperationException("Expected JsonWriteException.");
        JsonWriteException emptyTarget = Assert.Throws<JsonWriteException>(() => JsonOptionNormalizer.NormalizeWriterTarget(empty))
            ?? throw new InvalidOperationException("Expected JsonWriteException.");
        JsonWriteException bothTarget = Assert.Throws<JsonWriteException>(() => JsonOptionNormalizer.NormalizeWriterTarget(both))
            ?? throw new InvalidOperationException("Expected JsonWriteException.");

        using MemoryStream stream = new MemoryStream();
        JsonWriterTarget streamTarget = new JsonWriterTarget();
        streamTarget.Stream = stream;
        JsonWriterTarget bufferTarget = new JsonWriterTarget();
        bufferTarget.BufferWriter = new global::System.Buffers.ArrayBufferWriter<byte>();

        Assert.Multiple(() =>
        {
            Assert.That(missingTarget.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InvalidOptions));
            Assert.That(emptyTarget.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InvalidOptions));
            Assert.That(bothTarget.Diagnostic.Code, Is.EqualTo(JsonDiagnosticCode.InvalidOptions));
            Assert.That(JsonOptionNormalizer.NormalizeWriterTarget(streamTarget), Is.SameAs(streamTarget));
            Assert.That(JsonOptionNormalizer.NormalizeWriterTarget(bufferTarget), Is.SameAs(bufferTarget));
        });
    }

    private static JsonParseException ExpectReaderFailure(JsonReaderOptions options)
    {
        return Assert.Throws<JsonParseException>(() => JsonOptionNormalizer.NormalizeReaderOptions(options))
            ?? throw new InvalidOperationException("Expected JsonParseException.");
    }

    private static JsonReaderOptions CreateStrictReaderOptions(JsonConformanceProfile profile)
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.ConformanceProfile = profile;
        options.DuplicateKeyPolicy = JsonDuplicateKeyPolicy.Reject;
        options.UnicodePolicy = JsonUnicodePolicy.UnicodeScalar;
        options.BomPolicy = JsonBomPolicy.Reject;
        return options;
    }

    private static void SetUndefinedReaderEnum(JsonReaderOptions options, string fieldName)
    {
        switch (fieldName)
        {
            case nameof(JsonReaderOptions.ConformanceProfile):
                options.ConformanceProfile = (JsonConformanceProfile)99;
                break;
            case nameof(JsonReaderOptions.DuplicateKeyPolicy):
                options.DuplicateKeyPolicy = (JsonDuplicateKeyPolicy)99;
                break;
            case nameof(JsonReaderOptions.InputMode):
                options.InputMode = (JsonInputMode)99;
                break;
            case nameof(JsonReaderOptions.SequenceErrorPolicy):
                options.SequenceErrorPolicy = (JsonSequenceErrorPolicy)99;
                break;
            case nameof(JsonReaderOptions.UnicodePolicy):
                options.UnicodePolicy = (JsonUnicodePolicy)99;
                break;
            case nameof(JsonReaderOptions.BomPolicy):
                options.BomPolicy = (JsonBomPolicy)99;
                break;
            default:
                throw new InvalidOperationException(fieldName);
        }
    }

    private static void SetUndefinedWriterEnum(JsonWriterOptions options, string fieldName)
    {
        switch (fieldName)
        {
            case nameof(JsonWriterOptions.Formatting):
                options.Formatting = (JsonFormatting)99;
                break;
            case nameof(JsonWriterOptions.StringEscapeMode):
                options.StringEscapeMode = (JsonStringEscapeMode)99;
                break;
            case nameof(JsonWriterOptions.PropertyOrder):
                options.PropertyOrder = (JsonPropertyOrder)99;
                break;
            case nameof(JsonWriterOptions.DuplicateKeyPolicy):
                options.DuplicateKeyPolicy = (JsonDuplicateKeyPolicy)99;
                break;
            case nameof(JsonWriterOptions.UnicodePolicy):
                options.UnicodePolicy = (JsonUnicodePolicy)99;
                break;
            case nameof(JsonWriterOptions.OutputMode):
                options.OutputMode = (JsonInputMode)99;
                break;
            default:
                throw new InvalidOperationException(fieldName);
        }
    }

    private static void SetLimit(JsonLimits limits, string fieldName, int value)
    {
        switch (fieldName)
        {
            case nameof(JsonLimits.MaximumDocumentBytes):
                limits.MaximumDocumentBytes = value;
                break;
            case nameof(JsonLimits.MaximumInputBufferBytes):
                limits.MaximumInputBufferBytes = value;
                break;
            case nameof(JsonLimits.MaximumTokenBytes):
                limits.MaximumTokenBytes = value;
                break;
            case nameof(JsonLimits.MaximumDecodedStringBytes):
                limits.MaximumDecodedStringBytes = value;
                break;
            case nameof(JsonLimits.MaximumDecodedStringCodeUnits):
                limits.MaximumDecodedStringCodeUnits = value;
                break;
            case nameof(JsonLimits.MaximumNumberCharacters):
                limits.MaximumNumberCharacters = value;
                break;
            case nameof(JsonLimits.MaximumNestingDepth):
                limits.MaximumNestingDepth = value;
                break;
            case nameof(JsonLimits.MaximumArrayItems):
                limits.MaximumArrayItems = value;
                break;
            case nameof(JsonLimits.MaximumObjectMembers):
                limits.MaximumObjectMembers = value;
                break;
            case nameof(JsonLimits.MaximumDomNodes):
                limits.MaximumDomNodes = value;
                break;
            case nameof(JsonLimits.MaximumOutputBytes):
                limits.MaximumOutputBytes = value;
                break;
            case nameof(JsonLimits.MaximumDiagnosticExcerptBytes):
                limits.MaximumDiagnosticExcerptBytes = value;
                break;
            default:
                throw new InvalidOperationException(fieldName);
        }
    }
}
