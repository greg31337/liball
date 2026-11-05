namespace odf;

/// <summary>
/// Describes one constant or formula cell participating in recalculation.
/// </summary>
public class OdfFormulaCalculationCell
{
    /// <summary>The normalized cell or named-expression key.</summary>
    public string Key = "";
    /// <summary>The sheet that owns the formula.</summary>
    public string OwnerSheet = "";
    /// <summary>The parsed formula, or <c>null</c> for a constant cell.</summary>
    public OdfFormula? Formula;
    /// <summary>The current constant, cached, or calculated result.</summary>
    public OdfFormulaResult? Result;
    /// <summary>The optional document cell whose cached value is updated.</summary>
    public OdfCell? Cell;
    /// <summary>Whether the cell and its dependents require recalculation.</summary>
    public bool Dirty;
    internal FormulaCalculationNode? CalculationNode;
}
