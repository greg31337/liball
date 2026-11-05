using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace htmlgui;

/// <summary>Coordinates public form validation, callbacks, issues, and submission lifecycle.</summary>
public class UiValidationEngine : IUiValidationEngine
{
    internal FormCore Forms;
    internal FormValidationFlowCore Flow;
    internal FormExternalValidationCore External;
    internal List<string> HandlerIds;
    internal List<IUiValidationHandler> Handlers;

    /// <summary>Initializes an empty validation engine.</summary>
    public UiValidationEngine()
    {
        Forms = new FormCore();
        Flow = new FormValidationFlowCore(Forms);
        External = new FormExternalValidationCore(Flow);
        HandlerIds = new List<string>();
        Handlers = new List<IUiValidationHandler>();
    }

    /// <summary>Registers one copied form definition.</summary>
    public UiValidationResult RegisterValidationForm(UiForm? form)
    {
        return CreatePublicFormOperationResult(Forms.RegisterForm(form));
    }

    /// <summary>Unregisters one form and its validation state.</summary>
    public UiValidationResult UnregisterValidationForm(string? formKey)
    {
        FormOperationResult removed = Forms.UnregisterForm(formKey);
        if (removed.Success && formKey != null)
        {
            RemovePublicValidationFormState(formKey);
        }

        return CreatePublicFormOperationResult(removed);
    }

    /// <summary>Reads one copied form and its current validation state.</summary>
    public UiValidationResult ReadValidationForm(string? formKey)
    {
        return CreatePublicFormOperationResult(Forms.ReadForm(formKey));
    }

    /// <summary>Updates one copied field value and cancels callbacks made stale by the edit.</summary>
    public UiValidationResult UpdateValidationFieldValue(string? formKey, string? fieldKey, UiValue? value)
    {
        FormOperationResult updated = Forms.UpdateFormFieldValue(formKey, fieldKey, value);
        if (!updated.Success || !updated.Applied || formKey == null)
        {
            return CreatePublicFormOperationResult(updated);
        }

        CancelPublicStaleValidationCallbacks(formKey);
        UiValidationResult result = CreatePublicFormOperationResult(Forms.ReadForm(formKey));
        result.Applied = updated.Applied;
        return result;
    }

    /// <summary>Runs matching field validation rules and their registered callbacks.</summary>
    public Task<UiValidationResult> ValidateUiField(string? formKey, string? fieldKey, UiValidationTrigger trigger)
    {
        return BeginPublicValidation(formKey, fieldKey, trigger);
    }

    /// <summary>Runs matching rules for every field and the form.</summary>
    public Task<UiValidationResult> ValidateUiForm(string? formKey, UiValidationTrigger trigger)
    {
        return BeginPublicValidation(formKey, null, trigger);
    }

    /// <summary>Registers one stable custom-validation callback handler.</summary>
    public UiResult RegisterUiValidationHandler(string? handlerId, IUiValidationHandler? handler)
    {
        if (string.IsNullOrWhiteSpace(handlerId))
        {
            return CreatePublicValidationOperationFailure("validation.invalid-handler-id");
        }

        if (handler == null)
        {
            return CreatePublicValidationOperationFailure("validation.null-handler");
        }

        if (FindPublicValidationHandlerIndex(handlerId) >= 0)
        {
            return CreatePublicValidationOperationFailure("validation.duplicate-handler");
        }

        HandlerIds.Add(handlerId);
        Handlers.Add(handler);
        return new UiResult { Success = true };
    }

    /// <summary>Unregisters one custom-validation callback handler.</summary>
    public UiResult UnregisterUiValidationHandler(string? handlerId)
    {
        if (string.IsNullOrWhiteSpace(handlerId))
        {
            return CreatePublicValidationOperationFailure("validation.invalid-handler-id");
        }

        int index = FindPublicValidationHandlerIndex(handlerId);
        if (index < 0)
        {
            return CreatePublicValidationOperationFailure("validation.unknown-handler");
        }

        HandlerIds.RemoveAt(index);
        Handlers.RemoveAt(index);
        return new UiResult { Success = true };
    }

    /// <summary>Cancels one pending custom-validation callback.</summary>
    public UiValidationResult CancelUiValidationCallback(string? formKey, string? fieldKey, string? ruleKey)
    {
        FormCustomValidationResult canceled = Flow.Custom.CancelCustomValidation(formKey, fieldKey, ruleKey);
        if (!canceled.Success)
        {
            return CreatePublicValidationFailure(canceled.Code);
        }

        return CreatePublicValidationResult(canceled.Form, canceled.Applied);
    }

    /// <summary>Merges copied issues into current form validation state.</summary>
    public UiValidationResult MergeUiValidationIssues(string? formKey, string? fieldKey, UiValidationIssue[]? issues)
    {
        return CreatePublicValidationFlowResult(Flow.MergeFormValidationIssues(formKey, fieldKey, issues));
    }

    /// <summary>Clears issues from one source in a field or entire form scope.</summary>
    public UiValidationResult ClearUiValidationIssues(string? formKey, string? fieldKey, string? source)
    {
        return CreatePublicValidationFlowResult(Flow.ClearFormValidationIssues(formKey, fieldKey, source));
    }

    /// <summary>Returns current form validity and submission eligibility.</summary>
    public UiValidationResult QueryUiSubmitEligibility(string? formKey)
    {
        return CreatePublicFormOperationResult(Forms.ReadForm(formKey));
    }

    /// <summary>Begins submission and captures the submitted value version.</summary>
    public UiValidationResult BeginUiFormSubmission(string? formKey)
    {
        FormOperationResult begun = Forms.BeginFormSubmit(formKey);
        if (!begun.Success)
        {
            return CreatePublicFormOperationResult(begun);
        }

        FormExternalValidationResult captured = External.CaptureFormSubmission(formKey);
        if (!captured.Success)
        {
            Forms.CompleteFormSubmit(formKey, false);
            return CreatePublicValidationFailure(captured.Code);
        }

        UiValidationResult result = CreatePublicValidationResult(captured.Form, true);
        result.SubmittedVersion = captured.SubmittedVersion;
        return result;
    }

    /// <summary>Completes submission and discards its captured value snapshot.</summary>
    public UiValidationResult CompleteUiFormSubmission(string? formKey, int submittedVersion, bool success)
    {
        if (submittedVersion < 0)
        {
            return CreatePublicValidationFailure("validation.invalid-submitted-version");
        }

        if (External.FindFormSubmissionIndex(formKey, submittedVersion) < 0)
        {
            return CreatePublicValidationFailure("validation.unknown-submission");
        }

        FormOperationResult completed = Forms.CompleteFormSubmit(formKey, success);
        if (!completed.Success)
        {
            return CreatePublicFormOperationResult(completed);
        }

        FormExternalValidationResult discarded = External.DiscardFormSubmission(formKey, submittedVersion);
        UiValidationResult result = CreatePublicFormOperationResult(completed);
        result.SubmittedVersion = submittedVersion;
        if (!discarded.Success)
        {
            result.Success = false;
            AddPublicValidationDiagnostic(result, discarded.Code, "/validation/submission");
        }

        return result;
    }

    /// <summary>Ingests host or server issues for one submitted value version.</summary>
    public UiValidationResult IngestUiValidationIssues(string? formKey, string? fieldKey, int submittedVersion, string? source, UiValidationIssue[]? issues)
    {
        FormExternalValidationResult ingested = External.IngestFormValidationIssues(formKey, fieldKey, submittedVersion, source, issues);
        if (!ingested.Success)
        {
            return CreatePublicValidationFailure(ingested.Code);
        }

        UiValidationResult result = CreatePublicValidationResult(ingested.Form, ingested.Applied);
        result.SubmittedVersion = ingested.SubmittedVersion;
        if (ingested.Code.Length > 0)
        {
            AddPublicValidationDiagnostic(result, ingested.Code, "/validation/external");
        }

        return result;
    }

    internal Task<UiValidationResult> BeginPublicValidation(string? formKey, string? fieldKey, UiValidationTrigger trigger)
    {
        FormValidationFlowResult begun = Flow.ValidateFormTrigger(formKey, fieldKey, trigger);
        UiValidationResult result = CreatePublicValidationFlowResult(begun);
        if (!begun.Success)
        {
            return Task.FromResult(result);
        }

        FormValidationInvocation[] invocations = Flow.Custom.DrainCustomValidationInvocations();
        return DispatchPublicValidationCallbacks(invocations, 0, result);
    }

    internal Task<UiValidationResult> DispatchPublicValidationCallbacks(FormValidationInvocation[] invocations, int index, UiValidationResult result)
    {
        if (index >= invocations.Length)
        {
            RefreshPublicValidationResult(result);
            AddRoutedPublicValidationErrors(result);
            return Task.FromResult(result);
        }

        FormValidationInvocation invocation = invocations[index];
        int handlerIndex = FindPublicValidationHandlerIndex(invocation.HandlerId);
        if (handlerIndex < 0)
        {
            Flow.CompleteFormCustomValidation(invocation.FormKey, invocation.FieldKey, invocation.RuleKey, invocation.Generation, invocation.ValueVersion, false, null, "validation.missing-handler");
            result.Success = false;
            AddPublicValidationDiagnostic(result, "validation.missing-handler", "/validation/handler/" + invocation.HandlerId);
            return DispatchPublicValidationCallbacks(invocations, index + 1, result);
        }

        UiValidationContext context = CreatePublicValidationContext(invocation);
        IUiValidationHandler handler = Handlers[handlerIndex];
        Task<UiValidationHandlerResult> callback = Task.Run(() => handler.HandleUiValidation(context));
        return callback.ContinueWith(task => CompletePublicValidationCallback(invocations, index, result, task)).Unwrap();
    }

    internal Task<UiValidationResult> CompletePublicValidationCallback(FormValidationInvocation[] invocations, int index, UiValidationResult result, Task<UiValidationHandlerResult> callback)
    {
        FormValidationInvocation invocation = invocations[index];
        if (callback.IsCanceled)
        {
            Flow.CompleteFormCustomValidation(invocation.FormKey, invocation.FieldKey, invocation.RuleKey, invocation.Generation, invocation.ValueVersion, false, null, "validation.handler-canceled");
            result.Success = false;
            AddPublicValidationDiagnostic(result, "validation.handler-canceled", "/validation/handler/" + invocation.HandlerId);
        }
        else if (callback.IsFaulted)
        {
            Flow.CompleteFormCustomValidation(invocation.FormKey, invocation.FieldKey, invocation.RuleKey, invocation.Generation, invocation.ValueVersion, false, null, "validation.handler-exception");
            result.Success = false;
            AddPublicValidationDiagnostic(result, "validation.handler-exception", "/validation/handler/" + invocation.HandlerId);
        }
        else
        {
            UiValidationHandlerResult? handlerResult = callback.Result;
            if (handlerResult == null)
            {
                Flow.CompleteFormCustomValidation(invocation.FormKey, invocation.FieldKey, invocation.RuleKey, invocation.Generation, invocation.ValueVersion, false, null, "validation.null-handler-result");
                result.Success = false;
                AddPublicValidationDiagnostic(result, "validation.null-handler-result", "/validation/handler/" + invocation.HandlerId);
            }
            else
            {
                string failureCode = GetPublicValidationHandlerFailureCode(handlerResult);
                FormValidationFlowResult completed = Flow.CompleteFormCustomValidation(invocation.FormKey, invocation.FieldKey, invocation.RuleKey, invocation.Generation, invocation.ValueVersion, handlerResult.Success, handlerResult.Issues, failureCode);
                AddPublicValidationDiagnostics(result, handlerResult.Diagnostics);
                if (!handlerResult.Success || !completed.Success)
                {
                    result.Success = false;
                }
            }
        }

        AddRoutedPublicValidationErrors(result);
        return DispatchPublicValidationCallbacks(invocations, index + 1, result);
    }

    internal UiValidationContext CreatePublicValidationContext(FormValidationInvocation invocation)
    {
        UiValidationContext result = new UiValidationContext();
        result.FormKey = invocation.FormKey;
        result.FieldKey = invocation.FieldKey;
        result.RuleKey = invocation.RuleKey;
        result.Kind = invocation.Kind;
        result.Generation = invocation.Generation;
        result.ValueVersion = invocation.ValueVersion;
        result.ReturnsTask = invocation.ReturnsTask;
        if (invocation.Value != null)
        {
            result.Value = Forms.ValueStore.CopyStateValue(invocation.Value);
        }

        result.Values = Forms.CopyValueEntries(invocation.Values);
        result.Parameters = Forms.CopyValueEntries(invocation.Parameters);
        return result;
    }

    internal string GetPublicValidationHandlerFailureCode(UiValidationHandlerResult result)
    {
        if (result.Success)
        {
            return string.Empty;
        }

        UiDiagnostic[] diagnostics = result.Diagnostics ?? Array.Empty<UiDiagnostic>();
        int index = 0;
        while (index < diagnostics.Length)
        {
            UiDiagnostic? diagnostic = diagnostics[index];
            if (diagnostic != null && !string.IsNullOrWhiteSpace(diagnostic.Code))
            {
                return diagnostic.Code;
            }

            index++;
        }

        return "validation.handler-reported-failure";
    }

    internal int FindPublicValidationHandlerIndex(string? handlerId)
    {
        int index = 0;
        while (index < HandlerIds.Count)
        {
            if (ValueComparer.TextEquals(HandlerIds[index], handlerId))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal void CancelPublicStaleValidationCallbacks(string formKey)
    {
        int index = 0;
        while (index < Flow.Custom.Requests.Count)
        {
            FormValidationRequest request = Flow.Custom.Requests[index];
            if (request.Active && ValueComparer.TextEquals(request.FormKey, formKey))
            {
                Flow.Custom.CancelCustomValidation(request.FormKey, request.FieldKey, request.RuleKey);
            }

            index++;
        }
    }

    internal void RemovePublicValidationFormState(string formKey)
    {
        int index = External.Submissions.Count - 1;
        while (index >= 0)
        {
            if (ValueComparer.TextEquals(External.Submissions[index].FormKey, formKey))
            {
                External.Submissions.RemoveAt(index);
            }

            index--;
        }

        index = Flow.Custom.Requests.Count - 1;
        while (index >= 0)
        {
            if (ValueComparer.TextEquals(Flow.Custom.Requests[index].FormKey, formKey))
            {
                Flow.Custom.Requests.RemoveAt(index);
            }

            index--;
        }

        index = Flow.Custom.PendingInvocations.Count - 1;
        while (index >= 0)
        {
            if (ValueComparer.TextEquals(Flow.Custom.PendingInvocations[index].FormKey, formKey))
            {
                Flow.Custom.PendingInvocations.RemoveAt(index);
            }

            index--;
        }

        index = Flow.Custom.Errors.Count - 1;
        while (index >= 0)
        {
            if (ValueComparer.TextEquals(Flow.Custom.Errors[index].FormKey, formKey))
            {
                Flow.Custom.Errors.RemoveAt(index);
            }

            index--;
        }

        index = Flow.Custom.PendingStates.Count - 1;
        while (index >= 0)
        {
            if (ValueComparer.TextEquals(Flow.Custom.PendingStates[index].FormKey, formKey))
            {
                Flow.Custom.PendingStates.RemoveAt(index);
            }

            index--;
        }

        index = Flow.IssueOwners.Count - 1;
        while (index >= 0)
        {
            if (ValueComparer.TextEquals(Flow.IssueOwners[index].FormKey, formKey))
            {
                Flow.IssueOwners.RemoveAt(index);
            }

            index--;
        }
    }

    internal UiValidationResult CreatePublicFormOperationResult(FormOperationResult operation)
    {
        if (!operation.Success)
        {
            return CreatePublicValidationFailure(operation.Code);
        }

        return CreatePublicValidationResult(operation.Form, operation.Applied);
    }

    internal UiValidationResult CreatePublicValidationFlowResult(FormValidationFlowResult operation)
    {
        if (!operation.Success)
        {
            return CreatePublicValidationFailure(operation.Code);
        }

        UiValidationResult result = CreatePublicValidationResult(operation.Form, operation.Applied);
        if (operation.Code.Length > 0)
        {
            AddPublicValidationDiagnostic(result, operation.Code, "/validation");
        }

        return result;
    }

    internal UiValidationResult CreatePublicValidationResult(UiForm? form, bool applied)
    {
        UiValidationResult result = new UiValidationResult();
        result.Success = true;
        result.Applied = applied;
        if (form != null)
        {
            result.Form = Forms.CopyForm(form);
            result.Report = Forms.CopyValidationReport(form.Validation);
            result.IsValid = form.Validation.IsValid;
            result.IsPending = form.Validation.IsPending;
            result.CanSubmit = form.State.CanSubmit;
        }

        return result;
    }

    internal UiValidationResult CreatePublicValidationFailure(string code)
    {
        UiValidationResult result = new UiValidationResult();
        result.Diagnostics = new UiDiagnostic[] { CreatePublicValidationDiagnostic(code, "/validation") };
        return result;
    }

    internal UiResult CreatePublicValidationOperationFailure(string code)
    {
        return new UiResult
        {
            Diagnostics = new UiDiagnostic[] { CreatePublicValidationDiagnostic(code, "/validation/handler") }
        };
    }

    internal void RefreshPublicValidationResult(UiValidationResult result)
    {
        if (result.Form == null)
        {
            return;
        }

        FormOperationResult read = Forms.ReadForm(result.Form.Key);
        if (!read.Success || read.Form == null)
        {
            result.Success = false;
            AddPublicValidationDiagnostic(result, read.Code, "/validation/form");
            return;
        }

        result.Form = read.Form;
        result.Report = Forms.CopyValidationReport(read.Form.Validation);
        result.IsValid = read.Form.Validation.IsValid;
        result.IsPending = read.Form.Validation.IsPending;
        result.CanSubmit = read.Form.State.CanSubmit;
    }

    internal void AddRoutedPublicValidationErrors(UiValidationResult result)
    {
        FormValidationError[] errors = Flow.Custom.DrainCustomValidationErrors();
        if (errors.Length > 0)
        {
            result.Success = false;
        }

        int index = 0;
        while (index < errors.Length)
        {
            AddPublicValidationDiagnostic(result, errors[index].Code, "/validation/handler/" + errors[index].HandlerId);
            index++;
        }
    }

    internal void AddPublicValidationDiagnostics(UiValidationResult result, UiDiagnostic[]? diagnostics)
    {
        UiDiagnostic[] values = diagnostics ?? Array.Empty<UiDiagnostic>();
        int index = 0;
        while (index < values.Length)
        {
            UiDiagnostic? diagnostic = values[index];
            if (diagnostic == null)
            {
                AddPublicValidationDiagnostic(result, "validation.null-handler-diagnostic", "/validation/handler/diagnostics/" + InvariantFormatter.FormatInt32(index));
            }
            else
            {
                AddPublicValidationDiagnosticCopy(result, diagnostic);
            }

            index++;
        }
    }

    internal void AddPublicValidationDiagnostic(UiValidationResult result, string code, string location)
    {
        AddPublicValidationDiagnosticCopy(result, CreatePublicValidationDiagnostic(code, location));
    }

    internal void AddPublicValidationDiagnosticCopy(UiValidationResult result, UiDiagnostic diagnostic)
    {
        List<UiDiagnostic> diagnostics = new List<UiDiagnostic>();
        int index = 0;
        while (index < result.Diagnostics.Length)
        {
            UiDiagnostic source = result.Diagnostics[index];
            diagnostics.Add(new UiDiagnostic { Severity = source.Severity, Code = source.Code, Message = source.Message, Location = source.Location });
            index++;
        }

        diagnostics.Add(new UiDiagnostic { Severity = diagnostic.Severity, Code = diagnostic.Code, Message = diagnostic.Message, Location = diagnostic.Location });
        result.Diagnostics = diagnostics.ToArray();
    }

    internal UiDiagnostic CreatePublicValidationDiagnostic(string code, string location)
    {
        return new UiDiagnostic
        {
            Severity = UiDiagnosticSeverity.Error,
            Code = code,
            Message = GetPublicValidationDiagnosticMessage(code),
            Location = location
        };
    }

    internal string GetPublicValidationDiagnosticMessage(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return "Validation failed.";
        }

        return "Validation operation reported " + code + ".";
    }
}
