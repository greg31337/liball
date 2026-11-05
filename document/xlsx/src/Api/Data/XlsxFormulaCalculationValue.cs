using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Represents formula calculation value data used by the public workbook API.
/// </summary>
public class XlsxFormulaCalculationValue
{
    /// <summary>
    /// Stores the kind value used by this model.
    /// </summary>
    public XlsxFormulaCalculationValueKind Kind;
    /// <summary>
    /// Stores the number value used by this model.
    /// </summary>
    public double Number;
    /// <summary>
    /// Stores the text value used by this model.
    /// </summary>
    public string Text;
    /// <summary>
    /// Stores the boolean value used by this model.
    /// </summary>
    public bool Boolean;
    /// <summary>
    /// Stores the error value used by this model.
    /// </summary>
    public string Error;
    /// <summary>
    /// Contains items.
    /// </summary>
    public List<XlsxFormulaCalculationValue> Items;
    /// <summary>
    /// Contains rows.
    /// </summary>
    public int Rows;
    /// <summary>
    /// Contains columns.
    /// </summary>
    public int Columns;
    /// <summary>
    /// Contains lambda parameters.
    /// </summary>
    public List<string> LambdaParameters;
    /// <summary>
    /// Stores the lambda body value used by this model.
    /// </summary>
    public XlsxFormulaNode? LambdaBody;
    /// <summary>
    /// Initializes a new formula calculation value with its default state.
    /// </summary>
    public XlsxFormulaCalculationValue()
    {
        Kind = XlsxFormulaCalculationValueKind.Blank;
        Number = 0;
        Text = "";
        Boolean = false;
        Error = "";
        Items = new List<XlsxFormulaCalculationValue>();
        Rows = 0;
        Columns = 0;
        LambdaParameters = new List<string>();
        LambdaBody = null;
    }

    /// <summary>
    /// Creates blank and initializes the fields required by the OOXML format.
    /// </summary>
    /// <returns>The formula calculation value produced by the operation.</returns>
    internal static XlsxFormulaCalculationValue CreateBlank()
    {
        return new XlsxFormulaCalculationValue();
    }

    /// <summary>
    /// Creates number and initializes the fields required by the OOXML format.
    /// </summary>
    /// <param name = "number">The number value used by the operation.</param>
    /// <returns>The formula calculation value produced by the operation.</returns>
    internal static XlsxFormulaCalculationValue CreateNumber(double number)
    {
        XlsxFormulaCalculationValue value = new XlsxFormulaCalculationValue();
        value.Kind = XlsxFormulaCalculationValueKind.Number;
        value.Number = number;
        return value;
    }

    /// <summary>
    /// Creates text and initializes the fields required by the OOXML format.
    /// </summary>
    /// <param name = "text">The text value used by the operation.</param>
    /// <returns>The formula calculation value produced by the operation.</returns>
    internal static XlsxFormulaCalculationValue CreateText(string text)
    {
        XlsxFormulaCalculationValue value = new XlsxFormulaCalculationValue();
        value.Kind = XlsxFormulaCalculationValueKind.Text;
        value.Text = XlsxCellValueConverter.NormalizeText(text);
        return value;
    }

    /// <summary>
    /// Creates boolean and initializes the fields required by the OOXML format.
    /// </summary>
    /// <param name = "boolean">The boolean value used by the operation.</param>
    /// <returns>The formula calculation value produced by the operation.</returns>
    internal static XlsxFormulaCalculationValue CreateBoolean(bool boolean)
    {
        XlsxFormulaCalculationValue value = new XlsxFormulaCalculationValue();
        value.Kind = XlsxFormulaCalculationValueKind.Boolean;
        value.Boolean = boolean;
        return value;
    }

    /// <summary>
    /// Creates error and initializes the fields required by the OOXML format.
    /// </summary>
    /// <param name = "error">The error value used by the operation.</param>
    /// <returns>The formula calculation value produced by the operation.</returns>
    internal static XlsxFormulaCalculationValue CreateError(string error)
    {
        XlsxFormulaCalculationValue value = new XlsxFormulaCalculationValue();
        value.Kind = XlsxFormulaCalculationValueKind.Error;
        value.Error = XlsxCellValueConverter.NormalizeText(error);
        if (value.Error.Length == 0)
        {
            value.Error = "#VALUE!";
        }

        return value;
    }

    internal static XlsxFormulaCalculationValue CreateRange()
    {
        XlsxFormulaCalculationValue value = new XlsxFormulaCalculationValue();
        value.Kind = XlsxFormulaCalculationValueKind.Range;
        value.Rows = 0;
        value.Columns = 0;
        return value;
    }

    internal static XlsxFormulaCalculationValue CreateLambda()
    {
        XlsxFormulaCalculationValue value = new XlsxFormulaCalculationValue();
        value.Kind = XlsxFormulaCalculationValueKind.Lambda;
        return value;
    }
}
