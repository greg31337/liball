using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class FormExternalValidationCoreTests
{
    [Test]
    public void CapturesOneCopiedSnapshotForTheActiveSubmissionVersion()
    {
        FormCore forms = CreateForms();
        FormExternalValidationCore external = CreateExternal(forms);
        FormExternalValidationResult beforeSubmit = external.CaptureFormSubmission("sample");
        forms.BeginFormSubmit("sample");

        FormExternalValidationResult captured = external.CaptureFormSubmission("sample");
        FormExternalValidationResult capturedAgain = external.CaptureFormSubmission("sample");
        forms.CompleteFormSubmit("sample", false);
        forms.UpdateFormFieldValue("sample", "alpha", Text("edited"));
        FormSubmissionSnapshot snapshot = external.CopyFormSubmission(external.Submissions.Single());

        Assert.Multiple(() =>
        {
            Assert.That(beforeSubmit.Code, Is.EqualTo("validation.form-not-submitting"));
            Assert.That(captured.Success, Is.True);
            Assert.That(captured.Applied, Is.True);
            Assert.That(captured.SubmittedVersion, Is.EqualTo(0));
            Assert.That(capturedAgain.SubmittedVersion, Is.EqualTo(0));
            Assert.That(external.Submissions, Has.Count.EqualTo(1));
            Assert.That(snapshot.Values.Single(item => item.Key == "alpha").Value.StringValue, Is.EqualTo("a"));
            Assert.That(forms.ReadForm("sample").Form!.Fields.Single(item => item.Key == "alpha").Value.StringValue, Is.EqualTo("edited"));
        });
    }

    [Test]
    public void IngestsVersionMatchedServerFieldAndFormIssuesIntoTheSummary()
    {
        FormCore forms = CreateForms();
        FormExternalValidationCore external = Capture(forms);
        UiValidationIssue fieldIssue = Issue("alpha-error", "server.alpha", "Alpha failed", "server", "alpha", 0);
        UiValidationIssue formIssue = Issue("form-error", "server.form", "Form failed", "server", null, 0);

        FormExternalValidationResult ingested = external.IngestFormValidationIssues("sample", null, 0, "server", new UiValidationIssue[] { formIssue, fieldIssue });

        Assert.Multiple(() =>
        {
            Assert.That(ingested.Success, Is.True);
            Assert.That(ingested.Applied, Is.True);
            Assert.That(ingested.AcceptedIssueCount, Is.EqualTo(2));
            Assert.That(ingested.StaleIssueCount, Is.EqualTo(0));
            Assert.That(ingested.Form!.Fields.Single(item => item.Key == "alpha").Issues.Single().Id, Is.EqualTo("alpha-error"));
            Assert.That(ingested.Form.Validation.Issues.Select(item => item.Id), Is.EqualTo(new string[] { "form-error", "alpha-error" }));
            Assert.That(ingested.Form.Validation.FirstInvalidFieldKey, Is.EqualTo("alpha"));
            Assert.That(ingested.Form.Validation.IsValid, Is.False);
            Assert.That(ingested.Form.State.CanSubmit, Is.False);
        });
    }

    [Test]
    public void AcceptsUnchangedFieldsButSuppressesEditedFieldAndFormIssues()
    {
        FormCore forms = CreateForms();
        FormExternalValidationCore external = Capture(forms);
        forms.CompleteFormSubmit("sample", false);
        forms.UpdateFormFieldValue("sample", "alpha", Text("new alpha"));
        UiValidationIssue alpha = Issue("alpha", "server.alpha", "Old alpha error", "server", "alpha", 0);
        UiValidationIssue zeta = Issue("zeta", "server.zeta", "Zeta error", "server", "zeta", 0);
        UiValidationIssue form = Issue("form", "server.form", "Old form error", "server", null, 0);

        FormExternalValidationResult ingested = external.IngestFormValidationIssues("sample", null, 0, "server", new UiValidationIssue[] { form, alpha, zeta });

        Assert.Multiple(() =>
        {
            Assert.That(ingested.Success, Is.True);
            Assert.That(ingested.Code, Is.EqualTo("validation.partial-stale-submission"));
            Assert.That(ingested.AcceptedIssueCount, Is.EqualTo(1));
            Assert.That(ingested.StaleIssueCount, Is.EqualTo(2));
            Assert.That(ingested.Form!.Fields.Single(item => item.Key == "alpha").Value.StringValue, Is.EqualTo("new alpha"));
            Assert.That(ingested.Form.Fields.Single(item => item.Key == "alpha").Issues, Is.Empty);
            Assert.That(ingested.Form.Fields.Single(item => item.Key == "zeta").Issues.Single().Id, Is.EqualTo("zeta"));
            Assert.That(ingested.Form.Validation.Issues.Select(item => item.Id), Is.EqualTo(new string[] { "zeta" }));
        });
    }

    [Test]
    public void DoesNotReplaceNewerFieldIssuesWithAStaleEditedFieldResponse()
    {
        FormCore forms = CreateForms();
        FormValidationFlowCore flow = new FormValidationFlowCore(forms);
        FormExternalValidationCore external = new FormExternalValidationCore(flow);
        forms.BeginFormSubmit("sample");
        external.CaptureFormSubmission("sample");
        forms.CompleteFormSubmit("sample", false);
        forms.UpdateFormFieldValue("sample", "alpha", Text("edited"));
        flow.MergeFormValidationIssues("sample", "alpha", new UiValidationIssue[]
        {
            Issue("newer", "host.newer", "Newer issue", "host", null, 1)
        });

        FormExternalValidationResult stale = external.IngestFormValidationIssues("sample", "alpha", 0, "host", new UiValidationIssue[]
        {
            Issue("older", "host.older", "Older issue", "host", null, 0)
        });

        Assert.Multiple(() =>
        {
            Assert.That(stale.Success, Is.True);
            Assert.That(stale.Applied, Is.False);
            Assert.That(stale.Code, Is.EqualTo("validation.stale-submission"));
            Assert.That(stale.AcceptedIssueCount, Is.EqualTo(0));
            Assert.That(stale.StaleIssueCount, Is.EqualTo(1));
            Assert.That(stale.Form!.Fields.Single(item => item.Key == "alpha").Issues.Single().Id, Is.EqualTo("newer"));
            Assert.That(stale.Form.Fields.Single(item => item.Key == "alpha").Value.StringValue, Is.EqualTo("edited"));
        });
    }

    [Test]
    public void ReplacesExternalIssuesOnlyWithinTheRequestedCurrentScope()
    {
        FormCore forms = CreateForms();
        FormExternalValidationCore external = Capture(forms);
        external.IngestFormValidationIssues("sample", null, 0, "server", new UiValidationIssue[]
        {
            Issue("alpha", "server.alpha", "Alpha", "server", "alpha", 0),
            Issue("zeta", "server.zeta", "Zeta", "server", "zeta", 0)
        });

        FormExternalValidationResult clearedAlpha = external.IngestFormValidationIssues("sample", "alpha", 0, "server", System.Array.Empty<UiValidationIssue>());
        FormExternalValidationResult replacedZeta = external.IngestFormValidationIssues("sample", "zeta", 0, "server", new UiValidationIssue[]
        {
            Issue("zeta-new", "server.zeta-new", "New zeta", "server", null, 0)
        });

        Assert.Multiple(() =>
        {
            Assert.That(clearedAlpha.Applied, Is.True);
            Assert.That(clearedAlpha.Form!.Fields.Single(item => item.Key == "alpha").Issues, Is.Empty);
            Assert.That(clearedAlpha.Form.Fields.Single(item => item.Key == "zeta").Issues.Single().Id, Is.EqualTo("zeta"));
            Assert.That(replacedZeta.Form!.Fields.Single(item => item.Key == "zeta").Issues.Single().Id, Is.EqualTo("zeta-new"));
            Assert.That(replacedZeta.Form.Fields.Single(item => item.Key == "alpha").Issues, Is.Empty);
        });
    }

    [Test]
    public void HostAndServerIssuesWithTheSameIdMergeAndReplaceBySource()
    {
        FormCore forms = CreateForms();
        FormExternalValidationCore external = Capture(forms);
        external.IngestFormValidationIssues("sample", "alpha", 0, "server", new UiValidationIssue[]
        {
            Issue("shared", "server.shared", "Server issue", "server", null, 0)
        });

        FormExternalValidationResult merged = external.IngestFormValidationIssues("sample", "alpha", 0, "host", new UiValidationIssue[]
        {
            Issue("shared", "host.shared", "Host issue", "host", null, 0)
        });
        FormExternalValidationResult serverCleared = external.IngestFormValidationIssues("sample", "alpha", 0, "server", System.Array.Empty<UiValidationIssue>());

        Assert.Multiple(() =>
        {
            Assert.That(merged.Form!.Fields.Single(item => item.Key == "alpha").Issues.Select(item => item.Source), Is.EqualTo(new string[] { "host", "server" }));
            Assert.That(merged.Form.Validation.Issues.Select(item => item.Code), Is.EqualTo(new string[] { "host.shared", "server.shared" }));
            Assert.That(serverCleared.Applied, Is.True);
            Assert.That(serverCleared.Form!.Fields.Single(item => item.Key == "alpha").Issues.Single().Source, Is.EqualTo("host"));
            Assert.That(serverCleared.Form.Validation.Issues.Single().Code, Is.EqualTo("host.shared"));
        });
    }

    [Test]
    public void RejectsOutOfOrderResponsesAfterANewerSubmissionSnapshot()
    {
        FormCore forms = CreateForms();
        FormValidationFlowCore flow = new FormValidationFlowCore(forms);
        FormExternalValidationCore external = new FormExternalValidationCore(flow);
        forms.BeginFormSubmit("sample");
        external.CaptureFormSubmission("sample");
        forms.CompleteFormSubmit("sample", false);
        forms.UpdateFormFieldValue("sample", "alpha", Text("version one"));
        forms.BeginFormSubmit("sample");
        external.CaptureFormSubmission("sample");
        external.IngestFormValidationIssues("sample", "zeta", 1, "server", new UiValidationIssue[]
        {
            Issue("newer", "server.newer", "Newer", "server", null, 1)
        });

        FormExternalValidationResult older = external.IngestFormValidationIssues("sample", "zeta", 0, "server", new UiValidationIssue[]
        {
            Issue("older", "server.older", "Older", "server", null, 0)
        });

        Assert.Multiple(() =>
        {
            Assert.That(older.Success, Is.True);
            Assert.That(older.Applied, Is.False);
            Assert.That(older.Code, Is.EqualTo("validation.stale-submission"));
            Assert.That(older.SubmittedVersion, Is.EqualTo(0));
            Assert.That(older.Form!.Fields.Single(item => item.Key == "zeta").Issues.Single().Id, Is.EqualTo("newer"));
        });
    }

    [Test]
    public void DiscardsSubmissionSnapshotsAndRejectsMalformedExternalResults()
    {
        FormCore forms = CreateForms();
        FormExternalValidationCore external = Capture(forms);
        UiValidationIssue server = Issue("issue", "server.issue", "Issue", "server", "alpha", 0);
        UiValidationIssue wrongSource = Issue("issue", "server.issue", "Issue", "host", "alpha", 0);
        UiValidationIssue wrongVersion = Issue("issue", "server.issue", "Issue", "server", "alpha", 1);

        Assert.Multiple(() =>
        {
            Assert.That(external.IngestFormValidationIssues("sample", "alpha", -1, "server", new UiValidationIssue[] { server }).Code, Is.EqualTo("validation.invalid-submitted-version"));
            Assert.That(external.IngestFormValidationIssues("sample", "alpha", 0, "remote", new UiValidationIssue[] { server }).Code, Is.EqualTo("validation.invalid-external-source"));
            Assert.That(external.IngestFormValidationIssues("sample", "alpha", 0, "server", null).Code, Is.EqualTo("validation.null-issues"));
            Assert.That(external.IngestFormValidationIssues("sample", "alpha", 0, "server", new UiValidationIssue[] { wrongSource }).Code, Is.EqualTo("validation.issue-source-mismatch"));
            Assert.That(external.IngestFormValidationIssues("sample", "alpha", 0, "server", new UiValidationIssue[] { wrongVersion }).Code, Is.EqualTo("validation.issue-version-mismatch"));
            Assert.That(external.IngestFormValidationIssues("sample", "missing", 0, "server", new UiValidationIssue[] { server }).Code, Is.EqualTo("validation.unknown-submitted-field"));
        });

        FormExternalValidationResult discarded = external.DiscardFormSubmission("sample", 0);
        FormExternalValidationResult unknown = external.IngestFormValidationIssues("sample", "alpha", 0, "server", new UiValidationIssue[] { server });

        Assert.Multiple(() =>
        {
            Assert.That(discarded.Success, Is.True);
            Assert.That(discarded.Applied, Is.True);
            Assert.That(external.Submissions, Is.Empty);
            Assert.That(unknown.Code, Is.EqualTo("validation.unknown-submission"));
            Assert.That(external.DiscardFormSubmission("sample", 0).Code, Is.EqualTo("validation.unknown-submission"));
        });
    }

    private static FormExternalValidationCore Capture(FormCore forms)
    {
        FormExternalValidationCore external = CreateExternal(forms);
        forms.BeginFormSubmit("sample");
        external.CaptureFormSubmission("sample");
        return external;
    }

    private static FormExternalValidationCore CreateExternal(FormCore forms)
    {
        return new FormExternalValidationCore(new FormValidationFlowCore(forms));
    }

    private static FormCore CreateForms()
    {
        FormCore forms = new FormCore();
        forms.RegisterForm(new UiForm
        {
            Key = "sample",
            Fields = new UiField[] { Field("zeta", "z"), Field("alpha", "a") }
        });
        return forms;
    }

    private static UiField Field(string key, string value)
    {
        return new UiField { Key = key, Name = key, Value = Text(value) };
    }

    private static UiValidationIssue Issue(string id, string code, string message, string source, string? fieldKey, int valueVersion)
    {
        return new UiValidationIssue
        {
            Id = id,
            Code = code,
            Message = message,
            Severity = UiValidationSeverity.Error,
            Source = source,
            FieldKey = fieldKey,
            ValueVersion = valueVersion
        };
    }

    private static UiValue Text(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }
}
