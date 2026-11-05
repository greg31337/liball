using System;
using System.Linq;
using System.Threading.Tasks;
using htmlgui;

namespace htmlgui.Tests;

public class UiValidationEngineTests
{
    [Test]
    public void RegistersReadsUpdatesAndUnregistersCopiedFormsExceptionlessly()
    {
        IUiValidationEngine engine = new UiValidationEngine();
        UiForm form = Form("profile", Field("name", "Ada"));

        UiValidationResult registered = engine.RegisterValidationForm(form);
        form.Fields[0].Value.StringValue = "mutated source";
        registered.Form!.Fields[0].Value.StringValue = "mutated result";
        UiValidationResult retained = engine.ReadValidationForm("profile");
        UiValidationResult updated = engine.UpdateValidationFieldValue("profile", "name", Text("Grace"));
        UiValidationResult eligibility = engine.QueryUiSubmitEligibility("profile");
        UiValidationResult removed = engine.UnregisterValidationForm("profile");

        Assert.Multiple(() =>
        {
            Assert.That(registered.Success, Is.True);
            Assert.That(retained.Form!.Fields.Single().Value.StringValue, Is.EqualTo("Ada"));
            Assert.That(updated.Success, Is.True);
            Assert.That(updated.Applied, Is.True);
            Assert.That(updated.Form!.Fields.Single().Value.StringValue, Is.EqualTo("Grace"));
            Assert.That(updated.Form.State.Version, Is.EqualTo(1));
            Assert.That(eligibility.CanSubmit, Is.True);
            Assert.That(eligibility.IsValid, Is.True);
            Assert.That(removed.Success, Is.True);
            Assert.That(engine.ReadValidationForm("profile").Diagnostics.Single().Code, Is.EqualTo("form.unknown"));
            Assert.That(engine.RegisterValidationForm(null).Diagnostics.Single().Code, Is.EqualTo("form.null"));
        });
    }

    [Test]
    public async Task ValidatesBuiltInRulesAndExposesExceptionlessIssueOperations()
    {
        IUiValidationEngine engine = new UiValidationEngine();
        UiField name = Field("name", string.Empty);
        name.Rules = new UiValidationRule[]
        {
            new UiValidationRule { Key = "required", Kind = "required", Trigger = UiValidationTrigger.Input, Message = "Name is required" }
        };
        engine.RegisterValidationForm(Form("profile", name));

        UiValidationResult invalid = await engine.ValidateUiField("profile", "name", UiValidationTrigger.Input);
        UiValidationResult merged = engine.MergeUiValidationIssues("profile", "name", new UiValidationIssue[]
        {
            Issue("warning", "name.warning", "Check this value", UiValidationSeverity.Warning, "host", null, 0)
        });
        UiValidationResult cleared = engine.ClearUiValidationIssues("profile", "name", "host");
        engine.UpdateValidationFieldValue("profile", "name", Text("Ada"));
        UiValidationResult valid = await engine.ValidateUiField("profile", "name", UiValidationTrigger.Input);

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Success, Is.True);
            Assert.That(invalid.IsValid, Is.False);
            Assert.That(invalid.CanSubmit, Is.False);
            Assert.That(invalid.Report!.FirstInvalidFieldKey, Is.EqualTo("name"));
            Assert.That(invalid.Report.Issues.Single().Message, Is.EqualTo("Name is required"));
            Assert.That(merged.Report!.Issues.Select(item => item.Severity), Is.EqualTo(new UiValidationSeverity[] { UiValidationSeverity.Error, UiValidationSeverity.Warning }));
            Assert.That(cleared.Report!.Issues.Single().Id, Is.EqualTo("required"));
            Assert.That(valid.IsValid, Is.True);
            Assert.That(valid.CanSubmit, Is.True);
            Assert.That(valid.Report!.Issues, Is.Empty);
            Assert.That(engine.MergeUiValidationIssues("profile", "name", null).Diagnostics.Single().Code, Is.EqualTo("validation.null-issues"));
        });
    }

    [Test]
    public async Task InvokesRegisteredHandlersWithCopiedContextAndAppliesTheirIssues()
    {
        IUiValidationEngine engine = new UiValidationEngine();
        RecordingValidationHandler handler = new RecordingValidationHandler(new UiValidationHandlerResult
        {
            Issues = new UiValidationIssue[]
            {
                Issue("taken", "email.taken", "Address is taken", UiValidationSeverity.Error, string.Empty, null, 999)
            }
        });
        UiField email = Field("email", "ada@example.test");
        email.Rules = new UiValidationRule[]
        {
            CustomRule("available", "availability", UiValidationTrigger.Change, true)
        };
        engine.RegisterValidationForm(Form("account", email));
        UiResult registeredHandler = engine.RegisterUiValidationHandler("availability", handler);

        UiValidationResult result = await engine.ValidateUiField("account", "email", UiValidationTrigger.Change);
        handler.Context!.Value!.StringValue = "mutated callback copy";
        UiValidationResult retained = engine.ReadValidationForm("account");

        Assert.Multiple(() =>
        {
            Assert.That(registeredHandler.Success, Is.True);
            Assert.That(result.Success, Is.True);
            Assert.That(result.IsPending, Is.False);
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Report!.Issues.Single().Id, Is.EqualTo("taken"));
            Assert.That(result.Report.Issues.Single().Source, Is.EqualTo("custom"));
            Assert.That(result.Report.Issues.Single().ValueVersion, Is.EqualTo(0));
            Assert.That(handler.Context!.FormKey, Is.EqualTo("account"));
            Assert.That(handler.Context.FieldKey, Is.EqualTo("email"));
            Assert.That(handler.Context.RuleKey, Is.EqualTo("available"));
            Assert.That(handler.Context.ReturnsTask, Is.True);
            Assert.That(retained.Form!.Fields.Single().Value.StringValue, Is.EqualTo("ada@example.test"));
            Assert.That(engine.RegisterUiValidationHandler("availability", handler).Diagnostics.Single().Code, Is.EqualTo("validation.duplicate-handler"));
            Assert.That(engine.UnregisterUiValidationHandler("availability").Success, Is.True);
            Assert.That(engine.UnregisterUiValidationHandler("availability").Diagnostics.Single().Code, Is.EqualTo("validation.unknown-handler"));
        });
    }

    [Test]
    public async Task AwaitsDeferredHandlersAndSuppressesTheirResultAfterAnEdit()
    {
        IUiValidationEngine engine = new UiValidationEngine();
        DeferredValidationHandler handler = new DeferredValidationHandler();
        UiField email = Field("email", "old@example.test");
        email.Rules = new UiValidationRule[] { CustomRule("remote", "remote-handler", UiValidationTrigger.Input, true) };
        engine.RegisterValidationForm(Form("account", email));
        engine.RegisterUiValidationHandler("remote-handler", handler);

        Task<UiValidationResult> validation = engine.ValidateUiField("account", "email", UiValidationTrigger.Input);
        await handler.Invoked.Task;
        UiValidationResult pending = engine.ReadValidationForm("account");
        UiValidationResult edited = engine.UpdateValidationFieldValue("account", "email", Text("new@example.test"));
        handler.Completion.SetResult(new UiValidationHandlerResult
        {
            Issues = new UiValidationIssue[]
            {
                Issue("old-error", "remote.old", "Old error", UiValidationSeverity.Error, string.Empty, null, 0)
            }
        });
        UiValidationResult completed = await validation;

        Assert.Multiple(() =>
        {
            Assert.That(pending.IsPending, Is.True);
            Assert.That(pending.CanSubmit, Is.False);
            Assert.That(edited.IsPending, Is.False);
            Assert.That(edited.Form!.Fields.Single().Value.StringValue, Is.EqualTo("new@example.test"));
            Assert.That(completed.Success, Is.True);
            Assert.That(completed.IsPending, Is.False);
            Assert.That(completed.Report!.Issues, Is.Empty);
            Assert.That(completed.Form!.Fields.Single().Value.StringValue, Is.EqualTo("new@example.test"));
        });
    }

    [Test]
    public async Task RoutesMissingThrowingFaultedNullAndReportedHandlerFailures()
    {
        IUiValidationEngine engine = new UiValidationEngine();
        UiField field = Field("value", "data");
        field.Rules = new UiValidationRule[]
        {
            CustomRule("missing", "missing-handler", UiValidationTrigger.Manual, false),
            CustomRule("throwing", "throw-handler", UiValidationTrigger.Manual, false),
            CustomRule("faulted", "fault-handler", UiValidationTrigger.Manual, true),
            CustomRule("null", "null-handler", UiValidationTrigger.Manual, false),
            CustomRule("failed", "failed-handler", UiValidationTrigger.Manual, false)
        };
        engine.RegisterValidationForm(Form("failures", field));
        engine.RegisterUiValidationHandler("throw-handler", new ThrowingValidationHandler());
        engine.RegisterUiValidationHandler("fault-handler", new FaultedValidationHandler());
        engine.RegisterUiValidationHandler("null-handler", new NullValidationHandler());
        engine.RegisterUiValidationHandler("failed-handler", new RecordingValidationHandler(new UiValidationHandlerResult
        {
            Success = false,
            Diagnostics = new UiDiagnostic[] { new UiDiagnostic { Code = "handler.rejected", Message = "Rejected" } }
        }));

        UiValidationResult result = await engine.ValidateUiForm("failures", UiValidationTrigger.Manual);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.IsPending, Is.False);
            Assert.That(result.Diagnostics.Select(item => item.Code), Does.Contain("validation.missing-handler"));
            Assert.That(result.Diagnostics.Select(item => item.Code), Does.Contain("validation.handler-exception"));
            Assert.That(result.Diagnostics.Select(item => item.Code), Does.Contain("validation.null-handler-result"));
            Assert.That(result.Diagnostics.Select(item => item.Code), Does.Contain("handler.rejected"));
            Assert.That(result.Report!.Issues, Is.Empty);
        });
    }

    [Test]
    public async Task RejectsMalformedSuccessfulHandlerOutcomes()
    {
        IUiValidationEngine engine = new UiValidationEngine();
        UiField field = Field("value", "data");
        field.Rules = new UiValidationRule[] { CustomRule("invalid", "invalid-handler", UiValidationTrigger.Manual, false) };
        engine.RegisterValidationForm(Form("invalid-result", field));
        UiValidationHandlerResult malformed = new UiValidationHandlerResult();
        malformed.Issues = null!;
        engine.RegisterUiValidationHandler("invalid-handler", new RecordingValidationHandler(malformed));

        UiValidationResult result = await engine.ValidateUiField("invalid-result", "value", UiValidationTrigger.Manual);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Diagnostics.Select(item => item.Code), Does.Contain("validation.null-handler-result"));
            Assert.That(result.IsPending, Is.False);
            Assert.That(result.Report!.Issues, Is.Empty);
        });
    }

    [Test]
    public void ProvidesVersionedSubmissionAndExternalIssueApis()
    {
        IUiValidationEngine engine = new UiValidationEngine();
        engine.RegisterValidationForm(Form("profile", Field("name", "Ada"), Field("city", "London")));

        UiValidationResult begun = engine.BeginUiFormSubmission("profile");
        UiValidationResult server = engine.IngestUiValidationIssues("profile", "name", begun.SubmittedVersion, "server", new UiValidationIssue[]
        {
            Issue("server-name", "server.name", "Name was rejected", UiValidationSeverity.Error, "server", null, begun.SubmittedVersion)
        });
        UiValidationResult completed = engine.CompleteUiFormSubmission("profile", begun.SubmittedVersion, false);
        UiValidationResult eligibility = engine.QueryUiSubmitEligibility("profile");
        UiValidationResult unknownCompletion = engine.CompleteUiFormSubmission("profile", begun.SubmittedVersion, false);

        Assert.Multiple(() =>
        {
            Assert.That(begun.Success, Is.True);
            Assert.That(begun.SubmittedVersion, Is.EqualTo(0));
            Assert.That(begun.Form!.State.IsSubmitting, Is.True);
            Assert.That(server.Success, Is.True);
            Assert.That(server.Report!.Issues.Single().Id, Is.EqualTo("server-name"));
            Assert.That(completed.Success, Is.True);
            Assert.That(completed.Form!.State.IsSubmitting, Is.False);
            Assert.That(eligibility.IsValid, Is.False);
            Assert.That(eligibility.CanSubmit, Is.False);
            Assert.That(unknownCompletion.Diagnostics.Single().Code, Is.EqualTo("validation.unknown-submission"));
            Assert.That(engine.IngestUiValidationIssues("profile", "name", 99, "server", System.Array.Empty<UiValidationIssue>()).Diagnostics.Single().Code, Is.EqualTo("validation.unknown-submission"));
        });
    }

    [Test]
    public async Task CancelsExplicitCallbacksAndCleansRuntimeStateWhenAFormIsRemoved()
    {
        UiValidationEngine engine = new UiValidationEngine();
        DeferredValidationHandler handler = new DeferredValidationHandler();
        UiField field = Field("value", "data");
        field.Rules = new UiValidationRule[] { CustomRule("remote", "remote", UiValidationTrigger.Blur, true) };
        engine.RegisterValidationForm(Form("sample", field));
        engine.RegisterUiValidationHandler("remote", handler);
        Task<UiValidationResult> task = engine.ValidateUiField("sample", "value", UiValidationTrigger.Blur);
        await handler.Invoked.Task;

        UiValidationResult canceled = engine.CancelUiValidationCallback("sample", "value", "remote");
        handler.Completion.SetResult(new UiValidationHandlerResult());
        UiValidationResult completed = await task;
        engine.BeginUiFormSubmission("sample");
        UiValidationResult removed = engine.UnregisterValidationForm("sample");

        Assert.Multiple(() =>
        {
            Assert.That(canceled.Success, Is.True);
            Assert.That(canceled.Applied, Is.True);
            Assert.That(canceled.IsPending, Is.False);
            Assert.That(completed.IsPending, Is.False);
            Assert.That(removed.Success, Is.True);
            Assert.That(engine.External.Submissions, Is.Empty);
            Assert.That(engine.Flow.Custom.Requests, Is.Empty);
            Assert.That(engine.Flow.Custom.PendingInvocations, Is.Empty);
            Assert.That(engine.Flow.Custom.PendingStates, Is.Empty);
            Assert.That(engine.CancelUiValidationCallback("sample", "value", "remote").Diagnostics.Single().Code, Is.EqualTo("form.unknown"));
        });
    }

    private static UiForm Form(string key, params UiField[] fields)
    {
        return new UiForm { Key = key, Fields = fields };
    }

    private static UiField Field(string key, string value)
    {
        return new UiField { Key = key, Name = key, Value = Text(value) };
    }

    private static UiValidationRule CustomRule(string key, string handlerId, UiValidationTrigger trigger, bool returnsTask)
    {
        return new UiValidationRule
        {
            Key = key,
            Kind = "custom-field",
            HandlerId = handlerId,
            Trigger = trigger,
            Parameters = new UiValueEntry[]
            {
                new UiValueEntry { Key = "returns-task", Value = new UiValue { Kind = UiValueKind.Boolean, BooleanValue = returnsTask } }
            }
        };
    }

    private static UiValidationIssue Issue(string id, string code, string message, UiValidationSeverity severity, string source, string? fieldKey, int valueVersion)
    {
        return new UiValidationIssue
        {
            Id = id,
            Code = code,
            Message = message,
            Severity = severity,
            Source = source,
            FieldKey = fieldKey,
            ValueVersion = valueVersion
        };
    }

    private static UiValue Text(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }

    private class RecordingValidationHandler : IUiValidationHandler
    {
        public UiValidationContext? Context;
        public UiValidationHandlerResult Result;

        public RecordingValidationHandler(UiValidationHandlerResult result)
        {
            Result = result;
        }

        public Task<UiValidationHandlerResult> HandleUiValidation(UiValidationContext context)
        {
            Context = context;
            return Task.FromResult(Result);
        }
    }

    private class DeferredValidationHandler : IUiValidationHandler
    {
        public TaskCompletionSource<bool> Invoked = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<UiValidationHandlerResult> Completion = new TaskCompletionSource<UiValidationHandlerResult>(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<UiValidationHandlerResult> HandleUiValidation(UiValidationContext context)
        {
            Invoked.SetResult(true);
            return Completion.Task;
        }
    }

    private class ThrowingValidationHandler : IUiValidationHandler
    {
        public Task<UiValidationHandlerResult> HandleUiValidation(UiValidationContext context)
        {
            throw new InvalidOperationException("boom");
        }
    }

    private class FaultedValidationHandler : IUiValidationHandler
    {
        public Task<UiValidationHandlerResult> HandleUiValidation(UiValidationContext context)
        {
            return Task.FromException<UiValidationHandlerResult>(new InvalidOperationException("fault"));
        }
    }

    private class NullValidationHandler : IUiValidationHandler
    {
        public Task<UiValidationHandlerResult> HandleUiValidation(UiValidationContext context)
        {
            return Task.FromResult<UiValidationHandlerResult>(null!);
        }
    }
}
