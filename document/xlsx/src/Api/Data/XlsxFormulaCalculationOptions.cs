using System;

namespace xlsx;

/// <summary>
/// Controls workbook-aware formula calculation and cached-value handling.
/// </summary>
public class XlsxFormulaCalculationOptions
{
    /// <summary>
    /// Indicates whether references to other worksheets in the current workbook may be evaluated.
    /// </summary>
    public bool AllowCrossSheetReferences;
    /// <summary>
    /// Indicates whether volatile functions may be evaluated.
    /// </summary>
    public bool AllowVolatileFunctions;
    /// <summary>
    /// Indicates whether functions classified as unsafe may be evaluated.
    /// </summary>
    public bool AllowUnsafeFunctions;
    /// <summary>
    /// Indicates whether an existing cached value is retained when a function is unsupported.
    /// </summary>
    public bool PreserveCachedValuesForUnsupported;
    /// <summary>
    /// Indicates whether workbook calculation continues after a formula error.
    /// </summary>
    public bool ContinueAfterError;
    /// <summary>
    /// Stores the maximum number of dependency passes used to resolve formulas.
    /// </summary>
    public int MaximumPasses;

    /// <summary>
    /// Initializes formula calculation options with conservative active-content handling.
    /// </summary>
    public XlsxFormulaCalculationOptions()
    {
        AllowCrossSheetReferences = true;
        AllowVolatileFunctions = true;
        AllowUnsafeFunctions = false;
        PreserveCachedValuesForUnsupported = true;
        ContinueAfterError = true;
        MaximumPasses = 16;
    }
}
