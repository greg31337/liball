using System.Collections.Generic;

namespace cbor;

internal class CddlValidationState
{
    internal CddlSchema Schema;
    internal CborValue RootValue;
    internal CddlValidationOptions Options;
    internal CddlValidationResult Result = new CddlValidationResult();
    internal CddlRule? SelectedRule;
    internal CddlValidationBudget Budget;
    internal CddlFeatureSet Features;
    internal CddlControlLookup Controls;
    internal CddlValidationRuleStack RuleStack =
        new CddlValidationRuleStack();
    internal CddlInstancePath InstancePath = new CddlInstancePath();
    internal CddlSchemaPath SchemaPath = new CddlSchemaPath();
    internal CddlValidationMemoTable Memo = new CddlValidationMemoTable();
    internal CddlValidationDiagnostics Diagnostics;
    internal List<CddlControlResult> ControlResults =
        new List<CddlControlResult>();
    internal List<string> ReportedLimits = new List<string>();
    internal List<CborValue> InstanceValues = new List<CborValue>();
    internal List<CddlJsonNumber> JsonNumbers = new List<CddlJsonNumber>();
    internal bool JsonNumberMode;

    internal CddlValidationState(
        CddlSchema schema,
        CborValue value,
        CddlValidationOptions options)
    {
        Schema = schema;
        RootValue = value;
        Options = CloneOptions(options);
        Budget = new CddlValidationBudget(Options);
        Features = new CddlFeatureSet(Options.EnabledFeatures);
        Controls = new CddlControlLookup(schema.ControlRegistrations);
        Diagnostics = new CddlValidationDiagnostics(
            Options.MaxIssues,
            Options.CollectIssues,
            Options.MaxNestingDepth
        );
        Result.Options = Options;
    }

    internal bool InitializeValidation()
    {
        if (Controls.ErrorMessage != null)
        {
            AddInvalidSchemaIssue(Controls.ErrorMessage);
            return false;
        }
        int unknownPolicy = (int)Options.UnknownControlPolicy;
        if (unknownPolicy < (int)CddlUnknownControlPolicy.Reject
            || unknownPolicy > (int)CddlUnknownControlPolicy.Ignore)
        {
            AddInvalidSchemaIssue(
                "The unknown-control validation policy is not defined."
            );
            return false;
        }
        SelectedRule = SelectRule();
        if (SelectedRule == null)
        {
            AddInvalidSchemaIssue(
                "The selected CDDL validation rule does not exist."
            );
            return false;
        }
        Result.RuleName = SelectedRule.Name;
        if (SelectedRule.Kind != CddlRuleKind.Type
            || SelectedRule.Value == null)
        {
            AddInvalidSchemaIssue(
                "The selected CDDL validation rule is not a complete type rule."
            );
            return false;
        }
        return true;
    }

    internal CddlRule? SelectRule()
    {
        if (Options.RootRuleName != null)
        {
            return FindRule(Options.RootRuleName);
        }
        return Schema.RootRule;
    }

    internal CddlRule? FindRule(string name)
    {
        int index = 0;
        while (index < Schema.Rules.Length)
        {
            if (Schema.Rules[index].Name == name)
            {
                return Schema.Rules[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal CddlRule? FindReferencedRule(CddlNode node)
    {
        if (node.ReferencedRuleIndex >= 0
            && node.ReferencedRuleIndex < Schema.Rules.Length)
        {
            return Schema.Rules[node.ReferencedRuleIndex];
        }
        if (node.Name != null)
        {
            return FindRule(node.Name);
        }
        return null;
    }

    internal bool EnterRule(CddlRule rule)
    {
        if (!Budget.EnterNesting())
        {
            AddResourceLimitIssue("nesting depth", rule.Value);
            return false;
        }
        RuleStack.PushRule(rule);
        SchemaPath.PushRuleName(rule.Name);
        return true;
    }

    internal void LeaveRule()
    {
        if (RuleStack.PeekRule() != null)
        {
            RuleStack.PopRule();
            SchemaPath.PopSegment();
            Budget.LeaveNesting();
        }
    }

    internal bool EnterDataNesting(CddlNode? node)
    {
        if (!Budget.EnterNesting())
        {
            AddResourceLimitIssue("nesting depth", node);
            return false;
        }
        return true;
    }

    internal void LeaveDataNesting()
    {
        Budget.LeaveNesting();
    }

    internal bool TakeStep(CddlNode? node)
    {
        if (!Budget.TakeStep())
        {
            AddResourceLimitIssue("validation steps", node);
            return false;
        }
        return true;
    }

    internal bool TakeAlternative(CddlNode? node)
    {
        if (!Budget.TakeAlternative())
        {
            AddResourceLimitIssue("alternatives", node);
            return false;
        }
        return true;
    }

    internal bool TakeEmbeddedBytes(int count, CddlNode? node)
    {
        if (!Budget.TakeEmbeddedBytes(count))
        {
            AddResourceLimitIssue("embedded bytes", node);
            return false;
        }
        return true;
    }

    internal bool TakePatternStates(int count, CddlNode? node)
    {
        if (!Budget.TakePatternStates(count))
        {
            AddResourceLimitIssue("pattern states", node);
            return false;
        }
        return true;
    }

    internal CddlValidationMemoEntry? BeginMemo(
        CddlRule rule,
        CborValue value,
        int instancePosition,
        int context,
        CddlNode? node)
    {
        CddlValidationMemoEntry? existing = Memo.FindEntry(
            rule,
            value,
            instancePosition,
            context
        );
        if (existing != null)
        {
            return existing;
        }
        if (!Budget.TakeMemoEntry())
        {
            AddResourceLimitIssue("memo entries", node);
            return null;
        }
        return Memo.CreateEntry(rule, value, instancePosition, context);
    }

    internal CddlValidationMemoEntry? BeginMapMemo(
        CddlRule rule,
        CborValue value,
        bool[] usedEntries,
        CddlControlResult[] controlResults,
        int context,
        CddlNode? node)
    {
        CddlValidationMemoEntry? existing = Memo.FindMapEntry(
            rule,
            value,
            usedEntries,
            controlResults,
            context
        );
        if (existing != null)
        {
            return existing;
        }
        if (!Budget.TakeMemoEntry())
        {
            AddResourceLimitIssue("memo entries", node);
            return null;
        }
        return Memo.CreateMapEntry(
            rule,
            value,
            usedEntries,
            controlResults,
            context
        );
    }

    internal CddlValidationIssue CreateIssue(
        CddlValidationCode code,
        string message,
        CddlNode? node,
        string? expected,
        string? actual)
    {
        CddlValidationIssue issue = new CddlValidationIssue();
        issue.Code = code;
        issue.Message = message;
        issue.InstancePath = InstancePath.CreatePath();
        issue.ByteOffset = FindInstanceOffset();
        issue.SchemaPath = SchemaPath.CreatePath();
        issue.RuleName = CurrentRuleName();
        issue.Expected = expected;
        issue.Actual = actual;
        issue.Span = FindSpan(node);
        return issue;
    }

    internal void PushInstanceValue(CborValue value)
    {
        InstanceValues.Add(value);
    }

    internal void PopInstanceValue()
    {
        if (InstanceValues.Count > 0)
        {
            InstanceValues.RemoveAt(InstanceValues.Count - 1);
        }
    }

    internal void AddJsonNumber(
        CborValue value,
        System.Numerics.BigInteger coefficient,
        int scale)
    {
        JsonNumbers.Add(new CddlJsonNumber(value, coefficient, scale));
    }

    internal CddlJsonNumber? FindJsonNumber(CborValue value)
    {
        int index = 0;
        while (index < JsonNumbers.Count)
        {
            if (JsonNumbers[index].Value == value)
            {
                return JsonNumbers[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal long FindInstanceOffset()
    {
        if (InstanceValues.Count == 0)
        {
            return -1;
        }
        CborValue value = InstanceValues[InstanceValues.Count - 1];
        if (value.Representation == null
            || !value.Representation.HasSourceRange)
        {
            return -1;
        }
        return value.Representation.SourceOffset;
    }

    internal void AddIssue(CddlValidationIssue issue)
    {
        Diagnostics.AddIssue(issue);
    }

    internal void AddControlResult(CddlControlResult controlResult)
    {
        if (controlResult.FeatureName != null)
        {
            int index = 0;
            while (index < ControlResults.Count)
            {
                CddlControlResult existing = ControlResults[index];
                if (existing.FeatureName == controlResult.FeatureName
                    && CborValueComparer.AreEqual(
                        existing.FeatureDetail,
                        controlResult.FeatureDetail))
                {
                    return;
                }
                index = index + 1;
            }
        }
        ControlResults.Add(controlResult);
    }

    internal int CreateControlResultCheckpoint()
    {
        return ControlResults.Count;
    }

    internal CddlControlResult[] CreateControlResultSnapshot()
    {
        return ControlResults.ToArray();
    }

    internal void RestoreControlResultSnapshot(CddlControlResult[] snapshot)
    {
        ControlResults.Clear();
        int index = 0;
        while (index < snapshot.Length)
        {
            ControlResults.Add(snapshot[index]);
            index = index + 1;
        }
    }

    internal void RestoreControlResults(int checkpoint)
    {
        while (ControlResults.Count > checkpoint)
        {
            ControlResults.RemoveAt(ControlResults.Count - 1);
        }
    }

    internal CddlValidationResult CompleteValidation(bool matched)
    {
        Result.IsValid = matched && !Diagnostics.HasErrors;
        Result.Issues = Diagnostics.CreateIssueArray();
        Result.ControlResults = ControlResults.ToArray();
        return Result;
    }

    internal void AddInvalidSchemaIssue(string message)
    {
        CddlValidationIssue issue = CreateIssue(
            CddlValidationCode.InvalidSchema,
            message,
            null,
            "a compiled type rule",
            Options.RootRuleName
        );
        Diagnostics.AddIssue(issue);
    }

    internal void AddResourceLimitIssue(string limitName, CddlNode? node)
    {
        if (ContainsString(ReportedLimits, limitName))
        {
            return;
        }
        ReportedLimits.Add(limitName);
        CddlValidationIssue issue = CreateIssue(
            CddlValidationCode.ResourceLimit,
            "The configured " + limitName + " limit was exceeded.",
            node,
            "within the configured " + limitName + " budget",
            "limit exceeded"
        );
        Diagnostics.AddIssue(issue);
    }

    internal string? CurrentRuleName()
    {
        CddlRule? current = RuleStack.PeekRule();
        if (current != null)
        {
            return current.Name;
        }
        if (SelectedRule != null)
        {
            return SelectedRule.Name;
        }
        return Options.RootRuleName;
    }

    internal CddlSourceSpan FindSpan(CddlNode? node)
    {
        if (node != null)
        {
            return CloneSpan(node.Span);
        }
        CddlRule? current = RuleStack.PeekRule();
        if (current != null)
        {
            return CloneSpan(current.Span);
        }
        if (SelectedRule != null)
        {
            return CloneSpan(SelectedRule.Span);
        }
        return new CddlSourceSpan();
    }

    internal bool ContainsString(List<string> values, string value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == value)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal CddlSourceSpan CloneSpan(CddlSourceSpan source)
    {
        CddlSourceSpan span = new CddlSourceSpan();
        span.SourceName = source.SourceName;
        span.Start = source.Start;
        span.Length = source.Length;
        span.StartLine = source.StartLine;
        span.StartColumn = source.StartColumn;
        span.EndLine = source.EndLine;
        span.EndColumn = source.EndColumn;
        return span;
    }

    internal CddlValidationOptions CloneOptions(CddlValidationOptions source)
    {
        CddlValidationOptions options = new CddlValidationOptions();
        options.RootRuleName = source.RootRuleName;
        options.EnabledFeatures = source.EnabledFeatures == null
            ? []
            : (string[])source.EnabledFeatures.Clone();
        options.UnknownControlPolicy = source.UnknownControlPolicy;
        options.CollectIssues = source.CollectIssues;
        options.MaxIssues = source.MaxIssues;
        options.MaxNestingDepth = source.MaxNestingDepth;
        options.MaxSteps = source.MaxSteps;
        options.MaxAlternatives = source.MaxAlternatives;
        options.MaxMemoEntries = source.MaxMemoEntries;
        options.MaxEmbeddedBytes = source.MaxEmbeddedBytes;
        options.MaxPatternStates = source.MaxPatternStates;
        return options;
    }
}
