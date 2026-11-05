using System;
using System.Collections.Generic;
using System.Numerics;

namespace cbor;

internal class CddlValueGeneratorCore
{
    internal CddlGenerationState State;
    internal List<CddlRule> ActiveRules = new List<CddlRule>();
    internal CddlGenerationProductivity Productivity;
    internal string HookValidationRuleName = "__generated_hook";

    internal CddlValueGeneratorCore(CddlGenerationState state)
    {
        State = state;
        Productivity = new CddlGenerationProductivity(state);
    }

    internal CborValue? GenerateSelectedValue()
    {
        CddlRule? rule = State.SelectedRule;
        if (rule == null || rule.Value == null)
        {
            State.FailGeneration(
                "CDDL_GENERATION_RULE_INVALID",
                "The selected rule has no generation expression.",
                null
            );
            return null;
        }
        ActiveRules.Add(rule);
        CborValue? supplied = GenerateRuleHookValue(rule);
        if (supplied != null || State.Result.ErrorCode != null)
        {
            ActiveRules.RemoveAt(ActiveRules.Count - 1);
            return supplied;
        }
        if (!Productivity.CanGenerateRuleValue(rule))
        {
            ActiveRules.RemoveAt(ActiveRules.Count - 1);
            State.FailGeneration(
                "CDDL_GENERATION_NO_FINITE_VALUE",
                "The selected recursive schema cannot produce a finite value.",
                rule.Value
            );
            return null;
        }
        CborValue? value = GenerateNodeValue(rule.Value);
        ActiveRules.RemoveAt(ActiveRules.Count - 1);
        return value;
    }

    internal CborValue? GenerateNodeValue(CddlNode node)
    {
        if (!State.TakeStep(node))
        {
            return null;
        }
        switch (node.Kind)
        {
            case CddlNodeKind.Literal:
                return GenerateLiteralValue(node);
            case CddlNodeKind.TypeChoice:
                return GenerateChoiceValue(node);
            case CddlNodeKind.RuleReference:
                return GenerateReferenceValue(node);
            case CddlNodeKind.Range:
                return GenerateRangeValue(node);
            case CddlNodeKind.Array:
                return new CddlCollectionGenerator(this).GenerateArrayValue(
                    node
                );
            case CddlNodeKind.Map:
                return new CddlCollectionGenerator(this).GenerateMapValue(
                    node
                );
            case CddlNodeKind.GroupEnumeration:
                return new CddlCollectionGenerator(
                    this
                ).GenerateGroupEnumerationValue(node);
            case CddlNodeKind.Control:
                return new CddlControlGenerator(this).GenerateControlValue(
                    node
                );
            case CddlNodeKind.Tag:
                return GenerateTagValue(node);
            case CddlNodeKind.Representation:
                return GenerateRepresentationValue(node);
            case CddlNodeKind.ParenthesizedType:
                if (node.Children.Count == 1)
                {
                    return GenerateNodeValue(node.Children[0]);
                }
                break;
        }
        State.FailGeneration(
            "CDDL_GENERATION_NODE_UNSUPPORTED",
            "The schema node is not supported by scalar generation.",
            node
        );
        return null;
    }

    internal CborValue? GenerateLiteralValue(CddlNode node)
    {
        if (node.HasIntegerValue)
        {
            return CreateIntegerValue(node.IntegerValue);
        }
        if (node.HasFloatValue)
        {
            return CreateFloatValue(node.FloatValue, CborFloatWidth.Double);
        }
        if (node.LiteralKind == CddlLiteralKind.TextString)
        {
            CborValue text = new CborValue();
            text.Kind = CborValueKind.TextString;
            text.TextStringValue = node.TextValue ?? "";
            text.LengthKind = CborLengthKind.Definite;
            return text;
        }
        if (node.LiteralKind == CddlLiteralKind.Utf8ByteString
            || node.LiteralKind == CddlLiteralKind.HexByteString
            || node.LiteralKind == CddlLiteralKind.Base64UrlByteString)
        {
            byte[] source = node.ByteStringValue ?? [];
            byte[] bytes = new byte[source.Length];
            int index = 0;
            while (index < source.Length)
            {
                bytes[index] = source[index];
                index = index + 1;
            }
            CborValue value = new CborValue();
            value.Kind = CborValueKind.ByteString;
            value.ByteStringValue = bytes;
            value.LengthKind = CborLengthKind.Definite;
            return value;
        }
        State.FailGeneration(
            "CDDL_GENERATION_LITERAL_INVALID",
            "The CDDL literal has no generatable value.",
            node
        );
        return null;
    }

    internal CborValue? GenerateChoiceValue(CddlNode node)
    {
        if (node.Children.Count == 0)
        {
            State.FailGeneration(
                "CDDL_GENERATION_CHOICE_EMPTY",
                "The CDDL type choice has no alternatives.",
                node
            );
            return null;
        }
        if (State.ProductiveOnlyDepth > 0
            || State.Options.Strategy == CddlGenerationStrategy.Minimal)
        {
            return GenerateProductiveChoiceValue(node);
        }
        int start = SelectChoiceStart(node.Children.Count);
        int attempt = 0;
        while (attempt < node.Children.Count)
        {
            if (!State.TakeAttempt(node))
            {
                return null;
            }
            int index = (start + attempt) % node.Children.Count;
            State.ClearFailure();
            CborValue? value = GenerateNodeValue(node.Children[index]);
            if (value != null)
            {
                return value;
            }
            if (State.RemainingSteps <= 0
                || (attempt + 1 < node.Children.Count
                    && !State.HasAttemptBudget(node)))
            {
                return null;
            }
            attempt = attempt + 1;
        }
        State.FailGeneration(
            "CDDL_GENERATION_CHOICE_FAILED",
            "No CDDL type-choice alternative could generate a scalar value.",
            node
        );
        return null;
    }

    internal CborValue? GenerateProductiveChoiceValue(CddlNode node)
    {
        List<int> candidates = new List<int>();
        int index = 0;
        while (index < node.Children.Count)
        {
            if (Productivity.CanGenerateNodeWithoutRules(
                node.Children[index],
                ActiveRules
            ))
            {
                candidates.Add(index);
            }
            index = index + 1;
        }
        if (candidates.Count == 0)
        {
            State.FailGeneration(
                "CDDL_GENERATION_NO_FINITE_VALUE",
                "No finite base-case alternative is available.",
                node
            );
            return null;
        }
        int start = SelectChoiceStart(candidates.Count);
        int attempt = 0;
        while (attempt < candidates.Count)
        {
            if (!State.TakeAttempt(node))
            {
                return null;
            }
            int candidate = (start + attempt) % candidates.Count;
            State.ClearFailure();
            CborValue? value = GenerateNodeValue(
                node.Children[candidates[candidate]]
            );
            if (value != null)
            {
                return value;
            }
            if (State.RemainingSteps <= 0
                || (attempt + 1 < candidates.Count
                    && !State.HasAttemptBudget(node)))
            {
                return null;
            }
            attempt = attempt + 1;
        }
        State.FailGeneration(
            "CDDL_GENERATION_NO_FINITE_VALUE",
            "No finite base-case alternative could be generated.",
            node
        );
        return null;
    }

    internal int SelectChoiceStart(int count)
    {
        if (State.Options.Strategy == CddlGenerationStrategy.Minimal)
        {
            return 0;
        }
        if (State.Options.Strategy == CddlGenerationStrategy.Boundary)
        {
            return State.NextIndex(2) == 0 ? 0 : count - 1;
        }
        return State.NextIndex(count);
    }

    internal CborValue? GenerateReferenceValue(CddlNode node)
    {
        CddlRule? rule = State.FindReferencedRule(node);
        if (rule == null || rule.Value == null
            || rule.Kind != CddlRuleKind.Type)
        {
            State.FailGeneration(
                "CDDL_GENERATION_REFERENCE_INVALID",
                "The referenced CDDL type rule is unavailable.",
                node
            );
            return null;
        }
        CborValue? supplied = GenerateRuleHookValue(rule);
        if (supplied != null || State.Result.ErrorCode != null)
        {
            return supplied;
        }
        bool recursive = ContainsActiveRule(rule);
        if (recursive && !Productivity.CanGenerateRuleValue(rule))
        {
            State.FailGeneration(
                "CDDL_GENERATION_NO_FINITE_VALUE",
                "The recursive rule cannot produce a finite value.",
                node
            );
            return null;
        }
        bool productiveOnly = recursive
            && ShouldForceProductiveRule(rule);
        if (productiveOnly)
        {
            State.ProductiveOnlyDepth = State.ProductiveOnlyDepth + 1;
        }
        ActiveRules.Add(rule);
        CborValue? value = GenerateNodeValue(rule.Value);
        ActiveRules.RemoveAt(ActiveRules.Count - 1);
        if (productiveOnly)
        {
            State.ProductiveOnlyDepth = State.ProductiveOnlyDepth - 1;
        }
        return value;
    }

    internal bool ContainsActiveRule(CddlRule rule)
    {
        int index = 0;
        while (index < ActiveRules.Count)
        {
            if (ActiveRules[index] == rule)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal int CountActiveRule(CddlRule rule)
    {
        int count = 0;
        int index = 0;
        while (index < ActiveRules.Count)
        {
            if (ActiveRules[index] == rule)
            {
                count = count + 1;
            }
            index = index + 1;
        }
        return count;
    }

    internal bool ShouldForceProductiveRule(CddlRule rule)
    {
        if (State.ProductiveOnlyDepth > 0
            || State.Depth >= State.Options.MaxNestingDepth)
        {
            return true;
        }
        int recursionLimit = State.Options.MaxNestingDepth;
        if (recursionLimit < 1)
        {
            recursionLimit = 1;
        }
        if (CountActiveRule(rule) >= recursionLimit)
        {
            return true;
        }
        int stepHeadroom = ActiveRules.Count * 4 + 8;
        return State.RemainingSteps <= stepHeadroom;
    }

    internal CborValue? GenerateRangeValue(CddlNode node)
    {
        if (node.Children.Count != 2)
        {
            State.FailGeneration(
                "CDDL_GENERATION_RANGE_INVALID",
                "A numeric range requires two singleton endpoints.",
                node
            );
            return null;
        }
        CddlNumericValue lower = EvaluateNumericEndpoint(
            node.Children[0],
            new List<CddlRule>()
        );
        CddlNumericValue upper = EvaluateNumericEndpoint(
            node.Children[1],
            new List<CddlRule>()
        );
        if (lower.Kind == CddlNumericValue.Integer
            && upper.Kind == CddlNumericValue.Integer)
        {
            BigInteger highest = node.IsRangeUpperExclusive
                ? upper.IntegerValue - BigInteger.One
                : upper.IntegerValue;
            if (lower.IntegerValue > highest)
            {
                State.FailGeneration(
                    "CDDL_GENERATION_RANGE_EMPTY",
                    "The integer range contains no value.",
                    node
                );
                return null;
            }
            BigInteger integer = SelectIntegerRangeValue(
                lower.IntegerValue,
                highest
            );
            return CreateIntegerValue(integer);
        }
        if (lower.Kind == CddlNumericValue.Float
            && upper.Kind == CddlNumericValue.Float)
        {
            double highest = node.IsRangeUpperExclusive
                ? Math.BitDecrement(upper.FloatValue)
                : upper.FloatValue;
            if (double.IsNaN(lower.FloatValue)
                || double.IsNaN(highest)
                || lower.FloatValue > highest)
            {
                State.FailGeneration(
                    "CDDL_GENERATION_RANGE_EMPTY",
                    "The floating-point range contains no value.",
                    node
                );
                return null;
            }
            double floating = SelectFloatRangeValue(
                lower.FloatValue,
                highest
            );
            return CreateFloatValue(floating, CborFloatWidth.Double);
        }
        State.FailGeneration(
            "CDDL_GENERATION_RANGE_INVALID",
            "Range endpoints must be numeric singletons of the same kind.",
            node
        );
        return null;
    }

    internal CddlNumericValue EvaluateNumericEndpoint(
        CddlNode node,
        List<CddlRule> visited)
    {
        CddlNumericValue value = new CddlNumericValue();
        if (node.Kind == CddlNodeKind.Literal && node.HasIntegerValue)
        {
            value.Kind = CddlNumericValue.Integer;
            value.IntegerValue = node.IntegerValue;
            return value;
        }
        if (node.Kind == CddlNodeKind.Literal && node.HasFloatValue)
        {
            value.Kind = CddlNumericValue.Float;
            value.FloatValue = node.FloatValue;
            return value;
        }
        if (node.Kind == CddlNodeKind.Representation
            && node.HasArgumentValue
            && node.ArgumentValue >= BigInteger.Zero
            && node.ArgumentValue <= new BigInteger(23))
        {
            if (node.MajorType == 0)
            {
                value.Kind = CddlNumericValue.Integer;
                value.IntegerValue = node.ArgumentValue;
            }
            else if (node.MajorType == 1)
            {
                value.Kind = CddlNumericValue.Integer;
                value.IntegerValue = -BigInteger.One - node.ArgumentValue;
            }
            return value;
        }
        if ((node.Kind == CddlNodeKind.ParenthesizedType
                || node.Kind == CddlNodeKind.TypeChoice)
            && node.Children.Count == 1)
        {
            return EvaluateNumericEndpoint(node.Children[0], visited);
        }
        if (node.Kind == CddlNodeKind.RuleReference)
        {
            CddlRule? rule = State.FindReferencedRule(node);
            if (rule != null && rule.Value != null
                && !ContainsVisitedRule(visited, rule))
            {
                visited.Add(rule);
                CddlNumericValue resolved = EvaluateNumericEndpoint(
                    rule.Value,
                    visited
                );
                visited.RemoveAt(visited.Count - 1);
                return resolved;
            }
        }
        return value;
    }

    internal bool ContainsVisitedRule(
        List<CddlRule> visited,
        CddlRule rule)
    {
        int index = 0;
        while (index < visited.Count)
        {
            if (visited[index] == rule)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal BigInteger SelectIntegerRangeValue(
        BigInteger lower,
        BigInteger upper)
    {
        if (State.Options.Strategy == CddlGenerationStrategy.Minimal)
        {
            if (lower <= BigInteger.Zero && upper >= BigInteger.Zero)
            {
                return BigInteger.Zero;
            }
            return upper < BigInteger.Zero ? upper : lower;
        }
        if (State.Options.Strategy == CddlGenerationStrategy.Boundary)
        {
            return State.NextIndex(2) == 0 ? lower : upper;
        }
        BigInteger count = upper - lower + BigInteger.One;
        if (count <= new BigInteger(int.MaxValue))
        {
            return lower + State.NextIndex((int)count);
        }
        return lower + new BigInteger(State.NextRandomValue()) % count;
    }

    internal double SelectFloatRangeValue(double lower, double upper)
    {
        if (State.Options.Strategy == CddlGenerationStrategy.Minimal)
        {
            if (lower <= 0.0 && upper >= 0.0)
            {
                return 0.0;
            }
            return upper < 0.0 ? upper : lower;
        }
        if (State.Options.Strategy == CddlGenerationStrategy.Boundary)
        {
            return State.NextIndex(2) == 0 ? lower : upper;
        }
        if (double.IsInfinity(lower) || double.IsInfinity(upper))
        {
            if (lower <= 0.0 && upper >= 0.0)
            {
                return 0.0;
            }
            return double.IsNegativeInfinity(lower) ? upper : lower;
        }
        double fraction = State.NextRandomValue() / 2147483647.0;
        return lower + (upper - lower) * fraction;
    }

    internal CborValue? GenerateTagValue(CddlNode node)
    {
        CddlRule? currentRule = CurrentRule();
        if (currentRule == null || currentRule.Value != node)
        {
            CborValue? supplied = GenerateHookValue(
                node,
                currentRule
            );
            if (supplied != null || State.Result.ErrorCode != null)
            {
                return supplied;
            }
        }
        if (node.Children.Count == 0 || !State.EnterNesting(node))
        {
            if (node.Children.Count == 0)
            {
                State.FailGeneration(
                    "CDDL_GENERATION_TAG_INVALID",
                    "A semantic tag requires a content type.",
                    node
                );
            }
            return null;
        }
        BigInteger tagNumber;
        if (node.HasArgumentValue)
        {
            tagNumber = node.ArgumentValue;
        }
        else if (node.Children.Count == 2)
        {
            CborValue? generatedTag = GenerateNodeValue(node.Children[0]);
            if (generatedTag == null
                || generatedTag.Kind != CborValueKind.UnsignedInteger)
            {
                State.LeaveNesting();
                State.FailGeneration(
                    "CDDL_GENERATION_TAG_INVALID",
                    "The semantic tag-number type did not generate an unsigned integer.",
                    node
                );
                return null;
            }
            tagNumber = generatedTag.IntegerValue;
        }
        else
        {
            State.LeaveNesting();
            State.FailGeneration(
                "CDDL_GENERATION_TAG_INVALID",
                "The semantic tag number is unavailable.",
                node
            );
            return null;
        }
        if (tagNumber < BigInteger.Zero
            || tagNumber > new BigInteger(ulong.MaxValue))
        {
            State.LeaveNesting();
            State.FailGeneration(
                "CDDL_GENERATION_TAG_INVALID",
                "The semantic tag number is outside the CBOR tag range.",
                node
            );
            return null;
        }
        CborValue? content = GenerateNodeValue(
            node.Children[node.Children.Count - 1]
        );
        State.LeaveNesting();
        if (content == null)
        {
            return null;
        }
        CborValue tag = new CborValue();
        tag.Kind = CborValueKind.Tag;
        tag.TagNumber = (ulong)tagNumber;
        tag.TaggedValue = content;
        return tag;
    }

    internal CborValue? GenerateRuleHookValue(CddlRule rule)
    {
        if (rule.Value == null)
        {
            return null;
        }
        return GenerateHookValue(rule.Value, rule);
    }

    internal CborValue? GenerateHookValue(
        CddlNode node,
        CddlRule? rule)
    {
        ICddlGenerationHook? hook = State.Options.Hook;
        if (hook == null)
        {
            return null;
        }
        CddlGenerationContext context = CreateGenerationContext(
            node,
            rule
        );
        CddlControlResult? result;
        using (CddlGenerationHookScope scope =
            new CddlGenerationHookScope(
                "node generation",
                context
            ))
        {
            result = hook.GenerateNode(context);
            scope.CompleteInvocation();
        }
        return UseSuppliedValue(result, node);
    }

    internal CborValue? UseSuppliedValue(
        CddlControlResult? supplied,
        CddlNode node)
    {
        if (supplied == null
            || supplied.Status == CddlControlStatus.None
            || supplied.Status == CddlControlStatus.NotApplicable)
        {
            return null;
        }
        State.Result.ControlResult = supplied;
        if (supplied.Status == CddlControlStatus.Applied
            && supplied.Value != null)
        {
            if (SuppliedValueMatches(node, supplied.Value))
            {
                State.ClearFailure();
                return supplied.Value;
            }
            State.FailGeneration(
                "CDDL_GENERATION_HOOK_VALUE_INVALID",
                "The value supplied by the generation callback does not satisfy the intercepted schema node.",
                node
            );
            State.Result.ControlResult = supplied;
            return null;
        }
        string code = supplied.ErrorCode
            ?? (supplied.Status == CddlControlStatus.Applied
                ? "CDDL_GENERATION_HOOK_VALUE_MISSING"
                : "CDDL_GENERATION_HOOK_FAILED");
        string message = supplied.ErrorMessage
            ?? "The generation callback could not supply a value.";
        State.FailGeneration(code, message, node);
        State.Result.ControlResult = supplied;
        return null;
    }

    internal bool SuppliedValueMatches(
        CddlNode node,
        CborValue candidate)
    {
        CddlSchema schema = CreateHookValidationSchema(node);
        CddlValidationOptions options = new CddlValidationOptions();
        options.RootRuleName = HookValidationRuleName;
        options.EnabledFeatures = (string[])
            State.Options.EnabledFeatures.Clone();
        options.CollectIssues = false;
        options.MaxIssues = 1;
        options.MaxNestingDepth = State.Options.MaxNestingDepth + 8;
        options.MaxSteps = State.Options.MaxSteps;
        options.MaxAlternatives = State.Options.MaxAttempts;
        options.MaxMemoEntries = State.Options.MaxSteps;
        options.MaxEmbeddedBytes = State.Options.MaxCollectionItems;
        options.MaxPatternStates = State.Options.MaxSteps;
        return new CddlValidator().ValidateCbor(
            schema,
            candidate,
            options
        ).IsValid;
    }

    internal CddlSchema CreateHookValidationSchema(CddlNode node)
    {
        HookValidationRuleName = "__generated_hook";
        while (State.FindRule(HookValidationRuleName) != null)
        {
            HookValidationRuleName = HookValidationRuleName + "_";
        }
        CddlRule root = new CddlRule();
        root.Name = HookValidationRuleName;
        root.Kind = CddlRuleKind.Type;
        root.Value = node;
        root.Span = node.Span;
        CddlSchema schema = new CddlSchema();
        schema.Document = State.Schema.Document;
        schema.Rules = new CddlRule[State.Schema.Rules.Length + 1];
        int index = 0;
        while (index < State.Schema.Rules.Length)
        {
            schema.Rules[index] = State.Schema.Rules[index];
            index = index + 1;
        }
        schema.Rules[index] = root;
        schema.RootRule = root;
        schema.NormalizedNodes = State.Schema.NormalizedNodes;
        schema.SourceMap = State.Schema.SourceMap;
        schema.ControlRegistrations = State.Schema.ControlRegistrations;
        schema.Features = State.Schema.Features;
        return schema;
    }

    internal CddlGenerationContext CreateGenerationContext(
        CddlNode node,
        CddlRule? rule)
    {
        CddlGenerationContext context = new CddlGenerationContext();
        context.Schema = State.Schema;
        context.Rule = rule;
        context.Node = node;
        context.Options = State.CloneOptions(State.Options);
        context.Depth = State.Depth;
        context.RemainingSteps = State.RemainingSteps;
        context.RemainingAttempts = State.RemainingAttempts;
        context.Span = State.CloneSpan(node.Span);
        return context;
    }

    internal CddlRule? CurrentRule()
    {
        if (ActiveRules.Count == 0)
        {
            return State.SelectedRule;
        }
        return ActiveRules[ActiveRules.Count - 1];
    }

    internal CborValue? GenerateRepresentationValue(CddlNode node)
    {
        if (node.MajorType < 0)
        {
            return GenerateAnyScalar(node);
        }
        if (node.MajorType == 0)
        {
            return GenerateIntegerRepresentation(node, false);
        }
        if (node.MajorType == 1)
        {
            return GenerateIntegerRepresentation(node, true);
        }
        if (node.MajorType == 2 || node.MajorType == 3)
        {
            return GenerateStringRepresentation(node);
        }
        if (node.MajorType == 4)
        {
            return new CddlCollectionGenerator(
                this
            ).GenerateArrayRepresentationValue(node);
        }
        if (node.MajorType == 5)
        {
            return new CddlCollectionGenerator(
                this
            ).GenerateMapRepresentationValue(node);
        }
        if (node.MajorType == 6)
        {
            CborValue tag = new CborValue();
            tag.Kind = CborValueKind.Tag;
            tag.TagNumber = 0;
            tag.TaggedValue = CreateSimpleValue(CborSimpleValues.Null);
            return tag;
        }
        if (node.MajorType == 7)
        {
            return GenerateSimpleRepresentation(node);
        }
        State.FailGeneration(
            "CDDL_GENERATION_NODE_UNSUPPORTED",
            "Container representations require collection generation.",
            node
        );
        return null;
    }

    internal CborValue? GenerateAnyScalar(CddlNode node)
    {
        if (State.Options.Strategy == CddlGenerationStrategy.Minimal)
        {
            return CreateIntegerValue(BigInteger.Zero);
        }
        if (State.Options.Strategy == CddlGenerationStrategy.Boundary)
        {
            return CreateIntegerValue(new BigInteger(23));
        }
        int selected = State.NextIndex(7);
        if (selected == 0)
        {
            return GenerateUnsignedValue();
        }
        if (selected == 1)
        {
            return GenerateNegativeValue();
        }
        if (selected == 2)
        {
            return GenerateByteStringValue();
        }
        if (selected == 3)
        {
            return GenerateTextStringValue();
        }
        if (selected == 4)
        {
            return CreateSimpleValue(CborSimpleValues.False);
        }
        if (selected == 5)
        {
            return CreateSimpleValue(CborSimpleValues.Null);
        }
        return GenerateFloatValue(CborFloatWidth.Half);
    }

    internal CborValue? GenerateIntegerRepresentation(
        CddlNode node,
        bool negative)
    {
        if (!node.HasArgumentValue)
        {
            return negative ? GenerateNegativeValue() : GenerateUnsignedValue();
        }
        int additional = (int)node.ArgumentValue;
        BigInteger lower;
        BigInteger upper;
        if (additional >= 0 && additional <= 23)
        {
            lower = new BigInteger(additional);
            upper = lower;
        }
        else if (additional == 24)
        {
            lower = new BigInteger(24);
            upper = new BigInteger(byte.MaxValue);
        }
        else if (additional == 25)
        {
            lower = new BigInteger(byte.MaxValue) + BigInteger.One;
            upper = new BigInteger(ushort.MaxValue);
        }
        else if (additional == 26)
        {
            lower = new BigInteger(ushort.MaxValue) + BigInteger.One;
            upper = new BigInteger(uint.MaxValue);
        }
        else if (additional == 27)
        {
            lower = new BigInteger(uint.MaxValue) + BigInteger.One;
            upper = new BigInteger(ulong.MaxValue);
        }
        else
        {
            State.FailGeneration(
                "CDDL_GENERATION_REPRESENTATION_INVALID",
                "The integer additional-information value is invalid.",
                node
            );
            return null;
        }
        BigInteger argument = SelectIntegerRangeValue(lower, upper);
        return CreateIntegerValue(
            negative ? -BigInteger.One - argument : argument
        );
    }

    internal CborValue? GenerateStringRepresentation(CddlNode node)
    {
        int length = 0;
        CborLengthKind lengthKind = CborLengthKind.Definite;
        if (node.HasArgumentValue)
        {
            int additional = (int)node.ArgumentValue;
            if (additional >= 0 && additional <= 23)
            {
                length = additional;
            }
            else if (additional == 24)
            {
                length = 24;
            }
            else if (additional == 25)
            {
                length = 256;
            }
            else if (additional == 31)
            {
                lengthKind = CborLengthKind.Indefinite;
            }
            else
            {
                State.FailGeneration(
                    "CDDL_GENERATION_REPRESENTATION_LIMIT",
                    "The requested string representation is too large for scalar generation.",
                    node
                );
                return null;
            }
        }
        else if (State.Options.Strategy == CddlGenerationStrategy.Boundary)
        {
            length = 23;
        }
        else if (State.Options.Strategy == CddlGenerationStrategy.Representative)
        {
            length = 1 + State.NextIndex(8);
        }
        if (length > State.Options.MaxCollectionItems)
        {
            State.FailGeneration(
                "CDDL_GENERATION_COLLECTION_LIMIT",
                "The generated string exceeds the configured collection-item limit.",
                node
            );
            return null;
        }
        CborValue value = new CborValue();
        value.LengthKind = lengthKind;
        if (node.MajorType == 2)
        {
            value.Kind = CborValueKind.ByteString;
            value.ByteStringValue = new byte[length];
        }
        else
        {
            value.Kind = CborValueKind.TextString;
            value.TextStringValue = new string('a', length);
        }
        return value;
    }

    internal CborValue? GenerateSimpleRepresentation(CddlNode node)
    {
        int argument;
        if (node.Children.Count == 1)
        {
            CborValue? generated = GenerateNodeValue(node.Children[0]);
            if (generated == null
                || generated.Kind != CborValueKind.UnsignedInteger
                || generated.IntegerValue > new BigInteger(255))
            {
                State.FailGeneration(
                    "CDDL_GENERATION_REPRESENTATION_INVALID",
                    "The simple-value argument type did not generate a supported value.",
                    node
                );
                return null;
            }
            argument = (int)generated.IntegerValue;
        }
        else if (node.HasArgumentValue)
        {
            argument = (int)node.ArgumentValue;
        }
        else
        {
            return CreateSimpleValue(CborSimpleValues.Null);
        }
        if (argument == 25)
        {
            return GenerateFloatValue(CborFloatWidth.Half);
        }
        if (argument == 26)
        {
            return GenerateFloatValue(CborFloatWidth.Single);
        }
        if (argument == 27)
        {
            return GenerateFloatValue(CborFloatWidth.Double);
        }
        if (argument == 24)
        {
            return CreateSimpleValue(32);
        }
        if ((argument >= 0 && argument <= 23)
            || (argument >= 32 && argument <= 255))
        {
            return CreateSimpleValue((byte)argument);
        }
        State.FailGeneration(
            "CDDL_GENERATION_REPRESENTATION_INVALID",
            "The simple-value argument is reserved or outside the CBOR range.",
            node
        );
        return null;
    }

    internal CborValue GenerateUnsignedValue()
    {
        if (State.Options.Strategy == CddlGenerationStrategy.Minimal)
        {
            return CreateIntegerValue(BigInteger.Zero);
        }
        if (State.Options.Strategy == CddlGenerationStrategy.Boundary)
        {
            return CreateIntegerValue(new BigInteger(23));
        }
        return CreateIntegerValue(
            new BigInteger(State.NextRandomValue() % 1000L)
        );
    }

    internal CborValue GenerateNegativeValue()
    {
        if (State.Options.Strategy == CddlGenerationStrategy.Minimal)
        {
            return CreateIntegerValue(-BigInteger.One);
        }
        if (State.Options.Strategy == CddlGenerationStrategy.Boundary)
        {
            return CreateIntegerValue(new BigInteger(-24));
        }
        return CreateIntegerValue(
            new BigInteger(-1L - State.NextRandomValue() % 1000L)
        );
    }

    internal CborValue GenerateByteStringValue()
    {
        int length = State.Options.Strategy == CddlGenerationStrategy.Minimal
            ? 0
            : State.Options.Strategy == CddlGenerationStrategy.Boundary
                ? 23
                : 1 + State.NextIndex(8);
        if (length > State.Options.MaxCollectionItems)
        {
            length = State.Options.MaxCollectionItems;
        }
        byte[] bytes = new byte[length];
        int index = 0;
        while (index < bytes.Length)
        {
            bytes[index] = (byte)State.NextIndex(256);
            index = index + 1;
        }
        CborValue value = new CborValue();
        value.Kind = CborValueKind.ByteString;
        value.ByteStringValue = bytes;
        value.LengthKind = CborLengthKind.Definite;
        return value;
    }

    internal CborValue GenerateTextStringValue()
    {
        int length = State.Options.Strategy == CddlGenerationStrategy.Minimal
            ? 0
            : State.Options.Strategy == CddlGenerationStrategy.Boundary
                ? 23
                : 1 + State.NextIndex(8);
        if (length > State.Options.MaxCollectionItems)
        {
            length = State.Options.MaxCollectionItems;
        }
        char[] characters = new char[length];
        int index = 0;
        while (index < characters.Length)
        {
            characters[index] = (char)('a' + State.NextIndex(26));
            index = index + 1;
        }
        CborValue value = new CborValue();
        value.Kind = CborValueKind.TextString;
        value.TextStringValue = new string(characters);
        value.LengthKind = CborLengthKind.Definite;
        return value;
    }

    internal CborValue GenerateFloatValue(CborFloatWidth width)
    {
        double value = 0.0;
        if (State.Options.Strategy == CddlGenerationStrategy.Boundary)
        {
            bool negative = State.NextIndex(2) == 0;
            if (width == CborFloatWidth.Half)
            {
                value = negative ? -65504.0 : 65504.0;
            }
            else if (width == CborFloatWidth.Single)
            {
                value = negative ? -float.MaxValue : float.MaxValue;
            }
            else
            {
                value = negative ? -double.MaxValue : double.MaxValue;
            }
        }
        else if (State.Options.Strategy == CddlGenerationStrategy.Representative)
        {
            int selected = State.NextIndex(6);
            if (selected == 0)
            {
                value = -1.0;
            }
            else if (selected == 1)
            {
                value = -0.5;
            }
            else if (selected == 2)
            {
                value = 0.0;
            }
            else if (selected == 3)
            {
                value = 0.5;
            }
            else if (selected == 4)
            {
                value = 1.5;
            }
            else
            {
                value = 42.0;
            }
        }
        return CreateFloatValue(value, width);
    }

    internal CborValue CreateIntegerValue(BigInteger integer)
    {
        CborValue value = new CborValue();
        value.Kind = integer.Sign < 0
            ? CborValueKind.NegativeInteger
            : CborValueKind.UnsignedInteger;
        value.IntegerValue = integer;
        return value;
    }

    internal CborValue CreateFloatValue(
        double value,
        CborFloatWidth width)
    {
        CborFloatValue floating;
        if (width == CborFloatWidth.Half)
        {
            floating = CborHalfPrecision.CreateHalfFloat(
                CborHalfPrecision.ConvertDoubleToHalfBits(value)
            );
        }
        else if (width == CborFloatWidth.Single)
        {
            floating = CborHalfPrecision.CreateSingleFloat(
                CborHalfPrecision.ConvertDoubleToSingleBits(value)
            );
        }
        else
        {
            floating = CborHalfPrecision.CreateDoubleFloat(
                CborHalfPrecision.ConvertDoubleToDoubleBits(value)
            );
        }
        CborValue result = new CborValue();
        result.Kind = CborValueKind.Float;
        result.FloatValue = floating;
        result.FloatWidth = width;
        return result;
    }

    internal CborValue CreateSimpleValue(byte simple)
    {
        CborValue value = new CborValue();
        value.Kind = CborValueKind.SimpleValue;
        value.SimpleValue = simple;
        return value;
    }
}
