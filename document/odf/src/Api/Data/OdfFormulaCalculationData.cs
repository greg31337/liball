namespace odf;

/// <summary>
/// Supplies and receives the state of a formula recalculation pass.
/// </summary>
public class OdfFormulaCalculationData
{
    /// <summary>The cells participating in dependency calculation.</summary>
    public OdfFormulaCalculationCell[] Cells = [];
    /// <summary>Workbook and deterministic volatile-function inputs.</summary>
    public OdfFormulaEvaluationData EvaluationData =
        new OdfFormulaEvaluationData();
    /// <summary>The iterative and numerical calculation settings.</summary>
    public OdfSpreadsheetSettings SpreadsheetSettings =
        new OdfSpreadsheetSettings();
    /// <summary>Keys whose values changed before this recalculation pass.</summary>
    public string[] ChangedKeys = [];
    /// <summary>Whether every formula should be recalculated once.</summary>
    public bool RecalculateAll;
    /// <summary>The keys scheduled during the most recent pass.</summary>
    public string[] ScheduledKeys = [];
    /// <summary>The keys identified as members of circular dependencies.</summary>
    public string[] CircularKeys = [];
    /// <summary>The results corresponding to <see cref="Cells"/> after calculation.</summary>
    public OdfFormulaResult[] Results = [];
    internal FormulaCalculationGraph? CalculationGraph;
}
