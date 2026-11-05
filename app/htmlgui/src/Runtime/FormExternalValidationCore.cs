using System;
using System.Collections.Generic;

namespace htmlgui;

internal class FormSubmissionSnapshot
{
    internal string FormKey = string.Empty;
    internal int ValueVersion;
    internal UiValueEntry[] Values = Array.Empty<UiValueEntry>();
}

internal class FormExternalValidationResult
{
    internal bool Success;
    internal bool Applied;
    internal string Code = string.Empty;
    internal int SubmittedVersion;
    internal int AcceptedIssueCount;
    internal int StaleIssueCount;
    internal UiForm? Form;
}

internal class FormExternalValidationCore
{
    internal FormValidationFlowCore Flow;
    internal FormCore Forms;
    internal List<FormSubmissionSnapshot> Submissions = new List<FormSubmissionSnapshot>();

    internal FormExternalValidationCore(FormValidationFlowCore flow)
    {
        Flow = flow;
        Forms = flow.Forms;
    }

    internal FormExternalValidationResult CaptureFormSubmission(string? formKey)
    {
        int formIndex = Forms.FindCheckedFormIndex(formKey);
        if (formIndex < 0)
        {
            return CreateExternalValidationFailure(Forms.GetFormLookupCode(formKey));
        }

        UiForm form = Forms.Forms[formIndex];
        if (!form.State.IsSubmitting)
        {
            return CreateExternalValidationFailure("validation.form-not-submitting");
        }

        int snapshotIndex = FindFormSubmissionIndex(form.Key, form.State.Version);
        FormSubmissionSnapshot snapshot = new FormSubmissionSnapshot();
        snapshot.FormKey = form.Key;
        snapshot.ValueVersion = form.State.Version;
        snapshot.Values = Forms.CopyValueEntries(form.State.CurrentValues);
        if (snapshotIndex < 0)
        {
            Submissions.Add(snapshot);
        }
        else
        {
            Submissions[snapshotIndex] = snapshot;
        }

        FormExternalValidationResult result = CreateExternalValidationSuccess(form, true);
        result.SubmittedVersion = snapshot.ValueVersion;
        return result;
    }

    internal FormExternalValidationResult IngestFormValidationIssues(string? formKey, string? fieldKey, int submittedVersion, string? source, UiValidationIssue[]? issues)
    {
        int formIndex = Forms.FindCheckedFormIndex(formKey);
        if (formIndex < 0)
        {
            return CreateExternalValidationFailure(Forms.GetFormLookupCode(formKey));
        }

        if (submittedVersion < 0)
        {
            return CreateExternalValidationFailure("validation.invalid-submitted-version");
        }

        if (source != "host" && source != "server")
        {
            return CreateExternalValidationFailure("validation.invalid-external-source");
        }

        if (fieldKey != null && string.IsNullOrWhiteSpace(fieldKey))
        {
            return CreateExternalValidationFailure("form.invalid-field-key");
        }

        int submissionIndex = FindFormSubmissionIndex(formKey, submittedVersion);
        if (submissionIndex < 0)
        {
            return CreateExternalValidationFailure("validation.unknown-submission");
        }

        FormSubmissionSnapshot submission = Submissions[submissionIndex];
        string issueCode = ValidateExternalValidationIssues(submission, fieldKey, submittedVersion, source, issues);
        if (issueCode.Length > 0 || issues == null)
        {
            return CreateExternalValidationFailure(issueCode);
        }

        if (submittedVersion < FindLatestFormSubmissionVersion(formKey))
        {
            FormExternalValidationResult stale = CreateExternalValidationSuccess(Forms.Forms[formIndex], false);
            stale.Code = "validation.stale-submission";
            stale.SubmittedVersion = submittedVersion;
            stale.StaleIssueCount = issues.Length;
            return stale;
        }

        UiForm form = Forms.Forms[formIndex];
        List<UiValidationIssue> accepted = new List<UiValidationIssue>();
        int staleCount = 0;
        int issueIndex = 0;
        while (issueIndex < issues.Length)
        {
            UiValidationIssue normalized = Forms.CopyValidationIssue(issues[issueIndex]);
            if (normalized.FieldKey == null && fieldKey != null)
            {
                normalized.FieldKey = fieldKey;
            }

            if (IsExternalValidationIssueCurrent(form, submission, normalized))
            {
                accepted.Add(normalized);
            }
            else
            {
                staleCount++;
            }

            issueIndex++;
        }

        bool removed = ClearCurrentExternalValidationScope(form, submission, fieldKey, source);
        int acceptedIndex = 0;
        while (acceptedIndex < accepted.Count)
        {
            Flow.MergeFlowIssue(form, accepted[acceptedIndex]);
            acceptedIndex++;
        }

        Flow.RefreshFormValidationReport(form);
        FormExternalValidationResult result = CreateExternalValidationSuccess(form, removed || accepted.Count > 0);
        result.SubmittedVersion = submittedVersion;
        result.AcceptedIssueCount = accepted.Count;
        result.StaleIssueCount = staleCount;
        if (staleCount > 0)
        {
            result.Code = accepted.Count == 0 ? "validation.stale-submission" : "validation.partial-stale-submission";
        }

        return result;
    }

    internal FormExternalValidationResult DiscardFormSubmission(string? formKey, int submittedVersion)
    {
        int formIndex = Forms.FindCheckedFormIndex(formKey);
        if (formIndex < 0)
        {
            return CreateExternalValidationFailure(Forms.GetFormLookupCode(formKey));
        }

        if (submittedVersion < 0)
        {
            return CreateExternalValidationFailure("validation.invalid-submitted-version");
        }

        int submissionIndex = FindFormSubmissionIndex(formKey, submittedVersion);
        if (submissionIndex < 0)
        {
            return CreateExternalValidationFailure("validation.unknown-submission");
        }

        Submissions.RemoveAt(submissionIndex);
        FormExternalValidationResult result = CreateExternalValidationSuccess(Forms.Forms[formIndex], true);
        result.SubmittedVersion = submittedVersion;
        return result;
    }

    internal int FindFormSubmissionIndex(string? formKey, int submittedVersion)
    {
        int index = 0;
        while (index < Submissions.Count)
        {
            FormSubmissionSnapshot submission = Submissions[index];
            if (ValueComparer.TextEquals(submission.FormKey, formKey) && submission.ValueVersion == submittedVersion)
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal int FindLatestFormSubmissionVersion(string? formKey)
    {
        int result = -1;
        int index = 0;
        while (index < Submissions.Count)
        {
            FormSubmissionSnapshot submission = Submissions[index];
            if (ValueComparer.TextEquals(submission.FormKey, formKey) && submission.ValueVersion > result)
            {
                result = submission.ValueVersion;
            }

            index++;
        }

        return result;
    }

    internal string ValidateExternalValidationIssues(FormSubmissionSnapshot submission, string? fieldKey, int submittedVersion, string source, UiValidationIssue[]? issues)
    {
        if (issues == null)
        {
            return "validation.null-issues";
        }

        if (issues.Length > FormCustomValidationCore.MaximumValidationIssues)
        {
            return "validation.too-many-issues";
        }

        if (fieldKey != null && Forms.FindValueEntryIndex(submission.Values, fieldKey) < 0)
        {
            return "validation.unknown-submitted-field";
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

            if (!ValueComparer.TextEquals(issue.Source, source))
            {
                return "validation.issue-source-mismatch";
            }

            if (issue.ValueVersion != submittedVersion)
            {
                return "validation.issue-version-mismatch";
            }

            if (fieldKey != null && issue.FieldKey != null && !ValueComparer.TextEquals(fieldKey, issue.FieldKey))
            {
                return "validation.issue-field-mismatch";
            }

            string? issueFieldKey = issue.FieldKey ?? fieldKey;
            if (issueFieldKey != null && Forms.FindValueEntryIndex(submission.Values, issueFieldKey) < 0)
            {
                return "validation.unknown-submitted-field";
            }

            index++;
        }

        return string.Empty;
    }

    internal bool IsExternalValidationIssueCurrent(UiForm form, FormSubmissionSnapshot submission, UiValidationIssue issue)
    {
        if (issue.FieldKey == null)
        {
            return form.State.Version == submission.ValueVersion;
        }

        int submittedIndex = Forms.FindValueEntryIndex(submission.Values, issue.FieldKey);
        int currentIndex = Forms.FindFormFieldIndex(form, issue.FieldKey);
        if (submittedIndex < 0 || currentIndex < 0)
        {
            return false;
        }

        return Forms.ValueStore.StateValuesEqual(submission.Values[submittedIndex].Value, form.Fields[currentIndex].Value);
    }

    internal bool ClearCurrentExternalValidationScope(UiForm form, FormSubmissionSnapshot submission, string? fieldKey, string source)
    {
        if (fieldKey != null)
        {
            UiValidationIssue probe = new UiValidationIssue { FieldKey = fieldKey };
            if (!IsExternalValidationIssueCurrent(form, submission, probe))
            {
                return false;
            }

            return Flow.RemoveFlowIssuesBySource(form, fieldKey, source);
        }

        if (form.State.Version == submission.ValueVersion)
        {
            return Flow.RemoveFlowIssuesBySource(form, null, source);
        }

        bool removed = false;
        int fieldIndex = 0;
        while (fieldIndex < form.Fields.Length)
        {
            UiValidationIssue probe = new UiValidationIssue { FieldKey = form.Fields[fieldIndex].Key };
            if (IsExternalValidationIssueCurrent(form, submission, probe))
            {
                removed = Flow.RemoveFlowIssuesBySource(form, form.Fields[fieldIndex].Key, source) || removed;
            }

            fieldIndex++;
        }

        return removed;
    }

    internal FormSubmissionSnapshot CopyFormSubmission(FormSubmissionSnapshot source)
    {
        FormSubmissionSnapshot result = new FormSubmissionSnapshot();
        result.FormKey = source.FormKey;
        result.ValueVersion = source.ValueVersion;
        result.Values = Forms.CopyValueEntries(source.Values);
        return result;
    }

    internal FormExternalValidationResult CreateExternalValidationSuccess(UiForm form, bool applied)
    {
        FormExternalValidationResult result = new FormExternalValidationResult();
        result.Success = true;
        result.Applied = applied;
        result.Form = Forms.CopyForm(form);
        return result;
    }

    internal FormExternalValidationResult CreateExternalValidationFailure(string code)
    {
        return new FormExternalValidationResult { Code = code };
    }
}
