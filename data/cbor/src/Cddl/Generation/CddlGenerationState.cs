namespace cbor;

internal class CddlGenerationState
{
    internal CddlSchema Schema;
    internal CddlGenerationOptions Options;
    internal CddlGenerationResult Result = new CddlGenerationResult();
    internal CddlRule? SelectedRule;
    internal int RemainingSteps;
    internal int RemainingAttempts;
    internal int Depth;
    internal int ProductiveOnlyDepth;
    internal long RandomState;

    internal CddlGenerationState(
        CddlSchema schema,
        CddlGenerationOptions options)
    {
        Schema = schema;
        Options = CloneOptions(options);
        Result.Options = Options;
        RemainingSteps = Options.MaxSteps;
        RemainingAttempts = Options.MaxAttempts;
        long seed = Options.Seed;
        if (seed < 0)
        {
            seed = -seed;
        }
        RandomState = seed % 2147483646L + 1L;
    }

    internal bool InitializeGeneration()
    {
        int strategy = (int)Options.Strategy;
        if (strategy < (int)CddlGenerationStrategy.Minimal
            || strategy > (int)CddlGenerationStrategy.Representative)
        {
            FailGeneration(
                "CDDL_GENERATION_STRATEGY_INVALID",
                "The generation strategy is not defined.",
                null
            );
            return false;
        }
        if (Options.MaxNestingDepth < 0
            || Options.MaxCollectionItems < 0
            || Options.MaxSteps <= 0
            || Options.MaxAttempts <= 0)
        {
            FailGeneration(
                "CDDL_GENERATION_LIMIT_INVALID",
                "Generation limits must be nonnegative, with positive step and attempt budgets.",
                null
            );
            return false;
        }
        SelectedRule = SelectRule();
        if (SelectedRule == null)
        {
            FailGeneration(
                "CDDL_GENERATION_RULE_NOT_FOUND",
                "The selected CDDL generation rule does not exist.",
                null
            );
            return false;
        }
        Result.RuleName = SelectedRule.Name;
        if (SelectedRule.Kind != CddlRuleKind.Type
            || SelectedRule.Value == null)
        {
            FailGeneration(
                "CDDL_GENERATION_RULE_INVALID",
                "The selected CDDL generation rule is not a complete type rule.",
                SelectedRule.Value
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

    internal bool TakeStep(CddlNode? node)
    {
        if (RemainingSteps <= 0)
        {
            FailGeneration(
                "CDDL_GENERATION_STEP_LIMIT",
                "The configured generation step limit was exceeded.",
                node
            );
            return false;
        }
        RemainingSteps = RemainingSteps - 1;
        return true;
    }

    internal bool TakeAttempt(CddlNode? node)
    {
        if (!HasAttemptBudget(node))
        {
            return false;
        }
        RemainingAttempts = RemainingAttempts - 1;
        return true;
    }

    internal bool HasAttemptBudget(CddlNode? node)
    {
        if (RemainingAttempts <= 0)
        {
            FailGeneration(
                "CDDL_GENERATION_ATTEMPT_LIMIT",
                "The configured generation attempt limit was exceeded.",
                node
            );
            return false;
        }
        return true;
    }

    internal bool EnterNesting(CddlNode? node)
    {
        if (Depth >= Options.MaxNestingDepth)
        {
            FailGeneration(
                "CDDL_GENERATION_DEPTH_LIMIT",
                "The configured generation nesting limit was exceeded.",
                node
            );
            return false;
        }
        Depth = Depth + 1;
        return true;
    }

    internal void LeaveNesting()
    {
        if (Depth > 0)
        {
            Depth = Depth - 1;
        }
    }

    internal int NextIndex(int count)
    {
        if (count <= 1)
        {
            return 0;
        }
        RandomState = RandomState * 48271L % 2147483647L;
        return (int)(RandomState % count);
    }

    internal long NextRandomValue()
    {
        RandomState = RandomState * 48271L % 2147483647L;
        return RandomState;
    }

    internal void FailGeneration(
        string code,
        string message,
        CddlNode? node)
    {
        Result.IsSuccess = false;
        Result.Value = null;
        Result.ErrorCode = code;
        Result.ErrorMessage = message;
        Result.Span = FindSpan(node);
    }

    internal void ClearFailure()
    {
        Result.ErrorCode = null;
        Result.ErrorMessage = null;
        Result.Span = new CddlSourceSpan();
    }

    internal void CompleteSuccess(CborValue value)
    {
        Result.IsSuccess = true;
        Result.Value = value;
        Result.ErrorCode = null;
        Result.ErrorMessage = null;
        Result.Span = new CddlSourceSpan();
        Result.ValidationIssues = [];
    }

    internal void CompleteValidatedValue(CborValue value)
    {
        CddlValidationResult validation = new CddlValidator().ValidateCbor(
            Schema,
            value,
            CreateFinalValidationOptions()
        );
        Result.ValidationIssues = validation.Issues;
        if (validation.ControlResults.Length > 0)
        {
            Result.ControlResult = validation.ControlResults[
                validation.ControlResults.Length - 1
            ];
        }
        if (validation.IsValid)
        {
            CompleteSuccess(value);
            return;
        }
        FailFinalValidation(validation);
    }

    internal CddlValidationOptions CreateFinalValidationOptions()
    {
        CddlValidationOptions options = new CddlValidationOptions();
        options.RootRuleName = SelectedRule == null
            ? Options.RootRuleName
            : SelectedRule.Name;
        options.EnabledFeatures = (string[])
            Options.EnabledFeatures.Clone();
        options.CollectIssues = true;
        options.MaxIssues = 100;
        if (Options.MaxNestingDepth > int.MaxValue - 8)
        {
            options.MaxNestingDepth = int.MaxValue;
        }
        else
        {
            options.MaxNestingDepth = Options.MaxNestingDepth + 8;
        }
        options.MaxSteps = Options.MaxSteps;
        options.MaxAlternatives = Options.MaxAttempts;
        options.MaxMemoEntries = Options.MaxSteps;
        options.MaxEmbeddedBytes = Options.MaxCollectionItems;
        options.MaxPatternStates = Options.MaxSteps;
        return options;
    }

    internal void FailFinalValidation(CddlValidationResult validation)
    {
        Result.IsSuccess = false;
        Result.Value = null;
        Result.ErrorCode = "CDDL_GENERATION_VALUE_INVALID";
        Result.ErrorMessage =
            "The generated value does not satisfy the selected CDDL rule.";
        Result.Span = FindSpan(
            SelectedRule == null ? null : SelectedRule.Value
        );
        if (validation.Issues.Length > 0)
        {
            CddlValidationIssue issue = validation.Issues[0];
            Result.ErrorMessage = issue.Message;
            Result.Span = CloneSpan(issue.Span);
            if (issue.Code == CddlValidationCode.ResourceLimit)
            {
                Result.ErrorCode = "CDDL_GENERATION_VALIDATION_LIMIT";
            }
        }
    }

    internal CddlSourceSpan FindSpan(CddlNode? node)
    {
        if (node != null)
        {
            return CloneSpan(node.Span);
        }
        if (SelectedRule != null)
        {
            return CloneSpan(SelectedRule.Span);
        }
        return new CddlSourceSpan();
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

    internal CddlGenerationOptions CloneOptions(
        CddlGenerationOptions source)
    {
        CddlGenerationOptions options = new CddlGenerationOptions();
        options.RootRuleName = source.RootRuleName;
        options.Strategy = source.Strategy;
        options.Seed = source.Seed;
        options.EnabledFeatures = source.EnabledFeatures == null
            ? []
            : (string[])source.EnabledFeatures.Clone();
        options.Hook = source.Hook;
        options.MaxNestingDepth = source.MaxNestingDepth;
        options.MaxCollectionItems = source.MaxCollectionItems;
        options.MaxSteps = source.MaxSteps;
        options.MaxAttempts = source.MaxAttempts;
        return options;
    }
}
