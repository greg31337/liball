using System.Text;

namespace molecule;


/// <summary>
/// Defines the public operations provided by <see cref="PdbWriter"/>.
/// </summary>
public interface IPdbWriter
{
    /// <summary>
    /// Writes one molecule as PDB text.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting PDB text; an empty string indicates that no value could be produced.</returns>
    string WritePdb(Molecule? molecule);

    /// <summary>
    /// Writes one molecule as PDB and returns emitted record counts and diagnostics.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The output text, record counts, and diagnostics.</returns>
    PdbWriteResult WritePdbWithResult(Molecule? molecule);
}
