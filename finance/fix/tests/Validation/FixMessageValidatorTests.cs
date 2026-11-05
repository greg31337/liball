using System.Text;
using NUnit.Framework;

namespace fix.Tests;

public class FixMessageValidatorTests
{
    [Test]
    public void ValidatorCollectsMapsAndOrdersIssuesDeterministically()
    {
        FixDictionary dictionary = CreateDictionary();
        FixMessage message = new FixMessage
        {
            MsgType = "D",
            HeaderFields = [Field(FixTags.MsgSeqNum, "7")],
            BodyFields = [Field(54, "9"), Field(100, "unknown")]
        };
        ValidationOptions options = new ValidationOptions
        {
            Layers = ValidationLayer.DictionaryGrammar | ValidationLayer.Datatype | ValidationLayer.CodeSet | ValidationLayer.ConditionalRule,
            CheckFieldOrder = false
        };
        FixMessageValidator validator = new FixMessageValidator();

        ValidationResult first = validator.ValidateMessage(message, dictionary, options, new ValidationContext());
        ValidationResult second = validator.ValidateMessage(message, dictionary, options, new ValidationContext());

        string[] expectedCodes =
        [
            "unknown-field",
            "required-field-missing",
            "invalid-code-set-value",
            "conditionally-required-member-missing"
        ];
        Assert.Multiple(() =>
        {
            Assert.That(first.IsValid, Is.False);
            Assert.That(first.Issues.ConvertAll(issue => issue.Code), Is.EqualTo(expectedCodes));
            Assert.That(second.Issues.ConvertAll(issue => issue.Code), Is.EqualTo(expectedCodes));
            Assert.That(first.Issues.ConvertAll(issue => issue.Layer), Is.EqualTo(new[]
            {
                ValidationLayer.DictionaryGrammar,
                ValidationLayer.DictionaryGrammar,
                ValidationLayer.CodeSet,
                ValidationLayer.ConditionalRule
            }));
            Assert.That(first.Issues[0].SessionRejectReason, Is.EqualTo(3));
            Assert.That(first.Issues[1].SessionRejectReason, Is.EqualTo(1));
            Assert.That(first.Issues[2].SessionRejectReason, Is.EqualTo(5));
            Assert.That(first.Issues[3].BusinessRejectReason, Is.EqualTo(5));
            Assert.That(first.Issues.TrueForAll(issue => issue.HasRefSeqNum && issue.RefSeqNum == 7), Is.True);
        });
    }

    [Test]
    public void MinimumSeverityFiltersDiagnosticsWithoutHidingValidationFailure()
    {
        FixDictionary dictionary = CreateDictionary();
        dictionary.Messages[0].Members[2].PresenceRule = "$market.open == true";
        FixMessage message = new FixMessage { MsgType = "D" };
        FixMessageValidator validator = new FixMessageValidator();
        ValidationOptions errorsOnly = new ValidationOptions
        {
            Layers = ValidationLayer.DictionaryGrammar | ValidationLayer.ConditionalRule,
            CheckFieldOrder = false,
            MinimumSeverity = ValidationSeverity.Error
        };
        ValidationOptions fatalsOnly = new ValidationOptions
        {
            Layers = ValidationLayer.DictionaryGrammar | ValidationLayer.ConditionalRule,
            CheckFieldOrder = false,
            MinimumSeverity = ValidationSeverity.Fatal
        };

        ValidationResult retainedError = validator.ValidateMessage(message, dictionary, errorsOnly, new ValidationContext());
        ValidationResult hiddenError = validator.ValidateMessage(message, dictionary, fatalsOnly, new ValidationContext());

        Assert.Multiple(() =>
        {
            Assert.That(retainedError.IsValid, Is.False);
            Assert.That(retainedError.Issues.ConvertAll(issue => issue.Code), Is.EqualTo(new[] { "required-field-missing" }));
            Assert.That(hiddenError.IsValid, Is.False);
            Assert.That(hiddenError.Issues, Is.Empty);
            Assert.That(hiddenError.IssueLimitReached, Is.False);
        });
    }

    [Test]
    public void DefaultsAreStrictWhileExplicitDeviationsAllowTolerantValidation()
    {
        FixDictionary dictionary = CreateDictionary();
        FixMessage message = new FixMessage
        {
            MsgType = "D",
            BodyFields = [Field(9001, "custom"), Field(54, ""), Field(54, "")]
        };
        ValidationOptions strict = new ValidationOptions
        {
            Layers = ValidationLayer.DictionaryGrammar | ValidationLayer.Datatype,
            CheckFieldOrder = false
        };
        ValidationOptions tolerant = new ValidationOptions
        {
            Layers = ValidationLayer.DictionaryGrammar | ValidationLayer.Datatype,
            RejectEmptyValues = false,
            RejectDuplicateFields = false,
            RejectUnknownFields = false,
            AllowUserDefinedFields = true,
            CheckFieldOrder = false,
            CheckRequiredFields = false
        };
        FixMessageValidator validator = new FixMessageValidator();

        ValidationResult strictResult = validator.ValidateMessage(message, dictionary, strict, new ValidationContext());
        ValidationResult tolerantResult = validator.ValidateMessage(message, dictionary, tolerant, new ValidationContext());

        Assert.Multiple(() =>
        {
            Assert.That(strictResult.IsValid, Is.False);
            Assert.That(strictResult.Issues.ConvertAll(issue => issue.Code), Does.Contain("unknown-field"));
            Assert.That(strictResult.Issues.ConvertAll(issue => issue.Code), Does.Contain("required-field-missing"));
            Assert.That(strictResult.Issues.ConvertAll(issue => issue.Code), Does.Contain("duplicate-body-field"));
            Assert.That(strictResult.Issues.ConvertAll(issue => issue.Code), Does.Contain("empty-field-value"));
            Assert.That(tolerantResult.IsValid, Is.True, string.Join(",", tolerantResult.Issues.ConvertAll(issue => issue.Code)));
            Assert.That(tolerantResult.Issues, Is.Empty);
        });
    }

    private static FixDictionary CreateDictionary()
    {
        return new FixDictionary
        {
            Options = new FixDictionaryOptions { UserDefinedTagMinimum = 5000 },
            Fields =
            [
                new FixFieldDefinition { Tag = FixTags.MsgSeqNum, Name = "MsgSeqNum", FieldType = FixFieldType.SeqNum },
                new FixFieldDefinition { Tag = 11, Name = "ClOrdID", FieldType = FixFieldType.String },
                new FixFieldDefinition { Tag = 44, Name = "Price", FieldType = FixFieldType.Price },
                new FixFieldDefinition { Tag = 54, Name = "Side", FieldType = FixFieldType.Char, CodeSetName = "SideCodeSet" }
            ],
            Header = [Member(FixTags.MsgSeqNum, FixPresence.Optional, "")],
            CodeSets =
            [
                new FixCodeSetDefinition
                {
                    Name = "SideCodeSet",
                    FieldType = FixFieldType.Char,
                    Codes = [new FixCodeDefinition { Name = "Buy", Value = "1" }]
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
                        Member(44, FixPresence.Conditional, "exists(54)")
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
