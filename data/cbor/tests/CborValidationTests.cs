using cbor;

namespace cbor.Tests;

[TestFixture]
public class CborValidationTests
{
    [Test]
    public void WellFormednessAcceptsBasicValidityProblems()
    {
        CborValidator validator = new CborValidator();
        byte[] invalidUtf8 = [0x62, 0xc3, 0x28];
        byte[] duplicateMap = [0xa2, 0x01, 0x00, 0x01, 0x01];

        CborValidationResult text = validator.ValidateEncoding(
            invalidUtf8,
            CborValidationLevel.WellFormedness
        );
        CborValidationResult map = validator.ValidateEncoding(
            duplicateMap,
            CborValidationLevel.WellFormedness
        );

        Assert.Multiple(() =>
        {
            Assert.That(text.IsValid, Is.True);
            Assert.That(text.Issues, Is.Empty);
            Assert.That(map.IsValid, Is.True);
            Assert.That(map.Issues, Is.Empty);
        });
    }

    [Test]
    public void BasicValidityRejectsInvalidUtf8AtTheExactPayloadOffset()
    {
        CborValidationResult result = new CborValidator().ValidateEncoding(
            [0x62, 0xc3, 0x28],
            CborValidationLevel.Basic
        );

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues, Has.Length.EqualTo(1));
            Assert.That(result.Issues[0].Severity, Is.EqualTo(CborIssueSeverity.Error));
            Assert.That(result.Issues[0].Code, Is.EqualTo("CBOR_UTF8_INVALID_CONTINUATION"));
            Assert.That(result.Issues[0].ByteOffset, Is.EqualTo(2));
            Assert.That(result.Issues[0].Path, Is.EqualTo("$"));
        });
    }

    [Test]
    public void BasicValidityRejectsEquivalentMapKeysWithPathAndOffset()
    {
        CborValidationResult result = new CborValidator().ValidateEncoding(
            [0xa2, 0x01, 0x00, 0x01, 0x01],
            CborValidationLevel.Basic
        );

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues, Has.Length.EqualTo(1));
            Assert.That(result.Issues[0].Code, Is.EqualTo("CBOR_DUPLICATE_MAP_KEY"));
            Assert.That(result.Issues[0].ByteOffset, Is.EqualTo(3));
            Assert.That(result.Issues[0].Path, Is.EqualTo("$[1].key"));
        });
    }

    [Test]
    public void WellFormednessReportsEmptyTruncatedMalformedAndTrailingInput()
    {
        CborValidator validator = new CborValidator();
        CborValidationResult empty = validator.ValidateEncoding(
            [],
            CborValidationLevel.WellFormedness
        );
        CborValidationResult truncated = validator.ValidateEncoding(
            [0x1a, 0x00],
            CborValidationLevel.WellFormedness
        );
        CborValidationResult malformed = validator.ValidateEncoding(
            [0x1c],
            CborValidationLevel.WellFormedness
        );
        CborValidationResult trailing = validator.ValidateEncoding(
            [0x00, 0x01],
            CborValidationLevel.WellFormedness
        );

        Assert.Multiple(() =>
        {
            AssertIssue(empty, "CBOR_UNEXPECTED_END_OF_DATA", 0);
            AssertIssue(truncated, "CBOR_UNEXPECTED_END_OF_DATA", 2);
            AssertIssue(malformed, "CBOR_RESERVED_ADDITIONAL_INFORMATION", 0);
            AssertIssue(trailing, "CBOR_TRAILING_DATA", 1);
        });
    }

    [Test]
    public void TagValidityInvokesRegisteredValidationHandlers()
    {
        StubTagHandler invalidHandler = new StubTagHandler
        {
            Handled = true,
            Valid = false,
            ErrorCode = "CBOR_TEST_TAG_INVALID"
        };
        CborDecodeOptions invalidOptions = CreateTagOptions(invalidHandler);
        CborValidationResult invalid = new CborValidator().ValidateEncodingWithOptions(
            [0xd8, 0x64, 0x00],
            invalidOptions
        );

        StubTagHandler validHandler = new StubTagHandler
        {
            Handled = true,
            Valid = true
        };
        CborValidationResult valid = new CborValidator().ValidateEncodingWithOptions(
            [0xd8, 0x64, 0x00],
            CreateTagOptions(validHandler)
        );

        Assert.Multiple(() =>
        {
            Assert.That(invalid.IsValid, Is.False);
            Assert.That(invalid.Issues, Has.Length.EqualTo(1));
            Assert.That(invalid.Issues[0].Code, Is.EqualTo("CBOR_TEST_TAG_INVALID"));
            Assert.That(invalid.Issues[0].ByteOffset, Is.Zero);
            Assert.That(invalid.Issues[0].Path, Is.EqualTo("$"));
            Assert.That(invalidHandler.ValidationCalls, Is.EqualTo(1));
            Assert.That(invalidHandler.LastTagNumber, Is.EqualTo(100));
            Assert.That(valid.IsValid, Is.True);
            Assert.That(valid.Issues, Is.Empty);
            Assert.That(validHandler.ValidationCalls, Is.EqualTo(1));
        });
    }

    [Test]
    public void UnknownTagsDependOnTheSelectedApplicationPolicy()
    {
        CborValidator validator = new CborValidator();
        byte[] tagged = [0xd8, 0x64, 0x00];
        CborValidationResult tagValidity = validator.ValidateEncoding(
            tagged,
            CborValidationLevel.TagValidity
        );
        CborValidationResult rejected = validator.ValidateEncodingWithOptions(
            tagged,
            new CborDecodeOptions
            {
                ValidationLevel = CborValidationLevel.Application,
                TagPolicy = CborTagPolicy.RejectUnknown
            }
        );
        CborValidationResult forbidden = validator.ValidateEncodingWithOptions(
            tagged,
            new CborDecodeOptions
            {
                ValidationLevel = CborValidationLevel.Application,
                TagPolicy = CborTagPolicy.Forbid
            }
        );

        Assert.Multiple(() =>
        {
            Assert.That(tagValidity.IsValid, Is.True);
            Assert.That(rejected.IsValid, Is.False);
            Assert.That(rejected.Issues[0].Code, Is.EqualTo("CBOR_UNKNOWN_TAG"));
            Assert.That(rejected.Issues[0].ByteOffset, Is.Zero);
            Assert.That(forbidden.IsValid, Is.False);
            Assert.That(forbidden.Issues[0].Code, Is.EqualTo("CBOR_TAG_FORBIDDEN"));
        });
    }

    [Test]
    public void ApplicationValidationAppliesIndefiniteSimpleAndReferencePolicies()
    {
        CborValidator validator = new CborValidator();
        CborValidationResult indefinite = validator.ValidateEncodingWithOptions(
            [0x9f, 0x01, 0xff],
            new CborDecodeOptions
            {
                ValidationLevel = CborValidationLevel.Application,
                AllowIndefiniteLength = false
            }
        );
        CborValidationResult simple = validator.ValidateEncodingWithOptions(
            [0xf8, 0x20],
            new CborDecodeOptions
            {
                ValidationLevel = CborValidationLevel.Application,
                AllowUnknownSimpleValues = false
            }
        );
        CborValidationResult reference = validator.ValidateEncodingWithOptions(
            [0xd8, 0x1c, 0x00],
            new CborDecodeOptions
            {
                ValidationLevel = CborValidationLevel.Application,
                SharedValuePolicy = CborSharedValuePolicy.Disabled
            }
        );

        Assert.Multiple(() =>
        {
            Assert.That(indefinite.Issues.Select(issue => issue.Code),
                Does.Contain("CBOR_INDEFINITE_LENGTH_DISABLED"));
            Assert.That(indefinite.Issues[0].ByteOffset, Is.Zero);
            Assert.That(simple.Issues.Select(issue => issue.Code),
                Does.Contain("CBOR_UNKNOWN_SIMPLE_VALUE"));
            Assert.That(simple.Issues[0].ByteOffset, Is.Zero);
            Assert.That(reference.Issues.Select(issue => issue.Code),
                Does.Contain("CBOR_REFERENCE_TAG_DISABLED"));
            Assert.That(reference.Issues[0].ByteOffset, Is.Zero);
        });
    }

    [Test]
    public void CollectionModeReportsMultipleIssuesWhileFailFastStopsAtOne()
    {
        byte[] source =
        [0xbf, 0x00, 0xf8, 0x20, 0x00, 0x9f, 0x01, 0xff, 0xff];
        CborDecodeOptions collectOptions = new CborDecodeOptions
        {
            ValidationLevel = CborValidationLevel.Application,
            AllowIndefiniteLength = false,
            AllowUnknownSimpleValues = false,
            CollectValidationIssues = true,
            MaxValidationIssues = 10
        };
        CborValidationResult collected = new CborValidator().ValidateEncodingWithOptions(
            source,
            collectOptions
        );
        CborDecodeOptions failFastOptions = new CborDecodeOptions
        {
            ValidationLevel = CborValidationLevel.Application,
            AllowIndefiniteLength = false,
            AllowUnknownSimpleValues = false,
            CollectValidationIssues = false,
            MaxValidationIssues = 10
        };
        CborValidationResult failFast = new CborValidator().ValidateEncodingWithOptions(
            source,
            failFastOptions
        );

        Assert.Multiple(() =>
        {
            Assert.That(collected.IsValid, Is.False);
            Assert.That(collected.Issues, Has.Length.EqualTo(7));
            Assert.That(collected.Issues.Select(issue => issue.Code), Is.EquivalentTo(new[]
            {
                "CBOR_DUPLICATE_MAP_KEY",
                "CBOR_INDEFINITE_LENGTH_DISABLED",
                "CBOR_UNKNOWN_SIMPLE_VALUE",
                "CBOR_INDEFINITE_LENGTH_DISABLED",
                "CBOR_DETERMINISTIC_INDEFINITE_LENGTH",
                "CBOR_DETERMINISTIC_DUPLICATE_KEY",
                "CBOR_DETERMINISTIC_INDEFINITE_LENGTH"
            }));
            Assert.That(failFast.IsValid, Is.False);
            Assert.That(failFast.Issues, Has.Length.EqualTo(1));
            Assert.That(failFast.Issues[0].Code, Is.EqualTo("CBOR_DUPLICATE_MAP_KEY"));
        });
    }

    [Test]
    public void IssueCollectionHonorsItsConfiguredCap()
    {
        CborValidationResult capped = new CborValidator().ValidateEncodingWithOptions(
            [0xbf, 0x00, 0xf8, 0x20, 0x00, 0x9f, 0x01, 0xff, 0xff],
            new CborDecodeOptions
            {
                ValidationLevel = CborValidationLevel.Application,
                AllowIndefiniteLength = false,
                AllowUnknownSimpleValues = false,
                MaxValidationIssues = 2,
                CollectValidationIssues = true
            }
        );
        CborValidationResult zero = new CborValidator().ValidateEncodingWithOptions(
            [0x1c],
            new CborDecodeOptions
            {
                ValidationLevel = CborValidationLevel.WellFormedness,
                MaxValidationIssues = 0
            }
        );

        Assert.Multiple(() =>
        {
            Assert.That(capped.IsValid, Is.False);
            Assert.That(capped.Issues, Has.Length.EqualTo(2));
            Assert.That(zero.IsValid, Is.False);
            Assert.That(zero.Issues, Is.Empty);
        });
    }

    [Test]
    public void ConfiguredDecoderLimitsRemainObservableAsValidationIssues()
    {
        CborValidationResult result = new CborValidator().ValidateEncodingWithOptions(
            [0x18, 0x18],
            new CborDecodeOptions
            {
                ValidationLevel = CborValidationLevel.WellFormedness,
                MaxTotalBytes = 1
            }
        );

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues, Has.Length.EqualTo(1));
            Assert.That(result.Issues[0].Code, Is.EqualTo("CBOR_MAX_TOTAL_BYTES"));
            Assert.That(result.Issues[0].ByteOffset, Is.Zero);
        });
    }

    private static CborDecodeOptions CreateTagOptions(StubTagHandler handler)
    {
        CborDecodeOptions options = new CborDecodeOptions
        {
            ValidationLevel = CborValidationLevel.TagValidity
        };
        options.TagRegistrations.Add(new CborTagRegistration
        {
            TagNumber = 100,
            Name = "test",
            Handler = handler,
            ValidateContent = true
        });
        return options;
    }

    private static void AssertIssue(
        CborValidationResult result,
        string code,
        long offset)
    {
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Issues, Has.Length.EqualTo(1));
        Assert.That(result.Issues[0].Code, Is.EqualTo(code));
        Assert.That(result.Issues[0].ByteOffset, Is.EqualTo(offset));
    }

    private sealed class StubTagHandler : ICborTagHandler
    {
        public bool Handled;
        public bool Valid;
        public string? ErrorCode;
        public int ValidationCalls;
        public ulong LastTagNumber;

        public CborTagResult ValidateTag(ulong tagNumber, CborValue content)
        {
            ValidationCalls++;
            LastTagNumber = tagNumber;
            return new CborTagResult
            {
                Handled = Handled,
                IsValid = Valid,
                ErrorCode = ErrorCode,
                ErrorMessage = "test tag failure"
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
