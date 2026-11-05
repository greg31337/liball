using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace odf;

internal static class TableCellValueModel
{
    internal static void LoadCellValues(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for cell-value materialization.");
        }
        List<OdfCellValue> values = new List<OdfCellValue>();
        int tableIndex = 0;
        while (tableIndex < document.Tables.Length)
        {
            OdfTable table = document.Tables[tableIndex];
            int rowIndex = 0;
            while (rowIndex < table.Rows.Length)
            {
                OdfRow row = table.Rows[rowIndex];
                int cellIndex = 0;
                while (cellIndex < row.Cells.Length)
                {
                    InitializeCellValue(row.Cells[cellIndex]);
                    values.Add(row.Cells[cellIndex].Value);
                    cellIndex = cellIndex + 1;
                }
                rowIndex = rowIndex + 1;
            }
            tableIndex = tableIndex + 1;
        }
        document.CellValues = values.ToArray();
    }

    internal static void SynchronizeCellValues(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for cell-value synchronization.");
        }
        List<OdfCellValue> values = new List<OdfCellValue>();
        int tableIndex = 0;
        while (tableIndex < document.Tables.Length)
        {
            OdfTable table = document.Tables[tableIndex];
            int rowIndex = 0;
            while (rowIndex < table.Rows.Length)
            {
                OdfRow row = table.Rows[rowIndex];
                int cellIndex = 0;
                while (cellIndex < row.Cells.Length)
                {
                    SynchronizeCellValue(row.Cells[cellIndex]);
                    values.Add(row.Cells[cellIndex].Value);
                    cellIndex = cellIndex + 1;
                }
                rowIndex = rowIndex + 1;
            }
            tableIndex = tableIndex + 1;
        }
        document.CellValues = values.ToArray();
    }

    internal static void InitializeCellValue(OdfCell cell)
    {
        OdfNode? node = cell.Node;
        OdfCellValue value = new OdfCellValue();
        value.Cell = cell;
        cell.Value = value;
        cell.OriginalValue = value;
        cell.Formula = node == null
            ? ""
            : StyleDocumentModel.ReadAttribute(
                node,
                OdfNamespaces.Table,
                "formula");
        cell.OriginalFormula = cell.Formula;
        if (node == null)
        {
            value.Kind = OdfValueKind.Empty;
            value.IsValid = true;
            value.Provenance = OdfValueProvenance.Defaulted;
            RememberOriginalValue(value);
            return;
        }
        value.ValueType = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Office,
            "value-type");
        value.IsCachedFormulaResult = cell.Formula.Length > 0
            && value.ValueType.Length > 0
            && value.ValueType != "void";
        ReadTypedValue(node, value);
        RememberOriginalValue(value);
    }

    internal static void InitializeStreamingCellValue(
        OdfCell cell,
        OdfAttribute[] attributes,
        string displayedText)
    {
        OdfNode node = new OdfNode();
        node.Kind = OdfNodeKind.Element;
        node.Attributes = attributes;
        if (displayedText.Length > 0)
        {
            OdfNode text = new OdfNode();
            text.Kind = OdfNodeKind.Text;
            text.Value = displayedText;
            text.Parent = node;
            node.Children = [text];
        }
        cell.Node = node;
        InitializeCellValue(cell);
        cell.Node = null;
    }

    static void ReadTypedValue(OdfNode node, OdfCellValue value)
    {
        string valueType = value.ValueType;
        value.Provenance = OdfValueProvenance.Parsed;
        if (valueType.Length == 0)
        {
            value.Kind = OdfValueKind.Empty;
            value.IsValid = !HasOfficeValueAttribute(node);
            value.Provenance = value.IsValid
                ? OdfValueProvenance.Defaulted
                : OdfValueProvenance.InvalidPreserved;
            value.LexicalValue = FirstOfficeValue(node);
            return;
        }
        if (valueType == "void")
        {
            value.Kind = OdfValueKind.Empty;
            value.IsValid = !HasOfficeValueAttribute(node);
            SetInvalidProvenance(value);
            return;
        }
        if (valueType == "string")
        {
            ReadStringValue(node, value);
            return;
        }
        if (valueType == "float")
        {
            ReadNumericValue(node, value, OdfValueKind.Float);
            return;
        }
        if (valueType == "percentage")
        {
            value.UnitKind = OdfUnitKind.Percentage;
            ReadNumericValue(node, value, OdfValueKind.Percentage);
            return;
        }
        if (valueType == "currency")
        {
            ReadCurrencyValue(node, value);
            return;
        }
        if (valueType == "date")
        {
            ReadDateValue(node, value);
            return;
        }
        if (valueType == "time")
        {
            ReadDurationValue(node, value);
            return;
        }
        if (valueType == "boolean")
        {
            ReadBooleanValue(node, value);
            return;
        }
        value.Kind = OdfValueKind.Empty;
        value.LexicalValue = FirstOfficeValue(node);
        value.IsValid = false;
        value.Provenance = OdfValueProvenance.InvalidPreserved;
    }

    static void ReadStringValue(OdfNode node, OdfCellValue value)
    {
        OdfAttribute? attribute = FindOfficeAttribute(node, "string-value");
        value.StringValue = attribute == null
            ? ExtractCellText(node)
            : attribute.Value;
        value.LexicalValue = value.StringValue;
        value.Kind = OdfValueKind.String;
        value.IsValid = !HasConflictingValueAttribute(node, "string-value");
        OdfFormulaError error = ParseFormulaError(value.StringValue);
        if (value.IsCachedFormulaResult && error != OdfFormulaError.None)
        {
            value.Kind = OdfValueKind.Error;
            value.Error = error;
        }
        SetInvalidProvenance(value);
    }

    static void ReadNumericValue(
        OdfNode node,
        OdfCellValue value,
        OdfValueKind kind)
    {
        value.Kind = kind;
        value.UnitKind = kind == OdfValueKind.Percentage
            ? OdfUnitKind.Percentage
            : OdfUnitKind.Unitless;
        value.LexicalValue = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Office,
            "value");
        OdfLexicalValue parsed = OdfLexicalParser.ParseFloat(
            value.LexicalValue);
        value.IsValid = parsed.IsValid
            && !HasConflictingValueAttribute(node, "value")
            && (kind == OdfValueKind.Currency
                || FindOfficeAttribute(node, "currency") == null);
        if (parsed.IsValid)
        {
            value.NumericValue = parsed.FloatValue;
        }
        SetInvalidProvenance(value);
    }

    static void ReadCurrencyValue(OdfNode node, OdfCellValue value)
    {
        ReadNumericValue(node, value, OdfValueKind.Currency);
        value.Currency = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Office,
            "currency");
        value.IsValid = value.IsValid && value.Currency.Length > 0;
        SetInvalidProvenance(value);
    }

    static void ReadDateValue(OdfNode node, OdfCellValue value)
    {
        value.DateValue = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Office,
            "date-value");
        value.LexicalValue = value.DateValue;
        OdfTemporalValue date = OdfLexicalParser.ParseDate(value.DateValue);
        OdfTemporalValue dateTime = OdfLexicalParser.ParseDateTime(
            value.DateValue);
        value.Kind = dateTime.IsValid
            ? OdfValueKind.DateTime
            : OdfValueKind.Date;
        value.IsValid = (date.IsValid || dateTime.IsValid)
            && !HasConflictingValueAttribute(node, "date-value");
        SetInvalidProvenance(value);
    }

    static void ReadDurationValue(OdfNode node, OdfCellValue value)
    {
        value.Kind = OdfValueKind.Duration;
        value.DurationValue = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Office,
            "time-value");
        value.LexicalValue = value.DurationValue;
        value.IsValid = OdfLexicalParser.ParseDuration(
            value.DurationValue).IsValid
            && !HasConflictingValueAttribute(node, "time-value");
        SetInvalidProvenance(value);
    }

    static void ReadBooleanValue(OdfNode node, OdfCellValue value)
    {
        value.Kind = OdfValueKind.Boolean;
        value.LexicalValue = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Office,
            "boolean-value");
        OdfLexicalValue parsed = OdfLexicalParser.ParseBoolean(
            value.LexicalValue);
        value.IsValid = parsed.IsValid
            && !HasConflictingValueAttribute(node, "boolean-value");
        if (parsed.IsValid)
        {
            value.BooleanValue = parsed.BooleanValue;
        }
        SetInvalidProvenance(value);
    }

    static void SynchronizeCellValue(OdfCell cell)
    {
        OdfNode? node = cell.Node;
        if (node == null)
        {
            return;
        }
        OdfCellValue value = cell.Value;
        value.Cell = cell;
        if (value.Kind == OdfValueKind.Error && cell.Formula.Length == 0)
        {
            throw new OdfEditException(
                "An error cell value can only be stored as a cached formula result.");
        }
        if (value != cell.OriginalValue || ValueChanged(value))
        {
            WriteTypedValue(node, value);
            if (value.Provenance == OdfValueProvenance.Parsed
                || value.Provenance == OdfValueProvenance.InvalidPreserved
                || value.Provenance == OdfValueProvenance.Defaulted)
            {
                value.Provenance = OdfValueProvenance.Synthesized;
            }
        }
        if (cell.Formula != cell.OriginalFormula)
        {
            WriteOptionalAttribute(
                node,
                OdfNamespaces.Table,
                "formula",
                OdfNamespaces.TablePrefix,
                cell.Formula);
            cell.OriginalFormula = cell.Formula;
        }
        value.IsCachedFormulaResult = cell.Formula.Length > 0
            && value.Kind != OdfValueKind.Empty;
        RememberOriginalValue(value);
        cell.OriginalValue = value;
    }

    static void WriteTypedValue(OdfNode node, OdfCellValue value)
    {
        if (value.Kind == OdfValueKind.Empty)
        {
            WriteEmptyValue(node, value);
            return;
        }
        if (value.Kind == OdfValueKind.String)
        {
            WriteStringValue(node, value, SelectStringLexical(value));
            return;
        }
        if (value.Kind == OdfValueKind.Error)
        {
            string error = FormatFormulaError(value.Error);
            if (error.Length == 0)
            {
                throw new OdfEditException(
                    "An error cell value requires a recognized formula error.");
            }
            value.StringValue = error;
            WriteStringValue(node, value, error);
            return;
        }
        if (value.Kind == OdfValueKind.Float)
        {
            WriteNumericValue(node, value, "float");
            return;
        }
        if (value.Kind == OdfValueKind.Percentage)
        {
            value.UnitKind = OdfUnitKind.Percentage;
            WriteNumericValue(node, value, "percentage");
            return;
        }
        if (value.Kind == OdfValueKind.Currency)
        {
            if (value.Currency.Length == 0)
            {
                throw new OdfEditException(
                    "A currency cell value requires a currency code.");
            }
            WriteNumericValue(node, value, "currency");
            WriteRequiredAttribute(
                node,
                OdfNamespaces.Office,
                "currency",
                OdfNamespaces.OfficePrefix,
                value.Currency);
            return;
        }
        if (value.Kind == OdfValueKind.Date
            || value.Kind == OdfValueKind.DateTime)
        {
            WriteDateValue(node, value);
            return;
        }
        if (value.Kind == OdfValueKind.Duration)
        {
            WriteDurationValue(node, value);
            return;
        }
        if (value.Kind == OdfValueKind.Boolean)
        {
            WriteBooleanValue(node, value);
            return;
        }
        throw new OdfEditException(
            "The requested cell value kind cannot be stored by an ODF cell.");
    }

    static void WriteEmptyValue(OdfNode node, OdfCellValue value)
    {
        RemoveValueAttributes(node);
        value.LexicalValue = "";
        value.StringValue = "";
        value.Currency = "";
        value.DateValue = "";
        value.DurationValue = "";
        value.Error = OdfFormulaError.None;
        value.UnitKind = OdfUnitKind.Unknown;
        value.IsValid = true;
        if (value.ValueType == "void")
        {
            WriteRequiredAttribute(
                node,
                OdfNamespaces.Office,
                "value-type",
                OdfNamespaces.OfficePrefix,
                "void");
        }
        else
        {
            value.ValueType = "";
        }
    }

    static void WriteStringValue(
        OdfNode node,
        OdfCellValue value,
        string text)
    {
        RemoveValueAttributes(node);
        value.ValueType = "string";
        value.LexicalValue = text;
        value.StringValue = text;
        value.IsValid = true;
        WriteRequiredAttribute(
            node,
            OdfNamespaces.Office,
            "value-type",
            OdfNamespaces.OfficePrefix,
            "string");
        WriteRequiredAttribute(
            node,
            OdfNamespaces.Office,
            "string-value",
            OdfNamespaces.OfficePrefix,
            text);
    }

    static void WriteNumericValue(
        OdfNode node,
        OdfCellValue value,
        string valueType)
    {
        string lexical = SelectNumericLexical(value);
        OdfLexicalValue parsed = OdfLexicalParser.ParseFloat(lexical);
        if (!parsed.IsValid)
        {
            throw new OdfEditException(
                "A numeric cell value requires a valid XML Schema number.");
        }
        RemoveValueAttributes(node);
        value.ValueType = valueType;
        value.LexicalValue = lexical;
        value.NumericValue = parsed.FloatValue;
        value.IsValid = true;
        value.UnitKind = valueType == "percentage"
            ? OdfUnitKind.Percentage
            : OdfUnitKind.Unitless;
        WriteRequiredAttribute(
            node,
            OdfNamespaces.Office,
            "value-type",
            OdfNamespaces.OfficePrefix,
            valueType);
        WriteRequiredAttribute(
            node,
            OdfNamespaces.Office,
            "value",
            OdfNamespaces.OfficePrefix,
            lexical);
    }

    static string SelectNumericLexical(OdfCellValue value)
    {
        if (value.LexicalValue != value.OriginalLexicalValue
            && OdfLexicalParser.ParseFloat(value.LexicalValue).IsValid)
        {
            return value.LexicalValue;
        }
        if (double.IsPositiveInfinity(value.NumericValue))
        {
            return "INF";
        }
        if (double.IsNegativeInfinity(value.NumericValue))
        {
            return "-INF";
        }
        if (double.IsNaN(value.NumericValue))
        {
            return "NaN";
        }
        return value.NumericValue.ToString("R", CultureInfo.InvariantCulture);
    }

    static string SelectStringLexical(OdfCellValue value)
    {
        if (value.StringValue != value.OriginalStringValue)
        {
            return value.StringValue;
        }
        if (value.LexicalValue != value.OriginalLexicalValue)
        {
            return value.LexicalValue;
        }
        return value.StringValue;
    }

    static void WriteDateValue(OdfNode node, OdfCellValue value)
    {
        string lexical = SelectDateLexical(value);
        bool valid = value.Kind == OdfValueKind.Date
            ? OdfLexicalParser.ParseDate(lexical).IsValid
            : OdfLexicalParser.ParseDateTime(lexical).IsValid;
        if (!valid)
        {
            throw new OdfEditException(
                "A date cell value requires a valid XML Schema date or date-time.");
        }
        RemoveValueAttributes(node);
        value.ValueType = "date";
        value.DateValue = lexical;
        value.LexicalValue = lexical;
        value.IsValid = true;
        WriteRequiredAttribute(
            node,
            OdfNamespaces.Office,
            "value-type",
            OdfNamespaces.OfficePrefix,
            "date");
        WriteRequiredAttribute(
            node,
            OdfNamespaces.Office,
            "date-value",
            OdfNamespaces.OfficePrefix,
            lexical);
    }

    static void WriteDurationValue(OdfNode node, OdfCellValue value)
    {
        string lexical = SelectDurationLexical(value);
        if (!OdfLexicalParser.ParseDuration(lexical).IsValid)
        {
            throw new OdfEditException(
                "A duration cell value requires a valid XML Schema duration.");
        }
        RemoveValueAttributes(node);
        value.ValueType = "time";
        value.DurationValue = lexical;
        value.LexicalValue = lexical;
        value.IsValid = true;
        WriteRequiredAttribute(
            node,
            OdfNamespaces.Office,
            "value-type",
            OdfNamespaces.OfficePrefix,
            "time");
        WriteRequiredAttribute(
            node,
            OdfNamespaces.Office,
            "time-value",
            OdfNamespaces.OfficePrefix,
            lexical);
    }

    static void WriteBooleanValue(OdfNode node, OdfCellValue value)
    {
        string lexical = SelectBooleanLexical(value);
        OdfLexicalValue parsed = OdfLexicalParser.ParseBoolean(lexical);
        RemoveValueAttributes(node);
        value.ValueType = "boolean";
        value.LexicalValue = lexical;
        value.BooleanValue = parsed.BooleanValue;
        value.IsValid = true;
        WriteRequiredAttribute(
            node,
            OdfNamespaces.Office,
            "value-type",
            OdfNamespaces.OfficePrefix,
            "boolean");
        WriteRequiredAttribute(
            node,
            OdfNamespaces.Office,
            "boolean-value",
            OdfNamespaces.OfficePrefix,
            lexical);
    }

    static string SelectDateLexical(OdfCellValue value)
    {
        if (value.DateValue != value.OriginalDateValue)
        {
            return value.DateValue;
        }
        if (value.LexicalValue != value.OriginalLexicalValue)
        {
            return value.LexicalValue;
        }
        return value.DateValue;
    }

    static string SelectDurationLexical(OdfCellValue value)
    {
        if (value.DurationValue != value.OriginalDurationValue)
        {
            return value.DurationValue;
        }
        if (value.LexicalValue != value.OriginalLexicalValue)
        {
            return value.LexicalValue;
        }
        return value.DurationValue;
    }

    static string SelectBooleanLexical(OdfCellValue value)
    {
        if (value.BooleanValue == value.OriginalBooleanValue
            && value.LexicalValue != value.OriginalLexicalValue
            && OdfLexicalParser.ParseBoolean(value.LexicalValue).IsValid)
        {
            return value.LexicalValue;
        }
        return value.BooleanValue ? "true" : "false";
    }

    static bool ValueChanged(OdfCellValue value)
    {
        return value.Kind != value.OriginalKind
            || value.ValueType != value.OriginalValueType
            || value.LexicalValue != value.OriginalLexicalValue
            || value.StringValue != value.OriginalStringValue
            || !value.NumericValue.Equals(value.OriginalNumericValue)
            || value.Currency != value.OriginalCurrency
            || value.DateValue != value.OriginalDateValue
            || value.DurationValue != value.OriginalDurationValue
            || value.BooleanValue != value.OriginalBooleanValue
            || value.Error != value.OriginalError
            || value.IsValid != value.OriginalIsValid;
    }

    static void RememberOriginalValue(OdfCellValue value)
    {
        value.OriginalKind = value.Kind;
        value.OriginalValueType = value.ValueType;
        value.OriginalLexicalValue = value.LexicalValue;
        value.OriginalStringValue = value.StringValue;
        value.OriginalNumericValue = value.NumericValue;
        value.OriginalCurrency = value.Currency;
        value.OriginalDateValue = value.DateValue;
        value.OriginalDurationValue = value.DurationValue;
        value.OriginalBooleanValue = value.BooleanValue;
        value.OriginalError = value.Error;
        value.OriginalIsValid = value.IsValid;
    }

    static void SetInvalidProvenance(OdfCellValue value)
    {
        if (!value.IsValid)
        {
            value.Provenance = OdfValueProvenance.InvalidPreserved;
        }
    }

    static bool HasOfficeValueAttribute(OdfNode node)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[index];
            if (attribute.Name.NamespaceName == OdfNamespaces.Office
                && IsValueAttributeName(attribute.Name.LocalName)
                && attribute.Name.LocalName != "value-type")
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool HasConflictingValueAttribute(
        OdfNode node,
        string allowedName)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[index];
            if (attribute.Name.NamespaceName == OdfNamespaces.Office
                && IsValueAttributeName(attribute.Name.LocalName)
                && attribute.Name.LocalName != "value-type"
                && attribute.Name.LocalName != allowedName
                && !(allowedName == "value"
                    && attribute.Name.LocalName == "currency"))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool IsValueAttributeName(string localName)
    {
        return localName == "value-type"
            || localName == "value"
            || localName == "string-value"
            || localName == "currency"
            || localName == "date-value"
            || localName == "time-value"
            || localName == "boolean-value";
    }

    static string FirstOfficeValue(OdfNode node)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[index];
            if (attribute.Name.NamespaceName == OdfNamespaces.Office
                && IsValueAttributeName(attribute.Name.LocalName)
                && attribute.Name.LocalName != "value-type")
            {
                return attribute.Value;
            }
            index = index + 1;
        }
        return "";
    }

    static OdfAttribute? FindOfficeAttribute(
        OdfNode node,
        string localName)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            OdfAttribute attribute = node.Attributes[index];
            if (attribute.Name.NamespaceName == OdfNamespaces.Office
                && attribute.Name.LocalName == localName)
            {
                return attribute;
            }
            index = index + 1;
        }
        return null;
    }

    static void RemoveValueAttributes(OdfNode node)
    {
        StyleDocumentModel.RemoveAttribute(
            node,
            OdfNamespaces.Office,
            "value-type");
        StyleDocumentModel.RemoveAttribute(
            node,
            OdfNamespaces.Office,
            "value");
        StyleDocumentModel.RemoveAttribute(
            node,
            OdfNamespaces.Office,
            "string-value");
        StyleDocumentModel.RemoveAttribute(
            node,
            OdfNamespaces.Office,
            "currency");
        StyleDocumentModel.RemoveAttribute(
            node,
            OdfNamespaces.Office,
            "date-value");
        StyleDocumentModel.RemoveAttribute(
            node,
            OdfNamespaces.Office,
            "time-value");
        StyleDocumentModel.RemoveAttribute(
            node,
            OdfNamespaces.Office,
            "boolean-value");
    }

    static void WriteRequiredAttribute(
        OdfNode node,
        string namespaceName,
        string localName,
        string prefix,
        string value)
    {
        StyleDocumentModel.WriteAttribute(
            node,
            namespaceName,
            localName,
            prefix,
            value);
    }

    static void WriteOptionalAttribute(
        OdfNode node,
        string namespaceName,
        string localName,
        string prefix,
        string value)
    {
        if (value.Length == 0)
        {
            StyleDocumentModel.RemoveAttribute(
                node,
                namespaceName,
                localName);
        }
        else
        {
            StyleDocumentModel.WriteAttribute(
                node,
                namespaceName,
                localName,
                prefix,
                value);
        }
    }

    static string ExtractCellText(OdfNode cell)
    {
        StringBuilder result = new StringBuilder();
        bool paragraph = false;
        int index = 0;
        while (index < cell.Children.Length)
        {
            OdfNode child = cell.Children[index];
            bool currentParagraph = IsTextElement(child, "p")
                || IsTextElement(child, "h");
            if (currentParagraph && paragraph)
            {
                result.Append('\n');
            }
            AppendCellText(result, child);
            if (currentParagraph)
            {
                paragraph = true;
            }
            index = index + 1;
        }
        return result.ToString();
    }

    static void AppendCellText(StringBuilder result, OdfNode node)
    {
        if (node.Kind == OdfNodeKind.Text)
        {
            result.Append(node.Value);
            return;
        }
        if (IsTextElement(node, "tab"))
        {
            result.Append('\t');
            return;
        }
        if (IsTextElement(node, "line-break"))
        {
            result.Append('\n');
            return;
        }
        if (IsTextElement(node, "s"))
        {
            AppendSpaceElement(result, node);
            return;
        }
        if (node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == OdfNamespaces.Table
            && node.Name.LocalName == "table")
        {
            return;
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            AppendCellText(result, node.Children[index]);
            index = index + 1;
        }
    }

    static void AppendSpaceElement(StringBuilder result, OdfNode node)
    {
        string lexical = StyleDocumentModel.ReadAttribute(
            node,
            OdfNamespaces.Text,
            "c");
        OdfLexicalValue parsed = OdfLexicalParser.ParseInteger(lexical);
        long count = parsed.IsValid && parsed.IntegerValue > 0
            ? parsed.IntegerValue
            : 1;
        if (count > 1000000)
        {
            count = 1000000;
        }
        long index = 0;
        while (index < count)
        {
            result.Append(' ');
            index = index + 1;
        }
    }

    static bool IsTextElement(OdfNode node, string localName)
    {
        return node.Kind == OdfNodeKind.Element
            && node.Name != null
            && node.Name.NamespaceName == OdfNamespaces.Text
            && node.Name.LocalName == localName;
    }

    static OdfFormulaError ParseFormulaError(string value)
    {
        if (value == OdfFormulaNames.ErrorNull)
        {
            return OdfFormulaError.Null;
        }
        if (value == OdfFormulaNames.ErrorDivisionByZero)
        {
            return OdfFormulaError.DivisionByZero;
        }
        if (value == OdfFormulaNames.ErrorValue)
        {
            return OdfFormulaError.Value;
        }
        if (value == OdfFormulaNames.ErrorReference)
        {
            return OdfFormulaError.Reference;
        }
        if (value == OdfFormulaNames.ErrorName)
        {
            return OdfFormulaError.Name;
        }
        if (value == OdfFormulaNames.ErrorNumber)
        {
            return OdfFormulaError.Number;
        }
        if (value == OdfFormulaNames.ErrorNotAvailable)
        {
            return OdfFormulaError.NotAvailable;
        }
        return OdfFormulaError.None;
    }

    static string FormatFormulaError(OdfFormulaError error)
    {
        if (error == OdfFormulaError.Null)
        {
            return OdfFormulaNames.ErrorNull;
        }
        if (error == OdfFormulaError.DivisionByZero)
        {
            return OdfFormulaNames.ErrorDivisionByZero;
        }
        if (error == OdfFormulaError.Value)
        {
            return OdfFormulaNames.ErrorValue;
        }
        if (error == OdfFormulaError.Reference)
        {
            return OdfFormulaNames.ErrorReference;
        }
        if (error == OdfFormulaError.Name)
        {
            return OdfFormulaNames.ErrorName;
        }
        if (error == OdfFormulaError.Number)
        {
            return OdfFormulaNames.ErrorNumber;
        }
        if (error == OdfFormulaError.NotAvailable)
        {
            return OdfFormulaNames.ErrorNotAvailable;
        }
        return "";
    }
}
