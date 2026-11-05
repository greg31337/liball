using System.Text;

namespace odf;

internal static class FormulaLookupFunctions
{
    internal static OdfFormulaResult Evaluate(
        string name,
        OdfFormulaResult[] arguments,
        FormulaFunctionContext context)
    {
        if (name == OdfFormulaNames.ADDRESS)
        {
            return Address(arguments);
        }
        if (name == OdfFormulaNames.CHOOSE)
        {
            return Choose(arguments);
        }
        if (name == OdfFormulaNames.HLOOKUP)
        {
            return TableLookup(arguments, true, context);
        }
        if (name == OdfFormulaNames.VLOOKUP)
        {
            return TableLookup(arguments, false, context);
        }
        if (name == OdfFormulaNames.INDEX)
        {
            return Index(arguments, context);
        }
        if (name == OdfFormulaNames.LOOKUP)
        {
            return Lookup(arguments, context);
        }
        if (name == OdfFormulaNames.MATCH)
        {
            return Match(arguments, context);
        }
        if (name == OdfFormulaNames.OFFSET)
        {
            return Offset(arguments, context);
        }
        if (name == OdfFormulaNames.GETPIVOTDATA
            && arguments.Length > 2
            && arguments.Length % 2 != 0)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        if (name == OdfFormulaNames.MULTIPLE_OPERATIONS
            && arguments.Length == 4)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        return ResolveWorkbookFunction(name, arguments, context);
    }

    static OdfFormulaResult Address(OdfFormulaResult[] arguments)
    {
        OdfFormulaResult rowValue = FormulaValueModel.ConvertToNumber(
            arguments[0]);
        OdfFormulaResult columnValue = FormulaValueModel.ConvertToNumber(
            arguments[1]);
        if (rowValue.Error != OdfFormulaError.None)
        {
            return rowValue;
        }
        if (columnValue.Error != OdfFormulaError.None)
        {
            return columnValue;
        }
        int row = (int)global::System.Math.Truncate(rowValue.NumericValue);
        int column = (int)global::System.Math.Truncate(
            columnValue.NumericValue);
        if (row <= 0 || column <= 0)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        int absoluteMode = 1;
        if (arguments.Length > 2)
        {
            OdfFormulaResult mode = FormulaValueModel.ConvertToNumber(
                arguments[2]);
            if (mode.Error != OdfFormulaError.None)
            {
                return mode;
            }
            absoluteMode = (int)global::System.Math.Truncate(
                mode.NumericValue);
        }
        if (absoluteMode < 1 || absoluteMode > 4)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        bool a1Style = true;
        if (arguments.Length > 3)
        {
            OdfFormulaResult style = FormulaValueModel.ConvertToBoolean(
                arguments[3]);
            if (style.Error != OdfFormulaError.None)
            {
                return style;
            }
            a1Style = style.BooleanValue;
        }
        string sheet = "";
        if (arguments.Length > 4)
        {
            OdfFormulaResult sheetValue = FormulaValueModel.ConvertToText(
                arguments[4]);
            if (sheetValue.Error != OdfFormulaError.None)
            {
                return sheetValue;
            }
            sheet = FormatSheetName(sheetValue.StringValue, a1Style);
        }
        bool absoluteRow = absoluteMode == 1 || absoluteMode == 2;
        bool absoluteColumn = absoluteMode == 1 || absoluteMode == 3;
        string address = a1Style
            ? FormatA1Address(row, column, absoluteRow, absoluteColumn)
            : FormatR1C1Address(
                row,
                column,
                absoluteRow,
                absoluteColumn);
        return FormulaValueModel.CreateText(sheet + address);
    }

    static OdfFormulaResult Choose(OdfFormulaResult[] arguments)
    {
        OdfFormulaResult indexValue = FormulaValueModel.ConvertToNumber(
            arguments[0]);
        if (indexValue.Error != OdfFormulaError.None)
        {
            return indexValue;
        }
        int index = (int)global::System.Math.Truncate(
            indexValue.NumericValue);
        if (index < 1 || index >= arguments.Length)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        return arguments[index];
    }

    static OdfFormulaResult TableLookup(
        OdfFormulaResult[] arguments,
        bool horizontal,
        FormulaFunctionContext context)
    {
        OdfFormulaResult table = arguments[1];
        if (!HasTableShape(table))
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        OdfFormulaResult indexValue = FormulaValueModel.ConvertToNumber(
            arguments[2]);
        if (indexValue.Error != OdfFormulaError.None)
        {
            return indexValue;
        }
        int resultIndex = (int)global::System.Math.Truncate(
            indexValue.NumericValue);
        int limit = horizontal ? table.RowCount : table.ColumnCount;
        if (resultIndex < 1 || resultIndex > limit)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Reference);
        }
        bool approximate = true;
        if (arguments.Length > 3)
        {
            OdfFormulaResult rangeLookup = FormulaValueModel.ConvertToBoolean(
                arguments[3]);
            if (rangeLookup.Error != OdfFormulaError.None)
            {
                return rangeLookup;
            }
            approximate = rangeLookup.BooleanValue;
        }
        int searchLength = horizontal
            ? table.ColumnCount
            : table.RowCount;
        int found = FindLookupIndex(
            arguments[0],
            table,
            horizontal,
            approximate,
            searchLength,
            context);
        if (found < 0)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.NotAvailable);
        }
        int valueIndex = horizontal
            ? (resultIndex - 1) * table.ColumnCount + found
            : found * table.ColumnCount + resultIndex - 1;
        return table.Values[valueIndex];
    }

    static OdfFormulaResult Index(
        OdfFormulaResult[] arguments,
        FormulaFunctionContext context)
    {
        OdfFormulaResult source = arguments[0];
        if (!HasTableShape(source))
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        int row = arguments.Length > 1
            ? ReadOptionalIndex(arguments[1])
            : 0;
        int column = arguments.Length > 2
            ? ReadOptionalIndex(arguments[2])
            : 0;
        int area = arguments.Length > 3
            && arguments[3].Kind != OdfValueKind.Empty
            ? ReadOptionalIndex(arguments[3])
            : 1;
        if (row < 0 || column < 0 || area < 1)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        if (area != 1)
        {
            if (source.Reference != null
                && source.Reference.Kind
                    == OdfFormulaNodeKind.ReferenceConcatenation)
            {
                return ResolveWorkbookFunction(
                    OdfFormulaNames.INDEX,
                    arguments,
                    context);
            }
            return FormulaValueModel.CreateError(OdfFormulaError.Reference);
        }
        if (row > source.RowCount || column > source.ColumnCount)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Reference);
        }
        if (row > 0 && column > 0)
        {
            return source.Values[(row - 1) * source.ColumnCount + column - 1];
        }
        if (row == 0 && column == 0)
        {
            return source;
        }
        if (row == 0)
        {
            OdfFormulaResult[] values =
                new OdfFormulaResult[source.RowCount];
            int index = 0;
            while (index < source.RowCount)
            {
                values[index] = source.Values[
                    index * source.ColumnCount + column - 1];
                index = index + 1;
            }
            return CreateIndexedCollection(
                source,
                values,
                source.RowCount,
                1,
                0,
                column - 1);
        }
        OdfFormulaResult[] rowValues =
            new OdfFormulaResult[source.ColumnCount];
        int rowIndex = 0;
        while (rowIndex < source.ColumnCount)
        {
            rowValues[rowIndex] = source.Values[
                (row - 1) * source.ColumnCount + rowIndex];
            rowIndex = rowIndex + 1;
        }
        return CreateIndexedCollection(
            source,
            rowValues,
            1,
            source.ColumnCount,
            row - 1,
            0);
    }

    static OdfFormulaResult Lookup(
        OdfFormulaResult[] arguments,
        FormulaFunctionContext context)
    {
        OdfFormulaResult searched = arguments[1];
        if (!HasTableShape(searched))
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        if (arguments.Length > 2)
        {
            OdfFormulaResult results = arguments[2];
            if (!IsVector(results))
            {
                return FormulaValueModel.CreateError(OdfFormulaError.Value);
            }
            bool searchedHorizontally =
                searched.ColumnCount > searched.RowCount;
            int searchedLength = searchedHorizontally
                ? searched.ColumnCount
                : searched.RowCount;
            int found = FindLookupIndex(
                arguments[0],
                searched,
                searchedHorizontally,
                true,
                searchedLength,
                context);
            if (found < 0 || found >= results.Values.Length)
            {
                return FormulaValueModel.CreateError(
                    OdfFormulaError.NotAvailable);
            }
            return results.Values[found];
        }
        bool horizontal = searched.ColumnCount > searched.RowCount;
        int length = horizontal
            ? searched.ColumnCount
            : searched.RowCount;
        int index = FindLookupIndex(
            arguments[0],
            searched,
            horizontal,
            true,
            length,
            context);
        if (index < 0)
        {
            return FormulaValueModel.CreateError(
                OdfFormulaError.NotAvailable);
        }
        int resultIndex = horizontal
            ? (searched.RowCount - 1) * searched.ColumnCount + index
            : index * searched.ColumnCount + searched.ColumnCount - 1;
        return searched.Values[resultIndex];
    }

    static OdfFormulaResult Match(
        OdfFormulaResult[] arguments,
        FormulaFunctionContext context)
    {
        OdfFormulaResult searched = arguments[1];
        if (!IsVector(searched))
        {
            return FormulaValueModel.CreateError(OdfFormulaError.NotAvailable);
        }
        int matchType = 1;
        if (arguments.Length > 2)
        {
            OdfFormulaResult typeValue = FormulaValueModel.ConvertToNumber(
                arguments[2]);
            if (typeValue.Error != OdfFormulaError.None)
            {
                return typeValue;
            }
            matchType = (int)global::System.Math.Truncate(
                typeValue.NumericValue);
        }
        if (matchType != -1 && matchType != 0 && matchType != 1)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        int index = FindVectorIndex(
            arguments[0],
            searched,
            matchType,
            context);
        return index < 0
            ? FormulaValueModel.CreateError(OdfFormulaError.NotAvailable)
            : FormulaValueModel.CreateNumber(index + 1);
    }

    static OdfFormulaResult Offset(
        OdfFormulaResult[] arguments,
        FormulaFunctionContext context)
    {
        OdfFormulaResult source = arguments[0];
        if (source.Kind != OdfValueKind.Reference
            || source.Reference == null
            || !HasTableShape(source))
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Reference);
        }
        OdfFormulaResult rowValue = FormulaValueModel.ConvertToNumber(
            arguments[1]);
        OdfFormulaResult columnValue = FormulaValueModel.ConvertToNumber(
            arguments[2]);
        if (rowValue.Error != OdfFormulaError.None)
        {
            return rowValue;
        }
        if (columnValue.Error != OdfFormulaError.None)
        {
            return columnValue;
        }
        int rowOffset = (int)global::System.Math.Truncate(
            rowValue.NumericValue);
        int columnOffset = (int)global::System.Math.Truncate(
            columnValue.NumericValue);
        int height = source.RowCount;
        int width = source.ColumnCount;
        if (arguments.Length > 3
            && arguments[3].Kind != OdfValueKind.Empty)
        {
            height = ReadOptionalIndex(arguments[3]);
        }
        if (arguments.Length > 4
            && arguments[4].Kind != OdfValueKind.Empty)
        {
            width = ReadOptionalIndex(arguments[4]);
        }
        if (height <= 0 || width <= 0)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Reference);
        }
        global::System.Numerics.BigInteger targetRow =
            source.Reference.Start.RowNumber + rowOffset;
        global::System.Numerics.BigInteger targetColumn =
            source.Reference.Start.ColumnNumber + columnOffset;
        if (targetRow < 1 || targetColumn < 1)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Reference);
        }
        if (rowOffset < 0
            || columnOffset < 0
            || rowOffset + height > source.RowCount
            || columnOffset + width > source.ColumnCount)
        {
            return ResolveWorkbookFunction(
                OdfFormulaNames.OFFSET,
                arguments,
                context);
        }
        OdfFormulaResult[] values = new OdfFormulaResult[height * width];
        int row = 0;
        while (row < height)
        {
            int column = 0;
            while (column < width)
            {
                values[row * width + column] = source.Values[
                    (rowOffset + row) * source.ColumnCount
                    + columnOffset + column];
                column = column + 1;
            }
            row = row + 1;
        }
        OdfFormulaReference reference = CreateOffsetReference(
            source.Reference,
            targetRow,
            targetColumn,
            height,
            width);
        return FormulaValueModel.CreateReference(
            reference,
            values,
            height,
            width);
    }

    static OdfFormulaResult ResolveWorkbookFunction(
        string name,
        OdfFormulaResult[] arguments,
        FormulaFunctionContext context)
    {
        if (context.LookupResolver == null)
        {
            return FormulaValueModel.CreateError(
                OdfFormulaError.NotAvailable);
        }
        return context.LookupResolver.ResolveLookupFunction(name, arguments);
    }

    static int FindLookupIndex(
        OdfFormulaResult sought,
        OdfFormulaResult table,
        bool horizontal,
        bool approximate,
        int length,
        FormulaFunctionContext context)
    {
        int candidate = -1;
        int index = 0;
        while (index < length)
        {
            OdfFormulaResult value = horizontal
                ? table.Values[index]
                : table.Values[index * table.ColumnCount];
            OdfFormulaResult compared = FormulaValueModel.CompareValues(
                value,
                sought,
                false);
            if (compared.Error != OdfFormulaError.None)
            {
                return -1;
            }
            bool equal = compared.NumericValue == 0;
            if (!approximate && ExactMatches(value, sought, context))
            {
                return index;
            }
            if (approximate && equal)
            {
                candidate = index;
            }
            if (approximate && compared.NumericValue < 0)
            {
                candidate = index;
            }
            if (approximate && compared.NumericValue > 0)
            {
                return IsCompatibleApproximateMatch(
                    sought,
                    table,
                    horizontal,
                    candidate)
                    ? candidate
                    : -1;
            }
            index = index + 1;
        }
        return approximate && IsCompatibleApproximateMatch(
            sought,
            table,
            horizontal,
            candidate)
            ? candidate
            : -1;
    }

    static int FindVectorIndex(
        OdfFormulaResult sought,
        OdfFormulaResult vector,
        int matchType,
        FormulaFunctionContext context)
    {
        int candidate = -1;
        int index = 0;
        while (index < vector.Values.Length)
        {
            OdfFormulaResult compared = FormulaValueModel.CompareValues(
                vector.Values[index],
                sought,
                false);
            if (compared.Error != OdfFormulaError.None)
            {
                return -1;
            }
            if (matchType == 0
                && ExactMatches(vector.Values[index], sought, context))
            {
                return index;
            }
            if (matchType != 0 && compared.NumericValue == 0)
            {
                candidate = index;
            }
            if (matchType == 1 && compared.NumericValue < 0)
            {
                candidate = index;
            }
            if (matchType == 1 && compared.NumericValue > 0)
            {
                return IsCompatibleVectorMatch(
                    sought,
                    vector,
                    candidate,
                    matchType)
                    ? candidate
                    : -1;
            }
            if (matchType == -1 && compared.NumericValue > 0)
            {
                candidate = index;
            }
            if (matchType == -1 && compared.NumericValue < 0)
            {
                return IsCompatibleVectorMatch(
                    sought,
                    vector,
                    candidate,
                    matchType)
                    ? candidate
                    : -1;
            }
            index = index + 1;
        }
        return matchType != 0 && IsCompatibleVectorMatch(
            sought,
            vector,
            candidate,
            matchType)
            ? candidate
            : -1;
    }

    static int ReadOptionalIndex(OdfFormulaResult value)
    {
        if (value.Kind == OdfValueKind.Empty)
        {
            return 0;
        }
        OdfFormulaResult number = FormulaValueModel.ConvertToNumber(value);
        if (number.Error != OdfFormulaError.None
            || !double.IsFinite(number.NumericValue))
        {
            return -1;
        }
        return (int)global::System.Math.Truncate(number.NumericValue);
    }

    static bool HasTableShape(OdfFormulaResult value)
    {
        if (value == null)
        {
            return false;
        }
        return (value.Kind == OdfValueKind.Array
            || value.Kind == OdfValueKind.Reference)
            && value.RowCount > 0
            && value.ColumnCount > 0
            && value.Values.Length == value.RowCount * value.ColumnCount;
    }

    static bool IsVector(OdfFormulaResult value)
    {
        return HasTableShape(value)
            && (value.RowCount == 1 || value.ColumnCount == 1);
    }

    static OdfFormulaResult CreateIndexedCollection(
        OdfFormulaResult source,
        OdfFormulaResult[] values,
        int rows,
        int columns,
        int rowOffset,
        int columnOffset)
    {
        if (source.Kind != OdfValueKind.Reference
            || source.Reference == null)
        {
            return FormulaValueModel.CreateArray(values, rows, columns);
        }
        OdfFormulaReference reference = CreateOffsetReference(
            source.Reference,
            source.Reference.Start.RowNumber + rowOffset,
            source.Reference.Start.ColumnNumber + columnOffset,
            rows,
            columns);
        return FormulaValueModel.CreateReference(
            reference,
            values,
            rows,
            columns);
    }

    static OdfFormulaReference CreateOffsetReference(
        OdfFormulaReference source,
        global::System.Numerics.BigInteger row,
        global::System.Numerics.BigInteger column,
        int height,
        int width)
    {
        OdfFormulaReference result = new OdfFormulaReference();
        result.Kind = height == 1 && width == 1
            ? OdfFormulaNodeKind.Reference
            : OdfFormulaNodeKind.Range;
        result.Source = source.Source;
        result.HasSource = source.HasSource;
        result.Start = CopyEndpoint(source.Start);
        result.Start.RowNumber = row;
        result.Start.ColumnNumber = column;
        result.Start.ColumnName = FormatColumnName((int)column);
        result.End = CopyEndpoint(source.HasEnd ? source.End : source.Start);
        result.End.RowNumber = row + height - 1;
        result.End.ColumnNumber = column + width - 1;
        result.End.ColumnName = FormatColumnName(
            (int)(column + width - 1));
        result.HasEnd = height != 1 || width != 1;
        result.IsThreeDimensional = source.IsThreeDimensional;
        return result;
    }

    static OdfFormulaReferenceEndpoint CopyEndpoint(
        OdfFormulaReferenceEndpoint source)
    {
        OdfFormulaReferenceEndpoint result =
            new OdfFormulaReferenceEndpoint();
        result.SheetName = source.SheetName;
        result.EffectiveSheetName = source.EffectiveSheetName;
        result.ColumnName = source.ColumnName;
        result.ColumnNumber = source.ColumnNumber;
        result.RowNumber = source.RowNumber;
        result.HasSheet = source.HasSheet;
        result.HasColumn = source.HasColumn;
        result.HasRow = source.HasRow;
        result.SheetAbsolute = source.SheetAbsolute;
        result.ColumnAbsolute = source.ColumnAbsolute;
        result.RowAbsolute = source.RowAbsolute;
        return result;
    }

    static string FormatA1Address(
        int row,
        int column,
        bool absoluteRow,
        bool absoluteColumn)
    {
        string columnName = FormatColumnName(column);
        return (absoluteColumn ? "$" : "")
            + columnName
            + (absoluteRow ? "$" : "")
            + row.ToString(global::System.Globalization.CultureInfo.InvariantCulture);
    }

    static string FormatR1C1Address(
        int row,
        int column,
        bool absoluteRow,
        bool absoluteColumn)
    {
        int rowValue = row;
        int columnValue = column;
        string rowText = absoluteRow
            ? rowValue.ToString(
                global::System.Globalization.CultureInfo.InvariantCulture)
            : "[" + rowValue.ToString(
                global::System.Globalization.CultureInfo.InvariantCulture) + "]";
        string columnText = absoluteColumn
            ? columnValue.ToString(
                global::System.Globalization.CultureInfo.InvariantCulture)
            : "[" + columnValue.ToString(
                global::System.Globalization.CultureInfo.InvariantCulture) + "]";
        return "R" + rowText + "C" + columnText;
    }

    static string FormatColumnName(int column)
    {
        StringBuilder result = new StringBuilder();
        int remaining = column;
        while (remaining > 0)
        {
            int digit = (remaining - 1) % 26;
            result.Insert(0, (char)('A' + digit));
            remaining = (remaining - 1) / 26;
        }
        return result.ToString();
    }

    static string FormatSheetName(string sheet, bool a1Style)
    {
        if (sheet.Length == 0)
        {
            return "";
        }
        string escaped = sheet.Replace("'", "''");
        return "$'" + escaped + "'" + (a1Style ? "." : "!");
    }

    static bool ExactMatches(
        OdfFormulaResult value,
        OdfFormulaResult sought,
        FormulaFunctionContext context)
    {
        if (context.SpreadsheetSettings.UseWildcards
            && sought.Kind == OdfValueKind.String
            && HasWildcard(sought.StringValue))
        {
            return FormulaDatabaseFunctions.MatchesCriterion(value, sought);
        }
        OdfFormulaResult compared = FormulaValueModel.CompareValues(
            value,
            sought,
            false);
        return compared.Error == OdfFormulaError.None
            && compared.NumericValue == 0;
    }

    static bool HasWildcard(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if ((value[index] == '*' || value[index] == '?')
                && (index == 0 || value[index - 1] != '~'))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool IsCompatibleApproximateMatch(
        OdfFormulaResult sought,
        OdfFormulaResult table,
        bool horizontal,
        int candidate)
    {
        if (candidate < 0)
        {
            return false;
        }
        OdfFormulaResult value = horizontal
            ? table.Values[candidate]
            : table.Values[candidate * table.ColumnCount];
        return !(sought.Kind == OdfValueKind.String
            && IsNumeric(value.Kind));
    }

    static bool IsCompatibleVectorMatch(
        OdfFormulaResult sought,
        OdfFormulaResult vector,
        int candidate,
        int matchType)
    {
        if (candidate < 0)
        {
            return false;
        }
        OdfFormulaResult value = vector.Values[candidate];
        if (matchType == 1)
        {
            return !(sought.Kind == OdfValueKind.String
                && IsNumeric(value.Kind));
        }
        return !(IsNumeric(sought.Kind)
            && value.Kind == OdfValueKind.String);
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
