using System.Text;
using NUnit.Framework;

namespace fix.Tests;

public class EnvelopeValidatorTests
{
    [Test]
    public void CanonicalEnvelopeAndRequiredSessionSectionsProduceNoIssues()
    {
        FixDictionary dictionary = SessionDictionary();
        byte[] wire = Wire("FIXT.1.1", "35=D|49=SENDER|56=TARGET|34=1|52=20260830-12:34:56.000|11=ORDER|");
        FixMessage message = DecodeFlat(wire, dictionary);
        ValidationOptions options = new ValidationOptions();
        ValidationContext context = new ValidationContext { ExpectedBeginString = FixBeginStrings.Fixt11 };

        ValidationResult result = Validate(message, dictionary, options, context);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True, string.Join(",", result.Issues.ConvertAll(issue => issue.Code)));
            Assert.That(result.Issues, Is.Empty);
            Assert.That(result.IssueLimitReached, Is.False);
        });
    }

    [Test]
    public void MissingDuplicateAndMispositionedEnvelopeFieldsAreAllReported()
    {
        FixMessage message = new FixMessage
        {
            BeginString = FixBeginStrings.Fixt11,
            MsgType = "D",
            HeaderFields =
            [
                Field(FixTags.MsgType, "D", 0),
                Field(FixTags.BeginString, FixBeginStrings.Fixt11, 1),
                Field(FixTags.BeginString, FixBeginStrings.Fixt11, 2)
            ]
        };
        ValidationOptions options = new ValidationOptions { Layers = ValidationLayer.Envelope };

        ValidationResult result = Validate(message, SessionDictionary(), options, new ValidationContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues.ConvertAll(issue => issue.Code + ":" + issue.Tag), Is.EqualTo(new[]
            {
                "duplicate-envelope-field:8",
                "invalid-envelope-position:8",
                "missing-body-length:9",
                "invalid-envelope-position:35",
                "missing-checksum:10"
            }));
            Assert.That(result.Issues[0].HasSourceOffset, Is.True);
            Assert.That(result.Issues[0].SourceOffset, Is.EqualTo(10));
            Assert.That(result.Issues.ConvertAll(issue => issue.SessionRejectReason), Is.EqualTo(new[] { 13, 14, 1, 14, 1 }));
        });
    }

    [Test]
    public void EnvelopeEmptyAndDuplicateOptionsControlTheirOwnedFailures()
    {
        FixMessage message = new FixMessage
        {
            MsgType = "D",
            HeaderFields =
            [
                Field(FixTags.BeginString, "", 0),
                Field(FixTags.BodyLength, "0", 1),
                Field(FixTags.MsgType, "D", 2),
                Field(FixTags.BeginString, "", 3)
            ],
            TrailerFields = [Field(FixTags.CheckSum, "000", 4)]
        };
        ValidationOptions strictOptions = new ValidationOptions { Layers = ValidationLayer.Envelope };
        ValidationOptions relaxedOptions = new ValidationOptions
        {
            Layers = ValidationLayer.Envelope,
            RejectEmptyValues = false,
            RejectDuplicateFields = false
        };

        ValidationResult strict = Validate(message, SessionDictionary(), strictOptions, new ValidationContext());
        ValidationResult relaxed = Validate(message, SessionDictionary(), relaxedOptions, new ValidationContext());

        Assert.Multiple(() =>
        {
            Assert.That(strict.Issues.ConvertAll(issue => issue.Code), Is.EqualTo(new[] { "duplicate-envelope-field", "empty-envelope-field" }));
            Assert.That(strict.Issues.ConvertAll(issue => issue.SessionRejectReason), Is.EqualTo(new[] { 13, 4 }));
            Assert.That(relaxed.IsValid, Is.True, string.Join(",", relaxed.Issues.ConvertAll(issue => issue.Code)));
            Assert.That(relaxed.Issues, Is.Empty);
        });
    }

    [Test]
    public void MalformedAndDecodedEnvelopeValuesReceiveFormatAndValueMappings()
    {
        FixMessage message = new FixMessage
        {
            BeginString = FixBeginStrings.Fix44,
            MsgType = "D",
            HeaderFields =
            [
                Field(FixTags.BeginString, FixBeginStrings.Fixt11, 0),
                Field(FixTags.BodyLength, "not-a-length", 1),
                Field(FixTags.MsgType, "F", 2)
            ],
            TrailerFields = [Field(FixTags.CheckSum, "999", 3)]
        };
        ValidationOptions options = new ValidationOptions { Layers = ValidationLayer.Envelope };

        ValidationResult result = Validate(message, SessionDictionary(), options, new ValidationContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.Issues.ConvertAll(issue => issue.Code), Is.EqualTo(new[]
            {
                "invalid-body-length",
                "invalid-checksum",
                "begin-string-value-mismatch",
                "msg-type-value-mismatch"
            }));
            Assert.That(result.Issues.ConvertAll(issue => issue.SessionRejectReason), Is.EqualTo(new[] { 6, 6, 5, 5 }));
        });
    }

    [Test]
    public void PreservedWireBytesDetectBodyLengthAndChecksumMismatches()
    {
        FixDictionary dictionary = SessionDictionary();
        byte[] wire = WireWithAdjustments("FIXT.1.1", "35=D|49=S|56=T|34=1|52=20260830-12:34:56|", 1, 1);
        FixMessage message = DecodeFlat(wire, dictionary);
        ValidationOptions options = new ValidationOptions { Layers = ValidationLayer.Envelope };

        ValidationResult result = Validate(message, dictionary, options, new ValidationContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues.ConvertAll(issue => issue.Code), Is.EqualTo(new[] { "body-length-mismatch", "checksum-mismatch" }));
            Assert.That(result.Issues.ConvertAll(issue => issue.Tag), Is.EqualTo(new[] { FixTags.BodyLength, FixTags.CheckSum }));
            Assert.That(result.Issues, Is.All.Matches<ValidationIssue>(issue => issue.HasSourceOffset));
            Assert.That(result.Issues.ConvertAll(issue => issue.SessionRejectReason), Is.EqualTo(new[] { 5, 5 }));
        });
    }

    [Test]
    public void SessionValidationReportsVersionRequiredDuplicateAndEmptyHeaderFailures()
    {
        FixDictionary dictionary = SessionDictionary();
        byte[] wire = Wire("FIXT.1.1", "35=D|49=SENDER|49=SECOND|34=1|52=|1128=7|");
        FixMessage message = DecodeFlat(wire, dictionary);
        ValidationContext context = new ValidationContext
        {
            ExpectedBeginString = FixBeginStrings.Fix44,
            DictionaryIdentity = new FixDictionaryIdentity { ApplicationVersion = FixApplicationVersions.Fix50Sp2 }
        };
        ValidationOptions options = new ValidationOptions { Layers = ValidationLayer.SessionHeader };

        ValidationResult result = Validate(message, dictionary, options, context);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues.ConvertAll(issue => issue.Code + ":" + issue.Tag), Is.EqualTo(new[]
            {
                "begin-string-mismatch:8",
                "application-version-mismatch:1128",
                "required-header-field-missing:56",
                "duplicate-header-field:49",
                "empty-session-field:52"
            }));
            Assert.That(result.Issues.ConvertAll(issue => issue.SessionRejectReason), Is.EqualTo(new[] { 5, 18, 1, 13, 4 }));
        });
    }

    [Test]
    public void DefaultApplicationVersionMismatchMapsToUnsupportedVersion()
    {
        FixMessage message = new FixMessage
        {
            BeginString = FixBeginStrings.Fixt11,
            MsgType = FixAdminMessageTypes.Logon,
            HeaderFields = [Field(FixTags.DefaultApplVerID, FixApplicationVersions.Fix50, 0)]
        };
        ValidationOptions options = new ValidationOptions { Layers = ValidationLayer.SessionHeader, CheckRequiredFields = false };
        ValidationContext context = new ValidationContext { DefaultApplicationVersion = FixApplicationVersions.Fix50Sp2 };

        ValidationResult result = Validate(message, SessionDictionary(), options, context);

        Assert.Multiple(() =>
        {
            Assert.That(result.Issues.ConvertAll(issue => issue.Code), Is.EqualTo(new[] { "default-application-version-mismatch" }));
            Assert.That(result.Issues[0].SessionRejectReason, Is.EqualTo(18));
        });
    }

    [Test]
    public void SessionStrictnessOptionsDisableOnlyTheirOwnedChecks()
    {
        FixDictionary dictionary = SessionDictionary();
        byte[] wire = Wire("FIXT.1.1", "35=D|49=SENDER|49=SECOND|34=1|52=|1128=7|");
        FixMessage message = DecodeFlat(wire, dictionary);
        ValidationContext context = new ValidationContext
        {
            ExpectedBeginString = FixBeginStrings.Fixt11,
            DictionaryIdentity = new FixDictionaryIdentity { ApplicationVersion = FixApplicationVersions.Fix50 }
        };
        ValidationOptions options = new ValidationOptions
        {
            Layers = ValidationLayer.SessionHeader,
            CheckRequiredFields = false,
            RejectDuplicateFields = false,
            RejectEmptyValues = false
        };

        ValidationResult result = Validate(message, dictionary, options, context);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True, string.Join(",", result.Issues.ConvertAll(issue => issue.Code)));
            Assert.That(result.Issues, Is.Empty);
        });
    }

    [Test]
    public void ConfiguredMessageFieldAndValueLimitsAreEnforced()
    {
        FixDictionary dictionary = SessionDictionary();
        byte[] wire = Wire("FIXT.1.1", "35=D|49=LONG-SENDER|56=T|34=1|52=20260830-12:34:56|");
        FixMessage message = DecodeFlat(wire, dictionary);
        ValidationOptions options = new ValidationOptions
        {
            Layers = ValidationLayer.Envelope,
            MaxMessageSize = 20,
            MaxFieldSize = 4,
            MaxFieldsPerMessage = 3
        };

        ValidationResult result = Validate(message, dictionary, options, new ValidationContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues.ConvertAll(issue => issue.Code), Does.Contain("field-count-limit-exceeded"));
            Assert.That(result.Issues.ConvertAll(issue => issue.Code), Does.Contain("message-size-limit-exceeded"));
            Assert.That(result.Issues.ConvertAll(issue => issue.Code), Does.Contain("field-size-limit-exceeded"));
            Assert.That(result.Issues.Exists(issue => issue.Code == "field-size-limit-exceeded" && issue.Tag == FixTags.BeginString), Is.True);
            Assert.That(result.Issues.Exists(issue => issue.Code == "field-size-limit-exceeded" && issue.Tag == FixTags.SenderCompID), Is.True);
            Assert.That(result.Issues.TrueForAll(issue => issue.SessionRejectReason == 99), Is.True);
        });
    }

    [Test]
    public void NegativeConfiguredLimitsProduceOneFatalOptionsIssue()
    {
        FixDictionary dictionary = SessionDictionary();
        FixMessage message = DecodeFlat(Wire("FIXT.1.1", "35=D|49=S|56=T|34=1|52=20260830-12:34:56|"), dictionary);
        ValidationOptions options = new ValidationOptions { Layers = ValidationLayer.Envelope, MaxFieldSize = -1 };

        ValidationResult result = Validate(message, dictionary, options, new ValidationContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues.ConvertAll(issue => issue.Code), Is.EqualTo(new[] { "invalid-validation-limit" }));
            Assert.That(result.Issues[0].Severity, Is.EqualTo(ValidationSeverity.Fatal));
            Assert.That(result.Issues[0].HasSessionRejectReason, Is.False);
        });
    }

    private static ValidationResult Validate(FixMessage message, FixDictionary dictionary, ValidationOptions options, ValidationContext context)
    {
        List<ValidationIssue> candidates = EnvelopeValidator.ValidateEnvelopeAndSession(message, dictionary, options, context);
        RejectMapper.MapValidationIssues(candidates, message);
        return ValidationCoordinator.CoordinateValidation(candidates, options);
    }

    private static FixMessage DecodeFlat(byte[] wire, FixDictionary dictionary)
    {
        DecodeResult decoded = MessageBuilder.DecodeFlatMessage(wire, dictionary, new DecodeOptions { AllowEmptyValues = true });
        Assert.That(decoded.Message, Is.Not.Null, decoded.ErrorCode);
        return decoded.Message!;
    }

    private static FixDictionary SessionDictionary()
    {
        return new FixDictionary
        {
            Identity = new FixDictionaryIdentity { Name = "FIXT11", BeginString = FixBeginStrings.Fixt11 },
            Fields =
            [
                Definition(FixTags.BeginString, "BeginString", FixFieldType.String),
                Definition(FixTags.BodyLength, "BodyLength", FixFieldType.Length),
                Definition(FixTags.CheckSum, "CheckSum", FixFieldType.String),
                Definition(FixTags.MsgSeqNum, "MsgSeqNum", FixFieldType.SeqNum),
                Definition(FixTags.MsgType, "MsgType", FixFieldType.String),
                Definition(FixTags.SenderCompID, "SenderCompID", FixFieldType.String),
                Definition(FixTags.SendingTime, "SendingTime", FixFieldType.UtcTimestamp),
                Definition(FixTags.TargetCompID, "TargetCompID", FixFieldType.String),
                Definition(FixTags.ApplVerID, "ApplVerID", FixFieldType.String),
                Definition(FixTags.DefaultApplVerID, "DefaultApplVerID", FixFieldType.String),
                Definition(11, "ClOrdID", FixFieldType.String)
            ],
            Header =
            [
                Member(FixTags.BeginString, FixPresence.Required),
                Member(FixTags.BodyLength, FixPresence.Required),
                Member(FixTags.MsgType, FixPresence.Required),
                Member(FixTags.SenderCompID, FixPresence.Required),
                Member(FixTags.TargetCompID, FixPresence.Required),
                Member(FixTags.MsgSeqNum, FixPresence.Required),
                Member(FixTags.SendingTime, FixPresence.Required),
                Member(FixTags.ApplVerID, FixPresence.Optional),
                Member(FixTags.DefaultApplVerID, FixPresence.Optional)
            ],
            Trailer = [Member(FixTags.CheckSum, FixPresence.Required)]
        };
    }

    private static FixFieldDefinition Definition(int tag, string name, FixFieldType type)
    {
        return new FixFieldDefinition { Tag = tag, Name = name, FieldType = type };
    }

    private static FixMemberDefinition Member(int tag, FixPresence presence)
    {
        return new FixMemberDefinition { Name = tag.ToString(System.Globalization.CultureInfo.InvariantCulture), Kind = FixMemberKind.Field, ReferenceId = tag, Presence = presence };
    }

    private static FixField Field(int tag, string value, int wireIndex)
    {
        return new FixField { Tag = tag, Value = Encoding.ASCII.GetBytes(value), WireIndex = wireIndex, SourceOffset = wireIndex * 10 };
    }

    private static byte[] Wire(string beginString, string body)
    {
        return WireWithAdjustments(beginString, body, 0, 0);
    }

    private static byte[] WireWithAdjustments(string beginString, string body, int bodyLengthAdjustment, int checksumAdjustment)
    {
        string normalizedBody = body.Replace('|', (char)FixDefaults.Soh);
        int bodyLength = Encoding.ASCII.GetByteCount(normalizedBody) + bodyLengthAdjustment;
        string prefix = "8=" + beginString + (char)FixDefaults.Soh + "9=" + bodyLength.ToString(System.Globalization.CultureInfo.InvariantCulture) + (char)FixDefaults.Soh + normalizedBody;
        byte[] prefixBytes = Encoding.ASCII.GetBytes(prefix);
        int checksum = (ChecksumCalculator.CalculateChecksum(prefixBytes) + checksumAdjustment) & 255;
        string complete = prefix + "10=" + checksum.ToString("D3", System.Globalization.CultureInfo.InvariantCulture) + (char)FixDefaults.Soh;
        return Encoding.ASCII.GetBytes(complete);
    }
}
