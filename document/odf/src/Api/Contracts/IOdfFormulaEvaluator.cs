namespace odf;

/// <summary>
/// Evaluates individual OpenFormula expressions and dependency graphs.
/// </summary>
public interface IOdfFormulaEvaluator
{
    /// <summary>Evaluates one parsed formula.</summary>
    /// <param name="formula">The parsed formula.</param>
    /// <param name="data">The deterministic workbook evaluation data.</param>
    /// <returns>The formula result.</returns>
    public OdfFormulaResult EvaluateFormula(
        OdfFormula formula,
        OdfFormulaEvaluationData data);
    /// <summary>Recalculates a collection of dependent formula cells.</summary>
    /// <param name="data">The mutable calculation-run data.</param>
    /// <returns>The updated calculation-run data.</returns>
    public OdfFormulaCalculationData RecalculateFormulas(
        OdfFormulaCalculationData data);
}
