using System.Text;

namespace odf;

internal interface IFormulaExternalResourceResolver
{
    OdfFormulaResult ResolveExternalFunction(
        string functionName,
        OdfFormulaResult[] arguments);
}

internal interface IFormulaLookupResourceResolver
{
    OdfFormulaResult ResolveLookupFunction(
        string functionName,
        OdfFormulaResult[] arguments);
}

internal class FormulaFunctionContext
{
    internal IFormulaExternalResourceResolver? ExternalResolver;
    internal IFormulaLookupResourceResolver? LookupResolver;
    internal OdfFormulaResult? CurrentValue;
    internal OdfFormulaResult? CurrentDateTime;
    internal OdfSpreadsheetSettings SpreadsheetSettings =
        new OdfSpreadsheetSettings();
    internal string CurrentAddress = "";
    internal string ReferencedFormula = "";
    internal string FileName = "";
    internal string Directory = "";
    internal string OperatingSystem = "";
    internal string Release = "";
    internal string RecalculationMode = "Automatic";
    internal string Origin = "";
    internal string DecimalSeparator = ".";
    internal string GroupSeparator = ",";
    internal string CellFormat = "G";
    internal string CellPrefix = "";
    internal string[] SheetNames = [];
    internal int[] SheetNumbers = [];
    internal int CurrentRowNumber;
    internal int CurrentColumnNumber;
    internal int CurrentSheetNumber;
    internal int ReferenceRowNumber;
    internal int ReferenceColumnNumber;
    internal int ReferenceSheetNumber;
    internal int ActiveWorksheetCount;
    internal int SheetCount;
    internal int ReferenceSheetCount;
    internal double AvailableMemory;
    internal double UsedMemory;
    internal double TotalMemory;
    internal double CellWidth;
    internal double RandomValue = -1;
    internal bool CellUsesNegativeColor;
    internal bool CellProtected;
    internal bool CellUsesParentheses;
}

internal static class FormulaFunctionEvaluator
{
    internal static OdfFormulaResult Evaluate(
        string name,
        OdfFormulaResult[] arguments,
        FormulaFunctionContext context)
    {
        if (name == null || arguments == null || context == null)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        OdfFormulaFunctionInfo? info = FormulaFunctionCatalog.Find(name);
        if (info == null)
        {
            return FormulaValueModel.CreateError(
                OdfFormulaError.UnsupportedFunction);
        }
        if (arguments.Length < info.MinimumArguments
            || arguments.Length > info.MaximumArguments)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        string canonical = info.Name;
        if (info.Group == "Database")
        {
            return FormulaDatabaseFunctions.Evaluate(canonical, arguments);
        }
        if (info.Group == "External")
        {
            return EvaluateExternal(canonical, arguments, context);
        }
        if (info.Group == "DateTime")
        {
            return FormulaDateTimeFunctions.Evaluate(
                canonical,
                arguments,
                context);
        }
        if (info.Group == "Logical")
        {
            return FormulaLogicalFunctions.Evaluate(canonical, arguments);
        }
        if (info.Group == "Financial")
        {
            return FormulaFinancialFunctions.Evaluate(
                canonical,
                arguments,
                context.SpreadsheetSettings);
        }
        if (info.Group == "Matrix")
        {
            return FormulaMatrixFunctions.Evaluate(canonical, arguments);
        }
        if (info.Group == "Lookup")
        {
            return FormulaLookupFunctions.Evaluate(
                canonical,
                arguments,
                context);
        }
        if (info.Group == "Bit")
        {
            return FormulaBitFunctions.Evaluate(canonical, arguments);
        }
        if (info.Group == "Complex")
        {
            return FormulaComplexFunctions.Evaluate(canonical, arguments);
        }
        if (info.Group == "Mathematical")
        {
            return FormulaMathematicalFunctions.Evaluate(
                canonical,
                arguments,
                context);
        }
        if (info.Group == "Rounding")
        {
            return FormulaRoundingFunctions.Evaluate(canonical, arguments);
        }
        if (info.Group == "Conversion")
        {
            return FormulaRepresentationFunctions.Evaluate(
                canonical,
                arguments);
        }
        if (info.Group == "Statistical")
        {
            return FormulaStatisticalFunctions.Evaluate(
                canonical,
                arguments);
        }
        if (info.Group == "Legacy")
        {
            return FormulaStatisticalFunctions.EvaluateLegacy(
                canonical,
                arguments);
        }
        if (info.Group == "Text")
        {
            return FormulaTextFunctions.Evaluate(
                canonical,
                arguments,
                context);
        }
        return EvaluateInformation(canonical, arguments, context);
    }

    static OdfFormulaResult EvaluateExternal(
        string name,
        OdfFormulaResult[] arguments,
        FormulaFunctionContext context)
    {
        if (name == OdfFormulaNames.HYPERLINK)
        {
            OdfFormulaResult display = arguments.Length > 1
                ? arguments[1]
                : arguments[0];
            return FormulaValueModel.ConvertToText(display);
        }
        if (context.ExternalResolver == null)
        {
            return FormulaValueModel.CreateError(
                OdfFormulaError.NotAvailable);
        }
        return context.ExternalResolver.ResolveExternalFunction(
            name,
            arguments);
    }

    static OdfFormulaResult EvaluateInformation(
        string name,
        OdfFormulaResult[] arguments,
        FormulaFunctionContext context)
    {
        OdfFormulaResult value = arguments.Length > 0
            ? arguments[0]
            : FormulaValueModel.CreateEmpty();
        if (name == OdfFormulaNames.AREAS)
        {
            return AreaCount(value);
        }
        if (name == OdfFormulaNames.COLUMN)
        {
            return CoordinateNumbers(false, arguments, context);
        }
        if (name == OdfFormulaNames.COLUMNS)
        {
            return DimensionCount(value, false);
        }
        if (name == OdfFormulaNames.COUNT
            || name == OdfFormulaNames.COUNTA)
        {
            return CountArguments(name, arguments);
        }
        if (name == OdfFormulaNames.COUNTBLANK)
        {
            return CountBlank(value);
        }
        if (name == OdfFormulaNames.COUNTIF)
        {
            return CountIf(arguments);
        }
        if (name == OdfFormulaNames.COUNTIFS)
        {
            return CountIfs(arguments);
        }
        OdfFormulaResult scalar = Dereference(value);
        if (name == OdfFormulaNames.ISBLANK)
        {
            return FormulaValueModel.CreateBoolean(
                scalar.Kind == OdfValueKind.Empty);
        }
        if (name == OdfFormulaNames.ISERR)
        {
            return FormulaValueModel.CreateBoolean(
                scalar.Error != OdfFormulaError.None
                && scalar.Error != OdfFormulaError.NotAvailable);
        }
        if (name == OdfFormulaNames.ISERROR)
        {
            return FormulaValueModel.CreateBoolean(
                scalar.Error != OdfFormulaError.None);
        }
        if (name == OdfFormulaNames.ISLOGICAL)
        {
            return FormulaValueModel.CreateBoolean(
                scalar.Kind == OdfValueKind.Boolean);
        }
        if (name == OdfFormulaNames.ISNA)
        {
            return FormulaValueModel.CreateBoolean(
                scalar.Error == OdfFormulaError.NotAvailable);
        }
        if (name == OdfFormulaNames.ISNONTEXT)
        {
            return FormulaValueModel.CreateBoolean(
                scalar.Kind != OdfValueKind.String);
        }
        if (name == OdfFormulaNames.ISNUMBER)
        {
            return FormulaValueModel.CreateBoolean(IsNumeric(scalar.Kind));
        }
        if (name == OdfFormulaNames.ISREF)
        {
            return FormulaValueModel.CreateBoolean(
                value.Kind == OdfValueKind.Reference);
        }
        if (name == OdfFormulaNames.ISTEXT)
        {
            return FormulaValueModel.CreateBoolean(
                scalar.Kind == OdfValueKind.String);
        }
        if (name == OdfFormulaNames.ISEVEN
            || name == OdfFormulaNames.ISODD)
        {
            return EvaluateParity(name, scalar);
        }
        if (name == OdfFormulaNames.ERROR_TYPE)
        {
            return ErrorType(scalar);
        }
        if (name == OdfFormulaNames.N)
        {
            return ConvertWithN(scalar);
        }
        if (name == OdfFormulaNames.NA)
        {
            return FormulaValueModel.CreateError(
                OdfFormulaError.NotAvailable);
        }
        if (name == OdfFormulaNames.TYPE)
        {
            return TypeCode(value);
        }
        if (name == OdfFormulaNames.FORMULA)
        {
            return FormulaText(value, context);
        }
        if (name == OdfFormulaNames.ISFORMULA)
        {
            return FormulaValueModel.CreateBoolean(
                value.Kind == OdfValueKind.Reference
                && context.ReferencedFormula.Length > 0);
        }
        if (name == OdfFormulaNames.CELL)
        {
            return CellInformation(arguments, context);
        }
        if (name == OdfFormulaNames.INFO)
        {
            return EnvironmentInformation(value, context);
        }
        if (name == OdfFormulaNames.NUMBERVALUE)
        {
            return NumberValue(arguments, context);
        }
        if (name == OdfFormulaNames.ROW)
        {
            return CoordinateNumbers(true, arguments, context);
        }
        if (name == OdfFormulaNames.ROWS)
        {
            return DimensionCount(value, true);
        }
        if (name == OdfFormulaNames.SHEET)
        {
            return SheetNumber(arguments, context);
        }
        if (name == OdfFormulaNames.SHEETS)
        {
            int count = arguments.Length == 0
                ? context.SheetCount
                : context.ReferenceSheetCount;
            return count > 0
                ? FormulaValueModel.CreateNumber(count)
                : FormulaValueModel.CreateError(OdfFormulaError.NotAvailable);
        }
        if (name == OdfFormulaNames.VALUE)
        {
            return NumberValue(arguments, context);
        }
        return FormulaValueModel.CreateError(
            OdfFormulaError.UnsupportedFunction);
    }

    static OdfFormulaResult AreaCount(OdfFormulaResult value)
    {
        if (value.Kind != OdfValueKind.Reference || value.Reference == null)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        return FormulaValueModel.CreateNumber(
            CountReferenceAreas(value.Reference));
    }

    static int CountReferenceAreas(OdfFormulaReference reference)
    {
        if (reference.Kind != OdfFormulaNodeKind.ReferenceConcatenation
            || reference.Items.Length == 0)
        {
            return 1;
        }
        int count = 0;
        int index = 0;
        while (index < reference.Items.Length)
        {
            count = count + CountReferenceAreas(reference.Items[index]);
            index = index + 1;
        }
        return count;
    }

    static OdfFormulaResult CoordinateNumbers(
        bool rows,
        OdfFormulaResult[] arguments,
        FormulaFunctionContext context)
    {
        int first;
        int count = 1;
        if (arguments.Length == 0)
        {
            first = rows
                ? context.CurrentRowNumber
                : context.CurrentColumnNumber;
        }
        else
        {
            OdfFormulaResult value = arguments[0];
            if (value.Kind != OdfValueKind.Reference)
            {
                return FormulaValueModel.CreateError(OdfFormulaError.Value);
            }
            first = rows
                ? context.ReferenceRowNumber
                : context.ReferenceColumnNumber;
            count = rows ? value.RowCount : value.ColumnCount;
            if (value.Reference != null)
            {
                global::System.Numerics.BigInteger coordinate = rows
                    ? value.Reference.Start.RowNumber
                    : value.Reference.Start.ColumnNumber;
                if (coordinate > 0 && coordinate <= int.MaxValue)
                {
                    first = (int)coordinate;
                }
            }
        }
        if (first <= 0 || count <= 0)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Reference);
        }
        if (count == 1)
        {
            return FormulaValueModel.CreateNumber(first);
        }
        OdfFormulaResult[] values = new OdfFormulaResult[count];
        int index = 0;
        while (index < count)
        {
            values[index] = FormulaValueModel.CreateNumber(first + index);
            index = index + 1;
        }
        return FormulaValueModel.CreateArray(
            values,
            rows ? count : 1,
            rows ? 1 : count);
    }

    static OdfFormulaResult DimensionCount(
        OdfFormulaResult value,
        bool rows)
    {
        if (value.Kind != OdfValueKind.Reference
            && value.Kind != OdfValueKind.Array)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        int count = rows ? value.RowCount : value.ColumnCount;
        return count > 0
            ? FormulaValueModel.CreateNumber(count)
            : FormulaValueModel.CreateError(OdfFormulaError.Reference);
    }

    static OdfFormulaResult CountArguments(
        string name,
        OdfFormulaResult[] arguments)
    {
        int count = 0;
        int index = 0;
        while (index < arguments.Length)
        {
            count = count + CountArgumentValues(
                arguments[index],
                name == OdfFormulaNames.COUNT);
            index = index + 1;
        }
        return FormulaValueModel.CreateNumber(count);
    }

    static int CountArgumentValues(
        OdfFormulaResult value,
        bool numbersOnly)
    {
        if (value.Kind == OdfValueKind.Reference
            || value.Kind == OdfValueKind.Array)
        {
            int count = 0;
            int index = 0;
            while (index < value.Values.Length)
            {
                count = count + CountArgumentValues(
                    value.Values[index],
                    numbersOnly);
                index = index + 1;
            }
            return count;
        }
        if (numbersOnly)
        {
            return IsNumeric(value.Kind) ? 1 : 0;
        }
        return value.Kind == OdfValueKind.Empty ? 0 : 1;
    }

    static OdfFormulaResult CountBlank(OdfFormulaResult value)
    {
        if (value.Kind != OdfValueKind.Reference)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        int count = 0;
        int index = 0;
        while (index < value.Values.Length)
        {
            if (value.Values[index].Kind == OdfValueKind.Empty)
            {
                count = count + 1;
            }
            index = index + 1;
        }
        return FormulaValueModel.CreateNumber(count);
    }

    static OdfFormulaResult CountIf(OdfFormulaResult[] arguments)
    {
        OdfFormulaResult reference = arguments[0];
        if (reference.Kind != OdfValueKind.Reference)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        int count = 0;
        int index = 0;
        while (index < reference.Values.Length)
        {
            if (FormulaDatabaseFunctions.MatchesCriterion(
                reference.Values[index],
                arguments[1]))
            {
                count = count + 1;
            }
            index = index + 1;
        }
        return FormulaValueModel.CreateNumber(count);
    }

    static OdfFormulaResult CountIfs(OdfFormulaResult[] arguments)
    {
        if (arguments.Length % 2 != 0
            || arguments[0].Kind != OdfValueKind.Reference)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        int length = arguments[0].Values.Length;
        int pair = 0;
        while (pair < arguments.Length)
        {
            OdfFormulaResult reference = arguments[pair];
            if (reference.Kind != OdfValueKind.Reference
                || reference.RowCount != arguments[0].RowCount
                || reference.ColumnCount != arguments[0].ColumnCount)
            {
                return FormulaValueModel.CreateError(OdfFormulaError.Value);
            }
            pair = pair + 2;
        }
        int count = 0;
        int index = 0;
        while (index < length)
        {
            bool matches = true;
            pair = 0;
            while (pair < arguments.Length && matches)
            {
                matches = FormulaDatabaseFunctions.MatchesCriterion(
                    arguments[pair].Values[index],
                    arguments[pair + 1]);
                pair = pair + 2;
            }
            if (matches)
            {
                count = count + 1;
            }
            index = index + 1;
        }
        return FormulaValueModel.CreateNumber(count);
    }

    static OdfFormulaResult EvaluateParity(
        string name,
        OdfFormulaResult value)
    {
        OdfFormulaResult number = FormulaValueModel.ConvertToNumber(value);
        if (number.Error != OdfFormulaError.None)
        {
            return number;
        }
        double integral = global::System.Math.Truncate(
            global::System.Math.Abs(number.NumericValue));
        bool even = integral % 2 == 0;
        return FormulaValueModel.CreateBoolean(
            name == OdfFormulaNames.ISEVEN ? even : !even);
    }

    static OdfFormulaResult ErrorType(OdfFormulaResult value)
    {
        if (value.Error == OdfFormulaError.Null)
        {
            return FormulaValueModel.CreateNumber(1);
        }
        if (value.Error == OdfFormulaError.DivisionByZero)
        {
            return FormulaValueModel.CreateNumber(2);
        }
        if (value.Error == OdfFormulaError.Value)
        {
            return FormulaValueModel.CreateNumber(3);
        }
        if (value.Error == OdfFormulaError.Reference)
        {
            return FormulaValueModel.CreateNumber(4);
        }
        if (value.Error == OdfFormulaError.Name
            || value.Error == OdfFormulaError.UnsupportedFunction)
        {
            return FormulaValueModel.CreateNumber(5);
        }
        if (value.Error == OdfFormulaError.Number
            || value.Error == OdfFormulaError.CircularReference)
        {
            return FormulaValueModel.CreateNumber(6);
        }
        if (value.Error == OdfFormulaError.NotAvailable)
        {
            return FormulaValueModel.CreateNumber(7);
        }
        return FormulaValueModel.CreateError(OdfFormulaError.NotAvailable);
    }

    static OdfFormulaResult ConvertWithN(OdfFormulaResult value)
    {
        if (value.Error != OdfFormulaError.None)
        {
            return FormulaValueModel.CreateError(value.Error);
        }
        if (IsNumeric(value.Kind))
        {
            return FormulaValueModel.CreateNumber(value.NumericValue);
        }
        if (value.Kind == OdfValueKind.Boolean)
        {
            return FormulaValueModel.CreateNumber(value.BooleanValue ? 1 : 0);
        }
        return FormulaValueModel.CreateNumber(0);
    }

    static OdfFormulaResult TypeCode(OdfFormulaResult value)
    {
        if (value.Kind == OdfValueKind.Array)
        {
            return FormulaValueModel.CreateNumber(64);
        }
        if (value.Error != OdfFormulaError.None)
        {
            return FormulaValueModel.CreateNumber(16);
        }
        if (IsNumeric(value.Kind))
        {
            return FormulaValueModel.CreateNumber(1);
        }
        if (value.Kind == OdfValueKind.String)
        {
            return FormulaValueModel.CreateNumber(2);
        }
        if (value.Kind == OdfValueKind.Boolean)
        {
            return FormulaValueModel.CreateNumber(4);
        }
        if (value.Kind == OdfValueKind.Reference
            && value.Values.Length == 1)
        {
            return TypeCode(value.Values[0]);
        }
        return FormulaValueModel.CreateNumber(16);
    }

    static OdfFormulaResult FormulaText(
        OdfFormulaResult value,
        FormulaFunctionContext context)
    {
        if (value.Kind != OdfValueKind.Reference)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.NotAvailable);
        }
        return context.ReferencedFormula.Length > 0
            ? FormulaValueModel.CreateText(context.ReferencedFormula)
            : FormulaValueModel.CreateError(OdfFormulaError.NotAvailable);
    }

    static OdfFormulaResult CellInformation(
        OdfFormulaResult[] arguments,
        FormulaFunctionContext context)
    {
        OdfFormulaResult type = FormulaValueModel.ConvertToText(arguments[0]);
        if (type.Error != OdfFormulaError.None)
        {
            return type;
        }
        string requested = type.StringValue.ToLowerInvariant();
        OdfFormulaResult value = arguments.Length > 1
            ? arguments[1]
            : (context.CurrentValue
                ?? FormulaValueModel.CreateEmpty());
        if (requested == "address")
        {
            return FormulaValueModel.CreateText(context.CurrentAddress);
        }
        if (requested == "col")
        {
            return PositiveContextNumber(context.ReferenceColumnNumber,
                context.CurrentColumnNumber);
        }
        if (requested == "row")
        {
            return PositiveContextNumber(context.ReferenceRowNumber,
                context.CurrentRowNumber);
        }
        if (requested == "sheet")
        {
            return PositiveContextNumber(context.ReferenceSheetNumber,
                context.CurrentSheetNumber);
        }
        if (requested == "filename")
        {
            return FormulaValueModel.CreateText(context.FileName);
        }
        if (requested == "contents")
        {
            return Dereference(value);
        }
        if (requested == "type")
        {
            OdfFormulaResult scalar = Dereference(value);
            if (scalar.Kind == OdfValueKind.Empty)
            {
                return FormulaValueModel.CreateText("b");
            }
            if (scalar.Kind == OdfValueKind.String)
            {
                return FormulaValueModel.CreateText("l");
            }
            return FormulaValueModel.CreateText("v");
        }
        if (requested == "color")
        {
            return FormulaValueModel.CreateNumber(
                context.CellUsesNegativeColor ? 1 : 0);
        }
        if (requested == "format")
        {
            return FormulaValueModel.CreateText(context.CellFormat);
        }
        if (requested == "width")
        {
            return FormulaValueModel.CreateNumber(context.CellWidth);
        }
        if (requested == "protect")
        {
            return FormulaValueModel.CreateNumber(
                context.CellProtected ? 1 : 0);
        }
        if (requested == "parentheses")
        {
            return FormulaValueModel.CreateNumber(
                context.CellUsesParentheses ? 1 : 0);
        }
        if (requested == "prefix")
        {
            return FormulaValueModel.CreateText(context.CellPrefix);
        }
        return FormulaValueModel.CreateError(OdfFormulaError.NotAvailable);
    }

    static OdfFormulaResult PositiveContextNumber(
        int preferred,
        int fallback)
    {
        int value = preferred > 0 ? preferred : fallback;
        return value > 0
            ? FormulaValueModel.CreateNumber(value)
            : FormulaValueModel.CreateError(OdfFormulaError.NotAvailable);
    }

    static OdfFormulaResult EnvironmentInformation(
        OdfFormulaResult argument,
        FormulaFunctionContext context)
    {
        OdfFormulaResult text = FormulaValueModel.ConvertToText(argument);
        if (text.Error != OdfFormulaError.None)
        {
            return text;
        }
        string requested = text.StringValue.ToLowerInvariant();
        if (requested == "directory")
        {
            return FormulaValueModel.CreateText(context.Directory);
        }
        if (requested == "memavail")
        {
            return FormulaValueModel.CreateNumber(context.AvailableMemory);
        }
        if (requested == "memused")
        {
            return FormulaValueModel.CreateNumber(context.UsedMemory);
        }
        if (requested == "numfile")
        {
            return FormulaValueModel.CreateNumber(context.ActiveWorksheetCount);
        }
        if (requested == "osversion")
        {
            return FormulaValueModel.CreateText(context.OperatingSystem);
        }
        if (requested == "origin")
        {
            return FormulaValueModel.CreateText(context.Origin);
        }
        if (requested == "recalc")
        {
            return FormulaValueModel.CreateText(context.RecalculationMode);
        }
        if (requested == "release")
        {
            return FormulaValueModel.CreateText(context.Release);
        }
        if (requested == "system")
        {
            return FormulaValueModel.CreateText(context.OperatingSystem);
        }
        if (requested == "totmem")
        {
            return FormulaValueModel.CreateNumber(context.TotalMemory);
        }
        return FormulaValueModel.CreateError(OdfFormulaError.NotAvailable);
    }

    static OdfFormulaResult NumberValue(
        OdfFormulaResult[] arguments,
        FormulaFunctionContext context)
    {
        OdfFormulaResult source = FormulaValueModel.ConvertToText(
            Dereference(arguments[0]));
        if (source.Error != OdfFormulaError.None)
        {
            return source;
        }
        string decimalSeparator = context.DecimalSeparator;
        string groupSeparator = context.GroupSeparator;
        if (arguments.Length > 1)
        {
            OdfFormulaResult separator = FormulaValueModel.ConvertToText(
                Dereference(arguments[1]));
            if (separator.Error != OdfFormulaError.None)
            {
                return separator;
            }
            decimalSeparator = separator.StringValue;
        }
        if (arguments.Length > 2)
        {
            OdfFormulaResult separator = FormulaValueModel.ConvertToText(
                Dereference(arguments[2]));
            if (separator.Error != OdfFormulaError.None)
            {
                return separator;
            }
            groupSeparator = separator.StringValue;
        }
        if (decimalSeparator.Length != 1
            || groupSeparator.Contains(
                decimalSeparator,
                global::System.StringComparison.Ordinal))
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        string normalized = NormalizeNumberText(
            source.StringValue,
            decimalSeparator[0],
            groupSeparator);
        int percentages = 0;
        while (normalized.EndsWith(
            "%",
            global::System.StringComparison.Ordinal))
        {
            percentages = percentages + 1;
            normalized = normalized.Substring(0, normalized.Length - 1);
        }
        if (normalized.StartsWith(
            ".",
            global::System.StringComparison.Ordinal))
        {
            normalized = "0" + normalized;
        }
        else if (normalized.StartsWith(
            "-.",
            global::System.StringComparison.Ordinal))
        {
            normalized = "-0" + normalized.Substring(1);
        }
        else if (normalized.StartsWith(
            "+.",
            global::System.StringComparison.Ordinal))
        {
            normalized = "+0" + normalized.Substring(1);
        }
        OdfFormulaResult number = FormulaValueModel.ConvertToNumber(
            FormulaValueModel.CreateText(normalized));
        if (number.Error != OdfFormulaError.None)
        {
            return number;
        }
        while (percentages > 0)
        {
            number.NumericValue = number.NumericValue / 100;
            percentages = percentages - 1;
        }
        return number;
    }

    static string NormalizeNumberText(
        string source,
        char decimalSeparator,
        string groupSeparator)
    {
        StringBuilder result = new StringBuilder();
        bool foundDecimal = false;
        int index = 0;
        while (index < source.Length)
        {
            if (global::System.Char.IsWhiteSpace(source[index]))
            {
                index = index + 1;
            }
            else if (!foundDecimal
                && groupSeparator.Length > 0
                && source.Substring(index).StartsWith(
                    groupSeparator,
                    global::System.StringComparison.Ordinal))
            {
                index = index + groupSeparator.Length;
            }
            else if (!foundDecimal && source[index] == decimalSeparator)
            {
                result.Append('.');
                foundDecimal = true;
                index = index + 1;
            }
            else
            {
                result.Append(source[index]);
                index = index + 1;
            }
        }
        return result.ToString();
    }

    static OdfFormulaResult SheetNumber(
        OdfFormulaResult[] arguments,
        FormulaFunctionContext context)
    {
        if (arguments.Length == 0)
        {
            return PositiveContextNumber(context.CurrentSheetNumber, 0);
        }
        OdfFormulaResult value = arguments[0];
        if (value.Kind == OdfValueKind.Reference)
        {
            if (value.Reference != null && value.Reference.HasSource)
            {
                return FormulaValueModel.CreateError(OdfFormulaError.Reference);
            }
            if (context.ReferenceSheetNumber > 0)
            {
                return FormulaValueModel.CreateNumber(
                    context.ReferenceSheetNumber);
            }
            if (value.Reference != null)
            {
                string name = value.Reference.Start.EffectiveSheetName;
                if (name.Length == 0)
                {
                    name = value.Reference.Start.SheetName;
                }
                int resolved = ResolveSheetName(name, context);
                if (resolved > 0)
                {
                    return FormulaValueModel.CreateNumber(resolved);
                }
            }
            return PositiveContextNumber(context.CurrentSheetNumber, 0);
        }
        OdfFormulaResult text = FormulaValueModel.ConvertToText(value);
        if (text.Error != OdfFormulaError.None)
        {
            return text;
        }
        int sheet = ResolveSheetName(text.StringValue, context);
        return sheet > 0
            ? FormulaValueModel.CreateNumber(sheet)
            : FormulaValueModel.CreateError(OdfFormulaError.NotAvailable);
    }

    static int ResolveSheetName(
        string name,
        FormulaFunctionContext context)
    {
        if (context.SheetNames.Length != context.SheetNumbers.Length)
        {
            return 0;
        }
        int index = 0;
        while (index < context.SheetNames.Length)
        {
            if (global::System.String.Equals(
                context.SheetNames[index],
                name,
                global::System.StringComparison.OrdinalIgnoreCase))
            {
                return context.SheetNumbers[index];
            }
            index = index + 1;
        }
        return 0;
    }

    static OdfFormulaResult Dereference(OdfFormulaResult value)
    {
        if (value.Kind != OdfValueKind.Reference)
        {
            return value;
        }
        return value.Values.Length == 1
            ? value.Values[0]
            : FormulaValueModel.CreateError(OdfFormulaError.Value);
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
