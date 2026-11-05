using System.Collections.Generic;

namespace odf;

internal class FormulaDatabaseSelection
{
    internal OdfFormulaResult? Error;
    internal OdfFormulaResult Database = new OdfFormulaResult();
    internal int FieldColumn;
    internal int[] Rows = [];
}

internal static class FormulaDatabaseFunctions
{
    internal static OdfFormulaResult Evaluate(
        string name,
        OdfFormulaResult[] arguments)
    {
        FormulaDatabaseSelection selection = SelectRows(
            arguments[0],
            arguments[1],
            arguments[2],
            name == OdfFormulaNames.DCOUNT
                || name == OdfFormulaNames.DCOUNTA);
        if (selection.Error != null)
        {
            return selection.Error;
        }
        if (name == OdfFormulaNames.DGET)
        {
            if (selection.Rows.Length == 0)
            {
                return FormulaValueModel.CreateError(OdfFormulaError.Value);
            }
            if (selection.Rows.Length > 1)
            {
                return FormulaValueModel.CreateError(OdfFormulaError.Number);
            }
            return Cell(
                selection.Database,
                selection.Rows[0],
                selection.FieldColumn);
        }
        if (name == OdfFormulaNames.DCOUNT
            || name == OdfFormulaNames.DCOUNTA)
        {
            return CountValues(name, selection);
        }
        return AggregateValues(name, selection);
    }

    static FormulaDatabaseSelection SelectRows(
        OdfFormulaResult database,
        OdfFormulaResult field,
        OdfFormulaResult criteria,
        bool optionalField)
    {
        FormulaDatabaseSelection result = new FormulaDatabaseSelection();
        result.Database = database;
        if (!IsRectangularArray(database, 2)
            || !IsRectangularArray(criteria, 2))
        {
            result.Error = FormulaValueModel.CreateError(
                OdfFormulaError.Value);
            return result;
        }
        int fieldColumn = optionalField && field.Kind == OdfValueKind.Empty
            ? -1
            : FindFieldColumn(database, field);
        if (fieldColumn < 0
            && !(optionalField && field.Kind == OdfValueKind.Empty))
        {
            result.Error = FormulaValueModel.CreateError(
                OdfFormulaError.Value);
            return result;
        }
        int[] criteriaColumns = CriteriaColumns(database, criteria);
        if (criteriaColumns.Length != criteria.ColumnCount)
        {
            result.Error = FormulaValueModel.CreateError(
                OdfFormulaError.Value);
            return result;
        }
        List<int> rows = new List<int>();
        int row = 1;
        while (row < database.RowCount)
        {
            if (MatchesAnyCriteriaRow(
                database,
                row,
                criteria,
                criteriaColumns))
            {
                rows.Add(row);
            }
            row = row + 1;
        }
        result.FieldColumn = fieldColumn;
        result.Rows = rows.ToArray();
        return result;
    }

    static bool IsRectangularArray(OdfFormulaResult value, int minimumRows)
    {
        return value != null
            && value.Kind == OdfValueKind.Array
            && value.RowCount >= minimumRows
            && value.ColumnCount > 0
            && value.Values.Length == value.RowCount * value.ColumnCount;
    }

    static int FindFieldColumn(
        OdfFormulaResult database,
        OdfFormulaResult field)
    {
        OdfFormulaResult number = FormulaValueModel.ConvertToNumber(field);
        if (number.Error == OdfFormulaError.None
            && field.Kind != OdfValueKind.String)
        {
            double integral = global::System.Math.Truncate(number.NumericValue);
            if (integral >= 1 && integral <= database.ColumnCount)
            {
                return (int)integral - 1;
            }
            return -1;
        }
        OdfFormulaResult text = FormulaValueModel.ConvertToText(field);
        if (text.Error != OdfFormulaError.None)
        {
            return -1;
        }
        return FindHeader(database, text.StringValue);
    }

    static int[] CriteriaColumns(
        OdfFormulaResult database,
        OdfFormulaResult criteria)
    {
        int[] result = new int[criteria.ColumnCount];
        int index = 0;
        while (index < result.Length)
        {
            OdfFormulaResult header = Cell(criteria, 0, index);
            OdfFormulaResult text = FormulaValueModel.ConvertToText(header);
            if (text.Error != OdfFormulaError.None)
            {
                return [];
            }
            int column = FindHeader(database, text.StringValue);
            if (column < 0)
            {
                return [];
            }
            result[index] = column;
            index = index + 1;
        }
        return result;
    }

    static int FindHeader(OdfFormulaResult database, string name)
    {
        int index = 0;
        while (index < database.ColumnCount)
        {
            OdfFormulaResult header = FormulaValueModel.ConvertToText(
                Cell(database, 0, index));
            if (header.Error == OdfFormulaError.None
                && global::System.String.Equals(
                    header.StringValue,
                    name,
                    global::System.StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
            index = index + 1;
        }
        return -1;
    }

    static bool MatchesAnyCriteriaRow(
        OdfFormulaResult database,
        int databaseRow,
        OdfFormulaResult criteria,
        int[] criteriaColumns)
    {
        int criteriaRow = 1;
        while (criteriaRow < criteria.RowCount)
        {
            bool matches = true;
            int criteriaColumn = 0;
            while (criteriaColumn < criteria.ColumnCount && matches)
            {
                OdfFormulaResult criterion = Cell(
                    criteria,
                    criteriaRow,
                    criteriaColumn);
                if (criterion.Kind != OdfValueKind.Empty)
                {
                    OdfFormulaResult value = Cell(
                        database,
                        databaseRow,
                        criteriaColumns[criteriaColumn]);
                    matches = MatchesCriterion(value, criterion);
                }
                criteriaColumn = criteriaColumn + 1;
            }
            if (matches)
            {
                return true;
            }
            criteriaRow = criteriaRow + 1;
        }
        return false;
    }

    internal static bool MatchesCriterion(
        OdfFormulaResult value,
        OdfFormulaResult criterion)
    {
        if (criterion.Error != OdfFormulaError.None)
        {
            return value.Error == criterion.Error;
        }
        if (criterion.Kind != OdfValueKind.String)
        {
            OdfFormulaResult directCompared = FormulaValueModel.CompareValues(
                value,
                criterion,
                false);
            return directCompared.Error == OdfFormulaError.None
                && directCompared.NumericValue == 0;
        }
        string text = criterion.StringValue;
        string operation = "=";
        int start = 0;
        if (text.StartsWith("<=", global::System.StringComparison.Ordinal)
            || text.StartsWith(">=", global::System.StringComparison.Ordinal)
            || text.StartsWith("<>", global::System.StringComparison.Ordinal))
        {
            operation = text.Substring(0, 2);
            start = 2;
        }
        else if (text.StartsWith("=", global::System.StringComparison.Ordinal)
            || text.StartsWith("<", global::System.StringComparison.Ordinal)
            || text.StartsWith(">", global::System.StringComparison.Ordinal))
        {
            operation = text.Substring(0, 1);
            start = 1;
        }
        string operandText = text.Substring(start);
        OdfFormulaResult numericOperand =
            FormulaValueModel.ConvertToNumber(
                FormulaValueModel.CreateText(operandText));
        OdfFormulaResult compared;
        if (numericOperand.Error == OdfFormulaError.None
            && IsNumeric(value.Kind))
        {
            compared = FormulaValueModel.CompareValues(
                value,
                numericOperand,
                false);
        }
        else if (numericOperand.Error == OdfFormulaError.None)
        {
            return false;
        }
        else
        {
            OdfFormulaResult valueText = FormulaValueModel.ConvertToText(value);
            if (valueText.Error != OdfFormulaError.None)
            {
                return false;
            }
            if (operation == "=" && ContainsWildcard(operandText))
            {
                return WildcardMatches(valueText.StringValue, operandText);
            }
            compared = FormulaValueModel.CompareValues(
                valueText,
                FormulaValueModel.CreateText(operandText),
                false);
        }
        if (compared.Error != OdfFormulaError.None)
        {
            return false;
        }
        return ComparisonMatches(operation, compared.NumericValue);
    }

    static bool ComparisonMatches(string operation, double comparison)
    {
        if (operation == "=")
        {
            return comparison == 0;
        }
        if (operation == "<>")
        {
            return comparison != 0;
        }
        if (operation == "<")
        {
            return comparison < 0;
        }
        if (operation == "<=")
        {
            return comparison <= 0;
        }
        if (operation == ">")
        {
            return comparison > 0;
        }
        return comparison >= 0;
    }

    static bool ContainsWildcard(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] == '~' && index + 1 < value.Length)
            {
                index = index + 2;
            }
            else if (value[index] == '*' || value[index] == '?')
            {
                return true;
            }
            else
            {
                index = index + 1;
            }
        }
        return false;
    }

    static bool WildcardMatches(string value, string pattern)
    {
        int valueIndex = 0;
        int patternIndex = 0;
        int star = -1;
        int retry = -1;
        while (valueIndex < value.Length)
        {
            if (patternIndex < pattern.Length
                && PatternCharacterMatches(
                    pattern,
                    patternIndex,
                    value[valueIndex]))
            {
                patternIndex = NextPatternIndex(pattern, patternIndex);
                valueIndex = valueIndex + 1;
            }
            else if (patternIndex < pattern.Length
                && pattern[patternIndex] == '*')
            {
                star = patternIndex;
                patternIndex = patternIndex + 1;
                retry = valueIndex;
            }
            else if (star >= 0)
            {
                patternIndex = star + 1;
                retry = retry + 1;
                valueIndex = retry;
            }
            else
            {
                return false;
            }
        }
        while (patternIndex < pattern.Length && pattern[patternIndex] == '*')
        {
            patternIndex = patternIndex + 1;
        }
        return patternIndex == pattern.Length;
    }

    static bool PatternCharacterMatches(
        string pattern,
        int index,
        char value)
    {
        if (pattern[index] == '?')
        {
            return true;
        }
        char expected = pattern[index];
        if (expected == '~' && index + 1 < pattern.Length)
        {
            expected = pattern[index + 1];
        }
        return global::System.Char.ToUpperInvariant(expected)
            == global::System.Char.ToUpperInvariant(value);
    }

    static int NextPatternIndex(string pattern, int index)
    {
        return pattern[index] == '~' && index + 1 < pattern.Length
            ? index + 2
            : index + 1;
    }

    static OdfFormulaResult CountValues(
        string name,
        FormulaDatabaseSelection selection)
    {
        if (selection.FieldColumn < 0)
        {
            return FormulaValueModel.CreateNumber(selection.Rows.Length);
        }
        int count = 0;
        int index = 0;
        while (index < selection.Rows.Length)
        {
            OdfFormulaResult value = Cell(
                selection.Database,
                selection.Rows[index],
                selection.FieldColumn);
            if ((name == OdfFormulaNames.DCOUNT && IsNumeric(value.Kind))
                || (name == OdfFormulaNames.DCOUNTA
                    && value.Kind != OdfValueKind.Empty))
            {
                count = count + 1;
            }
            index = index + 1;
        }
        return FormulaValueModel.CreateNumber(count);
    }

    static OdfFormulaResult AggregateValues(
        string name,
        FormulaDatabaseSelection selection)
    {
        int count = 0;
        double sum = 0;
        double product = 1;
        double minimum = 0;
        double maximum = 0;
        double mean = 0;
        double squared = 0;
        int index = 0;
        while (index < selection.Rows.Length)
        {
            OdfFormulaResult value = Cell(
                selection.Database,
                selection.Rows[index],
                selection.FieldColumn);
            if (IsNumeric(value.Kind))
            {
                count = count + 1;
                sum = sum + value.NumericValue;
                product = product * value.NumericValue;
                if (count == 1 || value.NumericValue < minimum)
                {
                    minimum = value.NumericValue;
                }
                if (count == 1 || value.NumericValue > maximum)
                {
                    maximum = value.NumericValue;
                }
                double delta = value.NumericValue - mean;
                mean = mean + delta / count;
                squared = squared + delta * (value.NumericValue - mean);
            }
            index = index + 1;
        }
        if (name == OdfFormulaNames.DSUM)
        {
            return FormulaValueModel.CreateNumber(sum);
        }
        if (name == OdfFormulaNames.DPRODUCT)
        {
            return FormulaValueModel.CreateNumber(count == 0 ? 0 : product);
        }
        if (name == OdfFormulaNames.DMAX)
        {
            return FormulaValueModel.CreateNumber(count == 0 ? 0 : maximum);
        }
        if (name == OdfFormulaNames.DMIN)
        {
            return FormulaValueModel.CreateNumber(count == 0 ? 0 : minimum);
        }
        if (name == OdfFormulaNames.DAVERAGE)
        {
            return count == 0
                ? FormulaValueModel.CreateError(
                    OdfFormulaError.DivisionByZero)
                : FormulaValueModel.CreateNumber(sum / count);
        }
        bool sample = name == OdfFormulaNames.DSTDEV
            || name == OdfFormulaNames.DVAR;
        if (count == 0 || (sample && count < 2))
        {
            return FormulaValueModel.CreateError(
                OdfFormulaError.DivisionByZero);
        }
        double variance = squared / (sample ? count - 1 : count);
        if (name == OdfFormulaNames.DVAR
            || name == OdfFormulaNames.DVARP)
        {
            return FormulaValueModel.CreateNumber(variance);
        }
        return FormulaValueModel.CreateNumber(
            global::System.Math.Sqrt(variance));
    }

    static OdfFormulaResult Cell(
        OdfFormulaResult array,
        int row,
        int column)
    {
        return array.Values[row * array.ColumnCount + column];
    }

    static bool IsNumeric(OdfValueKind kind)
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
}
