using System;
using System.Numerics;
using System.Text.RegularExpressions;

namespace xml;

internal class XmlSchemaAssertionOperator
{
    internal int Position;
    internal string Token;

    internal XmlSchemaAssertionOperator(int position, string token)
    {
        Position = position;
        Token = token;
    }
}

internal class XmlSchemaAssertionEvaluator
{
    internal XmlSchemaSimpleTypeValidator Validator;

    internal XmlSchemaAssertionEvaluator(XmlSchemaSimpleTypeValidator validator)
    {
        Guard.NotNull(validator, "validator");
        Validator = validator;
    }

    internal bool EvaluateXmlSchemaAssertion(string expression, XmlSchemaSimpleTypeValidationResult value)
    {
        string normalized = new XdmStringDatatype().CollapseXdmStringWhitespace(expression);
        return EvaluateXmlSchemaAssertionBoolean(StripXmlSchemaAssertionParentheses(normalized), value);
    }

    internal bool EvaluateXmlSchemaAssertionBoolean(string expression, XmlSchemaSimpleTypeValidationResult value)
    {
        int logical = FindTopLevelXmlSchemaAssertionToken(expression, " or ");
        if (logical >= 0)
        {
            string left = expression.Substring(0, logical);
            string right = expression.Substring(logical + 4);
            return EvaluateXmlSchemaAssertionBoolean(StripXmlSchemaAssertionParentheses(left), value) || EvaluateXmlSchemaAssertionBoolean(StripXmlSchemaAssertionParentheses(right), value);
        }

        logical = FindTopLevelXmlSchemaAssertionToken(expression, " and ");
        if (logical >= 0)
        {
            string left = expression.Substring(0, logical);
            string right = expression.Substring(logical + 5);
            return EvaluateXmlSchemaAssertionBoolean(StripXmlSchemaAssertionParentheses(left), value) && EvaluateXmlSchemaAssertionBoolean(StripXmlSchemaAssertionParentheses(right), value);
        }

        if (expression == "true()")
        {
            return true;
        }

        if (expression == "false()")
        {
            return false;
        }

        if (expression.StartsWith("not(", StringComparison.Ordinal) && expression.EndsWith(")", StringComparison.Ordinal))
        {
            return !EvaluateXmlSchemaAssertionBoolean(StripXmlSchemaAssertionParentheses(expression.Substring(4, expression.Length - 5)), value);
        }

        if (expression == "exists($value)")
        {
            return value.AtomicValues.Length != 0;
        }

        if (expression == "empty($value)")
        {
            return value.AtomicValues.Length == 0;
        }

        if (IsXmlSchemaAssertionFunction(expression, "matches"))
        {
            string[] arguments = SplitXmlSchemaAssertionArguments(ReadXmlSchemaAssertionFunctionBody(expression));
            return arguments.Length == 2 && arguments[0] == "$value" && Regex.IsMatch(value.NormalizedValue, ReadXmlSchemaAssertionString(arguments[1]), RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(250));
        }

        if (IsXmlSchemaAssertionFunction(expression, "contains"))
        {
            string[] arguments = SplitXmlSchemaAssertionArguments(ReadXmlSchemaAssertionFunctionBody(expression));
            return arguments.Length == 2 && arguments[0] == "$value" && value.NormalizedValue.Contains(ReadXmlSchemaAssertionString(arguments[1]), StringComparison.Ordinal);
        }

        if (IsXmlSchemaAssertionFunction(expression, "starts-with"))
        {
            string[] arguments = SplitXmlSchemaAssertionArguments(ReadXmlSchemaAssertionFunctionBody(expression));
            return arguments.Length == 2 && arguments[0] == "$value" && value.NormalizedValue.StartsWith(ReadXmlSchemaAssertionString(arguments[1]), StringComparison.Ordinal);
        }

        if (IsXmlSchemaAssertionFunction(expression, "ends-with"))
        {
            string[] arguments = SplitXmlSchemaAssertionArguments(ReadXmlSchemaAssertionFunctionBody(expression));
            return arguments.Length == 2 && arguments[0] == "$value" && value.NormalizedValue.EndsWith(ReadXmlSchemaAssertionString(arguments[1]), StringComparison.Ordinal);
        }

        XmlSchemaAssertionOperator? operation = FindXmlSchemaAssertionComparison(expression);
        if (operation != null)
        {
            string left = expression.Substring(0, operation.Position).Trim();
            string right = expression.Substring(operation.Position + operation.Token.Length).Trim();
            return EvaluateXmlSchemaAssertionComparison(left, operation.Token.Trim(), right, value);
        }

        if (expression == "$value")
        {
            return EffectiveBooleanXmlSchemaAssertionValue(value);
        }

        return false;
    }

    internal bool EvaluateXmlSchemaAssertionComparison(string left, string operation, string right, XmlSchemaSimpleTypeValidationResult value)
    {
        if (left == "string-length($value)")
        {
            BigInteger length = new BigInteger(Validator.CountXmlSchemaCodepoints(value.NormalizedValue));
            XdmNumericParseResult expected = ParseXmlSchemaAssertionInteger(right);
            return expected.Success && expected.Value != null && CompareXmlSchemaAssertionIntegers(length, operation, new XdmNumericDatatype().ReadXdmIntegerValue(expected.Value));
        }

        if (left == "count($value)")
        {
            XdmNumericParseResult expected = ParseXmlSchemaAssertionInteger(right);
            return expected.Success && expected.Value != null && CompareXmlSchemaAssertionIntegers(new BigInteger(value.AtomicValues.Length), operation, new XdmNumericDatatype().ReadXdmIntegerValue(expected.Value));
        }

        int modulo = left.IndexOf(" mod ", StringComparison.Ordinal);
        if (modulo >= 0 && left.Substring(0, modulo).Trim() == "$value" && value.AtomicValue != null && Validator.Types.IsXdmAtomicSubtypeOf(value.AtomicValue.Type, XdmAtomicType.Integer))
        {
            XdmNumericParseResult parsedDivisor = ParseXmlSchemaAssertionInteger(left.Substring(modulo + 5).Trim());
            XdmNumericParseResult parsedExpected = ParseXmlSchemaAssertionInteger(right);
            if (!parsedDivisor.Success || parsedDivisor.Value == null || !parsedExpected.Success || parsedExpected.Value == null)
            {
                return false;
            }

            BigInteger divisor = new XdmNumericDatatype().ReadXdmIntegerValue(parsedDivisor.Value);
            if (divisor == BigInteger.Zero)
            {
                return false;
            }

            BigInteger actualInteger = new XdmNumericDatatype().ReadXdmIntegerValue(value.AtomicValue) % divisor;
            return CompareXmlSchemaAssertionIntegers(actualInteger, operation, new XdmNumericDatatype().ReadXdmIntegerValue(parsedExpected.Value));
        }

        if (left != "$value")
        {
            return false;
        }

        int index = 0;
        while (index < value.AtomicValues.Length)
        {
            if (CompareXmlSchemaAssertionAtomic(value.AtomicValues[index], operation, right))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool CompareXmlSchemaAssertionAtomic(XdmAtomicValue actual, string operation, string lexicalExpected)
    {
        if (Validator.Types.IsXdmStringDerivedAtomicType(actual.Type) || actual.Type == XdmAtomicType.AnyUri)
        {
            string actualValue = actual.StringValue == null ? "" : actual.StringValue;
            string expected = ReadXmlSchemaAssertionString(lexicalExpected);
            int comparison = string.CompareOrdinal(actualValue, expected);
            return ApplyXmlSchemaAssertionComparison(comparison, operation);
        }

        if (actual.Type == XdmAtomicType.Boolean)
        {
            bool expected = lexicalExpected == "true()" || lexicalExpected == "true" || lexicalExpected == "1";
            int comparison = actual.BooleanValue == expected ? 0 : actual.BooleanValue ? 1 : -1;
            return ApplyXmlSchemaAssertionComparison(comparison, operation);
        }

        if (Validator.Types.IsXdmNumericAtomicType(actual.Type))
        {
            XdmConversionResult expected = new XdmTypeConverter().TryParseXdmAtomicLexical(lexicalExpected, actual.Type, new XPathStaticContext());
            if (!expected.Success || expected.Value == null)
            {
                return false;
            }

            int comparison = new XdmNumericDatatype().CompareXdmNumeric(actual, expected.Value);
            return comparison != XdmFloatingDatatype.UnorderedComparison && ApplyXmlSchemaAssertionComparison(comparison, operation);
        }

        string actualCanonical = actual.CanonicalValue == null ? "" : actual.CanonicalValue;
        return ApplyXmlSchemaAssertionComparison(string.CompareOrdinal(actualCanonical, ReadXmlSchemaAssertionString(lexicalExpected)), operation);
    }

    internal bool CompareXmlSchemaAssertionIntegers(BigInteger left, string operation, BigInteger right)
    {
        return ApplyXmlSchemaAssertionComparison(left.CompareTo(right), operation);
    }

    internal bool ApplyXmlSchemaAssertionComparison(int comparison, string operation)
    {
        if (operation == "=" || operation == "eq")
        {
            return comparison == 0;
        }

        if (operation == "!=" || operation == "ne")
        {
            return comparison != 0;
        }

        if (operation == "<" || operation == "lt")
        {
            return comparison < 0;
        }

        if (operation == "<=" || operation == "le")
        {
            return comparison <= 0;
        }

        if (operation == ">" || operation == "gt")
        {
            return comparison > 0;
        }

        if (operation == ">=" || operation == "ge")
        {
            return comparison >= 0;
        }

        return false;
    }

    internal bool EffectiveBooleanXmlSchemaAssertionValue(XmlSchemaSimpleTypeValidationResult value)
    {
        if (value.AtomicValues.Length != 1)
        {
            return false;
        }

        XdmAtomicValue atomic = value.AtomicValues[0];
        if (atomic.Type == XdmAtomicType.Boolean)
        {
            return atomic.BooleanValue;
        }

        if (Validator.Types.IsXdmNumericAtomicType(atomic.Type))
        {
            if (atomic.Type == XdmAtomicType.Float || atomic.Type == XdmAtomicType.Double)
            {
                return atomic.FloatingValue != 0 && !double.IsNaN(atomic.FloatingValue);
            }

            return new XdmNumericDatatype().ReadXdmDecimalStorage(atomic).Coefficient != BigInteger.Zero;
        }

        string? stringValue = atomic.StringValue;
        return stringValue != null && stringValue.Length != 0;
    }

    internal XmlSchemaAssertionOperator? FindXmlSchemaAssertionComparison(string expression)
    {
        string[] operators = [" ne ", " eq ", " le ", " ge ", " lt ", " gt ", " != ", " <= ", " >= ", " = ", " < ", " > "];
        int index = 0;
        while (index < operators.Length)
        {
            int position = FindTopLevelXmlSchemaAssertionToken(expression, operators[index]);
            if (position >= 0)
            {
                return new XmlSchemaAssertionOperator(position, operators[index]);
            }

            index = index + 1;
        }

        return null;
    }

    internal int FindTopLevelXmlSchemaAssertionToken(string expression, string token)
    {
        int depth = 0;
        char quote = '\0';
        int index = 0;
        while (index <= expression.Length - token.Length)
        {
            char character = expression[index];
            if (quote != '\0')
            {
                if (character == quote)
                {
                    quote = '\0';
                }
            }
            else if (character == '\'' || character == '"')
            {
                quote = character;
            }
            else if (character == '(')
            {
                depth = depth + 1;
            }
            else if (character == ')')
            {
                depth = depth - 1;
            }
            else if (depth == 0 && string.CompareOrdinal(expression, index, token, 0, token.Length) == 0)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal string StripXmlSchemaAssertionParentheses(string expression)
    {
        string result = expression.Trim();
        while (result.Length >= 2 && result[0] == '(' && result[result.Length - 1] == ')' && FindMatchingXmlSchemaAssertionParenthesis(result, 0) == result.Length - 1)
        {
            result = result.Substring(1, result.Length - 2).Trim();
        }

        return result;
    }

    internal int FindMatchingXmlSchemaAssertionParenthesis(string expression, int opening)
    {
        int depth = 0;
        char quote = '\0';
        int index = opening;
        while (index < expression.Length)
        {
            char character = expression[index];
            if (quote != '\0')
            {
                if (character == quote)
                {
                    quote = '\0';
                }
            }
            else if (character == '\'' || character == '"')
            {
                quote = character;
            }
            else if (character == '(')
            {
                depth = depth + 1;
            }
            else if (character == ')')
            {
                depth = depth - 1;
                if (depth == 0)
                {
                    return index;
                }
            }

            index = index + 1;
        }

        return -1;
    }

    internal bool IsXmlSchemaAssertionFunction(string expression, string name)
    {
        return expression.StartsWith(name + "(", StringComparison.Ordinal) && expression.EndsWith(")", StringComparison.Ordinal);
    }

    internal string ReadXmlSchemaAssertionFunctionBody(string expression)
    {
        int opening = expression.IndexOf('(');
        return expression.Substring(opening + 1, expression.Length - opening - 2).Trim();
    }

    internal string[] SplitXmlSchemaAssertionArguments(string value)
    {
        int separator = FindTopLevelXmlSchemaAssertionToken(value, ",");
        if (separator < 0)
        {
            return [value.Trim()];
        }

        return [value.Substring(0, separator).Trim(), value.Substring(separator + 1).Trim()];
    }

    internal string ReadXmlSchemaAssertionString(string value)
    {
        string normalized = value.Trim();
        if (normalized.Length >= 2 && (normalized[0] == '\'' && normalized[normalized.Length - 1] == '\'' || normalized[0] == '"' && normalized[normalized.Length - 1] == '"'))
        {
            string quote = normalized.Substring(0, 1);
            return normalized.Substring(1, normalized.Length - 2).Replace(quote + quote, quote, StringComparison.Ordinal);
        }

        return normalized;
    }

    internal XdmNumericParseResult ParseXmlSchemaAssertionInteger(string value)
    {
        return new XdmNumericDatatype().TryParseXdmInteger(value, XdmAtomicType.Integer);
    }
}
