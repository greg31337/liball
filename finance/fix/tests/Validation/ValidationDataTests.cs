using fix;

namespace fix.Tests;

[TestFixture]
public class ValidationDataTests
{
    [Test]
    public void ValidationOptionsDefaultToStrictBoundedValidation()
    {
        ValidationOptions options = new ValidationOptions();

        Assert.Multiple(() =>
        {
            Assert.That(options.Layers, Is.EqualTo(ValidationLayer.All));
            Assert.That(options.MaxMessageSize, Is.EqualTo(FixDefaults.MaxMessageSize));
            Assert.That(options.MaxFieldSize, Is.EqualTo(FixDefaults.MaxFieldSize));
            Assert.That(options.MaxFieldsPerMessage, Is.EqualTo(FixDefaults.MaxFieldsPerMessage));
            Assert.That(options.MaxIssues, Is.EqualTo(FixDefaults.MaxDiagnostics));
            Assert.That(options.MinimumSeverity, Is.EqualTo(ValidationSeverity.Information));
            Assert.That(options.RejectEmptyValues, Is.True);
            Assert.That(options.RejectDuplicateFields, Is.True);
            Assert.That(options.RejectUnknownFields, Is.True);
            Assert.That(options.AllowUserDefinedFields, Is.False);
            Assert.That(options.CheckFieldOrder, Is.True);
            Assert.That(options.CheckRequiredFields, Is.True);
            Assert.That(options.CheckGroups, Is.True);
            Assert.That(options.CheckConditionalRules, Is.True);
        });
    }

    [Test]
    public void ValidationContextAndIssuesRetainExternalAndRejectMappingState()
    {
        FixMessage previous = new FixMessage { MsgType = "D" };
        ValidationContext context = new ValidationContext
        {
            DictionaryIdentity = new FixDictionaryIdentity
            {
                BeginString = FixBeginStrings.Fixt11,
                ApplicationVersion = FixApplicationVersions.Fix50Sp2
            },
            ExpectedBeginString = FixBeginStrings.Fixt11,
            DefaultApplicationVersion = FixApplicationVersions.Fix50Sp2,
            Scenario = "venue",
            PreviousMessage = previous,
            ExternalState =
            {
                ["market.status"] = "open",
                ["account.approved"] = "true"
            }
        };
        ValidationIssue missing = new ValidationIssue
        {
            Severity = ValidationSeverity.Error,
            Layer = ValidationLayer.DictionaryGrammar,
            Code = "required-field-missing",
            Message = "Side is required.",
            HasTag = true,
            Tag = 54,
            GroupPath = "453[1]/802[0]",
            MsgType = "D",
            HasSourceOffset = true,
            SourceOffset = 37,
            HasSessionRejectReason = true,
            SessionRejectReason = 1,
            HasBusinessRejectReason = true,
            BusinessRejectReason = 5
        };
        ValidationResult result = new ValidationResult
        {
            IsValid = false,
            Issues = [missing]
        };

        Assert.Multiple(() =>
        {
            Assert.That(context.DictionaryIdentity?.ApplicationVersion, Is.EqualTo("9"));
            Assert.That(context.PreviousMessage, Is.SameAs(previous));
            Assert.That(context.ExternalState["market.status"], Is.EqualTo("open"));
            Assert.That(context.ExternalState["account.approved"], Is.EqualTo("true"));
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Issues[0], Is.SameAs(missing));
            Assert.That(missing.Tag, Is.EqualTo(54));
            Assert.That(missing.GroupPath, Is.EqualTo("453[1]/802[0]"));
            Assert.That(missing.SourceOffset, Is.EqualTo(37));
            Assert.That(missing.SessionRejectReason, Is.EqualTo(1));
            Assert.That(missing.BusinessRejectReason, Is.EqualTo(5));
        });
    }

    [Test]
    public void RuleEvaluationDistinguishesUnavailableInputsFromMatchedRules()
    {
        RuleEvaluationResult unavailable = new RuleEvaluationResult
        {
            WasEvaluated = false,
            ErrorCode = "external-state-unavailable",
            ErrorMessage = "market.status is unavailable"
        };
        ValidationIssue issue = new ValidationIssue
        {
            Severity = ValidationSeverity.Warning,
            Layer = ValidationLayer.ConditionalRule,
            Code = "rule-input-unavailable"
        };
        unavailable.Issues.Add(issue);

        RuleEvaluationResult matched = new RuleEvaluationResult
        {
            WasEvaluated = true,
            ConditionMatched = true,
            Presence = FixPresence.Required,
            AssignedValue = "1"
        };

        Assert.Multiple(() =>
        {
            Assert.That(unavailable.WasEvaluated, Is.False);
            Assert.That(unavailable.ConditionMatched, Is.False);
            Assert.That(unavailable.ErrorCode, Is.EqualTo("external-state-unavailable"));
            Assert.That(unavailable.Issues[0], Is.SameAs(issue));
            Assert.That(matched.WasEvaluated, Is.True);
            Assert.That(matched.ConditionMatched, Is.True);
            Assert.That(matched.Presence, Is.EqualTo(FixPresence.Required));
            Assert.That(matched.AssignedValue, Is.EqualTo("1"));
        });
    }
}
