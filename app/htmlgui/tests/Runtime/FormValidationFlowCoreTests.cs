using System.Linq;
using htmlgui;

namespace htmlgui.Tests;

public class FormValidationFlowCoreTests
{
    [Test]
    public void RunsEveryTriggerAndSchedulesOnlyMatchingCustomCallbacks()
    {
        FormCore forms = new FormCore();
        UiField field = Field("name", string.Empty,
            Rule("on-input", "required", UiValidationTrigger.Input),
            Rule("on-change", "required", UiValidationTrigger.Change),
            CustomRule("on-blur", "custom-field", UiValidationTrigger.Blur, "blur-handler", false),
            CustomRule("on-manual", "custom-cross-field", UiValidationTrigger.Manual, "manual-handler", true),
            Rule("on-submit", "required", UiValidationTrigger.Submit));
        UiForm form = new UiForm
        {
            Key = "profile",
            Fields = new UiField[] { field },
            Rules = new UiValidationRule[]
            {
                CustomRule("form-submit", "custom-form", UiValidationTrigger.Submit, "form-handler", true)
            }
        };
        forms.RegisterForm(form);
        FormValidationFlowCore flow = new FormValidationFlowCore(forms);

        FormValidationFlowResult input = flow.ValidateFormTrigger("profile", "name", UiValidationTrigger.Input);
        FormValidationFlowResult change = flow.ValidateFormTrigger("profile", "name", UiValidationTrigger.Change);
        FormValidationFlowResult blur = flow.ValidateFormTrigger("profile", "name", UiValidationTrigger.Blur);
        FormValidationFlowResult manual = flow.ValidateFormTrigger("profile", "name", UiValidationTrigger.Manual);
        FormValidationFlowResult submit = flow.ValidateFormTrigger("profile", null, UiValidationTrigger.Submit);
        FormValidationInvocation[] callbacks = flow.Custom.DrainCustomValidationInvocations();

        Assert.Multiple(() =>
        {
            Assert.That(input.MatchedRuleCount, Is.EqualTo(1));
            Assert.That(change.MatchedRuleCount, Is.EqualTo(1));
            Assert.That(blur.ScheduledCallbackCount, Is.EqualTo(1));
            Assert.That(manual.ScheduledCallbackCount, Is.EqualTo(1));
            Assert.That(submit.MatchedRuleCount, Is.EqualTo(2));
            Assert.That(submit.ScheduledCallbackCount, Is.EqualTo(1));
            Assert.That(callbacks.Select(item => item.RuleKey), Is.EqualTo(new string[] { "on-blur", "on-manual", "form-submit" }));
            Assert.That(callbacks.Select(item => item.ReturnsTask), Is.EqualTo(new bool[] { false, true, true }));
            Assert.That(submit.Form!.Validation.Issues.Select(item => item.Id), Is.EquivalentTo(new string[] { "on-input", "on-change", "on-submit" }));
            Assert.That(submit.Form.State.IsPending, Is.True);
            Assert.That(flow.ValidateFormTrigger("profile", "name", (UiValidationTrigger)999).Code, Is.EqualTo("validation.invalid-trigger"));
        });
    }

    [Test]
    public void FieldScopedTriggerDoesNotRunFormRulesButFullFormTriggerDoes()
    {
        FormCore forms = new FormCore();
        forms.RegisterForm(new UiForm
        {
            Key = "profile",
            Fields = new UiField[]
            {
                Field("name", "Ada", CustomRule("field-rule", "custom-field", UiValidationTrigger.Manual, "field-handler", false))
            },
            Rules = new UiValidationRule[]
            {
                CustomRule("form-rule", "custom-form", UiValidationTrigger.Manual, "form-handler", false)
            }
        });
        FormValidationFlowCore flow = new FormValidationFlowCore(forms);

        FormValidationFlowResult fieldOnly = flow.ValidateFormTrigger("profile", "name", UiValidationTrigger.Manual);
        FormValidationInvocation[] fieldInvocations = flow.Custom.DrainCustomValidationInvocations();
        flow.Custom.CancelCustomValidation("profile", "name", "field-rule");
        FormValidationFlowResult fullForm = flow.ValidateFormTrigger("profile", null, UiValidationTrigger.Manual);
        FormValidationInvocation[] formInvocations = flow.Custom.DrainCustomValidationInvocations();

        Assert.Multiple(() =>
        {
            Assert.That(fieldOnly.MatchedRuleCount, Is.EqualTo(1));
            Assert.That(fieldInvocations.Select(item => item.RuleKey), Is.EqualTo(new string[] { "field-rule" }));
            Assert.That(fullForm.MatchedRuleCount, Is.EqualTo(2));
            Assert.That(formInvocations.Select(item => item.RuleKey), Is.EqualTo(new string[] { "field-rule", "form-rule" }));
        });
    }

    [Test]
    public void RevalidatingOneRuleClearsOnlyThatRulesPriorIssue()
    {
        FormCore forms = new FormCore();
        forms.RegisterForm(new UiForm
        {
            Key = "profile",
            Fields = new UiField[]
            {
                Field("name", string.Empty,
                    Rule("input-required", "required", UiValidationTrigger.Input),
                    Rule("change-required", "required", UiValidationTrigger.Change))
            }
        });
        FormValidationFlowCore flow = new FormValidationFlowCore(forms);
        flow.ValidateFormTrigger("profile", "name", UiValidationTrigger.Input);
        flow.ValidateFormTrigger("profile", "name", UiValidationTrigger.Change);
        forms.UpdateFormFieldValue("profile", "name", Text("Ada"));

        FormValidationFlowResult revalidated = flow.ValidateFormTrigger("profile", "name", UiValidationTrigger.Input);

        Assert.Multiple(() =>
        {
            Assert.That(revalidated.Form!.Fields.Single().Issues.Select(item => item.Id), Is.EqualTo(new string[] { "change-required" }));
            Assert.That(revalidated.Form.Validation.Issues.Select(item => item.Id), Is.EqualTo(new string[] { "change-required" }));
            Assert.That(revalidated.Form.Validation.IsValid, Is.False);
        });
    }

    [Test]
    public void MergesReplacesAndClearsIssuesWithSeverityOrderedSummaries()
    {
        FormCore forms = StandardForms();
        FormValidationFlowCore flow = new FormValidationFlowCore(forms);
        UiValidationIssue warning = Issue("warning", "warning.code", "Warning", UiValidationSeverity.Warning, "local", "alpha");
        UiValidationIssue information = Issue("information", "information.code", "Information", UiValidationSeverity.Information, "host", null);
        UiValidationIssue error = Issue("error", "error.code", "Error", UiValidationSeverity.Error, "local", "zeta");
        flow.MergeFormValidationIssues("sample", null, new UiValidationIssue[] { warning, information, error });
        UiValidationIssue replacement = Issue("warning", "warning.changed", "Changed warning", UiValidationSeverity.Warning, "local", "alpha");

        FormValidationFlowResult merged = flow.MergeFormValidationIssues("sample", null, new UiValidationIssue[] { replacement });
        FormValidationFlowResult replaced = flow.ReplaceFormValidationIssues("sample", "alpha", "local", new UiValidationIssue[]
        {
            Issue("new-information", "new.code", "New", UiValidationSeverity.Information, "local", "alpha")
        });
        FormValidationFlowResult cleared = flow.ClearFormValidationIssues("sample", null, "host");

        Assert.Multiple(() =>
        {
            Assert.That(merged.Form!.Validation.Issues.Select(item => item.Severity), Is.EqualTo(new UiValidationSeverity[] { UiValidationSeverity.Error, UiValidationSeverity.Warning, UiValidationSeverity.Information }));
            Assert.That(merged.Form.Fields.Single(item => item.Key == "alpha").Issues.Single().Message, Is.EqualTo("Changed warning"));
            Assert.That(replaced.Form!.Fields.Single(item => item.Key == "alpha").Issues.Single().Id, Is.EqualTo("new-information"));
            Assert.That(replaced.Form.Fields.Single(item => item.Key == "zeta").Issues.Single().Id, Is.EqualTo("error"));
            Assert.That(cleared.Applied, Is.True);
            Assert.That(cleared.Form!.Validation.Issues.Select(item => item.Id), Is.EqualTo(new string[] { "error", "new-information" }));
            Assert.That(cleared.Form.Validation.IsValid, Is.False);
            Assert.That(cleared.Form.State.CanSubmit, Is.False);
        });
    }

    [Test]
    public void SelectsFirstInvalidFieldInFormOrderAndIgnoresWarningsForValidity()
    {
        FormCore forms = StandardForms();
        FormValidationFlowCore flow = new FormValidationFlowCore(forms);
        flow.MergeFormValidationIssues("sample", "alpha", new UiValidationIssue[]
        {
            Issue("alpha-warning", "warning", "Warning", UiValidationSeverity.Warning, "local", null)
        });
        FormValidationFlowResult zetaInvalid = flow.MergeFormValidationIssues("sample", "zeta", new UiValidationIssue[]
        {
            Issue("zeta-error", "error", "Error", UiValidationSeverity.Error, "local", null)
        });
        FormValidationFlowResult alphaInvalid = flow.MergeFormValidationIssues("sample", "alpha", new UiValidationIssue[]
        {
            Issue("alpha-error", "error", "Error", UiValidationSeverity.Error, "local", null)
        });
        FormValidationFlowResult alphaCleared = flow.ClearFormValidationIssues("sample", "alpha", "local");
        FormValidationFlowResult allCleared = flow.ClearFormValidationIssues("sample", "zeta", "local");

        Assert.Multiple(() =>
        {
            Assert.That(zetaInvalid.Form!.Validation.FirstInvalidFieldKey, Is.EqualTo("zeta"));
            Assert.That(alphaInvalid.Form!.Validation.FirstInvalidFieldKey, Is.EqualTo("alpha"));
            Assert.That(alphaCleared.Form!.Validation.FirstInvalidFieldKey, Is.EqualTo("zeta"));
            Assert.That(allCleared.Form!.Validation.FirstInvalidFieldKey, Is.Null);
            Assert.That(allCleared.Form.Validation.IsValid, Is.True);
            Assert.That(allCleared.Form.State.CanSubmit, Is.True);
        });
    }

    [Test]
    public void AppliesAndReplacesAcceptedCustomCallbackOutcomes()
    {
        FormCore forms = new FormCore();
        forms.RegisterForm(new UiForm
        {
            Key = "account",
            Fields = new UiField[]
            {
                Field("email", "ada@example.test", CustomRule("available", "custom-field", UiValidationTrigger.Change, "availability", true))
            }
        });
        FormValidationFlowCore flow = new FormValidationFlowCore(forms);
        flow.ValidateFormTrigger("account", "email", UiValidationTrigger.Change);
        FormValidationInvocation first = flow.Custom.DrainCustomValidationInvocations().Single();
        FormValidationFlowResult invalid = flow.CompleteFormCustomValidation("account", "email", "available", first.Generation, first.ValueVersion, true, new UiValidationIssue[]
        {
            Issue("taken", "email.taken", "Taken", UiValidationSeverity.Error, "ignored", null)
        }, null);
        flow.ValidateFormTrigger("account", "email", UiValidationTrigger.Change);
        FormValidationInvocation second = flow.Custom.DrainCustomValidationInvocations().Single();

        FormValidationFlowResult valid = flow.CompleteFormCustomValidation("account", "email", "available", second.Generation, second.ValueVersion, true, System.Array.Empty<UiValidationIssue>(), null);

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Form!.Fields.Single().Issues.Single().Source, Is.EqualTo("custom"));
            Assert.That(invalid.Form.Validation.IsValid, Is.False);
            Assert.That(invalid.Form.State.IsPending, Is.False);
            Assert.That(valid.Form!.Fields.Single().Issues, Is.Empty);
            Assert.That(valid.Form.Validation.IsValid, Is.True);
            Assert.That(valid.Form.State.CanSubmit, Is.True);
        });
    }

    [Test]
    public void CreatesStableMessageNodesAriaRelationshipsAndFirstInvalidFocus()
    {
        FormCore forms = StandardForms();
        FormValidationFlowCore flow = new FormValidationFlowCore(forms);
        flow.MergeFormValidationIssues("sample", "alpha", new UiValidationIssue[]
        {
            Issue("required", "required", "A value is required", UiValidationSeverity.Error, "local", null),
            Issue("hint", "hint", "Check the format", UiValidationSeverity.Warning, "local", null)
        });
        UiNode node = InputNode("alpha-node", "alpha");
        node.Input!.HelperText = "Existing helper";
        node.Accessibility = new UiAccessibility
        {
            Relationships = new UiAriaRelationship[]
            {
                new UiAriaRelationship { Name = AriaAttributes.DescribedBy, TargetIds = new string[] { "external-help" } }
            }
        };

        FormValidationPresentation first = flow.CreateFieldValidationPresentation("sample", "alpha", node);
        DomNode rendered = new SemanticExpander().ExpandNode(node);
        DomNode input = rendered.Children[0];
        FormValidationPresentation second = flow.CreateFieldValidationPresentation("sample", "alpha", node);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(first.Messages.Select(item => item.Severity), Is.EqualTo(new UiValidationSeverity[] { UiValidationSeverity.Error, UiValidationSeverity.Warning }));
            Assert.That(first.Messages.Select(item => item.Node!.Id), Is.EqualTo(first.DescribedByIds));
            Assert.That(first.ErrorMessageIds, Has.Length.EqualTo(1));
            Assert.That(first.Messages[0].Node!.Children.Single().Text, Is.EqualTo("A value is required"));
            Assert.That(AttributeTokens(input, AriaAttributes.DescribedBy), Does.Contain("external-help"));
            Assert.That(AttributeTokens(input, AriaAttributes.DescribedBy), Does.Contain(first.DescribedByIds[0]));
            Assert.That(AttributeTokens(input, AriaAttributes.DescribedBy), Does.Contain(first.DescribedByIds[1]));
            Assert.That(AttributeTokens(input, AriaAttributes.ErrorMessage), Is.EqualTo(first.ErrorMessageIds));
            Assert.That(AttributeValue(input, AriaAttributes.Invalid), Is.EqualTo("true"));
            Assert.That(first.FocusRequest!.TargetId, Is.EqualTo(input.Id));
            Assert.That(first.FocusRequest.Reveal, Is.True);
            Assert.That(second.DescribedByIds, Is.EqualTo(first.DescribedByIds));
        });
    }

    [Test]
    public void SameIssueIdFromDifferentSourcesCreatesUniqueStableAriaNodesAndKeys()
    {
        FormCore forms = StandardForms();
        FormValidationFlowCore flow = new FormValidationFlowCore(forms);
        flow.MergeFormValidationIssues("sample", "alpha", new UiValidationIssue[]
        {
            Issue("shared", "host.shared", "Host message", UiValidationSeverity.Error, "host", null),
            Issue("shared", "server.shared", "Server message", UiValidationSeverity.Error, "server", null)
        });
        UiNode node = InputNode("alpha-node", "alpha");

        FormValidationPresentation first = flow.CreateFieldValidationPresentation("sample", "alpha", node);
        FormValidationPresentation second = flow.CreateFieldValidationPresentation("sample", "alpha", node);

        Assert.Multiple(() =>
        {
            Assert.That(first.Messages.Select(item => item.IssueId), Is.EqualTo(new string[] { "shared", "shared" }));
            Assert.That(first.DescribedByIds, Is.Unique);
            Assert.That(first.ErrorMessageIds, Is.EqualTo(first.DescribedByIds));
            Assert.That(first.Messages.Select(item => item.Node!.Key), Is.EqualTo(first.DescribedByIds));
            Assert.That(first.Messages.Select(item => item.Node!.Children.Single().Key), Is.EqualTo(first.DescribedByIds.Select(item => item + "-text")));
            Assert.That(second.DescribedByIds, Is.EqualTo(first.DescribedByIds));
            Assert.That(node.Accessibility!.Relationships.Single(item => item.Name == AriaAttributes.DescribedBy).TargetIds, Is.EqualTo(first.DescribedByIds));
        });
    }

    [Test]
    public void RemovesValidationAriaReferencesWhenIssuesClearAndPreservesExternalOnes()
    {
        FormCore forms = StandardForms();
        FormValidationFlowCore flow = new FormValidationFlowCore(forms);
        flow.MergeFormValidationIssues("sample", "alpha", new UiValidationIssue[]
        {
            Issue("required", "required", "Required", UiValidationSeverity.Error, "local", null)
        });
        UiNode node = InputNode("alpha-node", "alpha");
        node.Accessibility = new UiAccessibility
        {
            Relationships = new UiAriaRelationship[]
            {
                new UiAriaRelationship { Name = AriaAttributes.DescribedBy, TargetIds = new string[] { "external-help" } }
            }
        };
        flow.CreateFieldValidationPresentation("sample", "alpha", node);
        flow.ClearFormValidationIssues("sample", "alpha", "local");

        FormValidationPresentation cleared = flow.CreateFieldValidationPresentation("sample", "alpha", node);
        DomNode rendered = new SemanticExpander().ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(cleared.Messages, Is.Empty);
            Assert.That(cleared.FocusRequest, Is.Null);
            Assert.That(AttributeValue(rendered, AriaAttributes.DescribedBy), Is.EqualTo("external-help"));
            Assert.That(AttributeValue(rendered, AriaAttributes.ErrorMessage), Is.Null);
            Assert.That(AttributeValue(rendered, AriaAttributes.Invalid), Is.Null);
        });
    }

    [Test]
    public void RejectsMalformedIssueOperationsAndPresentationInputsExceptionlessly()
    {
        FormCore forms = StandardForms();
        FormValidationFlowCore flow = new FormValidationFlowCore(forms);
        UiValidationIssue issue = Issue("issue", "code", "Message", UiValidationSeverity.Error, "local", null);
        UiValidationIssue wrongSource = Issue("issue", "code", "Message", UiValidationSeverity.Error, "host", null);
        UiValidationIssue wrongField = Issue("issue", "code", "Message", UiValidationSeverity.Error, "local", "zeta");

        Assert.Multiple(() =>
        {
            Assert.That(flow.MergeFormValidationIssues("sample", "missing", new UiValidationIssue[] { issue }).Code, Is.EqualTo("form.unknown-field"));
            Assert.That(flow.MergeFormValidationIssues("sample", "alpha", null).Code, Is.EqualTo("validation.null-issues"));
            Assert.That(flow.MergeFormValidationIssues("sample", "alpha", new UiValidationIssue[] { wrongField }).Code, Is.EqualTo("validation.issue-field-mismatch"));
            Assert.That(flow.ReplaceFormValidationIssues("sample", "alpha", "local", new UiValidationIssue[] { wrongSource }).Code, Is.EqualTo("validation.issue-source-mismatch"));
            Assert.That(flow.ClearFormValidationIssues("sample", "alpha", " ").Code, Is.EqualTo("validation.invalid-source"));
            Assert.That(flow.CreateFieldValidationPresentation("sample", "alpha", null).Code, Is.EqualTo("validation.invalid-field-node"));
            Assert.That(flow.CreateFieldValidationPresentation("sample", "missing", InputNode("node", "missing")).Code, Is.EqualTo("form.unknown-field"));
        });
    }

    private static FormCore StandardForms()
    {
        FormCore forms = new FormCore();
        forms.RegisterForm(new UiForm
        {
            Key = "sample",
            Fields = new UiField[] { Field("zeta", "z"), Field("alpha", "a") }
        });
        return forms;
    }

    private static UiField Field(string key, string value, params UiValidationRule[] rules)
    {
        return new UiField
        {
            Key = key,
            Name = key,
            Label = key,
            Value = Text(value),
            Input = new UiInput { Name = key, Value = Text(value) },
            Rules = rules
        };
    }

    private static UiValidationRule Rule(string key, string kind, UiValidationTrigger trigger)
    {
        return new UiValidationRule { Key = key, Kind = kind, Trigger = trigger, Message = key };
    }

    private static UiValidationRule CustomRule(string key, string kind, UiValidationTrigger trigger, string handlerId, bool returnsTask)
    {
        return new UiValidationRule
        {
            Key = key,
            Kind = kind,
            Trigger = trigger,
            HandlerId = handlerId,
            Parameters = new UiValueEntry[]
            {
                new UiValueEntry { Key = "returns-task", Value = new UiValue { Kind = UiValueKind.Boolean, BooleanValue = returnsTask } }
            }
        };
    }

    private static UiValidationIssue Issue(string id, string code, string message, UiValidationSeverity severity, string source, string? fieldKey)
    {
        return new UiValidationIssue
        {
            Id = id,
            Code = code,
            Message = message,
            Severity = severity,
            Source = source,
            FieldKey = fieldKey
        };
    }

    private static UiNode InputNode(string id, string name)
    {
        return new UiNode
        {
            Kind = UiNodeKind.Control,
            Name = "text-input",
            Identity = new UiNodeIdentity { AuthorKey = name, SiblingIdentity = name, StableId = id },
            Input = new UiInput { Kind = UiInputKind.Text, Name = name, Value = Text("value") }
        };
    }

    private static UiValue Text(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }

    private static string? AttributeValue(DomNode node, string name)
    {
        return node.Attributes.SingleOrDefault(item => item.Name == name)?.Value;
    }

    private static string[] AttributeTokens(DomNode node, string name)
    {
        string? value = AttributeValue(node, name);
        return value == null ? System.Array.Empty<string>() : value.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
    }
}
