using System.Collections.Generic;
using System.Text;

namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="FormulaCalculator"/>.
/// </summary>
public interface IFormulaCalculator
{
    /// <summary>
    /// Returns the Hill-system molecular formula without isotope labels.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    string Formula(Molecule? molecule);

    /// <summary>
    /// Returns the Hill-system molecular formula with explicit isotope labels.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    string IsotopeFormula(Molecule? molecule);
}

