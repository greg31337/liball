namespace uspto;

internal class PatentsViewLocalNumericValue
{
    internal bool IsValid = false;
    internal int Sign = 0;
    internal string Digits = string.Empty;
    internal long DecimalPosition = 0L;
}

internal class PatentsViewLocalDateValue
{
    internal bool IsValid = false;
    internal int SortValue = 0;
}

internal static class PatentsViewLocalPredicateEvaluator
{
    internal static bool Matches(PatentsViewDerivedTableRecord record, SearchFilter filter)
    {
        if (record == null)
        {
            throw new ValidationException("A PatentsView local record is required.", "record", "REQUIRED", null);
        }

        if (filter == null)
        {
            throw new ValidationException("A PatentsView local predicate is required.", "filter", "REQUIRED", null);
        }

        return EvaluateNode(record, filter, 0);
    }

    internal static bool MatchesRange(PatentsViewDerivedTableRecord record, SearchRange range)
    {
        if (record == null)
        {
            throw new ValidationException("A PatentsView local record is required.", "record", "REQUIRED", null);
        }

        PatentsViewQueryWriter.ValidateRange(range);
        string? actual = record.ReadValue(range.Field ?? string.Empty);
        if (IsMissing(actual))
        {
            return false;
        }

        if (range.LowerValue != null)
        {
            int lowerComparison = CompareValues(actual ?? string.Empty, range.LowerValue);
            if (lowerComparison < 0 || lowerComparison == 0 && !range.IncludeLower)
            {
                return false;
            }
        }

        if (range.UpperValue != null)
        {
            int upperComparison = CompareValues(actual ?? string.Empty, range.UpperValue);
            if (upperComparison > 0 || upperComparison == 0 && !range.IncludeUpper)
            {
                return false;
            }
        }

        return true;
    }

    internal static int CompareValues(string left, string right)
    {
        if (left == null)
        {
            throw new ValidationException("A left local comparison value is required.", "left", "REQUIRED", null);
        }

        if (right == null)
        {
            throw new ValidationException("A right local comparison value is required.", "right", "REQUIRED", null);
        }

        PatentsViewLocalDateValue leftDate = ParseDate(left);
        PatentsViewLocalDateValue rightDate = ParseDate(right);
        if (leftDate.IsValid && rightDate.IsValid)
        {
            return leftDate.SortValue.CompareTo(rightDate.SortValue);
        }

        PatentsViewLocalNumericValue leftNumber = ParseNumber(left);
        PatentsViewLocalNumericValue rightNumber = ParseNumber(right);
        if (leftNumber.IsValid && rightNumber.IsValid)
        {
            return CompareNumbers(leftNumber, rightNumber);
        }

        return global::System.StringComparer.OrdinalIgnoreCase.Compare(left, right);
    }

    internal static bool IsMissing(string? value)
    {
        return global::System.String.IsNullOrWhiteSpace(value);
    }

    internal static bool EvaluateNode(PatentsViewDerivedTableRecord record, SearchFilter filter, int depth)
    {
        if (depth >= PatentsViewQueryWriter.MaximumPredicateDepth)
        {
            throw new ValidationException("The PatentsView local predicate tree exceeds the supported depth.", "filter", "PREDICATE_DEPTH_EXCEEDED", null);
        }

        switch (filter.Operator)
        {
            case SearchOperator.Equal:
            case SearchOperator.NotEqual:
            case SearchOperator.LessThan:
            case SearchOperator.LessThanOrEqual:
            case SearchOperator.GreaterThan:
            case SearchOperator.GreaterThanOrEqual:
            case SearchOperator.Contains:
            case SearchOperator.In:
                return EvaluateLeaf(record, filter);
            case SearchOperator.And:
                PatentsViewQueryWriter.ValidateLogical(filter, "_and", 2);
                int andIndex;
                for (andIndex = 0; andIndex < filter.Children.Length; andIndex += 1)
                {
                    if (!EvaluateNode(record, filter.Children[andIndex], depth + 1))
                    {
                        return false;
                    }
                }

                return true;
            case SearchOperator.Or:
                PatentsViewQueryWriter.ValidateLogical(filter, "_or", 2);
                int orIndex;
                for (orIndex = 0; orIndex < filter.Children.Length; orIndex += 1)
                {
                    if (EvaluateNode(record, filter.Children[orIndex], depth + 1))
                    {
                        return true;
                    }
                }

                return false;
            case SearchOperator.Not:
                PatentsViewQueryWriter.ValidateLogical(filter, "_not", 1);
                return !EvaluateNode(record, filter.Children[0], depth + 1);
            default:
                throw new ValidationException("The PatentsView local predicate has no supported operator.", "filter.Operator", "UNSUPPORTED_SEARCH_OPERATOR", null);
        }
    }

    internal static bool EvaluateLeaf(PatentsViewDerivedTableRecord record, SearchFilter filter)
    {
        PatentsViewQueryWriter.ValidateLeaf(filter);
        if (filter.Operator == SearchOperator.In)
        {
            PatentsViewQueryWriter.ValidateSetValues(filter);
        }
        else
        {
            PatentsViewQueryWriter.ValidateSingleValue(filter);
        }

        string? actual = record.ReadValue(filter.Field ?? string.Empty);
        if (IsMissing(actual))
        {
            return filter.Operator == SearchOperator.NotEqual;
        }

        if (filter.Operator == SearchOperator.In)
        {
            int valueIndex;
            for (valueIndex = 0; valueIndex < filter.Values.Length; valueIndex += 1)
            {
                if (CompareValues(actual ?? string.Empty, filter.Values[valueIndex]) == 0)
                {
                    return true;
                }
            }

            return false;
        }

        if (filter.Operator == SearchOperator.Contains)
        {
            return (actual ?? string.Empty).IndexOf(filter.Value ?? string.Empty, global::System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        int comparison = CompareValues(actual ?? string.Empty, filter.Value ?? string.Empty);
        switch (filter.Operator)
        {
            case SearchOperator.Equal:
                return comparison == 0;
            case SearchOperator.NotEqual:
                return comparison != 0;
            case SearchOperator.LessThan:
                return comparison < 0;
            case SearchOperator.LessThanOrEqual:
                return comparison <= 0;
            case SearchOperator.GreaterThan:
                return comparison > 0;
            case SearchOperator.GreaterThanOrEqual:
                return comparison >= 0;
            default:
                return false;
        }
    }

    internal static PatentsViewLocalDateValue ParseDate(string value)
    {
        PatentsViewLocalDateValue result = new PatentsViewLocalDateValue();
        string candidate = value.Trim();
        int year;
        int month;
        int day;
        if (candidate.Length == 10 && (candidate[4] == '-' && candidate[7] == '-' || candidate[4] == '/' && candidate[7] == '/'))
        {
            year = ReadDecimalDigits(candidate, 0, 4);
            month = ReadDecimalDigits(candidate, 5, 2);
            day = ReadDecimalDigits(candidate, 8, 2);
        }
        else if (candidate.Length == 8)
        {
            year = ReadDecimalDigits(candidate, 0, 4);
            month = ReadDecimalDigits(candidate, 4, 2);
            day = ReadDecimalDigits(candidate, 6, 2);
        }
        else
        {
            return result;
        }

        if (year < 1 || year > 9999 || month < 1 || month > 12)
        {
            return result;
        }

        if (day < 1 || day > global::System.DateTime.DaysInMonth(year, month))
        {
            return result;
        }

        result.IsValid = true;
        result.SortValue = year * 10000 + month * 100 + day;
        return result;
    }

    internal static int ReadDecimalDigits(string value, int start, int length)
    {
        int result = 0;
        int index;
        for (index = start; index < start + length; index += 1)
        {
            char character = value[index];
            if (character < '0' || character > '9')
            {
                return -1;
            }

            result = result * 10 + character - '0';
        }

        return result;
    }

    internal static PatentsViewLocalNumericValue ParseNumber(string value)
    {
        PatentsViewLocalNumericValue result = new PatentsViewLocalNumericValue();
        string candidate = value.Trim();
        if (candidate.Length == 0)
        {
            return result;
        }

        int index = 0;
        int sign = 1;
        if (candidate[index] == '-' || candidate[index] == '+')
        {
            if (candidate[index] == '-')
            {
                sign = -1;
            }

            index += 1;
        }

        global::System.Text.StringBuilder digits = new global::System.Text.StringBuilder();
        int integerDigits = 0;
        bool hasDigit = false;
        bool hasDecimalPoint = false;
        while (index < candidate.Length && candidate[index] != 'e' && candidate[index] != 'E')
        {
            char character = candidate[index];
            if (character >= '0' && character <= '9')
            {
                digits.Append(character);
                hasDigit = true;
                if (!hasDecimalPoint)
                {
                    integerDigits += 1;
                }
            }
            else if (character == '.' && !hasDecimalPoint)
            {
                hasDecimalPoint = true;
            }
            else
            {
                return result;
            }

            index += 1;
        }

        if (!hasDigit)
        {
            return result;
        }

        long exponent = 0L;
        if (index < candidate.Length)
        {
            index += 1;
            int exponentSign = 1;
            if (index < candidate.Length && (candidate[index] == '-' || candidate[index] == '+'))
            {
                if (candidate[index] == '-')
                {
                    exponentSign = -1;
                }

                index += 1;
            }

            int exponentStart = index;
            while (index < candidate.Length)
            {
                char character = candidate[index];
                if (character < '0' || character > '9')
                {
                    return result;
                }

                if (exponent <= 1000000L)
                {
                    exponent = exponent * 10L + character - '0';
                }

                index += 1;
            }

            if (index == exponentStart || exponent > 1000000L)
            {
                return result;
            }

            exponent *= exponentSign;
        }

        string digitText = digits.ToString();
        int firstNonZero = 0;
        while (firstNonZero < digitText.Length && digitText[firstNonZero] == '0')
        {
            firstNonZero += 1;
        }

        if (firstNonZero == digitText.Length)
        {
            result.IsValid = true;
            result.Sign = 0;
            result.Digits = "0";
            return result;
        }

        int lastNonZero = digitText.Length - 1;
        while (lastNonZero > firstNonZero && digitText[lastNonZero] == '0')
        {
            lastNonZero -= 1;
        }

        result.IsValid = true;
        result.Sign = sign;
        result.Digits = digitText.Substring(firstNonZero, lastNonZero - firstNonZero + 1);
        result.DecimalPosition = (long)integerDigits + exponent - firstNonZero;
        return result;
    }

    internal static int CompareNumbers(PatentsViewLocalNumericValue left, PatentsViewLocalNumericValue right)
    {
        if (left.Sign != right.Sign)
        {
            return left.Sign.CompareTo(right.Sign);
        }

        if (left.Sign == 0)
        {
            return 0;
        }

        int magnitude = left.DecimalPosition.CompareTo(right.DecimalPosition);
        if (magnitude == 0)
        {
            int length = global::System.Math.Max(left.Digits.Length, right.Digits.Length);
            int index;
            for (index = 0; index < length; index += 1)
            {
                char leftDigit = '0';
                if (index < left.Digits.Length)
                {
                    leftDigit = left.Digits[index];
                }

                char rightDigit = '0';
                if (index < right.Digits.Length)
                {
                    rightDigit = right.Digits[index];
                }

                if (leftDigit != rightDigit)
                {
                    magnitude = leftDigit.CompareTo(rightDigit);
                    break;
                }
            }
        }

        if (left.Sign < 0)
        {
            return -magnitude;
        }

        return magnitude;
    }
}
