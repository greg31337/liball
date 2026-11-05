using System.Text;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class ValidationProfileTests
{
    [Test]
    public void StrictDefaultsRejectDeviationsThatAnExplicitTolerantProfileAccepts()
    {
        FixDictionary dictionary = CreateDictionary();
        FixMessage message = new FixMessage
        {
            MsgType = "D",
            BodyFields =
            [
                Field(44, "10"),
                Field(54, ""),
                Field(54, ""),
                Field(9001, "counterparty-extension")
            ]
        };
        ValidationOptions strict = ProfileOptions();
        ValidationOptions tolerant = ProfileOptions();
        tolerant.RejectEmptyValues = false;
        tolerant.RejectDuplicateFields = false;
        tolerant.RejectUnknownFields = false;
        tolerant.AllowUserDefinedFields = true;
        tolerant.CheckFieldOrder = false;
        tolerant.CheckRequiredFields = false;

        ValidationResult strictResult = Validate(message, dictionary, strict);
        ValidationResult tolerantResult = Validate(message, dictionary, tolerant);

        Assert.Multiple(() =>
        {
            Assert.That(strictResult.IsValid, Is.False);
            Assert.That(strictResult.Issues.ConvertAll(issue => issue.Code), Is.EqualTo(new[]
            {
                "unknown-field",
                "required-field-missing",
                "duplicate-body-field",
                "body-field-order-invalid",
                "body-field-order-invalid",
                "empty-field-value",
                "empty-field-value"
            }));
            Assert.That(strictResult.Issues.ConvertAll(issue => issue.SessionRejectReason), Is.EqualTo(new[] { 3, 1, 13, 14, 14, 4, 4 }));
            Assert.That(tolerantResult.IsValid, Is.True, string.Join(",", tolerantResult.Issues.ConvertAll(issue => issue.Code)));
            Assert.That(tolerantResult.Issues, Is.Empty);
        });
    }

    [Test]
    public void GroupAndConditionalChecksRequireTheirOwnExplicitDeviations()
    {
        FixDictionary dictionary = CreateDictionary();
        FixGroup parties = new FixGroup { CountTag = 453, DelimiterTag = 448 };
        parties.Entries.Add(new FixGroupEntry { Fields = [Field(448, "PARTY")] });
        FixMessage message = new FixMessage
        {
            MsgType = "D",
            BodyFields = [Field(11, "ORDER"), Field(54, "1"), Field(453, "2")],
            BodyGroups = [parties]
        };
        ValidationOptions strict = ProfileOptions();
        ValidationOptions tolerant = ProfileOptions();
        tolerant.CheckGroups = false;
        tolerant.CheckConditionalRules = false;

        ValidationResult strictResult = Validate(message, dictionary, strict);
        ValidationResult tolerantResult = Validate(message, dictionary, tolerant);

        Assert.Multiple(() =>
        {
            Assert.That(strictResult.Issues.ConvertAll(issue => issue.Code), Is.EqualTo(new[]
            {
                "group-count-mismatch",
                "conditionally-required-member-missing"
            }));
            Assert.That(strictResult.Issues[0].SessionRejectReason, Is.EqualTo(16));
            Assert.That(strictResult.Issues[1].SessionRejectReason, Is.EqualTo(1));
            Assert.That(strictResult.Issues[1].BusinessRejectReason, Is.EqualTo(5));
            Assert.That(tolerantResult.IsValid, Is.True);
            Assert.That(tolerantResult.Issues, Is.Empty);
        });
    }

    [Test]
    public void TolerantOptionsDoNotWaiveForbiddenMembersOrCodeSets()
    {
        FixDictionary dictionary = CreateDictionary();
        FixMessage message = new FixMessage
        {
            MsgType = "D",
            BodyFields = [Field(11, "ORDER"), Field(54, "9"), Field(99, "forbidden")]
        };
        ValidationOptions tolerant = ProfileOptions();
        tolerant.RejectEmptyValues = false;
        tolerant.RejectDuplicateFields = false;
        tolerant.RejectUnknownFields = false;
        tolerant.AllowUserDefinedFields = true;
        tolerant.CheckFieldOrder = false;
        tolerant.CheckRequiredFields = false;
        tolerant.CheckGroups = false;
        tolerant.CheckConditionalRules = false;

        ValidationResult result = Validate(message, dictionary, tolerant);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues.ConvertAll(issue => issue.Code), Is.EqualTo(new[]
            {
                "forbidden-field-present",
                "invalid-code-set-value"
            }));
            Assert.That(result.Issues.ConvertAll(issue => issue.SessionRejectReason), Is.EqualTo(new[] { 2, 5 }));
        });
    }

    private static ValidationResult Validate(FixMessage message, FixDictionary dictionary, ValidationOptions options)
    {
        return new FixMessageValidator().ValidateMessage(message, dictionary, options, new ValidationContext());
    }

    private static ValidationOptions ProfileOptions()
    {
        return new ValidationOptions
        {
            Layers = ValidationLayer.DictionaryGrammar | ValidationLayer.Datatype | ValidationLayer.CodeSet | ValidationLayer.Group | ValidationLayer.ConditionalRule
        };
    }

    private static FixDictionary CreateDictionary()
    {
        return new FixDictionary
        {
            Options = new FixDictionaryOptions { UserDefinedTagMinimum = 5000 },
            Fields =
            [
                new FixFieldDefinition { Tag = 11, Name = "ClOrdID", FieldType = FixFieldType.String },
                new FixFieldDefinition { Tag = 44, Name = "Price", FieldType = FixFieldType.Price },
                new FixFieldDefinition { Tag = 54, Name = "Side", FieldType = FixFieldType.Char, CodeSetName = "SideCodeSet" },
                new FixFieldDefinition { Tag = 99, Name = "ForbiddenField", FieldType = FixFieldType.String },
                new FixFieldDefinition { Tag = 453, Name = "NoPartyIDs", FieldType = FixFieldType.NumInGroup },
                new FixFieldDefinition { Tag = 448, Name = "PartyID", FieldType = FixFieldType.String }
            ],
            CodeSets =
            [
                new FixCodeSetDefinition
                {
                    Name = "SideCodeSet",
                    FieldType = FixFieldType.Char,
                    Codes = [new FixCodeDefinition { Name = "Buy", Value = "1" }]
                }
            ],
            Groups =
            [
                new FixGroupDefinition
                {
                    Id = 453,
                    Name = "Parties",
                    CountTag = 453,
                    DelimiterTag = 448,
                    Members = [Member(448, FixPresence.Required, "")]
                }
            ],
            Messages =
            [
                new FixMessageDefinition
                {
                    Name = "NewOrderSingle",
                    MsgType = "D",
                    Members =
                    [
                        Member(11, FixPresence.Required, ""),
                        Member(54, FixPresence.Optional, ""),
                        Member(44, FixPresence.Conditional, "exists(54)"),
                        new FixMemberDefinition { Name = "Parties", Kind = FixMemberKind.Group, ReferenceId = 453, Presence = FixPresence.Optional },
                        Member(99, FixPresence.Forbidden, "")
                    ]
                }
            ]
        };
    }

    private static FixMemberDefinition Member(int tag, FixPresence presence, string rule)
    {
        return new FixMemberDefinition
        {
            Name = tag.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Kind = FixMemberKind.Field,
            ReferenceId = tag,
            Presence = presence,
            PresenceRule = rule,
            RulePresence = FixPresence.Required
        };
    }

    private static FixField Field(int tag, string value)
    {
        return new FixField { Tag = tag, Value = Encoding.ASCII.GetBytes(value) };
    }
}
