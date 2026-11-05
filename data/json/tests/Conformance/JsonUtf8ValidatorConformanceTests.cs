namespace json.Tests;

public class JsonUtf8ValidatorConformanceTests
{
    [TestCase("null")]
    [TestCase("false")]
    [TestCase("true")]
    [TestCase("-12.5e+2")]
    [TestCase("\"text\"")]
    [TestCase("[]")]
    [TestCase("{}")]
    [TestCase("{\"a\":[null,true,0,\"x\",{},[]]}")]
    public void EveryJsonRootKindValidatesThroughTheInterface(string jsonText)
    {
        IJsonUtf8Validator validator = new JsonUtf8Validator();
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes(jsonText);

        AssertSuccess(validator.ValidateUtf8(utf8, new JsonReaderOptions()), utf8.Length);
    }

    [TestCase("", JsonDiagnosticCode.UnexpectedEndOfInput)]
    [TestCase("/", JsonDiagnosticCode.UnexpectedCharacter)]
    [TestCase("truE", JsonDiagnosticCode.InvalidLiteral)]
    [TestCase("01", JsonDiagnosticCode.InvalidNumber)]
    [TestCase("\"unterminated", JsonDiagnosticCode.UnexpectedEndOfInput)]
    [TestCase("[0,]", JsonDiagnosticCode.TrailingComma)]
    [TestCase("{\"a\" 0}", JsonDiagnosticCode.MissingColon)]
    [TestCase("null true", JsonDiagnosticCode.TrailingContent)]
    public void RepresentativeInvalidInputsReturnSpecificDiagnostics(
        string jsonText,
        JsonDiagnosticCode expectedCode)
    {
        IJsonUtf8Validator validator = new JsonUtf8Validator();
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes(jsonText);

        AssertFailure(
            validator.ValidateUtf8(utf8, new JsonReaderOptions()),
            expectedCode,
            utf8.Length);
    }

    [Test]
    public void MalformedUtf8ReturnsAValidationFailure()
    {
        IJsonUtf8Validator validator = new JsonUtf8Validator();
        byte[] utf8 = new byte[]
        {
            JsonSyntaxBytes.StringDelimiter,
            0xF0,
            0x80,
            0x80,
            0x80,
            JsonSyntaxBytes.StringDelimiter
        };

        AssertFailure(
            validator.ValidateUtf8(utf8, new JsonReaderOptions()),
            JsonDiagnosticCode.InvalidUtf8,
            utf8.Length);
    }

    [TestCase(JsonInputMode.SingleValue, "{}")]
    [TestCase(JsonInputMode.WhitespaceSeparated, "null [] {\"a\":0}")]
    [TestCase(JsonInputMode.JsonTextSequence, "\u001e{}\n\u001e1\n\u001e\"x\"\n")]
    public void EveryFramingModeAcceptsItsCompleteOneShotForm(
        JsonInputMode inputMode,
        string framedText)
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.InputMode = inputMode;
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes(framedText);
        IJsonUtf8Validator validator = new JsonUtf8Validator();

        AssertSuccess(validator.ValidateUtf8(utf8, options), utf8.Length);
    }

    [TestCase(JsonInputMode.SingleValue, "null true", JsonDiagnosticCode.TrailingContent)]
    [TestCase(JsonInputMode.WhitespaceSeparated, "{}[]", JsonDiagnosticCode.UnexpectedToken)]
    [TestCase(JsonInputMode.JsonTextSequence, "{}", JsonDiagnosticCode.MissingRecordSeparator)]
    public void EveryFramingModeRejectsItsInvalidBoundary(
        JsonInputMode inputMode,
        string framedText,
        JsonDiagnosticCode expectedCode)
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.InputMode = inputMode;
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes(framedText);
        IJsonUtf8Validator validator = new JsonUtf8Validator();

        AssertFailure(
            validator.ValidateUtf8(utf8, options),
            expectedCode,
            utf8.Length);
    }

    [TestCase(JsonConformanceProfile.SyntaxCompatible, JsonInputMode.SingleValue)]
    [TestCase(JsonConformanceProfile.SyntaxCompatible, JsonInputMode.WhitespaceSeparated)]
    [TestCase(JsonConformanceProfile.SyntaxCompatible, JsonInputMode.JsonTextSequence)]
    [TestCase(JsonConformanceProfile.Portable, JsonInputMode.SingleValue)]
    [TestCase(JsonConformanceProfile.Portable, JsonInputMode.WhitespaceSeparated)]
    [TestCase(JsonConformanceProfile.Portable, JsonInputMode.JsonTextSequence)]
    [TestCase(JsonConformanceProfile.IJson, JsonInputMode.SingleValue)]
    [TestCase(JsonConformanceProfile.IJson, JsonInputMode.WhitespaceSeparated)]
    [TestCase(JsonConformanceProfile.IJson, JsonInputMode.JsonTextSequence)]
    [TestCase(JsonConformanceProfile.Jcs, JsonInputMode.SingleValue)]
    public void EveryCompatibleProfileAndFramingCombinationValidates(
        JsonConformanceProfile profile,
        JsonInputMode inputMode)
    {
        JsonReaderOptions options = CreateProfileOptions(profile, inputMode);
        byte[] utf8 = CreateFramedInput(inputMode);
        IJsonUtf8Validator validator = new JsonUtf8Validator();

        AssertSuccess(validator.ValidateUtf8(utf8, options), utf8.Length);
    }

    [TestCase(JsonInputMode.WhitespaceSeparated)]
    [TestCase(JsonInputMode.JsonTextSequence)]
    public void JcsRejectsEveryMultipleValueFramingMode(JsonInputMode inputMode)
    {
        JsonReaderOptions options = CreateProfileOptions(JsonConformanceProfile.Jcs, inputMode);
        IJsonUtf8Validator validator = new JsonUtf8Validator();

        JsonOperationResult result = validator.ValidateUtf8(CreateFramedInput(inputMode), options);

        AssertFailure(result, JsonDiagnosticCode.InvalidOptions, 0);
        Assert.That(result.BytesConsumed, Is.Zero);
    }

    [Test]
    public void SyntaxCompatibleProfileAllowsItsConfiguredUnicodeAndDuplicatePolicies()
    {
        JsonReaderOptions options = CreateProfileOptions(
            JsonConformanceProfile.SyntaxCompatible,
            JsonInputMode.SingleValue);
        IJsonUtf8Validator validator = new JsonUtf8Validator();
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes(
            "{\"a\":\"\\uD800\",\"a\":1}");

        AssertSuccess(validator.ValidateUtf8(utf8, options), utf8.Length);
    }

    [TestCase(JsonConformanceProfile.Portable)]
    [TestCase(JsonConformanceProfile.IJson)]
    [TestCase(JsonConformanceProfile.Jcs)]
    public void EveryStrictProfileRejectsLoneSurrogates(
        JsonConformanceProfile profile)
    {
        JsonReaderOptions options = CreateProfileOptions(profile, JsonInputMode.SingleValue);
        IJsonUtf8Validator validator = new JsonUtf8Validator();
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes("\"\\uD800\"");

        AssertFailure(
            validator.ValidateUtf8(utf8, options),
            JsonDiagnosticCode.InvalidSurrogatePair,
            utf8.Length);
    }

    [TestCase(JsonConformanceProfile.Portable)]
    [TestCase(JsonConformanceProfile.IJson)]
    [TestCase(JsonConformanceProfile.Jcs)]
    public void EveryStrictProfileRejectsDuplicateDecodedNames(
        JsonConformanceProfile profile)
    {
        JsonReaderOptions options = CreateProfileOptions(profile, JsonInputMode.SingleValue);
        IJsonUtf8Validator validator = new JsonUtf8Validator();
        byte[] utf8 = System.Text.Encoding.ASCII.GetBytes(
            "{\"a\":0,\"\\u0061\":1}");

        AssertFailure(
            validator.ValidateUtf8(utf8, options),
            JsonDiagnosticCode.DuplicatePropertyName,
            utf8.Length);
    }

    [TestCase("document", "[0]", JsonDiagnosticCode.DocumentByteLimitExceeded)]
    [TestCase("buffer", "[0]", JsonDiagnosticCode.InputBufferLimitExceeded)]
    [TestCase("token", "\"ab\"", JsonDiagnosticCode.TokenByteLimitExceeded)]
    [TestCase("decodedBytes", "\"€\"", JsonDiagnosticCode.DecodedStringByteLimitExceeded)]
    [TestCase("decodedUnits", "\"ab\"", JsonDiagnosticCode.DecodedStringCodeUnitLimitExceeded)]
    [TestCase("number", "1234", JsonDiagnosticCode.NumberCharacterLimitExceeded)]
    [TestCase("nesting", "[[]]", JsonDiagnosticCode.NestingDepthLimitExceeded)]
    [TestCase("array", "[0,1]", JsonDiagnosticCode.ArrayItemLimitExceeded)]
    [TestCase("object", "{\"a\":0,\"b\":1}", JsonDiagnosticCode.ObjectMemberLimitExceeded)]
    public void EveryInputLimitIsAppliedThroughTheInterface(
        string limitName,
        string jsonText,
        JsonDiagnosticCode expectedCode)
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.Limits = CreateLimits(limitName);
        byte[] utf8 = System.Text.Encoding.UTF8.GetBytes(jsonText);
        IJsonUtf8Validator validator = new JsonUtf8Validator();

        AssertFailure(
            validator.ValidateUtf8(utf8, options),
            expectedCode,
            utf8.Length);
    }

    [Test]
    public void OneShotValidationUsesOnlyTheSuppliedSpanAndOwnsFailureContext()
    {
        IJsonUtf8Validator validator = new JsonUtf8Validator();
        byte[] validBuffer = System.Text.Encoding.ASCII.GetBytes("x {\"a\":0} y");
        global::System.ReadOnlySpan<byte> validSlice = new global::System.ReadOnlySpan<byte>(
            validBuffer,
            2,
            7);

        JsonOperationResult valid = validator.ValidateUtf8(validSlice, new JsonReaderOptions());
        AssertSuccess(valid, 7);

        byte[] invalidBuffer = new byte[] { 0x78, 0x2F, 0x79 };
        global::System.ReadOnlySpan<byte> invalidSlice = new global::System.ReadOnlySpan<byte>(
            invalidBuffer,
            1,
            1);
        JsonOperationResult invalid = validator.ValidateUtf8(invalidSlice, new JsonReaderOptions());
        invalidBuffer[1] = 0x30;

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.Diagnostic?.Code, Is.EqualTo(JsonDiagnosticCode.UnexpectedCharacter));
            Assert.That(invalid.Diagnostic?.SourceExcerpt, Is.EqualTo(new byte[] { 0x2F }));
        });
    }

    static JsonReaderOptions CreateProfileOptions(
        JsonConformanceProfile profile,
        JsonInputMode inputMode)
    {
        JsonReaderOptions options = new JsonReaderOptions();
        options.ConformanceProfile = profile;
        options.InputMode = inputMode;
        if (profile != JsonConformanceProfile.SyntaxCompatible)
        {
            options.DuplicateKeyPolicy = JsonDuplicateKeyPolicy.Reject;
            options.UnicodePolicy = JsonUnicodePolicy.UnicodeScalar;
            options.BomPolicy = JsonBomPolicy.Reject;
        }

        return options;
    }

    static byte[] CreateFramedInput(JsonInputMode inputMode)
    {
        if (inputMode == JsonInputMode.WhitespaceSeparated)
        {
            return System.Text.Encoding.ASCII.GetBytes("{} [] null");
        }

        if (inputMode == JsonInputMode.JsonTextSequence)
        {
            return System.Text.Encoding.ASCII.GetBytes("\u001e{}\n\u001e[]\n\u001enull\n");
        }

        return System.Text.Encoding.ASCII.GetBytes("{\"a\":[0,true,null]}");
    }

    static JsonLimits CreateLimits(string limitName)
    {
        JsonLimits limits = new JsonLimits();
        switch (limitName)
        {
            case "document":
                limits.MaximumDocumentBytes = 2;
                break;
            case "buffer":
                limits.MaximumInputBufferBytes = 2;
                break;
            case "token":
                limits.MaximumTokenBytes = 3;
                break;
            case "decodedBytes":
                limits.MaximumDecodedStringBytes = 2;
                break;
            case "decodedUnits":
                limits.MaximumDecodedStringCodeUnits = 1;
                break;
            case "number":
                limits.MaximumNumberCharacters = 3;
                break;
            case "nesting":
                limits.MaximumNestingDepth = 1;
                break;
            case "array":
                limits.MaximumArrayItems = 1;
                break;
            case "object":
                limits.MaximumObjectMembers = 1;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(limitName));
        }

        return limits;
    }

    static void AssertSuccess(JsonOperationResult result, int expectedBytesConsumed)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.BytesConsumed, Is.EqualTo(expectedBytesConsumed));
            Assert.That(result.BytesWritten, Is.Zero);
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    static void AssertFailure(
        JsonOperationResult result,
        JsonDiagnosticCode expectedCode,
        int maximumBytesConsumed)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.BytesConsumed, Is.InRange(0, maximumBytesConsumed));
            Assert.That(result.BytesWritten, Is.Zero);
            Assert.That(result.Diagnostic?.Code, Is.EqualTo(expectedCode));
            Assert.That(result.Diagnostic?.Message, Is.Not.Null.And.Not.Empty);
        });
    }
}
