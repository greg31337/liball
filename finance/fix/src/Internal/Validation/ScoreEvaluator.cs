using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace fix;

internal static class ScoreEvaluator
{
    internal const int MaximumEvaluationDepth = 64;

    internal static RuleEvaluationResult EvaluateScoreRule(string expression, FixPresence presence, FixDictionary dictionary, FixMessage message, ValidationContext context)
    {
        ScoreEvaluationState state = new ScoreEvaluationState();
        state.Dictionary = dictionary;
        state.Message = message;
        state.Context = context;
        return EvaluateScoreRuleWithState(expression, presence, state);
    }

    internal static RuleEvaluationResult EvaluateScoreRuleInEntry(string expression, FixPresence presence, FixDictionary dictionary, FixMessage message, ValidationContext context, FixGroupEntry entry)
    {
        ScoreEvaluationState state = new ScoreEvaluationState();
        state.Dictionary = dictionary;
        state.Message = message;
        state.Context = context;
        state.Entry = entry;
        return EvaluateScoreRuleWithState(expression, presence, state);
    }

    internal static RuleEvaluationResult EvaluateScoreRuleWithState(string expression, FixPresence presence, ScoreEvaluationState state)
    {
        RuleEvaluationResult result = new RuleEvaluationResult();
        result.Presence = presence;
        ScoreParseResult parsed = ScoreParser.Parse(expression);
        if (!parsed.Success || parsed.Root == null)
        {
            string code = parsed.ErrorCode.Length == 0 ? "invalid-rule-expression" : parsed.ErrorCode;
            string description = parsed.ErrorMessage.Length == 0 ? "The Score rule expression is invalid." : parsed.ErrorMessage;
            SetUnavailableResult(result, code, description);
            return result;
        }
        ScoreValue value = EvaluateNode(parsed.Root, state);
        if (!value.Available)
        {
            SetUnavailableResult(result, value.ErrorCode, value.ErrorMessage);
            return result;
        }
        if (parsed.Root.Kind == ScoreSyntaxKind.Assignment)
        {
            result.WasEvaluated = true;
            result.ConditionMatched = true;
            result.AssignedValue = value.TextValue;
            return result;
        }
        if (value.Kind != ScoreValueKind.Boolean)
        {
            SetUnavailableResult(result, "condition-not-boolean", "The Score conditional expression did not produce a Boolean value.");
            return result;
        }
        result.WasEvaluated = true;
        result.ConditionMatched = value.BooleanValue;
        return result;
    }

    internal static ScoreValue EvaluateNode(ScoreSyntaxNode node, ScoreEvaluationState state)
    {
        if (state.Depth >= MaximumEvaluationDepth)
        {
            return CreateUnavailable("evaluation-depth-exceeded", "The Score expression exceeds the evaluation depth limit.");
        }
        state.Depth++;
        ScoreValue result;
        if (node.Kind == ScoreSyntaxKind.IntegerLiteral)
        {
            result = ParseNumberLiteral(node.Text, true);
        }
        else if (node.Kind == ScoreSyntaxKind.DecimalLiteral)
        {
            result = ParseNumberLiteral(node.Text, false);
        }
        else if (node.Kind == ScoreSyntaxKind.CharacterLiteral)
        {
            result = CreateTextValue(ScoreValueKind.Character, node.Text, null);
        }
        else if (node.Kind == ScoreSyntaxKind.StringLiteral)
        {
            result = CreateTextValue(ScoreValueKind.String, node.Text, null);
        }
        else if (node.Kind == ScoreSyntaxKind.DateLiteral)
        {
            result = CreateTextValue(ScoreValueKind.Date, node.Text, null);
        }
        else if (node.Kind == ScoreSyntaxKind.TimeLiteral)
        {
            result = CreateTextValue(ScoreValueKind.Time, node.Text, null);
        }
        else if (node.Kind == ScoreSyntaxKind.TimestampLiteral)
        {
            result = CreateTextValue(ScoreValueKind.Timestamp, node.Text, null);
        }
        else if (node.Kind == ScoreSyntaxKind.DurationLiteral)
        {
            result = CreateTextValue(ScoreValueKind.Duration, node.Text, null);
        }
        else if (node.Kind == ScoreSyntaxKind.Reference)
        {
            result = ResolveReference(node, state, false);
        }
        else if (node.Kind == ScoreSyntaxKind.Exists)
        {
            result = EvaluateExists(node, state);
        }
        else if (node.Kind == ScoreSyntaxKind.Unary)
        {
            result = EvaluateUnary(node, state);
        }
        else if (node.Kind == ScoreSyntaxKind.Binary)
        {
            result = EvaluateBinary(node, state);
        }
        else if (node.Kind == ScoreSyntaxKind.SetMembership)
        {
            result = EvaluateSetMembership(node, state);
        }
        else if (node.Kind == ScoreSyntaxKind.RangeMembership)
        {
            result = EvaluateRangeMembership(node, state);
        }
        else if (node.Kind == ScoreSyntaxKind.Assignment)
        {
            result = EvaluateAssignment(node, state);
        }
        else
        {
            result = CreateUnavailable("unsupported-expression", "The Score expression contains an unsupported syntax node.");
        }
        state.Depth--;
        return result;
    }

    internal static ScoreValue EvaluateExists(ScoreSyntaxNode node, ScoreEvaluationState state)
    {
        if (node.Left == null)
        {
            return CreateUnavailable("invalid-exists-expression", "The Score exists expression has no reference.");
        }
        ScoreValue resolved = ResolveReference(node.Left, state, true);
        if (resolved.Available)
        {
            return CreateBoolean(true);
        }
        if (resolved.ErrorCode == "field-unavailable" || resolved.ErrorCode == "external-state-unavailable" || resolved.ErrorCode == "group-entry-unavailable")
        {
            return CreateBoolean(false);
        }
        return resolved;
    }

    internal static ScoreValue EvaluateUnary(ScoreSyntaxNode node, ScoreEvaluationState state)
    {
        if (node.Left == null)
        {
            return CreateUnavailable("missing-operand", "The Score unary operator has no operand.");
        }
        ScoreValue operand = EvaluateNode(node.Left, state);
        if (!operand.Available)
        {
            return operand;
        }
        if (node.Operator == ScoreTokenKind.Not)
        {
            if (operand.Kind != ScoreValueKind.Boolean)
            {
                return CreateUnavailable("operand-not-boolean", "The Score logical negation operand is not Boolean.");
            }
            return CreateBoolean(!operand.BooleanValue);
        }
        if (node.Operator == ScoreTokenKind.Minus)
        {
            if (!IsNumeric(operand))
            {
                return CreateUnavailable("operand-not-numeric", "The Score numeric negation operand is not numeric.");
            }
            if (operand.NumberValue == decimal.MinValue)
            {
                return CreateUnavailable("arithmetic-overflow", "The Score numeric negation overflowed.");
            }
            return CreateNumber(operand.Kind, -operand.NumberValue);
        }
        return CreateUnavailable("unsupported-unary-operator", "The Score unary operator is unsupported.");
    }

    internal static ScoreValue EvaluateBinary(ScoreSyntaxNode node, ScoreEvaluationState state)
    {
        if (node.Left == null || node.Right == null)
        {
            return CreateUnavailable("missing-operand", "The Score binary operator has a missing operand.");
        }
        if (node.Operator == ScoreTokenKind.And || node.Operator == ScoreTokenKind.Or)
        {
            return EvaluateLogical(node, state);
        }
        ScoreValue left = EvaluateNode(node.Left, state);
        if (!left.Available)
        {
            return left;
        }
        ScoreValue right = EvaluateNode(node.Right, state);
        if (!right.Available)
        {
            return right;
        }
        if (node.Operator == ScoreTokenKind.Equal || node.Operator == ScoreTokenKind.NotEqual || node.Operator == ScoreTokenKind.Less || node.Operator == ScoreTokenKind.LessEqual || node.Operator == ScoreTokenKind.Greater || node.Operator == ScoreTokenKind.GreaterEqual)
        {
            return EvaluateComparison(node.Operator, left, right, state);
        }
        return EvaluateArithmetic(node.Operator, left, right);
    }

    internal static ScoreValue EvaluateLogical(ScoreSyntaxNode node, ScoreEvaluationState state)
    {
        ScoreSyntaxNode? leftNode = node.Left;
        ScoreSyntaxNode? rightNode = node.Right;
        if (leftNode == null || rightNode == null)
        {
            return CreateUnavailable("missing-operand", "The Score logical operator has a missing operand.");
        }
        ScoreValue left = EvaluateNode(leftNode, state);
        if (left.Available && left.Kind != ScoreValueKind.Boolean)
        {
            return CreateUnavailable("operand-not-boolean", "A Score logical operator has a non-Boolean operand.");
        }
        if (left.Available && node.Operator == ScoreTokenKind.And && !left.BooleanValue)
        {
            return CreateBoolean(false);
        }
        if (left.Available && node.Operator == ScoreTokenKind.Or && left.BooleanValue)
        {
            return CreateBoolean(true);
        }
        ScoreValue right = EvaluateNode(rightNode, state);
        if (right.Available && right.Kind != ScoreValueKind.Boolean)
        {
            return CreateUnavailable("operand-not-boolean", "A Score logical operator has a non-Boolean operand.");
        }
        if (!left.Available)
        {
            if (right.Available && node.Operator == ScoreTokenKind.And && !right.BooleanValue)
            {
                return CreateBoolean(false);
            }
            if (right.Available && node.Operator == ScoreTokenKind.Or && right.BooleanValue)
            {
                return CreateBoolean(true);
            }
            return left;
        }
        if (!right.Available)
        {
            return right;
        }
        if (node.Operator == ScoreTokenKind.And)
        {
            return CreateBoolean(left.BooleanValue && right.BooleanValue);
        }
        return CreateBoolean(left.BooleanValue || right.BooleanValue);
    }

    internal static ScoreValue EvaluateComparison(ScoreTokenKind operation, ScoreValue left, ScoreValue right, ScoreEvaluationState state)
    {
        ScoreComparison comparison = CompareValues(left, right, state);
        if (!comparison.Available)
        {
            return CreateUnavailable(comparison.ErrorCode, comparison.ErrorMessage);
        }
        if (operation == ScoreTokenKind.Equal) return CreateBoolean(comparison.Equal);
        if (operation == ScoreTokenKind.NotEqual) return CreateBoolean(!comparison.Equal);
        if (operation == ScoreTokenKind.Less) return CreateBoolean(comparison.Ordering < 0);
        if (operation == ScoreTokenKind.LessEqual) return CreateBoolean(comparison.Ordering <= 0);
        if (operation == ScoreTokenKind.Greater) return CreateBoolean(comparison.Ordering > 0);
        return CreateBoolean(comparison.Ordering >= 0);
    }

    internal static ScoreComparison CompareValues(ScoreValue left, ScoreValue right, ScoreEvaluationState state)
    {
        ScoreComparison result = new ScoreComparison();
        ScoreValue resolvedLeft = left;
        ScoreValue resolvedRight = right;
        if (left.Kind == ScoreValueKind.Code)
        {
            resolvedLeft = ResolveCodeLiteral(left, right.Definition, state.Dictionary);
        }
        if (right.Kind == ScoreValueKind.Code)
        {
            resolvedRight = ResolveCodeLiteral(right, left.Definition, state.Dictionary);
        }
        if (!resolvedLeft.Available)
        {
            CopyUnavailableComparison(result, resolvedLeft);
            return result;
        }
        if (!resolvedRight.Available)
        {
            CopyUnavailableComparison(result, resolvedRight);
            return result;
        }
        if (IsNumeric(resolvedLeft) && IsNumeric(resolvedRight))
        {
            result.Available = true;
            result.Equal = resolvedLeft.NumberValue == resolvedRight.NumberValue;
            result.Ordering = resolvedLeft.NumberValue < resolvedRight.NumberValue ? -1 : resolvedLeft.NumberValue > resolvedRight.NumberValue ? 1 : 0;
            return result;
        }
        if (resolvedLeft.Kind == ScoreValueKind.Boolean && resolvedRight.Kind == ScoreValueKind.Boolean)
        {
            result.Available = true;
            result.Equal = resolvedLeft.BooleanValue == resolvedRight.BooleanValue;
            result.Ordering = result.Equal ? 0 : resolvedLeft.BooleanValue ? 1 : -1;
            return result;
        }
        if (IsTextual(resolvedLeft) && IsTextual(resolvedRight))
        {
            result.Available = true;
            result.Ordering = string.CompareOrdinal(resolvedLeft.TextValue, resolvedRight.TextValue);
            result.Equal = result.Ordering == 0;
            return result;
        }
        result.ErrorCode = "incompatible-operands";
        result.ErrorMessage = "The Score comparison operands have incompatible types.";
        return result;
    }

    internal static ScoreValue EvaluateArithmetic(ScoreTokenKind operation, ScoreValue left, ScoreValue right)
    {
        if (!IsNumeric(left) || !IsNumeric(right))
        {
            return CreateUnavailable("operand-not-numeric", "A Score arithmetic operator has a non-numeric operand.");
        }
        decimal first = left.NumberValue;
        decimal second = right.NumberValue;
        decimal value;
        if (operation == ScoreTokenKind.Plus)
        {
            if (second > 0m && first > decimal.MaxValue - second || second < 0m && first < decimal.MinValue - second)
            {
                return CreateUnavailable("arithmetic-overflow", "The Score addition overflowed.");
            }
            value = first + second;
        }
        else if (operation == ScoreTokenKind.Minus)
        {
            if (second > 0m && first < decimal.MinValue + second || second < 0m && first > decimal.MaxValue + second)
            {
                return CreateUnavailable("arithmetic-overflow", "The Score subtraction overflowed.");
            }
            value = first - second;
        }
        else if (operation == ScoreTokenKind.Multiply)
        {
            if (WouldMultiplyOverflow(first, second))
            {
                return CreateUnavailable("arithmetic-overflow", "The Score multiplication overflowed.");
            }
            value = first * second;
        }
        else if (operation == ScoreTokenKind.Divide)
        {
            if (second == 0m)
            {
                return CreateUnavailable("division-by-zero", "The Score division operand is zero.");
            }
            if (first == decimal.MinValue && second == -1m)
            {
                return CreateUnavailable("arithmetic-overflow", "The Score division overflowed.");
            }
            value = first / second;
        }
        else if (operation == ScoreTokenKind.Modulo)
        {
            if (second == 0m)
            {
                return CreateUnavailable("division-by-zero", "The Score modulo operand is zero.");
            }
            if (first == decimal.MinValue && second == -1m)
            {
                value = 0m;
            }
            else
            {
                value = first % second;
            }
        }
        else
        {
            return CreateUnavailable("unsupported-binary-operator", "The Score binary operator is unsupported.");
        }
        ScoreValueKind kind = left.Kind == ScoreValueKind.Integer && right.Kind == ScoreValueKind.Integer && operation != ScoreTokenKind.Divide ? ScoreValueKind.Integer : ScoreValueKind.Decimal;
        if (operation == ScoreTokenKind.Divide && left.Kind == ScoreValueKind.Integer && right.Kind == ScoreValueKind.Integer && first % second == 0m)
        {
            kind = ScoreValueKind.Integer;
        }
        return CreateNumber(kind, value);
    }

    internal static bool WouldMultiplyOverflow(decimal first, decimal second)
    {
        if (first == 0m || second == 0m)
        {
            return false;
        }
        if (first > 0m)
        {
            if (second > 0m) return first > decimal.MaxValue / second;
            return second < decimal.MinValue / first;
        }
        if (second > 0m) return first < decimal.MinValue / second;
        return first < decimal.MaxValue / second;
    }

    internal static ScoreValue EvaluateSetMembership(ScoreSyntaxNode node, ScoreEvaluationState state)
    {
        if (node.Left == null)
        {
            return CreateUnavailable("missing-operand", "The Score set expression has no tested value.");
        }
        ScoreValue tested = EvaluateNode(node.Left, state);
        if (!tested.Available)
        {
            return tested;
        }
        ScoreValue? indeterminate = null;
        for (int index = 0; index < node.Children.Count; index++)
        {
            ScoreValue member = EvaluateNode(node.Children[index], state);
            if (!member.Available)
            {
                if (indeterminate == null) indeterminate = member;
                continue;
            }
            ScoreValue equal = EvaluateComparison(ScoreTokenKind.Equal, tested, member, state);
            if (!equal.Available)
            {
                if (indeterminate == null) indeterminate = equal;
                continue;
            }
            if (equal.BooleanValue)
            {
                return CreateBoolean(true);
            }
        }
        if (indeterminate != null)
        {
            return indeterminate;
        }
        return CreateBoolean(false);
    }

    internal static ScoreValue EvaluateRangeMembership(ScoreSyntaxNode node, ScoreEvaluationState state)
    {
        if (node.Left == null || node.Right == null || node.Third == null)
        {
            return CreateUnavailable("missing-operand", "The Score range expression has a missing operand.");
        }
        ScoreValue tested = EvaluateNode(node.Left, state);
        if (!tested.Available) return tested;
        ScoreValue minimum = EvaluateNode(node.Right, state);
        if (!minimum.Available) return minimum;
        ScoreValue maximum = EvaluateNode(node.Third, state);
        if (!maximum.Available) return maximum;
        ScoreValue lower = EvaluateComparison(ScoreTokenKind.GreaterEqual, tested, minimum, state);
        if (!lower.Available || !lower.BooleanValue) return lower;
        return EvaluateComparison(ScoreTokenKind.LessEqual, tested, maximum, state);
    }

    internal static ScoreValue EvaluateAssignment(ScoreSyntaxNode node, ScoreEvaluationState state)
    {
        if (node.Left == null || node.Right == null || node.Left.Kind != ScoreSyntaxKind.Reference)
        {
            return CreateUnavailable("invalid-assignment-target", "The Score assignment target is invalid.");
        }
        FixFieldDefinition? targetDefinition = ResolveAssignmentDefinition(node.Left, state);
        if (node.Left.Text != "$" && targetDefinition == null)
        {
            return CreateUnavailable("unknown-assignment-target", "The Score assignment field target is not defined by the dictionary.");
        }
        ScoreValue value = EvaluateNode(node.Right, state);
        if (!value.Available)
        {
            return value;
        }
        if (value.Kind == ScoreValueKind.Code)
        {
            value = ResolveCodeLiteral(value, targetDefinition, state.Dictionary);
            if (!value.Available) return value;
        }
        ScoreValue assigned = CreateTextValue(ScoreValueKind.String, FormatAssignedValue(value, targetDefinition), targetDefinition);
        return assigned;
    }

    internal static FixFieldDefinition? ResolveAssignmentDefinition(ScoreSyntaxNode reference, ScoreEvaluationState state)
    {
        if (reference.Text == "$" || reference.Children.Count == 0)
        {
            return null;
        }
        ScoreSyntaxNode part = reference.Children[reference.Children.Count - 1];
        return FindFieldDefinition(part.Text, state.Dictionary);
    }

    internal static ScoreValue ResolveReference(ScoreSyntaxNode reference, ScoreEvaluationState state, bool allowMissing)
    {
        if (reference.Children.Count == 0)
        {
            return CreateUnavailable("unknown-reference", "The Score reference has no name.");
        }
        if (reference.Text == "$")
        {
            return ResolveExternalState(reference, state.Context);
        }
        if (reference.Text == "^")
        {
            if (reference.Children.Count != 1)
            {
                return CreateUnavailable("invalid-code-literal", "A Score code literal must contain one code name.");
            }
            ScoreValue code = CreateTextValue(ScoreValueKind.Code, reference.Children[0].Text, null);
            return code;
        }
        if (reference.Text.Length == 0 && reference.Children.Count == 1 && reference.Children[0].Left == null)
        {
            string special = reference.Children[0].Text;
            if (special == "true") return CreateBoolean(true);
            if (special == "false") return CreateBoolean(false);
        }

        FixMessage selected = state.Message;
        if (reference.Text == "in" && state.Context.PreviousMessage != null)
        {
            selected = state.Context.PreviousMessage;
        }
        ScoreValue resolved = ResolveMessageReference(reference, selected, state);
        if (!resolved.Available && allowMissing)
        {
            return resolved;
        }
        return resolved;
    }

    internal static ScoreValue ResolveExternalState(ScoreSyntaxNode reference, ValidationContext context)
    {
        StringBuilder key = new StringBuilder();
        for (int index = 0; index < reference.Children.Count; index++)
        {
            if (index > 0) key.Append('.');
            ScoreSyntaxNode part = reference.Children[index];
            key.Append(part.Text);
            if (part.Left != null && part.Left.Kind == ScoreSyntaxKind.IntegerLiteral)
            {
                key.Append('[');
                key.Append(part.Left.Text);
                key.Append(']');
            }
        }
        string name = key.ToString();
        if (!context.ExternalState.ContainsKey(name))
        {
            return CreateUnavailable("external-state-unavailable", "External state '" + name + "' is unavailable.");
        }
        return InferTextValue(context.ExternalState[name], null);
    }

    internal static ScoreValue ResolveMessageReference(ScoreSyntaxNode reference, FixMessage message, ScoreEvaluationState state)
    {
        if (reference.Children.Count == 1)
        {
            ScoreSyntaxNode only = reference.Children[0];
            FixFieldDefinition? definition = FindFieldDefinition(only.Text, state.Dictionary);
            int numericTag = ParsePositiveTag(only.Text);
            if (definition == null && numericTag == 0)
            {
                return CreateUnavailable("unknown-reference", "Message field '" + only.Text + "' is not defined by the dictionary.");
            }
            int tag = definition == null ? numericTag : definition.Tag;
            FixField? field = state.Entry == null ? FindMessageField(message, tag) : FindEntryField(state.Entry, tag);
            if (field == null && state.Entry != null)
            {
                field = FindMessageField(message, tag);
            }
            if (field == null)
            {
                return CreateUnavailable("field-unavailable", "Message field '" + only.Text + "' is unavailable.");
            }
            return DecodeFieldValue(field, definition);
        }

        FixGroupEntry? entry = state.Entry;
        for (int index = 0; index < reference.Children.Count - 1; index++)
        {
            ScoreSyntaxNode part = reference.Children[index];
            FixGroupDefinition? groupDefinition = FindGroupDefinition(part.Text, state.Dictionary);
            if (groupDefinition == null)
            {
                return CreateUnavailable("unknown-reference", "Message group '" + part.Text + "' is not defined by the dictionary.");
            }
            FixGroup? group = entry == null ? FindMessageGroup(message, groupDefinition.CountTag) : FindEntryGroup(entry, groupDefinition.CountTag);
            if (group == null)
            {
                return CreateUnavailable("group-entry-unavailable", "Message group '" + part.Text + "' is unavailable.");
            }
            entry = SelectGroupEntry(group, part.Left, groupDefinition, state);
            if (entry == null)
            {
                return CreateUnavailable("group-entry-unavailable", "A unique entry for message group '" + part.Text + "' is unavailable.");
            }
        }
        ScoreSyntaxNode fieldPart = reference.Children[reference.Children.Count - 1];
        FixFieldDefinition? fieldDefinition = FindFieldDefinition(fieldPart.Text, state.Dictionary);
        if (fieldDefinition == null)
        {
            return CreateUnavailable("unknown-reference", "Message field '" + fieldPart.Text + "' is not defined by the dictionary.");
        }
        if (entry == null)
        {
            return CreateUnavailable("group-entry-unavailable", "The Score group reference has no selected entry.");
        }
        FixField? selectedField = FindEntryField(entry, fieldDefinition.Tag);
        if (selectedField == null)
        {
            return CreateUnavailable("field-unavailable", "Message field '" + fieldPart.Text + "' is unavailable in the selected group entry.");
        }
        return DecodeFieldValue(selectedField, fieldDefinition);
    }

    internal static FixGroupEntry? SelectGroupEntry(FixGroup group, ScoreSyntaxNode? selector, FixGroupDefinition definition, ScoreEvaluationState state)
    {
        if (selector == null)
        {
            return group.Entries.Count == 1 ? group.Entries[0] : null;
        }
        if (selector.Kind == ScoreSyntaxKind.IntegerLiteral)
        {
            ScoreValue ordinal = ParseNumberLiteral(selector.Text, true);
            if (!ordinal.Available) return null;
            int index = (int)ordinal.NumberValue;
            if (index < 1 || index > group.Entries.Count) return null;
            return group.Entries[index - 1];
        }
        if (selector.Kind != ScoreSyntaxKind.PredicateSelector || selector.Right == null)
        {
            return null;
        }
        FixFieldDefinition? predicateDefinition = FindFieldDefinition(selector.Text, state.Dictionary);
        if (predicateDefinition == null)
        {
            return null;
        }
        FixGroupEntry? selected = null;
        for (int index = 0; index < group.Entries.Count; index++)
        {
            FixField? field = FindEntryField(group.Entries[index], predicateDefinition.Tag);
            if (field == null) continue;
            ScoreValue left = DecodeFieldValue(field, predicateDefinition);
            FixGroupEntry? previousEntry = state.Entry;
            state.Entry = group.Entries[index];
            ScoreValue right = EvaluateNode(selector.Right, state);
            state.Entry = previousEntry;
            if (!right.Available) continue;
            ScoreComparison comparison = CompareValues(left, right, state);
            if (!comparison.Available || !comparison.Equal) continue;
            if (selected != null) return null;
            selected = group.Entries[index];
        }
        return selected;
    }

    internal static ScoreValue DecodeFieldValue(FixField field, FixFieldDefinition? definition)
    {
        if (definition == null)
        {
            return InferTextValue(Encoding.Latin1.GetString(field.Value), null);
        }
        FixFieldType type = definition.FieldType;
        if (IsIntegerType(type))
        {
            FixFieldType parseType = type == FixFieldType.DayOfMonth ? FixFieldType.Int : type;
            ScalarParseResult parsed = ScalarParser.ParseScalar(field.Value, parseType, FixDefaults.MaxFieldSize);
            if (!parsed.Success) return InvalidFieldValue(definition, parsed.ErrorCode);
            ScoreValue value = CreateNumber(ScoreValueKind.Integer, parsed.IntegerValue);
            value.Definition = definition;
            return value;
        }
        if (IsDecimalType(type))
        {
            ScalarParseResult parsed = ScalarParser.ParseScalar(field.Value, type, FixDefaults.MaxFieldSize);
            if (!parsed.Success) return InvalidFieldValue(definition, parsed.ErrorCode);
            ScoreValue value = CreateNumber(ScoreValueKind.Decimal, parsed.DecimalValue);
            value.Definition = definition;
            return value;
        }
        if (type == FixFieldType.Boolean)
        {
            ScalarParseResult parsed = ScalarParser.ParseScalar(field.Value, type, FixDefaults.MaxFieldSize);
            if (!parsed.Success) return InvalidFieldValue(definition, parsed.ErrorCode);
            ScoreValue value = CreateBoolean(parsed.BooleanValue);
            value.Definition = definition;
            return value;
        }
        string text = Encoding.Latin1.GetString(field.Value);
        ScoreValueKind kind = KindForFieldType(type);
        return CreateTextValue(kind, text, definition);
    }

    internal static ScoreValue ResolveCodeLiteral(ScoreValue code, FixFieldDefinition? definition, FixDictionary dictionary)
    {
        FixCodeDefinition? matched = null;
        FixCodeSetDefinition? matchedSet = null;
        for (int setIndex = 0; setIndex < dictionary.CodeSets.Count; setIndex++)
        {
            FixCodeSetDefinition set = dictionary.CodeSets[setIndex];
            if (definition != null && !NamesEqual(set.Name, definition.CodeSetName, dictionary.Options.NormalizeNames)) continue;
            for (int codeIndex = 0; codeIndex < set.Codes.Count; codeIndex++)
            {
                FixCodeDefinition candidate = set.Codes[codeIndex];
                if (!NamesEqual(candidate.Name, code.TextValue, dictionary.Options.NormalizeNames)) continue;
                if (matched != null && (matched.Value != candidate.Value || matchedSet == null || matchedSet.FieldType != set.FieldType))
                {
                    return CreateUnavailable("ambiguous-code-literal", "The Score code literal '" + code.TextValue + "' is ambiguous.");
                }
                matched = candidate;
                matchedSet = set;
            }
        }
        if (matched == null || matchedSet == null)
        {
            return CreateUnavailable("code-value-unavailable", "The Score code literal '" + code.TextValue + "' is unavailable.");
        }
        FixFieldDefinition synthetic = new FixFieldDefinition();
        synthetic.Name = definition == null ? code.TextValue : definition.Name;
        synthetic.FieldType = matchedSet.FieldType;
        synthetic.CodeSetName = matchedSet.Name;
        FixField field = new FixField();
        field.Value = Encoding.Latin1.GetBytes(matched.Value);
        ScoreValue resolved = DecodeFieldValue(field, synthetic);
        resolved.Definition = definition;
        return resolved;
    }

    internal static ScoreValue InferTextValue(string text, FixFieldDefinition? definition)
    {
        if (text == "true")
        {
            ScoreValue result = CreateBoolean(true);
            result.Definition = definition;
            return result;
        }
        if (text == "false")
        {
            ScoreValue result = CreateBoolean(false);
            result.Definition = definition;
            return result;
        }
        ScoreValue integer = ParseNumberLiteral(text, true);
        if (integer.Available)
        {
            integer.Definition = definition;
            return integer;
        }
        ScoreValue number = ParseNumberLiteral(text, false);
        if (number.Available)
        {
            number.Definition = definition;
            return number;
        }
        return CreateTextValue(ScoreValueKind.String, text, definition);
    }

    internal static ScoreValue ParseNumberLiteral(string text, bool integer)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(text);
        FixFieldType type = integer ? FixFieldType.Int : FixFieldType.Float;
        ScalarParseResult parsed = ScalarParser.ParseScalar(bytes, type, FixDefaults.MaxFieldSize);
        if (!parsed.Success)
        {
            return CreateUnavailable("invalid-numeric-value", "The Score numeric value is invalid or outside the supported decimal range.");
        }
        return CreateNumber(integer ? ScoreValueKind.Integer : ScoreValueKind.Decimal, integer ? parsed.IntegerValue : parsed.DecimalValue);
    }

    internal static int ParsePositiveTag(string text)
    {
        if (text.Length == 0) return 0;
        int value = 0;
        for (int index = 0; index < text.Length; index++)
        {
            char current = text[index];
            if (current < '0' || current > '9') return 0;
            int digit = current - '0';
            if (value > (int.MaxValue - digit) / 10) return 0;
            value = value * 10 + digit;
        }
        return value;
    }

    internal static FixFieldDefinition? FindFieldDefinition(string name, FixDictionary dictionary)
    {
        int tag = ParsePositiveTag(name);
        for (int index = 0; index < dictionary.Fields.Count; index++)
        {
            FixFieldDefinition definition = dictionary.Fields[index];
            if (tag > 0 && definition.Tag == tag || NamesEqual(definition.Name, name, dictionary.Options.NormalizeNames) || definition.Abbreviation.Length > 0 && NamesEqual(definition.Abbreviation, name, dictionary.Options.NormalizeNames))
            {
                return definition;
            }
        }
        return null;
    }

    internal static FixGroupDefinition? FindGroupDefinition(string name, FixDictionary dictionary)
    {
        for (int index = 0; index < dictionary.Groups.Count; index++)
        {
            FixGroupDefinition definition = dictionary.Groups[index];
            if (NamesEqual(definition.Name, name, dictionary.Options.NormalizeNames)) return definition;
            FixFieldDefinition? count = FindFieldDefinitionByTag(definition.CountTag, dictionary);
            if (count != null && NamesEqual(count.Name, name, dictionary.Options.NormalizeNames)) return definition;
        }
        return null;
    }

    internal static FixFieldDefinition? FindFieldDefinitionByTag(int tag, FixDictionary dictionary)
    {
        for (int index = 0; index < dictionary.Fields.Count; index++)
        {
            if (dictionary.Fields[index].Tag == tag) return dictionary.Fields[index];
        }
        return null;
    }

    internal static FixField? FindMessageField(FixMessage message, int tag)
    {
        FixField? found = FindFieldInList(message.HeaderFields, tag);
        if (found != null) return found;
        found = FindFieldInList(message.BodyFields, tag);
        if (found != null) return found;
        return FindFieldInList(message.TrailerFields, tag);
    }

    internal static FixField? FindFieldInList(List<FixField> fields, int tag)
    {
        for (int index = 0; index < fields.Count; index++)
        {
            if (fields[index].Tag == tag) return fields[index];
        }
        return null;
    }

    internal static FixField? FindEntryField(FixGroupEntry entry, int tag)
    {
        return FindFieldInList(entry.Fields, tag);
    }

    internal static FixGroup? FindMessageGroup(FixMessage message, int countTag)
    {
        FixGroup? found = FindGroupInList(message.HeaderGroups, countTag);
        if (found != null) return found;
        found = FindGroupInList(message.BodyGroups, countTag);
        if (found != null) return found;
        return FindGroupInList(message.TrailerGroups, countTag);
    }

    internal static FixGroup? FindGroupInList(List<FixGroup> groups, int countTag)
    {
        for (int index = 0; index < groups.Count; index++)
        {
            if (groups[index].CountTag == countTag) return groups[index];
        }
        return null;
    }

    internal static FixGroup? FindEntryGroup(FixGroupEntry entry, int countTag)
    {
        return FindGroupInList(entry.Groups, countTag);
    }

    internal static bool NamesEqual(string first, string second, bool normalized)
    {
        if (normalized) return string.Equals(first, second, StringComparison.OrdinalIgnoreCase);
        return first == second;
    }

    internal static bool IsNumeric(ScoreValue value)
    {
        return value.Kind == ScoreValueKind.Integer || value.Kind == ScoreValueKind.Decimal;
    }

    internal static bool IsTextual(ScoreValue value)
    {
        return value.Kind == ScoreValueKind.Character || value.Kind == ScoreValueKind.String || value.Kind == ScoreValueKind.Date || value.Kind == ScoreValueKind.Time || value.Kind == ScoreValueKind.Timestamp || value.Kind == ScoreValueKind.Duration;
    }

    internal static bool IsIntegerType(FixFieldType type)
    {
        return type == FixFieldType.Int || type == FixFieldType.SeqNum || type == FixFieldType.NumInGroup || type == FixFieldType.Length || type == FixFieldType.TagNum || type == FixFieldType.DayOfMonth;
    }

    internal static bool IsDecimalType(FixFieldType type)
    {
        return type == FixFieldType.Float || type == FixFieldType.Qty || type == FixFieldType.Price || type == FixFieldType.PriceOffset || type == FixFieldType.Amt || type == FixFieldType.Percentage;
    }

    internal static ScoreValueKind KindForFieldType(FixFieldType type)
    {
        if (type == FixFieldType.Char) return ScoreValueKind.Character;
        if (type == FixFieldType.LocalMktDate || type == FixFieldType.UtcDateOnly || type == FixFieldType.MonthYear) return ScoreValueKind.Date;
        if (type == FixFieldType.UtcTimeOnly || type == FixFieldType.TzTimeOnly) return ScoreValueKind.Time;
        if (type == FixFieldType.UtcTimestamp || type == FixFieldType.TzTimestamp) return ScoreValueKind.Timestamp;
        return ScoreValueKind.String;
    }

    internal static string FormatAssignedValue(ScoreValue value, FixFieldDefinition? target)
    {
        if (value.Kind == ScoreValueKind.Boolean)
        {
            if (target != null && target.FieldType == FixFieldType.Boolean) return value.BooleanValue ? "Y" : "N";
            return value.BooleanValue ? "true" : "false";
        }
        if (value.Kind == ScoreValueKind.Integer)
        {
            return value.NumberValue.ToString("0", CultureInfo.InvariantCulture);
        }
        if (value.Kind == ScoreValueKind.Decimal)
        {
            return value.NumberValue.ToString("0.############################", CultureInfo.InvariantCulture);
        }
        return value.TextValue;
    }

    internal static ScoreValue CreateBoolean(bool value)
    {
        ScoreValue result = new ScoreValue();
        result.Available = true;
        result.Kind = ScoreValueKind.Boolean;
        result.BooleanValue = value;
        return result;
    }

    internal static ScoreValue CreateNumber(ScoreValueKind kind, decimal value)
    {
        ScoreValue result = new ScoreValue();
        result.Available = true;
        result.Kind = kind;
        result.NumberValue = value;
        return result;
    }

    internal static ScoreValue CreateTextValue(ScoreValueKind kind, string text, FixFieldDefinition? definition)
    {
        ScoreValue result = new ScoreValue();
        result.Available = true;
        result.Kind = kind;
        result.TextValue = text;
        result.Definition = definition;
        return result;
    }

    internal static ScoreValue CreateUnavailable(string code, string message)
    {
        ScoreValue result = new ScoreValue();
        result.ErrorCode = code.Length == 0 ? "rule-input-unavailable" : code;
        result.ErrorMessage = message.Length == 0 ? "The Score rule could not be evaluated." : message;
        return result;
    }

    internal static ScoreValue InvalidFieldValue(FixFieldDefinition definition, string parseCode)
    {
        return CreateUnavailable("invalid-field-value", "Message field '" + definition.Name + "' has an invalid value (" + parseCode + ").");
    }

    internal static void CopyUnavailableComparison(ScoreComparison comparison, ScoreValue value)
    {
        comparison.ErrorCode = value.ErrorCode;
        comparison.ErrorMessage = value.ErrorMessage;
    }

    internal static void SetUnavailableResult(RuleEvaluationResult result, string code, string message)
    {
        result.ErrorCode = code.Length == 0 ? "rule-input-unavailable" : code;
        result.ErrorMessage = message.Length == 0 ? "The Score rule could not be evaluated." : message;
        ValidationIssue issue = new ValidationIssue();
        issue.Severity = ValidationSeverity.Warning;
        issue.Layer = ValidationLayer.ConditionalRule;
        issue.Code = result.ErrorCode;
        issue.Message = result.ErrorMessage;
        result.Issues.Add(issue);
    }
}
