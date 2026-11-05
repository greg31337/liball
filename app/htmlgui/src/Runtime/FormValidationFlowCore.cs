using System;
using System.Collections.Generic;

namespace htmlgui;

internal class FormValidationIssueOwner
{
    internal string FormKey = string.Empty;
    internal string? FieldKey;
    internal string RuleKey = string.Empty;
    internal string Source = string.Empty;
    internal UiValidationIssue[] Issues = Array.Empty<UiValidationIssue>();
}

internal class FormValidationFlowResult
{
    internal bool Success;
    internal bool Applied;
    internal string Code = string.Empty;
    internal int MatchedRuleCount;
    internal int ScheduledCallbackCount;
    internal UiForm? Form;
    internal FormCustomValidationResult? CustomResult;
}

internal class FormValidationMessage
{
    internal string Id = string.Empty;
    internal string IssueId = string.Empty;
    internal string Message = string.Empty;
    internal UiValidationSeverity Severity;
    internal DomNode? Node;
}

internal class FormValidationPresentation
{
    internal bool Success;
    internal string Code = string.Empty;
    internal string FieldKey = string.Empty;
    internal string TargetId = string.Empty;
    internal bool IsInvalid;
    internal string[] DescribedByIds = Array.Empty<string>();
    internal string[] ErrorMessageIds = Array.Empty<string>();
    internal FormValidationMessage[] Messages = Array.Empty<FormValidationMessage>();
    internal UiFocusRequest? FocusRequest;
}

internal class FormValidationFlowCore
{
    internal const string ValidationMessagePrefix = "ui-validation-message-";

    internal FormCore Forms;
    internal FormValidationCore BuiltIn;
    internal FormCustomValidationCore Custom;
    internal List<FormValidationIssueOwner> IssueOwners = new List<FormValidationIssueOwner>();

    internal FormValidationFlowCore(FormCore forms)
    {
        Forms = forms;
        BuiltIn = new FormValidationCore(forms);
        Custom = new FormCustomValidationCore(forms);
    }

    internal FormValidationFlowResult ValidateFormTrigger(string? formKey, string? fieldKey, UiValidationTrigger trigger)
    {
        if (!IsSupportedValidationTrigger(trigger))
        {
            return CreateValidationFlowFailure("validation.invalid-trigger");
        }

        int formIndex = Forms.FindCheckedFormIndex(formKey);
        if (formIndex < 0)
        {
            return CreateValidationFlowFailure(Forms.GetFormLookupCode(formKey));
        }

        UiForm form = Forms.Forms[formIndex];
        int matched = 0;
        int scheduled = 0;
        if (fieldKey != null)
        {
            if (string.IsNullOrWhiteSpace(fieldKey))
            {
                return CreateValidationFlowFailure("form.invalid-field-key");
            }

            int fieldIndex = Forms.FindFormFieldIndex(form, fieldKey);
            if (fieldIndex < 0)
            {
                return CreateValidationFlowFailure("form.unknown-field");
            }

            FormValidationFlowResult fieldResult = ValidateTriggeredFieldRules(form, form.Fields[fieldIndex], trigger);
            if (!fieldResult.Success)
            {
                return fieldResult;
            }

            matched += fieldResult.MatchedRuleCount;
            scheduled += fieldResult.ScheduledCallbackCount;
        }
        else
        {
            int currentFieldIndex = 0;
            while (currentFieldIndex < form.Fields.Length)
            {
                FormValidationFlowResult fieldResult = ValidateTriggeredFieldRules(form, form.Fields[currentFieldIndex], trigger);
                if (!fieldResult.Success)
                {
                    return fieldResult;
                }

                matched += fieldResult.MatchedRuleCount;
                scheduled += fieldResult.ScheduledCallbackCount;
                currentFieldIndex++;
            }

            int ruleIndex = 0;
            while (ruleIndex < form.Rules.Length)
            {
                UiValidationRule rule = form.Rules[ruleIndex];
                if (rule.Trigger == trigger)
                {
                    matched++;
                    if (rule.Kind != "custom-form")
                    {
                        return CreateValidationFlowFailure("validation.invalid-form-rule");
                    }

                    FormCustomValidationResult begun = Custom.BeginCustomValidation(form.Key, null, rule.Key, IsTaskReturningValidationRule(rule));
                    if (!begun.Success)
                    {
                        return CreateValidationFlowFailure(begun.Code);
                    }

                    scheduled++;
                }

                ruleIndex++;
            }
        }

        RefreshFormValidationReport(form);
        FormValidationFlowResult result = CreateValidationFlowSuccess(form, matched > 0);
        result.MatchedRuleCount = matched;
        result.ScheduledCallbackCount = scheduled;
        return result;
    }

    internal FormValidationFlowResult CompleteFormCustomValidation(string? formKey, string? fieldKey, string? ruleKey, int generation, int valueVersion, bool handlerSucceeded, UiValidationIssue[]? issues, string? errorCode)
    {
        FormCustomValidationResult completed = Custom.CompleteCustomValidation(formKey, fieldKey, ruleKey, generation, valueVersion, handlerSucceeded, issues, errorCode);
        if (!completed.Success || completed.Form == null)
        {
            FormValidationFlowResult failure = CreateValidationFlowFailure(completed.Code);
            failure.CustomResult = completed;
            return failure;
        }

        int formIndex = Forms.FindFormIndex(completed.Form.Key);
        if (formIndex < 0)
        {
            return CreateValidationFlowFailure("form.unknown");
        }

        UiForm form = Forms.Forms[formIndex];
        if (completed.HasOutcome && ruleKey != null)
        {
            ApplyOwnedValidationIssues(form, fieldKey, ruleKey, "custom", completed.Issues);
            RefreshFormValidationReport(form);
        }

        FormValidationFlowResult result = CreateValidationFlowSuccess(form, completed.Applied);
        result.Code = completed.Code;
        result.CustomResult = completed;
        return result;
    }

    internal FormValidationFlowResult MergeFormValidationIssues(string? formKey, string? fieldKey, UiValidationIssue[]? issues)
    {
        int formIndex = Forms.FindCheckedFormIndex(formKey);
        if (formIndex < 0)
        {
            return CreateValidationFlowFailure(Forms.GetFormLookupCode(formKey));
        }

        UiForm form = Forms.Forms[formIndex];
        string validationCode = ValidateFlowIssues(form, fieldKey, issues);
        if (validationCode.Length > 0 || issues == null)
        {
            return CreateValidationFlowFailure(validationCode);
        }

        int index = 0;
        while (index < issues.Length)
        {
            UiValidationIssue issue = NormalizeFlowIssue(form, fieldKey, issues[index]);
            MergeFlowIssue(form, issue);
            index++;
        }

        RefreshFormValidationReport(form);
        return CreateValidationFlowSuccess(form, issues.Length > 0);
    }

    internal FormValidationFlowResult ReplaceFormValidationIssues(string? formKey, string? fieldKey, string? source, UiValidationIssue[]? issues)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return CreateValidationFlowFailure("validation.invalid-source");
        }

        int formIndex = Forms.FindCheckedFormIndex(formKey);
        if (formIndex < 0)
        {
            return CreateValidationFlowFailure(Forms.GetFormLookupCode(formKey));
        }

        UiForm form = Forms.Forms[formIndex];
        string validationCode = ValidateFlowIssues(form, fieldKey, issues);
        if (validationCode.Length > 0 || issues == null)
        {
            return CreateValidationFlowFailure(validationCode);
        }

        int issueIndex = 0;
        while (issueIndex < issues.Length)
        {
            if (!ValueComparer.TextEquals(issues[issueIndex].Source, source))
            {
                return CreateValidationFlowFailure("validation.issue-source-mismatch");
            }

            issueIndex++;
        }

        bool removed = RemoveFlowIssuesBySource(form, fieldKey, source);
        issueIndex = 0;
        while (issueIndex < issues.Length)
        {
            MergeFlowIssue(form, NormalizeFlowIssue(form, fieldKey, issues[issueIndex]));
            issueIndex++;
        }

        RefreshFormValidationReport(form);
        return CreateValidationFlowSuccess(form, removed || issues.Length > 0);
    }

    internal FormValidationFlowResult ClearFormValidationIssues(string? formKey, string? fieldKey, string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return CreateValidationFlowFailure("validation.invalid-source");
        }

        int formIndex = Forms.FindCheckedFormIndex(formKey);
        if (formIndex < 0)
        {
            return CreateValidationFlowFailure(Forms.GetFormLookupCode(formKey));
        }

        UiForm form = Forms.Forms[formIndex];
        if (fieldKey != null && (string.IsNullOrWhiteSpace(fieldKey) || Forms.FindFormFieldIndex(form, fieldKey) < 0))
        {
            return CreateValidationFlowFailure(string.IsNullOrWhiteSpace(fieldKey) ? "form.invalid-field-key" : "form.unknown-field");
        }

        bool removed = RemoveFlowIssuesBySource(form, fieldKey, source);
        RefreshFormValidationReport(form);
        return CreateValidationFlowSuccess(form, removed);
    }

    internal FormValidationPresentation CreateFieldValidationPresentation(string? formKey, string? fieldKey, UiNode? node)
    {
        FormOperationResult lookup = Forms.FindFormField(formKey, fieldKey);
        if (!lookup.Success || lookup.Form == null || lookup.Field == null)
        {
            return CreateValidationPresentationFailure(lookup.Code);
        }

        if (node == null || node.Input == null || node.Identity == null || string.IsNullOrWhiteSpace(node.Identity.StableId))
        {
            return CreateValidationPresentationFailure("validation.invalid-field-node");
        }

        UiField field = lookup.Field;
        FormValidationMessage[] messages = new FormValidationMessage[field.Issues.Length];
        List<string> describedBy = new List<string>();
        List<string> errorMessages = new List<string>();
        int index = 0;
        while (index < field.Issues.Length)
        {
            UiValidationIssue issue = field.Issues[index];
            string messageId = CreateValidationMessageId(lookup.Form.Key, field.Key, issue);
            describedBy.Add(messageId);
            if (issue.Severity == UiValidationSeverity.Error)
            {
                errorMessages.Add(messageId);
            }

            messages[index] = CreateValidationMessage(issue, messageId, lookup.Form.Validation.Version);
            index++;
        }

        SetFormValidationRelationships(node, describedBy, errorMessages);
        FormValidationPresentation result = new FormValidationPresentation();
        result.Success = true;
        result.FieldKey = field.Key;
        result.TargetId = GetValidationFocusTargetId(node);
        result.IsInvalid = errorMessages.Count > 0;
        result.DescribedByIds = describedBy.ToArray();
        result.ErrorMessageIds = errorMessages.ToArray();
        result.Messages = messages;
        if (ValueComparer.TextEquals(lookup.Form.Validation.FirstInvalidFieldKey, field.Key))
        {
            result.FocusRequest = new UiFocusRequest
            {
                Operation = UiFocusOperation.Focus,
                TargetId = result.TargetId,
                Reveal = true
            };
        }

        return result;
    }

    internal FormValidationFlowResult ValidateTriggeredFieldRules(UiForm form, UiField field, UiValidationTrigger trigger)
    {
        int matched = 0;
        int scheduled = 0;
        int ruleIndex = 0;
        while (ruleIndex < field.Rules.Length)
        {
            UiValidationRule rule = field.Rules[ruleIndex];
            if (rule.Trigger == trigger)
            {
                matched++;
                if (rule.Kind == "custom-field" || rule.Kind == "custom-cross-field")
                {
                    FormCustomValidationResult begun = Custom.BeginCustomValidation(form.Key, field.Key, rule.Key, IsTaskReturningValidationRule(rule));
                    if (!begun.Success)
                    {
                        return CreateValidationFlowFailure(begun.Code);
                    }

                    scheduled++;
                }
                else
                {
                    ValidationRuleResult evaluated = BuiltIn.EvaluateBuiltInRule(form.Key, field.Key, rule.Key);
                    if (!evaluated.Success)
                    {
                        return CreateValidationFlowFailure(evaluated.Code);
                    }

                    UiValidationIssue[] outcome = evaluated.Issue == null ? Array.Empty<UiValidationIssue>() : new UiValidationIssue[] { evaluated.Issue };
                    ApplyOwnedValidationIssues(form, field.Key, rule.Key, "local", outcome);
                }
            }

            ruleIndex++;
        }

        FormValidationFlowResult result = CreateValidationFlowSuccess(form, matched > 0);
        result.MatchedRuleCount = matched;
        result.ScheduledCallbackCount = scheduled;
        return result;
    }

    internal bool IsSupportedValidationTrigger(UiValidationTrigger trigger)
    {
        return trigger == UiValidationTrigger.Input || trigger == UiValidationTrigger.Change || trigger == UiValidationTrigger.Blur || trigger == UiValidationTrigger.Submit || trigger == UiValidationTrigger.Manual;
    }

    internal bool IsTaskReturningValidationRule(UiValidationRule rule)
    {
        int index = 0;
        while (index < rule.Parameters.Length)
        {
            UiValueEntry parameter = rule.Parameters[index];
            if (parameter.Key == "returns-task" && parameter.Value.Kind == UiValueKind.Boolean)
            {
                return parameter.Value.BooleanValue;
            }

            index++;
        }

        return false;
    }

    internal void ApplyOwnedValidationIssues(UiForm form, string? fieldKey, string ruleKey, string source, UiValidationIssue[] issues)
    {
        int ownerIndex = FindValidationIssueOwnerIndex(form.Key, fieldKey, ruleKey, source);
        if (ownerIndex >= 0)
        {
            UiValidationIssue[] previous = IssueOwners[ownerIndex].Issues;
            int previousIndex = 0;
            while (previousIndex < previous.Length)
            {
                RemoveFlowIssue(form, previous[previousIndex]);
                previousIndex++;
            }

            IssueOwners.RemoveAt(ownerIndex);
        }

        UiValidationIssue[] ownedIssues = new UiValidationIssue[issues.Length];
        int index = 0;
        while (index < issues.Length)
        {
            UiValidationIssue normalized = NormalizeFlowIssue(form, fieldKey, issues[index]);
            MergeFlowIssue(form, normalized);
            ownedIssues[index] = Forms.CopyValidationIssue(normalized);
            index++;
        }

        if (ownedIssues.Length > 0)
        {
            IssueOwners.Add(new FormValidationIssueOwner
            {
                FormKey = form.Key,
                FieldKey = fieldKey,
                RuleKey = ruleKey,
                Source = source,
                Issues = ownedIssues
            });
        }
    }

    internal int FindValidationIssueOwnerIndex(string formKey, string? fieldKey, string ruleKey, string source)
    {
        int index = 0;
        while (index < IssueOwners.Count)
        {
            FormValidationIssueOwner owner = IssueOwners[index];
            if (ValueComparer.TextEquals(owner.FormKey, formKey) && ValueComparer.TextEquals(owner.FieldKey, fieldKey) && ValueComparer.TextEquals(owner.RuleKey, ruleKey) && ValueComparer.TextEquals(owner.Source, source))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal string ValidateFlowIssues(UiForm form, string? fieldKey, UiValidationIssue[]? issues)
    {
        if (fieldKey != null && string.IsNullOrWhiteSpace(fieldKey))
        {
            return "form.invalid-field-key";
        }

        if (fieldKey != null && Forms.FindFormFieldIndex(form, fieldKey) < 0)
        {
            return "form.unknown-field";
        }

        if (issues == null)
        {
            return "validation.null-issues";
        }

        if (issues.Length > FormCustomValidationCore.MaximumValidationIssues)
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

            if (string.IsNullOrWhiteSpace(issue.Id) || string.IsNullOrWhiteSpace(issue.Code) || string.IsNullOrWhiteSpace(issue.Message) || string.IsNullOrWhiteSpace(issue.Source))
            {
                return "validation.invalid-issue";
            }

            if (issue.Severity != UiValidationSeverity.Information && issue.Severity != UiValidationSeverity.Warning && issue.Severity != UiValidationSeverity.Error)
            {
                return "validation.invalid-issue-severity";
            }

            if (fieldKey != null && issue.FieldKey != null && !ValueComparer.TextEquals(fieldKey, issue.FieldKey))
            {
                return "validation.issue-field-mismatch";
            }

            string? targetFieldKey = issue.FieldKey ?? fieldKey;
            if (targetFieldKey != null && Forms.FindFormFieldIndex(form, targetFieldKey) < 0)
            {
                return "validation.unknown-issue-field";
            }

            index++;
        }

        return string.Empty;
    }

    internal UiValidationIssue NormalizeFlowIssue(UiForm form, string? fieldKey, UiValidationIssue source)
    {
        UiValidationIssue result = Forms.CopyValidationIssue(source);
        if (result.FieldKey == null && fieldKey != null)
        {
            result.FieldKey = fieldKey;
        }

        if (result.ValueVersion < 0)
        {
            result.ValueVersion = form.State.Version;
        }

        return result;
    }

    internal void MergeFlowIssue(UiForm form, UiValidationIssue issue)
    {
        if (issue.FieldKey == null)
        {
            List<UiValidationIssue> formIssues = GetFormLevelValidationIssues(form);
            ReplaceOrAddFlowIssue(formIssues, issue);
            SetFormLevelValidationIssues(form, formIssues);
            return;
        }

        int fieldIndex = Forms.FindFormFieldIndex(form, issue.FieldKey);
        if (fieldIndex < 0)
        {
            return;
        }

        List<UiValidationIssue> fieldIssues = CopyFlowIssueList(form.Fields[fieldIndex].Issues);
        ReplaceOrAddFlowIssue(fieldIssues, issue);
        form.Fields[fieldIndex].Issues = CopyFlowIssueArray(fieldIssues);
    }

    internal void ReplaceOrAddFlowIssue(List<UiValidationIssue> issues, UiValidationIssue issue)
    {
        int index = 0;
        while (index < issues.Count)
        {
            if (SameFlowIssueIdentity(issues[index], issue))
            {
                issues[index] = Forms.CopyValidationIssue(issue);
                return;
            }

            index++;
        }

        issues.Add(Forms.CopyValidationIssue(issue));
    }

    internal bool RemoveFlowIssuesBySource(UiForm form, string? fieldKey, string source)
    {
        bool removed = false;
        if (fieldKey == null)
        {
            List<UiValidationIssue> formIssues = GetFormLevelValidationIssues(form);
            removed = RemoveFlowIssueSourceFromList(formIssues, source) || removed;
            SetFormLevelValidationIssues(form, formIssues);
            int index = 0;
            while (index < form.Fields.Length)
            {
                List<UiValidationIssue> fieldIssues = CopyFlowIssueList(form.Fields[index].Issues);
                removed = RemoveFlowIssueSourceFromList(fieldIssues, source) || removed;
                form.Fields[index].Issues = CopyFlowIssueArray(fieldIssues);
                index++;
            }
        }
        else
        {
            int fieldIndex = Forms.FindFormFieldIndex(form, fieldKey);
            if (fieldIndex >= 0)
            {
                List<UiValidationIssue> fieldIssues = CopyFlowIssueList(form.Fields[fieldIndex].Issues);
                removed = RemoveFlowIssueSourceFromList(fieldIssues, source);
                form.Fields[fieldIndex].Issues = CopyFlowIssueArray(fieldIssues);
            }
        }

        RemoveValidationIssueOwners(form.Key, fieldKey, source);
        return removed;
    }

    internal bool RemoveFlowIssueSourceFromList(List<UiValidationIssue> issues, string source)
    {
        bool removed = false;
        int index = issues.Count - 1;
        while (index >= 0)
        {
            if (ValueComparer.TextEquals(issues[index].Source, source))
            {
                issues.RemoveAt(index);
                removed = true;
            }

            index--;
        }

        return removed;
    }

    internal void RemoveFlowIssue(UiForm form, UiValidationIssue issue)
    {
        if (issue.FieldKey == null)
        {
            List<UiValidationIssue> formIssues = GetFormLevelValidationIssues(form);
            RemoveFlowIssueFromList(formIssues, issue);
            SetFormLevelValidationIssues(form, formIssues);
            return;
        }

        int fieldIndex = Forms.FindFormFieldIndex(form, issue.FieldKey);
        if (fieldIndex >= 0)
        {
            List<UiValidationIssue> fieldIssues = CopyFlowIssueList(form.Fields[fieldIndex].Issues);
            RemoveFlowIssueFromList(fieldIssues, issue);
            form.Fields[fieldIndex].Issues = CopyFlowIssueArray(fieldIssues);
        }
    }

    internal void RemoveFlowIssueFromList(List<UiValidationIssue> issues, UiValidationIssue issue)
    {
        int index = issues.Count - 1;
        while (index >= 0)
        {
            if (SameFlowIssueIdentity(issues[index], issue))
            {
                issues.RemoveAt(index);
            }

            index--;
        }
    }

    internal void RemoveValidationIssueOwners(string formKey, string? fieldKey, string source)
    {
        int index = IssueOwners.Count - 1;
        while (index >= 0)
        {
            FormValidationIssueOwner owner = IssueOwners[index];
            bool fieldMatches = fieldKey == null || ValueComparer.TextEquals(owner.FieldKey, fieldKey);
            if (ValueComparer.TextEquals(owner.FormKey, formKey) && fieldMatches && ValueComparer.TextEquals(owner.Source, source))
            {
                IssueOwners.RemoveAt(index);
            }

            index--;
        }
    }

    internal bool SameFlowIssueIdentity(UiValidationIssue left, UiValidationIssue right)
    {
        return ValueComparer.TextEquals(left.Id, right.Id) && ValueComparer.TextEquals(left.Source, right.Source) && ValueComparer.TextEquals(left.FieldKey, right.FieldKey);
    }

    internal List<UiValidationIssue> GetFormLevelValidationIssues(UiForm form)
    {
        List<UiValidationIssue> result = new List<UiValidationIssue>();
        int index = 0;
        while (index < form.Validation.Issues.Length)
        {
            UiValidationIssue issue = form.Validation.Issues[index];
            if (issue.FieldKey == null)
            {
                result.Add(Forms.CopyValidationIssue(issue));
            }

            index++;
        }

        return result;
    }

    internal void SetFormLevelValidationIssues(UiForm form, List<UiValidationIssue> issues)
    {
        form.Validation.Issues = CopyFlowIssueArray(issues);
    }

    internal List<UiValidationIssue> CopyFlowIssueList(UiValidationIssue[] issues)
    {
        List<UiValidationIssue> result = new List<UiValidationIssue>();
        int index = 0;
        while (index < issues.Length)
        {
            result.Add(Forms.CopyValidationIssue(issues[index]));
            index++;
        }

        return result;
    }

    internal UiValidationIssue[] CopyFlowIssueArray(List<UiValidationIssue> issues)
    {
        UiValidationIssue[] result = new UiValidationIssue[issues.Count];
        int index = 0;
        while (index < issues.Count)
        {
            result[index] = Forms.CopyValidationIssue(issues[index]);
            index++;
        }

        return result;
    }

    internal void RefreshFormValidationReport(UiForm form)
    {
        List<UiValidationIssue> summary = GetFormLevelValidationIssues(form);
        bool valid = true;
        string? firstInvalidFieldKey = null;
        int fieldIndex = 0;
        while (fieldIndex < form.Fields.Length)
        {
            UiField field = form.Fields[fieldIndex];
            SortFlowIssues(field.Issues, form);
            bool fieldValid = !ContainsValidationError(field.Issues);
            field.IsValid = fieldValid;
            if (!fieldValid && firstInvalidFieldKey == null)
            {
                firstInvalidFieldKey = field.Key;
            }

            valid = valid && fieldValid;
            int issueIndex = 0;
            while (issueIndex < field.Issues.Length)
            {
                summary.Add(Forms.CopyValidationIssue(field.Issues[issueIndex]));
                issueIndex++;
            }

            fieldIndex++;
        }

        UiValidationIssue[] summaryIssues = CopyFlowIssueArray(summary);
        SortFlowIssues(summaryIssues, form);
        form.Validation.Issues = summaryIssues;
        valid = valid && !ContainsFormValidationError(summaryIssues);
        Custom.RefreshCustomValidationState(form);
        form.Validation.IsValid = valid;
        form.Validation.FirstInvalidFieldKey = firstInvalidFieldKey;
        form.State.CanSubmit = valid && !form.State.IsPending && !form.State.IsSubmitting;
    }

    internal bool ContainsValidationError(UiValidationIssue[] issues)
    {
        int index = 0;
        while (index < issues.Length)
        {
            if (issues[index].Severity == UiValidationSeverity.Error)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal bool ContainsFormValidationError(UiValidationIssue[] issues)
    {
        int index = 0;
        while (index < issues.Length)
        {
            if (issues[index].FieldKey == null && issues[index].Severity == UiValidationSeverity.Error)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal void SortFlowIssues(UiValidationIssue[] issues, UiForm form)
    {
        int index = 1;
        while (index < issues.Length)
        {
            UiValidationIssue candidate = issues[index];
            int insertion = index;
            while (insertion > 0 && CompareFlowIssues(candidate, issues[insertion - 1], form) < 0)
            {
                issues[insertion] = issues[insertion - 1];
                insertion--;
            }

            issues[insertion] = candidate;
            index++;
        }
    }

    internal int CompareFlowIssues(UiValidationIssue left, UiValidationIssue right, UiForm form)
    {
        int comparison = GetValidationSeverityRank(left.Severity) - GetValidationSeverityRank(right.Severity);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = GetValidationFieldOrder(form, left.FieldKey) - GetValidationFieldOrder(form, right.FieldKey);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = ValueComparer.CompareText(left.Id, right.Id);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = ValueComparer.CompareText(left.Source, right.Source);
        if (comparison != 0)
        {
            return comparison;
        }

        return ValueComparer.CompareText(left.Code, right.Code);
    }

    internal int GetValidationSeverityRank(UiValidationSeverity severity)
    {
        if (severity == UiValidationSeverity.Error)
        {
            return 0;
        }

        if (severity == UiValidationSeverity.Warning)
        {
            return 1;
        }

        return 2;
    }

    internal int GetValidationFieldOrder(UiForm form, string? fieldKey)
    {
        if (fieldKey == null)
        {
            return -1;
        }

        int index = Forms.FindFormFieldIndex(form, fieldKey);
        return index < 0 ? int.MaxValue : index;
    }

    internal string CreateValidationMessageId(string formKey, string fieldKey, UiValidationIssue issue)
    {
        string token = StableHash.HashParts(new string[] { formKey, fieldKey, issue.Source, issue.Id });
        return ValidationMessagePrefix + token.Substring(0, 20);
    }

    internal FormValidationMessage CreateValidationMessage(UiValidationIssue issue, string id, int version)
    {
        DomNode text = new DomNode
        {
            Kind = DomNodeKind.Text,
            Key = id + "-text",
            Id = id + "-text",
            Text = issue.Message,
            Version = version
        };
        DomNode node = new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = id,
            Id = id,
            Name = HtmlElements.Span,
            Attributes = new DomAttribute[] { new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-validation-message" } },
            Children = new DomNode[] { text },
            Version = version
        };
        return new FormValidationMessage
        {
            Id = id,
            IssueId = issue.Id,
            Message = issue.Message,
            Severity = issue.Severity,
            Node = node
        };
    }

    internal void SetFormValidationRelationships(UiNode node, List<string> describedBy, List<string> errorMessages)
    {
        if (node.Accessibility == null)
        {
            node.Accessibility = new UiAccessibility();
        }

        UiAriaRelationship[] relationships = node.Accessibility.Relationships ?? Array.Empty<UiAriaRelationship>();
        List<UiAriaRelationship> result = new List<UiAriaRelationship>();
        int index = 0;
        while (index < relationships.Length)
        {
            UiAriaRelationship relationship = relationships[index];
            if (relationship != null && relationship.Name != AriaAttributes.DescribedBy && relationship.Name != AriaAttributes.ErrorMessage)
            {
                result.Add(CopyValidationRelationship(relationship));
            }

            index++;
        }

        List<string> existingDescriptions = ReadExistingValidationRelationship(relationships, AriaAttributes.DescribedBy);
        AddValidationRelationshipIds(existingDescriptions, describedBy);
        if (existingDescriptions.Count > 0)
        {
            result.Add(new UiAriaRelationship { Name = AriaAttributes.DescribedBy, TargetIds = existingDescriptions.ToArray() });
        }

        List<string> existingErrors = ReadExistingValidationRelationship(relationships, AriaAttributes.ErrorMessage);
        AddValidationRelationshipIds(existingErrors, errorMessages);
        if (existingErrors.Count > 0)
        {
            result.Add(new UiAriaRelationship { Name = AriaAttributes.ErrorMessage, TargetIds = existingErrors.ToArray() });
        }

        node.Accessibility.Relationships = result.ToArray();
    }

    internal List<string> ReadExistingValidationRelationship(UiAriaRelationship[] relationships, string name)
    {
        List<string> result = new List<string>();
        int index = 0;
        while (index < relationships.Length)
        {
            UiAriaRelationship? relationship = relationships[index];
            if (relationship != null && relationship.Name == name)
            {
                string[] targets = relationship.TargetIds ?? Array.Empty<string>();
                int targetIndex = 0;
                while (targetIndex < targets.Length)
                {
                    string target = targets[targetIndex];
                    if (!target.StartsWith(ValidationMessagePrefix, StringComparison.Ordinal) && !ContainsValidationText(result, target))
                    {
                        result.Add(target);
                    }

                    targetIndex++;
                }
            }

            index++;
        }

        return result;
    }

    internal void AddValidationRelationshipIds(List<string> target, List<string> additions)
    {
        int index = 0;
        while (index < additions.Count)
        {
            if (!ContainsValidationText(target, additions[index]))
            {
                target.Add(additions[index]);
            }

            index++;
        }
    }

    internal bool ContainsValidationText(List<string> values, string value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (ValueComparer.TextEquals(values[index], value))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal UiAriaRelationship CopyValidationRelationship(UiAriaRelationship source)
    {
        string[] targets = source.TargetIds ?? Array.Empty<string>();
        string[] copiedTargets = new string[targets.Length];
        int index = 0;
        while (index < targets.Length)
        {
            copiedTargets[index] = targets[index];
            index++;
        }

        return new UiAriaRelationship { Name = source.Name, TargetIds = copiedTargets };
    }

    internal string GetValidationFocusTargetId(UiNode node)
    {
        if (node.Input != null && new InputOptionExpander().HasDecorations(node.Input))
        {
            return new InputOptionExpander().CreateSyntheticId(node, "control");
        }

        return node.Identity.StableId;
    }

    internal FormValidationFlowResult CreateValidationFlowSuccess(UiForm form, bool applied)
    {
        return new FormValidationFlowResult
        {
            Success = true,
            Applied = applied,
            Form = Forms.CopyForm(form)
        };
    }

    internal FormValidationFlowResult CreateValidationFlowFailure(string code)
    {
        return new FormValidationFlowResult { Code = code };
    }

    internal FormValidationPresentation CreateValidationPresentationFailure(string code)
    {
        return new FormValidationPresentation { Code = code };
    }
}
