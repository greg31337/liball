using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Controls the behavior of workbook validation operations.
/// </summary>
public class XlsxWorkbookValidationOptions
{
    /// <summary>
    /// Stores the validate package value used by this model.
    /// </summary>
    public bool ValidatePackage;
    /// <summary>
    /// Stores the validate workbook value used by this model.
    /// </summary>
    public bool ValidateWorkbook;
    /// <summary>
    /// Contains validate worksheets.
    /// </summary>
    public bool ValidateWorksheets;
    /// <summary>
    /// Contains validate styles.
    /// </summary>
    public bool ValidateStyles;
    /// <summary>
    /// Contains validate formulas.
    /// </summary>
    public bool ValidateFormulas;
    /// <summary>
    /// Contains validate relationships.
    /// </summary>
    public bool ValidateRelationships;
    /// <summary>
    /// Contains validate drawings.
    /// </summary>
    public bool ValidateDrawings;
    /// <summary>
    /// Contains validate tables.
    /// </summary>
    public bool ValidateTables;
    /// <summary>
    /// Contains validate pivots.
    /// </summary>
    public bool ValidatePivots;
    /// <summary>
    /// Contains validate security limits.
    /// </summary>
    public bool ValidateSecurityLimits;
    /// <summary>
    /// Contains validate unknown parts.
    /// </summary>
    public bool ValidateUnknownParts;
    /// <summary>
    /// Contains reader options.
    /// </summary>
    public XlsxReaderOptions ReaderOptions;
    /// <summary>
    /// Initializes a new workbook validation options with its default state.
    /// </summary>
    public XlsxWorkbookValidationOptions()
    {
        ValidatePackage = true;
        ValidateWorkbook = true;
        ValidateWorksheets = true;
        ValidateStyles = true;
        ValidateFormulas = true;
        ValidateRelationships = true;
        ValidateDrawings = true;
        ValidateTables = true;
        ValidatePivots = true;
        ValidateSecurityLimits = true;
        ValidateUnknownParts = true;
        ReaderOptions = new XlsxReaderOptions();
    }
}
