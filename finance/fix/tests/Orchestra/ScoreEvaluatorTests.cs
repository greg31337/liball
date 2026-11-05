using System.Text;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class ScoreEvaluatorTests
{
    [Test]
    public void EvaluatesFieldStateArithmeticLogicalAndCodeExpressions()
    {
        FixDictionary dictionary = CreateDictionary();
        OrchestraRuleEvaluator evaluator = new OrchestraRuleEvaluator(dictionary);
        FixMessage message = new FixMessage();
        message.BodyFields.Add(CreateField(40, "3"));
        message.BodyFields.Add(CreateField(44, "10.5"));
        ValidationContext context = new ValidationContext();
        context.ExternalState["market.adjustment"] = "1.0";
        FixMemberDefinition member = CreateRule("OrdType == ^Stop and Price + $market.adjustment >= 11.5");

        RuleEvaluationResult result = evaluator.EvaluateRule(member, message, context);

        Assert.Multiple(() =>
        {
            Assert.That(result.WasEvaluated, Is.True, result.ErrorMessage);
            Assert.That(result.ConditionMatched, Is.True);
            Assert.That(result.Presence, Is.EqualTo(FixPresence.Required));
            Assert.That(result.ErrorCode, Is.Empty);
            Assert.That(result.Issues, Is.Empty);
        });
    }

    [Test]
    public void EvaluatesSetRangeExistenceAndPreviousMessageReferences()
    {
        OrchestraRuleEvaluator evaluator = new OrchestraRuleEvaluator(CreateDictionary());
        FixMessage previous = new FixMessage();
        previous.BodyFields.Add(CreateField(11, "previous-order"));
        FixMessage message = new FixMessage();
        message.BodyFields.Add(CreateField(40, "4"));
        message.BodyFields.Add(CreateField(44, "15"));
        ValidationContext context = new ValidationContext();
        context.PreviousMessage = previous;
        FixMemberDefinition member = CreateRule("in.ClOrdID == \"previous-order\" and OrdType in {^Stop, ^StopLimit} and Price between 10 and 20 and exists(44) and !exists(54)");

        RuleEvaluationResult result = evaluator.EvaluateRule(member, message, context);

        Assert.Multiple(() =>
        {
            Assert.That(result.WasEvaluated, Is.True, result.ErrorMessage);
            Assert.That(result.ConditionMatched, Is.True);
        });
    }

    [Test]
    public void ResolvesOrdinalAndPredicateSelectedRepeatingGroupFields()
    {
        OrchestraRuleEvaluator evaluator = new OrchestraRuleEvaluator(CreateDictionary());
        FixMessage message = new FixMessage();
        FixGroup parties = new FixGroup { CountTag = 453, DelimiterTag = 448 };
        parties.Entries.Add(new FixGroupEntry
        {
            Fields =
            {
                CreateField(448, "CUSTOMER"),
                CreateField(452, "3")
            }
        });
        parties.Entries.Add(new FixGroupEntry
        {
            Fields =
            {
                CreateField(448, "BROKER"),
                CreateField(452, "1")
            }
        });
        message.BodyGroups.Add(parties);
        FixMemberDefinition member = CreateRule("Parties[2].PartyID == \"BROKER\" and Parties[PartyRole==^ExecutingFirm].PartyID == \"BROKER\"");

        RuleEvaluationResult result = evaluator.EvaluateRule(member, message, new ValidationContext());

        Assert.Multiple(() =>
        {
            Assert.That(result.WasEvaluated, Is.True, result.ErrorMessage);
            Assert.That(result.ConditionMatched, Is.True);
        });
    }

    [Test]
    public void AssignmentReturnsTargetLexicalValueWithoutMutatingTheMessage()
    {
        OrchestraRuleEvaluator evaluator = new OrchestraRuleEvaluator(CreateDictionary());
        FixMessage message = new FixMessage();
        message.BodyFields.Add(CreateField(44, "10.5"));
        FixMemberDefinition codeAssignment = CreateRule("out.OrdType = ^StopLimit");
        FixMemberDefinition stateAssignment = CreateRule("$order.notional = Price * 2");

        RuleEvaluationResult code = evaluator.EvaluateRule(codeAssignment, message, new ValidationContext());
        RuleEvaluationResult state = evaluator.EvaluateRule(stateAssignment, message, new ValidationContext());

        Assert.Multiple(() =>
        {
            Assert.That(code.WasEvaluated, Is.True, code.ErrorMessage);
            Assert.That(code.ConditionMatched, Is.True);
            Assert.That(code.AssignedValue, Is.EqualTo("4"));
            Assert.That(state.WasEvaluated, Is.True, state.ErrorMessage);
            Assert.That(state.AssignedValue, Is.EqualTo("21"));
            Assert.That(message.BodyFields, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void EvaluatesPublishedOrchestraArithmeticAndStateAssignmentExamples()
    {
        OrchestraRuleEvaluator evaluator = new OrchestraRuleEvaluator(CreateDictionary());
        FixMessage validTick = new FixMessage();
        validTick.BodyFields.Add(CreateField(44, "10.50"));
        FixMessage invalidTick = new FixMessage();
        invalidTick.BodyFields.Add(CreateField(44, "10.51"));

        RuleEvaluationResult valid = evaluator.EvaluateRule(CreateRule("(Price * 100) % 5 != 0"), validTick, new ValidationContext());
        RuleEvaluationResult invalid = evaluator.EvaluateRule(CreateRule("(Price * 100) % 5 != 0"), invalidTick, new ValidationContext());
        RuleEvaluationResult assignment = evaluator.EvaluateRule(CreateRule("$participant.RiskLimitAmount=15000"), new FixMessage(), new ValidationContext());

        Assert.Multiple(() =>
        {
            Assert.That(valid.WasEvaluated, Is.True, valid.ErrorMessage);
            Assert.That(valid.ConditionMatched, Is.False);
            Assert.That(invalid.WasEvaluated, Is.True, invalid.ErrorMessage);
            Assert.That(invalid.ConditionMatched, Is.True);
            Assert.That(assignment.WasEvaluated, Is.True, assignment.ErrorMessage);
            Assert.That(assignment.AssignedValue, Is.EqualTo("15000"));
        });
    }

    [Test]
    public void MissingInputsAreIndeterminateUnlessLogicalResultIsAlreadyKnown()
    {
        OrchestraRuleEvaluator evaluator = new OrchestraRuleEvaluator(CreateDictionary());
        FixMessage message = new FixMessage();

        RuleEvaluationResult unavailable = evaluator.EvaluateRule(CreateRule("$market.missing == 1"), message, new ValidationContext());
        RuleEvaluationResult falseAndMissing = evaluator.EvaluateRule(CreateRule("false and $market.missing == 1"), message, new ValidationContext());
        RuleEvaluationResult missingOrTrue = evaluator.EvaluateRule(CreateRule("$market.missing == 1 or true"), message, new ValidationContext());

        Assert.Multiple(() =>
        {
            Assert.That(unavailable.WasEvaluated, Is.False);
            Assert.That(unavailable.ErrorCode, Is.EqualTo("external-state-unavailable"));
            Assert.That(unavailable.Issues, Has.Count.EqualTo(1));
            Assert.That(falseAndMissing.WasEvaluated, Is.True, falseAndMissing.ErrorMessage);
            Assert.That(falseAndMissing.ConditionMatched, Is.False);
            Assert.That(missingOrTrue.WasEvaluated, Is.True, missingOrTrue.ErrorMessage);
            Assert.That(missingOrTrue.ConditionMatched, Is.True);
        });
    }

    [Test]
    public void MissingFieldAndGroupSelectionsHaveStableIndeterminateResults()
    {
        OrchestraRuleEvaluator evaluator = new OrchestraRuleEvaluator(CreateDictionary());
        FixMessage message = new FixMessage();

        RuleEvaluationResult missingField = evaluator.EvaluateRule(CreateRule("Price > 0"), message, new ValidationContext());
        RuleEvaluationResult missingGroup = evaluator.EvaluateRule(CreateRule("Parties[2].PartyID == \"BROKER\""), message, new ValidationContext());
        RuleEvaluationResult existsMissing = evaluator.EvaluateRule(CreateRule("exists(Price) or exists(Parties[1].PartyID)"), message, new ValidationContext());

        Assert.Multiple(() =>
        {
            Assert.That(missingField.WasEvaluated, Is.False);
            Assert.That(missingField.ErrorCode, Is.EqualTo("field-unavailable"));
            Assert.That(missingField.Issues, Has.Count.EqualTo(1));
            Assert.That(missingGroup.WasEvaluated, Is.False);
            Assert.That(missingGroup.ErrorCode, Is.EqualTo("group-entry-unavailable"));
            Assert.That(missingGroup.Issues, Has.Count.EqualTo(1));
            Assert.That(existsMissing.WasEvaluated, Is.True, existsMissing.ErrorMessage);
            Assert.That(existsMissing.ConditionMatched, Is.False);
            Assert.That(existsMissing.Issues, Is.Empty);
        });
    }

    [Test]
    public void ReportsInvalidOperandsAndMalformedFieldValuesWithoutThrowing()
    {
        OrchestraRuleEvaluator evaluator = new OrchestraRuleEvaluator(CreateDictionary());
        FixMessage malformed = new FixMessage();
        malformed.BodyFields.Add(CreateField(44, "not-a-price"));

        RuleEvaluationResult division = evaluator.EvaluateRule(CreateRule("10 / 0 == 1"), new FixMessage(), new ValidationContext());
        RuleEvaluationResult invalidField = evaluator.EvaluateRule(CreateRule("Price > 0"), malformed, new ValidationContext());
        RuleEvaluationResult invalidSyntax = evaluator.EvaluateRule(CreateRule("Price >"), malformed, new ValidationContext());

        Assert.Multiple(() =>
        {
            Assert.That(division.WasEvaluated, Is.False);
            Assert.That(division.ErrorCode, Is.EqualTo("division-by-zero"));
            Assert.That(invalidField.WasEvaluated, Is.False);
            Assert.That(invalidField.ErrorCode, Is.EqualTo("invalid-field-value"));
            Assert.That(invalidSyntax.WasEvaluated, Is.False);
            Assert.That(invalidSyntax.ErrorCode, Is.EqualTo("expected-expression"));
        });
    }

    private static FixDictionary CreateDictionary()
    {
        FixDictionary dictionary = new FixDictionary();
        dictionary.Fields.Add(new FixFieldDefinition { Tag = 11, Name = "ClOrdID", FieldType = FixFieldType.String });
        dictionary.Fields.Add(new FixFieldDefinition { Tag = 40, Name = "OrdType", FieldType = FixFieldType.Char, CodeSetName = "OrdTypeCodeSet" });
        dictionary.Fields.Add(new FixFieldDefinition { Tag = 44, Name = "Price", FieldType = FixFieldType.Price });
        dictionary.Fields.Add(new FixFieldDefinition { Tag = 54, Name = "Side", FieldType = FixFieldType.Char });
        dictionary.Fields.Add(new FixFieldDefinition { Tag = 453, Name = "NoPartyIDs", FieldType = FixFieldType.NumInGroup });
        dictionary.Fields.Add(new FixFieldDefinition { Tag = 448, Name = "PartyID", FieldType = FixFieldType.String });
        dictionary.Fields.Add(new FixFieldDefinition { Tag = 452, Name = "PartyRole", FieldType = FixFieldType.Int, CodeSetName = "PartyRoleCodeSet" });
        dictionary.CodeSets.Add(new FixCodeSetDefinition
        {
            Name = "OrdTypeCodeSet",
            FieldType = FixFieldType.Char,
            Codes =
            {
                new FixCodeDefinition { Name = "Stop", Value = "3" },
                new FixCodeDefinition { Name = "StopLimit", Value = "4" }
            }
        });
        dictionary.CodeSets.Add(new FixCodeSetDefinition
        {
            Name = "PartyRoleCodeSet",
            FieldType = FixFieldType.Int,
            Codes =
            {
                new FixCodeDefinition { Name = "ExecutingFirm", Value = "1" }
            }
        });
        dictionary.Groups.Add(new FixGroupDefinition
        {
            Id = 453,
            Name = "Parties",
            CountTag = 453,
            DelimiterTag = 448
        });
        return dictionary;
    }

    private static FixMemberDefinition CreateRule(string expression)
    {
        return new FixMemberDefinition
        {
            Name = "TestMember",
            Presence = FixPresence.Required,
            PresenceRule = expression
        };
    }

    private static FixField CreateField(int tag, string value)
    {
        return new FixField
        {
            Tag = tag,
            Value = Encoding.ASCII.GetBytes(value)
        };
    }
}
