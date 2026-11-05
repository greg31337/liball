using System.Globalization;

namespace odf;

internal static class FormulaCachedValueUpdater
{
    internal static void Update(
        FormulaCalculationNode node,
        OdfFormulaResult result,
        OdfSpreadsheetSettings settings)
    {
        if (node.Cell == null)
        {
            return;
        }
        OdfFormulaResult scalar = SelectScalar(result);
        OdfCellValue value = node.Cell.Value;
        if (value == null)
        {
            value = new OdfCellValue();
            node.Cell.Value = value;
        }
        string currency = value.Currency;
        ClearValue(value);
        value.Cell = node.Cell;
        value.Provenance = OdfValueProvenance.Computed;
        value.IsCachedFormulaResult = node.Formula != null
            && scalar.Kind != OdfValueKind.Empty;
        value.IsValid = scalar.IsValid;
        if (scalar.Kind == OdfValueKind.Empty)
        {
            value.Kind = OdfValueKind.Empty;
            value.ValueType = "void";
            return;
        }
        if (scalar.Kind == OdfValueKind.Error)
        {
            value.Kind = OdfValueKind.Error;
            value.ValueType = "string";
            value.StringValue = scalar.StringValue.Length > 0
                ? scalar.StringValue
                : FormulaValueModel.FormatError(scalar.Error);
            value.LexicalValue = value.StringValue;
            value.Error = scalar.Error;
            return;
        }
        if (scalar.Kind == OdfValueKind.String
            || scalar.Kind == OdfValueKind.Complex)
        {
            value.Kind = OdfValueKind.String;
            value.ValueType = "string";
            value.StringValue = scalar.StringValue;
            value.LexicalValue = scalar.StringValue;
            return;
        }
        if (scalar.Kind == OdfValueKind.Boolean)
        {
            value.Kind = OdfValueKind.Boolean;
            value.ValueType = "boolean";
            value.BooleanValue = scalar.BooleanValue;
            value.LexicalValue = scalar.BooleanValue ? "true" : "false";
            return;
        }
        if (scalar.Kind == OdfValueKind.Date
            || scalar.Kind == OdfValueKind.DateTime)
        {
            AssignDate(value, scalar, settings);
            return;
        }
        if (scalar.Kind == OdfValueKind.Time
            || scalar.Kind == OdfValueKind.Duration)
        {
            AssignDuration(value, scalar);
            return;
        }
        value.Kind = scalar.Kind == OdfValueKind.Decimal
            ? OdfValueKind.Float
            : scalar.Kind;
        value.ValueType = value.Kind == OdfValueKind.Percentage
            ? "percentage"
            : value.Kind == OdfValueKind.Currency ? "currency" : "float";
        value.UnitKind = value.Kind == OdfValueKind.Percentage
            ? OdfUnitKind.Percentage
            : OdfUnitKind.Unitless;
        value.NumericValue = scalar.NumericValue;
        value.LexicalValue = scalar.NumericValue.ToString(
            "R",
            CultureInfo.InvariantCulture);
        if (value.Kind == OdfValueKind.Currency)
        {
            value.Currency = currency;
            value.IsValid = value.IsValid && currency.Length > 0;
        }
    }

    static OdfFormulaResult SelectScalar(OdfFormulaResult result)
    {
        if (result.Kind != OdfValueKind.Array
            && result.Kind != OdfValueKind.Reference)
        {
            return result;
        }
        if (result.Values.Length == 0)
        {
            return FormulaValueModel.CreateError(OdfFormulaError.Value);
        }
        return result.Values[0];
    }

    static void AssignDate(
        OdfCellValue value,
        OdfFormulaResult result,
        OdfSpreadsheetSettings settings)
    {
        OdfFormulaResult temporal = result;
        if (temporal.StringValue.Length == 0)
        {
            temporal = FormulaTemporalModel.TemporalFromSerial(
                result.NumericValue,
                result.Kind,
                settings,
                0,
                false);
        }
        value.Kind = result.Kind;
        value.ValueType = "date";
        value.NumericValue = result.NumericValue;
        value.DateValue = temporal.StringValue;
        value.LexicalValue = temporal.StringValue;
        value.IsValid = temporal.Error == OdfFormulaError.None
            && temporal.StringValue.Length > 0;
    }

    static void AssignDuration(
        OdfCellValue value,
        OdfFormulaResult result)
    {
        OdfFormulaResult duration = result.StringValue.Length > 0
            ? result
            : FormulaTemporalModel.DurationFromSerial(result.NumericValue);
        value.Kind = OdfValueKind.Duration;
        value.ValueType = "time";
        value.NumericValue = result.NumericValue;
        value.DurationValue = duration.StringValue;
        value.LexicalValue = duration.StringValue;
        value.IsValid = duration.Error == OdfFormulaError.None
            && duration.StringValue.Length > 0;
    }

    static void ClearValue(OdfCellValue value)
    {
        value.Kind = OdfValueKind.Empty;
        value.UnitKind = OdfUnitKind.Unknown;
        value.ValueType = "";
        value.LexicalValue = "";
        value.StringValue = "";
        value.NumericValue = 0;
        value.Currency = "";
        value.DateValue = "";
        value.DurationValue = "";
        value.BooleanValue = false;
        value.Error = OdfFormulaError.None;
        value.IsValid = true;
        value.IsCachedFormulaResult = false;
    }
}
