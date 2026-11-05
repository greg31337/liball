using System.Text;
using NUnit.Framework;

namespace fix.Tests;

public class DictionaryStructureValidatorTests
{
    [Test]
    public void SupportedMessageExpandsNestedComponentsWithoutRequiringAbsentOptionalComponents()
    {
        FixDictionary dictionary = Dictionary();
        FixMessage message = Message("D");
        message.BodyFields = [Field(11, "ORDER", 5), Field(55, "SYMBOL", 6)];

        ValidationResult result = Validate(message, dictionary, new ValidationOptions(), new ValidationContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True, string.Join(",", result.Issues.ConvertAll(issue => issue.Code + ":" + issue.Tag)));
            Assert.That(result.Issues, Is.Empty);
        });
    }

    [Test]
    public void RequiredComponentsAndActivatedOptionalComponentsEnforceNestedRequiredFields()
    {
        FixDictionary dictionary = Dictionary();
        FixMessage message = Message("D");
        message.BodyFields = [Field(11, "ORDER", 5), Field(9001, "OPTIONAL-PRESENT", 6)];

        ValidationResult result = Validate(message, dictionary, new ValidationOptions { CheckFieldOrder = false }, new ValidationContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues.ConvertAll(issue => issue.Code + ":" + issue.Tag), Is.EqualTo(new[]
            {
                "required-field-missing:55",
                "required-field-missing:9000"
            }));
        });
    }

    [Test]
    public void PlacementAllowedFieldsAndUserDefinedPolicyAreAppliedTogether()
    {
        FixDictionary dictionary = Dictionary();
        FixMessage message = Message("D");
        message.HeaderFields.Add(Field(11, "BODY-IN-HEADER", 5));
        message.HeaderFields.Add(Field(9002, "USER-IN-HEADER", 6));
        message.BodyFields =
        [
            Field(FixTags.SenderCompID, "HEADER-IN-BODY", 7),
            Field(999, "KNOWN-NOT-ALLOWED", 8),
            Field(100, "UNKNOWN-STANDARD", 9),
            Field(9003, "UNKNOWN-USER", 10)
        ];
        ValidationOptions options = new ValidationOptions
        {
            CheckRequiredFields = false,
            CheckFieldOrder = false,
            AllowUserDefinedFields = true
        };

        ValidationResult result = Validate(message, dictionary, options, new ValidationContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues.ConvertAll(issue => issue.Code + ":" + issue.Tag), Is.EqualTo(new[]
            {
                "field-in-wrong-section:11",
                "user-defined-field-in-session-section:9002",
                "field-in-wrong-section:49",
                "field-not-allowed:999",
                "unknown-field:100"
            }));
            Assert.That(result.Issues.Exists(issue => issue.Tag == 9003), Is.False);
            Assert.That(result.Issues[0].HasSourceOffset, Is.True);
            Assert.That(result.Issues.ConvertAll(issue => issue.SessionRejectReason), Is.EqualTo(new[] { 2, 2, 2, 2, 3 }));
        });
    }

    [Test]
    public void UserDefinedAndUnknownFieldOptionsApplyIndependently()
    {
        FixDictionary dictionary = Dictionary();
        dictionary.Fields.Add(Definition(9004, "KnownCustomField"));
        FixMessage message = Message("D");
        message.BodyFields = [Field(100, "UNKNOWN-STANDARD", 5), Field(9003, "UNKNOWN-CUSTOM", 6), Field(9004, "KNOWN-CUSTOM", 7)];
        ValidationOptions strictOptions = new ValidationOptions { CheckRequiredFields = false, CheckFieldOrder = false };
        ValidationOptions customOptions = new ValidationOptions { CheckRequiredFields = false, CheckFieldOrder = false, AllowUserDefinedFields = true };
        ValidationOptions unknownOptions = new ValidationOptions { CheckRequiredFields = false, CheckFieldOrder = false, RejectUnknownFields = false };

        ValidationResult strict = Validate(message, dictionary, strictOptions, new ValidationContext());
        ValidationResult customAllowed = Validate(message, dictionary, customOptions, new ValidationContext());
        ValidationResult unknownAllowed = Validate(message, dictionary, unknownOptions, new ValidationContext());

        Assert.Multiple(() =>
        {
            Assert.That(strict.Issues.ConvertAll(issue => issue.Code + ":" + issue.Tag), Is.EqualTo(new[]
            {
                "unknown-field:100",
                "unknown-field:9003",
                "field-not-allowed:9004"
            }));
            Assert.That(customAllowed.Issues.ConvertAll(issue => issue.Code + ":" + issue.Tag), Is.EqualTo(new[] { "unknown-field:100" }));
            Assert.That(customAllowed.Issues[0].SessionRejectReason, Is.EqualTo(3));
            Assert.That(unknownAllowed.Issues.ConvertAll(issue => issue.Code + ":" + issue.Tag), Is.EqualTo(new[] { "field-not-allowed:9004" }));
            Assert.That(unknownAllowed.Issues[0].SessionRejectReason, Is.EqualTo(2));
        });
    }

    [Test]
    public void TopLevelFieldOrderCanBeStrictOrRelaxed()
    {
        FixDictionary dictionary = Dictionary();
        FixMessage strictMessage = Message("D");
        strictMessage.BodyFields = [Field(55, "SYMBOL", 5), Field(11, "ORDER", 6)];
        FixMessage relaxedMessage = Message("D");
        relaxedMessage.BodyFields = [Field(55, "SYMBOL", 5), Field(11, "ORDER", 6)];

        ValidationResult strict = Validate(strictMessage, dictionary, new ValidationOptions(), new ValidationContext());
        ValidationResult relaxed = Validate(relaxedMessage, dictionary, new ValidationOptions { CheckFieldOrder = false }, new ValidationContext());

        Assert.Multiple(() =>
        {
            Assert.That(strict.IsValid, Is.False);
            Assert.That(strict.Issues.ConvertAll(issue => issue.Code + ":" + issue.Tag), Is.EqualTo(new[] { "body-field-order-invalid:11" }));
            Assert.That(strict.Issues[0].SessionRejectReason, Is.EqualTo(14));
            Assert.That(relaxed.IsValid, Is.True, string.Join(",", relaxed.Issues.ConvertAll(issue => issue.Code)));
            Assert.That(relaxed.Issues, Is.Empty);
        });
    }

    [Test]
    public void HeaderAndTrailerOrderCanBeStrictOrRelaxed()
    {
        FixDictionary dictionary = Dictionary();
        dictionary.Fields.Add(Definition(90, "SecureDataLen"));
        dictionary.Fields.Add(Definition(91, "SecureData"));
        dictionary.Trailer =
        [
            Member(FixMemberKind.Field, 90, FixPresence.Optional),
            Member(FixMemberKind.Field, 91, FixPresence.Optional),
            Member(FixMemberKind.Field, FixTags.CheckSum, FixPresence.Required)
        ];
        FixMessage message = Message("D");
        message.HeaderFields =
        [
            Field(FixTags.BeginString, FixBeginStrings.Fixt11, 0),
            Field(FixTags.BodyLength, "0", 1),
            Field(FixTags.MsgType, "D", 2),
            Field(FixTags.TargetCompID, "TARGET", 3),
            Field(FixTags.SenderCompID, "SENDER", 4)
        ];
        message.BodyFields = [Field(11, "ORDER", 5), Field(55, "SYMBOL", 6)];
        message.TrailerFields = [Field(91, "DATA", 7), Field(90, "4", 8), Field(FixTags.CheckSum, "000", 9)];

        ValidationResult strict = Validate(message, dictionary, new ValidationOptions(), new ValidationContext());
        ValidationResult relaxed = Validate(message, dictionary, new ValidationOptions { CheckFieldOrder = false }, new ValidationContext());

        Assert.Multiple(() =>
        {
            Assert.That(strict.Issues.ConvertAll(issue => issue.Code + ":" + issue.Tag), Is.EqualTo(new[]
            {
                "header-field-order-invalid:49",
                "trailer-field-order-invalid:90"
            }));
            Assert.That(strict.Issues.TrueForAll(issue => issue.SessionRejectReason == 14), Is.True);
            Assert.That(relaxed.IsValid, Is.True);
            Assert.That(relaxed.Issues, Is.Empty);
        });
    }

    [Test]
    public void BodyDuplicatesExcludeFlattenedRepeatingGroupMembers()
    {
        FixDictionary dictionary = Dictionary();
        FixMessage strictMessage = Message("D");
        strictMessage.BodyFields =
        [
            Field(11, "FIRST", 5),
            Field(11, "SECOND", 6),
            Field(55, "SYMBOL", 7),
            Field(453, "2", 8),
            Field(448, "PARTY-1", 9),
            Field(448, "PARTY-2", 10)
        ];
        FixMessage relaxedMessage = Message("D");
        relaxedMessage.BodyFields.AddRange(strictMessage.BodyFields);

        ValidationResult strict = Validate(strictMessage, dictionary, new ValidationOptions { CheckFieldOrder = false }, new ValidationContext());
        ValidationResult relaxed = Validate(relaxedMessage, dictionary, new ValidationOptions { CheckFieldOrder = false, RejectDuplicateFields = false }, new ValidationContext());

        Assert.Multiple(() =>
        {
            Assert.That(strict.IsValid, Is.False);
            Assert.That(strict.Issues.ConvertAll(issue => issue.Code + ":" + issue.Tag), Is.EqualTo(new[] { "duplicate-body-field:11" }));
            Assert.That(strict.Issues[0].SessionRejectReason, Is.EqualTo(13));
            Assert.That(strict.Issues.Exists(issue => issue.Tag == 448), Is.False);
            Assert.That(relaxed.IsValid, Is.True);
            Assert.That(relaxed.Issues, Is.Empty);
        });
    }

    [Test]
    public void UnsupportedMessageTypeProducesOneNonCascadingIssue()
    {
        FixDictionary dictionary = Dictionary();
        FixMessage message = Message("Z");
        ValidationOptions options = new ValidationOptions { CheckRequiredFields = false };

        ValidationResult result = Validate(message, dictionary, options, new ValidationContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues.ConvertAll(issue => issue.Code), Is.EqualTo(new[] { "unsupported-message-type" }));
            Assert.That(result.Issues[0].MsgType, Is.EqualTo("Z"));
            Assert.That(result.Issues[0].HasTag, Is.False);
            Assert.That(result.Issues[0].SessionRejectReason, Is.EqualTo(11));
            Assert.That(result.Issues[0].BusinessRejectReason, Is.EqualTo(3));
        });
    }

    [Test]
    public void ForbiddenMembersAreRejectedAfterComponentExpansion()
    {
        FixDictionary dictionary = Dictionary();
        dictionary.Messages[0].Members.Add(new FixMemberDefinition { Name = "Forbidden", Kind = FixMemberKind.Field, ReferenceId = 999, Presence = FixPresence.Forbidden });
        FixMessage message = Message("D");
        message.BodyFields = [Field(11, "ORDER", 5), Field(55, "SYMBOL", 6), Field(999, "NO", 7)];

        ValidationResult result = Validate(message, dictionary, new ValidationOptions(), new ValidationContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues.ConvertAll(issue => issue.Code + ":" + issue.Tag), Is.EqualTo(new[] { "forbidden-field-present:999" }));
            Assert.That(result.Issues[0].SessionRejectReason, Is.EqualTo(2));
        });
    }

    private static ValidationResult Validate(FixMessage message, FixDictionary dictionary, ValidationOptions options, ValidationContext context)
    {
        options.Layers = ValidationLayer.DictionaryGrammar;
        return new FixMessageValidator().ValidateMessage(message, dictionary, options, context);
    }

    private static FixMessage Message(string msgType)
    {
        return new FixMessage
        {
            BeginString = FixBeginStrings.Fixt11,
            MsgType = msgType,
            HeaderFields =
            [
                Field(FixTags.BeginString, FixBeginStrings.Fixt11, 0),
                Field(FixTags.BodyLength, "0", 1),
                Field(FixTags.MsgType, msgType, 2),
                Field(FixTags.SenderCompID, "SENDER", 3),
                Field(FixTags.TargetCompID, "TARGET", 4)
            ],
            TrailerFields = [Field(FixTags.CheckSum, "000", 20)]
        };
    }

    private static FixDictionary Dictionary()
    {
        return new FixDictionary
        {
            Options = new FixDictionaryOptions { UserDefinedTagMinimum = 5000 },
            Fields =
            [
                Definition(FixTags.BeginString, "BeginString"),
                Definition(FixTags.BodyLength, "BodyLength"),
                Definition(FixTags.CheckSum, "CheckSum"),
                Definition(FixTags.MsgType, "MsgType"),
                Definition(FixTags.SenderCompID, "SenderCompID"),
                Definition(FixTags.TargetCompID, "TargetCompID"),
                Definition(11, "ClOrdID"),
                Definition(54, "Side"),
                Definition(55, "Symbol"),
                Definition(448, "PartyID"),
                Definition(453, "NoPartyIDs"),
                Definition(999, "KnownButNotAllowed"),
                Definition(9000, "RequiredExtra"),
                Definition(9001, "OptionalExtra")
            ],
            Header =
            [
                Member(FixMemberKind.Field, FixTags.BeginString, FixPresence.Required),
                Member(FixMemberKind.Field, FixTags.BodyLength, FixPresence.Required),
                Member(FixMemberKind.Field, FixTags.MsgType, FixPresence.Required),
                Member(FixMemberKind.Field, FixTags.SenderCompID, FixPresence.Required),
                Member(FixMemberKind.Field, FixTags.TargetCompID, FixPresence.Required)
            ],
            Trailer = [Member(FixMemberKind.Field, FixTags.CheckSum, FixPresence.Required)],
            Components =
            [
                new FixComponentDefinition
                {
                    Id = 100,
                    Name = "OrderCore",
                    Members =
                    [
                        Member(FixMemberKind.Field, 11, FixPresence.Required),
                        Member(FixMemberKind.Field, 54, FixPresence.Optional)
                    ]
                },
                new FixComponentDefinition
                {
                    Id = 101,
                    Name = "Instrument",
                    Members = [Member(FixMemberKind.Field, 55, FixPresence.Required)]
                },
                new FixComponentDefinition
                {
                    Id = 102,
                    Name = "OptionalDetails",
                    Members =
                    [
                        Member(FixMemberKind.Field, 9000, FixPresence.Required),
                        Member(FixMemberKind.Field, 9001, FixPresence.Optional)
                    ]
                },
                new FixComponentDefinition
                {
                    Id = 103,
                    Name = "OrderRoot",
                    Members =
                    [
                        Member(FixMemberKind.Component, 100, FixPresence.Required),
                        Member(FixMemberKind.Component, 101, FixPresence.Required)
                    ]
                }
            ],
            Groups =
            [
                new FixGroupDefinition
                {
                    Id = 200,
                    Name = "Parties",
                    CountTag = 453,
                    DelimiterTag = 448,
                    Members = [Member(FixMemberKind.Field, 448, FixPresence.Required)]
                }
            ],
            Messages =
            [
                new FixMessageDefinition
                {
                    Id = 1,
                    Name = "NewOrderSingle",
                    MsgType = "D",
                    Members =
                    [
                        Member(FixMemberKind.Component, 103, FixPresence.Required),
                        Member(FixMemberKind.Component, 102, FixPresence.Optional),
                        Member(FixMemberKind.Group, 200, FixPresence.Optional)
                    ]
                }
            ]
        };
    }

    private static FixFieldDefinition Definition(int tag, string name)
    {
        return new FixFieldDefinition { Tag = tag, Name = name, FieldType = FixFieldType.String };
    }

    private static FixMemberDefinition Member(FixMemberKind kind, int referenceId, FixPresence presence)
    {
        return new FixMemberDefinition { Name = referenceId.ToString(System.Globalization.CultureInfo.InvariantCulture), Kind = kind, ReferenceId = referenceId, Presence = presence };
    }

    private static FixField Field(int tag, string value, int wireIndex)
    {
        return new FixField { Tag = tag, Value = Encoding.ASCII.GetBytes(value), WireIndex = wireIndex, SourceOffset = wireIndex * 10 };
    }
}
