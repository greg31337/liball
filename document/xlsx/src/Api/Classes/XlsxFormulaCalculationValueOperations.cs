using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;

/// <summary>
/// Provides public operations for <see cref="XlsxFormulaCalculationValue"/> data.
/// </summary>
public class XlsxFormulaCalculationValueOperations : IXlsxFormulaCalculationValueOperations
{
    /// <summary>
    /// Initializes a new xlsx formula calculation value operations instance.
    /// </summary>
    public XlsxFormulaCalculationValueOperations()
    {
    }

    /// <summary>
    /// Invokes <see cref="XlsxFormulaCalculationValue"/>.CreateBlank through the CreateBlank operation.
    /// </summary>
    /// <returns>The value produced by the operation.</returns>
    public XlsxFormulaCalculationValue CreateBlank()
    {
        return XlsxFormulaCalculationValue.CreateBlank();
    }

    /// <summary>
    /// Invokes <see cref="XlsxFormulaCalculationValue"/>.CreateNumber through the CreateNumber operation.
    /// </summary>
    /// <param name="number">The number value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxFormulaCalculationValue CreateNumber(double number)
    {
        return XlsxFormulaCalculationValue.CreateNumber(number);
    }

    /// <summary>
    /// Invokes <see cref="XlsxFormulaCalculationValue"/>.CreateText through the CreateText operation.
    /// </summary>
    /// <param name="text">The text value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxFormulaCalculationValue CreateText(string text)
    {
        return XlsxFormulaCalculationValue.CreateText(text);
    }

    /// <summary>
    /// Invokes <see cref="XlsxFormulaCalculationValue"/>.CreateBoolean through the CreateBoolean operation.
    /// </summary>
    /// <param name="boolean">The boolean value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxFormulaCalculationValue CreateBoolean(bool boolean)
    {
        return XlsxFormulaCalculationValue.CreateBoolean(boolean);
    }

    /// <summary>
    /// Invokes <see cref="XlsxFormulaCalculationValue"/>.CreateError through the CreateError operation.
    /// </summary>
    /// <param name="error">The error value used by the operation.</param>
    /// <returns>The value produced by the operation.</returns>
    public XlsxFormulaCalculationValue CreateError(string error)
    {
        return XlsxFormulaCalculationValue.CreateError(error);
    }
}
