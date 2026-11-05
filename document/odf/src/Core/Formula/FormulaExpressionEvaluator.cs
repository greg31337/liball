using System.Text;

namespace odf;

internal class FormulaEvaluationResolver :
    IFormulaExternalResourceResolver,
    IFormulaLookupResourceResolver
{
    internal OdfFormulaEvaluationData Data = new OdfFormulaEvaluationData();
    internal FormulaCalculationGraph? Graph;

    public OdfFormulaResult ResolveExternalFunction(
        string functionName,
        OdfFormulaResult[] arguments)
    {
        return FindFunctionResult(functionName);
    }

    public OdfFormulaResult ResolveLookupFunction(
        string functionName,
        OdfFormulaResult[] arguments)
    {
        if (functionName == OdfFormulaNames.INDIRECT
            && arguments.Length > 0)
        {
            OdfFormulaResult text = FormulaValueModel.ConvertToText(
                arguments[0]);
            if (text.Error != OdfFormulaError.None)
            {
                return text;
            }
            OdfFormulaResult? value = FindReferenceValue(text.StringValue);
            if (value != null)
            {
                return value;
            }
        }
        return FindFunctionResult(functionName);
    }

    internal OdfFormulaResult? FindReferenceValue(string key)
    {
        if (Graph != null)
        {
            FormulaCalculationNode? node = FormulaDependencyGraph.FindNode(
                Graph,
                key);
            if (node != null && node.Result != null)
            {
                return node.Result;
            }
        }
        int index = 0;
        int count = global::System.Math.Min(
            Data.ReferenceKeys.Length,
            Data.ReferenceValues.Length);
        while (index < count)
        {
            if (Data.ReferenceKeys[index] == key)
            {
                return Data.ReferenceValues[index];
            }
            index = index + 1;
        }
        return null;
    }

    OdfFormulaResult FindFunctionResult(string name)
    {
        int index = 0;
        int count = global::System.Math.Min(
            Data.ExternalFunctionNames.Length,
            Data.ExternalFunctionResults.Length);
        while (index < count)
        {
            if (global::System.String.Equals(
                Data.ExternalFunctionNames[index],
                name,
                global::System.StringComparison.OrdinalIgnoreCase))
            {
                return Data.ExternalFunctionResults[index];
            }
            index = index + 1;
        }
        return FormulaValueModel.CreateError(OdfFormulaError.NotAvailable);
    }
}

internal static class FormulaExpressionEvaluator
{
    internal static OdfFormulaResult Evaluate(
        OdfFormula formula,
        OdfFormulaEvaluationData data,
        string ownerSheet,
        FormulaCalculationGraph? graph)
    {
        if (formula == null || data == null || formula.Root == null)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        if (!formula.IsValid)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        FormulaEvaluationResolver resolver = new FormulaEvaluationResolver();
        resolver.Data = data;
        resolver.Graph = graph;
        FormulaFunctionContext context = CreateFunctionContext(
            data,
            resolver);
        OdfFormulaResult result = EvaluateNode(
            formula.Root,
            data,
            ownerSheet,
            graph,
            context,
            resolver);
        formula.Result = result;
        return result;
    }

    static OdfFormulaResult EvaluateNode(
        OdfFormulaNode node,
        OdfFormulaEvaluationData data,
        string ownerSheet,
        FormulaCalculationGraph? graph,
        FormulaFunctionContext context,
        FormulaEvaluationResolver resolver)
    {
        if (node.Kind == OdfFormulaNodeKind.Literal
            || node.Kind == OdfFormulaNodeKind.Error)
        {
            return FormulaValueModel.FromLiteral(node);
        }
        if (node.Kind == OdfFormulaNodeKind.Reference
            || node.Kind == OdfFormulaNodeKind.Range
            || node.Kind == OdfFormulaNodeKind.ExternalReference
            || node.Kind == OdfFormulaNodeKind.Name
            || node.Kind == OdfFormulaNodeKind.Intersection
            || node.Kind == OdfFormulaNodeKind.ReferenceConcatenation)
        {
            return ResolveReference(
                node,
                data,
                ownerSheet,
                graph,
                context,
                resolver);
        }
        if (node.Kind == OdfFormulaNodeKind.UnaryOperation
            && node.Children.Length == 1)
        {
            return FormulaValueModel.ApplyUnary(
                node.Text,
                EvaluateNode(
                    node.Children[0],
                    data,
                    ownerSheet,
                    graph,
                    context,
                    resolver));
        }
        if (node.Kind == OdfFormulaNodeKind.BinaryOperation
            && node.Children.Length == 2)
        {
            OdfFormulaResult left = EvaluateNode(
                node.Children[0],
                data,
                ownerSheet,
                graph,
                context,
                resolver);
            OdfFormulaResult right = EvaluateNode(
                node.Children[1],
                data,
                ownerSheet,
                graph,
                context,
                resolver);
            return FormulaValueModel.ApplyBinary(node.Text, left, right);
        }
        if (node.Kind == OdfFormulaNodeKind.Array)
        {
            return EvaluateArray(
                node,
                data,
                ownerSheet,
                graph,
                context,
                resolver);
        }
        if (node.Kind == OdfFormulaNodeKind.FunctionCall)
        {
            return EvaluateFunction(
                node,
                data,
                ownerSheet,
                graph,
                context,
                resolver);
        }
        if (node.Kind == OdfFormulaNodeKind.Unknown
            && node.Text.Length == 0)
        {
            return FormulaValueModel.CreateEmpty();
        }
        return FormulaValueModel.CreateError(OdfFormulaError.Value);
    }

    static OdfFormulaResult EvaluateFunction(
        OdfFormulaNode node,
        OdfFormulaEvaluationData data,
        string ownerSheet,
        FormulaCalculationGraph? graph,
        FormulaFunctionContext context,
        FormulaEvaluationResolver resolver)
    {
        OdfFormulaFunctionInfo? info = node.Function
            ?? FormulaFunctionCatalog.Find(node.Text);
        if (info == null)
        {
            return FormulaValueModel.CreateError(
                OdfFormulaError.UnsupportedFunction);
        }
        if (node.Children.Length < info.MinimumArguments
            || node.Children.Length > info.MaximumArguments)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        if (info.Name == OdfFormulaNames.IF)
        {
            return EvaluateLazyIf(
                node,
                data,
                ownerSheet,
                graph,
                context,
                resolver);
        }
        if (info.Name == OdfFormulaNames.IFERROR
            || info.Name == OdfFormulaNames.IFNA)
        {
            return EvaluateLazyError(
                info.Name,
                node,
                data,
                ownerSheet,
                graph,
                context,
                resolver);
        }
        if (info.Name == OdfFormulaNames.CHOOSE)
        {
            return EvaluateLazyChoose(
                node,
                data,
                ownerSheet,
                graph,
                context,
                resolver);
        }
        OdfFormulaResult[] arguments =
            new OdfFormulaResult[node.Children.Length];
        int index = 0;
        while (index < arguments.Length)
        {
            arguments[index] = EvaluateNode(
                node.Children[index],
                data,
                ownerSheet,
                graph,
                context,
                resolver);
            index = index + 1;
        }
        return FormulaFunctionEvaluator.Evaluate(
            info.Name,
            arguments,
            context);
    }

    static OdfFormulaResult EvaluateLazyIf(
        OdfFormulaNode node,
        OdfFormulaEvaluationData data,
        string ownerSheet,
        FormulaCalculationGraph? graph,
        FormulaFunctionContext context,
        FormulaEvaluationResolver resolver)
    {
        OdfFormulaResult condition = FormulaValueModel.ConvertToBoolean(
            EvaluateNode(
                node.Children[0],
                data,
                ownerSheet,
                graph,
                context,
                resolver));
        if (condition.Error != OdfFormulaError.None)
        {
            return condition;
        }
        if (node.Children.Length == 1)
        {
            return condition;
        }
        int selected = condition.BooleanValue ? 1 : 2;
        if (selected >= node.Children.Length)
        {
            return FormulaValueModel.CreateBoolean(false);
        }
        OdfFormulaResult result = EvaluateNode(
            node.Children[selected],
            data,
            ownerSheet,
            graph,
            context,
            resolver);
        return result.Kind == OdfValueKind.Empty
            ? FormulaValueModel.CreateNumber(0)
            : result;
    }

    static OdfFormulaResult EvaluateLazyError(
        string name,
        OdfFormulaNode node,
        OdfFormulaEvaluationData data,
        string ownerSheet,
        FormulaCalculationGraph? graph,
        FormulaFunctionContext context,
        FormulaEvaluationResolver resolver)
    {
        OdfFormulaResult first = EvaluateNode(
            node.Children[0],
            data,
            ownerSheet,
            graph,
            context,
            resolver);
        bool replace = name == OdfFormulaNames.IFERROR
            ? first.Error != OdfFormulaError.None
            : first.Error == OdfFormulaError.NotAvailable;
        return replace
            ? EvaluateNode(
                node.Children[1],
                data,
                ownerSheet,
                graph,
                context,
                resolver)
            : first;
    }

    static OdfFormulaResult EvaluateLazyChoose(
        OdfFormulaNode node,
        OdfFormulaEvaluationData data,
        string ownerSheet,
        FormulaCalculationGraph? graph,
        FormulaFunctionContext context,
        FormulaEvaluationResolver resolver)
    {
        OdfFormulaResult selected = FormulaValueModel.ConvertToNumber(
            EvaluateNode(
                node.Children[0],
                data,
                ownerSheet,
                graph,
                context,
                resolver));
        if (selected.Error != OdfFormulaError.None)
        {
            return selected;
        }
        int index = (int)global::System.Math.Truncate(
            selected.NumericValue);
        if (index < 1 || index >= node.Children.Length)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        return EvaluateNode(
            node.Children[index],
            data,
            ownerSheet,
            graph,
            context,
            resolver);
    }

    static OdfFormulaResult EvaluateArray(
        OdfFormulaNode node,
        OdfFormulaEvaluationData data,
        string ownerSheet,
        FormulaCalculationGraph? graph,
        FormulaFunctionContext context,
        FormulaEvaluationResolver resolver)
    {
        if (node.Children.Length == 0)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        int columns = node.Children.Length;
        int rows = 1;
        int index = 0;
        while (index < node.Separators.Length)
        {
            if (node.Separators[index] == "|")
            {
                if (rows == 1)
                {
                    columns = index + 1;
                }
                rows = rows + 1;
            }
            index = index + 1;
        }
        if (columns <= 0 || rows * columns != node.Children.Length)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        OdfFormulaResult[] values =
            new OdfFormulaResult[node.Children.Length];
        index = 0;
        while (index < values.Length)
        {
            values[index] = EvaluateNode(
                node.Children[index],
                data,
                ownerSheet,
                graph,
                context,
                resolver);
            index = index + 1;
        }
        return FormulaValueModel.CreateArray(values, rows, columns);
    }

    static OdfFormulaResult ResolveReference(
        OdfFormulaNode node,
        OdfFormulaEvaluationData data,
        string ownerSheet,
        FormulaCalculationGraph? graph,
        FormulaFunctionContext context,
        FormulaEvaluationResolver resolver)
    {
        OdfFormulaReference? reference = node.Reference;
        if (reference == null)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Reference);
        }
        if (reference.IsReferenceError)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Reference);
        }
        OdfFormulaResult? supplied = resolver.FindReferenceValue(node.Text);
        if (supplied == null && reference.LexicalValue != node.Text)
        {
            supplied = resolver.FindReferenceValue(reference.LexicalValue);
        }
        if (supplied != null)
        {
            return WrapReference(reference, supplied);
        }
        if (reference.Items.Length > 0)
        {
            return ResolveCompositeReference(
                node,
                reference,
                data,
                ownerSheet,
                graph,
                context,
                resolver);
        }
        if (reference.HasSource)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.NotAvailable);
        }
        if (reference.Name.Length > 0)
        {
            string sheet = reference.NameSheet.Length > 0
                ? reference.NameSheet
                : ownerSheet;
            string key = sheet.Length > 0
                ? sheet + ".$$" + reference.Name
                : "$$" + reference.Name;
            supplied = resolver.FindReferenceValue(key);
            return supplied == null
                ? FormulaValueModel.CreateError(OdfFormulaError.Name)
                : WrapReference(reference, supplied);
        }
        if (reference.HasEnd)
        {
            return ResolveRange(reference, ownerSheet, resolver);
        }
        if (reference.Start.HasColumn && reference.Start.HasRow)
        {
            string key = CellKey(reference.Start, ownerSheet);
            supplied = resolver.FindReferenceValue(key);
            return supplied == null
                ? FormulaValueModel.CreateError(
                    OdfFormulaError.NotAvailable)
                : WrapReference(reference, supplied);
        }
        return FormulaValueModel.CreateError(OdfFormulaError.Reference);
    }

    static OdfFormulaResult ResolveCompositeReference(
        OdfFormulaNode node,
        OdfFormulaReference reference,
        OdfFormulaEvaluationData data,
        string ownerSheet,
        FormulaCalculationGraph? graph,
        FormulaFunctionContext context,
        FormulaEvaluationResolver resolver)
    {
        if (node.Kind != OdfFormulaNodeKind.ReferenceConcatenation)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Null);
        }
        OdfFormulaResult[] parts = new OdfFormulaResult[node.Children.Length];
        int total = 0;
        int index = 0;
        while (index < parts.Length)
        {
            parts[index] = EvaluateNode(
                node.Children[index],
                data,
                ownerSheet,
                graph,
                context,
                resolver);
            if (parts[index].Error != OdfFormulaError.None)
            {
                return parts[index];
            }
            total = total + (parts[index].Values.Length > 0
                ? parts[index].Values.Length
                : 1);
            index = index + 1;
        }
        OdfFormulaResult[] values = new OdfFormulaResult[total];
        int output = 0;
        index = 0;
        while (index < parts.Length)
        {
            if (parts[index].Values.Length > 0)
            {
                int item = 0;
                while (item < parts[index].Values.Length)
                {
                    values[output] = parts[index].Values[item];
                    output = output + 1;
                    item = item + 1;
                }
            }
            else
            {
                values[output] = parts[index];
                output = output + 1;
            }
            index = index + 1;
        }
        return FormulaValueModel.CreateReference(
            reference,
            values,
            1,
            values.Length);
    }

    static OdfFormulaResult ResolveRange(
        OdfFormulaReference reference,
        string ownerSheet,
        FormulaEvaluationResolver resolver)
    {
        if (!reference.Start.HasColumn
            || !reference.Start.HasRow
            || !reference.End.HasColumn
            || !reference.End.HasRow)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Reference);
        }
        string firstSheet = reference.Start.EffectiveSheetName.Length > 0
            ? reference.Start.EffectiveSheetName
            : ownerSheet;
        string secondSheet = reference.End.EffectiveSheetName.Length > 0
            ? reference.End.EffectiveSheetName
            : firstSheet;
        if (firstSheet != secondSheet)
        {
            return FormulaValueModel.CreateError(
                OdfFormulaError.NotAvailable);
        }
        global::System.Numerics.BigInteger lowRow =
            reference.Start.RowNumber < reference.End.RowNumber
                ? reference.Start.RowNumber
                : reference.End.RowNumber;
        global::System.Numerics.BigInteger highRow =
            reference.Start.RowNumber > reference.End.RowNumber
                ? reference.Start.RowNumber
                : reference.End.RowNumber;
        global::System.Numerics.BigInteger lowColumn =
            reference.Start.ColumnNumber < reference.End.ColumnNumber
                ? reference.Start.ColumnNumber
                : reference.End.ColumnNumber;
        global::System.Numerics.BigInteger highColumn =
            reference.Start.ColumnNumber > reference.End.ColumnNumber
                ? reference.Start.ColumnNumber
                : reference.End.ColumnNumber;
        global::System.Numerics.BigInteger rowCount = highRow - lowRow + 1;
        global::System.Numerics.BigInteger columnCount =
            highColumn - lowColumn + 1;
        if (rowCount > int.MaxValue
            || columnCount > int.MaxValue
            || rowCount * columnCount > int.MaxValue)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Number);
        }
        int rows = (int)rowCount;
        int columns = (int)columnCount;
        OdfFormulaResult[] values = new OdfFormulaResult[rows * columns];
        int row = 0;
        while (row < rows)
        {
            int column = 0;
            while (column < columns)
            {
                string coordinate = ColumnName(lowColumn + column)
                    + (lowRow + row).ToString(
                        global::System.Globalization.CultureInfo.InvariantCulture);
                string key = firstSheet.Length > 0
                    ? firstSheet + "." + coordinate
                    : coordinate;
                OdfFormulaResult? supplied = resolver.FindReferenceValue(key);
                if (supplied == null)
                {
                    return FormulaValueModel.CreateError(
                        OdfFormulaError.NotAvailable);
                }
                values[row * columns + column] = supplied;
                column = column + 1;
            }
            row = row + 1;
        }
        return FormulaValueModel.CreateReference(
            reference,
            values,
            rows,
            columns);
    }

    static OdfFormulaResult WrapReference(
        OdfFormulaReference reference,
        OdfFormulaResult value)
    {
        if (value.Kind == OdfValueKind.Reference)
        {
            return value;
        }
        if (value.Kind == OdfValueKind.Array)
        {
            return FormulaValueModel.CreateReference(
                reference,
                value.Values,
                value.RowCount,
                value.ColumnCount);
        }
        return FormulaValueModel.CreateReference(reference, [value], 1, 1);
    }

    static string CellKey(
        OdfFormulaReferenceEndpoint endpoint,
        string ownerSheet)
    {
        string sheet = endpoint.EffectiveSheetName.Length > 0
            ? endpoint.EffectiveSheetName
            : ownerSheet;
        string coordinate = endpoint.ColumnName
            + endpoint.RowNumber.ToString(
                global::System.Globalization.CultureInfo.InvariantCulture);
        return sheet.Length > 0 ? sheet + "." + coordinate : coordinate;
    }

    static string ColumnName(global::System.Numerics.BigInteger number)
    {
        StringBuilder result = new StringBuilder();
        global::System.Numerics.BigInteger current = number;
        while (current > 0)
        {
            current = current - 1;
            int remainder = (int)(current % 26);
            result.Insert(0, (char)('A' + remainder));
            current = current / 26;
        }
        return result.ToString();
    }

    static FormulaFunctionContext CreateFunctionContext(
        OdfFormulaEvaluationData data,
        FormulaEvaluationResolver resolver)
    {
        FormulaFunctionContext result = new FormulaFunctionContext();
        result.ExternalResolver = resolver;
        result.LookupResolver = resolver;
        result.CurrentValue = data.CurrentValue;
        result.CurrentDateTime = data.CurrentDateTime;
        result.SpreadsheetSettings = data.SpreadsheetSettings
            ?? new OdfSpreadsheetSettings();
        result.CurrentAddress = data.CurrentAddress;
        result.ReferencedFormula = data.ReferencedFormula;
        result.FileName = data.FileName;
        result.Directory = data.Directory;
        result.OperatingSystem = data.OperatingSystem;
        result.Release = data.Release;
        result.RecalculationMode = data.RecalculationMode;
        result.Origin = data.Origin;
        result.DecimalSeparator = data.DecimalSeparator;
        result.GroupSeparator = data.GroupSeparator;
        result.CellFormat = data.CellFormat;
        result.CellPrefix = data.CellPrefix;
        result.SheetNames = data.SheetNames;
        result.SheetNumbers = data.SheetNumbers;
        result.CurrentRowNumber = data.CurrentRowNumber;
        result.CurrentColumnNumber = data.CurrentColumnNumber;
        result.CurrentSheetNumber = data.CurrentSheetNumber;
        result.ReferenceRowNumber = data.ReferenceRowNumber;
        result.ReferenceColumnNumber = data.ReferenceColumnNumber;
        result.ReferenceSheetNumber = data.ReferenceSheetNumber;
        result.ActiveWorksheetCount = data.ActiveWorksheetCount;
        result.SheetCount = data.SheetCount;
        result.ReferenceSheetCount = data.ReferenceSheetCount;
        result.AvailableMemory = data.AvailableMemory;
        result.UsedMemory = data.UsedMemory;
        result.TotalMemory = data.TotalMemory;
        result.CellWidth = data.CellWidth;
        result.RandomValue = data.RandomValue;
        result.CellUsesNegativeColor = data.CellUsesNegativeColor;
        result.CellProtected = data.CellProtected;
        result.CellUsesParentheses = data.CellUsesParentheses;
        return result;
    }
}
