using System;
using System.Collections.Generic;

namespace htmlgui;

internal class FormValidationRequest
{
    internal string FormKey = string.Empty;
    internal string? FieldKey;
    internal string RuleKey = string.Empty;
    internal string HandlerId = string.Empty;
    internal string Kind = string.Empty;
    internal int Generation;
    internal int ValueVersion;
    internal bool ReturnsTask;
    internal bool Active;
    internal bool IsCancelled;
}

internal class FormValidationInvocation
{
    internal string FormKey = string.Empty;
    internal string? FieldKey;
    internal string RuleKey = string.Empty;
    internal string HandlerId = string.Empty;
    internal string Kind = string.Empty;
    internal int Generation;
    internal int ValueVersion;
    internal bool ReturnsTask;
    internal UiValue? Value;
    internal UiValueEntry[] Values = Array.Empty<UiValueEntry>();
    internal UiValueEntry[] Parameters = Array.Empty<UiValueEntry>();
}

internal class FormValidationError
{
    internal string FormKey = string.Empty;
    internal string? FieldKey;
    internal string RuleKey = string.Empty;
    internal string HandlerId = string.Empty;
    internal int Generation;
    internal int ValueVersion;
    internal string Code = string.Empty;
}

internal class FormValidationPendingState
{
    internal string FormKey = string.Empty;
    internal string FieldKey = string.Empty;
    internal bool WasPending;
}

internal class FormCustomValidationResult
{
    internal bool Success;
    internal bool Applied;
    internal bool IsPending;
    internal bool HasOutcome;
    internal string Code = string.Empty;
    internal int Generation;
    internal int ValueVersion;
    internal UiForm? Form;
    internal UiField? Field;
    internal UiValidationIssue[] Issues = Array.Empty<UiValidationIssue>();
}

internal class FormCustomValidationCore
{
    internal const int MaximumValidationIssues = 256;

    internal FormCore Forms;
    internal List<FormValidationRequest> Requests = new List<FormValidationRequest>();
    internal List<FormValidationInvocation> PendingInvocations = new List<FormValidationInvocation>();
    internal List<FormValidationError> Errors = new List<FormValidationError>();
    internal List<FormValidationPendingState> PendingStates = new List<FormValidationPendingState>();

    internal FormCustomValidationCore(FormCore forms)
    {
        Forms = forms;
    }

    internal FormCustomValidationResult BeginCustomValidation(string? formKey, string? fieldKey, string? ruleKey, bool returnsTask)
    {
        int formIndex = Forms.FindCheckedFormIndex(formKey);
        if (formIndex < 0)
        {
            return CreateCustomValidationFailure(Forms.GetFormLookupCode(formKey));
        }

        UiForm form = Forms.Forms[formIndex];
        UiField? field = null;
        UiValidationRule? rule;
        if (fieldKey == null)
        {
            rule = FindCustomValidationRule(form.Rules, ruleKey);
            if (rule == null)
            {
                return CreateCustomValidationFailure(GetCustomValidationRuleCode(ruleKey));
            }

            if (rule.Kind != "custom-form")
            {
                return CreateCustomValidationFailure("validation.invalid-custom-scope");
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(fieldKey))
            {
                return CreateCustomValidationFailure("form.invalid-field-key");
            }

            int fieldIndex = Forms.FindFormFieldIndex(form, fieldKey);
            if (fieldIndex < 0)
            {
                return CreateCustomValidationFailure("form.unknown-field");
            }

            field = form.Fields[fieldIndex];
            rule = FindCustomValidationRule(field.Rules, ruleKey);
            if (rule == null)
            {
                return CreateCustomValidationFailure(GetCustomValidationRuleCode(ruleKey));
            }

            if (rule.Kind != "custom-field" && rule.Kind != "custom-cross-field")
            {
                return CreateCustomValidationFailure("validation.invalid-custom-scope");
            }
        }

        if (string.IsNullOrWhiteSpace(rule.HandlerId))
        {
            return CreateCustomValidationFailure("validation.invalid-handler");
        }

        if (form.Validation.Version == int.MaxValue)
        {
            RouteCustomValidationError(form, field, rule, form.Validation.Version, form.State.Version, "validation.generation-exhausted");
            return CreateCustomValidationFailure("validation.generation-exhausted");
        }

        CaptureCustomValidationPendingState(form, field);
        form.Validation.Version++;
        int requestIndex = FindCustomValidationRequestIndex(form.Key, fieldKey, rule.Key);
        FormValidationRequest request;
        if (requestIndex < 0)
        {
            request = new FormValidationRequest();
            Requests.Add(request);
        }
        else
        {
            request = Requests[requestIndex];
            RemovePendingCustomValidationInvocation(request.FormKey, request.FieldKey, request.RuleKey);
        }

        request.FormKey = form.Key;
        request.FieldKey = fieldKey;
        request.RuleKey = rule.Key;
        request.HandlerId = rule.HandlerId;
        request.Kind = rule.Kind;
        request.Generation = form.Validation.Version;
        request.ValueVersion = form.State.Version;
        request.ReturnsTask = returnsTask;
        request.Active = true;
        request.IsCancelled = false;
        PendingInvocations.Add(CreateCustomValidationInvocation(form, field, rule, request));
        RefreshCustomValidationState(form);
        return CreateCustomValidationSuccess(form, field, request, true, Array.Empty<UiValidationIssue>());
    }

    internal FormCustomValidationResult CancelCustomValidation(string? formKey, string? fieldKey, string? ruleKey)
    {
        int formIndex = Forms.FindCheckedFormIndex(formKey);
        if (formIndex < 0)
        {
            return CreateCustomValidationFailure(Forms.GetFormLookupCode(formKey));
        }

        if (string.IsNullOrWhiteSpace(ruleKey))
        {
            return CreateCustomValidationFailure("validation.invalid-rule-key");
        }

        int requestIndex = FindCustomValidationRequestIndex(formKey, fieldKey, ruleKey);
        if (requestIndex < 0)
        {
            return CreateCustomValidationFailure("validation.unknown-request");
        }

        UiForm form = Forms.Forms[formIndex];
        FormValidationRequest request = Requests[requestIndex];
        UiField? field = FindOptionalCustomValidationField(form, request.FieldKey);
        if (!request.Active)
        {
            return CreateCustomValidationSuccess(form, field, request, false, Array.Empty<UiValidationIssue>());
        }

        request.Active = false;
        request.IsCancelled = true;
        RemovePendingCustomValidationInvocation(request.FormKey, request.FieldKey, request.RuleKey);
        RefreshCustomValidationState(form);
        return CreateCustomValidationSuccess(form, field, request, true, Array.Empty<UiValidationIssue>());
    }

    internal FormCustomValidationResult CompleteCustomValidation(string? formKey, string? fieldKey, string? ruleKey, int generation, int valueVersion, bool handlerSucceeded, UiValidationIssue[]? issues, string? errorCode)
    {
        int formIndex = Forms.FindCheckedFormIndex(formKey);
        if (formIndex < 0)
        {
            return CreateCustomValidationFailure(Forms.GetFormLookupCode(formKey));
        }

        if (string.IsNullOrWhiteSpace(ruleKey))
        {
            return CreateCustomValidationFailure("validation.invalid-rule-key");
        }

        UiForm form = Forms.Forms[formIndex];
        int requestIndex = FindCustomValidationRequestIndex(form.Key, fieldKey, ruleKey);
        if (requestIndex < 0)
        {
            return CreateCustomValidationStale(form, null, generation, valueVersion);
        }

        FormValidationRequest request = Requests[requestIndex];
        UiField? field = FindOptionalCustomValidationField(form, request.FieldKey);
        if (!request.Active || request.IsCancelled || generation != request.Generation || valueVersion != request.ValueVersion)
        {
            return CreateCustomValidationStale(form, field, generation, valueVersion);
        }

        if (form.State.Version != request.ValueVersion)
        {
            request.Active = false;
            RefreshCustomValidationState(form);
            return CreateCustomValidationStale(form, field, generation, valueVersion);
        }

        request.Active = false;
        if (!handlerSucceeded)
        {
            RouteCustomValidationError(form, field, FindRequestCustomValidationRule(form, field, request), generation, valueVersion, errorCode);
            RefreshCustomValidationState(form);
            return CreateCustomValidationSuccess(form, field, request, true, Array.Empty<UiValidationIssue>());
        }

        string issueCode = ValidateCustomValidationIssues(form, field, issues);
        if (issueCode.Length > 0 || issues == null)
        {
            RouteCustomValidationError(form, field, FindRequestCustomValidationRule(form, field, request), generation, valueVersion, issueCode);
            RefreshCustomValidationState(form);
            return CreateCustomValidationSuccess(form, field, request, true, Array.Empty<UiValidationIssue>());
        }

        UiValidationIssue[] copiedIssues = CopyCustomValidationIssues(form, field, issues, valueVersion);
        RefreshCustomValidationState(form);
        FormCustomValidationResult result = CreateCustomValidationSuccess(form, field, request, true, copiedIssues);
        result.HasOutcome = true;
        return result;
    }

    internal FormValidationInvocation[] DrainCustomValidationInvocations()
    {
        FormValidationInvocation[] result = new FormValidationInvocation[PendingInvocations.Count];
        int index = 0;
        while (index < PendingInvocations.Count)
        {
            result[index] = CopyCustomValidationInvocation(PendingInvocations[index]);
            index++;
        }

        PendingInvocations.Clear();
        return result;
    }

    internal FormValidationError[] DrainCustomValidationErrors()
    {
        FormValidationError[] result = new FormValidationError[Errors.Count];
        int index = 0;
        while (index < Errors.Count)
        {
            FormValidationError source = Errors[index];
            result[index] = new FormValidationError
            {
                FormKey = source.FormKey,
                FieldKey = source.FieldKey,
                RuleKey = source.RuleKey,
                HandlerId = source.HandlerId,
                Generation = source.Generation,
                ValueVersion = source.ValueVersion,
                Code = source.Code
            };
            index++;
        }

        Errors.Clear();
        return result;
    }

    internal UiValidationRule? FindCustomValidationRule(UiValidationRule[] rules, string? ruleKey)
    {
        if (string.IsNullOrWhiteSpace(ruleKey))
        {
            return null;
        }

        int index = 0;
        while (index < rules.Length)
        {
            if (ValueComparer.TextEquals(rules[index].Key, ruleKey))
            {
                return rules[index];
            }

            index++;
        }

        return null;
    }

    internal string GetCustomValidationRuleCode(string? ruleKey)
    {
        return string.IsNullOrWhiteSpace(ruleKey) ? "validation.invalid-rule-key" : "validation.unknown-rule";
    }

    internal int FindCustomValidationRequestIndex(string? formKey, string? fieldKey, string? ruleKey)
    {
        int index = 0;
        while (index < Requests.Count)
        {
            FormValidationRequest request = Requests[index];
            if (ValueComparer.TextEquals(request.FormKey, formKey) && ValueComparer.TextEquals(request.FieldKey, fieldKey) && ValueComparer.TextEquals(request.RuleKey, ruleKey))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal UiField? FindOptionalCustomValidationField(UiForm form, string? fieldKey)
    {
        if (fieldKey == null)
        {
            return null;
        }

        int index = Forms.FindFormFieldIndex(form, fieldKey);
        return index < 0 ? null : form.Fields[index];
    }

    internal UiValidationRule FindRequestCustomValidationRule(UiForm form, UiField? field, FormValidationRequest request)
    {
        UiValidationRule[] rules = field == null ? form.Rules : field.Rules;
        UiValidationRule? rule = FindCustomValidationRule(rules, request.RuleKey);
        return rule ?? new UiValidationRule { Key = request.RuleKey, Kind = request.Kind, HandlerId = request.HandlerId };
    }

    internal FormValidationInvocation CreateCustomValidationInvocation(UiForm form, UiField? field, UiValidationRule rule, FormValidationRequest request)
    {
        FormValidationInvocation result = new FormValidationInvocation();
        result.FormKey = form.Key;
        result.FieldKey = request.FieldKey;
        result.RuleKey = rule.Key;
        result.HandlerId = request.HandlerId;
        result.Kind = rule.Kind;
        result.Generation = request.Generation;
        result.ValueVersion = request.ValueVersion;
        result.ReturnsTask = request.ReturnsTask;
        if (field != null)
        {
            result.Value = Forms.ValueStore.CopyStateValue(field.Value);
        }

        result.Values = Forms.CopyValueEntries(form.State.CurrentValues);
        result.Parameters = Forms.CopyValueEntries(rule.Parameters ?? Array.Empty<UiValueEntry>());
        return result;
    }

    internal FormValidationInvocation CopyCustomValidationInvocation(FormValidationInvocation source)
    {
        FormValidationInvocation result = new FormValidationInvocation();
        result.FormKey = source.FormKey;
        result.FieldKey = source.FieldKey;
        result.RuleKey = source.RuleKey;
        result.HandlerId = source.HandlerId;
        result.Kind = source.Kind;
        result.Generation = source.Generation;
        result.ValueVersion = source.ValueVersion;
        result.ReturnsTask = source.ReturnsTask;
        if (source.Value != null)
        {
            result.Value = Forms.ValueStore.CopyStateValue(source.Value);
        }

        result.Values = Forms.CopyValueEntries(source.Values);
        result.Parameters = Forms.CopyValueEntries(source.Parameters);
        return result;
    }

    internal void RemovePendingCustomValidationInvocation(string formKey, string? fieldKey, string ruleKey)
    {
        int index = PendingInvocations.Count - 1;
        while (index >= 0)
        {
            FormValidationInvocation invocation = PendingInvocations[index];
            if (ValueComparer.TextEquals(invocation.FormKey, formKey) && ValueComparer.TextEquals(invocation.FieldKey, fieldKey) && ValueComparer.TextEquals(invocation.RuleKey, ruleKey))
            {
                PendingInvocations.RemoveAt(index);
            }

            index--;
        }
    }

    internal void RefreshCustomValidationState(UiForm form)
    {
        int fieldIndex = 0;
        while (fieldIndex < form.Fields.Length)
        {
            bool pending = false;
            int requestIndex = 0;
            while (requestIndex < Requests.Count)
            {
                FormValidationRequest request = Requests[requestIndex];
                if (request.Active && ValueComparer.TextEquals(request.FormKey, form.Key) && ValueComparer.TextEquals(request.FieldKey, form.Fields[fieldIndex].Key))
                {
                    pending = true;
                }

                requestIndex++;
            }

            if (pending)
            {
                form.Fields[fieldIndex].IsPending = true;
            }
            else
            {
                int pendingStateIndex = FindCustomValidationPendingStateIndex(form.Key, form.Fields[fieldIndex].Key);
                if (pendingStateIndex >= 0)
                {
                    form.Fields[fieldIndex].IsPending = PendingStates[pendingStateIndex].WasPending;
                    PendingStates.RemoveAt(pendingStateIndex);
                }
            }

            fieldIndex++;
        }

        Forms.RefreshFormState(form);
        bool formPending = form.State.IsPending;
        int index = 0;
        while (index < Requests.Count)
        {
            FormValidationRequest request = Requests[index];
            if (request.Active && ValueComparer.TextEquals(request.FormKey, form.Key))
            {
                formPending = true;
            }

            index++;
        }

        form.State.IsPending = formPending;
        form.Validation.IsPending = formPending;
        form.State.CanSubmit = form.Validation.IsValid && !formPending && !form.State.IsSubmitting;
    }

    internal void CaptureCustomValidationPendingState(UiForm form, UiField? field)
    {
        if (field == null || FindCustomValidationPendingStateIndex(form.Key, field.Key) >= 0)
        {
            return;
        }

        FormValidationPendingState state = new FormValidationPendingState();
        state.FormKey = form.Key;
        state.FieldKey = field.Key;
        state.WasPending = field.IsPending;
        PendingStates.Add(state);
    }

    internal int FindCustomValidationPendingStateIndex(string formKey, string fieldKey)
    {
        int index = 0;
        while (index < PendingStates.Count)
        {
            FormValidationPendingState state = PendingStates[index];
            if (ValueComparer.TextEquals(state.FormKey, formKey) && ValueComparer.TextEquals(state.FieldKey, fieldKey))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal string ValidateCustomValidationIssues(UiForm form, UiField? field, UiValidationIssue[]? issues)
    {
        if (issues == null)
        {
            return "validation.null-handler-result";
        }

        if (issues.Length > MaximumValidationIssues)
        {
            return "validation.too-many-issues";
        }

        int index = 0;
        while (index < issues.Length)
        {
            UiValidationIssue? issue = issues[index];
            if (issue == null)
            {
                return "validation.null-issue";
            }

            if (string.IsNullOrWhiteSpace(issue.Id) || string.IsNullOrWhiteSpace(issue.Code) || string.IsNullOrWhiteSpace(issue.Message))
            {
                return "validation.invalid-issue";
            }

            if (issue.Severity != UiValidationSeverity.Information && issue.Severity != UiValidationSeverity.Warning && issue.Severity != UiValidationSeverity.Error)
            {
                return "validation.invalid-issue-severity";
            }

            string? checkedFieldKey = issue.FieldKey ?? (field == null ? null : field.Key);
            if (checkedFieldKey != null && Forms.FindFormFieldIndex(form, checkedFieldKey) < 0)
            {
                return "validation.unknown-issue-field";
            }

            index++;
        }

        return string.Empty;
    }

    internal UiValidationIssue[] CopyCustomValidationIssues(UiForm form, UiField? field, UiValidationIssue[] issues, int valueVersion)
    {
        UiValidationIssue[] result = new UiValidationIssue[issues.Length];
        int index = 0;
        while (index < issues.Length)
        {
            UiValidationIssue source = issues[index];
            UiValidationIssue copy = Forms.CopyValidationIssue(source);
            if (copy.FieldKey == null && field != null)
            {
                copy.FieldKey = field.Key;
            }

            copy.Source = "custom";
            copy.ValueVersion = valueVersion;
            result[index] = copy;
            index++;
        }

        return result;
    }

    internal void RouteCustomValidationError(UiForm form, UiField? field, UiValidationRule rule, int generation, int valueVersion, string? errorCode)
    {
        FormValidationError error = new FormValidationError();
        error.FormKey = form.Key;
        error.FieldKey = field == null ? null : field.Key;
        error.RuleKey = rule.Key;
        error.HandlerId = rule.HandlerId ?? string.Empty;
        error.Generation = generation;
        error.ValueVersion = valueVersion;
        error.Code = string.IsNullOrWhiteSpace(errorCode) ? "validation.handler-exception" : errorCode;
        Errors.Add(error);
    }

    internal FormCustomValidationResult CreateCustomValidationSuccess(UiForm form, UiField? field, FormValidationRequest request, bool applied, UiValidationIssue[] issues)
    {
        FormCustomValidationResult result = new FormCustomValidationResult();
        result.Success = true;
        result.Applied = applied;
        result.IsPending = request.Active;
        result.Generation = request.Generation;
        result.ValueVersion = request.ValueVersion;
        result.Form = Forms.CopyForm(form);
        if (field != null)
        {
            result.Field = Forms.CopyFormField(field);
        }

        result.Issues = Forms.CopyValidationIssues(issues);
        return result;
    }

    internal FormCustomValidationResult CreateCustomValidationStale(UiForm form, UiField? field, int generation, int valueVersion)
    {
        FormCustomValidationResult result = new FormCustomValidationResult();
        result.Success = true;
        result.Code = "validation.stale-result";
        result.Generation = generation;
        result.ValueVersion = valueVersion;
        result.Form = Forms.CopyForm(form);
        if (field != null)
        {
            result.Field = Forms.CopyFormField(field);
        }

        return result;
    }

    internal FormCustomValidationResult CreateCustomValidationFailure(string code)
    {
        return new FormCustomValidationResult { Code = code };
    }
}
