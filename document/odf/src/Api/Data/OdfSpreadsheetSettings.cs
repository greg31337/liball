namespace odf;

/// <summary>Describes spreadsheet calculation and view settings.</summary>
public class OdfSpreadsheetSettings
{
    /// <summary>Whether an explicit calculation-settings element is present.</summary>
    public bool HasCalculationSettings;
    /// <summary>Whether text comparisons distinguish upper and lower case.</summary>
    public bool CaseSensitive = true;
    /// <summary>Whether calculations use the rounded values displayed in cells.</summary>
    public bool PrecisionAsShown;
    /// <summary>Whether search criteria must match the entire cell content.</summary>
    public bool MatchWholeCell = true;
    /// <summary>Whether row and column labels are found automatically.</summary>
    public bool AutomaticFindLabels = true;
    /// <summary>Whether regular expressions are enabled for string comparisons.</summary>
    public bool UseRegularExpressions = true;
    /// <summary>Whether wildcard matching is enabled for string comparisons.</summary>
    public bool UseWildcards;
    /// <summary>The first year used when expanding two-digit years.</summary>
    public long NullYear = 1930;
    /// <summary>Whether an explicit null-date element is present.</summary>
    public bool HasNullDate;
    /// <summary>The lexical date whose spreadsheet serial value is zero.</summary>
    public string NullDate = "1899-12-30";
    /// <summary>Whether an explicit iteration element is present.</summary>
    public bool HasIteration;
    /// <summary>Whether iterative calculation of cyclic references is enabled.</summary>
    public bool IterationEnabled;
    /// <summary>The maximum number of iterative calculations.</summary>
    public long IterationSteps = 100;
    /// <summary>The convergence difference that stops iterative calculation.</summary>
    public double MaximumDifference = 0.001;
    /// <summary>The name of the active spreadsheet table.</summary>
    public string ActiveTable = "";
    /// <summary>The application-defined lexical list of selected table names.</summary>
    public string SelectedTables = "";
    /// <summary>The application-defined lexical list of selected cell ranges.</summary>
    public string SelectedRanges = "";
    /// <summary>Whether insertion, deletion, movement, and renaming of tables is protected.</summary>
    public bool StructureProtected;
    /// <summary>The preserved spreadsheet-structure protection key.</summary>
    public string ProtectionKey = "";
    /// <summary>The spreadsheet protection-key digest algorithm IRI.</summary>
    public string ProtectionKeyDigestAlgorithm = "";
    /// <summary>The underlying calculation-settings element, when present.</summary>
    public OdfNode? CalculationNode;
    /// <summary>The underlying null-date element, when present.</summary>
    public OdfNode? NullDateNode;
    /// <summary>The underlying iteration element, when present.</summary>
    public OdfNode? IterationNode;
    internal bool OriginalHasCalculationSettings;
    internal bool OriginalHasNullDate;
    internal bool OriginalHasIteration;
    internal bool OriginalCaseSensitive = true;
    internal bool OriginalPrecisionAsShown;
    internal bool OriginalMatchWholeCell = true;
    internal bool OriginalAutomaticFindLabels = true;
    internal bool OriginalUseRegularExpressions = true;
    internal bool OriginalUseWildcards;
    internal long OriginalNullYear = 1930;
    internal string OriginalNullDate = "1899-12-30";
    internal bool OriginalIterationEnabled;
    internal long OriginalIterationSteps = 100;
    internal double OriginalMaximumDifference = 0.001;
    internal string OriginalActiveTable = "";
    internal string OriginalSelectedTables = "";
    internal string OriginalSelectedRanges = "";
    internal bool OriginalStructureProtected;
    internal string OriginalProtectionKey = "";
    internal string OriginalProtectionKeyDigestAlgorithm = "";
    internal OdfNode? ActiveTableNode;
    internal OdfNode? SelectedTablesNode;
    internal OdfNode? SelectedRangesNode;
}
