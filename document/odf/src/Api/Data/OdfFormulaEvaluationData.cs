namespace odf;

/// <summary>
/// Supplies deterministic values and workbook context for formula evaluation.
/// </summary>
public class OdfFormulaEvaluationData
{
    /// <summary>The sheet that owns the formula.</summary>
    public string OwnerSheet = "";
    /// <summary>Normalized cell, range, and named-expression keys.</summary>
    public string[] ReferenceKeys = [];
    /// <summary>Values corresponding to <see cref="ReferenceKeys"/>.</summary>
    public OdfFormulaResult[] ReferenceValues = [];
    /// <summary>Function names whose externally supplied results are available.</summary>
    public string[] ExternalFunctionNames = [];
    /// <summary>Results corresponding to <see cref="ExternalFunctionNames"/>.</summary>
    public OdfFormulaResult[] ExternalFunctionResults = [];
    /// <summary>The deterministic current date and time for volatile functions.</summary>
    public OdfFormulaResult? CurrentDateTime;
    /// <summary>The deterministic random value in the half-open interval from zero to one.</summary>
    public double RandomValue = -1;
    /// <summary>The spreadsheet calculation settings.</summary>
    public OdfSpreadsheetSettings SpreadsheetSettings =
        new OdfSpreadsheetSettings();
    /// <summary>The current formula-cell value.</summary>
    public OdfFormulaResult? CurrentValue;
    /// <summary>The current formula-cell address.</summary>
    public string CurrentAddress = "";
    /// <summary>The formula text associated with a referenced cell.</summary>
    public string ReferencedFormula = "";
    /// <summary>The current file name.</summary>
    public string FileName = "";
    /// <summary>The current directory name.</summary>
    public string Directory = "";
    /// <summary>The host operating-system description.</summary>
    public string OperatingSystem = "";
    /// <summary>The host application release description.</summary>
    public string Release = "";
    /// <summary>The localized recalculation-mode description.</summary>
    public string RecalculationMode = "Automatic";
    /// <summary>The current document origin.</summary>
    public string Origin = "";
    /// <summary>The localized decimal separator.</summary>
    public string DecimalSeparator = ".";
    /// <summary>The localized digit-group separator.</summary>
    public string GroupSeparator = ",";
    /// <summary>The current cell format identifier.</summary>
    public string CellFormat = "G";
    /// <summary>The current cell prefix.</summary>
    public string CellPrefix = "";
    /// <summary>The available sheet names.</summary>
    public string[] SheetNames = [];
    /// <summary>The sheet numbers corresponding to <see cref="SheetNames"/>.</summary>
    public int[] SheetNumbers = [];
    /// <summary>The one-based current row number.</summary>
    public int CurrentRowNumber;
    /// <summary>The one-based current column number.</summary>
    public int CurrentColumnNumber;
    /// <summary>The one-based current sheet number.</summary>
    public int CurrentSheetNumber;
    /// <summary>The one-based referenced row number.</summary>
    public int ReferenceRowNumber;
    /// <summary>The one-based referenced column number.</summary>
    public int ReferenceColumnNumber;
    /// <summary>The one-based referenced sheet number.</summary>
    public int ReferenceSheetNumber;
    /// <summary>The number of active worksheets.</summary>
    public int ActiveWorksheetCount;
    /// <summary>The number of sheets in the document.</summary>
    public int SheetCount;
    /// <summary>The number of sheets in the current reference.</summary>
    public int ReferenceSheetCount;
    /// <summary>The available-memory value supplied to information functions.</summary>
    public double AvailableMemory;
    /// <summary>The used-memory value supplied to information functions.</summary>
    public double UsedMemory;
    /// <summary>The total-memory value supplied to information functions.</summary>
    public double TotalMemory;
    /// <summary>The current cell width supplied to information functions.</summary>
    public double CellWidth;
    /// <summary>Whether the current cell format uses a negative-value color.</summary>
    public bool CellUsesNegativeColor;
    /// <summary>Whether the current cell is protected.</summary>
    public bool CellProtected;
    /// <summary>Whether the current cell format uses parentheses.</summary>
    public bool CellUsesParentheses;
    /// <summary>The maximum parser depth used for formula text encountered during recalculation.</summary>
    public int MaximumDepth = 256;
}
