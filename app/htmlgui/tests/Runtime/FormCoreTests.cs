using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class FormCoreTests
{
    [Test]
    public void RegistersCopiedFieldsOrdinallyWithInitialEditState()
    {
        FormCore core = new FormCore();
        UiField zeta = Field("zeta", "Zeta", "last");
        UiField alpha = Field("alpha", "Alpha", "first");
        UiForm definition = new UiForm
        {
            Key = "profile",
            Action = "/profile",
            Method = "put",
            Fields = new UiField[] { zeta, alpha },
            SubmitCommandId = "save"
        };

        FormOperationResult registered = core.RegisterForm(definition);
        zeta.Value.StringValue = "mutated source";
        registered.Form!.Fields[0].Value.StringValue = "mutated result";
        FormOperationResult retained = core.ReadForm("profile");

        Assert.Multiple(() =>
        {
            Assert.That(registered.Success, Is.True);
            Assert.That(registered.Applied, Is.True);
            Assert.That(retained.Form!.Fields.Select(item => item.Key), Is.EqualTo(new string[] { "alpha", "zeta" }));
            Assert.That(retained.Form.Fields.Select(item => item.Value.StringValue), Is.EqualTo(new string[] { "first", "last" }));
            Assert.That(retained.Form.State.InitialValues.Select(item => item.Key), Is.EqualTo(new string[] { "alpha", "zeta" }));
            Assert.That(retained.Form.State.CurrentValues.Select(item => item.Value.StringValue), Is.EqualTo(new string[] { "first", "last" }));
            Assert.That(retained.Form.State.Version, Is.EqualTo(0));
            Assert.That(retained.Form.State.IsDirty, Is.False);
            Assert.That(retained.Form.State.CanSubmit, Is.True);
            Assert.That(retained.Form.Validation.IsValid, Is.True);
            Assert.That(retained.Form.Fields, Has.All.Matches<UiField>(item => item.IsValid));
            Assert.That(retained.Form.Action, Is.EqualTo("/profile"));
            Assert.That(retained.Form.SubmitCommandId, Is.EqualTo("save"));
            Assert.That(core.RegisterForm(definition).Code, Is.EqualTo("form.duplicate"));
        });
    }

    [Test]
    public void TracksEveryFieldAndAggregateStateTransition()
    {
        FormCore core = new FormCore();
        core.RegisterForm(new UiForm { Key = "profile", Fields = new UiField[] { Field("name", "Name", "Ada") } });

        FormOperationResult visited = core.VisitFormField("profile", "name");
        FormOperationResult touched = core.TouchFormField("profile", "name");
        FormOperationResult updated = core.UpdateFormFieldValue("profile", "name", Text("Lin"));
        FormOperationResult unchanged = core.UpdateFormFieldValue("profile", "name", Text("Lin"));
        FormOperationResult pending = core.SetFormFieldPending("profile", "name", true);
        FormOperationResult invalid = core.SetFormFieldValidity("profile", "name", false);
        FormOperationResult notPending = core.SetFormFieldPending("profile", "name", false);
        FormOperationResult valid = core.SetFormFieldValidity("profile", "name", true);
        UiForm retained = core.ReadForm("profile").Form!;

        Assert.Multiple(() =>
        {
            Assert.That(visited.Applied, Is.True);
            Assert.That(touched.Applied, Is.True);
            Assert.That(updated.Applied, Is.True);
            Assert.That(updated.Form!.State.Version, Is.EqualTo(1));
            Assert.That(unchanged.Applied, Is.False);
            Assert.That(unchanged.Form!.State.Version, Is.EqualTo(1));
            Assert.That(pending.Form!.State.IsPending, Is.True);
            Assert.That(pending.Form.State.CanSubmit, Is.False);
            Assert.That(invalid.Form!.Validation.IsValid, Is.False);
            Assert.That(invalid.Form.State.CanSubmit, Is.False);
            Assert.That(notPending.Form!.State.IsPending, Is.False);
            Assert.That(notPending.Form.State.CanSubmit, Is.False);
            Assert.That(valid.Form!.State.CanSubmit, Is.True);
            Assert.That(retained.Fields.Single().IsDirty, Is.True);
            Assert.That(retained.Fields.Single().IsTouched, Is.True);
            Assert.That(retained.Fields.Single().IsVisited, Is.True);
            Assert.That(retained.Fields.Single().IsPending, Is.False);
            Assert.That(retained.Fields.Single().IsValid, Is.True);
            Assert.That(retained.State.IsDirty, Is.True);
            Assert.That(retained.State.CurrentValues.Single().Value.StringValue, Is.EqualTo("Lin"));
            Assert.That(retained.Fields.Single().Input!.Value.StringValue, Is.EqualTo("Lin"));
        });
    }

    [Test]
    public void FieldTransitionsAreIdempotentAndSubmissionTracksAggregateEligibility()
    {
        FormCore core = new FormCore();
        core.RegisterForm(new UiForm
        {
            Key = "profile",
            Fields = new UiField[] { Field("alpha", "Alpha", "Ada"), Field("beta", "Beta", "Lovelace") }
        });

        FormOperationResult firstVisit = core.VisitFormField("profile", "alpha");
        FormOperationResult repeatedVisit = core.VisitFormField("profile", "alpha");
        FormOperationResult firstTouch = core.TouchFormField("profile", "alpha");
        FormOperationResult repeatedTouch = core.TouchFormField("profile", "alpha");
        FormOperationResult unchangedPending = core.SetFormFieldPending("profile", "alpha", false);
        FormOperationResult pending = core.SetFormFieldPending("profile", "alpha", true);
        FormOperationResult repeatedPending = core.SetFormFieldPending("profile", "alpha", true);
        FormOperationResult pendingSubmission = core.BeginFormSubmit("profile");
        core.SetFormFieldPending("profile", "alpha", false);
        FormOperationResult unchangedValidity = core.SetFormFieldValidity("profile", "alpha", true);
        FormOperationResult invalid = core.SetFormFieldValidity("profile", "alpha", false);
        FormOperationResult repeatedInvalid = core.SetFormFieldValidity("profile", "alpha", false);
        FormOperationResult invalidSubmission = core.BeginFormSubmit("profile");
        core.SetFormFieldValidity("profile", "alpha", true);
        FormOperationResult changed = core.UpdateFormFieldValue("profile", "alpha", Text("Grace"));
        FormOperationResult restored = core.UpdateFormFieldValue("profile", "alpha", Text("Ada"));
        UiForm retained = core.ReadForm("profile").Form!;

        Assert.Multiple(() =>
        {
            Assert.That(firstVisit.Applied, Is.True);
            Assert.That(repeatedVisit.Applied, Is.False);
            Assert.That(firstTouch.Applied, Is.True);
            Assert.That(repeatedTouch.Applied, Is.False);
            Assert.That(unchangedPending.Applied, Is.False);
            Assert.That(pending.Applied, Is.True);
            Assert.That(repeatedPending.Applied, Is.False);
            Assert.That(pendingSubmission.Code, Is.EqualTo("form.cannot-submit"));
            Assert.That(unchangedValidity.Applied, Is.False);
            Assert.That(invalid.Applied, Is.True);
            Assert.That(repeatedInvalid.Applied, Is.False);
            Assert.That(invalidSubmission.Code, Is.EqualTo("form.cannot-submit"));
            Assert.That(changed.Form!.State.IsDirty, Is.True);
            Assert.That(changed.Form.State.Version, Is.EqualTo(1));
            Assert.That(restored.Form!.State.IsDirty, Is.False);
            Assert.That(restored.Form.State.Version, Is.EqualTo(2));
            Assert.That(retained.State.IsPending, Is.False);
            Assert.That(retained.Validation.IsPending, Is.False);
            Assert.That(retained.Validation.IsValid, Is.True);
            Assert.That(retained.State.CanSubmit, Is.True);
            Assert.That(retained.Fields.Single(item => item.Key == "beta").IsValid, Is.True);
        });
    }

    [Test]
    public void ResetCancelAndSubmitMaintainTheAcceptedBaselineAndVersions()
    {
        FormCore core = new FormCore();
        core.RegisterForm(new UiForm { Key = "profile", Fields = new UiField[] { Field("name", "Name", "Ada") } });
        FormOperationResult pristineReset = core.ResetForm("profile");
        FormOperationResult pristineCancel = core.CancelForm("profile");
        core.UpdateFormFieldValue("profile", "name", Text("draft"));
        core.TouchFormField("profile", "name");
        core.VisitFormField("profile", "name");

        FormOperationResult begun = core.BeginFormSubmit("profile");
        FormOperationResult duplicateBegin = core.BeginFormSubmit("profile");
        FormOperationResult failed = core.CompleteFormSubmit("profile", false);
        core.BeginFormSubmit("profile");
        FormOperationResult accepted = core.CompleteFormSubmit("profile", true);
        core.UpdateFormFieldValue("profile", "name", Text("discarded"));
        FormOperationResult canceled = core.CancelForm("profile");
        core.UpdateFormFieldValue("profile", "name", Text("reset me"));
        core.SetFormFieldPending("profile", "name", true);
        core.SetFormFieldValidity("profile", "name", false);
        FormOperationResult reset = core.ResetForm("profile");

        Assert.Multiple(() =>
        {
            Assert.That(pristineReset.Applied, Is.False);
            Assert.That(pristineCancel.Applied, Is.True);
            Assert.That(pristineCancel.Form!.State.Version, Is.EqualTo(0));
            Assert.That(begun.Success, Is.True);
            Assert.That(begun.Form!.State.IsSubmitting, Is.True);
            Assert.That(begun.Form.State.CanSubmit, Is.False);
            Assert.That(duplicateBegin.Code, Is.EqualTo("form.submit-active"));
            Assert.That(failed.Success, Is.True);
            Assert.That(failed.Form!.State.IsDirty, Is.True);
            Assert.That(failed.Form.State.InitialValues.Single().Value.StringValue, Is.EqualTo("Ada"));
            Assert.That(accepted.Form!.State.IsDirty, Is.False);
            Assert.That(accepted.Form.State.InitialValues.Single().Value.StringValue, Is.EqualTo("draft"));
            Assert.That(canceled.Form!.Fields.Single().Value.StringValue, Is.EqualTo("draft"));
            Assert.That(canceled.Form.State.Version, Is.EqualTo(3));
            Assert.That(canceled.Form.Fields.Single().IsTouched, Is.False);
            Assert.That(canceled.Form.Fields.Single().IsVisited, Is.False);
            Assert.That(reset.Form!.Fields.Single().Value.StringValue, Is.EqualTo("draft"));
            Assert.That(reset.Form.State.Version, Is.EqualTo(5));
            Assert.That(reset.Form.State.IsPending, Is.False);
            Assert.That(reset.Form.Validation.IsValid, Is.True);
            Assert.That(reset.Form.State.CanSubmit, Is.True);
            Assert.That(core.CompleteFormSubmit("profile", true).Code, Is.EqualTo("form.no-submit"));
        });
    }

    [Test]
    public void UnmountRemovesStateAndRemountStartsFromANewBaseline()
    {
        FormCore core = new FormCore();
        core.RegisterForm(new UiForm { Key = "profile", Fields = new UiField[] { Field("name", "Name", "Ada") } });
        core.UpdateFormFieldValue("profile", "name", Text("edited"));
        core.TouchFormField("profile", "name");
        core.SetFormFieldPending("profile", "name", true);

        FormOperationResult unmounted = core.UnregisterForm("profile");
        FormOperationResult missing = core.ReadForm("profile");
        FormOperationResult remounted = core.RegisterForm(new UiForm { Key = "profile", Fields = new UiField[] { Field("name", "Name", "Grace") } });

        Assert.Multiple(() =>
        {
            Assert.That(unmounted.Success, Is.True);
            Assert.That(unmounted.Applied, Is.True);
            Assert.That(unmounted.Form!.Fields.Single().Value.StringValue, Is.EqualTo("edited"));
            Assert.That(missing.Code, Is.EqualTo("form.unknown"));
            Assert.That(remounted.Success, Is.True);
            Assert.That(remounted.Form!.Fields.Single().Value.StringValue, Is.EqualTo("Grace"));
            Assert.That(remounted.Form.State.InitialValues.Single().Value.StringValue, Is.EqualTo("Grace"));
            Assert.That(remounted.Form.State.CurrentValues.Single().Value.StringValue, Is.EqualTo("Grace"));
            Assert.That(remounted.Form.State.Version, Is.EqualTo(0));
            Assert.That(remounted.Form.State.IsDirty, Is.False);
            Assert.That(remounted.Form.State.IsPending, Is.False);
            Assert.That(remounted.Form.Fields.Single().IsTouched, Is.False);
            Assert.That(remounted.Form.State.CanSubmit, Is.True);
        });
    }

    [Test]
    public void DynamicallyRegistersAndUnregistersFieldsWithExceptionlessFailures()
    {
        FormCore core = new FormCore();
        core.RegisterForm(new UiForm { Key = "profile" });
        UiField source = Field("name", "Name", "Ada");

        FormOperationResult registered = core.RegisterFormField("profile", source);
        source.Value.StringValue = "mutated";
        FormOperationResult duplicate = core.RegisterFormField("profile", Field("name", "Other", "value"));
        UiField malformed = Field("bad", "Bad", "value");
        malformed.Value = new UiValue { Kind = UiValueKind.String };
        FormOperationResult rejected = core.RegisterFormField("profile", malformed);
        UiField nullIssue = Field("issue", "Issue", "value");
        nullIssue.Issues = new UiValidationIssue[] { null! };
        FormOperationResult rejectedIssue = core.RegisterFormField("profile", nullIssue);
        FormOperationResult removed = core.UnregisterFormField("profile", "name");
        FormOperationResult unknownField = core.UnregisterFormField("profile", "name");
        FormOperationResult unregistered = core.UnregisterForm("profile");

        Assert.Multiple(() =>
        {
            Assert.That(registered.Success, Is.True);
            Assert.That(registered.Form!.Fields.Single().Value.StringValue, Is.EqualTo("Ada"));
            Assert.That(registered.Form.State.Version, Is.EqualTo(1));
            Assert.That(duplicate.Code, Is.EqualTo("form.duplicate-field"));
            Assert.That(rejected.Code, Is.EqualTo("state.invalid-value"));
            Assert.That(rejectedIssue.Code, Is.EqualTo("form.null-issue"));
            Assert.That(removed.Success, Is.True);
            Assert.That(removed.Field!.Key, Is.EqualTo("name"));
            Assert.That(removed.Form!.Fields, Is.Empty);
            Assert.That(removed.Form.State.InitialValues, Is.Empty);
            Assert.That(removed.Form.State.CurrentValues, Is.Empty);
            Assert.That(removed.Form.State.Version, Is.EqualTo(2));
            Assert.That(unknownField.Code, Is.EqualTo("form.unknown-field"));
            Assert.That(unregistered.Success, Is.True);
            Assert.That(core.ReadForm("profile").Code, Is.EqualTo("form.unknown"));
            Assert.That(core.RegisterForm(null).Code, Is.EqualTo("form.null"));
            Assert.That(core.RegisterForm(new UiForm()).Code, Is.EqualTo("form.invalid-key"));
        });
    }

    private static UiField Field(string key, string label, string value)
    {
        return new UiField
        {
            Key = key,
            Name = key,
            Label = label,
            Value = Text(value),
            Input = new UiInput { Name = key, Value = Text(value) }
        };
    }

    private static UiValue Text(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }
}
