using System.Text;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class ConditionalValidatorTests
{
    [Test]
    public void ExternalStateSelectsConditionalRequiredPresence()
    {
        FixDictionary dictionary = CreateRuleDictionary();
        FixMemberDefinition price = new FixMemberDefinition
        {
            Name = "Price",
            Kind = FixMemberKind.Field,
            ReferenceId = 44,
            Presence = FixPresence.Conditional,
            PresenceRule = "$market.open == true",
            RulePresence = FixPresence.Required
        };
        dictionary.Messages.Add(new FixMessageDefinition { MsgType = "D", Members = [price] });
        FixMessage message = new FixMessage { MsgType = "D" };
        ValidationContext open = new ValidationContext();
        open.ExternalState["market.open"] = "true";
        ValidationContext closed = new ValidationContext();
        closed.ExternalState["market.open"] = "false";

        ValidationResult required = Validate(message, dictionary, open);
        ValidationResult optional = Validate(message, dictionary, closed);
        ValidationResult indeterminate = Validate(message, dictionary, new ValidationContext());

        Assert.Multiple(() =>
        {
            Assert.That(required.Issues.ConvertAll(issue => issue.Code), Is.EqualTo(new[] { "conditionally-required-member-missing" }));
            Assert.That(required.Issues[0].Tag, Is.EqualTo(44));
            Assert.That(optional.IsValid, Is.True);
            Assert.That(optional.Issues, Is.Empty);
            Assert.That(indeterminate.IsValid, Is.True);
            Assert.That(indeterminate.Issues.ConvertAll(issue => issue.Code), Is.EqualTo(new[] { "conditional-rule-indeterminate" }));
            Assert.That(indeterminate.Issues[0].Severity, Is.EqualTo(ValidationSeverity.Warning));
        });
    }

    [Test]
    public void OrchestraArithmeticConditionSelectsRequiredOptionalAndIndeterminatePresence()
    {
        FixDictionary dictionary = CreateRuleDictionary();
        dictionary.Messages.Add(new FixMessageDefinition
        {
            MsgType = "D",
            Members =
            [
                new FixMemberDefinition
                {
                    Name = "VenueField",
                    Kind = FixMemberKind.Field,
                    ReferenceId = 99,
                    Presence = FixPresence.Conditional,
                    PresenceRule = "(Price * 100) % 5 != 0",
                    RulePresence = FixPresence.Required
                }
            ]
        });
        FixMessage invalidTick = new FixMessage { MsgType = "D", BodyFields = [Field(44, "10.51")] };
        FixMessage validTick = new FixMessage { MsgType = "D", BodyFields = [Field(44, "10.50")] };
        FixMessage missingInput = new FixMessage { MsgType = "D" };

        ValidationResult required = Validate(invalidTick, dictionary, new ValidationContext());
        ValidationResult optional = Validate(validTick, dictionary, new ValidationContext());
        ValidationResult indeterminate = Validate(missingInput, dictionary, new ValidationContext());

        Assert.Multiple(() =>
        {
            Assert.That(required.Issues.ConvertAll(issue => issue.Code), Is.EqualTo(new[] { "conditionally-required-member-missing" }));
            Assert.That(optional.IsValid, Is.True);
            Assert.That(optional.Issues, Is.Empty);
            Assert.That(indeterminate.IsValid, Is.True);
            Assert.That(indeterminate.Issues.ConvertAll(issue => issue.Code), Is.EqualTo(new[] { "conditional-rule-indeterminate" }));
            Assert.That(indeterminate.Issues[0].Severity, Is.EqualTo(ValidationSeverity.Warning));
            Assert.That(indeterminate.Issues[0].Tag, Is.EqualTo(99));
        });
    }

    [Test]
    public void ScenarioInheritanceAppliesOverridesAndMutualExclusionConstraints()
    {
        FixDictionary dictionary = CreateRuleDictionary();
        dictionary.Messages.Add(new FixMessageDefinition { MsgType = "D", Scenario = "child" });
        dictionary.Scenarios.Add(new FixScenarioDefinition
        {
            Name = "base",
            Members =
            [
                new FixMemberDefinition
                {
                    Name = "VenueField",
                    Kind = FixMemberKind.Field,
                    ReferenceId = 99,
                    Presence = FixPresence.Conditional,
                    PresenceRule = "exists(11)",
                    RulePresence = FixPresence.Required
                }
            ],
            Rules = ["not (exists(44) and exists(99))"]
        });
        dictionary.Scenarios.Add(new FixScenarioDefinition
        {
            Name = "child",
            ParentName = "base",
            Members =
            [
                new FixMemberDefinition
                {
                    Name = "VenueField",
                    Kind = FixMemberKind.Field,
                    ReferenceId = 99,
                    Presence = FixPresence.Conditional,
                    PresenceRule = "exists(44)",
                    RulePresence = FixPresence.Forbidden
                }
            ]
        });
        ValidationContext context = new ValidationContext { Scenario = "child" };
        FixMessage noOverrideMatch = new FixMessage { MsgType = "D", BodyFields = [Field(11, "order")] };
        FixMessage conflict = new FixMessage { MsgType = "D", BodyFields = [Field(44, "10"), Field(99, "venue")] };

        ValidationResult noIssue = Validate(noOverrideMatch, dictionary, context);
        ValidationResult violations = Validate(conflict, dictionary, context);

        Assert.Multiple(() =>
        {
            Assert.That(noIssue.IsValid, Is.True);
            Assert.That(noIssue.Issues, Is.Empty);
            Assert.That(violations.Issues.ConvertAll(issue => issue.Code), Is.EqualTo(new[]
            {
                "conditionally-forbidden-member-present",
                "scenario-rule-failed"
            }));
            Assert.That(violations.IsValid, Is.False);
        });
    }

    [Test]
    public void UnsupportedScenariosAndMalformedRulesHaveExplicitDiagnostics()
    {
        FixDictionary dictionary = CreateRuleDictionary();
        dictionary.Messages.Add(new FixMessageDefinition
        {
            MsgType = "D",
            Members =
            [
                new FixMemberDefinition
                {
                    Name = "Price",
                    Kind = FixMemberKind.Field,
                    ReferenceId = 44,
                    Presence = FixPresence.Conditional,
                    PresenceRule = "Price >",
                    RulePresence = FixPresence.Required
                },
                new FixMemberDefinition
                {
                    Name = "VenueField",
                    Kind = FixMemberKind.Field,
                    ReferenceId = 99,
                    Presence = FixPresence.Conditional
                }
            ]
        });
        FixMessage message = new FixMessage { MsgType = "D", BodyFields = [Field(44, "10")] };

        ValidationResult invalidRules = Validate(message, dictionary, new ValidationContext());
        ValidationResult unknownScenario = Validate(message, dictionary, new ValidationContext { Scenario = "missing" });

        Assert.Multiple(() =>
        {
            Assert.That(invalidRules.Issues.ConvertAll(issue => issue.Code), Is.EqualTo(new[]
            {
                "unsupported-conditional-rule",
                "unsupported-conditional-rule"
            }));
            Assert.That(invalidRules.IsValid, Is.False);
            Assert.That(unknownScenario.Issues[0].Code, Is.EqualTo("unsupported-scenario"));
        });
    }

    [Test]
    public void DiscriminatorFieldsAreRequiredAndValidatedAgainstTheirCodeSets()
    {
        FixDictionary dictionary = CreateRuleDictionary();
        dictionary.Fields.Add(new FixFieldDefinition
        {
            Tag = 48,
            Name = "SecurityID",
            FieldType = FixFieldType.String,
            HasDiscriminatorTag = true,
            DiscriminatorTag = 22
        });
        dictionary.Fields.Add(new FixFieldDefinition
        {
            Tag = 22,
            Name = "SecurityIDSource",
            FieldType = FixFieldType.String,
            CodeSetName = "SecurityIDSourceCodeSet"
        });
        dictionary.CodeSets.Add(new FixCodeSetDefinition
        {
            Name = "SecurityIDSourceCodeSet",
            FieldType = FixFieldType.String,
            Codes = [new FixCodeDefinition { Name = "Isin", Value = "4" }]
        });
        FixMessage missing = new FixMessage { MsgType = "D", BodyFields = [Field(48, "US123")] };
        FixMessage invalid = new FixMessage { MsgType = "D", BodyFields = [Field(48, "US123"), Field(22, "9")] };
        FixMessage valid = new FixMessage { MsgType = "D", BodyFields = [Field(48, "US123"), Field(22, "4")] };

        ValidationResult missingResult = Validate(missing, dictionary, new ValidationContext());
        ValidationResult invalidResult = Validate(invalid, dictionary, new ValidationContext());
        ValidationResult validResult = Validate(valid, dictionary, new ValidationContext());

        Assert.Multiple(() =>
        {
            Assert.That(missingResult.Issues.ConvertAll(issue => issue.Code), Is.EqualTo(new[] { "field-discriminator-missing" }));
            Assert.That(missingResult.Issues[0].Tag, Is.EqualTo(22));
            Assert.That(invalidResult.Issues.ConvertAll(issue => issue.Code), Is.EqualTo(new[] { "invalid-field-discriminator" }));
            Assert.That(validResult.IsValid, Is.True);
            Assert.That(validResult.Issues, Is.Empty);
        });
    }

    [Test]
    public void GroupDiscriminatorsAndConditionalMembersUseTheCurrentEntry()
    {
        FixDictionary dictionary = CreateRuleDictionary();
        dictionary.Fields.Add(new FixFieldDefinition { Tag = 453, Name = "NoPartyIDs", FieldType = FixFieldType.NumInGroup });
        dictionary.Fields.Add(new FixFieldDefinition
        {
            Tag = 448,
            Name = "PartyID",
            FieldType = FixFieldType.String,
            HasDiscriminatorTag = true,
            DiscriminatorTag = 447
        });
        dictionary.Fields.Add(new FixFieldDefinition { Tag = 447, Name = "PartyIDSource", FieldType = FixFieldType.Char, CodeSetName = "PartySourceCodes" });
        dictionary.Fields.Add(new FixFieldDefinition { Tag = 452, Name = "PartyRole", FieldType = FixFieldType.Int });
        dictionary.CodeSets.Add(new FixCodeSetDefinition
        {
            Name = "PartySourceCodes",
            FieldType = FixFieldType.Char,
            Codes = [new FixCodeDefinition { Name = "Bic", Value = "B" }]
        });
        dictionary.Groups.Add(new FixGroupDefinition
        {
            Id = 200,
            Name = "Parties",
            CountTag = 453,
            DelimiterTag = 448,
            Members =
            [
                new FixMemberDefinition { Name = "PartyID", Kind = FixMemberKind.Field, ReferenceId = 448 },
                new FixMemberDefinition { Name = "PartyIDSource", Kind = FixMemberKind.Field, ReferenceId = 447 },
                new FixMemberDefinition
                {
                    Name = "PartyRole",
                    Kind = FixMemberKind.Field,
                    ReferenceId = 452,
                    Presence = FixPresence.Conditional,
                    PresenceRule = "PartyID == \"BROKER\"",
                    RulePresence = FixPresence.Required
                }
            ]
        });
        dictionary.Messages.Add(new FixMessageDefinition
        {
            MsgType = "D",
            Members = [new FixMemberDefinition { Name = "Parties", Kind = FixMemberKind.Group, ReferenceId = 200 }]
        });
        FixGroup group = new FixGroup { CountTag = 453, DelimiterTag = 448 };
        group.Entries.Add(new FixGroupEntry { Fields = [Field(448, "BROKER")] });
        FixMessage message = new FixMessage { MsgType = "D", BodyGroups = [group] };

        ValidationResult result = Validate(message, dictionary, new ValidationContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.Issues.ConvertAll(issue => issue.Code), Is.EqualTo(new[]
            {
                "conditionally-required-member-missing",
                "field-discriminator-missing"
            }));
            Assert.That(result.Issues[0].GroupPath, Is.EqualTo("453[0]"));
            Assert.That(result.Issues[1].GroupPath, Is.EqualTo("453[0]"));
        });
    }

    private static ValidationResult Validate(FixMessage message, FixDictionary dictionary, ValidationContext context)
    {
        ValidationOptions options = new ValidationOptions { Layers = ValidationLayer.ConditionalRule };
        List<ValidationIssue> issues = ConditionalValidator.ValidateConditionalRules(message, dictionary, options, context);
        return ValidationCoordinator.CoordinateValidation(issues, options);
    }

    private static FixDictionary CreateRuleDictionary()
    {
        return new FixDictionary
        {
            Fields =
            [
                new FixFieldDefinition { Tag = 11, Name = "ClOrdID", FieldType = FixFieldType.String },
                new FixFieldDefinition { Tag = 44, Name = "Price", FieldType = FixFieldType.Price },
                new FixFieldDefinition { Tag = 99, Name = "VenueField", FieldType = FixFieldType.String }
            ]
        };
    }

    private static FixField Field(int tag, string value)
    {
        return new FixField { Tag = tag, Value = Encoding.ASCII.GetBytes(value) };
    }
}
