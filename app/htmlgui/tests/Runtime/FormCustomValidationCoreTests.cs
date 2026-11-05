using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class FormCustomValidationCoreTests
{
    [Test]
    public void QueuesFieldCrossFieldAndFormCallbacksWithCopiedVersionedSnapshots()
    {
        FormCore forms = CreateForms();
        FormCustomValidationCore validation = new FormCustomValidationCore(forms);

        FormCustomValidationResult field = validation.BeginCustomValidation("account", "email", "available", false);
        FormCustomValidationResult cross = validation.BeginCustomValidation("account", "confirmation", "matches-account", true);
        FormCustomValidationResult form = validation.BeginCustomValidation("account", null, "account-policy", true);
        FormValidationInvocation[] invocations = validation.DrainCustomValidationInvocations();
        invocations[0].Value!.StringValue = "mutated snapshot";

        Assert.Multiple(() =>
        {
            Assert.That(field.Success, Is.True);
            Assert.That(field.Generation, Is.EqualTo(1));
            Assert.That(field.ValueVersion, Is.EqualTo(0));
            Assert.That(cross.Generation, Is.EqualTo(2));
            Assert.That(form.Generation, Is.EqualTo(3));
            Assert.That(invocations.Select(item => item.Kind), Is.EqualTo(new string[] { "custom-field", "custom-cross-field", "custom-form" }));
            Assert.That(invocations.Select(item => item.HandlerId), Is.EqualTo(new string[] { "availability", "account-match", "account-policy-handler" }));
            Assert.That(invocations.Select(item => item.ReturnsTask), Is.EqualTo(new bool[] { false, true, true }));
            Assert.That(invocations[0].FieldKey, Is.EqualTo("email"));
            Assert.That(invocations[0].Parameters.Single().Value.StringValue, Is.EqualTo("remote"));
            Assert.That(invocations[1].Values.Select(item => item.Key), Is.EqualTo(new string[] { "confirmation", "email" }));
            Assert.That(invocations[2].FieldKey, Is.Null);
            Assert.That(invocations[2].Value, Is.Null);
            Assert.That(forms.ReadForm("account").Form!.Fields.Single(item => item.Key == "email").Value.StringValue, Is.EqualTo("ada@example.test"));
            Assert.That(forms.ReadForm("account").Form!.State.IsPending, Is.True);
            Assert.That(forms.ReadForm("account").Form!.Rules.Single().HandlerId, Is.EqualTo("account-policy-handler"));
        });
    }

    [Test]
    public void CompletesCallbacksWithNormalizedCopiedIssuesAndAccuratePendingState()
    {
        FormCore forms = CreateForms();
        FormCustomValidationCore validation = new FormCustomValidationCore(forms);
        FormCustomValidationResult begun = validation.BeginCustomValidation("account", "email", "available", false);
        validation.DrainCustomValidationInvocations();
        UiValidationIssue source = new UiValidationIssue
        {
            Id = "email-taken",
            Code = "account.email-taken",
            Message = "That address is already registered.",
            Severity = UiValidationSeverity.Error,
            Source = "untrusted",
            ValueVersion = 999
        };

        FormCustomValidationResult completed = validation.CompleteCustomValidation("account", "email", "available", begun.Generation, begun.ValueVersion, true, new UiValidationIssue[] { source }, null);
        source.Message = "mutated source";
        string copiedMessage = completed.Issues[0].Message;
        completed.Issues[0].Message = "mutated result";
        UiForm retained = forms.ReadForm("account").Form!;

        Assert.Multiple(() =>
        {
            Assert.That(completed.Success, Is.True);
            Assert.That(completed.Applied, Is.True);
            Assert.That(completed.IsPending, Is.False);
            Assert.That(completed.Issues.Single().FieldKey, Is.EqualTo("email"));
            Assert.That(completed.Issues.Single().Source, Is.EqualTo("custom"));
            Assert.That(completed.Issues.Single().ValueVersion, Is.EqualTo(0));
            Assert.That(copiedMessage, Is.EqualTo("That address is already registered."));
            Assert.That(retained.Fields.Single(item => item.Key == "email").IsPending, Is.False);
            Assert.That(retained.State.IsPending, Is.False);
            Assert.That(retained.State.CanSubmit, Is.True);
            Assert.That(validation.DrainCustomValidationErrors(), Is.Empty);
        });
    }

    [Test]
    public void SuppressesResultsForChangedValuesAndClearsTheirPendingState()
    {
        FormCore forms = CreateForms();
        FormCustomValidationCore validation = new FormCustomValidationCore(forms);
        FormCustomValidationResult begun = validation.BeginCustomValidation("account", "email", "available", true);
        validation.DrainCustomValidationInvocations();
        forms.UpdateFormFieldValue("account", "email", Text("grace@example.test"));

        FormCustomValidationResult stale = validation.CompleteCustomValidation("account", "email", "available", begun.Generation, begun.ValueVersion, true, System.Array.Empty<UiValidationIssue>(), null);
        UiForm retained = forms.ReadForm("account").Form!;

        Assert.Multiple(() =>
        {
            Assert.That(stale.Success, Is.True);
            Assert.That(stale.Applied, Is.False);
            Assert.That(stale.Code, Is.EqualTo("validation.stale-result"));
            Assert.That(retained.State.Version, Is.EqualTo(1));
            Assert.That(retained.State.IsPending, Is.False);
            Assert.That(retained.Fields.Single(item => item.Key == "email").IsPending, Is.False);
        });
    }

    [Test]
    public void PreservesPendingStateOwnedByAnotherValidationSource()
    {
        FormCore forms = CreateForms();
        forms.SetFormFieldPending("account", "email", true);
        FormCustomValidationCore validation = new FormCustomValidationCore(forms);
        FormCustomValidationResult begun = validation.BeginCustomValidation("account", "email", "available", false);
        validation.DrainCustomValidationInvocations();

        FormCustomValidationResult completed = validation.CompleteCustomValidation("account", "email", "available", begun.Generation, begun.ValueVersion, true, System.Array.Empty<UiValidationIssue>(), null);

        Assert.Multiple(() =>
        {
            Assert.That(completed.Success, Is.True);
            Assert.That(completed.Form!.Fields.Single(item => item.Key == "email").IsPending, Is.True);
            Assert.That(completed.Form.State.IsPending, Is.True);
            Assert.That(completed.Form.State.CanSubmit, Is.False);
        });
    }

    [Test]
    public void NewerCallbackGenerationSupersedesPendingWorkWithoutAcceptingOldCompletion()
    {
        FormCore forms = CreateForms();
        FormCustomValidationCore validation = new FormCustomValidationCore(forms);
        FormCustomValidationResult first = validation.BeginCustomValidation("account", "email", "available", true);
        validation.DrainCustomValidationInvocations();
        FormCustomValidationResult second = validation.BeginCustomValidation("account", "email", "available", true);
        FormValidationInvocation currentInvocation = validation.DrainCustomValidationInvocations().Single();

        FormCustomValidationResult stale = validation.CompleteCustomValidation("account", "email", "available", first.Generation, first.ValueVersion, true, System.Array.Empty<UiValidationIssue>(), null);
        UiForm whileCurrentPending = forms.ReadForm("account").Form!;
        FormCustomValidationResult current = validation.CompleteCustomValidation("account", "email", "available", second.Generation, second.ValueVersion, true, System.Array.Empty<UiValidationIssue>(), null);

        Assert.Multiple(() =>
        {
            Assert.That(second.Generation, Is.GreaterThan(first.Generation));
            Assert.That(currentInvocation.Generation, Is.EqualTo(second.Generation));
            Assert.That(stale.Code, Is.EqualTo("validation.stale-result"));
            Assert.That(whileCurrentPending.State.IsPending, Is.True);
            Assert.That(current.Applied, Is.True);
            Assert.That(current.Form!.State.IsPending, Is.False);
        });
    }

    [Test]
    public void ReverseCallbackCompletionStillProducesCanonicalFieldOrderedSummary()
    {
        FormCore forms = CreateForms();
        FormValidationFlowCore flow = new FormValidationFlowCore(forms);
        FormValidationFlowResult confirmationBegun = flow.ValidateFormTrigger("account", "confirmation", UiValidationTrigger.Input);
        FormValidationFlowResult emailBegun = flow.ValidateFormTrigger("account", "email", UiValidationTrigger.Input);
        FormValidationInvocation[] invocations = flow.Custom.DrainCustomValidationInvocations();
        FormValidationInvocation confirmation = invocations.Single(item => item.FieldKey == "confirmation");
        FormValidationInvocation email = invocations.Single(item => item.FieldKey == "email");

        flow.CompleteFormCustomValidation("account", "email", "available", email.Generation, email.ValueVersion, true, new UiValidationIssue[]
        {
            Issue("email-issue", "email.issue", "Email issue")
        }, null);
        FormValidationFlowResult completed = flow.CompleteFormCustomValidation("account", "confirmation", "matches-account", confirmation.Generation, confirmation.ValueVersion, true, new UiValidationIssue[]
        {
            Issue("confirmation-issue", "confirmation.issue", "Confirmation issue")
        }, null);

        Assert.Multiple(() =>
        {
            Assert.That(confirmationBegun.ScheduledCallbackCount + emailBegun.ScheduledCallbackCount, Is.EqualTo(2));
            Assert.That(invocations.Select(item => item.FieldKey), Is.EqualTo(new string[] { "confirmation", "email" }));
            Assert.That(completed.Form!.Validation.Issues.Select(item => item.FieldKey), Is.EqualTo(new string[] { "confirmation", "email" }));
            Assert.That(completed.Form.Validation.Issues.Select(item => item.Id), Is.EqualTo(new string[] { "confirmation-issue", "email-issue" }));
            Assert.That(completed.Form.State.IsPending, Is.False);
        });
    }

    [Test]
    public void CancelsQueuedAndDispatchedCallbacksAndRejectsTheirResults()
    {
        FormCore forms = CreateForms();
        FormCustomValidationCore validation = new FormCustomValidationCore(forms);
        FormCustomValidationResult queued = validation.BeginCustomValidation("account", null, "account-policy", true);
        FormCustomValidationResult canceledQueued = validation.CancelCustomValidation("account", null, "account-policy");
        FormValidationInvocation[] removed = validation.DrainCustomValidationInvocations();
        FormCustomValidationResult dispatched = validation.BeginCustomValidation("account", "confirmation", "matches-account", true);
        validation.DrainCustomValidationInvocations();
        FormCustomValidationResult canceledDispatched = validation.CancelCustomValidation("account", "confirmation", "matches-account");
        FormCustomValidationResult stale = validation.CompleteCustomValidation("account", "confirmation", "matches-account", dispatched.Generation, dispatched.ValueVersion, true, System.Array.Empty<UiValidationIssue>(), null);
        FormCustomValidationResult canceledAgain = validation.CancelCustomValidation("account", "confirmation", "matches-account");

        Assert.Multiple(() =>
        {
            Assert.That(queued.IsPending, Is.True);
            Assert.That(canceledQueued.Applied, Is.True);
            Assert.That(canceledQueued.IsPending, Is.False);
            Assert.That(removed, Is.Empty);
            Assert.That(canceledDispatched.Applied, Is.True);
            Assert.That(stale.Code, Is.EqualTo("validation.stale-result"));
            Assert.That(canceledAgain.Applied, Is.False);
            Assert.That(canceledAgain.Form!.State.IsPending, Is.False);
        });
    }

    [Test]
    public void RoutesHandlerAndMalformedResultFailuresWithCallbackContext()
    {
        FormCore forms = CreateForms();
        FormCustomValidationCore validation = new FormCustomValidationCore(forms);
        FormCustomValidationResult thrown = validation.BeginCustomValidation("account", null, "account-policy", true);
        validation.DrainCustomValidationInvocations();
        validation.CompleteCustomValidation("account", null, "account-policy", thrown.Generation, thrown.ValueVersion, false, null, null);
        FormCustomValidationResult nullResult = validation.BeginCustomValidation("account", "email", "available", false);
        validation.DrainCustomValidationInvocations();
        validation.CompleteCustomValidation("account", "email", "available", nullResult.Generation, nullResult.ValueVersion, true, null, null);
        FormCustomValidationResult invalid = validation.BeginCustomValidation("account", "confirmation", "matches-account", false);
        validation.DrainCustomValidationInvocations();
        UiValidationIssue malformed = Issue("bad", "bad", "Bad");
        malformed.Severity = (UiValidationSeverity)999;
        validation.CompleteCustomValidation("account", "confirmation", "matches-account", invalid.Generation, invalid.ValueVersion, true, new UiValidationIssue[] { malformed }, null);
        FormValidationError[] errors = validation.DrainCustomValidationErrors();
        string routedExceptionCode = errors[0].Code;
        errors[0].Code = "mutated";

        Assert.Multiple(() =>
        {
            Assert.That(errors.Select(item => item.Code), Is.EqualTo(new string[] { "mutated", "validation.null-handler-result", "validation.invalid-issue-severity" }));
            Assert.That(routedExceptionCode, Is.EqualTo("validation.handler-exception"));
            Assert.That(errors[0].FormKey, Is.EqualTo("account"));
            Assert.That(errors[0].RuleKey, Is.EqualTo("account-policy"));
            Assert.That(errors[0].HandlerId, Is.EqualTo("account-policy-handler"));
            Assert.That(errors[1].FieldKey, Is.EqualTo("email"));
            Assert.That(errors[1].Generation, Is.EqualTo(nullResult.Generation));
            Assert.That(validation.DrainCustomValidationErrors(), Is.Empty);
            Assert.That(forms.ReadForm("account").Form!.State.IsPending, Is.False);
        });
    }

    [Test]
    public void RejectsUnknownRulesInvalidScopesAndMissingHandlersExceptionlessly()
    {
        FormCore forms = CreateForms();
        UiField field = forms.Forms.Single().Fields.Single(item => item.Key == "email");
        field.Rules = field.Rules.Concat(new UiValidationRule[]
        {
            new UiValidationRule { Key = "built-in", Kind = "required", HandlerId = "unused" },
            new UiValidationRule { Key = "missing-handler", Kind = "custom-field" }
        }).ToArray();
        FormCustomValidationCore validation = new FormCustomValidationCore(forms);

        Assert.Multiple(() =>
        {
            Assert.That(validation.BeginCustomValidation(null, "email", "available", false).Code, Is.EqualTo("form.invalid-key"));
            Assert.That(validation.BeginCustomValidation("account", "missing", "available", false).Code, Is.EqualTo("form.unknown-field"));
            Assert.That(validation.BeginCustomValidation("account", "email", "missing", false).Code, Is.EqualTo("validation.unknown-rule"));
            Assert.That(validation.BeginCustomValidation("account", "email", "built-in", false).Code, Is.EqualTo("validation.invalid-custom-scope"));
            Assert.That(validation.BeginCustomValidation("account", "email", "missing-handler", false).Code, Is.EqualTo("validation.invalid-handler"));
            Assert.That(validation.BeginCustomValidation("account", null, "available", false).Code, Is.EqualTo("validation.unknown-rule"));
            Assert.That(validation.CancelCustomValidation("account", "email", "missing").Code, Is.EqualTo("validation.unknown-request"));
        });
    }

    private static FormCore CreateForms()
    {
        UiField email = new UiField
        {
            Key = "email",
            Name = "email",
            Value = Text("ada@example.test"),
            Rules = new UiValidationRule[]
            {
                Rule("available", "custom-field", "availability", Entry("mode", Text("remote")))
            }
        };
        UiField confirmation = new UiField
        {
            Key = "confirmation",
            Name = "confirmation",
            Value = Text("ada@example.test"),
            Rules = new UiValidationRule[]
            {
                Rule("matches-account", "custom-cross-field", "account-match")
            }
        };
        UiForm form = new UiForm
        {
            Key = "account",
            Fields = new UiField[] { email, confirmation },
            Rules = new UiValidationRule[]
            {
                Rule("account-policy", "custom-form", "account-policy-handler")
            }
        };
        FormCore forms = new FormCore();
        forms.RegisterForm(form);
        return forms;
    }

    private static UiValidationRule Rule(string key, string kind, string handlerId, params UiValueEntry[] parameters)
    {
        return new UiValidationRule
        {
            Key = key,
            Kind = kind,
            HandlerId = handlerId,
            Parameters = parameters
        };
    }

    private static UiValidationIssue Issue(string id, string code, string message)
    {
        return new UiValidationIssue { Id = id, Code = code, Message = message };
    }

    private static UiValueEntry Entry(string key, UiValue value)
    {
        return new UiValueEntry { Key = key, Value = value };
    }

    private static UiValue Text(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }
}
