using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborValidatorIssueCoverageTests
{
    [Test]
    public void ValidatorSetupDiagnosticsAreStable()
    {
        CborValidator validator = new CborValidator();
        CborValidationResult optionsMissing = validator.ValidateEncodingWithOptions(
            [0x00],
            null!);
        CborValidationResult sourceMissing = validator.ValidateEncodingWithOptions(
            null!,
            new CborDecodeOptions());
        CborValidationResult levelInvalid = validator.ValidateEncodingWithOptions(
            [0x00],
            new CborDecodeOptions
            {
                ValidationLevel = (CborValidationLevel)int.MaxValue
            });
        CborValidationResult issueLimitInvalid = validator.ValidateEncodingWithOptions(
            [0x00],
            new CborDecodeOptions { MaxValidationIssues = -1 });
        CborValidationResult registrationsMissing = validator.ValidateEncodingWithOptions(
            [0x00],
            new CborDecodeOptions { TagRegistrations = null! });
        CborDecodeOptions entryOptions = new CborDecodeOptions();
        entryOptions.TagRegistrations.Add(null!);
        CborValidationResult registrationMissing = validator.ValidateEncodingWithOptions(
            [0x00],
            entryOptions);

        Assert.Multiple(() =>
        {
            AssertIssue(
                optionsMissing,
                "CBOR_VALIDATION_OPTIONS_MISSING",
                -1,
                "$",
                1);
            AssertIssue(sourceMissing, "CBOR_SOURCE_MISSING", -1, "$", 1);
            AssertIssue(levelInvalid, "CBOR_VALIDATION_LEVEL_INVALID", -1, "$", 1);
            AssertIssue(
                issueLimitInvalid,
                "CBOR_MAX_VALIDATION_ISSUES_INVALID",
                -1,
                "$",
                1);
            AssertIssue(
                registrationsMissing,
                "CBOR_TAG_REGISTRATIONS_MISSING",
                -1,
                "$",
                1);
            AssertIssue(
                registrationMissing,
                "CBOR_TAG_REGISTRATION_MISSING",
                -1,
                "$",
                1);
        });
    }

    [TestCase("F81F", "CBOR_INVALID_SIMPLE_VALUE_ENCODING", 1L)]
    [TestCase("1F", "CBOR_ILLEGAL_INDEFINITE_HEAD", 0L)]
    [TestCase("FF", "CBOR_UNEXPECTED_BREAK", 0L)]
    [TestCase("81FF", "CBOR_BREAK_IN_DEFINITE_CONTAINER", 1L)]
    [TestCase("C0FF", "CBOR_BREAK_AFTER_TAG", 1L)]
    [TestCase("BF00FF", "CBOR_ODD_INDEFINITE_MAP", 2L)]
    [TestCase("5F6161FF", "CBOR_INVALID_BYTE_STRING_CHUNK", 1L)]
    [TestCase("7F4100FF", "CBOR_INVALID_TEXT_STRING_CHUNK", 1L)]
    [TestCase("5F5FFF", "CBOR_NESTED_INDEFINITE_BYTE_STRING", 1L)]
    [TestCase("7F7FFF", "CBOR_NESTED_INDEFINITE_TEXT_STRING", 1L)]
    public void WellFormednessDiagnosticsReportTheirExactCodeAndOffset(
        string hex,
        string code,
        long offset)
    {
        CborValidationResult result = new CborValidator().ValidateEncoding(
            Convert.FromHexString(hex),
            CborValidationLevel.WellFormedness);

        AssertIssue(result, code, offset, "$", 1);
    }

    [Test]
    public void WellFormednessLimitDiagnosticsRemainObservable()
    {
        CborValidationResult nesting = ValidateWithLimit(
            [0x81, 0x00],
            options => options.MaxNestingDepth = 0);
        CborValidationResult items = ValidateWithLimit(
            [0x00],
            options => options.MaxTotalItems = 0);
        CborValidationResult array = ValidateWithLimit(
            [0x81, 0x00],
            options => options.MaxArrayItems = 0);
        CborValidationResult map = ValidateWithLimit(
            [0xa1, 0x00, 0x00],
            options => options.MaxMapPairs = 0);
        CborValidationResult text = ValidateWithLimit(
            [0x61, 0x61],
            options => options.MaxStringBytes = 0);
        CborValidationResult bytes = ValidateWithLimit(
            [0x41, 0x00],
            options => options.MaxStringBytes = 0);

        Assert.Multiple(() =>
        {
            AssertIssue(nesting, "CBOR_MAX_NESTING_DEPTH", 0, "$", 1);
            AssertIssue(items, "CBOR_MAX_TOTAL_ITEMS", 0, "$", 1);
            AssertIssue(array, "CBOR_MAX_ARRAY_ITEMS", 0, "$", 1);
            AssertIssue(map, "CBOR_MAX_MAP_PAIRS", 0, "$", 1);
            AssertIssue(text, "CBOR_MAX_STRING_BYTES", 0, "$", 1);
            AssertIssue(bytes, "CBOR_MAX_STRING_BYTES", 0, "$", 1);
        });
    }

    [TestCase("61C0", "CBOR_UTF8_NON_SHORTEST", 1L)]
    [TestCase("61F5", "CBOR_UTF8_OUT_OF_RANGE", 1L)]
    [TestCase("6180", "CBOR_UTF8_INVALID_LEADING_BYTE", 1L)]
    [TestCase("62C220", "CBOR_UTF8_INVALID_CONTINUATION", 2L)]
    [TestCase("63EDA080", "CBOR_UTF8_SURROGATE", 1L)]
    [TestCase("61C2", "CBOR_UTF8_INCOMPLETE_SEQUENCE", 1L)]
    public void BasicUtf8DiagnosticsReportTheirExactCodeAndPayloadOffset(
        string hex,
        string code,
        long offset)
    {
        CborValidationResult result = new CborValidator().ValidateEncoding(
            Convert.FromHexString(hex),
            CborValidationLevel.Basic);

        AssertIssue(result, code, offset, "$", 1);
    }

    [Test]
    public void NestedBasicDiagnosticsUseTheDuplicateKeyPathAndSourceOffset()
    {
        CborValidationResult result = new CborValidator().ValidateEncoding(
            Convert.FromHexString("81A201000101"),
            CborValidationLevel.Basic);

        AssertIssue(result, "CBOR_DUPLICATE_MAP_KEY", 4, "$[0][1].key", 1);
    }

    [TestCase("C000", "CBOR_DATE_TIME_TYPE")]
    [TestCase("C16178", "CBOR_EPOCH_DATE_TIME_TYPE")]
    [TestCase("C200", "CBOR_BIGNUM_TYPE")]
    [TestCase("C480", "CBOR_BIG_NUMBER_PAIR_TYPE")]
    [TestCase("D81800", "CBOR_ENCODED_CBOR_TYPE")]
    [TestCase("D82000", "CBOR_TEXT_HINT_TYPE")]
    public void BuiltInTagTypeDiagnosticsAreExposedByTagValidity(
        string hex,
        string code)
    {
        CborValidationResult result = new CborValidator().ValidateEncoding(
            Convert.FromHexString(hex),
            CborValidationLevel.TagValidity);

        AssertIssue(result, code, 0, "$", 1);
    }

    [Test]
    public void RegisteredTagHandlerFallbackDiagnosticsAreStable()
    {
        CborValidationResult missingHandler = ValidateCustomTag(
            null,
            [0xd8, 0x64, 0x00]);
        CborValidationResult notHandled = ValidateCustomTag(
            new CoverageTagHandler { Handled = false },
            [0xd8, 0x64, 0x00]);
        CborValidationResult invalidContent = ValidateCustomTag(
            new CoverageTagHandler { Handled = true, Valid = false },
            [0x81, 0xd8, 0x64, 0x00]);

        Assert.Multiple(() =>
        {
            AssertIssue(missingHandler, "CBOR_TAG_HANDLER_MISSING", 0, "$", 1);
            AssertIssue(notHandled, "CBOR_TAG_NOT_HANDLED", 0, "$", 1);
            AssertIssue(
                invalidContent,
                "CBOR_TAG_CONTENT_INVALID",
                1,
                "$[0]",
                1);
        });
    }

    [Test]
    public void MissingTagContentIsReportedForApplicationCreatedValues()
    {
        CborValue tag = new CborValue
        {
            Kind = CborValueKind.Tag,
            TagNumber = CborTagNumbers.Uri
        };

        CborValidationResult result = new CborValidator().ValidateValue(
            tag,
            CborValidationLevel.TagValidity);
        CborValidationIssue issue = result.Issues.Single(item =>
            item.Code == "CBOR_TAG_CONTENT_MISSING");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(issue.ByteOffset, Is.EqualTo(-1));
            Assert.That(issue.Path, Is.EqualTo("$"));
        });
    }

    [Test]
    public void MissingEpochFloatPayloadUsesTheTagSpecificDiagnostic()
    {
        CborValue tag = new CborValue
        {
            Kind = CborValueKind.Tag,
            TagNumber = CborTagNumbers.EpochDateTime,
            TaggedValue = new CborValue { Kind = CborValueKind.Float }
        };

        CborValidationResult result = new CborValidator().ValidateValue(
            tag,
            CborValidationLevel.TagValidity);
        CborValidationIssue issue = result.Issues.Single(item =>
            item.Code == "CBOR_EPOCH_DATE_TIME_VALUE_MISSING");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(issue.ByteOffset, Is.EqualTo(-1));
            Assert.That(issue.Path, Is.EqualTo("$"));
        });
    }

    [TestCase("811800", "CBOR_DETERMINISTIC_NON_MINIMAL_HEAD", 1L, "$[0]")]
    [TestCase("81FB3FF8000000000000", "CBOR_DETERMINISTIC_FLOAT_WIDTH", 1L, "$[0]")]
    [TestCase("81F97E01", "CBOR_DETERMINISTIC_NAN", 1L, "$[0]")]
    [TestCase("819F00FF", "CBOR_DETERMINISTIC_INDEFINITE_LENGTH", 1L, "$[0]")]
    public void NestedDeterministicDiagnosticsRetainPathsAndOffsets(
        string hex,
        string code,
        long offset,
        string path)
    {
        CborValidationResult result = ValidateDeterministic(hex);

        CborValidationIssue issue = result.Issues.Single(item => item.Code == code);
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(issue.ByteOffset, Is.EqualTo(offset));
            Assert.That(issue.Path, Is.EqualTo(path));
        });
    }

    [Test]
    public void DeterministicDuplicateDiagnosticsRetainTheLaterKeyLocation()
    {
        CborValidationResult result = ValidateDeterministic("A200000001");
        CborValidationIssue issue = result.Issues.Single(item =>
            item.Code == "CBOR_DETERMINISTIC_DUPLICATE_KEY");

        Assert.Multiple(() =>
        {
            Assert.That(issue.ByteOffset, Is.EqualTo(3));
            Assert.That(issue.Path, Is.EqualTo("$[1].key"));
        });
    }

    private static CborValidationResult ValidateWithLimit(
        byte[] source,
        Action<CborDecodeOptions> configure)
    {
        CborDecodeOptions options = new CborDecodeOptions
        {
            ValidationLevel = CborValidationLevel.WellFormedness
        };
        configure(options);
        return new CborValidator().ValidateEncodingWithOptions(source, options);
    }

    private static CborValidationResult ValidateCustomTag(
        ICborTagHandler? handler,
        byte[] source)
    {
        CborDecodeOptions options = new CborDecodeOptions
        {
            ValidationLevel = CborValidationLevel.TagValidity
        };
        options.TagRegistrations.Add(new CborTagRegistration
        {
            TagNumber = 100,
            Handler = handler,
            ValidateContent = true
        });
        return new CborValidator().ValidateEncodingWithOptions(source, options);
    }

    private static CborValidationResult ValidateDeterministic(string hex)
    {
        return new CborValidator().ValidateEncodingWithOptions(
            Convert.FromHexString(hex),
            new CborDecodeOptions
            {
                ValidationLevel = CborValidationLevel.Deterministic,
                DeterministicEncodingProfile = CborEncodingProfile.CoreDeterministic,
                CollectValidationIssues = true
            });
    }

    private static void AssertIssue(
        CborValidationResult result,
        string code,
        long offset,
        string path,
        int issueCount)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues, Has.Length.EqualTo(issueCount));
            Assert.That(result.Issues[0].Code, Is.EqualTo(code));
            Assert.That(result.Issues[0].ByteOffset, Is.EqualTo(offset));
            Assert.That(result.Issues[0].Path, Is.EqualTo(path));
        });
    }

    private sealed class CoverageTagHandler : ICborTagHandler
    {
        public bool Handled;
        public bool Valid;

        public CborTagResult ValidateTag(ulong tagNumber, CborValue content)
        {
            return new CborTagResult
            {
                Handled = Handled,
                IsValid = Valid
            };
        }

        public CborTagResult DecodeTag(ulong tagNumber, CborValue content)
        {
            return new CborTagResult();
        }

        public CborTagResult EncodeTag(ulong tagNumber, object nativeValue)
        {
            return new CborTagResult();
        }
    }
}
