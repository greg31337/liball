using System;
using System.Text.RegularExpressions;

namespace htmlgui;

internal class ValidationRuleResult
{
    internal bool Success;
    internal bool IsValid;
    internal string Code = string.Empty;
    internal UiValidationIssue? Issue;
    internal UiField? Field;
}

internal class FormValidationCore
{
    internal const int MaximumPatternLength = 1024;
    internal FormCore Forms;

    internal FormValidationCore(FormCore forms)
    {
        Forms = forms;
    }

    internal ValidationRuleResult EvaluateBuiltInRule(string? formKey, string? fieldKey, string? ruleKey)
    {
        FormOperationResult lookup = Forms.FindFormField(formKey, fieldKey);
        if (!lookup.Success || lookup.Form == null || lookup.Field == null)
        {
            return CreateValidationFailure(lookup.Code);
        }

        if (string.IsNullOrWhiteSpace(ruleKey))
        {
            return CreateValidationFailure("validation.invalid-rule-key");
        }

        UiValidationRule? rule = FindValidationRule(lookup.Field, ruleKey);
        if (rule == null)
        {
            return CreateValidationFailure("validation.unknown-rule");
        }

        if (string.IsNullOrWhiteSpace(rule.Kind))
        {
            return CreateValidationFailure("validation.invalid-rule-kind");
        }

        bool valid;
        if (rule.Kind == "required")
        {
            valid = IsRequiredValidationValue(lookup.Field.Value);
        }
        else if (rule.Kind == "length")
        {
            return EvaluateLengthRule(lookup.Form, lookup.Field, rule);
        }
        else if (rule.Kind == "range")
        {
            return EvaluateRangeRule(lookup.Form, lookup.Field, rule);
        }
        else if (rule.Kind == "step")
        {
            return EvaluateStepRule(lookup.Form, lookup.Field, rule);
        }
        else if (rule.Kind == "pattern")
        {
            return EvaluatePatternRule(lookup.Form, lookup.Field, rule);
        }
        else if (rule.Kind == "comparison")
        {
            return EvaluateComparisonRule(lookup.Form, lookup.Field, rule);
        }
        else if (rule.Kind == "selection-count")
        {
            return EvaluateSelectionCountRule(lookup.Form, lookup.Field, rule);
        }
        else
        {
            return CreateValidationFailure("validation.not-built-in");
        }

        return CreateValidationOutcome(lookup.Form, lookup.Field, rule, valid);
    }

    internal ValidationRuleResult EvaluateLengthRule(UiForm form, UiField field, UiValidationRule rule)
    {
        int length = GetValidationValueLength(field.Value);
        if (length < 0)
        {
            return CreateValidationFailure("validation.length-value");
        }

        bool hasMinimum = HasValidationParameter(rule, "minimum");
        bool hasMaximum = HasValidationParameter(rule, "maximum");
        int minimum = GetValidationIntegerParameter(rule, "minimum");
        int maximum = GetValidationIntegerParameter(rule, "maximum");
        if ((!hasMinimum && !hasMaximum) || (hasMinimum && minimum < 0) || (hasMaximum && maximum < 0) || (hasMinimum && hasMaximum && minimum > maximum))
        {
            return CreateValidationFailure("validation.length-parameter");
        }

        bool valid = (!hasMinimum || length >= minimum) && (!hasMaximum || length <= maximum);
        return CreateValidationOutcome(form, field, rule, valid);
    }

    internal ValidationRuleResult EvaluateRangeRule(UiForm form, UiField field, UiValidationRule rule)
    {
        if (!IsNumericValidationValue(field.Value))
        {
            return CreateValidationFailure("validation.range-value");
        }

        bool hasMinimum = HasValidationParameter(rule, "minimum");
        bool hasMaximum = HasValidationParameter(rule, "maximum");
        UiValue? minimum = FindValidationParameter(rule, "minimum");
        UiValue? maximum = FindValidationParameter(rule, "maximum");
        if ((!hasMinimum && !hasMaximum) || (hasMinimum && (minimum == null || !IsNumericValidationValue(minimum))) || (hasMaximum && (maximum == null || !IsNumericValidationValue(maximum))))
        {
            return CreateValidationFailure("validation.range-parameter");
        }

        if (hasMinimum && hasMaximum && CompareValidationNumbers(minimum, maximum) > 0)
        {
            return CreateValidationFailure("validation.range-parameter");
        }

        bool valid = (!hasMinimum || CompareValidationNumbers(field.Value, minimum) >= 0) && (!hasMaximum || CompareValidationNumbers(field.Value, maximum) <= 0);
        return CreateValidationOutcome(form, field, rule, valid);
    }

    internal ValidationRuleResult EvaluateStepRule(UiForm form, UiField field, UiValidationRule rule)
    {
        UiValue? step = FindValidationParameter(rule, "step");
        UiValue? basis = FindValidationParameter(rule, "minimum");
        if (!IsNumericValidationValue(field.Value) || step == null || !IsPositiveValidationNumber(step) || (basis != null && !IsNumericValidationValue(basis)))
        {
            return CreateValidationFailure("validation.step-parameter");
        }

        bool valid = IsValidationStepAligned(field.Value, step, basis);
        return CreateValidationOutcome(form, field, rule, valid);
    }

    internal ValidationRuleResult EvaluatePatternRule(UiForm form, UiField field, UiValidationRule rule)
    {
        UiValue? patternValue = FindValidationParameter(rule, "pattern");
        if (field.Value.Kind != UiValueKind.String || field.Value.StringValue == null || patternValue == null || patternValue.Kind != UiValueKind.String || patternValue.StringValue == null || !IsSafeValidationPattern(patternValue.StringValue))
        {
            return CreateValidationFailure("validation.pattern-parameter");
        }

        string expression = "\\A(?:" + patternValue.StringValue + ")\\z";
        bool valid = Regex.IsMatch(field.Value.StringValue, expression, RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
        return CreateValidationOutcome(form, field, rule, valid);
    }

    internal ValidationRuleResult EvaluateComparisonRule(UiForm form, UiField field, UiValidationRule rule)
    {
        UiValue? fieldValue = FindValidationParameter(rule, "field");
        UiValue? operatorValue = FindValidationParameter(rule, "operator");
        if (fieldValue == null || fieldValue.Kind != UiValueKind.String || string.IsNullOrWhiteSpace(fieldValue.StringValue) || operatorValue == null || operatorValue.Kind != UiValueKind.String || string.IsNullOrWhiteSpace(operatorValue.StringValue))
        {
            return CreateValidationFailure("validation.comparison-parameter");
        }

        int comparedIndex = Forms.FindFormFieldIndex(form, fieldValue.StringValue);
        if (comparedIndex < 0)
        {
            return CreateValidationFailure("validation.unknown-comparison-field");
        }

        UiValue compared = form.Fields[comparedIndex].Value;
        string operation = operatorValue.StringValue;
        bool valid;
        if (operation == "equal")
        {
            valid = Forms.ValueStore.StateValuesEqual(field.Value, compared);
        }
        else if (operation == "not-equal")
        {
            valid = !Forms.ValueStore.StateValuesEqual(field.Value, compared);
        }
        else
        {
            int comparison = CompareValidationValues(field.Value, compared);
            if (comparison == int.MinValue)
            {
                return CreateValidationFailure("validation.comparison-value");
            }

            if (operation == "less-than")
            {
                valid = comparison < 0;
            }
            else if (operation == "less-than-or-equal")
            {
                valid = comparison <= 0;
            }
            else if (operation == "greater-than")
            {
                valid = comparison > 0;
            }
            else if (operation == "greater-than-or-equal")
            {
                valid = comparison >= 0;
            }
            else
            {
                return CreateValidationFailure("validation.comparison-operator");
            }
        }

        return CreateValidationOutcome(form, field, rule, valid);
    }

    internal ValidationRuleResult EvaluateSelectionCountRule(UiForm form, UiField field, UiValidationRule rule)
    {
        if (field.Value.Kind != UiValueKind.Array || field.Value.ArrayValue == null)
        {
            return CreateValidationFailure("validation.selection-count-value");
        }

        bool hasMinimum = HasValidationParameter(rule, "minimum");
        bool hasMaximum = HasValidationParameter(rule, "maximum");
        int minimum = GetValidationIntegerParameter(rule, "minimum");
        int maximum = GetValidationIntegerParameter(rule, "maximum");
        if ((!hasMinimum && !hasMaximum) || (hasMinimum && minimum < 0) || (hasMaximum && maximum < 0) || (hasMinimum && hasMaximum && minimum > maximum))
        {
            return CreateValidationFailure("validation.selection-count-parameter");
        }

        int count = field.Value.ArrayValue.Length;
        bool valid = (!hasMinimum || count >= minimum) && (!hasMaximum || count <= maximum);
        return CreateValidationOutcome(form, field, rule, valid);
    }

    internal FormOperationResult ApplyNativeFormConstraints(string? formKey, string? fieldKey)
    {
        FormOperationResult lookup = Forms.FindFormField(formKey, fieldKey);
        if (!lookup.Success || lookup.Form == null || lookup.Field == null)
        {
            return lookup;
        }

        if (lookup.Field.Input == null)
        {
            return Forms.CreateFormFailure("validation.no-input");
        }

        UiInput input = lookup.Field.Input;
        int index = 0;
        while (index < lookup.Field.Rules.Length)
        {
            UiValidationRule rule = lookup.Field.Rules[index];
            ValidationRuleResult validation = EvaluateBuiltInRule(formKey, fieldKey, rule.Key);
            if (!validation.Success && validation.Code != "validation.not-built-in")
            {
                return Forms.CreateFormFailure(validation.Code);
            }

            if (validation.Success || validation.Code == "validation.not-built-in")
            {
                ApplyNativeValidationRule(input, rule);
            }

            index++;
        }

        return Forms.CreateFormSuccess(lookup.Form, lookup.Field, true);
    }

    internal void ApplyNativeValidationRule(UiInput input, UiValidationRule rule)
    {
        if (rule.Kind == "required" && SupportsNativeRequired(input.Kind))
        {
            input.IsRequired = true;
        }
        else if (rule.Kind == "length" && IsNativeTextualKind(input.Kind))
        {
            if (HasValidationParameter(rule, "minimum"))
            {
                input.HasMinimumLength = true;
                input.MinimumLength = GetValidationIntegerParameter(rule, "minimum");
            }

            if (HasValidationParameter(rule, "maximum"))
            {
                input.HasMaximumLength = true;
                input.MaximumLength = GetValidationIntegerParameter(rule, "maximum");
            }
        }
        else if (rule.Kind == "range" && SupportsNativeRange(input.Kind))
        {
            UiValue? minimum = FindValidationParameter(rule, "minimum");
            UiValue? maximum = FindValidationParameter(rule, "maximum");
            input.Minimum = Forms.CopyOptionalFormValue(minimum);
            input.Maximum = Forms.CopyOptionalFormValue(maximum);
        }
        else if (rule.Kind == "step" && SupportsNativeRange(input.Kind))
        {
            input.Step = Forms.CopyOptionalFormValue(FindValidationParameter(rule, "step"));
            UiValue? minimum = FindValidationParameter(rule, "minimum");
            if (minimum != null && input.Minimum == null)
            {
                input.Minimum = Forms.CopyOptionalFormValue(minimum);
            }
        }
        else if (rule.Kind == "pattern" && SupportsNativePattern(input.Kind))
        {
            UiValue? pattern = FindValidationParameter(rule, "pattern");
            if (pattern != null)
            {
                input.Pattern = pattern.StringValue;
            }
        }
    }

    internal UiValidationRule? FindValidationRule(UiField field, string ruleKey)
    {
        int index = 0;
        while (index < field.Rules.Length)
        {
            if (ValueComparer.TextEquals(field.Rules[index].Key, ruleKey))
            {
                return field.Rules[index];
            }

            index++;
        }

        return null;
    }

    internal UiValue? FindValidationParameter(UiValidationRule rule, string key)
    {
        int index = 0;
        while (index < rule.Parameters.Length)
        {
            if (ValueComparer.TextEquals(rule.Parameters[index].Key, key))
            {
                return rule.Parameters[index].Value;
            }

            index++;
        }

        return null;
    }

    internal bool HasValidationParameter(UiValidationRule rule, string key)
    {
        int index = 0;
        while (index < rule.Parameters.Length)
        {
            if (ValueComparer.TextEquals(rule.Parameters[index].Key, key))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal int GetValidationIntegerParameter(UiValidationRule rule, string key)
    {
        UiValue? value = FindValidationParameter(rule, key);
        if (value == null || value.Kind != UiValueKind.Integer || value.IntegerValue < 0 || value.IntegerValue > int.MaxValue)
        {
            return -1;
        }

        return (int)value.IntegerValue;
    }

    internal bool IsRequiredValidationValue(UiValue value)
    {
        switch (value.Kind)
        {
            case UiValueKind.Null:
                return false;
            case UiValueKind.Boolean:
                return value.BooleanValue;
            case UiValueKind.String:
                return value.StringValue != null && value.StringValue.Length > 0;
            case UiValueKind.Binary:
                return value.BinaryValue != null && value.BinaryValue.Length > 0;
            case UiValueKind.Array:
                return value.ArrayValue != null && value.ArrayValue.Length > 0;
            case UiValueKind.Object:
                return value.ObjectValue != null && value.ObjectValue.Length > 0;
            default:
                return true;
        }
    }

    internal int GetValidationValueLength(UiValue value)
    {
        if (value.Kind == UiValueKind.String && value.StringValue != null)
        {
            return CountUnicodeScalars(value.StringValue);
        }

        if (value.Kind == UiValueKind.Binary && value.BinaryValue != null)
        {
            return value.BinaryValue.Length;
        }

        if (value.Kind == UiValueKind.Array && value.ArrayValue != null)
        {
            return value.ArrayValue.Length;
        }

        return -1;
    }

    internal int CountUnicodeScalars(string value)
    {
        int count = 0;
        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            if (char.IsHighSurrogate(current) && index + 1 < value.Length && char.IsLowSurrogate(value[index + 1]))
            {
                index += 2;
            }
            else
            {
                index++;
            }

            count++;
        }

        return count;
    }

    internal bool IsNumericValidationValue(UiValue? value)
    {
        return value != null && (value.Kind == UiValueKind.Integer || value.Kind == UiValueKind.FloatingPoint || value.Kind == UiValueKind.Decimal);
    }

    internal bool IsPositiveValidationNumber(UiValue value)
    {
        if (!IsNumericValidationValue(value))
        {
            return false;
        }

        return GetValidationDouble(value) > 0.0;
    }

    internal double GetValidationDouble(UiValue? value)
    {
        if (value == null)
        {
            return 0.0;
        }

        switch (value.Kind)
        {
            case UiValueKind.Integer:
                return value.IntegerValue;
            case UiValueKind.FloatingPoint:
                return value.FloatingPointValue;
            case UiValueKind.Decimal:
                return (double)value.DecimalValue;
            default:
                return 0.0;
        }
    }

    internal bool IsValidationStepAligned(UiValue value, UiValue step, UiValue? basis)
    {
        if (value.Kind != UiValueKind.FloatingPoint && step.Kind != UiValueKind.FloatingPoint && (basis == null || basis.Kind != UiValueKind.FloatingPoint))
        {
            decimal decimalValue = value.Kind == UiValueKind.Integer ? value.IntegerValue : value.DecimalValue;
            decimal decimalStep = step.Kind == UiValueKind.Integer ? step.IntegerValue : step.DecimalValue;
            decimal decimalBasis = basis == null ? 0m : basis.Kind == UiValueKind.Integer ? basis.IntegerValue : basis.DecimalValue;
            return (decimalValue - decimalBasis) % decimalStep == 0m;
        }

        double quotient = (GetValidationDouble(value) - GetValidationDouble(basis)) / GetValidationDouble(step);
        double nearest = Math.Round(quotient);
        double scale = Math.Max(1.0, Math.Abs(quotient));
        return Math.Abs(quotient - nearest) <= 0.000000001 * scale;
    }

    internal int CompareValidationValues(UiValue left, UiValue right)
    {
        if (IsNumericValidationValue(left) && IsNumericValidationValue(right))
        {
            return CompareValidationNumbers(left, right);
        }

        if (left.Kind == UiValueKind.String && right.Kind == UiValueKind.String && left.StringValue != null && right.StringValue != null)
        {
            return ValueComparer.CompareText(left.StringValue, right.StringValue);
        }

        if (left.Kind == UiValueKind.DateTime && right.Kind == UiValueKind.DateTime && left.DateTimeValue != null && right.DateTimeValue != null)
        {
            return ValueComparer.CompareText(left.DateTimeValue, right.DateTimeValue);
        }

        return int.MinValue;
    }

    internal int CompareValidationNumbers(UiValue? left, UiValue? right)
    {
        if (left == null || right == null)
        {
            return 0;
        }

        if (left.Kind == UiValueKind.FloatingPoint || right.Kind == UiValueKind.FloatingPoint)
        {
            double leftNumber = GetValidationDouble(left);
            double rightNumber = GetValidationDouble(right);
            return leftNumber < rightNumber ? -1 : leftNumber > rightNumber ? 1 : 0;
        }

        decimal leftNumberDecimal = left.Kind == UiValueKind.Integer ? left.IntegerValue : left.DecimalValue;
        decimal rightNumberDecimal = right.Kind == UiValueKind.Integer ? right.IntegerValue : right.DecimalValue;
        return leftNumberDecimal < rightNumberDecimal ? -1 : leftNumberDecimal > rightNumberDecimal ? 1 : 0;
    }

    internal bool IsSafeValidationPattern(string pattern)
    {
        if (pattern.Length > MaximumPatternLength || pattern.Contains("(?"))
        {
            return false;
        }

        int parenthesisDepth = 0;
        bool inClass = false;
        bool classHasValue = false;
        bool canQuantify = false;
        bool quantified = false;
        char classPrevious = '\0';
        int index = 0;
        while (index < pattern.Length)
        {
            char current = pattern[index];
            if (current == '\\')
            {
                if (index + 1 >= pattern.Length || char.IsDigit(pattern[index + 1]) || IsUnsupportedValidationEscape(pattern[index + 1]))
                {
                    return false;
                }

                if (inClass)
                {
                    classHasValue = true;
                    classPrevious = '\0';
                }
                else
                {
                    canQuantify = true;
                    quantified = false;
                }

                index += 2;
                continue;
            }

            if (inClass)
            {
                if (current == ']')
                {
                    if (!classHasValue)
                    {
                        return false;
                    }

                    inClass = false;
                    canQuantify = true;
                    quantified = false;
                }
                else
                {
                    if (current == '[')
                    {
                        return false;
                    }

                    if (current == '-' && (classPrevious == '\0' || index + 1 >= pattern.Length || pattern[index + 1] == ']' || pattern[index + 1] == '\\' || classPrevious > pattern[index + 1]))
                    {
                        return false;
                    }

                    if (current == '^' && !classHasValue)
                    {
                        classPrevious = '\0';
                    }
                    else if (current != '-')
                    {
                        classHasValue = true;
                        classPrevious = current;
                    }
                }

                index++;
                continue;
            }

            if (current == '[')
            {
                inClass = true;
                classHasValue = false;
                classPrevious = '\0';
                canQuantify = false;
                quantified = false;
            }
            else if (current == ']')
            {
                return false;
            }
            else if (current == '(')
            {
                parenthesisDepth++;
                canQuantify = false;
                quantified = false;
            }
            else if (current == ')')
            {
                if (parenthesisDepth == 0 || !canQuantify)
                {
                    return false;
                }

                parenthesisDepth--;
                canQuantify = true;
                quantified = false;
            }
            else if (current == '|')
            {
                if (!canQuantify || index + 1 >= pattern.Length || pattern[index + 1] == ')' || pattern[index + 1] == '|')
                {
                    return false;
                }

                canQuantify = false;
                quantified = false;
            }
            else if (current == '*' || current == '+' || current == '?')
            {
                if (!canQuantify || quantified)
                {
                    return false;
                }

                quantified = true;
            }
            else if (current == '{')
            {
                int end = FindValidationQuantifierEnd(pattern, index);
                if (!canQuantify || quantified || end < 0 || !IsValidValidationQuantifier(pattern, index + 1, end))
                {
                    return false;
                }

                index = end;
                quantified = true;
            }
            else if (current == '}')
            {
                return false;
            }
            else if (current == '^')
            {
                if (index != 0 && pattern[index - 1] != '(' && pattern[index - 1] != '|')
                {
                    return false;
                }

                canQuantify = false;
                quantified = false;
            }
            else if (current == '$')
            {
                if (index + 1 < pattern.Length && pattern[index + 1] != ')' && pattern[index + 1] != '|')
                {
                    return false;
                }

                canQuantify = false;
                quantified = false;
            }
            else
            {
                canQuantify = true;
                quantified = false;
            }

            index++;
        }

        return !inClass && parenthesisDepth == 0 && (pattern.Length == 0 || canQuantify || pattern[pattern.Length - 1] == '$');
    }

    internal int FindValidationQuantifierEnd(string pattern, int start)
    {
        int index = start + 1;
        while (index < pattern.Length)
        {
            if (pattern[index] == '}')
            {
                return index;
            }

            if (!char.IsDigit(pattern[index]) && pattern[index] != ',')
            {
                return -1;
            }

            index++;
        }

        return -1;
    }

    internal bool IsValidValidationQuantifier(string pattern, int start, int end)
    {
        if (start >= end || pattern[start] == ',')
        {
            return false;
        }

        int commaCount = 0;
        int minimum = 0;
        int maximum = 0;
        bool readingMaximum = false;
        bool hasMaximum = false;
        int index = start;
        while (index < end)
        {
            if (pattern[index] == ',')
            {
                commaCount++;
                if (commaCount > 1)
                {
                    return false;
                }

                readingMaximum = true;
            }
            else if (readingMaximum)
            {
                maximum = maximum * 10 + pattern[index] - '0';
                hasMaximum = true;
                if (maximum > 1000000)
                {
                    return false;
                }
            }
            else
            {
                minimum = minimum * 10 + pattern[index] - '0';
                if (minimum > 1000000)
                {
                    return false;
                }
            }

            index++;
        }

        return !hasMaximum || minimum <= maximum;
    }

    internal bool IsUnsupportedValidationEscape(char value)
    {
        if (!char.IsLetter(value))
        {
            return false;
        }

        return value != 'd' && value != 'D' && value != 's' && value != 'S' && value != 'w' && value != 'W';
    }

    internal bool SupportsNativeRequired(UiInputKind kind)
    {
        return kind != UiInputKind.Hidden && kind != UiInputKind.ReadOnly && kind != UiInputKind.Range && kind != UiInputKind.Color;
    }

    internal bool IsNativeTextualKind(UiInputKind kind)
    {
        return kind == UiInputKind.Text || kind == UiInputKind.Password || kind == UiInputKind.Search || kind == UiInputKind.Email || kind == UiInputKind.Url || kind == UiInputKind.Telephone || kind == UiInputKind.Multiline;
    }

    internal bool SupportsNativeRange(UiInputKind kind)
    {
        return kind == UiInputKind.Number || kind == UiInputKind.Range || kind == UiInputKind.Date || kind == UiInputKind.Time || kind == UiInputKind.Month || kind == UiInputKind.Week || kind == UiInputKind.LocalDateTime;
    }

    internal bool SupportsNativePattern(UiInputKind kind)
    {
        return IsNativeTextualKind(kind) && kind != UiInputKind.Multiline;
    }

    internal ValidationRuleResult CreateValidationOutcome(UiForm form, UiField field, UiValidationRule rule, bool valid)
    {
        ValidationRuleResult result = new ValidationRuleResult();
        result.Success = true;
        result.IsValid = valid;
        result.Field = Forms.CopyFormField(field);
        if (!valid)
        {
            UiValidationIssue issue = new UiValidationIssue();
            issue.Id = rule.Key;
            issue.FieldKey = field.Key;
            issue.Code = "validation." + rule.Kind;
            issue.Message = rule.Message;
            issue.Severity = rule.Severity;
            issue.Source = "local";
            issue.ValueVersion = form.State.Version;
            result.Issue = issue;
        }

        return result;
    }

    internal ValidationRuleResult CreateValidationFailure(string code)
    {
        return new ValidationRuleResult { Code = code };
    }
}
