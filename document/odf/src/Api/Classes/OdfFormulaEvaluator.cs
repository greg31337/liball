namespace odf;

/// <summary>
/// Evaluates individual OpenFormula expressions and dependency graphs.
/// </summary>
public class OdfFormulaEvaluator : IOdfFormulaEvaluator
{
    /// <summary>Initializes a formula evaluator.</summary>
    public OdfFormulaEvaluator()
    {
    }

    /// <summary>Evaluates one parsed formula.</summary>
    public OdfFormulaResult EvaluateFormula(
        OdfFormula formula,
        OdfFormulaEvaluationData data)
    {
        if (formula == null || data == null)
        {
            throw new OdfFormulaException(
                "A parsed formula and evaluation data are required.");
        }
        return FormulaExpressionEvaluator.Evaluate(
            formula,
            data,
            data.OwnerSheet,
            null);
    }

    /// <summary>Recalculates a collection of dependent formula cells.</summary>
    public OdfFormulaCalculationData RecalculateFormulas(
        OdfFormulaCalculationData data)
    {
        return FormulaCalculationApiModel.Recalculate(data);
    }
}
