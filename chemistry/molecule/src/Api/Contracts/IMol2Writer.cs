using System.Collections.Generic;
using System.Text;

namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="Mol2Writer"/>.
/// </summary>
public interface IMol2Writer
{
    /// <summary>
    /// Writes one molecule as MOL2 text.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting MOL2 text; an empty string indicates that no value could be produced.</returns>
    string WriteMol2(Molecule? molecule);

    /// <summary>
    /// Writes one molecule as MOL2 and returns section counts and diagnostics.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The output text, emitted fields, section counts, and diagnostics.</returns>
    Mol2WriteResult WriteMol2WithResult(Molecule? molecule);
}
