using System.Globalization;

namespace odf;

internal static class FormulaValueModel
{
    internal static OdfFormulaResult CreateEmpty()
    {
        OdfFormulaResult result = NewResult(OdfValueKind.Empty);
        return result;
    }

    internal static OdfFormulaResult CreateNumber(double value)
    {
        if (!double.IsFinite(value))
        {
            return CreateError(OdfFormulaError.Number);
        }
        OdfFormulaResult result = NewResult(OdfValueKind.Float);
        result.NumericValue = value;
        return result;
    }

    internal static OdfFormulaResult CreateText(string value)
    {
        if (value == null)
        {
            return CreateError(OdfFormulaError.Value);
        }
        OdfFormulaResult result = NewResult(OdfValueKind.String);
        result.StringValue = value;
        return result;
    }

    internal static OdfFormulaResult CreateBoolean(bool value)
    {
        OdfFormulaResult result = NewResult(OdfValueKind.Boolean);
        result.BooleanValue = value;
        return result;
    }

    internal static OdfFormulaResult CreateComplex(string value)
    {
        if (value == null)
        {
            return CreateError(OdfFormulaError.Value);
        }
        OdfFormulaResult result = NewResult(OdfValueKind.Complex);
        result.StringValue = value;
        return result;
    }

    internal static OdfFormulaResult CreateError(OdfFormulaError error)
    {
        OdfFormulaResult result = NewResult(OdfValueKind.Error);
        result.Error = error == OdfFormulaError.None
            ? OdfFormulaError.Value
            : error;
        result.StringValue = FormatError(result.Error);
        return result;
    }

    internal static OdfFormulaResult CreateArray(
        OdfFormulaResult[] values,
        int rowCount,
        int columnCount)
    {
        if (values == null
            || rowCount <= 0
            || columnCount <= 0
            || values.Length != rowCount * columnCount)
        {
            return CreateError(OdfFormulaError.Value);
        }
        OdfFormulaResult result = NewResult(OdfValueKind.Array);
        result.Values = CopyValues(values);
        result.RowCount = rowCount;
        result.ColumnCount = columnCount;
        return result;
    }

    internal static OdfFormulaResult CreateReference(
        OdfFormulaReference reference,
        OdfFormulaResult[] values,
        int rowCount,
        int columnCount)
    {
        if (reference == null
            || values == null
            || rowCount < 0
            || columnCount < 0
            || values.Length != rowCount * columnCount)
        {
            return CreateError(OdfFormulaError.Reference);
        }
        OdfFormulaResult result = NewResult(OdfValueKind.Reference);
        result.Reference = reference;
        result.Values = CopyValues(values);
        result.RowCount = rowCount;
        result.ColumnCount = columnCount;
        return result;
    }

    internal static OdfFormulaResult FromLiteral(OdfFormulaNode node)
    {
        if (node == null)
        {
            return CreateError(OdfFormulaError.Value);
        }
        if (node.TokenKind == OdfFormulaTokenKind.String)
        {
            return CreateText(node.Value);
        }
        if (node.TokenKind == OdfFormulaTokenKind.Boolean)
        {
            return CreateBoolean(global::System.String.Equals(
                node.Text,
                "TRUE",
                global::System.StringComparison.OrdinalIgnoreCase));
        }
        if (node.TokenKind == OdfFormulaTokenKind.Error)
        {
            return CreateError(ParseError(node.Text));
        }
        if (node.TokenKind == OdfFormulaTokenKind.Number)
        {
            return ParseNumberText(node.Text);
        }
        if (node.TokenKind == OdfFormulaTokenKind.Percentage)
        {
            string numeric = node.Text.Substring(0, node.Text.Length - 1);
            OdfFormulaResult parsed = ParseNumberText(numeric);
            if (parsed.Error != OdfFormulaError.None)
            {
                return parsed;
            }
            parsed.Kind = OdfValueKind.Percentage;
            parsed.NumericValue = parsed.NumericValue / 100.0;
            return parsed;
        }
        return CreateError(OdfFormulaError.Value);
    }

    internal static OdfFormulaResult ConvertToNumber(OdfFormulaResult value)
    {
        OdfFormulaResult scalar = ScalarValue(value);
        if (scalar.Error != OdfFormulaError.None)
        {
            return scalar;
        }
        if (IsNumericKind(scalar.Kind))
        {
            return CreateNumber(scalar.NumericValue);
        }
        if (scalar.Kind == OdfValueKind.Empty)
        {
            return CreateNumber(0);
        }
        if (scalar.Kind == OdfValueKind.Boolean)
        {
            return CreateNumber(scalar.BooleanValue ? 1 : 0);
        }
        if (scalar.Kind == OdfValueKind.String)
        {
            string text = scalar.StringValue.Trim();
            if (text.Length == 0)
            {
                return CreateNumber(0);
            }
            return ParseNumberText(text);
        }
        return CreateError(OdfFormulaError.Value);
    }

    internal static OdfFormulaResult ConvertToText(OdfFormulaResult value)
    {
        OdfFormulaResult scalar = ScalarValue(value);
        if (scalar.Error != OdfFormulaError.None)
        {
            return scalar;
        }
        if (scalar.Kind == OdfValueKind.String)
        {
            return CreateText(scalar.StringValue);
        }
        if (scalar.Kind == OdfValueKind.Empty)
        {
            return CreateText("");
        }
        if (scalar.Kind == OdfValueKind.Boolean)
        {
            return CreateText(scalar.BooleanValue ? "TRUE" : "FALSE");
        }
        if (IsNumericKind(scalar.Kind))
        {
            return CreateText(scalar.NumericValue.ToString(
                "R",
                CultureInfo.InvariantCulture));
        }
        return CreateError(OdfFormulaError.Value);
    }

    internal static OdfFormulaResult ConvertToBoolean(OdfFormulaResult value)
    {
        OdfFormulaResult scalar = ScalarValue(value);
        if (scalar.Error != OdfFormulaError.None)
        {
            return scalar;
        }
        if (scalar.Kind == OdfValueKind.Boolean)
        {
            return CreateBoolean(scalar.BooleanValue);
        }
        if (scalar.Kind == OdfValueKind.Empty)
        {
            return CreateBoolean(false);
        }
        if (IsNumericKind(scalar.Kind))
        {
            return CreateBoolean(scalar.NumericValue != 0);
        }
        if (scalar.Kind == OdfValueKind.String)
        {
            if (global::System.String.Equals(
                scalar.StringValue,
                "TRUE",
                global::System.StringComparison.OrdinalIgnoreCase))
            {
                return CreateBoolean(true);
            }
            if (global::System.String.Equals(
                scalar.StringValue,
                "FALSE",
                global::System.StringComparison.OrdinalIgnoreCase))
            {
                return CreateBoolean(false);
            }
        }
        return CreateError(OdfFormulaError.Value);
    }

    internal static OdfFormulaResult ApplyUnary(
        string operation,
        OdfFormulaResult value)
    {
        if (IsCollection(value))
        {
            OdfFormulaResult[] items = new OdfFormulaResult[value.Values.Length];
            int index = 0;
            while (index < items.Length)
            {
                items[index] = ApplyUnary(operation, value.Values[index]);
                index = index + 1;
            }
            return CreateArray(items, value.RowCount, value.ColumnCount);
        }
        OdfFormulaResult number = ConvertToNumber(value);
        if (number.Error != OdfFormulaError.None)
        {
            return number;
        }
        if (operation == "+")
        {
            return number;
        }
        if (operation == "-")
        {
            return CreateNumber(-number.NumericValue);
        }
        if (operation == "%")
        {
            return CreateNumber(number.NumericValue / 100.0);
        }
        return CreateError(OdfFormulaError.Value);
    }

    internal static OdfFormulaResult ApplyBinary(
        string operation,
        OdfFormulaResult left,
        OdfFormulaResult right)
    {
        if (left == null || right == null)
        {
            return CreateError(OdfFormulaError.Value);
        }
        if (left.Error != OdfFormulaError.None)
        {
            return CopyResult(left);
        }
        if (right.Error != OdfFormulaError.None)
        {
            return CopyResult(right);
        }
        if (IsCollection(left) || IsCollection(right))
        {
            return ApplyArrayBinary(operation, left, right);
        }
        if (operation == "&")
        {
            OdfFormulaResult leftText = ConvertToText(left);
            OdfFormulaResult rightText = ConvertToText(right);
            if (leftText.Error != OdfFormulaError.None)
            {
                return leftText;
            }
            if (rightText.Error != OdfFormulaError.None)
            {
                return rightText;
            }
            return CreateText(leftText.StringValue + rightText.StringValue);
        }
        if (IsComparisonOperator(operation))
        {
            OdfFormulaResult comparison = CompareValues(left, right, false);
            if (comparison.Error != OdfFormulaError.None)
            {
                return comparison;
            }
            return ComparisonBoolean(operation, comparison.NumericValue);
        }
        OdfFormulaResult leftNumber = ConvertToNumber(left);
        OdfFormulaResult rightNumber = ConvertToNumber(right);
        if (leftNumber.Error != OdfFormulaError.None)
        {
            return leftNumber;
        }
        if (rightNumber.Error != OdfFormulaError.None)
        {
            return rightNumber;
        }
        if (operation == "+")
        {
            return CreateNumber(leftNumber.NumericValue + rightNumber.NumericValue);
        }
        if (operation == "-")
        {
            return CreateNumber(leftNumber.NumericValue - rightNumber.NumericValue);
        }
        if (operation == "*")
        {
            return CreateNumber(leftNumber.NumericValue * rightNumber.NumericValue);
        }
        if (operation == "/")
        {
            if (rightNumber.NumericValue == 0)
            {
                return CreateError(OdfFormulaError.DivisionByZero);
            }
            return CreateNumber(leftNumber.NumericValue / rightNumber.NumericValue);
        }
        if (operation == "^")
        {
            return CreateNumber(global::System.Math.Pow(
                leftNumber.NumericValue,
                rightNumber.NumericValue));
        }
        return CreateError(OdfFormulaError.Value);
    }

    internal static OdfFormulaResult CompareValues(
        OdfFormulaResult left,
        OdfFormulaResult right,
        bool caseSensitive)
    {
        OdfFormulaResult leftScalar = ScalarValue(left);
        OdfFormulaResult rightScalar = ScalarValue(right);
        if (leftScalar.Error != OdfFormulaError.None)
        {
            return leftScalar;
        }
        if (rightScalar.Error != OdfFormulaError.None)
        {
            return rightScalar;
        }
        if (ComparableAsNumbers(leftScalar, rightScalar))
        {
            OdfFormulaResult leftNumber = ConvertToNumber(leftScalar);
            OdfFormulaResult rightNumber = ConvertToNumber(rightScalar);
            return CreateNumber(CompareNumbers(
                leftNumber.NumericValue,
                rightNumber.NumericValue));
        }
        if (ComparableAsText(leftScalar, rightScalar))
        {
            OdfFormulaResult leftText = ConvertEmptyToText(leftScalar);
            OdfFormulaResult rightText = ConvertEmptyToText(rightScalar);
            global::System.StringComparison comparison = caseSensitive
                ? global::System.StringComparison.Ordinal
                : global::System.StringComparison.OrdinalIgnoreCase;
            int compared = global::System.String.Compare(
                leftText.StringValue,
                rightText.StringValue,
                comparison);
            return CreateNumber(compared < 0 ? -1 : (compared > 0 ? 1 : 0));
        }
        if (ComparableAsBooleans(leftScalar, rightScalar))
        {
            bool leftBoolean = leftScalar.Kind == OdfValueKind.Empty
                ? false
                : leftScalar.BooleanValue;
            bool rightBoolean = rightScalar.Kind == OdfValueKind.Empty
                ? false
                : rightScalar.BooleanValue;
            return CreateNumber(leftBoolean == rightBoolean
                ? 0
                : (leftBoolean ? 1 : -1));
        }
        return CreateNumber(CompareTypeRanks(leftScalar.Kind, rightScalar.Kind));
    }

    internal static OdfFormulaResult RoundValue(
        OdfFormulaResult value,
        long digits)
    {
        return ApplyRounding(value, digits, "round");
    }

    internal static OdfFormulaResult RoundDownValue(
        OdfFormulaResult value,
        long digits)
    {
        return ApplyRounding(value, digits, "down");
    }

    internal static OdfFormulaResult RoundUpValue(
        OdfFormulaResult value,
        long digits)
    {
        return ApplyRounding(value, digits, "up");
    }

    internal static OdfFormulaResult TruncateValue(
        OdfFormulaResult value,
        long digits)
    {
        return ApplyRounding(value, digits, "truncate");
    }

    internal static OdfFormulaError ParseError(string lexical)
    {
        if (lexical == OdfFormulaNames.ErrorNull)
        {
            return OdfFormulaError.Null;
        }
        if (lexical == OdfFormulaNames.ErrorDivisionByZero)
        {
            return OdfFormulaError.DivisionByZero;
        }
        if (lexical == OdfFormulaNames.ErrorValue)
        {
            return OdfFormulaError.Value;
        }
        if (lexical == OdfFormulaNames.ErrorReference)
        {
            return OdfFormulaError.Reference;
        }
        if (lexical == OdfFormulaNames.ErrorName)
        {
            return OdfFormulaError.Name;
        }
        if (lexical == OdfFormulaNames.ErrorNumber)
        {
            return OdfFormulaError.Number;
        }
        if (lexical == OdfFormulaNames.ErrorNotAvailable)
        {
            return OdfFormulaError.NotAvailable;
        }
        return OdfFormulaError.Value;
    }

    internal static string FormatError(OdfFormulaError error)
    {
        if (error == OdfFormulaError.Null)
        {
            return OdfFormulaNames.ErrorNull;
        }
        if (error == OdfFormulaError.DivisionByZero)
        {
            return OdfFormulaNames.ErrorDivisionByZero;
        }
        if (error == OdfFormulaError.Reference)
        {
            return OdfFormulaNames.ErrorReference;
        }
        if (error == OdfFormulaError.Name
            || error == OdfFormulaError.UnsupportedFunction)
        {
            return OdfFormulaNames.ErrorName;
        }
        if (error == OdfFormulaError.Number
            || error == OdfFormulaError.CircularReference)
        {
            return OdfFormulaNames.ErrorNumber;
        }
        if (error == OdfFormulaError.NotAvailable)
        {
            return OdfFormulaNames.ErrorNotAvailable;
        }
        return OdfFormulaNames.ErrorValue;
    }

    static OdfFormulaResult NewResult(OdfValueKind kind)
    {
        OdfFormulaResult result = new OdfFormulaResult();
        result.Kind = kind;
        result.IsValid = true;
        return result;
    }

    static OdfFormulaResult ParseNumberText(string text)
    {
        OdfLexicalValue parsed = OdfLexicalParser.ParseFloat(text);
        if (!parsed.IsValid || !double.IsFinite(parsed.FloatValue))
        {
            return CreateError(OdfFormulaError.Value);
        }
        return CreateNumber(parsed.FloatValue);
    }

    static OdfFormulaResult ScalarValue(OdfFormulaResult value)
    {
        if (value == null)
        {
            return CreateError(OdfFormulaError.Value);
        }
        if (value.Error != OdfFormulaError.None)
        {
            return CopyResult(value);
        }
        if (value.Kind != OdfValueKind.Array
            && value.Kind != OdfValueKind.Reference)
        {
            return value;
        }
        if (value.Values.Length != 1)
        {
            return CreateError(value.Kind == OdfValueKind.Reference
                ? OdfFormulaError.Reference
                : OdfFormulaError.Value);
        }
        return ScalarValue(value.Values[0]);
    }

    static bool IsNumericKind(OdfValueKind kind)
    {
        return kind == OdfValueKind.Float
            || kind == OdfValueKind.Decimal
            || kind == OdfValueKind.Percentage
            || kind == OdfValueKind.Currency
            || kind == OdfValueKind.Date
            || kind == OdfValueKind.DateTime
            || kind == OdfValueKind.Time
            || kind == OdfValueKind.Duration;
    }

    static bool IsCollection(OdfFormulaResult value)
    {
        if (value == null)
        {
            return false;
        }
        if (value.Kind == OdfValueKind.Array)
        {
            return value.RowCount > 0
                && value.ColumnCount > 0
                && value.Values.Length == value.RowCount * value.ColumnCount;
        }
        return value.Kind == OdfValueKind.Reference
            && value.Values.Length > 1
            && value.RowCount > 0
            && value.ColumnCount > 0
            && value.Values.Length == value.RowCount * value.ColumnCount;
    }

    static OdfFormulaResult ApplyArrayBinary(
        string operation,
        OdfFormulaResult left,
        OdfFormulaResult right)
    {
        bool leftCollection = IsCollection(left);
        bool rightCollection = IsCollection(right);
        int rows = leftCollection ? left.RowCount : right.RowCount;
        int columns = leftCollection ? left.ColumnCount : right.ColumnCount;
        if (leftCollection
            && rightCollection
            && (left.RowCount != right.RowCount
                || left.ColumnCount != right.ColumnCount))
        {
            return CreateError(OdfFormulaError.Value);
        }
        OdfFormulaResult[] values = new OdfFormulaResult[rows * columns];
        int index = 0;
        while (index < values.Length)
        {
            OdfFormulaResult leftValue = leftCollection
                ? left.Values[index]
                : left;
            OdfFormulaResult rightValue = rightCollection
                ? right.Values[index]
                : right;
            values[index] = ApplyBinary(operation, leftValue, rightValue);
            index = index + 1;
        }
        return CreateArray(values, rows, columns);
    }

    static bool IsComparisonOperator(string operation)
    {
        return operation == "="
            || operation == "<>"
            || operation == "<"
            || operation == "<="
            || operation == ">"
            || operation == ">=";
    }

    static OdfFormulaResult ComparisonBoolean(
        string operation,
        double comparison)
    {
        if (operation == "=")
        {
            return CreateBoolean(comparison == 0);
        }
        if (operation == "<>")
        {
            return CreateBoolean(comparison != 0);
        }
        if (operation == "<")
        {
            return CreateBoolean(comparison < 0);
        }
        if (operation == "<=")
        {
            return CreateBoolean(comparison <= 0);
        }
        if (operation == ">")
        {
            return CreateBoolean(comparison > 0);
        }
        return CreateBoolean(comparison >= 0);
    }

    static bool ComparableAsNumbers(
        OdfFormulaResult left,
        OdfFormulaResult right)
    {
        return (IsNumericKind(left.Kind) || left.Kind == OdfValueKind.Empty)
            && (IsNumericKind(right.Kind) || right.Kind == OdfValueKind.Empty);
    }

    static bool ComparableAsText(
        OdfFormulaResult left,
        OdfFormulaResult right)
    {
        return (left.Kind == OdfValueKind.String
                || left.Kind == OdfValueKind.Empty)
            && (right.Kind == OdfValueKind.String
                || right.Kind == OdfValueKind.Empty);
    }

    static bool ComparableAsBooleans(
        OdfFormulaResult left,
        OdfFormulaResult right)
    {
        return (left.Kind == OdfValueKind.Boolean
                || left.Kind == OdfValueKind.Empty)
            && (right.Kind == OdfValueKind.Boolean
                || right.Kind == OdfValueKind.Empty);
    }

    static OdfFormulaResult ConvertEmptyToText(OdfFormulaResult value)
    {
        return value.Kind == OdfValueKind.Empty ? CreateText("") : value;
    }

    static int CompareNumbers(double left, double right)
    {
        return left < right ? -1 : (left > right ? 1 : 0);
    }

    static int CompareTypeRanks(OdfValueKind left, OdfValueKind right)
    {
        int leftRank = TypeRank(left);
        int rightRank = TypeRank(right);
        return leftRank < rightRank ? -1 : (leftRank > rightRank ? 1 : 0);
    }

    static int TypeRank(OdfValueKind kind)
    {
        if (IsNumericKind(kind) || kind == OdfValueKind.Empty)
        {
            return 0;
        }
        if (kind == OdfValueKind.String)
        {
            return 1;
        }
        if (kind == OdfValueKind.Boolean)
        {
            return 2;
        }
        return 3;
    }

    static OdfFormulaResult ApplyRounding(
        OdfFormulaResult value,
        long digits,
        string mode)
    {
        if (IsCollection(value))
        {
            OdfFormulaResult[] values = new OdfFormulaResult[value.Values.Length];
            int index = 0;
            while (index < values.Length)
            {
                values[index] = ApplyRounding(
                    value.Values[index],
                    digits,
                    mode);
                index = index + 1;
            }
            return CreateArray(values, value.RowCount, value.ColumnCount);
        }
        OdfFormulaResult number = ConvertToNumber(value);
        if (number.Error != OdfFormulaError.None)
        {
            return number;
        }
        if (digits > 308)
        {
            return number;
        }
        if (digits < -308)
        {
            return CreateNumber(0);
        }
        double scale = digits >= 0
            ? global::System.Math.Pow(10, digits)
            : global::System.Math.Pow(10, -digits);
        double scaled = digits >= 0
            ? number.NumericValue * scale
            : number.NumericValue / scale;
        if (!double.IsFinite(scaled))
        {
            return number;
        }
        double absolute = global::System.Math.Abs(scaled);
        double rounded;
        if (mode == "round")
        {
            rounded = global::System.Math.Floor(absolute + 0.5);
        }
        else if (mode == "up")
        {
            rounded = global::System.Math.Ceiling(absolute);
        }
        else
        {
            rounded = global::System.Math.Floor(absolute);
        }
        if (scaled < 0)
        {
            rounded = -rounded;
        }
        double result = digits >= 0 ? rounded / scale : rounded * scale;
        return CreateNumber(result);
    }

    static OdfFormulaResult[] CopyValues(OdfFormulaResult[] source)
    {
        OdfFormulaResult[] result = new OdfFormulaResult[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = source[index];
            index = index + 1;
        }
        return result;
    }

    static OdfFormulaResult CopyResult(OdfFormulaResult source)
    {
        OdfFormulaResult result = new OdfFormulaResult();
        result.Kind = source.Kind;
        result.NumericValue = source.NumericValue;
        result.StringValue = source.StringValue;
        result.BooleanValue = source.BooleanValue;
        result.Error = source.Error;
        result.Values = CopyValues(source.Values);
        result.RowCount = source.RowCount;
        result.ColumnCount = source.ColumnCount;
        result.Reference = source.Reference;
        result.IsValid = source.IsValid;
        return result;
    }
}
